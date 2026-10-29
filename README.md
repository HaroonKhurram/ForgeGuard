# ForgeGuard: Industrial Anomaly Detection and Prediction System

**Real-time Industrial IoT Sensor Monitor**

A real-time monitoring system for factory machines. It receives live vibration and temperature readings over MQTT, answers questions about recent data in O(log n) time using custom data structures written in C#, and warns the operator early about unusual machine behaviour using an LSTM Autoencoder in Python. If the ML service goes offline, the system keeps working with simple threshold alerts.

> **Academic project.** Developed at the Institute of Data Sciences, University of Engineering and Technology (UET) Lahore, Session 2025-2029. Supervised by Ms. Hijab Zaidi.
>
> **Project status:** in development. Sections marked *(planned)* describe work that is not finished yet. Benchmark results will be added once they have been measured.

---

## Table of Contents

1. [Problem Statement](#problem-statement)
2. [Key Features](#key-features)
3. [System Architecture](#system-architecture)
4. [Data Structures and Algorithms](#data-structures-and-algorithms)
5. [Anomaly Detection](#anomaly-detection)
6. [Tech Stack](#tech-stack)
7. [Project Structure](#project-structure)
8. [Getting Started](#getting-started)
9. [Usage](#usage)
10. [Testing and Benchmarking](#testing-and-benchmarking)
11. [Dataset](#dataset)
12. [Scope and Limitations](#scope-and-limitations)
13. [Roadmap](#roadmap)
14. [Team](#team)
15. [Acknowledgements](#acknowledgements)
16. [References](#references)
17. [License](#license)

---

## Problem Statement

Unplanned downtime is one of the most expensive things that can happen in a factory, yet the machine usually gives signs earlier: a bearing wears, vibration shifts a little, temperature creeps up. Factories mostly rely on scheduled maintenance, which wastes time and parts, or on fixed-limit alarms such as "alert above a set temperature". A fixed limit watches one signal at a time and reacts only after it has gone far, so a machine can look normal on each signal while the combination is already a warning sign.

There is also a data problem: sensors send many readings every second, and rescanning all of them for every question soon becomes slow.

ForgeGuard addresses both problems. A fast tier (custom data structures) answers window queries instantly, and a deep tier (LSTM Autoencoder) catches subtle, multi-signal faults.

## Key Features

- **Live data ingestion** from MQTT topics such as `factory/line1/motor3/vibration`.
- **Burst protection:** an incoming queue sits between the MQTT receiver and the processing stage, so bursts of messages are buffered and not lost.
- **Rolling window storage** in a custom array-based Circular Buffer that overwrites the oldest reading when full.
- **Fast window queries:** max, min and sum over the last N minutes using a custom Segment Tree, in O(log n) instead of O(n).
- **Time-window lookup** with a recursive binary search on timestamps.
- **Window statistics:** median and percentiles computed with our own merge sort and quick sort. Late or out-of-order readings are put back in order with insertion sort.
- **Threshold alerts** that fire without waiting for the ML service.
- **Deep anomaly detection:** an LSTM Autoencoder trained on normal data flags unusual windows, including combinations of signals that fixed limits miss.
- **Failure risk levels** per machine: Normal, Warning or Critical, based on the trend of anomaly scores.
- **Sensor health monitoring** through the MQTT Last Will message (offline sensors are marked).
- **Alert history** kept in a linked list (newest first), with an alert queue for alerts waiting to be shown or sent.
- **Graceful degradation:** if the ML service is unreachable, ingestion, window queries and threshold alerts continue, pending windows are queued for retry, and the dashboard shows the ML service status.
- **Browser dashboard** with live graphs, query results, colour-coded machine status and role-based sign-in (operator, maintenance engineer, admin).

## System Architecture

ForgeGuard uses a layered, two-tier design.

```
 Sensor simulator           MQTT broker              C# web application (fast tier)
 (replays dataset)  --->    (Mosquitto)    --->   +------------------------------------+
                                                  | MQTT subscriber                    |
                                                  |   -> incoming queue                |
                                                  |   -> Circular Buffer + Segment Tree|
                                                  |   -> threshold alerts              |
                                                  |   -> alert queue + linked list log |
                                                  +----------------+-------------------+
                                                                   |
                                    REST (rolling window)          |        live data, alerts,
                                                                   |        risk levels
                                                  +----------------v-------+     +------------------+
                                                  | Python FastAPI service |     | Database         |
                                                  | LSTM Autoencoder       |     | alerts, users,   |
                                                  | (deep tier)            |     | predictions      |
                                                  +------------------------+     +------------------+
                                                                   |
                                                       React dashboard in the browser
```

| Layer | Responsibility |
|---|---|
| Data source | Sensor simulator replays dataset readings to the broker |
| Messaging | MQTT broker receives readings and delivers them to subscribers |
| Processing (fast tier) | C# backend stores readings, answers window queries, raises threshold alerts |
| Analytics (deep tier) | FastAPI service runs the LSTM Autoencoder |
| Storage | Database keeps alerts, predictions, users and settings |
| Presentation | React dashboard shows live data, alerts and risk levels |

**How one reading flows through the system**

1. A sensor (or the replay script) publishes a timestamped reading to an MQTT topic.
2. The backend subscribes and places the reading in the incoming queue.
3. The reading is stored in that sensor's Circular Buffer and Segment Tree.
4. The fast tier checks thresholds on every reading and answers dashboard queries.
5. Every few seconds the current window is sent over REST to the Python service, which returns a reconstruction-error score and an anomaly flag.
6. The backend tracks the trend of scores and updates the machine's risk level, then pushes alerts and status to the dashboard.

## Data Structures and Algorithms

All of the following are implemented by us in C#, not taken from a library.

| Component | What it does in ForgeGuard |
|---|---|
| **Circular Buffer** (array used as a circular queue) | Keeps only the most recent window of readings and overwrites the oldest when full |
| **Segment Tree** (built recursively over the buffer's array) | Max, min and sum over the last N minutes in O(log n) |
| **Incoming queue** | Buffers bursts of MQTT messages between the receiver and the processing stage |
| **Alert queue** | Holds alerts waiting to be shown or sent |
| **Merge sort and quick sort** (recursive) | Median and percentiles (for example the 95th percentile of vibration) of a window |
| **Insertion sort** | Re-orders late or out-of-order readings, which are nearly sorted already |
| **Recursive binary search** | Finds where the last N minutes begin in the timestamp-ordered buffer |
| **Linked list** | Alert history, newest alert added at the head |
| Bubble sort and selection sort | Used **only as baselines in the benchmark**, not in the live pipeline |

A stack-based evaluator for combined alert rules (for example `temperature > 80 AND vibration > 1`) is a possible extension and is not part of the core system.

## Anomaly Detection

An anomaly is a reading, or a pattern of readings, that does not match how a machine normally behaves. The system targets four types:

| Type | Meaning | Example |
|---|---|---|
| Point | A single reading far outside the normal range | Sudden temperature spike |
| Contextual | Normal in one situation, abnormal in another | Temperature fine under load, suspicious when idle |
| Collective | Each reading is normal, but the pattern over time is not | Vibration slowly rising as a bearing wears |
| Multivariate | Each signal is normal alone, but the relationship between them is broken | Vibration and temperature no longer move together |

Fixed limits reliably catch only the first type. An **LSTM Autoencoder** is trained on normal data only, so it rebuilds unusual windows poorly. The reconstruction error is the anomaly signal, and because the model looks at both signals over a whole window, it can also detect the other three types. Whether it actually does so on our data will be reported honestly in the evaluation, including the types it misses.

**Machine states:** Normal, Warning (scores rising over consecutive windows), Critical (scores stay high), Sensor Offline (Last Will message received).

## Tech Stack

| Area | Technology |
|---|---|
| Backend and data structures | C#, .NET (web application) |
| Messaging | MQTT, Eclipse Mosquitto broker |
| ML service | Python, FastAPI, TensorFlow/Keras or PyTorch |
| Dashboard | React (browser) |
| Database | Relational database for alerts, predictions, users and settings |
| Sensor simulator | Script that replays a dataset to the MQTT broker |

## Project Structure

```
ForgeGuard/
├── broker/                       # Mosquitto config and topic notes
│   └── README.md
├── csharp/                       # C# solution
│   ├── SensorMonitor.Core/       # custom data structures and algorithms
│   │   ├── Buffer/               # Circular Buffer
│   │   ├── Collections/          # incoming queue, alert queue, linked list
│   │   ├── Searching/            # recursive binary search
│   │   ├── SegmentTree/          # Segment Tree
│   │   └── Sorting/              # merge, quick, insertion, bubble, selection sort
│   ├── SensorMonitor.Ingest/     # MQTT subscriber and message validation
│   ├── SensorMonitor.Web/        # web application, API and live push to the dashboard
│   ├── SensorMonitor.Tests/      # unit tests
│   └── README.md
├── dashboard/                    # React dashboard
│   └── README.md
├── data/                         # sample data only (full datasets are not committed)
│   ├── sample/
│   └── README.md
├── docs/                         # project documents
│   ├── contracts.md              # agreed message formats and naming rules
│   └── Project_Work_Plan.docx    # work plan and task split
├── ml-service/                   # Python FastAPI service and LSTM Autoencoder
│   └── README.md
├── models3d/                     # Blender files and glTF exports
│   └── README.md
├── simulator/                    # dataset replay script (publishes to MQTT)
│   ├── requirements.txt
│   └── README.md
├── .gitignore
├── CONTRIBUTING.md
└── README.md
```

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Python 3.x](https://www.python.org/downloads/) with `pip`
- [Node.js](https://nodejs.org/) and `npm`
- [Eclipse Mosquitto](https://mosquitto.org/download/) MQTT broker
- A standard laptop is enough. No physical sensors are needed, and a GPU is optional for faster model training.

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/HaroonKhurram/ForgeGuard.git
cd ForgeGuard

# 2. Python ML service
cd ml-service
python -m venv venv
source venv/bin/activate        # on Windows: venv\Scripts\activate
pip install -r requirements.txt
cd ..

# 3. React dashboard
cd dashboard
npm install
cd ..

# 4. C# backend
cd backend
dotnet restore
cd ..
```

### Configuration

Copy the example settings file and fill in your own values. **Never commit passwords or keys.**

```
MQTT broker address, username and password
ML service URL
Database connection string
```

## Usage

Start the parts in this order (during development all of them can run on one laptop):

```bash
# 1. MQTT broker
mosquitto -c mosquitto.conf

# 2. ML service
cd ml-service && uvicorn main:app --reload

# 3. C# backend
cd backend && dotnet run

# 4. Dashboard
cd dashboard && npm start

# 5. Sensor simulator (replays dataset readings into the broker)
cd simulator && python replay.py
```

Then open the dashboard in your browser (the address is shown in the terminal) and sign in.

**Example MQTT message** *(final format to be fixed by the team)*

```
Topic:   factory/line1/motor3/vibration
Payload: { "deviceId": "motor3", "timestamp": "2026-11-01T10:15:30Z", "value": 0.42 }
```

**User roles**

| Role | Can do |
|---|---|
| Operator | View the live dashboard, query window max / min / sum, view alerts |
| Maintenance engineer | View the live dashboard, failure risk level and alert history |
| Admin | Manage users and configure thresholds |

## Testing and Benchmarking

- **Unit tests** cover the custom data structures (Circular Buffer, Segment Tree, queues, linked list) and the sorting and search algorithms. The Segment Tree is checked against a plain scan.
- **Failure test:** the ML service is switched off to confirm that ingestion, window queries and threshold alerts keep working and that the dashboard shows the service status.
- **Benchmarks:** the Segment Tree is compared with a plain scan, and all sorting algorithms are compared with each other across window sizes.
- **Performance target:** a window query result should appear on the dashboard in under 100 ms. This will be verified by benchmarking and reported with the measured values.

```bash
cd backend && dotnet test
```

**Results** *(to be added after measurement)*

| Test | Result |
|---|---|
| Segment Tree vs plain scan | TBD |
| Sorting algorithms by window size | TBD |
| Dashboard query time | TBD |
| Anomaly detection (precision, recall, confusion matrix) | TBD |

## Dataset

ForgeGuard is built and tested on **public industrial sensor datasets, replayed through the MQTT broker** in place of a real factory. The SRS names the **NASA IMS bearing dataset** (run-to-failure vibration recordings) as the dataset. Any readings produced by our own simulator are synthetic and will be labelled as such in the report.

Dataset files are large, so they are **not stored in this repository**. Download instructions go in `data/README.md`.

## Scope and Limitations

- Data comes from public datasets and a simulator, **not from a real factory**.
- The system **only monitors and warns**. It does not control machines and does not name the exact fault.
- A mobile app is outside the scope. The dashboard runs in a browser.
- The ML service is a second tier. When it is offline, only threshold alerts and window queries are available.

## Roadmap

- [x] Software Requirements Specification
- [ ] Simulator and dataset preparation
- [ ] MQTT broker setup and C# subscriber
- [ ] Circular Buffer and Segment Tree with unit tests
- [ ] Incoming queue, alert queue and linked-list alert history
- [ ] Sorting algorithms, recursive binary search and window statistics
- [ ] LSTM Autoencoder training and evaluation
- [ ] FastAPI inference service and REST client with retries
- [ ] Fault-tolerant alerting (ML service offline)
- [ ] React dashboard with live data
- [ ] Benchmarks and final report
- [ ] *(planned)* 3D view of the machines inside the dashboard, showing status and live values
- [ ] *(optional)* Stack-based evaluator for combined alert rules

## Team

| Name | Roll No. |
|---|---|
| Mahnoor Murtaza | 2025-DS-48 |
| Fatima Zafar | 2025-DS-16 |
| Amaan Mohsin | 2025-DS-33 |
| Haroon Khurram | 2025-DS-52 |

**Supervisor:** Ms. Hijab Zaidi, Institute of Data Sciences, UET Lahore

## Acknowledgements

We thank our supervisor, Ms. Hijab Zaidi, for her guidance and honest feedback, and our instructors in Data Structures & Algorithms, Computer Networks, Data Science and Software Engineering, whose lessons made a project of this kind possible.

## References

1. Krupitzer, C., et al. (2020). *A Survey on Predictive Maintenance for Industry 4.0.* arXiv.
2. Chalapathy, R., & Chawla, S. (2019). *Deep Learning for Anomaly Detection: A Survey.* arXiv:1901.03407.
3. Malhotra, P., et al. (2016). *LSTM-based Encoder-Decoder for Multi-sensor Anomaly Detection.* arXiv:1607.00148.
4. Tangwongsan, K., Hirzel, M., Schneider, S., & Wu, K.-L. (2015). *General Incremental Sliding-Window Aggregation.* Proceedings of the VLDB Endowment, 702-713.
5. Lee, J., Qiu, H., Yu, G., & Lin, J. (2007). *Bearing Data Set.* NASA Prognostics Data Repository.
6. Naik, N. (2017). *Choice of Effective Messaging Protocols for IoT Systems: MQTT, CoAP, AMQP and HTTP.* IEEE ISSE.
7. HiveMQ. *MQTT Essentials Guide.*

The full reference list is in the SRS (`docs/`).

## License

This project was created for academic purposes at UET Lahore. All rights reserved by the authors unless a licence file is added to this repository.

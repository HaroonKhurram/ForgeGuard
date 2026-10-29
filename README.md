# Real-time Industrial IoT Sensor Monitor

Real-time industrial IoT sensor monitor. MQTT ingestion, sliding-window queries, ML-based anomaly detection, live alerts and a 3D machine view. C# · FastAPI · React · MQTT · Three.js · PostgreSQL

> University semester project (DSA, IDS, CN, SE). Group of 4, split into Pair A and Pair B.

## About

Older machinery often fails without warning because wear and tear is not analysed until a breakdown happens. This system receives vibration and temperature readings from machines, answers fast questions such as "what was the maximum temperature in the last 5 minutes?", and flags unusual behaviour using a threshold check and a machine-learning detector.

**Data:** we have no access to a real factory. Readings come from our own simulator and from public datasets (the free CIRA pump dataset), replayed through MQTT. The 3D view only *displays* machine status and values that already arrive through MQTT. It is not a data source.

## How it works

```
Simulator / dataset replay
        |  MQTT (TCP)
        v
C# ingest -> validation -> message queue -> circular buffer + Segment Tree -> web dashboard (React + 3D)
                                                   |
                                                   +-- REST (timeout + circuit breaker) --> Python ML service
                                                                                             (LSTM Autoencoder)
If the ML service is slow or offline, the C# app falls back to simple threshold alerts.
```

## Tech stack

| Part | Technology | Status |
|---|---|---|
| Backend and data structures | C# (.NET) | Decided |
| ML service | Python, FastAPI | Suggested |
| Dashboard | React | Decided |
| 3D view | Blender, glTF, Three.js / React Three Fiber | Decided |
| Messaging | MQTT broker (Mosquitto) | Suggested |
| Database | PostgreSQL | Suggested, group to confirm |

## Repository structure

| Folder | What goes here |
|---|---|
| `docs/` | Work plan, interface contracts, SRS, diagrams, test reports |
| `simulator/` | Python sensor simulator and fault injection |
| `broker/` | MQTT broker configuration and setup notes |
| `csharp/SensorMonitor.Core/` | Circular buffer, Segment Tree, queue, sorting, searching, linked list |
| `csharp/SensorMonitor.Ingest/` | MQTT subscriber, validation, custom TCP protocol |
| `csharp/SensorMonitor.Web/` | Web API, alerts, ML client, circuit breaker |
| `csharp/SensorMonitor.Tests/` | Unit tests |
| `ml-service/` | Python anomaly detection notebooks, models and inference service |
| `dashboard/` | React dashboard and 3D viewer |
| `models3d/` | Blender source files and exported glTF models |
| `data/` | Small sample files only. Large datasets are not committed |

## Milestones

| Milestone | Goal | Status |
|---|---|---|
| First 20% | Simulator and dataset inspection, broker running, C# subscriber, circular buffer, core Segment Tree | In progress |
| 50% | To be filled in after the 20% review | Not started |
| 100% | Full system, benchmarks, tests, final report | Not started |

The full task list, weights and pair split are in [`docs/Project_Work_Plan.docx`](docs/Project_Work_Plan.docx).

## Team

| Pair | Share | Focus |
|---|---|---|
| Pair A | 56% | Standard, easier tasks: simulator, data, broker, sorting, database, dashboard, most 3D modelling |
| Pair B | 44% | Harder tasks: Segment Tree, LSTM Autoencoder, custom TCP protocol, circuit breaker, integration |

Add member names and GitHub usernames here.

## Getting started

Each folder has its own `README.md` with setup steps. Start with [`docs/contracts.md`](docs/contracts.md), because every part depends on the agreed message format.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Limitations

- Data is simulated or from public datasets, not from a real factory.
- Public datasets differ from cheap IoT sensors (sampling rate and sensor type), so results are a demonstration, not a claim about any real plant.
- Credit the dataset authors in the final report.

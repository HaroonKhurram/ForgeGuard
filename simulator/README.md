# Simulator (Python)

Generates vibration and temperature readings for pumps: normal values, noise and
slow drift (task I1a). Fault injection (I1b) and MQTT publishing (N2) come later.

Output follows `docs/contracts.md`.

## Setup (Windows PowerShell)

```
python -m venv venv
venv\Scripts\activate
pip install -r simulator\requirements.txt
```

## Run

```
cd simulator
python simulator.py --hours 6 --interval 1 --plot
```

Options:

| Option | Default | Meaning |
|---|---|---|
| `--machines` | `pump1 pump2 pump3` | device ids to simulate |
| `--hours` | `6` | length of the simulation |
| `--interval` | `1` | seconds between readings |
| `--start` | `2026-11-01T00:00:00` | first timestamp (UTC) |
| `--seed` | `42` | random seed, same seed gives the same data |
| `--out` | `output` | output folder (not committed to git) |
| `--plot` | off | also save `simulation_plot.png` |

## Output files (in `output/`)

- `simulated_readings.csv`: columns `device_id, time, vibration, temp, label`.
  `label` is always `0` (normal) until I1b adds faults. `time` is UTC ISO 8601.
- `simulated_messages.jsonl`: one MQTT-style message per reading, per contract:

```json
{"topic": "factory/line1/pump1/telemetry", "payload": {"device_id": "pump1", "time": "2026-11-01T00:00:00Z", "vibration": 0.3798, "temp": 53.4627}}
```

- `simulation_plot.png`: noisy readings with the underlying trend (baseline,
  slow drift and a 30-minute load cycle).

## Tests

```
cd simulator
python -m pytest -q
```

## Notes

- The baseline and noise numbers in `simulator.py` are placeholders for a healthy
  machine. The vibration unit is still to be confirmed (see `docs/contracts.md`);
  the CIRA dataset records pump vibration in m/s².
- Real data (CIRA pump dataset) is inspected separately in task I2. This
  simulator produces synthetic data only.
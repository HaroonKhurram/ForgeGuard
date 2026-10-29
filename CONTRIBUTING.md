# Contributing

## Branches
- `main` always works. Do not push to it directly.
- Work on your own branch named after the task ID from the work plan: `N1-broker-setup`, `D1-circular-buffer`, `I1a-simulator`.
- Open a pull request into `main`. Another member must review and approve it before merging.

## Commits
Start the message with the task ID: `D1: add circular buffer with overwrite on full`.
Small, frequent commits are better than one large commit.

## Do not commit
- Passwords, tokens, the Mosquitto password file, `.env` files
- Large datasets (use `data/sample/` for small sample files only)
- Build output (`bin/`, `obj/`, `node_modules/`, `__pycache__/`)

## Folder ownership (to avoid merge conflicts)
| Folder | Mainly worked on by |
|---|---|
| `simulator/`, `broker/`, `data/` | Pair A |
| `csharp/SensorMonitor.Core/Buffer`, `SegmentTree` | Pair B |
| `csharp/SensorMonitor.Core/Collections`, `Sorting`, `Searching` | Pair A |
| `csharp/SensorMonitor.Ingest/` | Pair B |
| `csharp/SensorMonitor.Web/` | Shared (agree before editing the same file) |
| `ml-service/` | Pair A (service), Pair B (LSTM, features, evaluation) |
| `dashboard/`, `models3d/` | Pair A (viewer, modelling), Pair B (live data binding) |

If you must change a file in someone else's folder, tell them first or open a pull request and tag them.

## Interface contracts
Message format, REST format and public method names are in `docs/contracts.md`. Change them only through a pull request that both pairs approve.

# Data

Do **not** commit large datasets. Put them in this folder locally; they are ignored by Git.
Commit only small sample files (a few hundred rows) in `data/sample/`.

## Sources
- **CIRA centrifugal pump dataset** (Zenodo, free): https://zenodo.org/records/18479728
  Check the dataset description for columns, units and whether it contains faults. Cite the authors in the report.
- **Our simulator** (`simulator/`): produces vibration and temperature with noise, drift and injected faults.

Write here, after inspecting the data: the columns used, units, time range, and whether faults are present.

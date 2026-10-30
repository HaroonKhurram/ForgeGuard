# Data

Do **not** commit large datasets. Put them in this folder locally; they are ignored by Git.
Commit only small sample files (a few hundred rows) in `data/sample/`.

## Sources

- **CIRA centrifugal pump dataset** (Zenodo, free): https://zenodo.org/records/18479728
  Please cite the dataset and its authors (named on the Zenodo page) in the report.
- **Our simulator** (`simulator/`): produces synthetic vibration and temperature
  with noise and slow drift (task I1a). Fault injection comes in I1b.

## CIRA dataset

Operational data of centrifugal pumps that supply demineralised water to the
boilers of a heating plant at CIRA (Italian Aerospace Research Centre).

- 8 CSV files for pumps `A`, `B` and `C` on three operational days.
  File names are `<pump>_<date>.csv`, for example `A_2024-04-10.csv`.
- We downloaded the three files of 2024-04-10 into `data/raw/` (not committed).
- Each file has 11 columns: `Timestamp` and 10 measurements.

### Columns (as written in the dataset description)

`X` is the pump letter (`A`, `B` or `C`).

| Column | Meaning | Unit |
|---|---|---|
| `Timestamp` | time of the measurement | - |
| `X_ACR_Mot.PV` | motor accelerometer, "displacement" | m/s |
| `X_ACR_Mot.SV` | motor accelerometer, peak value | m/s² |
| `X_ACR_Mot.TV` | motor accelerometer, contact temperature | °C |
| `X_ACR_Pmp.PV` | pump accelerometer, "displacement" | m/s |
| `X_ACR_Pmp.SV` | pump accelerometer, peak value | m/s² |
| `X_ACR_Pmp.TV` | pump accelerometer, contact temperature | °C |
| `X_Temp.SV` | motor casing temperature | °C |
| `X_Pres.SV` | pump outlet pressure | bar |
| `Barometer` | atmospheric pressure | mbar |
| `Temperature` | ambient temperature | °C |

**Differences between description and files:** in the real files the last two pump
columns are named `X_Temp.PV` and `X_Pres.PV` (not `.SV`). The description also
calls the `.PV` accelerometer columns "displacement" but gives the unit m/s, which
does not match. We therefore rely on the `.SV` (peak value) columns for vibration.

## What we found (`inspect_cira.py`, 2024-04-10, pumps A, B, C)

- Each file has 4981 rows. Times run from 12:00:00 to 13:59:59 (the file has no time
  zone; the script treats the times as UTC).
- One row per file has no timestamp. The script drops it.
- The sampling interval is not constant: the median gap is 1 s, the largest gap is
  5 s, and the first rows appear to be 5 s apart.
- **No fault or label column exists**, so there are no labelled anomalies.
- Missing values: between 2 and 171 per column, per file (most in the temperature
  columns, for example `B_Temp.PV`).
- `Barometer` and `Temperature` are identical in all three files, so they are plant
  sensors shared by all pumps and not pump-specific.
- Big spikes: the median pump vibration (`Pmp.SV`) is about 0.4 to 0.5, but the
  maximum is 291.6 for pump A, 37.7 for pump C and 26.2 for pump B.
- In pump A the pump seems to run from about 12:28 to 12:50 (vibration about 20),
  and sits idle at about 0.5 before and after. The big spikes lie at the start and
  end of that run. This is our reading of the plot, not stated by the authors.
  So the data is not one long "normal running" stretch.

Plots are produced locally in `data/plots/`.

## Columns we plan to use (to be agreed in the group)

- vibration: `X_ACR_Pmp.SV` (m/s²)
- temperature: `X_Temp.PV` (motor casing, assumed to be the same sensor as `X_Temp.SV`)

## Run the inspection

```
cd data
python inspect_cira.py raw\A_2024-04-10.csv
```

## Real data versus simulator

CIRA data is real. The simulator data is synthetic. The simulator baselines
(vibration about 0.4, temperature about 55 °C) are placeholders and are not yet
matched to the CIRA ranges (idle vibration about 0.5 m/s², temperature 18 to 31 °C).
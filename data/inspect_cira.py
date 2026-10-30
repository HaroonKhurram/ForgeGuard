"""
I2 - Inspect the CIRA centrifugal pump dataset (Zenodo record 18479728).

Download the CSV files yourself into  data/raw/  (that folder is NOT committed).
Then run:
    python inspect_cira.py raw/A_2024-04-10.csv

It prints: shape, columns, dtypes, time range, sampling interval, missing values,
basic statistics, and saves plots into data/plots/.
It does not assume exact column names: it looks for a time column and plots the
vibration / temperature columns it can find, otherwise the first numeric columns.
"""
import sys
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd

# Preferred columns (names from the Zenodo description). Used only if present.
PREFERRED_SUFFIXES = ["ACR_Pmp.SV", "ACR_Pmp.TV", "Temp.PV", "ACR_Mot.SV", "Pres.PV"]


def find_time_column(df):
    for c in df.columns:
        if any(k in c.lower() for k in ("time", "date", "stamp")):
            parsed = pd.to_datetime(df[c], errors="coerce", utc=True)
            if parsed.notna().mean() > 0.9:
                return c, parsed
    return None, None


def main(path):
    path = Path(path)
    df = pd.read_csv(path)
    print(f"\n=== {path.name} ===")
    print("Shape (rows, columns):", df.shape)
    print("\nColumns and types:\n", df.dtypes.to_string())

    tcol, ts = find_time_column(df)
    if tcol:
        print("Rows with missing/invalid time (dropped):", int(ts.isna().sum()))
        df = df.assign(_t=ts).dropna(subset=["_t"]).sort_values("_t")
        gaps = df["_t"].diff().dropna()
        print(f"\nTime column: {tcol}")
        print("From", df["_t"].iloc[0], "to", df["_t"].iloc[-1])
        print("Typical sampling interval:", gaps.median(), "| largest gap:", gaps.max())
    else:
        print("\nNo time column found - check the CSV header manually.")

    num = df.select_dtypes("number")
    print("\nMissing values per column:\n", df.drop(columns="_t", errors="ignore").isna().sum().to_string())
    print("\nStatistics:\n", num.describe().T.round(4).to_string())

    # Is a fault / label column present?
    label_like = [c for c in df.columns if any(k in c.lower() for k in ("fault", "label", "anomal", "status"))]
    print("\nFault/label columns:", label_like if label_like else
          "NONE found -> the data has no fault labels (write this in the report)")

    # Plots
    cols = [c for s in PREFERRED_SUFFIXES for c in num.columns if c.endswith(s)] or list(num.columns[:4])
    cols = cols[:4]
    out = Path(__file__).parent / "plots"
    out.mkdir(exist_ok=True)
    x = df["_t"] if tcol else df.index
    fig, axes = plt.subplots(len(cols), 1, figsize=(11, 2.6 * len(cols)), sharex=True)
    axes = [axes] if len(cols) == 1 else axes
    for ax, c in zip(axes, cols):
        ax.plot(x, df[c], lw=0.6)
        ax.set_ylabel(c)
        ax.grid(alpha=0.3)
    axes[0].set_title(f"CIRA pump data: {path.name}")
    fig.tight_layout()
    fig.savefig(out / f"{path.stem}_overview.png", dpi=150)
    plt.close(fig)

    df[cols].hist(bins=50, figsize=(10, 6))
    plt.tight_layout()
    plt.savefig(out / f"{path.stem}_histograms.png", dpi=150)
    print(f"\nPlots saved in {out}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit("Usage: python inspect_cira.py raw/<file>.csv")
    main(sys.argv[1])

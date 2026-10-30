"""
I1a - Basic sensor simulator for ForgeGuard.

Generates NORMAL machine behaviour for two signals (vibration, temperature):
    value(t) = baseline + slow drift + small daily load cycle + Gaussian noise

No faults here. Fault injection (spikes, gradual failure, missing readings)
is task I1b and will be added later.

Outputs (in --out folder):
    simulated_readings.csv    one row per timestamp per machine (wide format)
    simulated_messages.jsonl  one MQTT-style message per line (topic + payload)
    simulation_plot.png       plot for the teacher (use --plot)

Run:
    python simulator.py --hours 6 --interval 1 --plot
"""
import argparse
import json
from pathlib import Path

import numpy as np
import pandas as pd

# Signal settings. Units: vibration in mm/s, temperature in degC.
# These numbers are assumptions chosen to look like a healthy motor.
SIGNALS = {
    "vibration":   dict(baseline=0.40, noise_std=0.03, drift_per_hour=0.005, cycle_amp=0.02),
    "temperature": dict(baseline=55.0, noise_std=0.40, drift_per_hour=0.15,  cycle_amp=1.00),
}


def generate_signal(n, interval_s, baseline, noise_std, drift_per_hour, cycle_amp, rng):
    """Return (clean, drift, noisy) arrays of length n."""
    t_s = np.arange(n) * interval_s
    drift = drift_per_hour * (t_s / 3600.0)                              # slow linear drift
    cycle = cycle_amp * np.sin(2 * np.pi * t_s / (30 * 60))               # 30-min load cycle
    clean = baseline + drift + cycle
    noisy = clean + rng.normal(0.0, noise_std, size=n)                    # sensor noise
    return clean, drift, noisy


def simulate(machines, hours, interval_s, start, seed):
    rng = np.random.default_rng(seed)
    n = int(hours * 3600 / interval_s)
    ts = pd.date_range(start=start, periods=n, freq=f"{interval_s}s", tz="UTC")
    frames = []
    for m in machines:
        df = pd.DataFrame({"timestamp": ts, "deviceId": m})
        for name, cfg in SIGNALS.items():
            # small per-machine offset so machines are not identical
            cfg = dict(cfg, baseline=cfg["baseline"] * rng.uniform(0.95, 1.05))
            clean, _, noisy = generate_signal(n, interval_s, rng=rng, **cfg)
            df[name] = noisy.round(4)
            df[f"{name}_clean"] = clean.round(4)   # kept only for plotting
        df["label"] = 0                            # 0 = normal (faults come in I1b)
        frames.append(df)
    return pd.concat(frames, ignore_index=True)


def write_jsonl(df, path, line="line1"):
    """One message per signal reading, in the format from the README example."""
    with open(path, "w", encoding="utf-8") as f:
        for row in df.itertuples(index=False):
            iso = row.timestamp.strftime("%Y-%m-%dT%H:%M:%SZ")
            for name in SIGNALS:
                msg = {
                    "topic": f"factory/{line}/{row.deviceId}/{name}",
                    "payload": {"deviceId": row.deviceId, "timestamp": iso,
                                "value": float(getattr(row, name))},
                }
                f.write(json.dumps(msg) + "\n")


def make_plot(df, path):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    m = df["deviceId"].iloc[0]
    d = df[df["deviceId"] == m]
    fig, axes = plt.subplots(2, 1, figsize=(11, 7), sharex=True)
    units = {"vibration": "mm/s", "temperature": "degC"}
    for ax, name in zip(axes, SIGNALS):
        ax.plot(d["timestamp"], d[name], lw=0.6, alpha=0.7, label="noisy reading")
        ax.plot(d["timestamp"], d[f"{name}_clean"], lw=2, color="red",
                label="underlying trend (baseline + drift + load cycle)")
        ax.set_ylabel(f"{name} ({units[name]})")
        ax.legend(loc="upper left")
        ax.grid(alpha=0.3)
    axes[0].set_title(f"Simulated normal behaviour of {m}: noise and slow drift")
    axes[1].set_xlabel("time (UTC)")
    fig.tight_layout()
    fig.savefig(path, dpi=150)
    plt.close(fig)


def main():
    p = argparse.ArgumentParser(description="ForgeGuard basic sensor simulator (I1a)")
    p.add_argument("--machines", nargs="+", default=["motor1", "motor2", "motor3"])
    p.add_argument("--hours", type=float, default=6)
    p.add_argument("--interval", type=int, default=1, help="seconds between readings")
    p.add_argument("--start", default="2026-11-01T00:00:00")
    p.add_argument("--seed", type=int, default=42)
    p.add_argument("--out", default="output")
    p.add_argument("--plot", action="store_true")
    a = p.parse_args()

    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    df = simulate(a.machines, a.hours, a.interval, a.start, a.seed)

    cols = ["timestamp", "deviceId", "vibration", "temperature", "label"]
    df[cols].to_csv(out / "simulated_readings.csv", index=False)
    write_jsonl(df, out / "simulated_messages.jsonl")
    if a.plot:
        make_plot(df, out / "simulation_plot.png")
    print(f"{len(df)} rows for {len(a.machines)} machines -> {out.resolve()}")


if __name__ == "__main__":
    main()

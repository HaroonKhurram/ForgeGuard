import numpy as np

from simulator import generate_signal

# Same kind of settings the simulator uses for temperature.
CFG = dict(baseline=55.0, noise_std=0.4, drift_per_hour=0.15, cycle_amp=1.0)


def make(n=3600, interval_s=1, seed=1, **overrides):
    cfg = dict(CFG, **overrides)
    return generate_signal(n, interval_s, rng=np.random.default_rng(seed), **cfg)


def test_returns_three_arrays_of_requested_length():
    clean, drift, noisy = make(n=500)
    assert len(clean) == len(drift) == len(noisy) == 500


def test_same_seed_gives_same_data():
    _, _, a = make(seed=7)
    _, _, b = make(seed=7)
    assert np.array_equal(a, b)


def test_different_seed_gives_different_data():
    _, _, a = make(seed=1)
    _, _, b = make(seed=2)
    assert not np.array_equal(a, b)


def test_drift_starts_at_zero_and_only_goes_up():
    _, drift, _ = make(n=7200)
    assert drift[0] == 0
    assert np.all(np.diff(drift) >= 0)
    # 7200 s = 2 h at 0.15 per hour -> about 0.3 (last sample is 7199 s)
    assert abs(drift[-1] - 0.15 * 7199 / 3600) < 1e-9


def test_noise_size_matches_setting():
    clean, _, noisy = make(n=50000)
    assert abs(np.std(noisy - clean) - CFG["noise_std"]) < 0.02


def test_zero_noise_gives_clean_signal():
    clean, _, noisy = make(noise_std=0.0)
    assert np.allclose(clean, noisy)
    # ---- simulate(), CSV and JSONL against docs/contracts.md -------------------
import json
import re
import sys
from datetime import datetime

import pandas as pd

import simulator as sim

MACHINES = ["pump1", "pump2", "pump3"]
TIME_FORMAT = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")


def small_df():
    # 0.01 h at 1 s interval = 36 readings per machine
    return sim.simulate(MACHINES, hours=0.01, interval_s=1,
                        start="2026-11-01T00:00:00", seed=1)


def test_simulate_has_contract_columns_and_label_zero():
    df = small_df()
    for col in ["device_id", "time", "vibration", "temp", "label"]:
        assert col in df.columns
    assert (df["label"] == 0).all()


def test_simulate_row_count_and_machine_ids():
    df = small_df()
    assert len(df) == 36 * 3
    assert set(df["device_id"]) == set(MACHINES)
    assert (df.groupby("device_id").size() == 36).all()


def test_simulate_time_is_utc_and_one_second_apart():
    d = small_df()
    d = d[d["device_id"] == "pump1"]
    assert str(d["time"].dt.tz) == "UTC"
    assert d["time"].iloc[0] == pd.Timestamp("2026-11-01T00:00:00", tz="UTC")
    assert (d["time"].diff().dropna() == pd.Timedelta(seconds=1)).all()


def test_jsonl_follows_contract(tmp_path):
    df = small_df()
    path = tmp_path / "messages.jsonl"
    sim.write_jsonl(df, path)
    lines = path.read_text(encoding="utf-8").splitlines()
    assert len(lines) == len(df)          # one message per reading
    for line in lines:
        msg = json.loads(line)
        payload = msg["payload"]
        assert set(payload) == {"device_id", "time", "vibration", "temp"}
        assert msg["topic"] == f"factory/line1/{payload['device_id']}/telemetry"
        assert TIME_FORMAT.match(payload["time"])
        datetime.strptime(payload["time"], "%Y-%m-%dT%H:%M:%SZ")
        assert isinstance(payload["vibration"], float)
        assert isinstance(payload["temp"], float)


def test_cli_writes_csv_with_contract_columns(tmp_path, monkeypatch):
    monkeypatch.setattr(sys, "argv", ["simulator.py", "--hours", "0.01",
                                      "--out", str(tmp_path)])
    sim.main()
    csv = pd.read_csv(tmp_path / "simulated_readings.csv")
    assert list(csv.columns) == ["device_id", "time", "vibration", "temp", "label"]
    assert set(csv["device_id"]) == set(MACHINES)
    assert csv["time"].map(lambda s: bool(TIME_FORMAT.match(s))).all()
    assert (tmp_path / "simulated_messages.jsonl").exists()
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
"""
ml_service.py  —  FastAPI ML service for MKR IoT Carrier sensor data
=====================================================================
Port  : 8000
Sensors: HTS221 (temp/humidity), LPS22HB (pressure), APDS-9960 (light)

Feature vector (7 features extracted from rolling sensor arrays):
  [current_temp, temp_trend, temp_mean, temp_std,
   current_humidity, current_pressure, current_light]

Labels:
  0 → VeryCold  (temp ≤ -5 °C)
  1 → Cold      (-5 °C < temp < 10 °C)
  2 → Normal    (10 °C ≤ temp < 25 °C)
  3 → Hot       (temp ≥ 25 °C)

─── Running as a service ────────────────────────────────────────────
  uvicorn ml_service:app --port 8000

─── Generating real training data from sensors.db ───────────────────
  python ml_service.py --generate-data
  python ml_service.py --generate-data --db "C:/Users/You/Documents/PansIoTassignment/sensors.db"
  python ml_service.py --generate-data --min-rows 200

  After generating data.txt, restart the service to retrain on real data.
"""
#python -m uvicorn app:app --port 8000 to run
import argparse
import sqlite3
import sys
from pathlib import Path
from typing import List

import numpy as np
from fastapi import FastAPI
from pydantic import BaseModel
from sklearn.metrics import accuracy_score
from sklearn.model_selection import train_test_split

# ── Constants ────────────────────────────────────────────────────────────────

N_FEATURES  = 7
N_CLASSES   = 4
LABEL_MAP   = {0: "VeryCold", 1: "Cold", 2: "Normal", 3: "Hot"}
DATA_PATH   = Path("data.txt")

DEFAULT_DB  = (
    Path.home() / "Documents" / "PansIoTassignment" / "sensors.db"
)

# Realistic sensor ranges for synthetic training data
# Temperature : HTS221     → -40 to 120 °C  (practical indoor -10..50)
# Humidity    : HTS221     → 0 to 100 %RH
# Pressure    : LPS22HB    → 260 to 1260 hPa (typical 950..1060)
# Light       : APDS-9960  → 0 to 4096 (raw clear channel)
TEMP_CLASS_RANGES = [(-20, -5), (-5, 10), (10, 25), (25, 50)]
HUM_RANGE         = (20.0, 100.0)
PRESS_RANGE       = (950.0, 1060.0)
LIGHT_RANGE       = (0.0,   4096.0)

# ═══════════════════════════════════════════════════════════════════════════
#  SECTION 1 — NEURAL NETWORK
# ═══════════════════════════════════════════════════════════════════════════

def sigmoid(x: np.ndarray) -> np.ndarray:
    """Sigmoid activation — clipped to prevent overflow on extreme inputs."""
    return 1.0 / (1.0 + np.exp(-np.clip(x, -500, 500)))

def sigmoid_derivative(x: np.ndarray) -> np.ndarray:
    return x * (1.0 - x)

def train_model(data: np.ndarray):
    """
    Train a 2-hidden-layer sigmoid network.
    Architecture: 7 → 32 → 16 → 4
    Returns: weights, biases, feature mean, feature std
    """
    X = data[:, :-1]
    y = data[:,  -1]

    # Normalise features
    mean = np.mean(X, axis=0)
    std  = np.std(X,  axis=0)
    std[std == 0] = 1.0                     # avoid divide-by-zero
    X_norm = (X - mean) / std

    # One-hot encode labels
    Y = np.eye(N_CLASSES)[y.astype(int)]

    # Stratified train / test split
    X_train, X_test, y_train, y_test = train_test_split(
        X_norm, Y, test_size=0.2, random_state=42, stratify=y
    )

    # Build weight and bias tensors: 7 → 32 → 16 → 4
    all_units = [N_FEATURES, 32, 16, N_CLASSES]
    rng       = np.random.default_rng(0)
    weights   = [
        rng.standard_normal((all_units[i], all_units[i + 1])) * 0.1
        for i in range(len(all_units) - 1)
    ]
    biases = [
        np.zeros((1, all_units[i + 1]))
        for i in range(len(all_units) - 1)
    ]

    learning_rate = 0.05

    for _ in range(10000):
        # Forward pass
        activations = [X_train]
        for i in range(len(weights)):
            activations.append(
                sigmoid(activations[i].dot(weights[i]) + biases[i])
            )

        # Backward pass
        error  = y_train - activations[-1]
        deltas = [error * sigmoid_derivative(activations[-1])]
        for i in range(len(weights) - 2, -1, -1):
            delta = (
                deltas[-1].dot(weights[i + 1].T)
                * sigmoid_derivative(activations[i + 1])
            )
            deltas.append(delta)
        deltas.reverse()

        # Weight / bias update
        for i in range(len(weights)):
            weights[i] += activations[i].T.dot(deltas[i]) * learning_rate
            biases[i]  += (
                np.sum(deltas[i], axis=0, keepdims=True) * learning_rate
            )

    # Evaluate on held-out test set
    act = [X_test]
    for i in range(len(weights)):
        act.append(sigmoid(act[i].dot(weights[i]) + biases[i]))
    preds       = np.argmax(act[-1], axis=1)
    true_labels = np.argmax(y_test,  axis=1)
    acc = accuracy_score(true_labels, preds)
    print(f"Training complete — Test Accuracy: {acc:.2%}")

    return weights, biases, mean, std

def predict_nn(
    features: np.ndarray,
    weights,
    biases,
    mean: np.ndarray,
    std:  np.ndarray,
) -> int:
    """Normalise a feature row and run a forward pass. Returns class index."""
    std_safe = np.where(std == 0, 1.0, std)
    x = (features - mean) / std_safe
    for i in range(len(weights)):
        x = sigmoid(x.dot(weights[i]) + biases[i])
    return int(np.argmax(x, axis=1)[0])

# ═══════════════════════════════════════════════════════════════════════════
#  SECTION 2 — TRAINING DATA
# ═══════════════════════════════════════════════════════════════════════════

def temperature_label(temp: float) -> int:
    """Map a temperature reading to a class index."""
    if temp <= -5:
        return 0    # VeryCold
    elif temp < 10:
        return 1    # Cold
    elif temp < 25:
        return 2    # Normal
    else:
        return 3    # Hot

def generate_synthetic_data(n_per_class: int = 500) -> np.ndarray:
    """
    Generate synthetic training data using realistic sensor ranges.
    Used automatically when data.txt does not exist yet.
    Row format: [current_temp, trend, mean, std, humidity, pressure, light, label]
    """
    rng    = np.random.default_rng(42)
    blocks = []

    for label, (t_min, t_max) in enumerate(TEMP_CLASS_RANGES):
        n      = n_per_class
        temps  = rng.uniform(t_min,       t_max,       n)
        trends = rng.uniform(-1.5,        1.5,         n)
        means  = np.clip(
                     temps + rng.uniform(-3, 3, n),
                     t_min - 5, t_max + 5
                 )
        stds   = rng.uniform(0.0,         3.0,         n)
        hums   = rng.uniform(*HUM_RANGE,               n)
        press  = rng.uniform(*PRESS_RANGE,             n)
        light  = rng.uniform(*LIGHT_RANGE,             n)
        labels = np.full(n, label, dtype=float)

        blocks.append(
            np.column_stack([temps, trends, means, stds, hums, press, light, labels])
        )

    data = np.vstack(blocks)
    rng.shuffle(data)
    return data

def load_training_data() -> np.ndarray:
    """Load data.txt if it exists, otherwise fall back to synthetic data."""
    if DATA_PATH.exists():
        print(f"Loading real training data from {DATA_PATH}")
        return np.loadtxt(DATA_PATH)
    else:
        print("data.txt not found — using synthetic training data.")
        print(
            "Run:  python ml_service.py --generate-data\n"
            "to build data.txt from your real sensor readings in sensors.db."
        )
        return generate_synthetic_data(n_per_class=500)

def generate_data_from_db(db_path: Path, out_path: Path, min_rows: int) -> None:
    """
    Read sensors.db, join all four sensor tables, build rolling features,
    and write data.txt.  Run this once you have enough real readings.
    """
    if not db_path.exists():
        print(f"ERROR: sensors.db not found at {db_path}")
        sys.exit(1)

    print(f"Connecting to {db_path} ...")
    con = sqlite3.connect(db_path)
    cur = con.cursor()

    # Join all four tables on SensorId + 6-second timestamp tolerance.
    # Only includes rows where all four sensors report valid readings.
    query = """
        SELECT
            t.Temp           AS current_temp,
            h.Hum            AS humidity,
            p.Value          AS pressure,
            l.CurrentLight   AS light,
            t.LastUpdated    AS ts
        FROM TemperatureSensor  t
        JOIN HumiditySensor     h
            ON  t.SensorId = h.SensorId
            AND ABS(strftime('%s', t.LastUpdated) - strftime('%s', h.LastUpdated)) <= 6
        JOIN PressureSensorData p
            ON  t.SensorId = p.SensorId
            AND ABS(strftime('%s', t.LastUpdated) - strftime('%s', p.Timestamp)) <= 6
        JOIN LightSensor        l
            ON  t.SensorId = l.SensorId
            AND ABS(strftime('%s', t.LastUpdated) - strftime('%s', l.LastUpdated)) <= 6
        WHERE t.IsValid = 1
          AND h.IsValid = 1
          AND p.IsValid = 1
          AND l.IsValid = 1
        ORDER BY t.LastUpdated ASC
    """

    try:
        cur.execute(query)
        rows = cur.fetchall()
    except sqlite3.OperationalError as e:
        print(f"SQL ERROR: {e}")
        print("Check that column names match your actual schema.")
        con.close()
        sys.exit(1)

    con.close()

    if len(rows) < min_rows:
        print(
            f"Only {len(rows)} joined rows found (minimum required: {min_rows}). "
            "Collect more sensor readings, then run this again."
        )
        sys.exit(1)

    print(f"Found {len(rows)} valid rows. Building feature vectors ...")

    all_temps = [r[0] for r in rows]
    records   = []

    for i, (current_temp, humidity, pressure, light, _) in enumerate(rows):
        # Rolling window of up to 5 readings for trend / mean / std
        window     = all_temps[max(0, i - 4) : i + 1]
        temp_trend = window[-1] - window[-2] if len(window) >= 2 else 0.0
        temp_mean  = float(np.mean(window))
        temp_std   = float(np.std(window)) if len(window) > 1 else 0.0
        label      = temperature_label(current_temp)

        records.append([
            current_temp,
            temp_trend,
            temp_mean,
            temp_std,
            humidity,
            pressure,
            light,
            label,
        ])

    data = np.array(records, dtype=float)
    np.savetxt(out_path, data, fmt="%.4f")
    print(f"Saved {len(data)} rows → {out_path}")
    print("Restart the FastAPI service to retrain on real data.")

# ═══════════════════════════════════════════════════════════════════════════
#  SECTION 3 — FEATURE EXTRACTION (used by /predict endpoint)
# ═══════════════════════════════════════════════════════════════════════════

def extract_features(
    temperatures: List[float],
    humidities:   List[float],
    pressures:    List[float],
    light_levels: List[float],
) -> np.ndarray:
    """
    Build the 7-element feature vector from the rolling sensor arrays
    sent by the C# DashboardApp FeatureBuilder.
    Falls back gracefully if optional arrays are empty.
    """
    temps = np.array(temperatures, dtype=float)
    hums  = np.array(humidities,   dtype=float) if humidities   else np.array([50.0])
    press = np.array(pressures,    dtype=float) if pressures    else np.array([1013.0])
    light = np.array(light_levels, dtype=float) if light_levels else np.array([0.0])

    current_temp     = float(temps[-1])
    temp_trend       = float(temps[-1] - temps[-2]) if len(temps) >= 2 else 0.0
    temp_mean        = float(np.mean(temps))
    temp_std         = float(np.std(temps)) if len(temps) > 1 else 0.0
    current_humidity = float(hums[-1])
    current_pressure = float(press[-1])
    current_light    = float(light[-1])

    return np.array([[
        current_temp,
        temp_trend,
        temp_mean,
        temp_std,
        current_humidity,
        current_pressure,
        current_light,
    ]], dtype=float)

# ═══════════════════════════════════════════════════════════════════════════
#  SECTION 4 — FASTAPI APP
# ═══════════════════════════════════════════════════════════════════════════

app = FastAPI()

# Train on startup
try:
    _data = load_training_data()
    nn_weights, nn_biases, nn_mean, nn_std = train_model(_data)
    NN_READY    = True
    DATA_SOURCE = "real" if DATA_PATH.exists() else "synthetic"
except Exception as e:
    print(f"WARNING: model training failed ({e}). Falling back to heuristic.")
    NN_READY    = False
    DATA_SOURCE = "none"

# ── Pydantic schemas — must match C# PredictRequest / PredictResponse ───────

class PredictRequest(BaseModel):
    sensor_id:    str
    timestamps:   List[str]          # ISO-8601 strings from C# DashboardApp
    temperatures: List[float]
    humidities:   List[float] = []
    pressures:    List[float] = []
    light_levels: List[float] = []

class PredictResponse(BaseModel):
    label:              str
    score:              float
    predicted_temp_10m: float
    explanation:        str

# ── Routes ───────────────────────────────────────────────────────────────────

@app.get("/health")
def health():
    return {
        "status":      "online",
        "nn_ready":    NN_READY,
        "data_source": DATA_SOURCE,    # "real" | "synthetic" | "none"
    }

@app.post("/predict", response_model=PredictResponse)
def predict(req: PredictRequest):
    temps = np.array(req.temperatures, dtype=float)

    # Need at least 2 readings to calculate a trend
    if len(temps) < 2:
        current = float(temps[-1]) if len(temps) else 0.0
        return PredictResponse(
            label="NotEnoughData",
            score=0.0,
            predicted_temp_10m=current,
            explanation="Need at least 2 temperature readings to predict a trend.",
        )

    # Linear trend projection (~10 min ahead at 5 s intervals)
    current   = float(temps[-1])
    trend     = float(temps[-1] - temps[-2])
    predicted = round(current + trend * 2, 2)

    if NN_READY:
        features = extract_features(
            req.temperatures,
            req.humidities,
            req.pressures,
            req.light_levels,
        )
        nn_class    = predict_nn(features, nn_weights, nn_biases, nn_mean, nn_std)
        label       = LABEL_MAP[nn_class]
        score       = 0.85
        explanation = (
            f"Neural network classified sensor readings as '{label}'. "
            f"Projected temperature in ~10 min: {predicted} °C."
        )
    else:
        # Heuristic fallback — always works even if training failed
        if predicted <= -5:
            label, score, explanation = (
                "VeryCold", 0.9,
                f"Trend projects {predicted} °C — very cold. Wear extra layers.",
            )
        elif predicted < 10:
            label, score, explanation = (
                "Cold", 0.7,
                f"Trend projects {predicted} °C — cold. Consider a coat.",
            )
        elif predicted < 25:
            label, score, explanation = (
                "Normal", 0.6,
                f"Trend projects {predicted} °C — comfortable range.",
            )
        else:
            label, score, explanation = (
                "Hot", 0.8,
                f"Trend projects {predicted} °C — hot. Stay hydrated.",
            )

    return PredictResponse(
        label=label,
        score=score,
        predicted_temp_10m=predicted,
        explanation=explanation,
    )

# ═══════════════════════════════════════════════════════════════════════════
#  SECTION 5 — CLI ENTRY POINT
#  Allows:  python ml_service.py --generate-data [--db ...] [--min-rows ...]
# ═══════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="MKR IoT ML Service — run as FastAPI or generate training data."
    )
    parser.add_argument(
        "--generate-data",
        action="store_true",
        help="Export training data from sensors.db into data.txt, then exit.",
    )
    parser.add_argument(
        "--db",
        default=str(DEFAULT_DB),
        help=f"Path to sensors.db (default: {DEFAULT_DB})",
    )
    parser.add_argument(
        "--out",
        default="data.txt",
        help="Output path for data.txt (default: data.txt)",
    )
    parser.add_argument(
        "--min-rows",
        type=int,
        default=100,
        help="Minimum joined rows required to write data.txt (default: 100)",
    )

    args = parser.parse_args()

    if args.generate_data:
        generate_data_from_db(
            db_path  = Path(args.db),
            out_path = Path(args.out),
            min_rows = args.min_rows,
        )
    else:
        print("To start the service run:")
        print("  uvicorn ml_service:app --port 8000")
        print("\nTo generate training data from sensors.db run:")
        print("  python ml_service.py --generate-data")
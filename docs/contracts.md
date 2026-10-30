# Interface contracts

**Status: PROPOSAL.** Agree these in the day-1 meeting, edit anything the group wants to change, then mark each section "Agreed". Later changes go through a pull request approved by both pairs.

## 1. MQTT topic

```
factory/line1/{device_id}/telemetry
```

Example: `factory/line1/pump1/telemetry`. Suggested QoS: 1.

## 2. Message format (JSON)

```json
{
  "device_id": "pump1",
  "time": "2026-10-29T09:15:30Z",
  "vibration": 0.52,
  "temp": 61.4
}
```

| Field | Type | Notes |
|---|---|---|
| `device_id` | string | Must match a machine in the registry. The 3D object name must be identical. |
| `time` | string | UTC, ISO 8601 |
| `vibration` | number | Unit to confirm (depends on the dataset used) |
| `temp` | number | Degrees Celsius |

## 3. Machine IDs

Proposal: `pump1`, `pump2`, `pump3`. Decide now, because 3D object names must equal these IDs.

## 4. C# reading class (proposal)

```csharp
public record Reading(string DeviceId, DateTime Time, double Vibration, double Temp);
```

## 5. Public methods (proposal)

| Component | Methods |
|---|---|
| Circular buffer | `Add(Reading r)`, `Count`, `ToArrayOldestFirst()` |
| Segment Tree | `Build(double[] values)`, `Update(int index, double value)`, `QueryMax(int l, int r)`, `QueryMin(int l, int r)`, `QuerySum(int l, int r)` |

## 6. ML scoring service (REST, proposal)

`POST /score`

Request:
```json
{ "device_id": "pump1", "window": [ { "time": "...", "vibration": 0.5, "temp": 60.1 } ] }
```
Response:
```json
{ "score": 0.12, "is_anomaly": false }
```

## 7. Sign-off

| Section | Agreed by | Date |
|---|---|---|
| 1 to 3 | Pair A & Pair B | 2026-10-30 |
| 4 to 5 | | |
| 6 | | |

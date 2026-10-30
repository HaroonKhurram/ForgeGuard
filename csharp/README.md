# C# solution

ForgeGuard .NET 10 solution:

- `SensorMonitor.Core`: Core domain models (`Reading`) and custom data structures (Buffer, SegmentTree, Collections, Sorting, Searching).
- `SensorMonitor.Ingest`: MQTT subscriber (Task N3), validation, and live runner CLI.
- `SensorMonitor.Web`: Web API, alerts, ML client, circuit breaker.
- `SensorMonitor.Tests`: Unit tests for data structures, serialization, and ingestion.

## Running the MQTT Subscriber (Task N3)

The subscriber connects to the Mosquitto broker (Task N1) and parses readings from `pump1`, `pump2`, and `pump3`.

### Default Configuration
- **Broker Host:** `192.168.10.15`
- **Port:** `1883`
- **Username:** `forgeguard_sub`
- **QoS:** `1` (At Least Once)
- **Topic Filter:** `factory/line1/+/telemetry`
- **Supported Devices:** `pump1`, `pump2`, `pump3`

### Commands

Run with defaults (will prompt for password interactively if not passed via env/args):
```bash
dotnet run --project SensorMonitor.Ingest
```

Run with explicit arguments:
```bash
dotnet run --project SensorMonitor.Ingest -- --host 192.168.10.15 --port 1883 --user forgeguard_sub --pass <YOUR_PASSWORD>
```

Or configure via environment variables:
```bash
# Windows PowerShell
$env:MQTT_BROKER_HOST="192.168.10.15"
$env:MQTT_BROKER_PORT="1883"
$env:MQTT_USERNAME="forgeguard_sub"
$env:MQTT_PASSWORD="<YOUR_PASSWORD>"
dotnet run --project SensorMonitor.Ingest
```

### Running Tests
```bash
dotnet test SensorMonitor.slnx
```

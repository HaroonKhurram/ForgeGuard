namespace SensorMonitor.Core;

/// <summary>
/// Immutable record representing a single sensor reading from a factory machine.
/// Field names follow the JSON contract in docs/contracts.md.
/// </summary>
/// <param name="DeviceId">Must match a machine in the registry (e.g. "pump1").</param>
/// <param name="Time">UTC timestamp of the reading (ISO 8601).</param>
/// <param name="Vibration">Vibration value (unit depends on the dataset).</param>
/// <param name="Temp">Temperature in degrees Celsius.</param>
public record Reading(string DeviceId, DateTime Time, double Vibration, double Temp);

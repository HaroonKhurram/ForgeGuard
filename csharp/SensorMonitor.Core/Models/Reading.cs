namespace SensorMonitor.Core.Models;

/// <summary>
/// One sensor reading from one machine at one moment in time.
/// Matches section 4 of docs/contracts.md.
/// </summary>
public record Reading(string DeviceId, DateTime Time, double Vibration, double Temp);
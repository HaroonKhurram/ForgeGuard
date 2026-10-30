using SensorMonitor.Core;
using SensorMonitor.Ingest;

namespace SensorMonitor.Tests;

/// <summary>
/// Unit tests for <see cref="MqttSubscriber.ParsePayload"/>.
/// These validate the JSON-to-<see cref="Reading"/> parsing logic
/// defined by the interface contracts in docs/contracts.md.
/// </summary>
public class MqttSubscriberTests
{
    // ───────────────────────────── Happy-path tests ─────────────────

    [Fact]
    public void ParsePayload_ValidJson_ReturnsCorrectReading()
    {
        // Arrange — exact format from contracts.md §2
        const string json = """
            {
                "device_id": "pump1",
                "time": "2026-10-29T09:15:30Z",
                "vibration": 0.52,
                "temp": 61.4
            }
            """;

        // Act
        var reading = MqttSubscriber.ParsePayload(json);

        // Assert
        Assert.Equal("pump1", reading.DeviceId);
        Assert.Equal(new DateTime(2026, 10, 29, 9, 15, 30, DateTimeKind.Utc), reading.Time);
        Assert.Equal(0.52, reading.Vibration, precision: 6);
        Assert.Equal(61.4, reading.Temp, precision: 6);
    }

    [Fact]
    public void ParsePayload_AllDeviceIds_AreAccepted()
    {
        // contracts.md §3 says device IDs are pump1, pump2, pump3.
        string[] ids = ["pump1", "pump2", "pump3"];

        foreach (var id in ids)
        {
            var json = $$"""
                {
                    "device_id": "{{id}}",
                    "time": "2026-10-29T10:00:00Z",
                    "vibration": 1.0,
                    "temp": 50.0
                }
                """;

            var reading = MqttSubscriber.ParsePayload(json);
            Assert.Equal(id, reading.DeviceId);
        }
    }

    [Fact]
    public void ParsePayload_NegativeValues_AreAccepted()
    {
        // Vibration and temp can be negative (e.g. cold-start scenarios).
        const string json = """
            {
                "device_id": "pump1",
                "time": "2026-10-29T10:00:00Z",
                "vibration": -0.1,
                "temp": -5.3
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json);
        Assert.Equal(-0.1, reading.Vibration, precision: 6);
        Assert.Equal(-5.3, reading.Temp, precision: 6);
    }

    [Fact]
    public void ParsePayload_ZeroValues_AreAccepted()
    {
        const string json = """
            {
                "device_id": "pump2",
                "time": "2026-10-29T10:00:00Z",
                "vibration": 0,
                "temp": 0
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json);
        Assert.Equal(0.0, reading.Vibration);
        Assert.Equal(0.0, reading.Temp);
    }

    [Fact]
    public void ParsePayload_TimestampWithOffset_IsConvertedToUtc()
    {
        // A publisher may send an offset; we must store UTC.
        const string json = """
            {
                "device_id": "pump3",
                "time": "2026-10-29T14:15:30+05:00",
                "vibration": 0.5,
                "temp": 60.0
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json);
        Assert.Equal(DateTimeKind.Utc, reading.Time.Kind);
        Assert.Equal(new DateTime(2026, 10, 29, 9, 15, 30, DateTimeKind.Utc), reading.Time);
    }

    [Fact]
    public void ParsePayload_ExtraFieldsAreIgnored()
    {
        // Forward-compatible: future messages may add new fields.
        const string json = """
            {
                "device_id": "pump1",
                "time": "2026-10-29T09:15:30Z",
                "vibration": 0.52,
                "temp": 61.4,
                "humidity": 45.0,
                "pressure": 101.3
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json);
        Assert.Equal("pump1", reading.DeviceId);
    }

    [Fact]
    public void ParsePayload_UserSpecifiedFormat_MatchesExactly()
    {
        // Exact JSON from user prompt
        const string json = """
            {
              "device_id": "pump1",
              "time": "2026-10-30T10:00:00Z",
              "vibration": 0.5,
              "temp": 60.2
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json);

        Assert.Equal("pump1", reading.DeviceId);
        Assert.Equal(new DateTime(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc), reading.Time);
        Assert.Equal(0.5, reading.Vibration, precision: 6);
        Assert.Equal(60.2, reading.Temp, precision: 6);
    }

    [Theory]
    [InlineData("PUMP1")]
    [InlineData("Pump2")]
    [InlineData("pump3")]
    public void ParsePayload_SupportedDeviceIds_CaseInsensitive(string deviceId)
    {
        var json = $$"""
            {
              "device_id": "{{deviceId}}",
              "time": "2026-10-30T10:00:00Z",
              "vibration": 0.5,
              "temp": 60.2
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json, validateSupportedDevice: true);
        Assert.Equal(deviceId, reading.DeviceId);
    }

    [Fact]
    public void ParsePayload_UnsupportedDevice_ThrowsFormatException_WhenValidationEnabled()
    {
        const string json = """
            {
              "device_id": "unknown_robot",
              "time": "2026-10-30T10:00:00Z",
              "vibration": 0.5,
              "temp": 60.2
            }
            """;

        var ex = Assert.Throws<FormatException>(() =>
            MqttSubscriber.ParsePayload(json, validateSupportedDevice: true));

        Assert.Contains("Unsupported device_id: 'unknown_robot'", ex.Message);
    }

    [Fact]
    public void ParsePayload_UnsupportedDevice_Succeeds_WhenValidationDisabled()
    {
        const string json = """
            {
              "device_id": "custom_sensor",
              "time": "2026-10-30T10:00:00Z",
              "vibration": 0.5,
              "temp": 60.2
            }
            """;

        var reading = MqttSubscriber.ParsePayload(json, validateSupportedDevice: false);
        Assert.Equal("custom_sensor", reading.DeviceId);
    }

    [Fact]
    public void MqttSubscriber_DefaultConfiguration_MatchesSpecifications()
    {
        using var sub = new MqttSubscriber();

        Assert.Equal("192.168.10.15", sub.BrokerHost);
        Assert.Equal(1883, sub.BrokerPort);
        Assert.Equal("forgeguard_sub", sub.Username);
        Assert.Equal("factory/line1/+/telemetry", sub.TopicFilter);
        Assert.True(sub.ValidateSupportedDevices);
        Assert.Contains("pump1", MqttSubscriber.SupportedDeviceIds);
        Assert.Contains("pump2", MqttSubscriber.SupportedDeviceIds);
        Assert.Contains("pump3", MqttSubscriber.SupportedDeviceIds);
    }

    // ───────────────────────────── Error-path tests ─────────────────

    [Fact]
    public void ParsePayload_EmptyString_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => MqttSubscriber.ParsePayload(""));
    }

    [Fact]
    public void ParsePayload_InvalidJson_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() =>
            MqttSubscriber.ParsePayload("this is not json"));
    }

    [Fact]
    public void ParsePayload_MissingDeviceId_ThrowsFormatException()
    {
        const string json = """
            {
                "time": "2026-10-29T09:15:30Z",
                "vibration": 0.52,
                "temp": 61.4
            }
            """;

        Assert.Throws<FormatException>(() => MqttSubscriber.ParsePayload(json));
    }

    [Fact]
    public void ParsePayload_EmptyDeviceId_ThrowsFormatException()
    {
        const string json = """
            {
                "device_id": "",
                "time": "2026-10-29T09:15:30Z",
                "vibration": 0.52,
                "temp": 61.4
            }
            """;

        Assert.Throws<FormatException>(() => MqttSubscriber.ParsePayload(json));
    }

    [Fact]
    public void ParsePayload_MissingTime_ThrowsFormatException()
    {
        const string json = """
            {
                "device_id": "pump1",
                "vibration": 0.52,
                "temp": 61.4
            }
            """;

        Assert.Throws<FormatException>(() => MqttSubscriber.ParsePayload(json));
    }

    [Fact]
    public void ParsePayload_InvalidTimestamp_ThrowsFormatException()
    {
        const string json = """
            {
                "device_id": "pump1",
                "time": "not-a-date",
                "vibration": 0.52,
                "temp": 61.4
            }
            """;

        Assert.Throws<FormatException>(() => MqttSubscriber.ParsePayload(json));
    }

    [Fact]
    public void ParsePayload_NullPayload_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() =>
            MqttSubscriber.ParsePayload(null!));
    }

    // ───────────────────────── Reading record tests ─────────────────

    [Fact]
    public void Reading_EqualityWorks()
    {
        var dt = new DateTime(2026, 10, 29, 9, 15, 30, DateTimeKind.Utc);
        var r1 = new Reading("pump1", dt, 0.52, 61.4);
        var r2 = new Reading("pump1", dt, 0.52, 61.4);

        Assert.Equal(r1, r2);
    }

    [Fact]
    public void Reading_InequalityOnDifferentDevice()
    {
        var dt = new DateTime(2026, 10, 29, 9, 15, 30, DateTimeKind.Utc);
        var r1 = new Reading("pump1", dt, 0.52, 61.4);
        var r2 = new Reading("pump2", dt, 0.52, 61.4);

        Assert.NotEqual(r1, r2);
    }
}

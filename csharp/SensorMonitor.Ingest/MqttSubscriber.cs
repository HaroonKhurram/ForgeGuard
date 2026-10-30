using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using SensorMonitor.Core;

namespace SensorMonitor.Ingest;

/// <summary>
/// JSON DTO that mirrors the MQTT payload defined in docs/contracts.md §2:
/// {
///   "device_id": "pump1",
///   "time": "2026-10-30T10:00:00Z",
///   "vibration": 0.5,
///   "temp": 60.2
/// }
/// Property names use snake_case to match the JSON wire format; the
/// <see cref="JsonPropertyNameAttribute"/> attributes handle the mapping.
/// </summary>
internal sealed class TelemetryPayload
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("vibration")]
    public double Vibration { get; set; }

    [JsonPropertyName("temp")]
    public double Temp { get; set; }
}

/// <summary>
/// Connects to an MQTT broker, subscribes to the telemetry topic for all
/// devices on line 1, parses incoming JSON payloads into <see cref="Reading"/>
/// objects, and forwards them through the <see cref="ReadingReceived"/> event.
///
/// Topic pattern: <c>factory/line1/+/telemetry</c> (the + wildcard matches
/// any device_id). QoS 1 is used as specified in contracts.md §1.
///
/// Authentication: Supports authenticated brokers (e.g. forgeguard_sub).
///
/// Supported Device IDs: pump1, pump2, pump3.
///
/// This class is the deliverable for task N3 in the ForgeGuard work plan.
/// </summary>
public sealed class MqttSubscriber : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Set of supported machine/device IDs according to contracts.md §3:
    /// pump1, pump2, pump3.
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedDeviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "pump1",
        "pump2",
        "pump3"
    };

    // ── Configuration ────────────────────────────────────────────────
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _clientId;
    private readonly string _topicFilter;
    private readonly bool _validateSupportedDevices;

    // ── MQTTnet internals ────────────────────────────────────────────
    private IMqttClient? _client;

    // ── JSON options (reused for every message) ──────────────────────
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Public events ────────────────────────────────────────────────

    /// <summary>Raised for every successfully parsed reading.</summary>
    public event Action<Reading>? ReadingReceived;

    /// <summary>Raised when a message cannot be parsed (bad JSON, missing fields, etc.).</summary>
    public event Action<string, Exception>? ParseError;

    /// <summary>Raised when the client disconnects from the broker.</summary>
    public event Action<string>? Disconnected;

    /// <summary>Raised when the client successfully connects to the broker.</summary>
    public event Action? Connected;

    // ── Properties ───────────────────────────────────────────────────
    public string BrokerHost => _brokerHost;
    public int BrokerPort => _brokerPort;
    public string? Username => _username;
    public string TopicFilter => _topicFilter;
    public string ClientId => _clientId;
    public bool ValidateSupportedDevices => _validateSupportedDevices;
    public bool IsConnected => _client?.IsConnected ?? false;

    // ── Constructor ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a new subscriber instance configured for the ForgeGuard N1 broker.
    /// </summary>
    /// <param name="brokerHost">
    /// Hostname or IP of the Mosquitto broker. Defaults to environment variable
    /// <c>MQTT_BROKER_HOST</c> or <c>192.168.10.15</c>.
    /// </param>
    /// <param name="brokerPort">
    /// TCP port of the broker (default: 1883). Defaults to environment variable <c>MQTT_BROKER_PORT</c> if set.
    /// </param>
    /// <param name="username">
    /// Broker username for authentication (default: "forgeguard_sub"). Defaults to environment variable <c>MQTT_USERNAME</c>.
    /// </param>
    /// <param name="password">
    /// Broker password for authentication (optional, or read from environment variable <c>MQTT_PASSWORD</c>).
    /// </param>
    /// <param name="clientId">
    /// MQTT client ID; must be unique per connection. Generated automatically if null.
    /// </param>
    /// <param name="topicFilter">
    /// Topic filter to subscribe to. Defaults to <c>factory/line1/+/telemetry</c>.
    /// </param>
    /// <param name="validateSupportedDevices">
    /// If true, validates incoming device IDs against {pump1, pump2, pump3}. Defaults to true.
    /// </param>
    public MqttSubscriber(
        string? brokerHost = null,
        int? brokerPort = null,
        string? username = null,
        string? password = null,
        string? clientId = null,
        string? topicFilter = null,
        bool validateSupportedDevices = true)
    {
        _brokerHost = brokerHost
            ?? Environment.GetEnvironmentVariable("MQTT_BROKER_HOST")
            ?? "192.168.10.15";

        _brokerPort = brokerPort
            ?? (int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out var p) ? p : 1883);

        _username = username
            ?? Environment.GetEnvironmentVariable("MQTT_USERNAME")
            ?? "forgeguard_sub";

        _password = password
            ?? Environment.GetEnvironmentVariable("MQTT_PASSWORD");

        _clientId = clientId ?? $"ForgeGuard-Sub-{Guid.NewGuid():N}";
        _topicFilter = topicFilter ?? "factory/line1/+/telemetry";
        _validateSupportedDevices = validateSupportedDevices;
    }

    // ── Connect & Subscribe ──────────────────────────────────────────

    /// <summary>
    /// Connects to the broker using TCP (port 1883) with credentials and subscribes with QoS 1.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        // Wire up handlers before connecting.
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;

        var builder = factory.CreateClientOptionsBuilder()
            .WithTcpServer(_brokerHost, _brokerPort)
            .WithClientId(_clientId)
            .WithCleanSession(true);

        if (!string.IsNullOrEmpty(_username))
        {
            builder.WithCredentials(_username, _password);
        }

        var options = builder.Build();

        var result = await _client.ConnectAsync(options, ct);

        if (result.ResultCode != MqttClientConnectResultCode.Success)
        {
            throw new InvalidOperationException(
                $"MQTT connection to {_brokerHost}:{_brokerPort} failed: {result.ResultCode} ({result.ResponseInformation})");
        }

        Connected?.Invoke();

        // Subscribe with QoS 1 as specified in contracts.md §1.
        var subOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(_topicFilter)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _client.SubscribeAsync(subOptions, ct);
    }

    // ── Disconnect ───────────────────────────────────────────────────

    /// <summary>
    /// Gracefully disconnects from the broker.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_client is { IsConnected: true })
        {
            var disconnectOptions = new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build();
            await _client.DisconnectAsync(disconnectOptions);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _client?.Dispose();
        _client = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    // ── Message handling ─────────────────────────────────────────────

    /// <summary>
    /// Called for every MQTT message that matches the subscription filter.
    /// Parses the JSON payload into a <see cref="Reading"/> and fires
    /// <see cref="ReadingReceived"/>, or fires <see cref="ParseError"/>
    /// if parsing fails.
    /// </summary>
    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        string raw = string.Empty;
        try
        {
            raw = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var reading = ParsePayload(raw, _validateSupportedDevices);
            ReadingReceived?.Invoke(reading);
        }
        catch (Exception ex)
        {
            ParseError?.Invoke(raw, ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles broker disconnection events.
    /// </summary>
    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        var reason = e.Reason.ToString();
        Disconnected?.Invoke(reason);
        return Task.CompletedTask;
    }

    // ── Parsing & Validation ─────────────────────────────────────────

    /// <summary>
    /// Checks whether the given device ID belongs to the supported devices registry.
    /// </summary>
    public static bool IsSupportedDevice(string? deviceId)
    {
        return !string.IsNullOrWhiteSpace(deviceId) && SupportedDeviceIds.Contains(deviceId.Trim());
    }

    /// <summary>
    /// Parses a JSON payload string into a <see cref="Reading"/>.
    /// Validates that required fields are present, device_id is valid, and timestamps are in ISO 8601 UTC.
    /// </summary>
    /// <param name="json">Raw UTF-8 JSON string from the MQTT payload.</param>
    /// <param name="validateSupportedDevice">If true, throws if device_id is not in pump1, pump2, pump3. Defaults to true.</param>
    /// <returns>A validated <see cref="Reading"/>.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the JSON is malformed or required fields are missing / invalid.
    /// </exception>
    public static Reading ParsePayload(string json, bool validateSupportedDevice = true)
    {
        var dto = JsonSerializer.Deserialize<TelemetryPayload>(json, JsonOptions);

        if (dto is null)
            throw new FormatException("Payload deserialized to null.");

        // ── Validate required fields ──
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
            throw new FormatException("Missing or empty 'device_id' field.");

        var cleanDeviceId = dto.DeviceId.Trim();

        if (validateSupportedDevice && !IsSupportedDevice(cleanDeviceId))
        {
            throw new FormatException(
                $"Unsupported device_id: '{cleanDeviceId}'. Supported devices are: {string.Join(", ", SupportedDeviceIds)}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Time))
            throw new FormatException("Missing or empty 'time' field.");

        if (!DateTime.TryParse(dto.Time, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsedTime))
        {
            throw new FormatException(
                $"Invalid timestamp format: '{dto.Time}'. Expected ISO 8601.");
        }

        // Ensure the timestamp is stored in UTC.
        parsedTime = parsedTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(parsedTime, DateTimeKind.Utc)
            : parsedTime.ToUniversalTime();

        return new Reading(cleanDeviceId, parsedTime, dto.Vibration, dto.Temp);
    }
}

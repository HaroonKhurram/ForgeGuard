using SensorMonitor.Ingest;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("====================================================================");
Console.WriteLine("  ForgeGuard Industrial IoT - C# MQTT Telemetry Subscriber (N3)     ");
Console.WriteLine("====================================================================");

string host = "192.168.10.15";
int port = 1883;
string username = "forgeguard_sub";
string? password = Environment.GetEnvironmentVariable("MQTT_PASSWORD");
string topic = "factory/line1/+/telemetry";
bool validateDevices = true;

// Parse command line arguments
for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--host" or "-h" when i + 1 < args.Length:
            host = args[++i];
            break;
        case "--port" or "-p" when i + 1 < args.Length:
            if (int.TryParse(args[++i], out var p)) port = p;
            break;
        case "--user" or "-u" when i + 1 < args.Length:
            username = args[++i];
            break;
        case "--pass" when i + 1 < args.Length:
            password = args[++i];
            break;
        case "--topic" or "-t" when i + 1 < args.Length:
            topic = args[++i];
            break;
        case "--no-validate":
            validateDevices = false;
            break;
        case "--help":
            PrintUsage();
            return;
    }
}

// Prompt for password if not supplied and stdin is interactive
if (string.IsNullOrEmpty(password) && !Console.IsInputRedirected)
{
    Console.Write($"Enter MQTT password for user '{username}' (press Enter to skip): ");
    password = ReadMaskedPassword();
    Console.WriteLine();
}

Console.WriteLine($"[Config] Broker Host       : {host}:{port}");
Console.WriteLine($"[Config] Username          : {username}");
Console.WriteLine($"[Config] Topic Filter      : {topic}");
Console.WriteLine($"[Config] QoS Level         : 1 (AtLeastOnce)");
Console.WriteLine($"[Config] Supported Devices : {string.Join(", ", MqttSubscriber.SupportedDeviceIds)}");
Console.WriteLine($"[Config] Device Validation : {(validateDevices ? "Enabled" : "Disabled")}");
Console.WriteLine("--------------------------------------------------------------------");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[Shutdown] Stopping subscriber gracefully...");
    cts.Cancel();
};

await using var subscriber = new MqttSubscriber(
    brokerHost: host,
    brokerPort: port,
    username: username,
    password: string.IsNullOrEmpty(password) ? null : password,
    topicFilter: topic,
    validateSupportedDevices: validateDevices);

subscriber.Connected += () =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[INFO] Successfully connected to broker at {host}:{port}");
    Console.WriteLine($"[INFO] Subscribed to '{topic}' (QoS 1). Waiting for messages... (Ctrl+C to quit)");
    Console.ResetColor();
};

subscriber.Disconnected += reason =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[WARN] Disconnected from broker: {reason}");
    Console.ResetColor();
};

subscriber.ReadingReceived += reading =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"[{reading.Time:yyyy-MM-dd HH:mm:ss 'UTC'}] ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"[{reading.DeviceId,-6}] ");
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write($"Vibration: {reading.Vibration,6:F3} ");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"| Temp: {reading.Temp,5:F1} °C");
    Console.ResetColor();
};

subscriber.ParseError += (rawPayload, ex) =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR] Failed to parse payload: {ex.Message}");
    Console.WriteLine($"        Raw JSON: {rawPayload}");
    Console.ResetColor();
};

try
{
    Console.WriteLine($"[Connecting] Attempting connection to {host}:{port}...");
    await subscriber.ConnectAsync(cts.Token);
    
    // Keep alive until Ctrl+C is pressed
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[INFO] Subscriber stopped.");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[FATAL] Error running MQTT subscriber: {ex.Message}");
    Console.ResetColor();
}
finally
{
    await subscriber.DisconnectAsync();
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run --project csharp/SensorMonitor.Ingest [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --host, -h <ip>       Broker IP/hostname (default: 192.168.10.15 or MQTT_BROKER_HOST)");
    Console.WriteLine("  --port, -p <port>     Broker port (default: 1883 or MQTT_BROKER_PORT)");
    Console.WriteLine("  --user, -u <user>     Username (default: forgeguard_sub or MQTT_USERNAME)");
    Console.WriteLine("  --pass <password>     Password (or MQTT_PASSWORD env var, or interactive prompt)");
    Console.WriteLine("  --topic, -t <filter>  Subscription topic filter (default: factory/line1/+/telemetry)");
    Console.WriteLine("  --no-validate         Allow arbitrary device IDs outside pump1, pump2, pump3");
    Console.WriteLine("  --help                Show this help message");
}

static string ReadMaskedPassword()
{
    var sb = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                sb.Length--;
                Console.Write("\b \b");
            }
        }
        else if (!char.IsControl(key.KeyChar))
        {
            sb.Append(key.KeyChar);
            Console.Write("*");
        }
    }
    return sb.ToString();
}


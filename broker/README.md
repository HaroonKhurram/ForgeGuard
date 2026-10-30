# MQTT broker

Task N1. Document here: how to install Mosquitto, how to start it, the topic design, authentication setup,
and how to send a test message with `mosquitto_pub`.

Never commit the password file.
## 1. Install Mosquitto

Download Mosquitto from the official website:

https://mosquitto.org/download/

Verify installation:

```powershell
mosquitto -h
```

## 2. Broker Configuration

The `mosquitto.conf` file:

```conf
listener 1883
allow_anonymous false
password_file passwd
```

## 3. Authentication Setup

First, open the broker directory from the project root:

```powershell
cd broker
```

Create publisher and subscriber accounts:

```powershell
mosquitto_passwd -c passwd forgeguard_pub
mosquitto_passwd passwd forgeguard_sub
```

Start the broker from the same directory:

```powershell
mosquitto -c mosquitto.conf -v
```

Keep this terminal running while testing MQTT communication.
## 4. MQTT Topic Design

Topic structure:

```text
factory/line1/{device_id}/telemetry
```

Examples:

- `factory/line1/pump1/telemetry`
- `factory/line1/pump2/telemetry`
- `factory/line1/pump3/telemetry`

## 5. Send a Test Message

Subscribe in another terminal:

```powershell
mosquitto_sub -h localhost -u forgeguard_sub -P "YOUR_PASSWORD" -t "factory/line1/+/telemetry" -q 1
```

Publish a test message:

```powershell
mosquitto_pub -h localhost -u forgeguard_pub -P "YOUR_PASSWORD" -t "factory/line1/pump1/telemetry" -q 1 -m '{"device_id":"pump1","vibration":0.52,"temp":61.4}'
```

## 6. Security

- Anonymous access is disabled.
- Authentication is required.
- Never commit the actual `passwd` file.
- The `passwd` file is excluded through `.gitignore`.

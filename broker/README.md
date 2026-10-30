# MQTT Broker Setup & Authentication (Task N1)

Configuration and notes for the Eclipse Mosquitto broker powering ForgeGuard.

> **Security Warning:** Never commit password files, secret keys, or `.env` files to git.

---

## 1. Broker Configuration
- **Host / IP:** `192.168.10.15` (or `127.0.0.1` for local loopback)
- **Port:** `1883`
- **Protocol:** MQTT
- **Authentication:** Required (`allow_anonymous false`)
- **Subscriber Username:** `forgeguard_sub`
- **Publisher Username:** `forgeguard_pub`
- **QoS:** `1`

---

## 2. Setting Up Broker Passwords with `mosquitto_passwd`

Run these commands on the machine hosting the Mosquitto broker (`192.168.10.15`):

### Step 1: Create the password file and add `forgeguard_sub`
The `-c` flag creates (or overwrites) a new password file:
```bash
mosquitto_passwd -c passwordfile forgeguard_sub
```
You will be prompted to type and confirm the password for `forgeguard_sub`.

### Step 2: Add `forgeguard_pub` (without `-c`)
Do **not** use `-c` here, so you append rather than overwrite:
```bash
mosquitto_passwd passwordfile forgeguard_pub
```

*(Optional: To set passwords non-interactively in scripts, use `-b`)*:
```bash
mosquitto_passwd -b -c passwordfile forgeguard_sub <SUB_PASSWORD>
mosquitto_passwd -b passwordfile forgeguard_pub <PUB_PASSWORD>
```

---

## 3. Configure `mosquitto.conf`

Ensure your `mosquitto.conf` contains the following lines:

```ini
# Listen on port 1883 for incoming network connections
listener 1883 0.0.0.0

# Disable anonymous access
allow_anonymous false

# Absolute or relative path to your password file
password_file passwordfile
```

> **Note on Windows:** If Mosquitto is installed at `C:\Program Files\mosquitto`, use:
> `password_file C:\Program Files\mosquitto\passwordfile` (or place it next to `mosquitto.conf`).

---

## 4. Start / Restart Mosquitto

Start Mosquitto with your configuration file:

```bash
mosquitto -c mosquitto.conf -v
```

*(If running as a Windows service)*:
```powershell
net stop mosquitto
net start mosquitto
```

*(If running on Linux)*:
```bash
sudo systemctl restart mosquitto
```

---

## 5. Testing the Setup

### Test Publishing (`mosquitto_pub`)
```bash
mosquitto_pub -h 192.168.10.15 -p 1883 -u forgeguard_pub -P "<PUB_PASSWORD>" -q 1 \
  -t "factory/line1/pump1/telemetry" \
  -m '{"device_id":"pump1","time":"2026-10-30T10:00:00Z","vibration":0.5,"temp":60.2}'
```

### Test Subscribing (`mosquitto_sub`)
```bash
mosquitto_sub -h 192.168.10.15 -p 1883 -u forgeguard_sub -P "<SUB_PASSWORD>" -q 1 \
  -t "factory/line1/+/telemetry" -v
```

---

## 6. Supplying the Password to the C# Subscriber (Task N3)

Once the password is set on the broker, provide it to the C# subscriber in one of three ways:

1. **Interactive Prompt:**
   ```powershell
   dotnet run --project csharp/SensorMonitor.Ingest
   ```
   *(Prompts for the password securely on launch).*

2. **Command Line Flag:**
   ```powershell
   dotnet run --project csharp/SensorMonitor.Ingest -- --pass <SUB_PASSWORD>
   ```

3. **Environment Variable:**
   ```powershell
   $env:MQTT_PASSWORD="<SUB_PASSWORD>"
   dotnet run --project csharp/SensorMonitor.Ingest
   ```

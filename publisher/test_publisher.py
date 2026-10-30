
import json
import os
import time
import random
from datetime import datetime, timezone

import paho.mqtt.client as mqtt

BROKER = os.getenv("MQTT_HOST", "localhost")
PORT = int(os.getenv("MQTT_PORT", "1883"))
USER = os.getenv("MQTT_USER", "forgeguard_pub")
PASSWORD = os.environ["MQTT_PASS"]

DEVICES = ["pump1", "pump2", "pump3"]

client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2)
client.username_pw_set(USER, PASSWORD)

client.connect(BROKER, PORT)
client.loop_start()

try:
    while True:
        for device in DEVICES:
            msg = {
                "device_id": device,
                "time": datetime.now(timezone.utc).strftime(
                    "%Y-%m-%dT%H:%M:%SZ"
                ),
                "vibration": round(random.gauss(0.5, 0.05), 3),
                "temp": round(random.gauss(60, 1.5), 1)
            }

            topic = f"factory/line1/{device}/telemetry"

            client.publish(topic, json.dumps(msg), qos=1)
            print("Sent:", topic, msg)

        time.sleep(1)

except KeyboardInterrupt:
    print("\nPublisher stopped.")

finally:
    client.loop_stop()
    client.disconnect()

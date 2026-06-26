"""
ControlSource demo script for Frisson.

Usage:
    python ctrl.py [name]

Connects to Frisson via WebSocket, completes the bind handshake, then enters
an interactive loop:

    s / send  — read input.json, send to Frisson, write reply to output.json
    q / quit  — exit

If WebSocket fails at any point, prints error to stdout and exits with code 1.
"""

import json
import os
import sys

try:
    import websocket
except ImportError:
    print("Error: websocket-client not installed. Run: pip install websocket-client")
    sys.exit(1)


SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
INPUT_FILE = os.path.join(SCRIPT_DIR, "input.json")
OUTPUT_FILE = os.path.join(SCRIPT_DIR, "output.json")
WS_URL = "ws://localhost:6969"


def main():
    name = sys.argv[1] if len(sys.argv) > 1 else "DemoController"

    try:
        ws = websocket.create_connection(WS_URL)
    except Exception as e:
        print(f"Error: Failed to connect to {WS_URL}: {e}")
        sys.exit(1)

    try:
        # Step 1: receive initial bind from Frisson
        raw = ws.recv()
        initial = json.loads(raw)
        print(f"[Frisson] initial bind: {json.dumps(initial)}")

        # Step 2: send Control bind (name only, Frisson assigns UUID)
        bind_msg = json.dumps({"type": "bind", "name": name})
        ws.send(bind_msg)
        print(f"[ -> ] bind: {bind_msg}")

        # Step 3: receive Frisson's bind reply (contains assigned UUID)
        raw = ws.recv()
        reply = json.loads(raw)
        print(f"[Frisson] bind reply: {json.dumps(reply)}")
        assigned_id = reply.get("id", "?")
        print(f"Connected as '{name}' (id={assigned_id})")
    except Exception as e:
        print(f"Error: Bind handshake failed: {e}")
        ws.close()
        sys.exit(1)

    # Interactive loop
    print("Ready. Commands: s/send  q/quit")
    try:
        while True:
            try:
                cmd = input().strip().lower()
            except EOFError:
                break

            if cmd in ("q", "quit"):
                break
            elif cmd in ("s", "send"):
                try:
                    with open(INPUT_FILE, "r") as f:
                        data = json.load(f)
                    ws.send(json.dumps(data))
                    raw = ws.recv()
                    reply = json.loads(raw)
                    with open(OUTPUT_FILE, "w") as f:
                        json.dump(reply, f, indent=2)
                    print("OK")
                except Exception as e:
                    print(f"Error: {e}")
                    ws.close()
                    sys.exit(1)
    finally:
        ws.close()


if __name__ == "__main__":
    main()

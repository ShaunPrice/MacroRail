"""Web UI server: control the rail from a phone or browser over WiFi.

Mirrors the Windows MacroRail workflow - jog, set start, program a shoot
(step count x step size), run it, then optionally stack the result on
the board.

Run on the Ultra96-V2:
    sudo python3 -m macrorail96.server --backend tic          # Pololu TIC over USB
    sudo python3 -m macrorail96.server --backend pl           # FPGA peripheral
    sudo python3 -m macrorail96.server --backend pl --hw-trigger

Then browse to http://<board-ip>:8096/.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import argparse
import os
import threading
import traceback

from flask import Flask, jsonify, request, send_from_directory

from .camera import CameraError, GPhoto2Camera, HardwareTrigger

app = Flask(__name__, static_folder="static")

state = {
    "stepper": None,
    "camera": None,
    "shoot": {
        "running": False,
        "abort": False,
        "current": 0,
        "total": 0,
        "message": "idle",
    },
    "lock": threading.Lock(),
}


@app.route("/")
def index():
    return send_from_directory(app.static_folder, "index.html")


@app.route("/api/status")
def api_status():
    stepper = state["stepper"]
    status = stepper.status() if stepper else {}
    camera = state["camera"]
    camera_name = None
    if camera:
        try:
            camera_name = camera.detect()
        except CameraError as exc:
            camera_name = f"not detected ({exc})"
    return jsonify(
        {
            "stepper": status,
            "camera": camera_name,
            "shoot": {k: v for k, v in state["shoot"].items() if k != "abort"},
        }
    )


@app.route("/api/jog", methods=["POST"])
def api_jog():
    mm = float(request.json.get("mm", 0.0))
    state["stepper"].move_mm(mm, trigger=False, wait=False)
    return jsonify({"ok": True})


@app.route("/api/abort", methods=["POST"])
def api_abort():
    state["shoot"]["abort"] = True
    state["stepper"].abort()
    return jsonify({"ok": True})


@app.route("/api/zero", methods=["POST"])
def api_zero():
    state["stepper"].zero_position()
    return jsonify({"ok": True})


@app.route("/api/speed", methods=["POST"])
def api_speed():
    state["stepper"].set_speed(float(request.json["max_steps_per_sec"]))
    return jsonify({"ok": True})


@app.route("/api/shoot", methods=["POST"])
def api_shoot():
    """Start a stacking shoot: step_count moves of step_size_mm, one
    capture per position (including the start position)."""
    if state["shoot"]["running"]:
        return jsonify({"ok": False, "error": "shoot already running"}), 409
    cfg = request.json
    thread = threading.Thread(
        target=_run_shoot,
        args=(
            cfg.get("name", "shoot"),
            int(cfg["step_count"]),
            float(cfg["step_size_mm"]),
            bool(cfg.get("return_to_start", True)),
        ),
        daemon=True,
    )
    thread.start()
    return jsonify({"ok": True})


def _run_shoot(name: str, step_count: int, step_size_mm: float, return_to_start: bool):
    shoot = state["shoot"]
    stepper = state["stepper"]
    camera = state["camera"]
    shoot.update(running=True, abort=False, current=0, total=step_count + 1)
    captured = []
    try:
        for i in range(step_count + 1):
            if shoot["abort"]:
                shoot["message"] = "aborted"
                break
            shoot["message"] = f"capturing {i + 1}/{step_count + 1}"
            shoot["current"] = i + 1
            if camera:
                captured.append(camera.capture(f"{name}_{i:04d}.jpg"))
            if i < step_count:
                shoot["message"] = f"moving to position {i + 2}"
                stepper.move_mm(step_size_mm, trigger=False, wait=True)
        if return_to_start and not shoot["abort"]:
            shoot["message"] = "returning to start"
            stepper.move_mm(-step_size_mm * step_count, trigger=False, wait=True)
        if not shoot["abort"]:
            shoot["message"] = f"done ({len(captured)} frames)"
    except Exception as exc:  # surface errors to the UI
        traceback.print_exc()
        shoot["message"] = f"error: {exc}"
    finally:
        shoot["running"] = False


@app.route("/api/stack", methods=["POST"])
def api_stack():
    """Stack the frames of a completed shoot on the board."""
    from .stacking import stack_images  # deferred: needs OpenCV

    shoot_dir = request.json["dir"]
    output = os.path.join(shoot_dir, "stacked.jpg")
    import glob

    paths = sorted(glob.glob(os.path.join(shoot_dir, "*.jpg")))
    paths = [p for p in paths if not p.endswith("stacked.jpg")]
    result = stack_images(paths, progress=lambda m: state["shoot"].update(message=m))
    import cv2

    cv2.imwrite(output, result)
    return jsonify({"ok": True, "output": output})


def main() -> None:
    parser = argparse.ArgumentParser(description="MacroRail96 controller")
    parser.add_argument("--backend", choices=["pl", "tic"], default="tic")
    parser.add_argument("--steps-per-mm", type=float, default=800.0)
    parser.add_argument("--hw-trigger", action="store_true",
                        help="fire the camera via the FPGA cable-release pins "
                             "instead of gphoto2 (pl backend only)")
    parser.add_argument("--no-camera", action="store_true")
    parser.add_argument("--port", type=int, default=8096)
    args = parser.parse_args()

    if args.backend == "pl":
        from .stepper_pl import PLStepper

        state["stepper"] = PLStepper(steps_per_mm=args.steps_per_mm)
        state["stepper"].enable_motor(True)
        state["stepper"].set_trigger()
    else:
        from .stepper_tic import TicStepper

        state["stepper"] = TicStepper(steps_per_mm=args.steps_per_mm)
        state["stepper"].enable_motor(True)

    if not args.no_camera:
        if args.hw_trigger and args.backend == "pl":
            state["camera"] = HardwareTrigger(state["stepper"])
        else:
            state["camera"] = GPhoto2Camera()

    app.run(host="0.0.0.0", port=args.port, threaded=True)


if __name__ == "__main__":
    main()

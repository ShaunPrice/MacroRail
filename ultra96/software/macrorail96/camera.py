"""Nikon camera control for the Ultra96-V2.

Two capture paths:

- ``GPhoto2Camera``: full tethering over USB with libgphoto2, which
  supports Nikon PTP (capture, image download, exposure settings). This
  replaces the Windows-only Nikon SDK used by the desktop app, and is
  what makes the Ultra96 port possible at all.
- ``HardwareTrigger``: the FPGA peripheral's optically isolated
  focus/shutter outputs wired to a Nikon 10-pin (MC-30 style) or MC-DC2
  remote cable. Images then land on the camera's memory card; timing is
  hardware-exact.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import os
import subprocess
import time


class CameraError(RuntimeError):
    pass


class GPhoto2Camera:
    """Tethered capture through the gphoto2 CLI (apt install gphoto2)."""

    def __init__(self, save_dir: str = "/home/xilinx/shoots"):
        self.save_dir = save_dir
        os.makedirs(save_dir, exist_ok=True)

    def _run(self, *args: str, timeout: float = 60.0) -> str:
        result = subprocess.run(
            ["gphoto2", *args],
            capture_output=True,
            text=True,
            timeout=timeout,
        )
        if result.returncode != 0:
            raise CameraError(result.stderr.strip() or result.stdout.strip())
        return result.stdout

    def detect(self) -> str:
        """Return the detected camera model, or raise CameraError."""
        out = self._run("--auto-detect")
        lines = [l for l in out.splitlines()[2:] if l.strip()]
        if not lines:
            raise CameraError("no camera detected on USB")
        return lines[0].rsplit("usb", 1)[0].strip()

    def capture(self, filename: str) -> str:
        """Capture a frame and download it to save_dir/filename."""
        path = os.path.join(self.save_dir, filename)
        self._run(
            "--capture-image-and-download",
            "--filename", path,
            "--force-overwrite",
            timeout=120.0,
        )
        return path

    def get_config(self, name: str) -> str:
        out = self._run("--get-config", name)
        for line in out.splitlines():
            if line.startswith("Current:"):
                return line.split(":", 1)[1].strip()
        return ""

    def set_config(self, name: str, value: str) -> None:
        self._run("--set-config", f"{name}={value}")


class HardwareTrigger:
    """Fire the camera through the FPGA peripheral's cable-release pins."""

    def __init__(self, pl_stepper, post_delay_s: float = 1.0):
        self.stepper = pl_stepper
        self.post_delay_s = post_delay_s

    def detect(self) -> str:
        return "hardware cable release (images stored on camera card)"

    def capture(self, filename: str) -> str:
        self.stepper.trigger_camera(wait=True)
        # Give the camera time to write the frame before the next move.
        time.sleep(self.post_delay_s)
        return filename

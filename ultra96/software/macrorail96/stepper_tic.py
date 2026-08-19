"""Pololu TIC driver over USB, mirroring the Windows app's TicDotNet use.

This is the zero-FPGA path: plug the same Pololu TIC you already use
with the Windows application into the Ultra96-V2's USB port and the
board becomes the rail controller with no bitstream required.

Requires the ``ticlib`` package (pip install ticlib pyusb) and udev
permission for the TIC (or run as root). The TIC keeps its own settings
(current limit, accel, decel, limits) exactly as configured in the
Pololu TIC Control Center, so an existing MacroRail rig works as-is.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import time

try:
    from ticlib import TicUSB
except ImportError:  # pragma: no cover - optional dependency
    TicUSB = None


class TicStepper:
    """MacroRail stepper axis using a Pololu TIC over USB."""

    def __init__(self, steps_per_mm: float = 800.0):
        if TicUSB is None:
            raise RuntimeError("ticlib is not installed (pip install ticlib pyusb)")
        self.steps_per_mm = steps_per_mm
        self.tic = TicUSB()
        self.tic.halt_and_set_position(self.tic.get_current_position())
        self._offset = self.tic.get_current_position()

    def close(self) -> None:
        self.tic.enter_safe_start()
        self.tic.deenergize()

    # ------------------------------------------------------------------
    # Configuration
    # ------------------------------------------------------------------
    def set_speed(self, max_steps_per_sec: float, **_ignored) -> None:
        # TIC speed unit is steps/10000s.
        self.tic.set_max_speed(int(max_steps_per_sec * 10_000))

    def set_trigger(self, settle_ms: float = 500.0, shutter_ms: float = 100.0) -> None:
        # Camera triggering is handled in software (camera.py) on this path.
        self.settle_ms = settle_ms
        self.shutter_ms = shutter_ms

    def enable_motor(self, enable: bool = True) -> None:
        if enable:
            self.tic.energize()
            self.tic.exit_safe_start()
        else:
            self.tic.enter_safe_start()
            self.tic.deenergize()

    # ------------------------------------------------------------------
    # Motion
    # ------------------------------------------------------------------
    def move_steps(self, steps: int, trigger: bool = False, wait: bool = True) -> None:
        target = self.tic.get_current_position() + steps
        self.tic.exit_safe_start()
        self.tic.set_target_position(target)
        if wait:
            self.wait_idle()

    def move_mm(self, mm: float, trigger: bool = False, wait: bool = True) -> None:
        self.move_steps(int(round(mm * self.steps_per_mm)), trigger, wait)

    def abort(self) -> None:
        self.tic.halt_and_hold()

    def zero_position(self) -> None:
        self.tic.halt_and_set_position(0)
        self._offset = 0

    def wait_idle(self, timeout_s: float = 300.0) -> None:
        deadline = time.monotonic() + timeout_s
        while True:
            # Keep the command timeout from tripping during long moves.
            self.tic.reset_command_timeout()
            current = self.tic.get_current_position()
            target = self.tic.get_target_position()
            if current == target:
                return
            if time.monotonic() > deadline:
                self.abort()
                raise TimeoutError("TIC move timed out")
            time.sleep(0.05)

    # ------------------------------------------------------------------
    # Status
    # ------------------------------------------------------------------
    @property
    def position_steps(self) -> int:
        return self.tic.get_current_position()

    @property
    def position_mm(self) -> float:
        return self.position_steps / self.steps_per_mm

    def status(self) -> dict:
        return {
            "busy": self.tic.get_current_position() != self.tic.get_target_position(),
            "done": True,
            "limit_fwd": False,
            "limit_rev": False,
            "halted_on_limit": False,
            "position_steps": self.position_steps,
            "position_mm": self.position_mm,
        }

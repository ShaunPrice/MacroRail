"""Driver for the FPGA stepper/trigger peripheral via /dev/mem MMIO.

Works on the stock PYNQ v3 image and on plain PetaLinux (no pynq package
required). Run as root, or give the user access to /dev/mem.

The peripheral generates hardware-timed STEP/DIR pulses with a
trapezoidal ramp, honours the limit switches in hardware, and can fire
the camera's focus/shutter cable-release lines after a settle delay,
so shot timing is deterministic to 10 ns regardless of Linux load.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import mmap
import os
import struct
import time

from . import registers as R


class PLStepper:
    """MacroRail stepper axis implemented in the Ultra96-V2 fabric."""

    def __init__(self, base_addr: int = R.BASE_ADDR, steps_per_mm: float = 800.0):
        self.steps_per_mm = steps_per_mm
        self._fd = os.open("/dev/mem", os.O_RDWR | os.O_SYNC)
        self._mem = mmap.mmap(
            self._fd,
            R.SPAN,
            mmap.MAP_SHARED,
            mmap.PROT_READ | mmap.PROT_WRITE,
            offset=base_addr,
        )
        ident = self._read(R.ID)
        if ident != R.ID_VALUE:
            raise RuntimeError(
                f"MacroRail peripheral not found at 0x{base_addr:08X} "
                f"(ID read 0x{ident:08X}, expected 0x{R.ID_VALUE:08X}). "
                "Is the bitstream loaded?"
            )
        self._config = R.CFG_LIMIT_POLARITY  # active-high limits by default

    def close(self) -> None:
        self._mem.close()
        os.close(self._fd)

    # ------------------------------------------------------------------
    # Raw register access
    # ------------------------------------------------------------------
    def _read(self, offset: int) -> int:
        return struct.unpack_from("<I", self._mem, offset)[0]

    def _write(self, offset: int, value: int) -> None:
        struct.pack_into("<I", self._mem, offset, value & 0xFFFF_FFFF)

    # ------------------------------------------------------------------
    # Configuration
    # ------------------------------------------------------------------
    def set_speed(
        self,
        max_steps_per_sec: float,
        start_steps_per_sec: float = None,
        accel_steps: int = 200,
        step_width_us: float = 5.0,
    ) -> None:
        """Program the speed ramp.

        accel_steps is roughly how many steps the ramp takes to go from
        the start speed to full speed.
        """
        if start_steps_per_sec is None:
            start_steps_per_sec = max(max_steps_per_sec / 4.0, 1.0)
        start_period = int(R.CLOCK_HZ / start_steps_per_sec)
        min_period = int(R.CLOCK_HZ / max_steps_per_sec)
        accel = max(1, (start_period - min_period) // max(1, accel_steps))
        self._write(R.START_PERIOD, min(start_period, 0xFF_FFFF))
        self._write(R.MIN_PERIOD, min(min_period, 0xFF_FFFF))
        self._write(R.ACCEL, min(accel, 0xFFFF))
        self._write(R.STEP_WIDTH, int(step_width_us * R.CLOCK_HZ / 1_000_000))

    def set_trigger(self, settle_ms: float = 500.0, shutter_ms: float = 100.0) -> None:
        """Settle delay after motion and shutter pulse width."""
        self._write(R.SETTLE, int(settle_ms * R.CLOCK_HZ / 1000))
        self._write(R.TRIG_WIDTH, int(shutter_ms * R.CLOCK_HZ / 1000))

    def enable_motor(self, enable: bool = True) -> None:
        if enable:
            self._config |= R.CFG_MOTOR_EN
        else:
            self._config &= ~R.CFG_MOTOR_EN
        self._write(R.CONFIG, self._config)

    def set_limit_polarity(self, active_high: bool) -> None:
        if active_high:
            self._config |= R.CFG_LIMIT_POLARITY
        else:
            self._config &= ~R.CFG_LIMIT_POLARITY
        self._write(R.CONFIG, self._config)

    # ------------------------------------------------------------------
    # Motion
    # ------------------------------------------------------------------
    def move_steps(self, steps: int, trigger: bool = False, wait: bool = True) -> None:
        """Relative move; positive steps are forward."""
        if steps >= 0:
            self._config |= R.CFG_DIR
        else:
            self._config &= ~R.CFG_DIR
        if trigger:
            self._config |= R.CFG_TRIGGER_EN
        else:
            self._config &= ~R.CFG_TRIGGER_EN
        self._write(R.CONFIG, self._config)
        self._write(R.STEPS, abs(steps))
        self._write(R.CTRL, R.CTRL_START)
        if wait:
            self.wait_idle()

    def move_mm(self, mm: float, trigger: bool = False, wait: bool = True) -> None:
        self.move_steps(int(round(mm * self.steps_per_mm)), trigger, wait)

    def trigger_camera(self, wait: bool = True) -> None:
        """Fire the focus/shutter outputs without moving."""
        self._write(R.CTRL, R.CTRL_TRIG_NOW)
        if wait:
            self.wait_idle()

    def abort(self) -> None:
        self._write(R.CTRL, R.CTRL_ABORT)

    def zero_position(self) -> None:
        self._write(R.CTRL, R.CTRL_ZERO_POS)

    def wait_idle(self, timeout_s: float = 300.0) -> None:
        deadline = time.monotonic() + timeout_s
        while self._read(R.STATUS) & R.ST_BUSY:
            if time.monotonic() > deadline:
                self.abort()
                raise TimeoutError("stepper move timed out")
            time.sleep(0.001)

    # ------------------------------------------------------------------
    # Status
    # ------------------------------------------------------------------
    @property
    def position_steps(self) -> int:
        raw = self._read(R.POSITION)
        return raw - 0x1_0000_0000 if raw & 0x8000_0000 else raw

    @property
    def position_mm(self) -> float:
        return self.position_steps / self.steps_per_mm

    def status(self) -> dict:
        st = self._read(R.STATUS)
        return {
            "busy": bool(st & R.ST_BUSY),
            "done": bool(st & R.ST_DONE),
            "limit_fwd": bool(st & R.ST_LIMIT_FWD),
            "limit_rev": bool(st & R.ST_LIMIT_REV),
            "halted_on_limit": bool(st & R.ST_HALTED_ON_LIMIT),
            "position_steps": self.position_steps,
            "position_mm": self.position_mm,
        }

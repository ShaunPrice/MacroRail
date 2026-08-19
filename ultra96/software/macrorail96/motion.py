"""Driver for the two-axis FPGA motion controller with hardware safety.

Talks to ultra96/fpga/rtl/axi_motion.v over /dev/mem. Works on the stock
PYNQ image and on plain PetaLinux; no pynq package required.

The API is arranged so the safe thing is the easy thing:

    bot = MotionController(geometry=DiffDriveGeometry(...))
    bot.configure(max_wheel_speed_mps=0.6, watchdog_ms=300)
    bot.lock_safety_envelope()      # <- from here the ceiling is immutable
    bot.enable()
    bot.set_body_velocity(linear=0.3, angular=0.0)   # must be repeated!

`set_body_velocity` pets the hardware watchdog. If this process stops
calling it - because it crashed, blocked on I/O, was OOM-killed, or lost
the network - the fabric stops the motors on its own. Nothing in this file
is required for that to happen, which is the entire point.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
import mmap
import os
import struct
from dataclasses import dataclass

# ----------------------------------------------------------------------
# Register map - must match ultra96/fpga/rtl/axi_motion.v
# ----------------------------------------------------------------------
BASE_ADDR = 0xA001_0000
SPAN = 0x1000
CLOCK_HZ = 100_000_000

G_CTRL, G_STATUS, G_WDOG = 0x00, 0x04, 0x08
G_SAMPLE, G_PWMPER, G_DEAD, G_SPLIM, G_ID = 0x0C, 0x10, 0x14, 0x18, 0x1C

AXIS_BASE = (0x40, 0x80)
A_SP, A_KP, A_KI, A_KD = 0x00, 0x04, 0x08, 0x0C
A_IMAX, A_OMAX, A_CTRL = 0x10, 0x14, 0x18
A_POS, A_VEL, A_ERR, A_DUTY, A_STAT = 0x1C, 0x20, 0x24, 0x28, 0x2C

GC_ENABLE, GC_ESTOP_CLEAR, GC_LOCK = 1, 2, 4

ST_ESTOP, ST_WATCHDOG, ST_LOCKED, ST_ESTOP_RAW, ST_PERMITTED = 1, 2, 4, 8, 16

AST_SATURATED, AST_ENC_ERROR, AST_INDEX, AST_CLAMPED = 1, 2, 4, 8
AST_LIMIT_FWD, AST_LIMIT_REV = 16, 32

ID_VALUE = 0x4D52_3200

# Fixed-point formats used by the fabric (see axi_motion.v).
VEL_Q = 256        # velocity / velocity setpoints are Q24.8 counts per period
GAIN_Q = 65536     # PID gains are Q16.16


class SafetyError(RuntimeError):
    """Raised when the hardware refuses a command, or is inhibiting motion."""


@dataclass
class DiffDriveGeometry:
    """Physical constants of the robot. Measure these; do not guess.

    wheel_radius_m   : rolling radius under load, not the moulded diameter
    wheel_base_m     : distance between the two wheel contact patches
    counts_per_rev   : encoder CPR x 4 x gearbox ratio, at the wheel
    """

    wheel_radius_m: float
    wheel_base_m: float
    counts_per_rev: float

    def mps_to_counts_per_s(self, mps: float) -> float:
        circumference = 2.0 * math.pi * self.wheel_radius_m
        return (mps / circumference) * self.counts_per_rev

    def counts_per_s_to_mps(self, counts: float) -> float:
        circumference = 2.0 * math.pi * self.wheel_radius_m
        return (counts / self.counts_per_rev) * circumference


class MotionController:
    def __init__(
        self,
        geometry: DiffDriveGeometry,
        base_addr: int = BASE_ADDR,
        control_rate_hz: float = 1000.0,
    ):
        self.geom = geometry
        self.control_rate_hz = control_rate_hz
        self._fd = os.open("/dev/mem", os.O_RDWR | os.O_SYNC)
        self._mem = mmap.mmap(
            self._fd, SPAN, mmap.MAP_SHARED,
            mmap.PROT_READ | mmap.PROT_WRITE, offset=base_addr,
        )
        ident = self._rd(G_ID)
        if ident != ID_VALUE:
            raise RuntimeError(
                f"motion controller not found at 0x{base_addr:08X} "
                f"(ID 0x{ident:08X}, expected 0x{ID_VALUE:08X}). "
                "Is the robot bitstream loaded?"
            )
        self._enabled = False

    def close(self) -> None:
        try:
            self.disable()
        finally:
            self._mem.close()
            os.close(self._fd)

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.close()

    # ------------------------------------------------------------------
    # Raw access
    # ------------------------------------------------------------------
    def _rd(self, off: int) -> int:
        return struct.unpack_from("<I", self._mem, off)[0]

    def _rd_signed(self, off: int) -> int:
        raw = self._rd(off)
        return raw - 0x1_0000_0000 if raw & 0x8000_0000 else raw

    def _wr(self, off: int, val: int) -> None:
        struct.pack_into("<I", self._mem, off, val & 0xFFFF_FFFF)

    def _axis(self, axis: int, reg: int) -> int:
        return AXIS_BASE[axis] + reg

    # ------------------------------------------------------------------
    # Configuration
    # ------------------------------------------------------------------
    def configure(
        self,
        max_wheel_speed_mps: float,
        watchdog_ms: float = 300.0,
        pwm_khz: float = 20.0,
        deadtime_ns: float = 500.0,
        kp: float = 0.2,
        ki: float = 0.015,
        kd: float = 0.004,
    ) -> None:
        """Set the control loop and the safety envelope.

        Call before lock_safety_envelope(); afterwards the lockable fields
        (watchdog, dead-time, speed ceiling) are rejected by the hardware.

        The default gains are a starting point for a small geared rover,
        not a tune. Gains map Q24.8 velocity error to PWM compare counts,
        and they are only meaningful at the control rate they were tuned
        at - record both together. Tune Kp first (raise until it responds
        briskly and just starts to oscillate, then halve), add Kd to damp
        overshoot, and add Ki last, only to remove steady-state error.
        """
        if self.locked:
            raise SafetyError(
                "safety envelope is locked; reset the PL to reconfigure"
            )

        self._wr(G_SAMPLE, int(CLOCK_HZ / self.control_rate_hz))
        self._wr(G_PWMPER, int(CLOCK_HZ / (pwm_khz * 1000)))
        self._wr(G_DEAD, int(deadtime_ns * CLOCK_HZ / 1e9))
        self._wr(G_WDOG, int(watchdog_ms * CLOCK_HZ / 1000))

        # Speed ceiling, expressed in the fabric's units (counts per
        # control period).
        ceiling = self.geom.mps_to_counts_per_s(max_wheel_speed_mps)
        self._wr(G_SPLIM, max(1, int(ceiling / self.control_rate_hz * VEL_Q)))

        pwm_full = int(CLOCK_HZ / (pwm_khz * 1000))
        for axis in (0, 1):
            self._wr(self._axis(axis, A_KP), int(kp * GAIN_Q))
            self._wr(self._axis(axis, A_KI), int(ki * GAIN_Q))
            self._wr(self._axis(axis, A_KD), int(kd * GAIN_Q))
            self._wr(self._axis(axis, A_IMAX), 20_000_000)
            self._wr(self._axis(axis, A_OMAX), pwm_full)

    def lock_safety_envelope(self) -> None:
        """Freeze the speed ceiling, watchdog and dead-time until PL reset.

        Do this at the end of boot, before any autonomy or model-driven
        code starts. After this call, no software on this board - however
        buggy or however compromised - can raise its own limits.
        """
        self._wr(G_CTRL, (GC_ENABLE if self._enabled else 0) | GC_LOCK)
        if not self.locked:
            raise SafetyError("hardware did not accept the lock request")

    # ------------------------------------------------------------------
    # Arming
    # ------------------------------------------------------------------
    def enable(self) -> None:
        self._enabled = True
        self._wr(G_CTRL, GC_ENABLE)

    def disable(self) -> None:
        self._enabled = False
        self._wr(G_CTRL, 0)

    def clear_estop(self) -> None:
        """Re-arm after an E-stop. Fails while the button is still pressed."""
        self._wr(G_CTRL, (GC_ENABLE if self._enabled else 0) | GC_ESTOP_CLEAR)
        if self._rd(G_STATUS) & ST_ESTOP:
            raise SafetyError("E-stop still latched - is the button released?")

    # ------------------------------------------------------------------
    # Commanding
    # ------------------------------------------------------------------
    def set_wheel_speeds(self, left_mps: float, right_mps: float) -> None:
        """Command both wheels. Also pets the hardware watchdog."""
        for axis, mps in ((0, left_mps), (1, right_mps)):
            counts_per_period = (
                self.geom.mps_to_counts_per_s(mps) / self.control_rate_hz
            )
            self._wr(
                self._axis(axis, A_SP),
                int(round(counts_per_period * VEL_Q)),
            )

    def set_body_velocity(self, linear: float, angular: float) -> None:
        """Differential-drive kinematics: m/s forward, rad/s yaw."""
        half_base = self.geom.wheel_base_m / 2.0
        self.set_wheel_speeds(
            left_mps=linear - angular * half_base,
            right_mps=linear + angular * half_base,
        )

    def stop(self) -> None:
        self.set_wheel_speeds(0.0, 0.0)

    # ------------------------------------------------------------------
    # Feedback
    # ------------------------------------------------------------------
    def wheel_positions_m(self):
        """Distance travelled by each wheel since the last zero, in metres."""
        return tuple(
            self.geom.counts_per_s_to_mps(
                self._rd_signed(self._axis(a, A_POS))
            )
            for a in (0, 1)
        )

    def wheel_speeds_mps(self):
        return tuple(
            self.geom.counts_per_s_to_mps(
                self._rd_signed(self._axis(a, A_VEL))
                / VEL_Q
                * self.control_rate_hz
            )
            for a in (0, 1)
        )

    @property
    def locked(self) -> bool:
        return bool(self._rd(G_STATUS) & ST_LOCKED)

    @property
    def motion_permitted(self) -> bool:
        return bool(self._rd(G_STATUS) & ST_PERMITTED)

    def status(self) -> dict:
        st = self._rd(G_STATUS)
        axes = []
        for a in (0, 1):
            ast = self._rd(self._axis(a, A_STAT))
            axes.append(
                {
                    "position_counts": self._rd_signed(self._axis(a, A_POS)),
                    "velocity_counts_per_period":
                        self._rd_signed(self._axis(a, A_VEL)) / VEL_Q,
                    "duty": self._rd(self._axis(a, A_DUTY)),
                    "saturated": bool(ast & AST_SATURATED),
                    "encoder_error": bool(ast & AST_ENC_ERROR),
                    "setpoint_clamped": bool(ast & AST_CLAMPED),
                    "limit_fwd": bool(ast & AST_LIMIT_FWD),
                    "limit_rev": bool(ast & AST_LIMIT_REV),
                }
            )
        return {
            "estop_latched": bool(st & ST_ESTOP),
            "estop_button_pressed": bool(st & ST_ESTOP_RAW),
            "watchdog_tripped": bool(st & ST_WATCHDOG),
            "envelope_locked": bool(st & ST_LOCKED),
            "motion_permitted": bool(st & ST_PERMITTED),
            "axes": axes,
        }

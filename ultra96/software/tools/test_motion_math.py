"""Hardware-free tests for the motion driver's units and kinematics.

Wrong unit conversions are the most common cause of a robot that drives
in curves when told to go straight, or that ignores its speed limit
because the ceiling was computed in the wrong units. None of that needs a
board to catch, so it is checked here.

Run:  python3 ultra96/software/tools/test_motion_math.py

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from macrorail96 import motion as M  # noqa: E402

FAILURES = []


def check(name, got, expected, tol=1e-9):
    if abs(got - expected) > tol:
        FAILURES.append(f"FAIL: {name}: got {got!r}, expected {expected!r}")
        print(FAILURES[-1])
    else:
        print(f"PASS: {name} = {got!r}")


def check_eq(name, got, expected):
    if got != expected:
        FAILURES.append(f"FAIL: {name}: got {got!r}, expected {expected!r}")
        print(FAILURES[-1])
    else:
        print(f"PASS: {name} = {got!r}")


class FakeController(M.MotionController):
    """MotionController with the MMIO layer replaced by a dict."""

    def __init__(self, geometry, control_rate_hz=1000.0):
        self.geom = geometry
        self.control_rate_hz = control_rate_hz
        self.regs = {M.G_ID: M.ID_VALUE, M.G_STATUS: 0}
        self._enabled = False

    def _rd(self, off):
        return self.regs.get(off, 0)

    def _wr(self, off, val):
        self.regs[off] = val & 0xFFFF_FFFF


def main():
    # A plausible small rover: 65 mm wheels, 200 mm track, 1000 CPR at the
    # wheel after gearing and x4 decoding.
    geom = M.DiffDriveGeometry(
        wheel_radius_m=0.0325,
        wheel_base_m=0.20,
        counts_per_rev=1000.0,
    )

    # --- Unit conversions round-trip -------------------------------------
    circumference = 2 * math.pi * 0.0325
    # One wheel revolution per second == circumference metres per second.
    check(
        "one rev/s converts to counts",
        geom.mps_to_counts_per_s(circumference),
        1000.0,
        tol=1e-6,
    )
    check(
        "counts convert back to m/s",
        geom.counts_per_s_to_mps(1000.0),
        circumference,
        tol=1e-9,
    )
    for v in (0.0, 0.25, -0.4, 1.5):
        check(
            f"round-trip {v} m/s",
            geom.counts_per_s_to_mps(geom.mps_to_counts_per_s(v)),
            v,
            tol=1e-9,
        )

    ctl = FakeController(geom, control_rate_hz=1000.0)

    # --- Straight-line driving: both wheels equal ------------------------
    ctl.set_body_velocity(linear=0.3, angular=0.0)
    left = ctl.regs[M.AXIS_BASE[0] + M.A_SP]
    right = ctl.regs[M.AXIS_BASE[1] + M.A_SP]
    check_eq("straight line drives both wheels equally", left, right)

    expected_counts = round(geom.mps_to_counts_per_s(0.3) / 1000.0 * M.VEL_Q)
    check_eq("straight-line setpoint magnitude", left, expected_counts)

    # --- Spin in place: equal and opposite -------------------------------
    ctl.set_body_velocity(linear=0.0, angular=1.0)
    left = ctl._rd_signed_raw = ctl.regs[M.AXIS_BASE[0] + M.A_SP]
    right = ctl.regs[M.AXIS_BASE[1] + M.A_SP]
    left_signed = left - 0x1_0000_0000 if left & 0x8000_0000 else left
    right_signed = right - 0x1_0000_0000 if right & 0x8000_0000 else right
    check_eq(
        "spin in place is equal and opposite",
        left_signed,
        -right_signed,
    )
    if right_signed <= 0:
        FAILURES.append("FAIL: positive yaw should drive the right wheel forward")
        print(FAILURES[-1])
    else:
        print(f"PASS: positive yaw drives right wheel forward = {right_signed}")

    # A +1 rad/s yaw means each wheel runs at half the wheel base, i.e.
    # 0.1 m/s in opposite directions.
    check_eq(
        "yaw wheel speed matches kinematics",
        right_signed,
        round(geom.mps_to_counts_per_s(0.1) / 1000.0 * M.VEL_Q),
    )

    # --- Speed ceiling is computed in fabric units -----------------------
    ctl.configure(max_wheel_speed_mps=0.5, watchdog_ms=250.0)
    ceiling = ctl.regs[M.G_SPLIM]
    expected_ceiling = int(geom.mps_to_counts_per_s(0.5) / 1000.0 * M.VEL_Q)
    check_eq("speed ceiling in counts per control period", ceiling, expected_ceiling)

    # A command at the ceiling must not exceed it; one above must.
    ctl.set_wheel_speeds(0.5, 0.5)
    at_limit = ctl.regs[M.AXIS_BASE[0] + M.A_SP]
    if at_limit > ceiling + 1:
        FAILURES.append(
            f"FAIL: command at ceiling ({at_limit}) exceeds limit ({ceiling})"
        )
        print(FAILURES[-1])
    else:
        print(f"PASS: command at ceiling ({at_limit}) within limit ({ceiling})")

    # --- Watchdog and PWM registers in clock cycles ----------------------
    check_eq(
        "watchdog timeout in clk cycles",
        ctl.regs[M.G_WDOG],
        int(250.0 * M.CLOCK_HZ / 1000),
    )
    check_eq(
        "control loop divider",
        ctl.regs[M.G_SAMPLE],
        int(M.CLOCK_HZ / 1000.0),
    )
    check_eq(
        "20 kHz PWM period",
        ctl.regs[M.G_PWMPER],
        int(M.CLOCK_HZ / 20000),
    )

    # --- configure() must refuse to run once the envelope is locked ------
    ctl.regs[M.G_STATUS] = M.ST_LOCKED
    try:
        ctl.configure(max_wheel_speed_mps=99.0)
        FAILURES.append("FAIL: configure() succeeded despite a locked envelope")
        print(FAILURES[-1])
    except M.SafetyError:
        print("PASS: configure() refused while the envelope is locked")

    print()
    if FAILURES:
        print(f"{len(FAILURES)} TEST(S) FAILED")
        return 1
    print("ALL MOTION MATH TESTS PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())

"""Tests for differential-drive dead reckoning against closed-form answers.

Each case has a trajectory whose exact endpoint is known analytically, so
a sign error or a wheel-base slip shows up as a numeric mismatch rather
than as a robot that mysteriously drifts three rooms away.

Run:  python3 ultra96/software/tools/test_odometry.py

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from macrorail96.odometry import DeadReckoner, normalize_angle  # noqa: E402

FAILURES = []


def check_angle(name, got, expected, tol=1e-6):
    """Compare angles modulo 2*pi: +pi and -pi are the same heading."""
    diff = abs(math.atan2(math.sin(got - expected), math.cos(got - expected)))
    if diff > tol:
        FAILURES.append(f"FAIL: {name}: got {got:.6f}, expected {expected:.6f}")
        print(FAILURES[-1])
    else:
        print(f"PASS: {name} = {got:.6f}")


def check(name, got, expected, tol=1e-6):
    if abs(got - expected) > tol:
        FAILURES.append(f"FAIL: {name}: got {got:.6f}, expected {expected:.6f}")
        print(FAILURES[-1])
    else:
        print(f"PASS: {name} = {got:.6f}")


def main():
    WB = 0.20   # wheel base

    # --- 1. Straight line -------------------------------------------------
    dr = DeadReckoner(WB)
    for _ in range(1000):
        dr.update(0.001, 0.001)
    check("straight line: x", dr.pose.x, 1.0)
    check("straight line: y", dr.pose.y, 0.0)
    check("straight line: yaw", dr.pose.yaw, 0.0)

    # --- 2. Pure rotation in place ---------------------------------------
    # Each wheel travels +/- (WB/2)*theta for a rotation of theta.
    dr = DeadReckoner(WB)
    quarter = math.pi / 2
    arc = (WB / 2.0) * quarter
    steps = 1000
    for _ in range(steps):
        dr.update(-arc / steps, arc / steps)
    check("spin in place: x", dr.pose.x, 0.0)
    check("spin in place: y", dr.pose.y, 0.0)
    check("spin in place: yaw is +90 deg", dr.pose.yaw, quarter)

    # --- 3. Constant-radius arc: quarter circle of radius R --------------
    # Inner wheel travels (R - WB/2)*theta, outer (R + WB/2)*theta.
    # Starting at the origin facing +x, a left turn of 90 degrees about
    # centre (0, R) ends at (R, R) facing +y.
    R = 0.5
    dr = DeadReckoner(WB)
    inner = (R - WB / 2.0) * quarter
    outer = (R + WB / 2.0) * quarter
    steps = 20000
    for _ in range(steps):
        dr.update(inner / steps, outer / steps)
    check("quarter arc: x", dr.pose.x, R, tol=1e-4)
    check("quarter arc: y", dr.pose.y, R, tol=1e-4)
    check("quarter arc: yaw", dr.pose.yaw, quarter, tol=1e-6)

    # --- 4. A closed loop must return to the origin -----------------------
    # Drive a square: four (straight leg + 90 degree turn) pairs.
    dr = DeadReckoner(WB)
    leg_steps, turn_steps = 500, 500
    for _ in range(4):
        for _ in range(leg_steps):
            dr.update(0.001, 0.001)               # 0.5 m leg
        for _ in range(turn_steps):
            dr.update(-arc / turn_steps, arc / turn_steps)
    check("closed square: back to x=0", dr.pose.x, 0.0, tol=1e-6)
    check("closed square: back to y=0", dr.pose.y, 0.0, tol=1e-6)
    check("closed square: yaw wrapped to 0", dr.pose.yaw, 0.0, tol=1e-6)

    # --- 5. Reversing undoes forward travel exactly -----------------------
    dr = DeadReckoner(WB)
    dr.update(0.3, 0.1)
    dr.update(-0.3, -0.1)
    check("reverse undoes forward: x", dr.pose.x, 0.0, tol=1e-9)
    check("reverse undoes forward: y", dr.pose.y, 0.0, tol=1e-9)
    check("reverse undoes forward: yaw", dr.pose.yaw, 0.0, tol=1e-9)

    # --- 6. Angle wrapping stays in (-pi, pi] -----------------------------
    dr = DeadReckoner(WB)
    for _ in range(4000):
        dr.update(-arc / 1000, arc / 1000)     # 4 full-ish revolutions
    if not (-math.pi - 1e-9 < dr.pose.yaw <= math.pi + 1e-9):
        FAILURES.append(f"FAIL: yaw not wrapped: {dr.pose.yaw}")
        print(FAILURES[-1])
    else:
        print(f"PASS: yaw stays wrapped = {dr.pose.yaw:.6f}")

    check_angle("normalize_angle(3pi)", normalize_angle(3 * math.pi), math.pi)
    check_angle("normalize_angle(-3pi)", normalize_angle(-3 * math.pi), math.pi)

    # --- 7. Body velocity conversion --------------------------------------
    dr = DeadReckoner(WB)
    linear, angular = dr.body_velocity(0.2, 0.2)
    check("straight: linear", linear, 0.2)
    check("straight: angular", angular, 0.0)
    linear, angular = dr.body_velocity(-0.1, 0.1)
    check("spin: linear", linear, 0.0)
    check("spin: angular", angular, 0.2 / WB)

    # --- 8. Degenerate geometry is rejected -------------------------------
    try:
        DeadReckoner(0.0)
        FAILURES.append("FAIL: zero wheel base was accepted")
        print(FAILURES[-1])
    except ValueError:
        print("PASS: zero wheel base rejected")

    print()
    if FAILURES:
        print(f"{len(FAILURES)} TEST(S) FAILED")
        return 1
    print("ALL ODOMETRY TESTS PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())

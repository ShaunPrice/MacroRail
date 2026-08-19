"""Tests for the safety gate and the detection geometry.

The gate is the layer that decides whether a model's output is allowed to
move the robot, so it gets adversarial cases rather than happy paths:
NaN, infinity, absurd magnitudes, stale perception, flat batteries. The
detection geometry gets round-trip checks, because a letterbox mapping
error aims the robot at the wrong place while looking entirely plausible.

Run:  python3 ultra96/software/tools/test_autonomy.py

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import numpy as np  # noqa: E402

from macrorail96.autonomy import (  # noqa: E402
    FollowBehaviour,
    GateLimits,
    RobotState,
    SafetyGate,
)
from macrorail96.vision import (  # noqa: E402
    Detection,
    iou,
    letterbox,
    non_max_suppression,
    unletterbox_box,
)

FAILURES = []


def ok(name, condition, detail=""):
    if condition:
        print(f"PASS: {name}" + (f" ({detail})" if detail else ""))
    else:
        FAILURES.append(f"FAIL: {name} {detail}")
        print(FAILURES[-1])


def close(name, got, expected, tol=1e-6):
    ok(name, abs(got - expected) <= tol, f"got {got!r}, expected {expected!r}")


def healthy(t=10.0):
    """A state in which a sane command should pass untouched."""
    return RobotState(now_s=t, battery_v=12.4, last_perception_s=t)


# ----------------------------------------------------------------------
def test_gate():
    print("\n--- SafetyGate ---")
    lim = GateLimits(max_linear_mps=0.5, max_angular_rps=1.5)

    # A reasonable command passes through unchanged.
    gate = SafetyGate(lim)
    d = gate.evaluate(0.3, 0.4, healthy())
    close("sane command passes: linear", d.linear, 0.3)
    close("sane command passes: angular", d.angular, 0.4)
    ok("sane command unmodified", not d.modified)

    # NaN and infinity must be rejected, not silently clamped. A naive
    # min/max clamp lets both straight through.
    for bad in (float("nan"), float("inf"), float("-inf")):
        gate = SafetyGate(lim)
        d = gate.evaluate(bad, 0.0, healthy())
        ok(f"rejects {bad}", d.linear == 0.0 and d.angular == 0.0)
    gate = SafetyGate(lim)
    d = gate.evaluate(0.0, float("nan"), healthy())
    ok("rejects NaN angular", d.angular == 0.0)

    # Absurd magnitudes get clamped to the envelope.
    gate = SafetyGate(lim)
    d = gate.evaluate(99.0, -99.0, healthy())
    close("clamps huge linear", d.linear, 0.5)
    close("clamps huge angular", d.angular, -1.5)

    # E-stop, flat battery and stale perception are hard stops.
    gate = SafetyGate(lim)
    st = healthy()
    st.estop_latched = True
    d = gate.evaluate(0.4, 0.4, st)
    ok("E-stop zeroes command", d.linear == 0.0 and d.angular == 0.0)

    gate = SafetyGate(lim)
    st = healthy()
    st.battery_v = 9.0
    d = gate.evaluate(0.4, 0.4, st)
    ok("flat battery zeroes command", d.linear == 0.0 and d.angular == 0.0)

    gate = SafetyGate(lim)
    st = RobotState(now_s=10.0, battery_v=12.4, last_perception_s=5.0)
    d = gate.evaluate(0.4, 0.4, st)
    ok("stale perception zeroes command", d.linear == 0.0)
    ok("stale perception is explained", any("stale" in r for r in d.reasons))

    # An obstacle blocks forward motion but must leave an escape route.
    gate = SafetyGate(lim)
    st = healthy()
    st.obstacle_ahead = True
    d = gate.evaluate(0.4, 0.0, st)
    close("obstacle blocks forward", d.linear, 0.0)

    gate = SafetyGate(lim)
    st = healthy()
    st.obstacle_ahead = True
    d = gate.evaluate(-0.2, 0.0, st)
    ok("obstacle still allows reversing", d.linear < 0.0, f"got {d.linear}")

    gate = SafetyGate(lim)
    st = healthy()
    st.obstacle_ahead = True
    d = gate.evaluate(0.0, 1.0, st)
    ok("obstacle still allows turning", d.angular > 0.0, f"got {d.angular}")

    # Acceleration limiting: a step command must be ramped.
    gate = SafetyGate(GateLimits(max_linear_accel_mps2=1.0, max_linear_mps=1.0))
    gate.evaluate(0.0, 0.0, healthy(t=0.0))
    d = gate.evaluate(1.0, 0.0, healthy(t=0.1))     # 100 ms later
    close("accel limited to 1.0 m/s^2 over 0.1 s", d.linear, 0.1, tol=1e-9)
    d = gate.evaluate(1.0, 0.0, healthy(t=0.2))
    close("accel continues to ramp", d.linear, 0.2, tol=1e-9)

    # Deceleration is rate-limited in the same way...
    gate = SafetyGate(GateLimits(max_linear_accel_mps2=1.0, max_linear_mps=1.0))
    gate.evaluate(0.5, 0.0, healthy(t=0.0))
    d = gate.evaluate(0.0, 0.0, healthy(t=0.1))
    close("decel is rate limited too", d.linear, 0.4, tol=1e-9)

    # ...but a hard stop must NOT be, or an E-stop would ramp down.
    gate = SafetyGate(GateLimits(max_linear_accel_mps2=1.0, max_linear_mps=1.0))
    gate.evaluate(1.0, 0.0, healthy(t=0.0))
    st = healthy(t=0.01)
    st.estop_latched = True
    d = gate.evaluate(1.0, 0.0, st)
    ok("E-stop bypasses accel limiting", d.linear == 0.0, f"got {d.linear}")

    # Every modification must be explained, for postmortems.
    gate = SafetyGate(lim)
    d = gate.evaluate(99.0, 0.0, healthy())
    ok("clamping is explained", len(d.reasons) > 0, str(d.reasons))


# ----------------------------------------------------------------------
def test_vision_geometry():
    print("\n--- Detection geometry ---")

    # Letterbox a 640x360 frame into 416x416 and map a box back.
    frame = np.zeros((360, 640, 3), dtype=np.uint8)
    padded, scale, pad_x, pad_y = letterbox(frame, 416)
    ok("letterbox output is square", padded.shape[:2] == (416, 416),
       str(padded.shape))
    close("letterbox scale", scale, 416 / 640)
    ok("letterbox pads vertically only", pad_x == 0 and pad_y > 0,
       f"pad_x={pad_x} pad_y={pad_y}")

    # A box covering the whole letterboxed content must map back to the
    # whole original frame.
    content_h = int(round(360 * scale))
    box = (0, pad_y, 416, pad_y + content_h)
    x1, y1, x2, y2 = unletterbox_box(box, scale, pad_x, pad_y, 640, 360)
    close("full-frame box maps back: x1", x1, 0.0, tol=1e-6)
    close("full-frame box maps back: y1", y1, 0.0, tol=1e-6)
    close("full-frame box maps back: x2", x2, 640.0, tol=1e-3)
    close("full-frame box maps back: y2", y2, 360.0, tol=1e-3)

    # A centred box stays centred through the round trip.
    cx_pad, cy_pad = 208, 208
    box = (cx_pad - 20, cy_pad - 20, cx_pad + 20, cy_pad + 20)
    x1, y1, x2, y2 = unletterbox_box(box, scale, pad_x, pad_y, 640, 360)
    close("centred box stays centred: cx", (x1 + x2) / 2, 320.0, tol=1e-3)
    close("centred box stays centred: cy", (y1 + y2) / 2, 180.0, tol=1e-3)

    # Boxes outside the frame are clipped, never negative.
    x1, y1, x2, y2 = unletterbox_box((-500, -500, 900, 900), scale,
                                     pad_x, pad_y, 640, 360)
    ok("out-of-frame box is clipped",
       0 <= x1 <= 640 and 0 <= y1 <= 360 and 0 <= x2 <= 640 and 0 <= y2 <= 360,
       f"({x1},{y1},{x2},{y2})")

    # A portrait frame must pad horizontally instead.
    frame = np.zeros((640, 360, 3), dtype=np.uint8)
    _, scale, pad_x, pad_y = letterbox(frame, 416)
    ok("portrait pads horizontally", pad_x > 0 and pad_y == 0,
       f"pad_x={pad_x} pad_y={pad_y}")

    # IoU sanity.
    close("iou of identical boxes", iou((0, 0, 10, 10), (0, 0, 10, 10)), 1.0)
    close("iou of disjoint boxes", iou((0, 0, 10, 10), (20, 20, 30, 30)), 0.0)
    close("iou of half-overlap", iou((0, 0, 10, 10), (5, 0, 15, 10)), 1 / 3)

    # NMS keeps the best of a cluster, and keeps distinct objects.
    boxes = [(0, 0, 10, 10), (1, 1, 11, 11), (100, 100, 110, 110)]
    scores = [0.9, 0.8, 0.7]
    classes = [0, 0, 0]
    keep = non_max_suppression(boxes, scores, classes, iou_threshold=0.45)
    ok("NMS suppresses the duplicate", len(keep) == 2, f"kept {keep}")
    ok("NMS keeps the highest score", keep[0] == 0, f"kept {keep}")

    # Overlapping boxes of DIFFERENT classes must both survive.
    keep = non_max_suppression(boxes[:2], scores[:2], [0, 1], iou_threshold=0.45)
    ok("NMS is per-class", len(keep) == 2, f"kept {keep}")


# ----------------------------------------------------------------------
def test_follow_behaviour():
    print("\n--- FollowBehaviour ---")
    beh = FollowBehaviour(target_class=0, target_area_fraction=0.12)
    W, H = 640, 480

    # No detections -> no motion.
    linear, angular = beh.propose([], W, H)
    ok("no target means no motion", linear == 0.0 and angular == 0.0)

    # A target to the RIGHT of centre should yaw negative (clockwise).
    det = Detection(400, 200, 500, 400, 0.9, 0)
    _, angular = beh.propose([det], W, H)
    ok("target right of centre yaws right", angular < 0.0, f"got {angular}")

    # A target to the LEFT should yaw positive.
    det = Detection(140, 200, 240, 400, 0.9, 0)
    _, angular = beh.propose([det], W, H)
    ok("target left of centre yaws left", angular > 0.0, f"got {angular}")

    # A small (distant) target -> approach; a large (near) one -> back off.
    small = Detection(300, 220, 340, 260, 0.9, 0)
    linear, _ = beh.propose([small], W, H)
    ok("small target is approached", linear > 0.0, f"got {linear}")

    large = Detection(50, 20, 600, 460, 0.9, 0)
    linear, _ = beh.propose([large], W, H)
    ok("large target is backed away from", linear < 0.0, f"got {linear}")

    # Objects of other classes are ignored.
    other = Detection(400, 200, 500, 400, 0.99, 7)
    linear, angular = beh.propose([other], W, H)
    ok("non-target classes ignored", linear == 0.0 and angular == 0.0)

    # The behaviour's raw output must still be gated. Give it a target so
    # close that it proposes a large reverse, and check the gate clamps it.
    gate = SafetyGate(GateLimits(max_linear_mps=0.5))
    linear, angular = beh.propose([large], W, H)
    d = gate.evaluate(linear, angular, healthy())
    ok("behaviour output survives the gate within limits",
       abs(d.linear) <= 0.5 and abs(d.angular) <= 1.5,
       f"linear={d.linear} angular={d.angular}")


def main():
    test_gate()
    test_vision_geometry()
    test_follow_behaviour()

    print()
    if FAILURES:
        print(f"{len(FAILURES)} TEST(S) FAILED")
        return 1
    print("ALL AUTONOMY AND VISION TESTS PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())

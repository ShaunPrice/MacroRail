"""Safety-gated autonomy: perception proposes, the gate disposes.

The robot architecture has three loops:

    SLOW  (0.1-1 Hz, may be cloud)  LLM/VLM planner -> goals
    MID   (5-30 Hz, on the A53s)    detection, navigation -> setpoints
    FAST  (1-100 kHz, in the PL)    PID + safety -> motor outputs

This module is the boundary between MID and FAST. Nothing here is allowed
to be clever: every command produced by a model, a planner, or a behaviour
passes through SafetyGate, which is deliberately boring, deterministic,
and easy to read in full.

Defence in depth, three layers, each independent:

  1. SafetyGate (this file)  - semantic rules: obstacles, battery, stale
     perception, malformed numbers, acceleration limits.
  2. MotionController        - unit conversion and API discipline.
  3. safety_core.v in the PL - the hard ceiling, the E-stop and the
     watchdog, which hold even if layers 1 and 2 are not running at all.

Layer 3 is the only one that survives this process being killed. Layers 1
and 2 exist to make the robot behave well; layer 3 exists to make it safe.
Never let a reviewer talk you into treating them as redundant.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
from dataclasses import dataclass, field
from typing import List, Optional, Sequence, Tuple


@dataclass
class GateLimits:
    """Hard bounds the gate enforces. Set once; treat as read-only."""

    max_linear_mps: float = 0.5
    max_angular_rps: float = 1.5
    max_linear_accel_mps2: float = 0.8
    max_angular_accel_rps2: float = 3.0
    # A detection whose box occupies more than this fraction of the frame
    # is treated as an imminent obstacle.
    obstacle_area_fraction: float = 0.25
    perception_timeout_s: float = 1.0
    battery_floor_v: float = 10.5


@dataclass
class RobotState:
    """Everything the gate needs to judge a proposed command."""

    now_s: float
    battery_v: float = 99.0
    last_perception_s: float = 0.0
    estop_latched: bool = False
    obstacle_ahead: bool = False


@dataclass
class GateDecision:
    linear: float
    angular: float
    reasons: List[str] = field(default_factory=list)

    @property
    def modified(self) -> bool:
        return bool(self.reasons)


class SafetyGate:
    """Validates and clamps every proposed velocity command.

    Deliberately has no knowledge of *why* a command was proposed. A
    follow-me behaviour, a Nav2 plan and an LLM tool call are all treated
    identically, because the gate's guarantees must not depend on the
    trustworthiness of the caller.
    """

    def __init__(self, limits: Optional[GateLimits] = None):
        self.limits = limits or GateLimits()
        self._last_linear = 0.0
        self._last_angular = 0.0
        self._last_time: Optional[float] = None

    def reset(self) -> None:
        self._last_linear = 0.0
        self._last_angular = 0.0
        self._last_time = None

    # ------------------------------------------------------------------
    def evaluate(
        self, proposed_linear: float, proposed_angular: float, state: RobotState
    ) -> GateDecision:
        lim = self.limits
        reasons: List[str] = []

        linear = proposed_linear
        angular = proposed_angular

        # A hard stop must reach the motors immediately. Everything that
        # sets this flag bypasses the acceleration limiter below, because
        # an E-stop that ramps down over half a second is not an E-stop.
        hard_stop = False

        # 1. Malformed numbers. A model or a divide-by-zero can produce
        #    NaN or inf; both compare false against every bound, so they
        #    would sail through a naive min/max clamp untouched.
        if not _is_finite(linear) or not _is_finite(angular):
            reasons.append("non-finite command rejected")
            linear = angular = 0.0
            hard_stop = True

        # 2. Hard stops. These zero the command outright rather than
        #    scaling it, because "slower toward the wall" is not safe.
        if state.estop_latched:
            reasons.append("E-stop latched")
            linear = angular = 0.0
            hard_stop = True

        if state.battery_v < lim.battery_floor_v:
            reasons.append(
                f"battery {state.battery_v:.1f}V below floor "
                f"{lim.battery_floor_v:.1f}V"
            )
            linear = angular = 0.0
            hard_stop = True

        age = state.now_s - state.last_perception_s
        if age > lim.perception_timeout_s:
            reasons.append(f"perception stale by {age:.2f}s")
            linear = angular = 0.0
            hard_stop = True

        # 3. Obstacle: forward motion is blocked, but reversing and
        #    turning in place stay available so the robot can recover.
        #    Also a hard stop - decelerating gently into an obstacle
        #    defeats the purpose of detecting it.
        if state.obstacle_ahead and linear > 0.0:
            reasons.append("obstacle ahead - forward motion blocked")
            linear = 0.0
            hard_stop = True

        # 4. Magnitude clamps.
        clamped_linear = _clamp(linear, -lim.max_linear_mps, lim.max_linear_mps)
        if clamped_linear != linear:
            reasons.append(
                f"linear clamped {linear:.2f} -> {clamped_linear:.2f} m/s"
            )
            linear = clamped_linear

        clamped_angular = _clamp(
            angular, -lim.max_angular_rps, lim.max_angular_rps
        )
        if clamped_angular != angular:
            reasons.append(
                f"angular clamped {angular:.2f} -> {clamped_angular:.2f} rad/s"
            )
            angular = clamped_angular

        # 5. Acceleration limits. Skipped on the first call, when there is
        #    no previous command to rate-limit against, and skipped
        #    entirely after a hard stop so the stop is immediate.
        if hard_stop:
            # Re-seed the limiter with what actually goes out, so resuming
            # ramps from a standstill rather than jumping back to the
            # pre-stop speed. An obstacle stop leaves yaw untouched, so
            # the robot can still turn away.
            self._last_linear = 0.0
            self._last_angular = angular
            self._last_time = state.now_s
            return GateDecision(linear=0.0, angular=angular, reasons=reasons)

        if self._last_time is not None:
            dt = state.now_s - self._last_time
            if dt > 0:
                max_dv = lim.max_linear_accel_mps2 * dt
                if abs(linear - self._last_linear) > max_dv:
                    limited = self._last_linear + math.copysign(
                        max_dv, linear - self._last_linear
                    )
                    reasons.append(
                        f"linear accel limited {linear:.2f} -> {limited:.2f}"
                    )
                    linear = limited

                max_dw = lim.max_angular_accel_rps2 * dt
                if abs(angular - self._last_angular) > max_dw:
                    limited = self._last_angular + math.copysign(
                        max_dw, angular - self._last_angular
                    )
                    reasons.append(
                        f"angular accel limited {angular:.2f} -> {limited:.2f}"
                    )
                    angular = limited

        self._last_linear = linear
        self._last_angular = angular
        self._last_time = state.now_s

        return GateDecision(linear=linear, angular=angular, reasons=reasons)


# ----------------------------------------------------------------------
# An example MID-loop behaviour: follow the largest detection of a class.
# ----------------------------------------------------------------------
class FollowBehaviour:
    """Proposes a velocity that keeps a target centred and at range.

    Range is inferred from the box area, which is crude but needs no depth
    camera. It is a proposal only - SafetyGate has the final say, so a
    wildly wrong estimate cannot run the robot into anything.
    """

    def __init__(
        self,
        target_class: int = 0,          # 0 == person in COCO
        target_area_fraction: float = 0.12,
        turn_gain: float = 2.0,
        approach_gain: float = 1.5,
        deadband: float = 0.05,
    ):
        self.target_class = target_class
        self.target_area_fraction = target_area_fraction
        self.turn_gain = turn_gain
        self.approach_gain = approach_gain
        self.deadband = deadband

    def propose(
        self, detections: Sequence, frame_w: int, frame_h: int
    ) -> Tuple[float, float]:
        """Returns (linear m/s, angular rad/s). Zero when no target."""
        if frame_w <= 0 or frame_h <= 0:
            return 0.0, 0.0

        candidates = [d for d in detections if d.class_id == self.target_class]
        if not candidates:
            return 0.0, 0.0

        target = max(candidates, key=lambda d: d.area)
        frame_area = float(frame_w * frame_h)

        # Horizontal error in [-1, 1]; positive means the target is right
        # of centre, so the robot should yaw negative (clockwise).
        cx, _ = target.center
        x_error = (cx - frame_w / 2.0) / (frame_w / 2.0)
        angular = -self.turn_gain * x_error if abs(x_error) > self.deadband else 0.0

        # Approach error: positive when the target looks too far away.
        area_fraction = target.area / frame_area
        area_error = self.target_area_fraction - area_fraction
        linear = (
            self.approach_gain * area_error
            if abs(area_error) > self.deadband * self.target_area_fraction
            else 0.0
        )

        return linear, angular


# ----------------------------------------------------------------------
def _clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def _is_finite(value: float) -> bool:
    try:
        return math.isfinite(float(value))
    except (TypeError, ValueError):
        return False

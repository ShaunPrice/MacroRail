"""Differential-drive dead reckoning, with no ROS or hardware dependency.

Kept separate from ros2_node.py so the pose integration can be tested on
any machine. Odometry bugs are miserable to debug on a real robot: the
symptom is "the map slowly bends", which looks like a SLAM problem, a TF
problem, or a wheel-slip problem long before anyone suspects the
integrator.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math
from dataclasses import dataclass


@dataclass
class Pose2D:
    x: float = 0.0
    y: float = 0.0
    yaw: float = 0.0


class DeadReckoner:
    """Integrates wheel travel into a planar pose.

    Uses the midpoint of the yaw change over each step rather than the
    yaw at its start. For a robot turning while driving, the naive form
    biases every arc outward, and the error accumulates in one direction -
    which is what makes a mapped corridor come out curved.
    """

    def __init__(self, wheel_base_m: float, pose: Pose2D = None):
        if wheel_base_m <= 0.0:
            raise ValueError("wheel_base_m must be positive")
        self.wheel_base_m = wheel_base_m
        self.pose = pose or Pose2D()

    def update(self, d_left_m: float, d_right_m: float) -> Pose2D:
        """Advance the pose by one step of wheel travel."""
        d_center = (d_left_m + d_right_m) / 2.0
        d_yaw = (d_right_m - d_left_m) / self.wheel_base_m

        mid_yaw = self.pose.yaw + d_yaw / 2.0
        self.pose.x += d_center * math.cos(mid_yaw)
        self.pose.y += d_center * math.sin(mid_yaw)
        self.pose.yaw = normalize_angle(self.pose.yaw + d_yaw)
        return self.pose

    def body_velocity(self, v_left_mps: float, v_right_mps: float):
        """Wheel speeds -> (linear m/s, angular rad/s)."""
        linear = (v_left_mps + v_right_mps) / 2.0
        angular = (v_right_mps - v_left_mps) / self.wheel_base_m
        return linear, angular


def normalize_angle(angle: float) -> float:
    """Wrap to [-pi, pi].

    Exactly +/-pi may come back with either sign - they are the same
    heading, and floating point decides which representative you get.
    Compare headings modulo 2*pi rather than by equality.
    """
    return math.atan2(math.sin(angle), math.cos(angle))

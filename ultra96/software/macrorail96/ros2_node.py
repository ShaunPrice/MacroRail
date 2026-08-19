"""ROS 2 differential-drive node backed by the FPGA motion controller.

This is the one node you have to write yourself in any ROS 2 robot:
/cmd_vel in, motor commands out, encoders in, /odom + TF out. Everything
else (teleop_twist_joy, slam_toolbox, nav2) plugs into these interfaces.

Subscribes:
    /cmd_vel        geometry_msgs/Twist
Publishes:
    /odom           nav_msgs/Odometry
    /motion_status  diagnostic_msgs/DiagnosticStatus
    TF: odom -> base_link

Two watchdogs, deliberately:

  * this node stops commanding when /cmd_vel goes stale, which handles the
    ordinary case of a teleop publisher going away;
  * the fabric stops the motors when *this node* goes away, which handles
    the case the software watchdog cannot - a crash, an OOM kill, a kernel
    stall, or a wedged DDS stack.

The second one is the reason for the FPGA. Never remove it.

Install into a ROS 2 workspace as an ament_python package, or run
directly with rclpy on the PYNQ image after `pip install rclpy`.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import math

import rclpy
from diagnostic_msgs.msg import DiagnosticStatus, KeyValue
from geometry_msgs.msg import Quaternion, TransformStamped, Twist
from nav_msgs.msg import Odometry
from rclpy.node import Node
from tf2_ros import TransformBroadcaster

from .motion import DiffDriveGeometry, MotionController, SafetyError
from .odometry import DeadReckoner


def yaw_to_quaternion(yaw: float) -> Quaternion:
    q = Quaternion()
    q.z = math.sin(yaw / 2.0)
    q.w = math.cos(yaw / 2.0)
    return q


class MotionNode(Node):
    def __init__(self):
        super().__init__("macrorail96_motion")

        # --- Parameters: measure these on the real robot -----------------
        self.declare_parameter("wheel_radius_m", 0.0325)
        self.declare_parameter("wheel_base_m", 0.20)
        self.declare_parameter("counts_per_rev", 1000.0)
        self.declare_parameter("max_wheel_speed_mps", 0.6)
        self.declare_parameter("control_rate_hz", 1000.0)
        self.declare_parameter("odom_rate_hz", 50.0)
        self.declare_parameter("cmd_timeout_s", 0.5)
        self.declare_parameter("hw_watchdog_ms", 300.0)
        self.declare_parameter("kp", 0.2)
        self.declare_parameter("ki", 0.015)
        self.declare_parameter("kd", 0.004)

        p = self.get_parameter
        geom = DiffDriveGeometry(
            wheel_radius_m=p("wheel_radius_m").value,
            wheel_base_m=p("wheel_base_m").value,
            counts_per_rev=p("counts_per_rev").value,
        )

        self.bot = MotionController(
            geometry=geom, control_rate_hz=p("control_rate_hz").value
        )
        self.bot.configure(
            max_wheel_speed_mps=p("max_wheel_speed_mps").value,
            watchdog_ms=p("hw_watchdog_ms").value,
            kp=p("kp").value,
            ki=p("ki").value,
            kd=p("kd").value,
        )
        # Freeze the envelope before anything else can touch the board.
        self.bot.lock_safety_envelope()
        self.bot.enable()
        self.get_logger().info(
            f"motion controller armed; envelope locked at "
            f"{p('max_wheel_speed_mps').value} m/s per wheel"
        )

        # --- Odometry state ----------------------------------------------
        self.odom = DeadReckoner(wheel_base_m=geom.wheel_base_m)
        self.last_positions = self.bot.wheel_positions_m()
        self.last_cmd_time = self.get_clock().now()
        self.cmd_timeout_s = p("cmd_timeout_s").value

        self.create_subscription(Twist, "cmd_vel", self.on_cmd_vel, 10)
        self.odom_pub = self.create_publisher(Odometry, "odom", 10)
        self.status_pub = self.create_publisher(
            DiagnosticStatus, "motion_status", 10
        )
        self.tf_broadcaster = TransformBroadcaster(self)

        self.create_timer(1.0 / p("odom_rate_hz").value, self.on_timer)

    # ------------------------------------------------------------------
    def on_cmd_vel(self, msg: Twist) -> None:
        self.last_cmd_time = self.get_clock().now()
        try:
            self.bot.set_body_velocity(
                linear=msg.linear.x, angular=msg.angular.z
            )
        except SafetyError as exc:
            self.get_logger().warn(f"command refused: {exc}")

    # ------------------------------------------------------------------
    def on_timer(self) -> None:
        now = self.get_clock().now()

        # Software watchdog: a stale /cmd_vel means stop commanding. The
        # hardware watchdog is the backstop if this node itself dies.
        age = (now - self.last_cmd_time).nanoseconds / 1e9
        if age > self.cmd_timeout_s:
            self.bot.stop()

        self.publish_odometry(now)
        self.publish_status()

    # ------------------------------------------------------------------
    def publish_odometry(self, now) -> None:
        left, right = self.bot.wheel_positions_m()
        d_left = left - self.last_positions[0]
        d_right = right - self.last_positions[1]
        self.last_positions = (left, right)

        pose = self.odom.update(d_left, d_right)

        v_left, v_right = self.bot.wheel_speeds_mps()
        linear, angular = self.odom.body_velocity(v_left, v_right)

        odom = Odometry()
        odom.header.stamp = now.to_msg()
        odom.header.frame_id = "odom"
        odom.child_frame_id = "base_link"
        odom.pose.pose.position.x = pose.x
        odom.pose.pose.position.y = pose.y
        odom.pose.pose.orientation = yaw_to_quaternion(pose.yaw)
        odom.twist.twist.linear.x = linear
        odom.twist.twist.angular.z = angular
        self.odom_pub.publish(odom)

        tf = TransformStamped()
        tf.header.stamp = now.to_msg()
        tf.header.frame_id = "odom"
        tf.child_frame_id = "base_link"
        tf.transform.translation.x = pose.x
        tf.transform.translation.y = pose.y
        tf.transform.rotation = yaw_to_quaternion(pose.yaw)
        self.tf_broadcaster.sendTransform(tf)

    # ------------------------------------------------------------------
    def publish_status(self) -> None:
        st = self.bot.status()
        msg = DiagnosticStatus()
        msg.name = "macrorail96/motion"

        if st["estop_latched"]:
            msg.level = DiagnosticStatus.ERROR
            msg.message = "E-stop latched"
        elif st["watchdog_tripped"]:
            msg.level = DiagnosticStatus.ERROR
            msg.message = "hardware watchdog tripped - motors inhibited"
        elif any(a["encoder_error"] for a in st["axes"]):
            msg.level = DiagnosticStatus.WARN
            msg.message = "encoder illegal transition (missed counts)"
        elif not st["envelope_locked"]:
            msg.level = DiagnosticStatus.WARN
            msg.message = "safety envelope NOT locked"
        else:
            msg.level = DiagnosticStatus.OK
            msg.message = "ok"

        msg.values = [
            KeyValue(key=k, value=str(v))
            for k, v in st.items()
            if k != "axes"
        ]
        self.status_pub.publish(msg)

    # ------------------------------------------------------------------
    def destroy_node(self):
        try:
            self.bot.stop()
            self.bot.close()
        finally:
            super().destroy_node()


def main(args=None):
    rclpy.init(args=args)
    node = MotionNode()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()

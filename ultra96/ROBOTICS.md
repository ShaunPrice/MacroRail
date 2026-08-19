# The Ultra96-V2 as an AI and robotics platform

This directory adds a **two-axis closed-loop motion controller with a
hardware safety core**, a **ROS 2 differential-drive node**, and a
**safety-gated perception pipeline** to the Ultra96-V2.

Everything here is simulated and tested — `./ultra96/run_tests.sh` runs
six suites (RTL simulation, an RTL-vs-model fixed-point cross-check, and
Python unit tests) with no board, no camera and no Vivado required.

---

## Why this board, honestly

Most hobby robots end up with **two brains**: a Linux computer that
thinks (Pi, Jetson) and a microcontroller that reacts (ESP32, Teensy,
flight controller). The split exists because Linux cannot be trusted with
a control loop — it garbage-collects, it swaps, it gets OOM-killed — while
an MCU cannot be trusted with a neural network.

The Ultra96-V2 collapses both brains onto one chip, and puts a **hardware
boundary** between them:

```
┌──────────────────────────────────────────────────────────────┐
│ SLOW  0.1–1 Hz   LLM / VLM planner (cloud or local)          │
│                  emits goals: "check the garage door"        │  A53 / cloud
├──────────────────────────────────────────────────────────────┤
│ MID   5–30 Hz    detection, SLAM, Nav2                       │  A53 + DPU
│                  emits velocity setpoints                    │  (vision.py)
├──────────────────────────────────────────────────────────────┤
│ GATE             SafetyGate: obstacles, battery, NaN, accel  │  A53
│                  (autonomy.py)                               │
├══════════════════════════════════════════════════════════════┤
│ FAST  1–100 kHz  PID + PWM + encoders + SAFETY CORE          │  PL FABRIC
│                  hard limits nothing above can override      │  ← silicon
└──────────────────────────────────────────────────────────────┘
```

The standard advice is *"AI proposes, the deterministic layer disposes."*
On a two-chip robot that boundary is a convention — the fast loop is
firmware, and firmware can be reflashed by whatever is running upstream.
Here it is enforced by logic gates that keep working when Linux is not.

**Where this board loses.** A DPU on a ZU3EG delivers roughly 0.5–1.2 TOPS.
A Jetson Orin Nano Super is ~67 TOPS for similar money, runs stock
PyTorch, and exports to TensorRT in one line. Vitis AI is a genuinely
harder toolchain: INT8 quantization, `.xmodel` compilation, and an
`arch.json` fingerprint that must match your bitstream. **If raw inference
throughput is the goal, buy the Jetson.** Choose the Ultra96 for the
determinism, the integration, and the few-watt power budget.

---

## What the fabric gives you that an MCU does not

| Capability | Why the FPGA wins |
|---|---|
| **Quadrature decoding** | A 2000 CPR encoder at 3000 rpm emits 100k edges/s **per axis**. As interrupts, that saturates an MCU and Linux drops them outright. The fabric counts every edge in parallel, per axis, forever. |
| **Control loop timing** | The PID runs on a hardware tick with zero jitter. Gains stay valid because `dt` is exactly constant — no "tuned like a haunted house" from 30% timing jitter. |
| **Dead-time insertion** | Shoot-through protection is counted in 10 ns hardware cycles, not hoped for in software. The testbench asserts zero shoot-through cycles across the entire run. |
| **E-stop** | Latches every axis into brake within one clock, with no software in the path. |
| **Command watchdog** | If Linux hangs, panics, or loses its network, the motors stop *on their own*. |
| **Limit lock** | Once locked, the speed ceiling is read-only until a PL reset. No software on the board can raise its own limits. |

### The limit lock is the interesting one

```python
bot.configure(max_wheel_speed_mps=0.6, watchdog_ms=300)
bot.lock_safety_envelope()      # one-way latch, in hardware
```

After that call, writes to the speed ceiling, watchdog timeout and
dead-time registers are **discarded by the fabric**. A buggy autonomy
node, a mis-scaled model output, or a compromised process cannot widen
the envelope it runs inside. The only way back is a power cycle.

The testbench proves this: it locks the envelope, tries to write a
1,000,000-count ceiling, and confirms the register still reads the
original value — then resets the core and confirms the lock releases only
then.

---

## Layout

```
ultra96/
├── run_tests.sh                     # runs everything below
├── fpga/
│   ├── rtl/
│   │   ├── quad_decoder.v           # x4 decode, glitch filter, windowed velocity
│   │   ├── pid_axis.v               # Q16.16 PID, anti-windup, derivative-on-measurement
│   │   ├── pwm_hbridge.v            # sign-magnitude PWM + dead-time
│   │   ├── safety_core.v            # E-stop, watchdog, one-way limit lock
│   │   ├── motion_axis.v            # one closed-loop axis
│   │   └── axi_motion.v             # AXI4-Lite, 2 axes  (map documented in-file)
│   ├── sim/
│   │   ├── tb_axi_motion.v          # closed loop vs a DC motor model, 27 checks
│   │   └── tb_pid_vectors.v         # emits a trace for the Python cross-check
│   ├── constraints/ultra96v2_robot.xdc
│   └── vivado/build_robot.tcl       # → .bit / .hwh / .xsa, fails on negative slack
└── software/
    ├── macrorail96/
    │   ├── motion.py                # driver: MMIO, units, safety API
    │   ├── odometry.py              # dead reckoning (pure, no ROS)
    │   ├── autonomy.py              # SafetyGate + an example follow behaviour
    │   ├── vision.py                # DPU / CPU detector + letterbox geometry
    │   └── ros2_node.py             # /cmd_vel → motors, encoders → /odom + TF
    └── tools/
        ├── pid_model.py             # independent PID model (verification)
        ├── test_motion_math.py
        ├── test_odometry.py
        └── test_autonomy.py
```

---

## How it is verified

Run `./ultra96/run_tests.sh`.

**1. Closed-loop RTL simulation.** The testbench wraps the DUT around a
first-order DC motor model whose shaft position is fed back as real
quadrature waveforms into the DUT's own decoder — so the loop under test
is the complete one: PID → PWM → H-bridge → motor → encoder → decoder →
PID. It checks velocity tracking in both directions (exact: 2560 Q8 =
10.00 counts/period), E-stop latching and deliberate clearing, watchdog
trip and auto-recovery, one-way limit lock, setpoint clamping,
limit-switch direction blocking, and asserts **zero** H-bridge
shoot-through cycles across the run.

**2. Fixed-point cross-check.** `pid_model.py` is a second implementation
of the PID written from the register specification, not translated from
the Verilog. The RTL is driven through 200 vectors that deliberately
exercise integral clamping and output saturation, and the two must agree
bit-for-bit.

> This caught a real bug during development. Verilog concatenations are
> **unsigned regardless of their contents**, so `integ_next > {i_max_s[31], i_max_s}`
> silently promoted the comparison to unsigned — every *negative*
> integrator value read as larger than the ceiling and clamped to
> *+i_max*. The controller still produced plausible-looking numbers. On
> hardware that is a motor that runs away in one direction.

**3. Python unit tests.** Units and diff-drive kinematics, dead reckoning
against closed-form trajectories (a driven square must return exactly to
the origin), and the safety gate against adversarial input.

> The gate tests caught a second real bug: the acceleration limiter ran
> *after* the E-stop check, so an E-stop ramped the command from 1.0 down
> to 0.99 instead of cutting it. Hard stops now bypass rate limiting
> entirely.

A third design flaw surfaced from testing rather than reading: at a 1 kHz
loop rate, integer counts-per-period quantized 0.1 m/s to **zero** — the
robot would have silently ignored slow commands. Velocity is now measured
over a 16-period sliding window in Q24.8, giving 1/16-count resolution
for ~16 ms of estimator lag.

---

## Getting it running

Follow the rungs in order. Each one de-risks the next; skipping is how
robots get broken.

### 1. Simulate (no hardware)

```sh
sudo apt install iverilog
pip3 install -r ultra96/software/requirements.txt
./ultra96/run_tests.sh
```

### 2. Build the bitstream

```sh
cd ultra96/fpga/vivado
vivado -mode batch -source build_robot.tcl
```

Needs Vivado 2021.2+ (free edition covers the ZU3EG) and ideally the
[Avnet board files](https://github.com/Avnet/bdf). The script refuses to
finish if the design misses timing — a bitstream with negative slack must
never reach a robot.

### 3. Wire it — read this section twice

**The 40-pin header is 1.8 V logic.** It is not 3.3 V or 5 V tolerant.
Every signal needs a level shifter (TXS0108E or similar):

- **Motor bridge** (`m0_in1/in2`, `m1_in1/in2`) → shifted to your driver's
  logic level. A DRV8871, BTS7960 or TB6612 all take sign-magnitude
  IN1/IN2 directly.
- **Encoders** → shifted *down* to 1.8 V. Most hobby encoders are 5 V.
- **Limit switches / bumpers** → shifted, pulled down by the XDC.
- **E-stop** → see below.

**Wire the E-stop fail-safe.** Use a **normally-closed** mushroom-head
switch, so the closed contact holds `estop_n` high and pressing it (or
cutting the wire, or unplugging the connector, or losing shifter power)
lets the pin fall low and inhibits motion. A normally-open button wired to
pull the pin low fails the other way — the robot keeps running with the
E-stop disconnected. That is the most dangerous wiring mistake available
on this board.

The button must **also break motor power directly**, through a contactor
in the battery lead. A software-visible E-stop is a convenience; the
contactor is the real one.

**Battery safety.** LiPo packs are the most dangerous item in the build.
Never charge unattended, never charge a puffed pack, charge in a LiPo-safe
bag, storage-charge to ~3.8 V/cell when idle, and never discharge below
~3.3 V/cell. The Ultra96 itself wants a clean 5 V supply — brown-outs
present as mystery reboots, so use a buck converter rated well above your
peak draw and never share it with motor current.

### 4. First motion — wheels off the ground

Always. Every time you run new motion code.

```sh
sudo python3 -c "
from macrorail96.motion import MotionController, DiffDriveGeometry
geom = DiffDriveGeometry(wheel_radius_m=0.0325, wheel_base_m=0.20,
                         counts_per_rev=1000)
with MotionController(geom) as bot:
    bot.configure(max_wheel_speed_mps=0.15, watchdog_ms=300)
    bot.lock_safety_envelope()
    bot.enable()
    import time
    for _ in range(30):
        bot.set_body_velocity(linear=0.05, angular=0.0)  # pets the watchdog
        time.sleep(0.1)
        print(bot.status()['axes'][0])
"
```

Verify before anything else: both wheels turn the **same** way for a
positive linear command, position counts **increase** when a wheel is
turned forward by hand, and pressing the E-stop stops everything
instantly. If a wheel counts backwards, set `invert_enc` for that axis
rather than rewiring.

Then **stop commanding** and confirm the wheels stop within your watchdog
timeout. That test is the whole reason the fabric exists.

### 5. Tune the PID

Wheels still off the ground, then on the floor with room to run.

Zero Ki and Kd. Raise Kp until the wheel responds briskly and just starts
to oscillate, then halve it. Add Kd to kill the overshoot. Add a little
Ki last, only to remove steady-state error. Change one gain at a time,
and record the gains **with the control rate** — gains are meaningless
without it.

Gains map Q24.8 velocity error to PWM compare counts, so useful values
are well below 1.0 (around 0.2 for a small geared rover). The defaults in
`motion.py` are a starting point, not a tune.

### 6. ROS 2

```sh
ros2 run macrorail96 motion_node --ros-args \
    -p wheel_radius_m:=0.0325 -p wheel_base_m:=0.20 \
    -p counts_per_rev:=1000 -p max_wheel_speed_mps:=0.6
```

Publishes `/odom` and the `odom → base_link` transform, subscribes
`/cmd_vel`, and reports hardware state on `/motion_status`. From there the
standard stack drops straight in:

```
joy + teleop_twist_joy   → /cmd_vel
robot_state_publisher    → TF from your URDF
sllidar_ros2             → /scan
slam_toolbox             → /map
nav2_bringup             → autonomous navigation
```

Measure your wheel separation properly — a wrong `wheel_base_m` makes
"straight" lines curve, and it looks exactly like a SLAM problem.

### 7. Vision and AI

`vision.py` runs YOLO-style detection on the DPU when a compiled
`.xmodel` and the Vitis AI runtime are present, and falls back to OpenCV
on the A53s otherwise — so the robot is testable before you fight the
quantization toolchain.

`autonomy.py` is where model output becomes motion, and it is deliberately
boring. Every proposed command passes `SafetyGate`, which rejects
non-finite numbers, zeroes on E-stop / flat battery / stale perception,
blocks forward motion on an obstacle while leaving reverse and turn
available, clamps magnitude, and rate-limits acceleration — recording a
human-readable reason for every modification it makes.

Note what the gate does *not* do: it has no idea whether a command came
from a follow-me behaviour, a Nav2 plan, or an LLM tool call. That is the
point. Its guarantees must not depend on trusting the caller.

If you add an LLM planner, give it **tools, not actuators**:
`navigate_to(named_pose)`, `capture_image()`, `vlm_query(q)`,
`report(text)`. The model should be structurally incapable of expressing
"set motor PWM". And treat anything the robot reads — signs, QR codes,
speech — as untrusted data, never as instructions.

---

## Other things worth building on this board

- **More axes.** The axis module is instantiable; a ZU3EG has room for
  many. Four for mecanum, six for an arm, or one per joint on a legged
  robot.
- **Sensor fusion in fabric.** IMU + encoder fusion at kHz rates, feeding
  Linux a clean pose estimate instead of raw samples.
- **Hardware-timed camera sync.** Trigger a global-shutter camera from the
  same clock that timestamps the encoders, and get sub-microsecond
  alignment between images and odometry — genuinely hard on a Pi, nearly
  free here. (This is also what the [MacroRail camera-rail
  controller](README.md) does for focus stacking.)
- **Motor current sensing** via the PS ADC, with per-axis stall detection
  in fabric.

---

## Licence

GPL-3.0-or-later, same as the rest of MacroRail.

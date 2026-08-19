#!/usr/bin/env bash
#
# Run every simulation and software test in ultra96/.
#
# Needs: iverilog (apt install iverilog), python3 with numpy and opencv
# (pip install -r software/requirements.txt). Nothing here needs an
# Ultra96 board, a camera, or Vivado - the point is that the control
# logic, the fixed-point maths, the odometry and the safety gate are all
# checkable before any hardware is powered on.
#
# Usage:  ./ultra96/run_tests.sh
#
# This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
# and is released under the GNU General Public License v3 or later.

set -u

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RTL="$HERE/fpga/rtl"
SIM="$HERE/fpga/sim"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

FAILED=0

banner() { printf '\n=== %s ===\n' "$1"; }

run_step() {
    local name="$1"; shift
    banner "$name"
    if "$@"; then
        echo "--> $name: OK"
    else
        echo "--> $name: FAILED"
        FAILED=$((FAILED + 1))
    fi
}

# ---------------------------------------------------------------------
# 1. MacroRail camera-rail stepper controller
# ---------------------------------------------------------------------
stepper_sim() {
    iverilog -g2012 -o "$WORK/tb_stepper" \
        "$RTL/stepper_ctrl.v" "$RTL/axi_stepper.v" \
        "$SIM/tb_axi_stepper.v" || return 1
    ( cd "$WORK" && ./tb_stepper ) | tee "$WORK/stepper.log"
    grep -q "ALL TESTS PASSED" "$WORK/stepper.log"
}

# ---------------------------------------------------------------------
# 2. Robot motion controller, closed loop against a DC motor model
# ---------------------------------------------------------------------
motion_sim() {
    iverilog -g2012 -o "$WORK/tb_motion" \
        "$RTL/quad_decoder.v" "$RTL/pid_axis.v" "$RTL/pwm_hbridge.v" \
        "$RTL/safety_core.v" "$RTL/motion_axis.v" "$RTL/axi_motion.v" \
        "$SIM/tb_axi_motion.v" || return 1
    ( cd "$WORK" && ./tb_motion ) | tee "$WORK/motion.log"
    grep -q "ALL TESTS PASSED" "$WORK/motion.log"
}

# ---------------------------------------------------------------------
# 3. RTL PID vs an independent Python model, bit for bit
# ---------------------------------------------------------------------
pid_crosscheck() {
    iverilog -g2012 -o "$WORK/tb_pid" \
        "$RTL/pid_axis.v" "$SIM/tb_pid_vectors.v" || return 1
    ( cd "$WORK" && ./tb_pid ) > "$WORK/pid_trace.csv"
    python3 "$HERE/software/tools/pid_model.py" "$WORK/pid_trace.csv"
}

run_step "RTL: MacroRail stepper controller"  stepper_sim
run_step "RTL: robot motion controller"       motion_sim
run_step "RTL vs model: PID fixed-point"      pid_crosscheck
run_step "Python: motion units + kinematics"  \
    python3 "$HERE/software/tools/test_motion_math.py"
run_step "Python: odometry dead reckoning"    \
    python3 "$HERE/software/tools/test_odometry.py"
run_step "Python: safety gate + detection geometry" \
    python3 "$HERE/software/tools/test_autonomy.py"

banner "Summary"
if [ "$FAILED" -eq 0 ]; then
    echo "All test suites passed."
    exit 0
fi
echo "$FAILED test suite(s) FAILED."
exit 1

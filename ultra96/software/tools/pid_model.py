"""Independent Python model of pid_axis, used to verify the RTL.

This is deliberately a second implementation written from the register
specification rather than a translation of the Verilog, so that a mistake
in one is unlikely to be mirrored in the other. It reproduces the fixed
point arithmetic exactly:

  * gains are unsigned Q16.16, so every product is scaled back by >> 16,
  * the shift is arithmetic (Python's >> on negative ints floors, matching
    Verilog's >>> on a signed value),
  * the integrator is clamped to +/- i_max, and frozen entirely when the
    output is already saturated and the error would push it further out,
  * the derivative acts on the measurement, not the error.

Usage:
    iverilog -g2012 -o pv ultra96/fpga/rtl/pid_axis.v \\
        ultra96/fpga/sim/tb_pid_vectors.v && vvp pv > trace.csv
    python3 ultra96/software/tools/pid_model.py trace.csv

Exits non-zero on any mismatch.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

import csv
import sys

# Must match tb_pid_vectors.v
KP = 12800      # 0.195 in Q16.16
KI = 1024       # 0.0156
KD = 256        # 0.0039
I_MAX = 200
OUT_MAX = 1000


class PidModel:
    """Reference model of one pid_axis instance."""

    def __init__(self, kp=KP, ki=KI, kd=KD, i_max=I_MAX, out_max=OUT_MAX):
        self.kp, self.ki, self.kd = kp, ki, kd
        self.i_max, self.out_max = i_max, out_max
        self.integral = 0
        self.meas_prev = 0
        self.meas_prev_valid = False
        self.out = 0
        self.saturated = False

    def update(self, setpoint: int, measurement: int) -> int:
        error = setpoint - measurement
        d_meas = (measurement - self.meas_prev) if self.meas_prev_valid else 0

        # Conditional integration: freeze rather than wind up further.
        push_further = self.saturated and (
            (self.out > 0 and error > 0) or (self.out < 0 and error < 0)
        )
        if push_further:
            integral = self.integral
        else:
            integral = max(-self.i_max, min(self.i_max, self.integral + error))

        p_term = self.kp * error
        i_term = self.ki * integral
        d_term = self.kd * (-d_meas)

        total = (p_term + i_term + d_term) >> 16      # arithmetic shift

        if total > self.out_max:
            out, saturated = self.out_max, True
        elif total < -self.out_max:
            out, saturated = -self.out_max, True
        else:
            out, saturated = total, False

        self.integral = integral
        self.meas_prev = measurement
        self.meas_prev_valid = True
        self.out = out
        self.saturated = saturated
        return out


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2

    # Simulators interleave their own banners ($finish, VCD notices) with
    # the trace on stdout, so keep only well-formed vector rows.
    def is_vector(row):
        try:
            int(row["k"])
            int(row["setpoint"])
            int(row["measurement"])
            int(row["out"])
            int(row["saturated"])
            return True
        except (TypeError, ValueError):
            return False

    with open(sys.argv[1], newline="") as fh:
        rows = [r for r in csv.DictReader(fh) if is_vector(r)]

    if not rows:
        print("no vectors found in trace - did the simulation run?")
        return 2

    model = PidModel()
    mismatches = 0
    for row in rows:
        k = int(row["k"])
        setpoint = int(row["setpoint"])
        measurement = int(row["measurement"])
        rtl_out = int(row["out"])
        rtl_sat = bool(int(row["saturated"]))

        expected = model.update(setpoint, measurement)
        if expected != rtl_out or model.saturated != rtl_sat:
            mismatches += 1
            if mismatches <= 10:
                print(
                    f"MISMATCH k={k} sp={setpoint} meas={measurement}: "
                    f"rtl out={rtl_out} sat={int(rtl_sat)} | "
                    f"model out={expected} sat={int(model.saturated)}"
                )

    total = len(rows)
    if mismatches:
        print(f"\nFAIL: {mismatches}/{total} vectors disagree")
        return 1

    print(f"PASS: RTL matches the reference model on all {total} vectors")
    return 0


if __name__ == "__main__":
    sys.exit(main())

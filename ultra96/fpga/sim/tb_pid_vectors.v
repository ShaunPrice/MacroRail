// -----------------------------------------------------------------------------
// tb_pid_vectors.v
//
// Emits a deterministic trace of pid_axis behaviour so an independent
// Python model (ultra96/software/tools/pid_model.py) can check the
// fixed-point arithmetic bit-for-bit.
//
// Fixed-point control code fails quietly: a Q-format slip or a sign
// mistake still produces plausible-looking numbers, and you find out on
// hardware when a motor runs away. Comparing the RTL against a separate
// implementation of the same specification catches that on the bench.
//
// The stimulus deliberately drives the controller through integral
// clamping and output saturation, not just the linear region.
//
// Run:
//   iverilog -g2012 -o pv ultra96/fpga/rtl/pid_axis.v \
//       ultra96/fpga/sim/tb_pid_vectors.v && vvp pv > trace.csv
//   python3 ultra96/software/tools/pid_model.py trace.csv
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps

module tb_pid_vectors;

    localparam N_VECTORS = 200;

    // Must match pid_model.py (gains are Q16.16)
    localparam [31:0] KP      = 32'd12800;    // 0.195 in Q16.16
    localparam [31:0] KI      = 32'd1024;     // 0.0156
    localparam [31:0] KD      = 32'd256;      // 0.0039
    localparam [31:0] I_MAX   = 32'd200;      // small, to force clamping
    localparam [31:0] OUT_MAX = 32'd1000;

    reg clk = 0;
    reg rst_n = 0;
    always #5 clk = ~clk;

    reg               tick = 0;
    reg               enable = 1;
    reg signed [31:0] setpoint = 0;
    reg signed [31:0] measurement = 0;

    wire signed [31:0] out;
    wire signed [31:0] error_out;
    wire               out_valid, saturated;

    pid_axis dut (
        .clk         (clk),
        .rst_n       (rst_n),
        .sample_tick (tick),
        .enable      (enable),
        .setpoint    (setpoint),
        .measurement (measurement),
        .kp          (KP),
        .ki          (KI),
        .kd          (KD),
        .i_max       (I_MAX),
        .out_max     (OUT_MAX),
        .out         (out),
        .out_valid   (out_valid),
        .error_out   (error_out),
        .saturated   (saturated)
    );

    integer k;
    integer sp, ms;

    initial begin
        repeat (4) @(posedge clk);
        rst_n = 1;
        repeat (4) @(posedge clk);

        $display("k,setpoint,measurement,out,saturated");

        for (k = 0; k < N_VECTORS; k = k + 1) begin
            // Deterministic stimulus, reproducible in Python.
            sp = ((k * 7)  % 61) - 30;
            ms = ((k * 13) % 41) - 20;

            setpoint    = sp;
            measurement = ms;

            @(posedge clk);
            tick = 1;
            @(posedge clk);
            tick = 0;
            // Stage 2 lands on the next edge; sample after it settles.
            @(posedge clk);
            @(posedge clk);

            $display("%0d,%0d,%0d,%0d,%0d", k, sp, ms, out, saturated);
        end

        $finish;
    end

endmodule

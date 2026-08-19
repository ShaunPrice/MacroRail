// -----------------------------------------------------------------------------
// pid_axis.v
//
// Fixed-point PID controller, one control update per sample_tick.
//
// Implements the three things that separate a working PID from a
// textbook one:
//   * integral clamping (anti-windup) plus conditional integration, so a
//     saturated output cannot keep charging the integrator - the classic
//     "works for three seconds then diverges" failure;
//   * derivative-on-measurement rather than on error, which removes the
//     derivative kick when the setpoint steps;
//   * a hard output clamp, so the value handed to the PWM stage is always
//     within the commanded envelope.
//
// Gains are unsigned Q16.16 (1.0 == 65536, range 0 .. 65535.99998). The
// wide fractional part is deliberate: in velocity mode the measurement is
// itself Q24.8 counts per period, so the useful gain is well below 1.0
// (around 0.2 for a typical rover wheel loop). A Q8.8 gain word would
// quantise Kd down to one or two LSBs and make the derivative term
// untunable.
//
// Units: position mode works in encoder counts; velocity mode works in
// Q24.8 counts per control period. The two modes therefore need different
// gains - retune when you switch, and record the loop rate next to the
// gains, because gains are only meaningful at a fixed sample rate.
//
// Two-cycle pipeline: products on the tick, sum/clamp on the next cycle.
// At 100 MHz with a 1 kHz control rate there are ~100,000 cycles of slack,
// so the DSP48 multipliers infer and time comfortably.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module pid_axis (
    input  wire               clk,
    input  wire               rst_n,

    input  wire               sample_tick,
    input  wire               enable,          // low clears integrator and output

    input  wire signed [31:0] setpoint,
    input  wire signed [31:0] measurement,

    input  wire        [31:0] kp,              // Q16.16
    input  wire        [31:0] ki,              // Q16.16
    input  wire        [31:0] kd,              // Q16.16
    input  wire        [31:0] i_max,           // integrator clamp (magnitude)
    input  wire        [31:0] out_max,         // output clamp (magnitude)

    output reg  signed [31:0] out,             // control effort
    output reg                out_valid,
    output reg  signed [31:0] error_out,
    output reg                saturated
);

    localparam integer GAIN_SHIFT = 16;
    localparam integer PW = 72;                // product / accumulator width

    reg signed [31:0] integral;
    reg signed [31:0] meas_prev;
    reg               meas_prev_valid;

    // Pipeline stage 1 registers
    reg signed [PW-1:0] p_prod, i_prod, d_prod;
    reg                 stage2;

    // ------------------------------------------------------------------
    // Stage 1 arithmetic (combinational; registered on sample_tick)
    // ------------------------------------------------------------------
    wire signed [31:0] error  = setpoint - measurement;
    wire signed [31:0] d_meas = meas_prev_valid ? (measurement - meas_prev) : 32'sd0;

    wire signed [32:0] integ_next = integral + error;
    wire signed [31:0] i_max_s    = $signed({1'b0, i_max[30:0]});
    wire signed [31:0] out_max_s  = $signed({1'b0, out_max[30:0]});

    // Widen the clamp through an explicitly signed wire before comparing.
    // A concatenation is unsigned in Verilog no matter what it contains, so
    // comparing a signed value against one inline makes the whole
    // comparison unsigned - and every negative integral then reads as
    // greater than the ceiling and clamps to +i_max.
    wire signed [32:0] i_max_ext = {i_max_s[31], i_max_s};

    // Clamp the integrator.
    wire signed [31:0] integ_clamped =
        (integ_next >  i_max_ext) ?  i_max_s :
        (integ_next < -i_max_ext) ? -i_max_s :
        integ_next[31:0];

    // Conditional integration: if the output is already saturated and the
    // error would drive it further out, freeze the integrator instead.
    wire push_further = saturated &&
                        ((out > 0 && error > 0) || (out < 0 && error < 0));
    wire signed [31:0] integ_new = push_further ? integral : integ_clamped;

    // Gains are unsigned; widen to signed so the products stay signed.
    wire signed [32:0] kp_s = $signed({1'b0, kp});
    wire signed [32:0] ki_s = $signed({1'b0, ki});
    wire signed [32:0] kd_s = $signed({1'b0, kd});

    // ------------------------------------------------------------------
    // Stage 2 arithmetic
    // ------------------------------------------------------------------
    wire signed [PW-1:0] sum_raw    = p_prod + i_prod + d_prod;
    wire signed [PW-1:0] sum_scaled = sum_raw >>> GAIN_SHIFT;

    // Sign-extend the clamp to the accumulator width. Comparing a signed
    // value against a plain concatenation would make the whole expression
    // unsigned, and every negative output would read as "over positive".
    wire signed [PW-1:0] out_max_ext = {{(PW-32){out_max_s[31]}}, out_max_s};

    wire over_pos = sum_scaled >  out_max_ext;
    wire over_neg = sum_scaled < -out_max_ext;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            integral        <= 32'sd0;
            meas_prev       <= 32'sd0;
            meas_prev_valid <= 1'b0;
            p_prod          <= {PW{1'b0}};
            i_prod          <= {PW{1'b0}};
            d_prod          <= {PW{1'b0}};
            stage2          <= 1'b0;
            out             <= 32'sd0;
            out_valid       <= 1'b0;
            error_out       <= 32'sd0;
            saturated       <= 1'b0;
        end else begin
            out_valid <= 1'b0;

            if (!enable) begin
                // Disabled: hold the loop reset so re-enabling starts clean.
                integral        <= 32'sd0;
                meas_prev_valid <= 1'b0;
                out             <= 32'sd0;
                saturated       <= 1'b0;
                stage2          <= 1'b0;
            end else begin
                if (sample_tick) begin
                    integral        <= integ_new;
                    meas_prev       <= measurement;
                    meas_prev_valid <= 1'b1;
                    error_out       <= error;

                    p_prod <= kp_s * error;
                    i_prod <= ki_s * integ_new;
                    // Derivative on measurement, negated: opposes change.
                    d_prod <= kd_s * (-d_meas);

                    stage2 <= 1'b1;
                end else if (stage2) begin
                    stage2 <= 1'b0;
                    if (over_pos) begin
                        out       <= out_max_s;
                        saturated <= 1'b1;
                    end else if (over_neg) begin
                        out       <= -out_max_s;
                        saturated <= 1'b1;
                    end else begin
                        out       <= sum_scaled[31:0];
                        saturated <= 1'b0;
                    end
                    out_valid <= 1'b1;
                end
            end
        end
    end

endmodule

`default_nettype wire

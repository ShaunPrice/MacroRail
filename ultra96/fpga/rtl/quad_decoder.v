// -----------------------------------------------------------------------------
// quad_decoder.v
//
// x4 quadrature encoder decoder with a digital glitch filter, illegal-
// transition detection, optional index (Z) homing, and per-sample velocity
// estimation.
//
// Counting all four edges of both channels gives 4x the encoder's native
// CPR. A 500 CPR encoder on a motor shaft therefore yields 2000 counts per
// revolution, which is what makes precise low-speed velocity control
// possible.
//
// Why this belongs in the fabric: decoding quadrature in software means an
// interrupt per edge. A 2000 CPR encoder at 3000 rpm emits 100k edges per
// second, per axis. Linux drops them; this module cannot.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module quad_decoder #(
    // Number of consecutive agreeing samples required to accept a level.
    // At 100 MHz, 4 rejects glitches shorter than ~40 ns.
    parameter FILTER_LEN = 4,

    // Velocity is measured over 2**VEL_WINDOW_LOG2 control periods and
    // reported in Q24.8 counts per period.
    //
    // This matters more than it looks. Measuring the raw position delta
    // over a single control period gives integer resolution only, and at a
    // 1 kHz loop rate a 1000 count/rev wheel creeping at 0.1 m/s moves
    // less than one count per period - so the whole command quantises to
    // zero and the robot silently ignores slow speeds. Averaging over a
    // 16-period window and keeping 8 fractional bits gives 1/16 count per
    // period of resolution, at the cost of ~16 ms of estimator lag, which
    // is well inside the mechanical time constant of any real drivetrain.
    parameter VEL_WINDOW_LOG2 = 4
) (
    input  wire               clk,
    input  wire               rst_n,

    // Raw encoder inputs (asynchronous)
    input  wire               enc_a,
    input  wire               enc_b,
    input  wire               enc_z,

    // Control
    input  wire               zero_pos,      // clear the position counter
    input  wire               index_en,      // zero the counter on the next Z pulse
    input  wire               invert,        // swap counting direction
    input  wire               sample_tick,   // control-loop rate strobe

    // Outputs
    output reg signed [31:0]  position,      // counts (x4)
    output reg signed [31:0]  velocity,      // Q24.8 counts per sample period
    output reg                index_seen,
    output reg                error_illegal  // both channels changed at once
);

    // ------------------------------------------------------------------
    // Synchronise and glitch-filter the raw inputs
    // ------------------------------------------------------------------
    reg [1:0] a_meta, b_meta, z_meta;
    always @(posedge clk) begin
        a_meta <= {a_meta[0], enc_a};
        b_meta <= {b_meta[0], enc_b};
        z_meta <= {z_meta[0], enc_z};
    end

    reg [FILTER_LEN-1:0] a_hist, b_hist, z_hist;
    reg a_filt, b_filt, z_filt;
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            a_hist <= {FILTER_LEN{1'b0}};
            b_hist <= {FILTER_LEN{1'b0}};
            z_hist <= {FILTER_LEN{1'b0}};
            a_filt <= 1'b0;
            b_filt <= 1'b0;
            z_filt <= 1'b0;
        end else begin
            a_hist <= {a_hist[FILTER_LEN-2:0], a_meta[1]};
            b_hist <= {b_hist[FILTER_LEN-2:0], b_meta[1]};
            z_hist <= {z_hist[FILTER_LEN-2:0], z_meta[1]};
            if (a_hist == {FILTER_LEN{1'b1}}) a_filt <= 1'b1;
            else if (a_hist == {FILTER_LEN{1'b0}}) a_filt <= 1'b0;
            if (b_hist == {FILTER_LEN{1'b1}}) b_filt <= 1'b1;
            else if (b_hist == {FILTER_LEN{1'b0}}) b_filt <= 1'b0;
            if (z_hist == {FILTER_LEN{1'b1}}) z_filt <= 1'b1;
            else if (z_hist == {FILTER_LEN{1'b0}}) z_filt <= 1'b0;
        end
    end

    // ------------------------------------------------------------------
    // x4 decode
    //
    // State is {A,B}. A single valid step changes exactly one bit; the
    // direction of travel is prev[1] ^ cur[0] (0 = forward). Both bits
    // changing in one sample means a step was missed - flag it rather than
    // silently counting wrong.
    // ------------------------------------------------------------------
    reg  [1:0] state, state_prev;
    wire [1:0] state_next = {a_filt, b_filt};
    wire       changed_a  = state[1] ^ state_next[1];
    wire       changed_b  = state[0] ^ state_next[0];
    wire       step_valid = changed_a ^ changed_b;   // exactly one bit moved
    wire       step_bad   = changed_a & changed_b;
    wire       dir_rev    = state[1] ^ state_next[0];

    localparam integer VEL_WINDOW = (1 << VEL_WINDOW_LOG2);
    localparam integer VEL_SHIFT  = 8 - VEL_WINDOW_LOG2;

    // Sliding window of past positions, one entry per control period.
    reg signed [31:0] pos_hist [0:VEL_WINDOW-1];
    reg               z_prev;
    integer           h;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            state           <= 2'b00;
            state_prev      <= 2'b00;
            position        <= 32'sd0;
            velocity        <= 32'sd0;
            index_seen      <= 1'b0;
            error_illegal   <= 1'b0;
            z_prev          <= 1'b0;
            for (h = 0; h < VEL_WINDOW; h = h + 1)
                pos_hist[h] <= 32'sd0;
        end else begin
            state      <= state_next;
            state_prev <= state;
            z_prev     <= z_filt;

            if (zero_pos) begin
                position      <= 32'sd0;
                error_illegal <= 1'b0;
                for (h = 0; h < VEL_WINDOW; h = h + 1)
                    pos_hist[h] <= 32'sd0;
            end else if (index_en && z_filt && !z_prev) begin
                // Rising edge of the index pulse: this is the home mark.
                position   <= 32'sd0;
                index_seen <= 1'b1;
            end else if (step_valid) begin
                if (dir_rev ^ invert)
                    position <= position - 32'sd1;
                else
                    position <= position + 32'sd1;
            end else if (step_bad) begin
                error_illegal <= 1'b1;
            end

            // Velocity: position delta across the whole window, rescaled
            // to Q24.8 counts per single control period.
            //   delta = v * VEL_WINDOW  ->  Q8 value = v * 256
            //   so shift left by (8 - log2(VEL_WINDOW)).
            if (sample_tick) begin
                velocity <= (position - pos_hist[VEL_WINDOW-1]) <<< VEL_SHIFT;
                for (h = VEL_WINDOW-1; h > 0; h = h - 1)
                    pos_hist[h] <= pos_hist[h-1];
                pos_hist[0] <= position;
            end
        end
    end

endmodule

`default_nettype wire

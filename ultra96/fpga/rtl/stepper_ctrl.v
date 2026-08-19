// -----------------------------------------------------------------------------
// stepper_ctrl.v
//
// Hardware-timed stepper pulse generator with trapezoidal speed ramp,
// limit-switch halt, and a two-stage (focus/shutter) camera trigger that
// fires after a programmable settle delay at the end of each move.
//
// Designed for the MacroRail focus-stacking rail on the Avnet Ultra96-V2
// (Zynq UltraScale+ ZU3EG). Drives any STEP/DIR stepper driver, including
// the Pololu TIC in STEP/DIR mode, via a 1.8 V -> 5 V level shifter.
//
// All timing values are in clk cycles (100 MHz PL clock by default, so
// 1 cycle = 10 ns). The step period ramps linearly from start_period down
// to min_period by accel per step, and back up over the same number of
// steps at the end of the move (trapezoid, degrading to a triangle for
// short moves).
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module stepper_ctrl (
    input  wire        clk,
    input  wire        rst_n,

    // Command strobes (single-cycle pulses)
    input  wire        start,          // begin a move using the settings below
    input  wire        abort,          // stop motion immediately, no trigger
    input  wire        trig_now,       // run settle+trigger only (no motion)
    input  wire        zero_pos,       // reset the position counter to zero

    // Move settings (sampled when start is asserted)
    input  wire [31:0] steps,          // number of steps in the move
    input  wire [23:0] start_period,   // step period at the start of the ramp
    input  wire [23:0] min_period,     // step period at full speed
    input  wire [15:0] accel,          // period decrement per step
    input  wire [15:0] step_width,     // STEP pulse high time
    input  wire [31:0] settle,         // delay after motion before trigger
    input  wire [31:0] trig_width,     // shutter pulse width
    input  wire        dir,            // 1 = forward (position increments)
    input  wire        trigger_en,     // fire the camera after the move
    input  wire        limit_polarity, // 1 = limit inputs are active high

    // Limit switches (asynchronous inputs)
    input  wire        limit_fwd,
    input  wire        limit_rev,

    // Status
    output reg         busy,
    output reg         done,           // set when a move+trigger completes; cleared on start
    output reg         halted_on_limit,
    output wire        limit_fwd_active,
    output wire        limit_rev_active,
    output reg  signed [31:0] position,

    // Motor and camera outputs
    output reg         step_out,
    output reg         dir_out,
    output reg         cam_focus,      // half-press: asserted from settle through shutter
    output reg         cam_shutter     // full-press: trig_width pulse after settle
);

    // ------------------------------------------------------------------
    // Limit switch synchronisers
    // ------------------------------------------------------------------
    reg [1:0] sync_fwd, sync_rev;
    always @(posedge clk) begin
        sync_fwd <= {sync_fwd[0], limit_fwd};
        sync_rev <= {sync_rev[0], limit_rev};
    end
    assign limit_fwd_active = limit_polarity ? sync_fwd[1] : ~sync_fwd[1];
    assign limit_rev_active = limit_polarity ? sync_rev[1] : ~sync_rev[1];

    // Moving into an asserted limit is blocked; moving away is allowed.
    wire limit_blocked = dir_out ? limit_fwd_active : limit_rev_active;

    // ------------------------------------------------------------------
    // State machine
    // ------------------------------------------------------------------
    localparam [2:0] S_IDLE      = 3'd0,
                     S_STEP_HIGH = 3'd1,
                     S_STEP_LOW  = 3'd2,
                     S_SETTLE    = 3'd3,
                     S_TRIGGER   = 3'd4;

    reg [2:0]  state;
    reg [31:0] steps_left;
    reg [23:0] cur_period;
    reg [31:0] ramp_count;     // steps spent accelerating (mirrored for decel)
    reg [31:0] timer;
    reg        do_trigger;

    // Low time for the current step: period minus the high time, at least 1.
    wire [23:0] low_time = (cur_period > {8'd0, step_width}) ?
                           (cur_period - {8'd0, step_width}) : 24'd1;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            state           <= S_IDLE;
            busy            <= 1'b0;
            done            <= 1'b0;
            halted_on_limit <= 1'b0;
            position        <= 32'sd0;
            step_out        <= 1'b0;
            dir_out         <= 1'b0;
            cam_focus       <= 1'b0;
            cam_shutter     <= 1'b0;
            steps_left      <= 32'd0;
            cur_period      <= 24'd0;
            ramp_count      <= 32'd0;
            timer           <= 32'd0;
            do_trigger      <= 1'b0;
        end else begin
            if (zero_pos)
                position <= 32'sd0;

            if (abort) begin
                state       <= S_IDLE;
                busy        <= 1'b0;
                step_out    <= 1'b0;
                cam_focus   <= 1'b0;
                cam_shutter <= 1'b0;
            end else begin
                case (state)
                    S_IDLE: begin
                        step_out    <= 1'b0;
                        cam_focus   <= 1'b0;
                        cam_shutter <= 1'b0;
                        if (start) begin
                            done            <= 1'b0;
                            halted_on_limit <= 1'b0;
                            dir_out         <= dir;
                            steps_left      <= steps;
                            cur_period      <= (start_period < min_period) ?
                                               min_period : start_period;
                            ramp_count      <= 32'd0;
                            do_trigger      <= trigger_en;
                            busy            <= 1'b1;
                            if (steps == 32'd0) begin
                                // Zero-length move: settle+trigger only.
                                timer <= settle;
                                state <= S_SETTLE;
                            end else begin
                                timer <= {16'd0, step_width};
                                state <= S_STEP_HIGH;
                            end
                        end else if (trig_now) begin
                            done       <= 1'b0;
                            do_trigger <= 1'b1;
                            busy       <= 1'b1;
                            timer      <= settle;
                            state      <= S_SETTLE;
                        end
                    end

                    S_STEP_HIGH: begin
                        if (limit_blocked) begin
                            step_out        <= 1'b0;
                            halted_on_limit <= 1'b1;
                            busy            <= 1'b0;
                            state           <= S_IDLE;
                        end else begin
                            step_out <= 1'b1;
                            if (timer <= 32'd1) begin
                                step_out <= 1'b0;
                                // Step complete on the falling edge.
                                position   <= dir_out ? position + 32'sd1
                                                      : position - 32'sd1;
                                steps_left <= steps_left - 32'd1;
                                timer      <= {8'd0, low_time};
                                state      <= S_STEP_LOW;
                            end else begin
                                timer <= timer - 32'd1;
                            end
                        end
                    end

                    S_STEP_LOW: begin
                        if (timer <= 32'd1) begin
                            if (steps_left == 32'd0) begin
                                timer <= settle;
                                state <= S_SETTLE;
                            end else begin
                                // Trapezoidal ramp update.
                                if (steps_left <= ramp_count) begin
                                    // Deceleration phase.
                                    if (cur_period + {8'd0, accel} < start_period)
                                        cur_period <= cur_period + {8'd0, accel};
                                    else
                                        cur_period <= start_period;
                                end else if (cur_period > min_period + {8'd0, accel}) begin
                                    // Acceleration phase.
                                    cur_period <= cur_period - {8'd0, accel};
                                    ramp_count <= ramp_count + 32'd1;
                                end else begin
                                    // Cruise.
                                    cur_period <= min_period;
                                end
                                timer <= {16'd0, step_width};
                                state <= S_STEP_HIGH;
                            end
                        end else begin
                            timer <= timer - 32'd1;
                        end
                    end

                    S_SETTLE: begin
                        if (!do_trigger) begin
                            busy  <= 1'b0;
                            done  <= 1'b1;
                            state <= S_IDLE;
                        end else begin
                            cam_focus <= 1'b1;
                            if (timer == 32'd0) begin
                                timer <= trig_width;
                                state <= S_TRIGGER;
                            end else begin
                                timer <= timer - 32'd1;
                            end
                        end
                    end

                    S_TRIGGER: begin
                        cam_shutter <= 1'b1;
                        if (timer <= 32'd1) begin
                            cam_shutter <= 1'b0;
                            cam_focus   <= 1'b0;
                            busy        <= 1'b0;
                            done        <= 1'b1;
                            state       <= S_IDLE;
                        end else begin
                            timer <= timer - 32'd1;
                        end
                    end

                    default: state <= S_IDLE;
                endcase
            end
        end
    end

endmodule

`default_nettype wire

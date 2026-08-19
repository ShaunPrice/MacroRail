// -----------------------------------------------------------------------------
// pwm_hbridge.v
//
// Sign-magnitude H-bridge PWM driver with shoot-through protection.
//
// Takes the signed control effort from pid_axis and produces the two gate
// signals an H-bridge motor driver expects (IN1/IN2 on a DRV8871, RPWM/LPWM
// on a BTS7960, and so on).
//
// Dead-time insertion is the safety-critical part: on a direction reversal
// both outputs are driven low for `deadtime` cycles before the opposite
// side turns on. Without it, the high-side FET of one leg and the low-side
// of the other can conduct simultaneously for a few hundred nanoseconds,
// shorting the battery through the bridge. That is how motor drivers die.
//
// Brake (both low-side on, motor windings shorted) decelerates hard;
// coast (all off) lets the motor freewheel. Disable coasts, because an
// unpowered robot that freewheels is safer than one that locks a wheel.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module pwm_hbridge (
    input  wire               clk,
    input  wire               rst_n,

    input  wire signed [31:0] duty,       // signed effort; |duty| >= period is full scale
    input  wire        [15:0] period,     // PWM period in clk cycles
    input  wire        [15:0] deadtime,   // reversal blanking in clk cycles
    input  wire               enable,     // low = coast
    input  wire               brake,      // high = short the windings

    output reg                in1,
    output reg                in2,
    output reg         [15:0] duty_applied,
    output reg                dir_applied  // 1 = reverse
);

    // ------------------------------------------------------------------
    // Magnitude and requested direction
    // ------------------------------------------------------------------
    wire               dir_req = duty[31];
    wire signed [31:0] mag_s   = dir_req ? -duty : duty;
    wire        [15:0] mag     = (mag_s >= $signed({16'd0, period}))
                                 ? period : mag_s[15:0];

    // ------------------------------------------------------------------
    // PWM carrier
    // ------------------------------------------------------------------
    reg [15:0] counter;
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n)
            counter <= 16'd0;
        else if (counter >= period - 16'd1 || period == 16'd0)
            counter <= 16'd0;
        else
            counter <= counter + 16'd1;
    end
    wire pwm_on = (counter < duty_applied) && (duty_applied != 16'd0);

    // ------------------------------------------------------------------
    // Direction change with dead-time blanking
    // ------------------------------------------------------------------
    localparam [1:0] S_RUN = 2'd0, S_BLANK = 2'd1;

    reg [1:0]  state;
    reg [15:0] blank_count;

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            state        <= S_RUN;
            blank_count  <= 16'd0;
            dir_applied  <= 1'b0;
            duty_applied <= 16'd0;
            in1          <= 1'b0;
            in2          <= 1'b0;
        end else if (!enable) begin
            // Coast: both sides off, and re-arm the blanking so the next
            // enable cannot reverse straight into a conducting bridge.
            in1          <= 1'b0;
            in2          <= 1'b0;
            duty_applied <= 16'd0;
            state        <= S_BLANK;
            blank_count  <= deadtime;
        end else if (brake) begin
            in1          <= 1'b1;
            in2          <= 1'b1;
            duty_applied <= 16'd0;
        end else begin
            case (state)
                S_RUN: begin
                    if (dir_req != dir_applied && mag != 16'd0) begin
                        // Reversal requested: blank both sides first.
                        in1          <= 1'b0;
                        in2          <= 1'b0;
                        duty_applied <= 16'd0;
                        blank_count  <= deadtime;
                        state        <= S_BLANK;
                    end else begin
                        duty_applied <= mag;
                        in1 <= (!dir_applied) && pwm_on;
                        in2 <= ( dir_applied) && pwm_on;
                    end
                end

                S_BLANK: begin
                    in1 <= 1'b0;
                    in2 <= 1'b0;
                    if (blank_count == 16'd0) begin
                        dir_applied <= dir_req;
                        state       <= S_RUN;
                    end else begin
                        blank_count <= blank_count - 16'd1;
                    end
                end

                default: state <= S_RUN;
            endcase
        end
    end

endmodule

`default_nettype wire

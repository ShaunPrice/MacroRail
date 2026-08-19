// -----------------------------------------------------------------------------
// motion_axis.v
//
// One complete closed-loop axis: quadrature feedback -> setpoint clamping ->
// PID -> H-bridge PWM, with the safety gates applied last so nothing
// upstream can bypass them.
//
// The clamp order matters and is deliberate:
//   1. the requested setpoint is limited to the (lockable) envelope,
//   2. then zeroed if it drives into an asserted limit switch,
//   3. then the PID runs against the clamped value,
//   4. then the output stage is gated by motion_permitted / brake_cmd.
// A setpoint that survives to the motor has passed all four.
//
// Velocity mode is the normal choice for a wheeled robot (a /cmd_vel style
// interface). Position mode drives a joint, a lead screw, or the MacroRail
// camera carriage to an absolute count.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module motion_axis (
    input  wire               clk,
    input  wire               rst_n,
    input  wire               sample_tick,

    // Encoder
    input  wire               enc_a,
    input  wire               enc_b,
    input  wire               enc_z,

    // Limit / bumper inputs, active high after polarity correction upstream
    input  wire               limit_fwd,
    input  wire               limit_rev,

    // Command
    input  wire signed [31:0] setpoint,
    input  wire               mode_position,   // 0 = velocity, 1 = position
    input  wire               invert_enc,
    input  wire               zero_pos,
    input  wire               index_en,

    // Gains and envelope
    input  wire        [31:0] kp,              // Q16.16
    input  wire        [31:0] ki,              // Q16.16
    input  wire        [31:0] kd,              // Q16.16
    input  wire        [31:0] i_max,
    input  wire        [31:0] out_max,
    input  wire        [31:0] setpoint_limit,  // lockable envelope

    // PWM configuration
    input  wire        [15:0] pwm_period,
    input  wire        [15:0] deadtime,

    // Safety arbitration from safety_core
    input  wire               motion_permitted,
    input  wire               brake_cmd,

    // Motor outputs
    output wire               in1,
    output wire               in2,

    // Telemetry
    output wire signed [31:0] position,
    output wire signed [31:0] velocity,
    output wire        [15:0] duty_applied,
    output wire signed [31:0] error,
    output wire               saturated,
    output wire               enc_error,
    output wire               index_seen,
    output wire               clamped          // setpoint was limited
);

    // ------------------------------------------------------------------
    // Feedback
    // ------------------------------------------------------------------
    quad_decoder #(.FILTER_LEN(4)) u_enc (
        .clk           (clk),
        .rst_n         (rst_n),
        .enc_a         (enc_a),
        .enc_b         (enc_b),
        .enc_z         (enc_z),
        .zero_pos      (zero_pos),
        .index_en      (index_en),
        .invert        (invert_enc),
        .sample_tick   (sample_tick),
        .position      (position),
        .velocity      (velocity),
        .index_seen    (index_seen),
        .error_illegal (enc_error)
    );

    wire signed [31:0] measurement = mode_position ? position : velocity;

    // ------------------------------------------------------------------
    // Setpoint clamping - step 1: the lockable envelope
    // ------------------------------------------------------------------
    wire signed [31:0] limit_s = $signed({1'b0, setpoint_limit[30:0]});

    wire over_pos = (setpoint >  limit_s);
    wire over_neg = (setpoint < -limit_s);
    wire signed [31:0] sp_enveloped = over_pos ?  limit_s :
                                      over_neg ? -limit_s : setpoint;

    // ------------------------------------------------------------------
    // Setpoint clamping - step 2: limit switches block the offending
    // direction only, so the robot can always be driven back off a limit.
    // ------------------------------------------------------------------
    wire block_fwd = limit_fwd && (sp_enveloped > 0);
    wire block_rev = limit_rev && (sp_enveloped < 0);
    wire signed [31:0] sp_safe = (block_fwd || block_rev) ? 32'sd0 : sp_enveloped;

    assign clamped = over_pos || over_neg || block_fwd || block_rev;

    // ------------------------------------------------------------------
    // Control
    // ------------------------------------------------------------------
    wire signed [31:0] pid_out;

    pid_axis u_pid (
        .clk         (clk),
        .rst_n       (rst_n),
        .sample_tick (sample_tick),
        .enable      (motion_permitted),
        .setpoint    (sp_safe),
        .measurement (measurement),
        .kp          (kp),
        .ki          (ki),
        .kd          (kd),
        .i_max       (i_max),
        .out_max     (out_max),
        .out         (pid_out),
        .out_valid   (),
        .error_out   (error),
        .saturated   (saturated)
    );

    // ------------------------------------------------------------------
    // Output stage - the last gate. Even a wild PID output cannot reach
    // the bridge unless motion_permitted is high.
    // ------------------------------------------------------------------
    pwm_hbridge u_pwm (
        .clk          (clk),
        .rst_n        (rst_n),
        .duty         (motion_permitted ? pid_out : 32'sd0),
        .period       (pwm_period),
        .deadtime     (deadtime),
        .enable       (motion_permitted),
        .brake        (brake_cmd),
        .in1          (in1),
        .in2          (in2),
        .duty_applied (duty_applied),
        .dir_applied  ()
    );

endmodule

`default_nettype wire

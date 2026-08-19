// -----------------------------------------------------------------------------
// safety_core.v
//
// The deterministic layer that AI cannot argue with.
//
// The standard robotics architecture is: a slow LLM/planner proposes goals,
// a mid-rate perception/navigation stack turns them into setpoints, and a
// fast loop enforces hard limits. On a Pi-plus-microcontroller robot that
// boundary is a convention - the fast loop is firmware, and firmware can be
// reflashed by whatever is running upstream. Here the boundary is silicon.
//
// Three guarantees, in order of importance:
//
//  1. E-STOP. A hardware pin. Asserting it latches every axis into brake
//     within one clock, with no software in the path. Clearing the latch
//     requires BOTH a deliberate software write AND the physical button
//     released, so a crashed-and-restarted process cannot re-arm the robot
//     by itself.
//
//  2. COMMAND WATCHDOG. Software must refresh a setpoint every
//     `watchdog_timeout` cycles. If the Linux side hangs, panics, gets OOM
//     killed, or simply loses its network link, the motors stop. This is
//     the single most valuable safety feature on any robot that carries its
//     own computer, and it is the one most often left out.
//
//  3. LIMIT LOCK. Once `lock_req` is pulsed, the velocity/effort ceilings
//     become read-only until a full PL reset (i.e. a power cycle or an
//     explicit bitstream-level reset). Boot code sets a sane envelope and
//     locks it; from then on nothing running on Linux - a bug, a bad model
//     output, or a compromised process - can raise its own speed limit.
//
// Everything above operates whether or not the processor is even running.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module safety_core (
    input  wire        clk,
    input  wire        rst_n,

    // Physical E-stop, active LOW when pressed (fail-safe: a cut wire or a
    // pulled connector reads as pressed).
    input  wire        estop_n,

    // Software interface
    input  wire        enable_req,        // master enable from the PS
    input  wire        pet,               // pulses when a setpoint is written
    input  wire [31:0] watchdog_timeout,  // clk cycles; 0 disables the watchdog
    input  wire        estop_clear,       // deliberate write to clear the latch
    input  wire        lock_req,          // one-way: freeze the limit registers

    // Status / arbitration
    output reg         estop_latched,
    output reg         watchdog_tripped,
    output reg         locked,
    output wire        motion_permitted,  // gate on every axis output stage
    output wire        brake_cmd,
    output wire        estop_raw          // live (synchronised) button state
);

    // ------------------------------------------------------------------
    // Synchronise the E-stop pin. Two flops; the latch below is what makes
    // a momentary press stick, so no edge can be missed downstream.
    // ------------------------------------------------------------------
    reg [1:0] estop_sync;
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) estop_sync <= 2'b11;      // assume released until sampled
        else        estop_sync <= {estop_sync[0], estop_n};
    end
    wire estop_pressed = ~estop_sync[1];
    assign estop_raw = estop_pressed;

    // ------------------------------------------------------------------
    // E-stop latch
    // ------------------------------------------------------------------
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n)
            estop_latched <= 1'b0;
        else if (estop_pressed)
            estop_latched <= 1'b1;            // set dominates
        else if (estop_clear && !estop_pressed)
            estop_latched <= 1'b0;
    end

    // ------------------------------------------------------------------
    // Command watchdog
    // ------------------------------------------------------------------
    reg [31:0] wd_count;
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            wd_count         <= 32'd0;
            watchdog_tripped <= 1'b0;
        end else if (watchdog_timeout == 32'd0) begin
            // Watchdog disabled (bench work only - never for a mobile robot).
            wd_count         <= 32'd0;
            watchdog_tripped <= 1'b0;
        end else if (pet) begin
            // A fresh command both resets the timer and releases the trip,
            // so a robot recovers on its own when commands resume.
            wd_count         <= 32'd0;
            watchdog_tripped <= 1'b0;
        end else if (wd_count >= watchdog_timeout) begin
            watchdog_tripped <= 1'b1;
        end else begin
            wd_count <= wd_count + 32'd1;
        end
    end

    // ------------------------------------------------------------------
    // One-way limit lock
    // ------------------------------------------------------------------
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n)        locked <= 1'b0;
        else if (lock_req) locked <= 1'b1;   // no path back except reset
    end

    // ------------------------------------------------------------------
    // Arbitration
    //
    // Braking (rather than coasting) on a watchdog trip is the right
    // default for a ground robot: a rover that keeps rolling after losing
    // its brain travels a lot further than one that stops hard. Flip this
    // for a boat or an arm on a lead screw, where coasting is gentler.
    // ------------------------------------------------------------------
    assign motion_permitted = enable_req && !estop_latched && !watchdog_tripped;
    assign brake_cmd        = estop_latched || watchdog_tripped;

endmodule

`default_nettype wire

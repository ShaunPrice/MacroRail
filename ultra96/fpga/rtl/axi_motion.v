// -----------------------------------------------------------------------------
// axi_motion.v
//
// AXI4-Lite front end for a two-axis closed-loop motion controller with a
// hardware safety core. Attaches to the Zynq UltraScale+ PS master port
// (M_AXI_HPM0_FPD) on the Ultra96-V2.
//
// Register map (byte offsets)
// ---------------------------------------------------------------------
// Global
//   0x00 GLOBAL_CTRL    W   bit0 ENABLE, bit1 ESTOP_CLEAR, bit2 LOCK_LIMITS
//   0x04 GLOBAL_STATUS  R   bit0 estop_latched, bit1 watchdog_tripped,
//                           bit2 locked, bit3 estop_raw, bit4 motion_permitted
//   0x08 WATCHDOG_TO    RW  clk cycles; 0 disables            [LOCKABLE]
//   0x0C SAMPLE_DIV     RW  clk cycles per control tick
//   0x10 PWM_PERIOD     RW  clk cycles
//   0x14 DEADTIME       RW  clk cycles                        [LOCKABLE]
//   0x18 SETPOINT_LIMIT RW  magnitude ceiling, both axes      [LOCKABLE]
//                           (velocity mode: Q24.8 counts/period)
//   0x1C ID             R   0x4D523200 ("MR2" + rev 0)
//
// Per axis: axis 0 at 0x40, axis 1 at 0x80
//   +0x00 SETPOINT      RW  writing also pets the watchdog
//   +0x04 KP            RW  Q16.16 (1.0 == 65536)
//   +0x08 KI            RW  Q16.16
//   +0x0C KD            RW  Q16.16
//   +0x10 I_MAX         RW
//   +0x14 OUT_MAX       RW
//   +0x18 AXIS_CTRL     RW  bit0 mode_position, bit1 invert_enc,
//                           bit2 index_en, bit3 zero_position (strobe)
//   +0x1C POSITION      R   signed counts
//   +0x20 VELOCITY      R   signed Q24.8 counts per control period
//   +0x24 ERROR         R   signed
//   +0x28 DUTY          R   applied PWM compare value
//   +0x2C AXIS_STATUS   R   bit0 saturated, bit1 enc_error, bit2 index_seen,
//                           bit3 clamped, bit4 limit_fwd, bit5 limit_rev
//
// Registers marked [LOCKABLE] stop accepting writes once GLOBAL_CTRL bit2
// has been pulsed, until the PL is reset. Set the envelope at boot, lock
// it, and no later software fault can widen it.
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module axi_motion #(
    parameter C_S_AXI_ADDR_WIDTH = 9
) (
    input  wire                          s_axi_aclk,
    input  wire                          s_axi_aresetn,

    input  wire [C_S_AXI_ADDR_WIDTH-1:0] s_axi_awaddr,
    input  wire                          s_axi_awvalid,
    output reg                           s_axi_awready,
    input  wire [31:0]                   s_axi_wdata,
    input  wire [3:0]                    s_axi_wstrb,
    input  wire                          s_axi_wvalid,
    output reg                           s_axi_wready,
    output reg  [1:0]                    s_axi_bresp,
    output reg                           s_axi_bvalid,
    input  wire                          s_axi_bready,
    input  wire [C_S_AXI_ADDR_WIDTH-1:0] s_axi_araddr,
    input  wire                          s_axi_arvalid,
    output reg                           s_axi_arready,
    output reg  [31:0]                   s_axi_rdata,
    output reg  [1:0]                    s_axi_rresp,
    output reg                           s_axi_rvalid,
    input  wire                          s_axi_rready,

    // Physical pins
    input  wire                          estop_n,
    input  wire                          enc0_a, enc0_b, enc0_z,
    input  wire                          enc1_a, enc1_b, enc1_z,
    input  wire                          limit0_fwd, limit0_rev,
    input  wire                          limit1_fwd, limit1_rev,
    output wire                          m0_in1, m0_in2,
    output wire                          m1_in1, m1_in2,

    // Interrupt: asserted while the safety core is inhibiting motion
    output wire                          irq
);

    localparam ID_VALUE = 32'h4D52_3200;

    // ------------------------------------------------------------------
    // Global configuration
    // ------------------------------------------------------------------
    reg        cfg_enable;
    reg [31:0] cfg_watchdog_to;
    reg [31:0] cfg_sample_div;
    reg [15:0] cfg_pwm_period;
    reg [15:0] cfg_deadtime;
    reg [31:0] cfg_setpoint_limit;

    reg        strobe_estop_clear;
    reg        strobe_lock;
    reg        strobe_pet;

    // Per-axis configuration
    reg signed [31:0] ax_setpoint [0:1];
    reg        [31:0] ax_kp       [0:1];
    reg        [31:0] ax_ki       [0:1];
    reg        [31:0] ax_kd       [0:1];
    reg        [31:0] ax_i_max    [0:1];
    reg        [31:0] ax_out_max  [0:1];
    reg        [3:0]  ax_ctrl     [0:1];
    reg        [1:0]  ax_zero;

    // Per-axis telemetry
    wire signed [31:0] ax_position [0:1];
    wire signed [31:0] ax_velocity [0:1];
    wire signed [31:0] ax_error    [0:1];
    wire        [15:0] ax_duty     [0:1];
    wire               ax_sat      [0:1];
    wire               ax_encerr   [0:1];
    wire               ax_index    [0:1];
    wire               ax_clamped  [0:1];

    // ------------------------------------------------------------------
    // Control-loop rate generator
    // ------------------------------------------------------------------
    reg [31:0] sample_count;
    reg        sample_tick;
    always @(posedge s_axi_aclk or negedge s_axi_aresetn) begin
        if (!s_axi_aresetn) begin
            sample_count <= 32'd0;
            sample_tick  <= 1'b0;
        end else if (cfg_sample_div == 32'd0) begin
            sample_count <= 32'd0;
            sample_tick  <= 1'b0;
        end else if (sample_count >= cfg_sample_div - 32'd1) begin
            sample_count <= 32'd0;
            sample_tick  <= 1'b1;
        end else begin
            sample_count <= sample_count + 32'd1;
            sample_tick  <= 1'b0;
        end
    end

    // ------------------------------------------------------------------
    // Safety core
    // ------------------------------------------------------------------
    wire safety_estop_latched, safety_wd_tripped, safety_locked;
    wire safety_permitted, safety_brake, safety_estop_raw;

    safety_core u_safety (
        .clk              (s_axi_aclk),
        .rst_n            (s_axi_aresetn),
        .estop_n          (estop_n),
        .enable_req       (cfg_enable),
        .pet              (strobe_pet),
        .watchdog_timeout (cfg_watchdog_to),
        .estop_clear      (strobe_estop_clear),
        .lock_req         (strobe_lock),
        .estop_latched    (safety_estop_latched),
        .watchdog_tripped (safety_wd_tripped),
        .locked           (safety_locked),
        .motion_permitted (safety_permitted),
        .brake_cmd        (safety_brake),
        .estop_raw        (safety_estop_raw)
    );

    assign irq = safety_estop_latched || safety_wd_tripped;

    // ------------------------------------------------------------------
    // Axes
    // ------------------------------------------------------------------
    wire [1:0] enc_a  = {enc1_a, enc0_a};
    wire [1:0] enc_b  = {enc1_b, enc0_b};
    wire [1:0] enc_z  = {enc1_z, enc0_z};
    wire [1:0] lim_f  = {limit1_fwd, limit0_fwd};
    wire [1:0] lim_r  = {limit1_rev, limit0_rev};
    wire [1:0] mot_i1, mot_i2;

    genvar gi;
    generate
        for (gi = 0; gi < 2; gi = gi + 1) begin : g_axis
            motion_axis u_axis (
                .clk              (s_axi_aclk),
                .rst_n            (s_axi_aresetn),
                .sample_tick      (sample_tick),
                .enc_a            (enc_a[gi]),
                .enc_b            (enc_b[gi]),
                .enc_z            (enc_z[gi]),
                .limit_fwd        (lim_f[gi]),
                .limit_rev        (lim_r[gi]),
                .setpoint         (ax_setpoint[gi]),
                .mode_position    (ax_ctrl[gi][0]),
                .invert_enc       (ax_ctrl[gi][1]),
                .zero_pos         (ax_zero[gi]),
                .index_en         (ax_ctrl[gi][2]),
                .kp               (ax_kp[gi]),
                .ki               (ax_ki[gi]),
                .kd               (ax_kd[gi]),
                .i_max            (ax_i_max[gi]),
                .out_max          (ax_out_max[gi]),
                .setpoint_limit   (cfg_setpoint_limit),
                .pwm_period       (cfg_pwm_period),
                .deadtime         (cfg_deadtime),
                .motion_permitted (safety_permitted),
                .brake_cmd        (safety_brake),
                .in1              (mot_i1[gi]),
                .in2              (mot_i2[gi]),
                .position         (ax_position[gi]),
                .velocity         (ax_velocity[gi]),
                .duty_applied     (ax_duty[gi]),
                .error            (ax_error[gi]),
                .saturated        (ax_sat[gi]),
                .enc_error        (ax_encerr[gi]),
                .index_seen       (ax_index[gi]),
                .clamped          (ax_clamped[gi])
            );
        end
    endgenerate

    assign m0_in1 = mot_i1[0];
    assign m0_in2 = mot_i2[0];
    assign m1_in1 = mot_i1[1];
    assign m1_in2 = mot_i2[1];

    // ------------------------------------------------------------------
    // AXI4-Lite write channel
    // ------------------------------------------------------------------
    reg [C_S_AXI_ADDR_WIDTH-1:0] awaddr_q;
    reg [31:0] wdata_q;
    reg        aw_seen, w_seen;
    wire       write_go = aw_seen && w_seen && !s_axi_bvalid;

    // Regions: 0x00 global, 0x40 axis 0, 0x80 axis 1. The axis index is
    // bit 7 (0x40 -> 0, 0x80 -> 1), not bit 6.
    wire [1:0] w_region = awaddr_q[7:6];
    wire [3:0] w_index  = awaddr_q[5:2];
    wire       w_axis   = awaddr_q[7];     // valid when w_region != 0

    integer i;

    always @(posedge s_axi_aclk or negedge s_axi_aresetn) begin
        if (!s_axi_aresetn) begin
            s_axi_awready <= 1'b0;
            s_axi_wready  <= 1'b0;
            s_axi_bvalid  <= 1'b0;
            s_axi_bresp   <= 2'b00;
            aw_seen       <= 1'b0;
            w_seen        <= 1'b0;
            awaddr_q      <= {C_S_AXI_ADDR_WIDTH{1'b0}};
            wdata_q       <= 32'd0;

            cfg_enable         <= 1'b0;
            cfg_watchdog_to    <= 32'd50_000_000;  // 500 ms at 100 MHz
            cfg_sample_div     <= 32'd100_000;     // 1 kHz control loop
            cfg_pwm_period     <= 16'd5000;        // 20 kHz PWM
            cfg_deadtime       <= 16'd100;         // 1 us
            cfg_setpoint_limit <= 32'd1000;

            strobe_estop_clear <= 1'b0;
            strobe_lock        <= 1'b0;
            strobe_pet         <= 1'b0;
            ax_zero            <= 2'b00;

            for (i = 0; i < 2; i = i + 1) begin
                ax_setpoint[i] <= 32'sd0;
                ax_kp[i]       <= 32'd65536;  // 1.0 in Q16.16
                ax_ki[i]       <= 32'd0;
                ax_kd[i]       <= 32'd0;
                ax_i_max[i]    <= 32'd100_000;
                ax_out_max[i]  <= 32'd5000;
                ax_ctrl[i]     <= 4'd0;
            end
        end else begin
            strobe_estop_clear <= 1'b0;
            strobe_lock        <= 1'b0;
            strobe_pet         <= 1'b0;
            ax_zero            <= 2'b00;

            s_axi_awready <= 1'b0;
            if (s_axi_awvalid && !aw_seen) begin
                s_axi_awready <= 1'b1;
                awaddr_q      <= s_axi_awaddr;
                aw_seen       <= 1'b1;
            end

            s_axi_wready <= 1'b0;
            if (s_axi_wvalid && !w_seen) begin
                s_axi_wready <= 1'b1;
                wdata_q      <= s_axi_wdata;
                w_seen       <= 1'b1;
            end

            if (write_go) begin
                aw_seen      <= 1'b0;
                w_seen       <= 1'b0;
                s_axi_bvalid <= 1'b1;
                s_axi_bresp  <= 2'b00;

                if (w_region == 2'b00) begin
                    case (w_index)
                        4'h0: begin
                            cfg_enable         <= wdata_q[0];
                            strobe_estop_clear <= wdata_q[1];
                            strobe_lock        <= wdata_q[2];
                        end
                        // Lockable registers: writes are dropped once locked.
                        4'h2: if (!safety_locked) cfg_watchdog_to    <= wdata_q;
                        4'h3:                     cfg_sample_div     <= wdata_q;
                        4'h4:                     cfg_pwm_period     <= wdata_q[15:0];
                        4'h5: if (!safety_locked) cfg_deadtime       <= wdata_q[15:0];
                        4'h6: if (!safety_locked) cfg_setpoint_limit <= wdata_q;
                        default: ;
                    endcase
                end else begin
                    case (w_index)
                        4'h0: begin
                            ax_setpoint[w_axis] <= wdata_q;
                            strobe_pet          <= 1'b1;
                        end
                        4'h1: ax_kp[w_axis]      <= wdata_q;
                        4'h2: ax_ki[w_axis]      <= wdata_q;
                        4'h3: ax_kd[w_axis]      <= wdata_q;
                        4'h4: ax_i_max[w_axis]   <= wdata_q;
                        4'h5: ax_out_max[w_axis] <= wdata_q;
                        4'h6: begin
                            ax_ctrl[w_axis]  <= wdata_q[3:0];
                            ax_zero[w_axis]  <= wdata_q[3];
                        end
                        default: ;
                    endcase
                end
            end else if (s_axi_bvalid && s_axi_bready) begin
                s_axi_bvalid <= 1'b0;
            end
        end
    end

    // ------------------------------------------------------------------
    // AXI4-Lite read channel
    // ------------------------------------------------------------------
    wire [1:0] r_region = s_axi_araddr[7:6];
    wire [3:0] r_index  = s_axi_araddr[5:2];
    wire       r_axis   = s_axi_araddr[7];

    always @(posedge s_axi_aclk or negedge s_axi_aresetn) begin
        if (!s_axi_aresetn) begin
            s_axi_arready <= 1'b0;
            s_axi_rvalid  <= 1'b0;
            s_axi_rresp   <= 2'b00;
            s_axi_rdata   <= 32'd0;
        end else begin
            s_axi_arready <= 1'b0;
            if (s_axi_arvalid && !s_axi_rvalid && !s_axi_arready) begin
                s_axi_arready <= 1'b1;
                s_axi_rvalid  <= 1'b1;
                s_axi_rresp   <= 2'b00;

                if (r_region == 2'b00) begin
                    case (r_index)
                        4'h0: s_axi_rdata <= {31'd0, cfg_enable};
                        4'h1: s_axi_rdata <= {27'd0, safety_permitted,
                                              safety_estop_raw, safety_locked,
                                              safety_wd_tripped, safety_estop_latched};
                        4'h2: s_axi_rdata <= cfg_watchdog_to;
                        4'h3: s_axi_rdata <= cfg_sample_div;
                        4'h4: s_axi_rdata <= {16'd0, cfg_pwm_period};
                        4'h5: s_axi_rdata <= {16'd0, cfg_deadtime};
                        4'h6: s_axi_rdata <= cfg_setpoint_limit;
                        4'h7: s_axi_rdata <= ID_VALUE;
                        default: s_axi_rdata <= 32'd0;
                    endcase
                end else begin
                    case (r_index)
                        4'h0: s_axi_rdata <= ax_setpoint[r_axis];
                        4'h1: s_axi_rdata <= ax_kp[r_axis];
                        4'h2: s_axi_rdata <= ax_ki[r_axis];
                        4'h3: s_axi_rdata <= ax_kd[r_axis];
                        4'h4: s_axi_rdata <= ax_i_max[r_axis];
                        4'h5: s_axi_rdata <= ax_out_max[r_axis];
                        4'h6: s_axi_rdata <= {28'd0, ax_ctrl[r_axis]};
                        4'h7: s_axi_rdata <= ax_position[r_axis];
                        4'h8: s_axi_rdata <= ax_velocity[r_axis];
                        4'h9: s_axi_rdata <= ax_error[r_axis];
                        4'hA: s_axi_rdata <= {16'd0, ax_duty[r_axis]};
                        4'hB: s_axi_rdata <= {26'd0, lim_r[r_axis], lim_f[r_axis],
                                              ax_clamped[r_axis], ax_index[r_axis],
                                              ax_encerr[r_axis], ax_sat[r_axis]};
                        default: s_axi_rdata <= 32'd0;
                    endcase
                end
            end else if (s_axi_rvalid && s_axi_rready) begin
                s_axi_rvalid <= 1'b0;
            end
        end
    end

endmodule

`default_nettype wire

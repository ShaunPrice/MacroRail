// -----------------------------------------------------------------------------
// axi_stepper.v
//
// AXI4-Lite register interface for stepper_ctrl. Connects to the Zynq
// UltraScale+ PS master port (M_AXI_HPM0_FPD on the Ultra96-V2) so the
// Linux software can command moves through /dev/mem or a UIO driver.
//
// Register map (byte offsets, 32-bit registers):
//   0x00  CTRL        W    bit0 START, bit1 ABORT, bit2 TRIG_NOW, bit3 ZERO_POS
//   0x04  CONFIG      RW   bit0 DIR, bit1 TRIGGER_EN, bit2 LIMIT_POLARITY,
//                          bit3 MOTOR_EN (drives the driver's enable pin)
//   0x08  STATUS      R    bit0 BUSY, bit1 DONE, bit2 LIMIT_FWD, bit3 LIMIT_REV,
//                          bit4 HALTED_ON_LIMIT
//   0x0C  STEPS       RW   steps per move
//   0x10  START_PER   RW   step period at ramp start (clk cycles)
//   0x14  MIN_PER     RW   step period at full speed (clk cycles)
//   0x18  ACCEL       RW   period decrement per step (clk cycles)
//   0x1C  STEP_WIDTH  RW   STEP high time (clk cycles)
//   0x20  SETTLE      RW   post-move settle before trigger (clk cycles)
//   0x24  TRIG_WIDTH  RW   shutter pulse width (clk cycles)
//   0x28  POSITION    R    signed step counter
//   0x2C  ID          R    0x4D520100 ("MR" + version 1.0)
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps
`default_nettype none

module axi_stepper #(
    parameter C_S_AXI_ADDR_WIDTH = 6
) (
    input  wire                              s_axi_aclk,
    input  wire                              s_axi_aresetn,

    input  wire [C_S_AXI_ADDR_WIDTH-1:0]     s_axi_awaddr,
    input  wire                              s_axi_awvalid,
    output reg                               s_axi_awready,
    input  wire [31:0]                       s_axi_wdata,
    input  wire [3:0]                        s_axi_wstrb,
    input  wire                              s_axi_wvalid,
    output reg                               s_axi_wready,
    output reg  [1:0]                        s_axi_bresp,
    output reg                               s_axi_bvalid,
    input  wire                              s_axi_bready,
    input  wire [C_S_AXI_ADDR_WIDTH-1:0]     s_axi_araddr,
    input  wire                              s_axi_arvalid,
    output reg                               s_axi_arready,
    output reg  [31:0]                       s_axi_rdata,
    output reg  [1:0]                        s_axi_rresp,
    output reg                               s_axi_rvalid,
    input  wire                              s_axi_rready,

    // Physical pins
    input  wire                              limit_fwd,
    input  wire                              limit_rev,
    output wire                              step_out,
    output wire                              dir_out,
    output wire                              motor_en,
    output wire                              cam_focus,
    output wire                              cam_shutter,

    // Interrupt on move complete (optional, level until CTRL write)
    output wire                              irq
);

    localparam ID_VALUE = 32'h4D52_0100;

    // ------------------------------------------------------------------
    // Configuration registers
    // ------------------------------------------------------------------
    reg [31:0] reg_steps;
    reg [23:0] reg_start_period;
    reg [23:0] reg_min_period;
    reg [15:0] reg_accel;
    reg [15:0] reg_step_width;
    reg [31:0] reg_settle;
    reg [31:0] reg_trig_width;
    reg        cfg_dir;
    reg        cfg_trigger_en;
    reg        cfg_limit_polarity;
    reg        cfg_motor_en;

    reg        cmd_start, cmd_abort, cmd_trig_now, cmd_zero_pos;

    wire        core_busy, core_done, core_halted;
    wire        core_limit_fwd, core_limit_rev;
    wire signed [31:0] core_position;

    stepper_ctrl u_core (
        .clk              (s_axi_aclk),
        .rst_n            (s_axi_aresetn),
        .start            (cmd_start),
        .abort            (cmd_abort),
        .trig_now         (cmd_trig_now),
        .zero_pos         (cmd_zero_pos),
        .steps            (reg_steps),
        .start_period     (reg_start_period),
        .min_period       (reg_min_period),
        .accel            (reg_accel),
        .step_width       (reg_step_width),
        .settle           (reg_settle),
        .trig_width       (reg_trig_width),
        .dir              (cfg_dir),
        .trigger_en       (cfg_trigger_en),
        .limit_polarity   (cfg_limit_polarity),
        .limit_fwd        (limit_fwd),
        .limit_rev        (limit_rev),
        .busy             (core_busy),
        .done             (core_done),
        .halted_on_limit  (core_halted),
        .limit_fwd_active (core_limit_fwd),
        .limit_rev_active (core_limit_rev),
        .position         (core_position),
        .step_out         (step_out),
        .dir_out          (dir_out),
        .cam_focus        (cam_focus),
        .cam_shutter      (cam_shutter)
    );

    assign motor_en = cfg_motor_en;
    assign irq      = core_done;

    // ------------------------------------------------------------------
    // AXI4-Lite write channel
    // ------------------------------------------------------------------
    reg [C_S_AXI_ADDR_WIDTH-1:0] awaddr_q;
    reg aw_seen, w_seen;
    reg [31:0] wdata_q;

    wire write_go = aw_seen && w_seen && !s_axi_bvalid;

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
            cmd_start     <= 1'b0;
            cmd_abort     <= 1'b0;
            cmd_trig_now  <= 1'b0;
            cmd_zero_pos  <= 1'b0;
            reg_steps        <= 32'd0;
            reg_start_period <= 24'd20000;   // 5 kHz at 100 MHz
            reg_min_period   <= 24'd10000;   // 10 kHz at 100 MHz
            reg_accel        <= 16'd10;
            reg_step_width   <= 16'd500;     // 5 us
            reg_settle       <= 32'd0;
            reg_trig_width   <= 32'd0;
            cfg_dir            <= 1'b0;
            cfg_trigger_en     <= 1'b0;
            cfg_limit_polarity <= 1'b1;
            cfg_motor_en       <= 1'b0;
        end else begin
            // Command strobes are single-cycle.
            cmd_start    <= 1'b0;
            cmd_abort    <= 1'b0;
            cmd_trig_now <= 1'b0;
            cmd_zero_pos <= 1'b0;

            // Address handshake
            s_axi_awready <= 1'b0;
            if (s_axi_awvalid && !aw_seen) begin
                s_axi_awready <= 1'b1;
                awaddr_q      <= s_axi_awaddr;
                aw_seen       <= 1'b1;
            end

            // Data handshake
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
                case (awaddr_q[5:2])
                    4'h0: begin // CTRL
                        cmd_start    <= wdata_q[0];
                        cmd_abort    <= wdata_q[1];
                        cmd_trig_now <= wdata_q[2];
                        cmd_zero_pos <= wdata_q[3];
                    end
                    4'h1: begin // CONFIG
                        cfg_dir            <= wdata_q[0];
                        cfg_trigger_en     <= wdata_q[1];
                        cfg_limit_polarity <= wdata_q[2];
                        cfg_motor_en       <= wdata_q[3];
                    end
                    4'h3: reg_steps        <= wdata_q;
                    4'h4: reg_start_period <= wdata_q[23:0];
                    4'h5: reg_min_period   <= wdata_q[23:0];
                    4'h6: reg_accel        <= wdata_q[15:0];
                    4'h7: reg_step_width   <= wdata_q[15:0];
                    4'h8: reg_settle       <= wdata_q;
                    4'h9: reg_trig_width   <= wdata_q;
                    default: ; // read-only or unmapped: accept and ignore
                endcase
            end else if (s_axi_bvalid && s_axi_bready) begin
                s_axi_bvalid <= 1'b0;
            end
        end
    end

    // ------------------------------------------------------------------
    // AXI4-Lite read channel
    // ------------------------------------------------------------------
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
                case (s_axi_araddr[5:2])
                    4'h1: s_axi_rdata <= {28'd0, cfg_motor_en, cfg_limit_polarity,
                                          cfg_trigger_en, cfg_dir};
                    4'h2: s_axi_rdata <= {27'd0, core_halted, core_limit_rev,
                                          core_limit_fwd, core_done, core_busy};
                    4'h3: s_axi_rdata <= reg_steps;
                    4'h4: s_axi_rdata <= {8'd0, reg_start_period};
                    4'h5: s_axi_rdata <= {8'd0, reg_min_period};
                    4'h6: s_axi_rdata <= {16'd0, reg_accel};
                    4'h7: s_axi_rdata <= {16'd0, reg_step_width};
                    4'h8: s_axi_rdata <= reg_settle;
                    4'h9: s_axi_rdata <= reg_trig_width;
                    4'hA: s_axi_rdata <= core_position;
                    4'hB: s_axi_rdata <= ID_VALUE;
                    default: s_axi_rdata <= 32'd0;
                endcase
            end else if (s_axi_rvalid && s_axi_rready) begin
                s_axi_rvalid <= 1'b0;
            end
        end
    end

endmodule

`default_nettype wire

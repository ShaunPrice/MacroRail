// -----------------------------------------------------------------------------
// tb_axi_stepper.v
//
// Self-checking testbench for the MacroRail stepper controller, driven
// through its AXI4-Lite interface exactly as the Linux software would.
//
// Run with Icarus Verilog:
//   iverilog -g2012 -o tb ultra96/fpga/rtl/stepper_ctrl.v \
//       ultra96/fpga/rtl/axi_stepper.v ultra96/fpga/sim/tb_axi_stepper.v
//   vvp tb
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps

module tb_axi_stepper;

    // Register offsets
    localparam CTRL       = 6'h00;
    localparam CONFIG     = 6'h04;
    localparam STATUS     = 6'h08;
    localparam STEPS      = 6'h0C;
    localparam START_PER  = 6'h10;
    localparam MIN_PER    = 6'h14;
    localparam ACCEL      = 6'h18;
    localparam STEP_WIDTH = 6'h1C;
    localparam SETTLE     = 6'h20;
    localparam TRIG_WIDTH = 6'h24;
    localparam POSITION   = 6'h28;
    localparam ID         = 6'h2C;

    // CTRL bits
    localparam C_START    = 32'h1;
    localparam C_ABORT    = 32'h2;
    localparam C_TRIG_NOW = 32'h4;
    localparam C_ZERO     = 32'h8;

    reg         clk = 0;
    reg         rst_n = 0;
    always #5 clk = ~clk;   // 100 MHz

    reg  [5:0]  awaddr = 0;
    reg         awvalid = 0;
    wire        awready;
    reg  [31:0] wdata = 0;
    reg         wvalid = 0;
    wire        wready;
    wire [1:0]  bresp;
    wire        bvalid;
    reg         bready = 1;
    reg  [5:0]  araddr = 0;
    reg         arvalid = 0;
    wire        arready;
    wire [31:0] rdata;
    wire [1:0]  rresp;
    wire        rvalid;
    reg         rready = 1;

    reg         limit_fwd = 0;   // active-high in this test (polarity bit set)
    reg         limit_rev = 0;
    wire        step_out, dir_out, motor_en, cam_focus, cam_shutter, irq;

    axi_stepper dut (
        .s_axi_aclk    (clk),
        .s_axi_aresetn (rst_n),
        .s_axi_awaddr  (awaddr),
        .s_axi_awvalid (awvalid),
        .s_axi_awready (awready),
        .s_axi_wdata   (wdata),
        .s_axi_wstrb   (4'hF),
        .s_axi_wvalid  (wvalid),
        .s_axi_wready  (wready),
        .s_axi_bresp   (bresp),
        .s_axi_bvalid  (bvalid),
        .s_axi_bready  (bready),
        .s_axi_araddr  (araddr),
        .s_axi_arvalid (arvalid),
        .s_axi_arready (arready),
        .s_axi_rdata   (rdata),
        .s_axi_rresp   (rresp),
        .s_axi_rvalid  (rvalid),
        .s_axi_rready  (rready),
        .limit_fwd     (limit_fwd),
        .limit_rev     (limit_rev),
        .step_out      (step_out),
        .dir_out       (dir_out),
        .motor_en      (motor_en),
        .cam_focus     (cam_focus),
        .cam_shutter   (cam_shutter),
        .irq           (irq)
    );

    integer errors = 0;
    integer step_count = 0;
    integer shutter_count = 0;
    reg counting = 0;

    always @(posedge step_out) if (counting) step_count = step_count + 1;
    always @(posedge cam_shutter) shutter_count = shutter_count + 1;

    task axi_write(input [5:0] addr, input [31:0] data);
        begin
            @(posedge clk);
            awaddr  <= addr; awvalid <= 1;
            wdata   <= data; wvalid  <= 1;
            wait (bvalid);
            @(posedge clk);
            awvalid <= 0; wvalid <= 0;
            wait (!bvalid);
            @(posedge clk);
        end
    endtask

    task axi_read(input [5:0] addr, output [31:0] data);
        begin
            @(posedge clk);
            araddr <= addr; arvalid <= 1;
            wait (rvalid);
            data = rdata;
            @(posedge clk);
            arvalid <= 0;
            wait (!rvalid);
            @(posedge clk);
        end
    endtask

    task check(input [255:0] name, input [31:0] got, input [31:0] exp);
        begin
            if (got !== exp) begin
                $display("FAIL: %0s got 0x%08x expected 0x%08x", name, got, exp);
                errors = errors + 1;
            end else begin
                $display("PASS: %0s = 0x%08x", name, got);
            end
        end
    endtask

    task wait_idle;
        reg [31:0] st;
        begin
            st = 32'h1;
            while (st[0]) axi_read(STATUS, st);
        end
    endtask

    reg [31:0] rd;

    initial begin
        $dumpfile("tb_axi_stepper.vcd");
        $dumpvars(0, tb_axi_stepper);

        repeat (5) @(posedge clk);
        rst_n = 1;
        repeat (5) @(posedge clk);

        // 1. ID register
        axi_read(ID, rd);
        check("ID", rd, 32'h4D520100);

        // 2. Configure a 20-step forward move with trigger.
        axi_write(STEPS,      32'd20);
        axi_write(START_PER,  32'd200);
        axi_write(MIN_PER,    32'd100);
        axi_write(ACCEL,      32'd10);
        axi_write(STEP_WIDTH, 32'd20);
        axi_write(SETTLE,     32'd100);
        axi_write(TRIG_WIDTH, 32'd50);
        // DIR=1, TRIGGER_EN=1, LIMIT_POLARITY=1 (active high), MOTOR_EN=1
        axi_write(CONFIG,     32'b1111);
        axi_read(CONFIG, rd);
        check("CONFIG readback", rd, 32'b1111);
        check("motor_en pin", motor_en, 1);

        step_count = 0; counting = 1;
        axi_write(CTRL, C_START);
        wait_idle;
        counting = 0;

        check("forward move step count", step_count, 20);
        axi_read(POSITION, rd);
        check("position after forward move", rd, 32'd20);
        check("dir pin during forward move", dir_out, 1);
        check("shutter fired once", shutter_count, 1);
        axi_read(STATUS, rd);
        check("done flag set", rd[1], 1);

        // 3. Reverse 5 steps without trigger.
        axi_write(CONFIG, 32'b1100);  // DIR=0, TRIGGER_EN=0
        axi_write(STEPS,  32'd5);
        step_count = 0; counting = 1;
        axi_write(CTRL, C_START);
        wait_idle;
        counting = 0;
        check("reverse move step count", step_count, 5);
        axi_read(POSITION, rd);
        check("position after reverse move", rd, 32'd15);
        check("no extra shutter pulses", shutter_count, 1);

        // 4. Manual trigger with no motion.
        axi_write(CTRL, C_TRIG_NOW);
        wait_idle;
        check("manual trigger fired", shutter_count, 2);
        axi_read(POSITION, rd);
        check("position unchanged by trigger", rd, 32'd15);

        // 5. Forward move into the forward limit switch: must halt early.
        axi_write(CONFIG, 32'b1101);  // DIR=1, TRIGGER_EN=0
        axi_write(STEPS,  32'd1000);
        step_count = 0; counting = 1;
        axi_write(CTRL, C_START);
        // Let ~10 steps happen, then trip the limit.
        wait (step_count >= 10);
        limit_fwd = 1;
        wait_idle;
        counting = 0;
        axi_read(STATUS, rd);
        check("halted-on-limit flag", rd[4], 1);
        check("limit fwd status bit", rd[2], 1);
        if (step_count >= 1000) begin
            $display("FAIL: move did not halt on limit (steps=%0d)", step_count);
            errors = errors + 1;
        end else
            $display("PASS: move halted on limit after %0d steps", step_count);

        // 6. Reverse away from the asserted forward limit must be allowed.
        axi_write(CONFIG, 32'b1100);  // DIR=0
        axi_write(STEPS,  32'd3);
        step_count = 0; counting = 1;
        axi_write(CTRL, C_START);
        wait_idle;
        counting = 0;
        check("reverse allowed while fwd limit asserted", step_count, 3);
        limit_fwd = 0;

        // 7. Abort mid-move.
        axi_write(CONFIG, 32'b1101);  // DIR=1
        axi_write(STEPS,  32'd100000);
        step_count = 0; counting = 1;
        axi_write(CTRL, C_START);
        wait (step_count >= 5);
        axi_write(CTRL, C_ABORT);
        wait_idle;
        counting = 0;
        if (step_count >= 100000) begin
            $display("FAIL: abort did not stop the move");
            errors = errors + 1;
        end else
            $display("PASS: abort stopped the move after %0d steps", step_count);
        axi_read(STATUS, rd);
        check("done not set after abort", rd[1], 0);

        // 8. Zero the position counter.
        axi_write(CTRL, C_ZERO);
        axi_read(POSITION, rd);
        check("position zeroed", rd, 32'd0);

        if (errors == 0)
            $display("ALL TESTS PASSED");
        else
            $display("%0d TEST(S) FAILED", errors);
        $finish;
    end

    // Watchdog
    initial begin
        #50_000_000;
        $display("FAIL: testbench watchdog timeout");
        $finish;
    end

endmodule

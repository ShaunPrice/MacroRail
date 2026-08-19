// -----------------------------------------------------------------------------
// tb_axi_motion.v
//
// Closed-loop testbench for the two-axis motion controller. The DUT drives
// a first-order DC motor model, whose shaft position is fed back as real
// quadrature waveforms into the DUT's own encoder decoder - so the loop
// under test is the complete one: PID -> PWM -> H-bridge -> motor ->
// encoder -> decoder -> PID.
//
// Covers the safety contract as well as the control loop, because on a
// robot the safety behaviour is the part that must never regress:
// E-stop latching, command watchdog, one-way limit lock, setpoint
// clamping, limit-switch direction blocking, and a continuous assertion
// that the H-bridge never shoot-throughs.
//
// Run:
//   iverilog -g2012 -o tb ultra96/fpga/rtl/{quad_decoder,pid_axis,\
//       pwm_hbridge,safety_core,motion_axis,axi_motion}.v \
//       ultra96/fpga/sim/tb_axi_motion.v
//   vvp tb
//
// This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
// and is released under the GNU General Public License v3 or later.
// -----------------------------------------------------------------------------

`timescale 1ns / 1ps

module tb_axi_motion;

    // Global register offsets
    localparam G_CTRL   = 9'h00;
    localparam G_STATUS = 9'h04;
    localparam G_WDOG   = 9'h08;
    localparam G_SAMPLE = 9'h0C;
    localparam G_PWMPER = 9'h10;
    localparam G_DEAD   = 9'h14;
    localparam G_SPLIM  = 9'h18;
    localparam G_ID     = 9'h1C;

    // Axis 0 register offsets
    localparam A0_SP    = 9'h40;
    localparam A0_KP    = 9'h44;
    localparam A0_KI    = 9'h48;
    localparam A0_KD    = 9'h4C;
    localparam A0_IMAX  = 9'h50;
    localparam A0_OMAX  = 9'h54;
    localparam A0_CTRL  = 9'h58;
    localparam A0_POS   = 9'h5C;
    localparam A0_VEL   = 9'h60;
    localparam A0_ERR   = 9'h64;
    localparam A0_DUTY  = 9'h68;
    localparam A0_STAT  = 9'h6C;

    localparam A1_SP    = 9'h80;
    localparam A1_POS   = 9'h9C;

    // Velocity mode works in Q24.8 counts per control period.
    localparam Q8 = 256;

    // GLOBAL_CTRL bits
    localparam GC_ENABLE      = 32'h1;
    localparam GC_ESTOP_CLEAR = 32'h2;
    localparam GC_LOCK        = 32'h4;

    reg clk = 0;
    reg rst_n = 0;
    always #5 clk = ~clk;              // 100 MHz

    reg  [8:0]  awaddr = 0;
    reg         awvalid = 0;
    wire        awready;
    reg  [31:0] wdata = 0;
    reg         wvalid = 0;
    wire        wready;
    wire [1:0]  bresp;
    wire        bvalid;
    reg         bready = 1;
    reg  [8:0]  araddr = 0;
    reg         arvalid = 0;
    wire        arready;
    wire [31:0] rdata;
    wire [1:0]  rresp;
    wire        rvalid;
    reg         rready = 1;

    reg  estop_n    = 1;               // released
    reg  limit0_fwd = 0, limit0_rev = 0;
    reg  limit1_fwd = 0, limit1_rev = 0;

    wire enc0_a, enc0_b;
    wire m0_in1, m0_in2, m1_in1, m1_in2;
    wire irq;

    axi_motion dut (
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
        .estop_n       (estop_n),
        .enc0_a        (enc0_a),
        .enc0_b        (enc0_b),
        .enc0_z        (1'b0),
        .enc1_a        (1'b0),
        .enc1_b        (1'b0),
        .enc1_z        (1'b0),
        .limit0_fwd    (limit0_fwd),
        .limit0_rev    (limit0_rev),
        .limit1_fwd    (limit1_fwd),
        .limit1_rev    (limit1_rev),
        .m0_in1        (m0_in1),
        .m0_in2        (m0_in2),
        .m1_in1        (m1_in1),
        .m1_in2        (m1_in2),
        .irq           (irq)
    );

    // ------------------------------------------------------------------
    // DC motor plant model (first order) + quadrature encoder generator
    //
    //   dv/dt = (V_FULL * u - v) / TAU        u in {-1, 0, +1} from the bridge
    //   dx/dt = v
    //
    // The motor's own mechanical lag is what filters the PWM carrier, so
    // no artificial low-pass is needed - this is how a real motor behaves.
    // ------------------------------------------------------------------
    localparam real DT      = 10.0e-9;   // one clk
    localparam real TAU     = 2.0e-3;    // mechanical time constant
    localparam real V_FULL  = 200000.0;  // counts/s at 100% duty

    real motor_vel = 0.0;
    real motor_pos = 0.0;
    real u;
    integer pos_i;
    reg plant_on = 0;
    reg enc_a_r = 0, enc_b_r = 0;

    // Quadrature waveform: forward sequence {A,B} = 00 -> 10 -> 11 -> 01.
    // Driven from the clocked block rather than a continuous assignment,
    // because @(*) does not reliably track a `real` variable.
    always @(posedge clk) begin
        if (plant_on) begin
            u = (m0_in1 && !m0_in2) ?  1.0 :
                (m0_in2 && !m0_in1) ? -1.0 : 0.0;
            motor_vel = motor_vel + (V_FULL * u - motor_vel) * (DT / TAU);
            motor_pos = motor_pos + motor_vel * DT;
        end
        pos_i   = $rtoi(motor_pos);
        enc_a_r <= pos_i[1] ^ pos_i[0];
        enc_b_r <= pos_i[1];
    end

    assign enc0_a = enc_a_r;
    assign enc0_b = enc_b_r;

    // ------------------------------------------------------------------
    // Continuous shoot-through assertion
    // ------------------------------------------------------------------
    integer shoot_through = 0;
    always @(posedge clk) begin
        if (rst_n && !dut.safety_brake && (m0_in1 && m0_in2)) begin
            shoot_through = shoot_through + 1;
        end
    end

    // ------------------------------------------------------------------
    // AXI helpers
    // ------------------------------------------------------------------
    integer errors = 0;

    task axi_write(input [8:0] addr, input [31:0] data);
        begin
            @(posedge clk);
            awaddr <= addr; awvalid <= 1;
            wdata  <= data; wvalid  <= 1;
            wait (bvalid);
            @(posedge clk);
            awvalid <= 0; wvalid <= 0;
            wait (!bvalid);
            @(posedge clk);
        end
    endtask

    task axi_read(input [8:0] addr, output [31:0] data);
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

    task check(input [255:0] name, input integer got, input integer exp);
        begin
            if (got !== exp) begin
                $display("FAIL: %0s got %0d expected %0d", name, got, exp);
                errors = errors + 1;
            end else
                $display("PASS: %0s = %0d", name, got);
        end
    endtask

    task check_near(input [255:0] name, input integer got, input integer exp,
                    input integer tol);
        integer diff;
        begin
            diff = got - exp;
            if (diff < 0) diff = -diff;
            if (diff > tol) begin
                $display("FAIL: %0s got %0d expected %0d +/-%0d", name, got, exp, tol);
                errors = errors + 1;
            end else
                $display("PASS: %0s = %0d (target %0d +/-%0d)", name, got, exp, tol);
        end
    endtask

    reg [31:0] rd;
    integer    sgn;

    // Read a register as a signed integer.
    task axi_read_signed(input [8:0] addr, output integer val);
        reg [31:0] raw;
        begin
            axi_read(addr, raw);
            val = raw[31] ? (raw - 4294967296.0) : raw;
        end
    endtask

    initial begin
        repeat (5) @(posedge clk);
        rst_n = 1;
        repeat (5) @(posedge clk);

        // ==============================================================
        // 1. Identification
        // ==============================================================
        axi_read(G_ID, rd);
        check("ID register", rd, 32'h4D523200);

        // ==============================================================
        // 2. Safety envelope: set limits, then LOCK them
        // ==============================================================
        axi_write(G_SAMPLE, 32'd10000);     // 10 kHz control loop
        axi_write(G_PWMPER, 32'd1000);      // 100 kHz PWM
        axi_write(G_DEAD,   32'd50);        // 500 ns dead-time
        axi_write(G_WDOG,   32'd0);         // watchdog off for bring-up
        axi_write(G_SPLIM,  40*Q8);         // ceiling: 40 counts/period

        axi_read(G_SPLIM, rd);
        check("setpoint limit before lock", rd, 40*Q8);

        axi_write(G_CTRL, GC_LOCK);         // one-way latch
        axi_read(G_STATUS, rd);
        check("locked flag", (rd >> 2) & 1, 1);

        // Software now tries to raise its own speed limit - must fail.
        axi_write(G_SPLIM, 32'd1000000);
        axi_read(G_SPLIM, rd);
        check("setpoint limit is read-only after lock", rd, 40*Q8);

        // The watchdog timeout is lockable too.
        axi_write(G_WDOG, 32'd12345);
        axi_read(G_WDOG, rd);
        check("watchdog timeout locked", rd, 0);

        // ==============================================================
        // 3. E-stop inhibits motion regardless of software state
        // ==============================================================
        axi_write(A0_KP,   32'd12800);      // 0.195 in Q16.16
        axi_write(A0_KI,   32'd1024);       // 0.0156
        axi_write(A0_KD,   32'd256);        // 0.0039
        axi_write(A0_IMAX, 32'd20000000);
        axi_write(A0_OMAX, 32'd1000);       // full duty == pwm period

        estop_n = 0;                        // button pressed
        repeat (20) @(posedge clk);
        axi_write(G_CTRL, GC_ENABLE);
        axi_write(A0_SP, 20*Q8);            // full-throated command
        repeat (2000) @(posedge clk);

        axi_read(G_STATUS, rd);
        check("estop latched", rd & 1, 1);
        check("motion inhibited under estop", (rd >> 4) & 1, 0);
        check("motor coasting under estop (in1)", m0_in1, 0);

        // Releasing the button alone must NOT re-arm the robot.
        estop_n = 1;
        repeat (200) @(posedge clk);
        axi_read(G_STATUS, rd);
        check("estop stays latched after release", rd & 1, 1);

        // Only a deliberate clear re-arms it.
        axi_write(G_CTRL, GC_ENABLE | GC_ESTOP_CLEAR);
        repeat (20) @(posedge clk);
        axi_read(G_STATUS, rd);
        check("estop cleared deliberately", rd & 1, 0);
        check("motion permitted again", (rd >> 4) & 1, 1);

        // ==============================================================
        // 4. Setpoint clamping against the locked envelope
        // ==============================================================
        axi_write(A0_SP, 32'd100000);       // way over the ceiling
        repeat (200) @(posedge clk);
        axi_read(A0_STAT, rd);
        check("clamped flag raised", (rd >> 3) & 1, 1);

        // ==============================================================
        // 5. Closed-loop velocity tracking
        // ==============================================================
        axi_write(A0_SP, 32'd0);
        motor_vel = 0.0;
        motor_pos = 0.0;
        plant_on  = 1;
        repeat (100) @(posedge clk);

        // Command +10 counts per 100 us control period (== 100k counts/s).
        axi_write(A0_SP, 10*Q8);
        repeat (1_500_000) @(posedge clk);   // ~15 ms, several time constants

        axi_read_signed(A0_VEL, sgn);
        check_near("closed-loop velocity tracks +10", sgn, 10*Q8, Q8/4);
        axi_read_signed(A0_POS, sgn);
        if (sgn > 0)
            $display("PASS: position advanced forward = %0d counts", sgn);
        else begin
            $display("FAIL: position did not advance forward (%0d)", sgn);
            errors = errors + 1;
        end

        // ==============================================================
        // 6. Reversal - exercises the dead-time path under load
        // ==============================================================
        axi_write(A0_SP, -10*Q8);
        repeat (1_500_000) @(posedge clk);
        axi_read_signed(A0_VEL, sgn);
        check_near("closed-loop velocity tracks -10", sgn, -10*Q8, Q8/4);

        // ==============================================================
        // 7. Limit switch blocks one direction only
        // ==============================================================
        axi_write(A0_SP, 32'd0);
        repeat (200_000) @(posedge clk);
        limit0_fwd = 1;
        axi_write(A0_SP, 10*Q8);           // into the limit
        // The modelled motor coasts down with a 2 ms time constant, so
        // allow ~10 constants for it to actually reach a standstill.
        repeat (2_000_000) @(posedge clk);
        axi_read(A0_STAT, rd);
        check("limit blocks forward command", (rd >> 3) & 1, 1);
        axi_read_signed(A0_VEL, sgn);
        check_near("velocity held at zero on limit", sgn, 0, Q8/4);

        axi_write(A0_SP, -10*Q8);         // away from the limit
        repeat (1_500_000) @(posedge clk);
        axi_read_signed(A0_VEL, sgn);
        check_near("reverse still allowed off the limit", sgn, -10*Q8, Q8/4);
        limit0_fwd = 0;

        // ==============================================================
        // 8. Command watchdog: the robot stops when Linux goes quiet
        // ==============================================================
        axi_write(A0_SP, 32'd0);
        repeat (200_000) @(posedge clk);

        // The watchdog register is locked, so prove the lock really is
        // one-way by resetting the core and reconfiguring from scratch.
        rst_n = 0;
        repeat (10) @(posedge clk);
        rst_n = 1;
        repeat (10) @(posedge clk);
        motor_vel = 0.0;
        motor_pos = 0.0;

        axi_read(G_STATUS, rd);
        check("lock released only by reset", (rd >> 2) & 1, 0);

        axi_write(G_SAMPLE, 32'd10000);
        axi_write(G_PWMPER, 32'd1000);
        axi_write(G_DEAD,   32'd50);
        axi_write(G_SPLIM,  40*Q8);
        axi_write(A0_KP,    32'd12800);
        axi_write(A0_KI,    32'd1024);
        axi_write(A0_KD,    32'd256);
        axi_write(A0_IMAX,  32'd20000000);
        axi_write(A0_OMAX,  32'd1000);
        axi_write(G_WDOG,   32'd100000);    // 1 ms of silence is fatal
        axi_write(G_CTRL,   GC_ENABLE);

        // Keep petting: motion stays permitted.
        repeat (20) begin
            axi_write(A0_SP, 10*Q8);
            repeat (5000) @(posedge clk);
        end
        axi_read(G_STATUS, rd);
        check("watchdog healthy while commands flow", (rd >> 1) & 1, 0);
        check("motion permitted while commands flow", (rd >> 4) & 1, 1);

        // Now simulate the Linux side hanging: stop writing setpoints.
        repeat (300_000) @(posedge clk);    // 3 ms of silence
        axi_read(G_STATUS, rd);
        check("watchdog tripped on command loss", (rd >> 1) & 1, 1);
        check("motion inhibited by watchdog", (rd >> 4) & 1, 0);
        check("irq raised on watchdog trip", irq, 1);

        // Same coast-down allowance as above before asserting standstill.
        repeat (2_000_000) @(posedge clk);
        axi_read_signed(A0_VEL, sgn);
        check_near("motor stopped after watchdog trip", sgn, 0, Q8/4);

        // Commands resuming should recover automatically.
        axi_write(A0_SP, 10*Q8);
        repeat (100) @(posedge clk);
        axi_read(G_STATUS, rd);
        check("watchdog recovers when commands resume", (rd >> 1) & 1, 0);

        // ==============================================================
        // 9. Shoot-through never occurred
        // ==============================================================
        check("H-bridge shoot-through cycles", shoot_through, 0);

        if (errors == 0)
            $display("\nALL TESTS PASSED");
        else
            $display("\n%0d TEST(S) FAILED", errors);
        $finish;
    end

    initial begin
        #200_000_000;                       // 200 ms of simulated time
        $display("FAIL: testbench watchdog timeout");
        $finish;
    end

endmodule

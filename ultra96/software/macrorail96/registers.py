"""Register map of the MacroRail FPGA stepper peripheral.

Must match ultra96/fpga/rtl/axi_stepper.v. The peripheral is mapped at
BASE_ADDR on the PS M_AXI_HPM0_FPD port by fpga/vivado/build.tcl.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

BASE_ADDR = 0xA000_0000
SPAN = 0x1000

# 100 MHz PL clock -> 10 ns per cycle.
CLOCK_HZ = 100_000_000

# Register byte offsets
CTRL = 0x00
CONFIG = 0x04
STATUS = 0x08
STEPS = 0x0C
START_PERIOD = 0x10
MIN_PERIOD = 0x14
ACCEL = 0x18
STEP_WIDTH = 0x1C
SETTLE = 0x20
TRIG_WIDTH = 0x24
POSITION = 0x28
ID = 0x2C

# CTRL bits (write-only strobes)
CTRL_START = 1 << 0
CTRL_ABORT = 1 << 1
CTRL_TRIG_NOW = 1 << 2
CTRL_ZERO_POS = 1 << 3

# CONFIG bits
CFG_DIR = 1 << 0
CFG_TRIGGER_EN = 1 << 1
CFG_LIMIT_POLARITY = 1 << 2
CFG_MOTOR_EN = 1 << 3

# STATUS bits
ST_BUSY = 1 << 0
ST_DONE = 1 << 1
ST_LIMIT_FWD = 1 << 2
ST_LIMIT_REV = 1 << 3
ST_HALTED_ON_LIMIT = 1 << 4

ID_VALUE = 0x4D52_0100  # "MR" v1.0

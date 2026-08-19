# -----------------------------------------------------------------------------
# build_robot.tcl
#
# Builds the two-axis robot motion-controller bitstream for the Avnet
# Ultra96-V2 (Zynq UltraScale+ ZU3EG, xczu3eg-sbva484-1-i).
#
# Usage (Vivado 2021.2 or later, Avnet board files recommended):
#   vivado -mode batch -source build_robot.tcl
#
# Outputs (in ./out_robot):
#   robot_top.bit    bitstream
#   robot_top.hwh    hardware handoff for PYNQ overlays
#   robot_top.xsa    hardware export for PetaLinux/Vitis
#
# The motion peripheral is mapped at 0xA001_0000, which is the base
# address software/macrorail96/motion.py expects. The MacroRail stepper
# peripheral (build.tcl) sits at 0xA000_0000, so both can coexist in one
# design if you want a camera rail and a rover on the same board.
#
# Adding the Vitis AI DPU
# -----------------------
# This script builds the control fabric only. To add a DPUCZDX8G for
# on-board inference, use the Vitis flow rather than this Vivado project:
# start from the Vitis AI DPU-TRD, target a B1152 or B2304 configuration
# (a ZU3EG has ~360 DSP slices and 141k LUTs - a B4096 will not fit
# alongside this controller), and add these RTL sources as a second
# kernel. Then recompile your .xmodel against the arch.json the DPU-TRD
# emits, or vision.py will refuse to load it.
#
# This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
# and is released under the GNU General Public License v3 or later.
# -----------------------------------------------------------------------------

set proj_name  macrorail_robot
set proj_dir   ./build_robot
set out_dir    ./out_robot
set src_dir    [file normalize [file dirname [info script]]/..]

set part       xczu3eg-sbva484-1-i
set board_part ""
foreach bp [get_board_parts -quiet *ultra96v2*] { set board_part $bp }

create_project $proj_name $proj_dir -part $part -force
if {$board_part ne ""} {
    set_property board_part $board_part [current_project]
    puts "INFO: using board part $board_part"
} else {
    puts "WARNING: Ultra96-V2 board files not found; using raw part $part."
    puts "WARNING: PS DDR/MIO settings must then be checked manually."
    puts "WARNING: board files: https://github.com/Avnet/bdf"
}

add_files -norecurse [list \
    $src_dir/rtl/quad_decoder.v \
    $src_dir/rtl/pid_axis.v \
    $src_dir/rtl/pwm_hbridge.v \
    $src_dir/rtl/safety_core.v \
    $src_dir/rtl/motion_axis.v \
    $src_dir/rtl/axi_motion.v ]

add_files -fileset constrs_1 -norecurse \
    $src_dir/constraints/ultra96v2_robot.xdc

# Simulation sources, so the testbenches can be run from the GUI too.
add_files -fileset sim_1 -norecurse [list \
    $src_dir/sim/tb_axi_motion.v \
    $src_dir/sim/tb_pid_vectors.v ]
set_property top tb_axi_motion [get_filesets sim_1]

# ------------------------------------------------------------------
# Block design: PS + AXI interconnect + motion controller
# ------------------------------------------------------------------
create_bd_design "robot"

set ps [create_bd_cell -type ip -vlnv xilinx.com:ip:zynq_ultra_ps_e:3.* zynq_ps]
if {$board_part ne ""} {
    apply_bd_automation -rule xilinx.com:bd_rule:zynq_ultra_ps_e \
        -config {apply_board_preset "1"} $ps
}
set_property -dict [list \
    CONFIG.PSU__USE__M_AXI_GP0 {1} \
    CONFIG.PSU__MAXIGP0__DATA_WIDTH {32} \
    CONFIG.PSU__USE__M_AXI_GP1 {0} \
    CONFIG.PSU__USE__M_AXI_GP2 {0} \
    CONFIG.PSU__USE__IRQ0 {1} \
    CONFIG.PSU__FPGA_PL0_ENABLE {1} \
    CONFIG.PSU__CRL_APB__PL0_REF_CTRL__FREQMHZ {100} ] $ps

set motion [create_bd_cell -type module -reference axi_motion motion_0]

apply_bd_automation -rule xilinx.com:bd_rule:axi4 -config [list \
    Master "/zynq_ps/M_AXI_HPM0_FPD" Clk "Auto"] [get_bd_intf_pins motion_0/s_axi]

# The safety core raises this whenever it is inhibiting motion, so Linux
# learns about an E-stop or a watchdog trip without polling.
connect_bd_net [get_bd_pins motion_0/irq] [get_bd_pins zynq_ps/pl_ps_irq0]

foreach p {estop_n enc0_a enc0_b enc0_z enc1_a enc1_b enc1_z \
           limit0_fwd limit0_rev limit1_fwd limit1_rev \
           m0_in1 m0_in2 m1_in1 m1_in2} {
    make_bd_pins_external [get_bd_pins motion_0/$p]
    set_property name $p [get_bd_ports ${p}_0]
}

# Fix the peripheral at the address the driver expects.
set seg [get_bd_addr_segs -of_objects [get_bd_addr_spaces zynq_ps/Data] *motion*]
if {$seg ne ""} {
    set_property offset 0xA0010000 $seg
    set_property range  4K         $seg
}

validate_bd_design
save_bd_design

make_wrapper -files [get_files robot.bd] -top
add_files -norecurse $proj_dir/$proj_name.gen/sources_1/bd/robot/hdl/robot_wrapper.v
set_property top robot_wrapper [current_fileset]

launch_runs impl_1 -to_step write_bitstream -jobs 4
wait_on_run impl_1

# ------------------------------------------------------------------
# Collect outputs
# ------------------------------------------------------------------
file mkdir $out_dir
file copy -force $proj_dir/$proj_name.runs/impl_1/robot_wrapper.bit \
    $out_dir/robot_top.bit
file copy -force $proj_dir/$proj_name.gen/sources_1/bd/robot/hw_handoff/robot.hwh \
    $out_dir/robot_top.hwh
write_hw_platform -fixed -include_bit -force $out_dir/robot_top.xsa

# Fail loudly rather than shipping a bitstream that does not meet timing.
open_run impl_1
set wns [get_property SLACK [get_timing_paths -delay_type max]]
if {$wns < 0} {
    puts "ERROR: design fails timing, worst negative slack = $wns ns"
    puts "ERROR: do NOT deploy this bitstream to a robot."
    exit 1
}
puts "Timing met, worst slack = $wns ns"
puts "Build complete. Outputs in $out_dir"

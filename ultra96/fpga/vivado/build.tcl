# -----------------------------------------------------------------------------
# build.tcl
#
# Builds the MacroRail stepper-controller bitstream for the Avnet
# Ultra96-V2 (Zynq UltraScale+ ZU3EG, xczu3eg-sbva484-1-i).
#
# Usage (Vivado 2021.2 or later, Avnet board files recommended):
#   vivado -mode batch -source build.tcl
#
# Outputs (in ./out):
#   macrorail_top.bit        bitstream
#   macrorail_top.hwh        hardware handoff for PYNQ overlays
#   macrorail_top.xsa        hardware export for PetaLinux/Vitis
#
# The stepper AXI peripheral is mapped at 0xA000_0000 on the PS
# M_AXI_HPM0_FPD port, which is the base address the Python software
# (software/macrorail96) expects.
# -----------------------------------------------------------------------------

set proj_name  macrorail_u96
set proj_dir   ./build
set out_dir    ./out
set src_dir    [file normalize [file dirname [info script]]/..]

# Use the Avnet board part when the board files are installed, otherwise
# fall back to the bare part. Board files: https://github.com/Avnet/bdf
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
}

add_files -norecurse [list \
    $src_dir/rtl/stepper_ctrl.v \
    $src_dir/rtl/axi_stepper.v ]
add_files -fileset constrs_1 -norecurse $src_dir/constraints/ultra96v2_macrorail.xdc

# ------------------------------------------------------------------
# Block design: PS + AXI interconnect + stepper peripheral
# ------------------------------------------------------------------
create_bd_design "system"

set ps [create_bd_cell -type ip -vlnv xilinx.com:ip:zynq_ultra_ps_e:3.* zynq_ps]
# Board automation applies the Ultra96-V2 preset (DDR, MIO, WiFi, etc.)
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

set stepper [create_bd_cell -type module -reference axi_stepper stepper_0]

apply_bd_automation -rule xilinx.com:bd_rule:axi4 -config [list \
    Master "/zynq_ps/M_AXI_HPM0_FPD" Clk "Auto"] [get_bd_intf_pins stepper_0/s_axi]

connect_bd_net [get_bd_pins stepper_0/irq] [get_bd_pins zynq_ps/pl_ps_irq0]

# External pins
foreach p {step_out dir_out motor_en cam_focus cam_shutter} {
    make_bd_pins_external [get_bd_pins stepper_0/$p]
    set_property name $p [get_bd_ports ${p}_0]
}
foreach p {limit_fwd limit_rev} {
    make_bd_pins_external [get_bd_pins stepper_0/$p]
    set_property name $p [get_bd_ports ${p}_0]
}

# Fix the peripheral at the address the software expects.
set seg [get_bd_addr_segs -of_objects [get_bd_addr_spaces zynq_ps/Data] *stepper*]
if {$seg ne ""} {
    set_property offset 0xA0000000 $seg
    set_property range  4K         $seg
}

validate_bd_design
save_bd_design

make_wrapper -files [get_files system.bd] -top
add_files -norecurse $proj_dir/$proj_name.gen/sources_1/bd/system/hdl/system_wrapper.v
set_property top system_wrapper [current_fileset]

launch_runs impl_1 -to_step write_bitstream -jobs 4
wait_on_run impl_1

# ------------------------------------------------------------------
# Collect outputs
# ------------------------------------------------------------------
file mkdir $out_dir
file copy -force $proj_dir/$proj_name.runs/impl_1/system_wrapper.bit $out_dir/macrorail_top.bit
file copy -force $proj_dir/$proj_name.gen/sources_1/bd/system/hw_handoff/system.hwh $out_dir/macrorail_top.hwh
write_hw_platform -fixed -include_bit -force $out_dir/macrorail_top.xsa

puts "Build complete. Outputs in $out_dir"

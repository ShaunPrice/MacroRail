# -----------------------------------------------------------------------------
# ultra96v2_macrorail.xdc
#
# Pin constraints for the MacroRail stepper controller on the Avnet
# Ultra96-V2 40-pin low-speed expansion header (J1, 96Boards LS spec).
#
# Package pins are taken from Avnet's official ultra96v2_valtest.xdc
# (https://github.com/Avnet/hdl). All header GPIO are in an HD bank at
# LVCMOS18 — the header is 1.8 V logic ONLY.
#
#   *** Level-shift to 5 V before the Pololu TIC / stepper driver and    ***
#   *** use optocouplers for the camera remote-release inputs. Never     ***
#   *** connect 5 V signals (e.g. the LJ8A3-2-Z/BX proximity sensors)    ***
#   *** directly to the header — divide or shift them down to 1.8 V.     ***
#
# Signal          FPGA pin  Header net
# step_out        D7        HD_GPIO_0
# dir_out         F8        HD_GPIO_1
# motor_en        F7        HD_GPIO_2
# limit_fwd       G7        HD_GPIO_3
# limit_rev       G5        HD_GPIO_5
# cam_focus       F6        HD_GPIO_4
# cam_shutter     A6        HD_GPIO_6
#
# Cross-check header pin numbers against the Ultra96-V2 Hardware User
# Guide for your board revision before wiring.
# -----------------------------------------------------------------------------

set_property PACKAGE_PIN D7 [get_ports step_out]
set_property PACKAGE_PIN F8 [get_ports dir_out]
set_property PACKAGE_PIN F7 [get_ports motor_en]
set_property PACKAGE_PIN G7 [get_ports limit_fwd]
set_property PACKAGE_PIN G5 [get_ports limit_rev]
set_property PACKAGE_PIN F6 [get_ports cam_focus]
set_property PACKAGE_PIN A6 [get_ports cam_shutter]

set_property IOSTANDARD LVCMOS18 [get_ports {step_out dir_out motor_en limit_fwd limit_rev cam_focus cam_shutter}]

# Keep the limit inputs from floating when nothing is wired.
set_property PULLUP true [get_ports limit_fwd]
set_property PULLUP true [get_ports limit_rev]

# Camera and motor outputs must idle low at power-up.
set_property PULLDOWN true [get_ports cam_focus]
set_property PULLDOWN true [get_ports cam_shutter]

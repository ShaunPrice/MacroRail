# -----------------------------------------------------------------------------
# ultra96v2_robot.xdc
#
# Pin constraints for the two-axis robot motion controller on the Avnet
# Ultra96-V2 40-pin low-speed expansion header (J1, 96Boards LS spec).
#
# Package pins are taken from Avnet's official ultra96v2_valtest.xdc
# (https://github.com/Avnet/hdl). All header GPIO sit in an HD bank at
# LVCMOS18 - the header is 1.8 V logic ONLY, and is NOT 3.3 V or 5 V
# tolerant. Every signal below needs a level shifter.
#
# Signal        FPGA pin  Direction  Notes
# estop_n       F8        in         FAIL-SAFE, see below
# m0_in1        D7        out        left motor bridge, forward
# m0_in2        F7        out        left motor bridge, reverse
# m1_in1        F6        out        right motor bridge, forward
# m1_in2        A8        out        right motor bridge, reverse
# enc0_a        G7        in         left encoder channel A
# enc0_b        G5        in         left encoder channel B
# enc0_z        A6        in         left encoder index (tie low if unused)
# enc1_a        E5        in         right encoder channel A
# enc1_b        D5        in         right encoder channel B
# enc1_z        C7        in         right encoder index (tie low if unused)
# limit0_fwd    C5        in         left forward limit / bumper
# limit0_rev    E6        in         left reverse limit / bumper
# limit1_fwd    D6        in         right forward limit / bumper
# limit1_rev    C8        in         right reverse limit / bumper
#
# Cross-check header pin numbers against the Ultra96-V2 Hardware User
# Guide for your board revision before wiring anything.
#
# -----------------------------------------------------------------------------
# WIRING THE E-STOP (read this before powering a robot)
# -----------------------------------------------------------------------------
# estop_n is active LOW: a low level means "pressed, stop now". Wire it so
# that a FAILURE also reads as pressed:
#
#   * use a NORMALLY-CLOSED mushroom-head E-stop switch,
#   * the closed contact holds estop_n HIGH (through the level shifter),
#   * pressing the button opens the contact, and the PULLDOWN below drags
#     the pin low.
#
# With that arrangement a cut wire, a pulled connector, a dead shifter or
# an unpowered E-stop circuit all read as "pressed" and inhibit motion. A
# normally-open button wired to pull the pin low would fail the other way:
# the robot would keep running with the E-stop disconnected, which is the
# single most dangerous wiring mistake on this board.
#
# The button must also break motor power directly. A software-visible
# E-stop is a convenience; a contactor in the battery lead is the real one.
#
# This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
# and is released under the GNU General Public License v3 or later.
# -----------------------------------------------------------------------------

# --- E-stop -----------------------------------------------------------------
set_property PACKAGE_PIN F8 [get_ports estop_n]
set_property PULLDOWN true  [get_ports estop_n]

# --- Motor bridge outputs ---------------------------------------------------
set_property PACKAGE_PIN D7 [get_ports m0_in1]
set_property PACKAGE_PIN F7 [get_ports m0_in2]
set_property PACKAGE_PIN F6 [get_ports m1_in1]
set_property PACKAGE_PIN A8 [get_ports m1_in2]

# Bridge inputs must idle low at power-up, before the PL is configured.
set_property PULLDOWN true [get_ports {m0_in1 m0_in2 m1_in1 m1_in2}]

# --- Encoders ---------------------------------------------------------------
set_property PACKAGE_PIN G7 [get_ports enc0_a]
set_property PACKAGE_PIN G5 [get_ports enc0_b]
set_property PACKAGE_PIN A6 [get_ports enc0_z]
set_property PACKAGE_PIN E5 [get_ports enc1_a]
set_property PACKAGE_PIN D5 [get_ports enc1_b]
set_property PACKAGE_PIN C7 [get_ports enc1_z]

# --- Limit switches / bumpers ----------------------------------------------
set_property PACKAGE_PIN C5 [get_ports limit0_fwd]
set_property PACKAGE_PIN E6 [get_ports limit0_rev]
set_property PACKAGE_PIN D6 [get_ports limit1_fwd]
set_property PACKAGE_PIN C8 [get_ports limit1_rev]

# Limits are active high in the RTL. Pull them down so a disconnected
# switch reads "not at the limit" rather than jamming the axis; the
# E-stop, not the limits, is the fail-safe of last resort.
set_property PULLDOWN true [get_ports {limit0_fwd limit0_rev limit1_fwd limit1_rev}]

# --- Bank standard ----------------------------------------------------------
set_property IOSTANDARD LVCMOS18 [get_ports {estop_n \
    m0_in1 m0_in2 m1_in1 m1_in2 \
    enc0_a enc0_b enc0_z enc1_a enc1_b enc1_z \
    limit0_fwd limit0_rev limit1_fwd limit1_rev}]

# --- Timing -----------------------------------------------------------------
# Encoder and limit inputs are asynchronous; the RTL synchronises and
# glitch-filters them, so exclude them from input timing analysis.
set_false_path -from [get_ports {enc0_a enc0_b enc0_z enc1_a enc1_b enc1_z \
    limit0_fwd limit0_rev limit1_fwd limit1_rev estop_n}]

"""MacroRail96 - focus-stacking rail controller for the Avnet Ultra96-V2.

This package turns the Ultra96-V2 into a standalone, WiFi-accessible
replacement for the Windows MacroRail application:

- ``stepper_pl``   drives the hardware-timed stepper/trigger peripheral in
  the FPGA fabric (see ``ultra96/fpga``).
- ``stepper_tic``  drives the existing Pololu TIC controller over USB, so
  the board is useful before any FPGA bitstream is loaded.
- ``camera``       tethers a Nikon body with gphoto2, or fires the optically
  isolated cable-release outputs of the FPGA peripheral.
- ``stacking``     aligns and merges the captured slices into a single
  focus-stacked image with OpenCV.
- ``server``       serves the web UI used to jog, program, and run shoots.

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail)
and is released under the GNU General Public License v3 or later.
"""

__version__ = "0.1.0"

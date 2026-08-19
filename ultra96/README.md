# MacroRail96 — MacroRail on the Avnet Ultra96-V2

This directory turns an [Avnet Ultra96-V2](https://www.avnet.com/wps/portal/us/products/avnet-boards/avnet-board-families/ultra96-v2/)
(Zynq UltraScale+ ZU3EG MPSoC: quad Cortex-A53 + FPGA fabric + WiFi) into a
**standalone, WiFi-controlled focus-stacking rail controller** — replacing the
Windows PC, and adding things a PC can't do:

| Tier | What you get | FPGA work needed |
|------|--------------|------------------|
| **0 — Headless controller** | The board drives your existing Pololu TIC over USB and tethers the Nikon with gphoto2. Control everything from your phone via the built-in WiFi web UI. | None |
| **1 — Hardware-timed motion + trigger** | The included FPGA peripheral generates STEP/DIR pulses with a trapezoidal ramp, enforces limit switches *in hardware*, and fires the camera cable release after an exact settle delay — timing deterministic to 10 ns, immune to Linux scheduling. | Build the provided bitstream |
| **2 — On-board processing** | The quad A53s align and Laplacian-blend the slices into the finished stack right after the shoot. (Future: PL-accelerated stacking and live focus peaking.) | None extra |

Everything here is buildable/runnable today: the RTL passes its self-checking
simulation (Icarus Verilog), and the focus stacker is tested against synthetic
focus slices.

## Directory layout

```
ultra96/
├── fpga/
│   ├── rtl/stepper_ctrl.v         # pulse generator: trapezoid ramp, limits, camera trigger
│   ├── rtl/axi_stepper.v          # AXI4-Lite register interface (map documented in-file)
│   ├── sim/tb_axi_stepper.v       # self-checking testbench (20 checks)
│   ├── constraints/ultra96v2_macrorail.xdc   # pins from Avnet's official XDC
│   └── vivado/build.tcl           # batch build -> .bit / .hwh / .xsa
└── software/
    ├── requirements.txt
    └── macrorail96/
        ├── registers.py           # register map shared with the RTL
        ├── stepper_pl.py          # FPGA peripheral driver (/dev/mem MMIO, no pynq needed)
        ├── stepper_tic.py         # Pololu TIC over USB (ticlib) — Tier 0 path
        ├── camera.py              # gphoto2 tethering + hardware cable-release trigger
        ├── stacking.py            # ECC align + Laplacian-pyramid focus stack (OpenCV)
        ├── server.py              # Flask web app
        └── static/index.html      # phone-friendly UI (jog / zero / shoot)
```

## Quick start — Tier 0 (no FPGA build)

1. Flash the [PYNQ v3 image for Ultra96-V2](http://www.pynq.io/boards.html)
   (or Avnet's PetaLinux BSP) to a microSD card and boot the board.
2. Connect to the board's WiFi AP (or join it to your network) and SSH in.
3. Install the software:

   ```sh
   sudo apt install gphoto2            # Nikon tethering (replaces the Nikon SDK)
   pip3 install -r software/requirements.txt
   ```

4. Plug the Pololu TIC and the Nikon into the board's USB, then:

   ```sh
   cd software
   sudo python3 -m macrorail96.server --backend tic --steps-per-mm 800
   ```

5. Browse to `http://<board-ip>:8096/` from your phone: jog, set the start
   position, enter step count and step size, and start the shoot. Frames are
   downloaded to the board; stack them there with:

   ```sh
   python3 -m macrorail96.stacking /home/xilinx/shoots -o stacked.jpg
   ```

The TIC keeps all its Control Center settings (current limit, accel, limit
switches), so an existing MacroRail rig works unchanged.

> gphoto2/libgphoto2 supports Nikon PTP tethering on ARM Linux, which is what
> removes the Windows-only Nikon SDK dependency noted in the main README.

## Tier 1 — building and using the FPGA peripheral

### Simulate (no Vivado needed)

```sh
sudo apt install iverilog
iverilog -g2012 -o tb fpga/rtl/stepper_ctrl.v fpga/rtl/axi_stepper.v fpga/sim/tb_axi_stepper.v
vvp tb        # expect "ALL TESTS PASSED"
```

### Build the bitstream

Requires Vivado 2021.2+ (the free WebPACK/Standard edition covers the ZU3EG)
and ideally the [Avnet board files](https://github.com/Avnet/bdf):

```sh
cd fpga/vivado
vivado -mode batch -source build.tcl
```

Outputs land in `fpga/vivado/out/`: `macrorail_top.bit` + `.hwh` (PYNQ) and
`.xsa` (PetaLinux/Vitis). On a PYNQ image, load it with:

```python
from pynq import Overlay
Overlay("macrorail_top.bit")   # expects macrorail_top.hwh alongside
```

Then run the server against the fabric:

```sh
sudo python3 -m macrorail96.server --backend pl --hw-trigger
```

### Wiring — read this before plugging anything in

**The 40-pin low-speed header is 1.8 V logic.** It is not 3.3 V or 5 V
tolerant. You need:

- a level shifter (e.g. TXS0108E) between the header and the stepper driver's
  STEP/DIR/EN inputs. The Pololu TIC accepts STEP/DIR on its TX/RX pins when
  configured for "Serial/I2C/USB → STEP/DIR" — or use any plain driver
  (DRV8825, TMC2209) for a fully self-contained rig;
- optocouplers (e.g. PC817, ~330 Ω from a shifted 3.3/5 V drive) on
  `cam_focus`/`cam_shutter` into a Nikon 10-pin (MC-30) or MC-DC2 remote
  cable — never wire the camera release directly;
- a divider or shifter for the LJ8A3-2-Z/BX proximity limit switches (they
  are 5 V devices) down to 1.8 V on `limit_fwd`/`limit_rev`.

Pin assignments (package pins from Avnet's official `ultra96v2_valtest.xdc`):

| Signal | FPGA pin | Header net |
|--------|----------|------------|
| `step_out` | D7 | HD_GPIO_0 |
| `dir_out` | F8 | HD_GPIO_1 |
| `motor_en` | F7 | HD_GPIO_2 |
| `limit_fwd` | G7 | HD_GPIO_3 |
| `cam_focus` | F6 | HD_GPIO_4 |
| `limit_rev` | G5 | HD_GPIO_5 |
| `cam_shutter` | A6 | HD_GPIO_6 |

Cross-check header pin numbers against the Ultra96-V2 Hardware User Guide for
your board revision before wiring.

### Why bother with the FPGA path?

- **Deterministic trigger timing.** Settle delay and shutter pulse are counted
  in 10 ns hardware cycles. Vibration-sensitive high-magnification stacks get
  identical settle time on every frame, no matter what Linux is doing.
- **Hardware limit switches.** A move into an asserted limit halts within one
  step period — no USB latency, no software in the loop. Backing away from the
  limit is still allowed.
- **Focus + shutter sequencing.** The peripheral holds "half-press" through
  the settle window and then pulses the shutter, like a real cable release.
- **It's a template.** The AXI peripheral + driver + testbench is a worked
  example you can clone for any other real-time I/O you want to hang off the
  board (encoders, more axes, lighting/flash sync).

## Register map (fabric peripheral, base 0xA000_0000)

See `fpga/rtl/axi_stepper.v` and `software/macrorail96/registers.py` — they
are kept in lock-step. Times are in cycles of the 100 MHz PL clock.

## Robotics and AI on the same board

The Ultra96-V2 is also a strong robotics controller, and that work lives
alongside this one: see **[ROBOTICS.md](ROBOTICS.md)** for a two-axis
closed-loop motion controller (quadrature encoders, hardware PID, H-bridge
PWM with dead-time) fronted by a hardware safety core — E-stop, command
watchdog, and a one-way speed-limit lock that software cannot raise — plus
a ROS 2 differential-drive node and a safety-gated vision pipeline.

The stepper peripheral here sits at 0xA000_0000 and the motion controller
at 0xA001_0000, so both can be built into one bitstream if you want a
camera rail and a rover on the same board.

Run every simulation and test in this directory with `./run_tests.sh`.

## Other Ultra96-V2 project ideas for this rig

- **Live focus peaking / autofocus metric in the PL**: take the camera's HDMI
  or a USB (UVC) preview into the fabric, compute a Laplacian sharpness metric
  per region in real time, and auto-detect the front/rear focus bounds instead
  of jogging manually.
- **Closed-loop rail**: add a cheap linear/rotary encoder; the fabric counts
  quadrature at MHz rates and the ramp generator becomes a position servo.
- **Multi-axis**: instantiate the stepper peripheral 3–4 times (pan/tilt/
  rotate for photogrammetry turntables) — the ZU3EG has room for dozens.
- **PL-accelerated stacking**: move the align+Laplacian merge into the fabric
  with Vitis HLS for near-instant stacks of full-resolution NEFs.

## Licence

GPL-3.0-or-later, same as the rest of MacroRail.

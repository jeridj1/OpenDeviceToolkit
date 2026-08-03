# ODT RP2040 Bridge

This firmware is the first hardware-facing bridge target for Open Device Toolkit. The RP2040 is treated as a reconfigurable instrument rather than a single-purpose programmer.

The first usable milestone is intentionally conservative: USB command transport, high-impedance GPIO observation, target-voltage measurement, and UART bridge support. SWD/CMSIS-DAP, SPI/I2C and high-speed PIO capture are separate modes and should be added without changing the host-side session model.

Electrical rule: unknown target pins remain inputs. The RP2040 GPIO domain is not five-volt tolerant. Do not connect a target signal until its voltage domain is known and suitable level shifting is provided where required.

Build with the Raspberry Pi Pico SDK. The resulting UF2 can be flashed to a Pico/RP2040 board. The host application should identify the USB CDC serial device and negotiate the bridge protocol before configuring pins.

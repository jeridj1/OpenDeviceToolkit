# ODT RP2040 Bridge

The bridge is a reconfigurable diagnostic instrument. The current milestone supports safe high-impedance GPIO setup, target-voltage measurement, PIO/DMA digital capture, and a simple USB command protocol.

Build requirements: Raspberry Pi Pico SDK and a CMake toolchain for RP2040. Build the `odt_rp2040_bridge` target and flash the generated UF2 to a Pico.

The current USB commands are `HELLO`, `SAFE`, `VOLTAGE`, `SNIFF pin,pin,...`, and `CAPTURE pin sample_rate word_count`.

`CAPTURE` returns packed samples as hexadecimal words. The host side can feed those samples into ODT's edge and protocol analyzers.

Safety: target-facing GPIO starts high impedance. RP2040 GPIO is not five-volt tolerant. A measured target voltage is not permission to connect directly to an incompatible signal; use appropriate level shifting or buffering.

# Experimental Research Engine

## Purpose

The Experimental Research Engine is a planned ODT subsystem for researching obsolete, unusual, undocumented, or otherwise poorly supported hardware that may have little or no existing community documentation.

The goal is not to stop at the first known answer. ODT should systematically determine what a device exposes, what it can communicate with, and what legitimate control or repurposing opportunities exist.

This is a research and evidence-gathering system. It should preserve observations, failed hypotheses, successful discoveries, hardware identity, firmware/build information, and the exact conditions under which a result was obtained.

## Research philosophy

When a device has no known procedure, ODT should progressively:

1. Identify the hardware, processor family, memory, connectors, buses, firmware/build information, and available host interfaces.
2. Determine electrical characteristics and safe operating constraints before driving unknown connections.
3. Enumerate readable interfaces and operating modes such as USB, serial, boot/recovery modes, ADB/Fastboot, UART, SPI, I2C, SWD, JTAG/CMSIS-DAP, GPIO, and other supported transports.
4. Passively observe unknown signals before attempting active interaction whenever practical.
5. Generate protocol hypotheses from timing, voltage, framing, repetition, and captured data.
6. Compare observations against device knowledge, firmware/build databases, protocol signatures, and previously recorded experiments.
7. Try increasingly capable research procedures when the evidence supports them, while avoiding redundant attempts and preserving a complete experiment history.
8. Report confidence, evidence, prerequisites, risk, and likely outcomes instead of presenting guesses as facts.
9. Where a known or documented recovery, unlock, debugging, rooting, or modification path exists, identify it and explain the required prerequisites.
10. Where no known path exists, leave a structured research record that another operator or AI can continue from without repeating completed work.

## Experimental escalation

Research should be staged from passive and reversible observation toward more invasive operations. Each stage should have explicit prerequisites and a recorded result. Persistent or destructive operations require deliberate operator confirmation.

The engine should support a clear distinction between:

- Passive observation
- Non-invasive identification
- Controlled protocol probing
- Reversible interaction
- Persistent modification
- Destructive or irreversible experimentation

The system should never silently cross from one category into another.

## Undocumented and unconventional capabilities

A major research goal is discovering capabilities that were not part of the original intended use of a device. ODT should be able to investigate, where technically appropriate and safely measurable:

- undocumented connectors and test points
- alternate serial or debug interfaces
- unexpected GPIO behavior
- unused peripherals
- alternate signal paths
- unintended electrical or electromagnetic signaling
- acoustic or mechanical side effects of existing hardware
- unusual combinations of existing peripherals
- obsolete hardware that can be repurposed for a new function

These findings should be treated as experimental observations and clearly separated from manufacturer-supported functionality.

## Android and phone research

For phones and similar embedded devices, the engine should eventually be able to build a device-specific capability map covering SoC, boot chain, bootloader state, recovery modes, USB modes, debug interfaces, partition layout, firmware/build identity, available privileges, and known or experimentally demonstrated control paths.

The desired operator experience is: connect a device, let ODT fingerprint it, perform safe reconnaissance automatically, identify the strongest available control paths, and present a research plan when no established path is known.

Unknown vulnerability research should focus on controlled discovery, instrumentation, evidence collection, and reproducible experiments. Any operation intended to defeat a security boundary or permanently alter a device must remain explicitly gated and documented.

## RP2040 development instrument

The RP2040 bridge is the initial experimental hardware platform. It should evolve toward a multifunction research instrument supporting voltage measurement, protected GPIO, digital capture, UART analysis, SPI/I2C experimentation, and eventually JTAG/SWD/CMSIS-DAP and other suitable interfaces.

The desktop application should be able to configure the bridge, capture observations, analyze them, generate hypotheses, and store the results in the target's research history.

## AI continuation / handoff requirement

This document is part of the authoritative project state. Future developers or AI agents should read it together with `ROADMAP.md`, `ARCHITECTURE.md`, source code, tests, and changelog before making changes.

When the Experimental Research Engine gains functionality, update this document and the roadmap so the repository remains self-describing and no requirement depends on private conversation history.

## Current status

This is a planned next-phase subsystem. Existing ODT hardware probing, RP2040 bridge, capture, protocol hypothesis, target-session, and authorization components are groundwork for this design. The engine is not yet a complete autonomous universal hardware research system.

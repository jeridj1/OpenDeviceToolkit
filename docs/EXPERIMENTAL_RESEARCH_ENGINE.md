# Experimental Research Engine

## Purpose

The Experimental Research Engine is the planned ODT subsystem for researching obsolete, damaged, unusual, undocumented, or otherwise poorly supported hardware that may have little or no existing community documentation.

The goal is not to stop at the first known answer or simply report `unsupported`. ODT should systematically determine what a device exposes, what it can communicate with, what it can control, and what useful programming, debugging, recovery, or repurposing opportunities exist.

The engine is intended to become an autonomous research workbench, not merely a collection of fixed protocol tools. It should be able to form hypotheses from evidence, test them, learn from failed attempts, and leave a durable record for another human or AI to continue.

## Desired operator experience

The long-term operator experience is task-oriented:

> Connect or describe a device, state the desired outcome, and let ODT determine the best available path.

For example, the operator may provide a failed older phone or an obscure board and ask ODT to determine whether it can be repurposed for a particular function. ODT should automatically perform reconnaissance, use known procedures when available, and then develop a research plan when no established procedure exists.

The operator should be able to leave a long-running research task operating and return later to a report containing what was discovered, what worked, what failed, what remains unknown, and what the next experiments are.

## Research philosophy

When a device has no known procedure, ODT should progressively:

1. Identify the hardware, processor family, memory, connectors, buses, firmware/build information, and available host interfaces.
2. Determine electrical characteristics and safe operating constraints before driving unknown connections.
3. Enumerate readable interfaces and operating modes such as USB, serial, boot/recovery modes, ADB/Fastboot, UART, SPI, I2C, SWD, JTAG/CMSIS-DAP, GPIO, and other supported transports.
4. Passively observe unknown signals before attempting active interaction whenever practical.
5. Generate protocol and interface hypotheses from timing, voltage, framing, repetition, captured data, topology, and known hardware signatures.
6. Compare observations against device knowledge, firmware/build databases, protocol signatures, tool databases, and previously recorded experiments.
7. Try increasingly capable research procedures when the evidence supports them, avoiding redundant attempts and preserving a complete experiment history.
8. Reassess the hypothesis after each result rather than blindly repeating a failed procedure.
9. Report confidence, evidence, prerequisites, risk, and likely outcomes instead of presenting guesses as facts.
10. Where a known or documented recovery, debugging, programming, or modification path exists, identify it and explain the required prerequisites.
11. Where no known path exists, continue structured research within the currently authorized risk envelope.
12. Leave a structured research record that another operator or AI can continue from without repeating completed work.

## Research stages

Research should distinguish these stages:

- Passive observation
- Non-invasive identification
- Controlled protocol probing
- Reversible interaction
- Persistent modification
- Destructive or irreversible experimentation

The engine may automate steps within an authorized stage. It must never silently cross into a materially higher-risk stage.

## Authorization and experimental escalation

Persistent or destructive experimentation must be explicitly gated. Before escalation, ODT should tell the operator what it intends to try, what evidence led to that plan, what can go wrong, whether recovery is known, and whether permanent damage is possible.

The operator may explicitly authorize an experimental risk envelope for a device that is otherwise failed, obsolete, disposable, or destined for recycling. This authorization means the operator accepts that the research may permanently alter or brick the device.

For irreversible operations, the preferred confirmation flow is deliberately unambiguous and repeated, for example requiring the operator to acknowledge that the device may be permanently damaged. After authorization, ODT may execute configured experiments within that envelope without asking for confirmation for every harmless sub-step. It must stop and request another authorization if a new materially greater risk category is reached.

The authorization record belongs in the research session so later operators and AI agents know what was approved.

## Undocumented and unconventional capabilities

A major research goal is discovering capabilities that were not part of the original intended use of a device. ODT should be able to investigate, where technically appropriate and safely measurable:

- undocumented connectors and test points
- alternate serial or debug interfaces
- unexpected GPIO behavior
- unused peripherals
- alternate signal paths
- unusual combinations of existing peripherals
- obsolete hardware that can be repurposed for a new function
- other technically measurable behaviors suggested by evidence

Findings must be treated as experimental observations and clearly separated from manufacturer-supported functionality.

## Universal programming and instrumentation direction

The long-term goal is not to promise that every device can be programmed. The goal is to give ODT enough instrumentation and research capability to discover a programming or control path when one exists and is technically reachable.

The system should support multiple interchangeable physical instruments and bridges. The initial RP2040 bridge should evolve toward voltage measurement, protected GPIO, digital capture, UART analysis, SPI/I2C experimentation, and eventually JTAG/SWD/CMSIS-DAP and other suitable interfaces.

The desktop application should configure the bridge, capture observations, analyze them, generate hypotheses, execute approved experiments, and store the results in the target's research history.

## Research session record

A persistent research session should retain at least:

- objective
- target identity
- hardware and firmware observations
- physical connection and wiring information
- instrument configuration
- commands, measurements, and captures
- hypotheses considered
- experiments attempted
- results and errors
- artifacts and hashes
- evidence and confidence
- risk category
- authorization record
- current state
- successful discoveries
- failed approaches
- next recommended experiments

This record is essential for long-running autonomous work and AI continuation.

## Android and phone research

For phones and similar embedded devices, the engine should eventually build a device-specific capability map covering SoC, boot chain, bootloader state, recovery modes, USB modes, debug interfaces, partition layout, firmware/build identity, available privileges, and known or experimentally demonstrated control paths.

The desired operator experience is: connect a device, let ODT fingerprint it, perform safe reconnaissance automatically, identify the strongest available control paths, and present a research plan when no established path is known.

Unknown vulnerability research should focus on controlled discovery, instrumentation, evidence collection, and reproducible experiments. Any operation intended to defeat a security boundary or permanently alter a device must remain explicitly gated and documented.

## AI and continuation requirements

This document is part of the authoritative project state. Future developers or AI agents should read `docs/AI_CONTINUATION.md`, `ROADMAP.md`, `ARCHITECTURE.md`, source code, tests, and changelog before making changes.

When the Experimental Research Engine gains functionality, update this document and the roadmap in the same development change. Do not claim a planned autonomous capability is implemented until the behavior exists and has appropriate tests or hardware validation.

## Current status

This is an active next-phase subsystem, not a completed autonomous universal hardware research system. Existing ODT reconnaissance, capability analysis, operation gating, hardware-bridge, capture, protocol-hypothesis, target-session, and authorization components are groundwork for this design. The next priority is to turn that groundwork into a real, resumable, demonstrable research workflow.

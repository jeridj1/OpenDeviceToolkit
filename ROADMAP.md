# Open Device Toolkit Roadmap

## Project goal

Open Device Toolkit (ODT) is a Windows workbench for identifying, inspecting, documenting, diagnosing, backing up, and eventually interacting with electronic devices. The design favors transparent evidence, reversible operations, and explicit confirmation before writes.

The project should produce visible, usable functionality early. Foundational architecture should support the next capability rather than becoming a prerequisite for every capability.

## Milestones

### 0.1 Alpha - Device reconnaissance
- [x] Repository and architecture baseline
- [x] Windows GUI shell
- [x] ADB discovery
- [x] Android device information
- [x] Raw property collection
- [x] Human-readable report generation
- [x] Workspace management
- [ ] Structured application logging
- [ ] Basic environment checks
- [ ] Automated parser tests
- [ ] CI build verification

### 0.15 - First usable workbench workflows
- [ ] User-facing task/workflow model
- [ ] Device capability discovery independent of the UI
- [ ] First end-to-end guided device task using the real application
- [ ] Firmware file identification, metadata, and SHA-256 hashing
- [ ] Device/firmware compatibility evidence model
- [ ] Explicit operation prerequisites and confirmation model
- [ ] Post-operation verification model
- [ ] Recovery/rollback information surfaced before risky operations
- [ ] Plain-language task input designed to sit above structured workflows

This milestone intentionally overlaps the foundation work. The goal is to make ODT useful rather than waiting for a complete architecture before any real interaction is available.

### 0.2 - PC/tooling diagnostics
- [ ] Driver inventory
- [ ] ADB/Fastboot tool detection
- [ ] LG/Qualcomm tool detection
- [ ] Tool version reporting
- [ ] Download manager with checksums

### 0.3 - Android/LG research workbench
- [ ] Partition map inspection
- [ ] A/B slot analysis
- [ ] Boot-chain analysis
- [ ] LG Download Mode detection
- [ ] Qualcomm 9008 detection
- [ ] Device-specific knowledge cards

### 0.4 - Safe backup and snapshots
- [ ] Read-only partition metadata
- [ ] Supported partition backup workflows
- [ ] SHA-256 verification
- [ ] Device snapshots and comparisons
- [ ] Backup manifests

### 0.5 - Plugin architecture
- [ ] Device/provider interfaces
- [ ] Android provider
- [ ] LG provider
- [ ] Qualcomm provider
- [ ] External plugin loading

### 0.6 - Hardware bridge research
- [ ] RP2040 host/bridge abstraction
- [ ] RP2040 serial communication transport
- [ ] UART probing and capture
- [ ] GPIO probing and controlled signaling
- [ ] I2C discovery and transactions
- [ ] SPI discovery and transactions
- [ ] CMSIS-DAP/SWD research and support where appropriate
- [ ] JTAG research and support where appropriate
- [ ] Guided wiring/probe instructions
- [ ] Probe results captured as evidence
- [ ] Firmware read/backup workflows where the target permits them

### 0.7 - Firmware and embedded workbench
- [ ] Firmware identification and metadata
- [ ] Firmware comparison and binary inspection
- [ ] Supported flash-memory backup workflows
- [ ] Target-specific programming workflows
- [ ] Device/provider knowledge cards for common embedded families
- [ ] Guided recovery workflows

### 1.0 - Stable workbench
- [ ] Polished UI
- [ ] Documentation
- [ ] Automated tests
- [ ] Reproducible builds
- [ ] Release packaging

## Current implementation

The repository contains the first functional .NET 8 Windows Forms application, a Core library, and an Android provider. The alpha can discover ADB, enumerate Android devices, collect `getprop`, display important state, and write a text report. A Windows GitHub Actions build workflow has also been added.

The immediate development direction is now to keep strengthening the foundation while simultaneously producing demonstrable workflows that interact with real hardware. Persistent writes remain guarded, but they are no longer treated as something that must wait until the end of the entire project.

## Longer-term research

- RP2040 multifunction bridge
- UART, SPI, I2C, SWD/JTAG/CMSIS-DAP and GPIO adapters
- ESP32/STM32/RP2040 device support
- Serial-console tooling
- Firmware identification and backup
- Hardware probing and protocol discovery
- Plain-language and voice-driven task orchestration over structured workflows

The intended user experience is eventually: connect or describe a device, state the desired outcome, and let ODT determine what evidence, interfaces, tools, firmware, wiring, and prerequisites are needed. The software must clearly distinguish what it knows from what it cannot determine automatically.

These are research goals, not promises of universal automatic identification. Hardware with no readable identity or standard boot protocol may require user input or an adapter-specific probe.

## Safety rule

Read-only inspection comes first for unknown devices. Any operation that can alter firmware, boot partitions, calibration data, security state, or other persistent device state must identify the target, explain the risk, verify prerequisites, provide recovery information where available, require deliberate confirmation, and verify the result afterward.

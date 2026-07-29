# Open Device Toolkit Roadmap

## Project goal
Open Device Toolkit (ODT) is a Windows workbench for identifying, inspecting, documenting, diagnosing, backing up, and eventually interacting with electronic devices. The design favors transparent evidence, reversible operations, and explicit confirmation before writes.

## Milestones

### 0.1 Alpha - Device reconnaissance
- [x] Repository and architecture baseline
- [x] Windows GUI shell
- [x] ADB discovery
- [x] Android device information
- [x] Raw property collection
- [x] Human-readable report generation
- [x] Workspace management
- [x] Structured application logging
- [x] Basic environment checks
- [x] Automated parser tests
- [ ] CI build verification

### 0.2 - PC/tooling diagnostics
- [ ] Driver inventory
- [x] ADB/Fastboot tool detection
- [ ] LG/Qualcomm tool detection
- [x] Tool version reporting
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

### 1.0 - Stable workbench
- [ ] Polished UI
- [ ] Documentation
- [ ] Automated tests
- [ ] Reproducible builds
- [ ] Release packaging

## Current implementation
The repository contains the first functional .NET 8 Windows Forms application, a Core library, and an Android provider. Alpha 0.1 includes local/PATH ADB discovery, structured file logging, environment diagnostics, and automated Android parser/state tests. The 0.2 tooling layer now detects ADB/Fastboot and probes their versions without modifying the host or device. CI is configured to build the application and test project and run the tests; it remains unchecked until a successful workflow run is confirmed.

## Longer-term research
- RP2040 multifunction bridge
- UART, SPI, I2C, SWD, JTAG/CMSIS-DAP and GPIO adapters
- ESP32/STM32/RP2040 device support
- Serial-console tooling
- Firmware identification and backup
- Hardware probing and protocol discovery

These are research goals, not promises of universal automatic identification. Hardware with no readable identity or standard boot protocol may require user input or an adapter-specific probe.

## Safety rule
Read-only inspection comes first. Any future operation that can alter firmware, boot partitions, calibration data, security state, or other persistent device state must identify the target, explain the risk, verify prerequisites, and require deliberate confirmation.

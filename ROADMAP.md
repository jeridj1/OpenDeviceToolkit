# Open Device Toolkit Roadmap

## Project goal
Open Device Toolkit (ODT) is a Windows workbench for identifying, inspecting, documenting, diagnosing, backing up, and interacting with electronic devices. The design favors transparent evidence, reversible operations, and explicit confirmation before high-impact writes.

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
- [x] ADB/Fastboot tool detection
- [x] Tool version reporting
- [ ] Driver inventory
- [ ] LG/Qualcomm tool detection
- [ ] Download manager with checksums

### 0.3 - Android/LG operation workbench
- [x] Operation risk model
- [x] Explicit-confirmation guard
- [x] Guarded ADB reboot operations
- [x] Guarded bootloader/recovery transitions
- [x] Guarded ADB root request
- [ ] Partition map inspection
- [ ] A/B slot analysis
- [ ] Boot-chain analysis
- [ ] LG Download Mode detection
- [ ] Qualcomm 9008 detection
- [ ] Device-specific knowledge cards
- [ ] Fastboot operation provider
- [ ] Firmware flashing workflow

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
ODT now has an explicit operation layer separating read-only, reversible, and destructive actions. Every operation requires a target serial, while high-impact operations can require explicit confirmation. Android currently exposes guarded ADB reboot, bootloader, recovery, and root-request operations. These are library-level capabilities first; the GUI will expose them only after target selection and confirmation UX are implemented.

## Longer-term research
- RP2040 multifunction bridge
- UART, SPI, I2C, SWD, JTAG/CMSIS-DAP and GPIO adapters
- ESP32/STM32/RP2040 device support
- Serial-console tooling
- Firmware identification and backup
- Hardware probing and protocol discovery

## Safety rule
ODT is not permanently read-only. Modification is a core goal. Operations that can alter firmware, boot partitions, calibration data, security state, or other persistent device state must identify the target, describe the expected effect and risk, verify prerequisites, and require deliberate confirmation before execution.

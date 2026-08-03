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
- [x] CI test/build workflow configured

### 0.2 - PC/tooling diagnostics
- [ ] Driver inventory
- [x] ADB/Fastboot tool detection
- [x] Tool version reporting
- [ ] LG/Qualcomm tool detection
- [ ] Download manager with checksums
- [ ] USB device inventory
- [ ] Windows device/driver problem reporting

### 0.3 - Android/LG research workbench
- [x] Read-only boot/USB state probes
- [x] Read-only named partition inspection
- [x] Read-only kernel partition table inspection
- [x] Read-only filesystem space inspection
- [x] Evidence-backed capability analysis
- [x] Capability and modification workflow documentation
- [ ] Partition map inspection and structured parsing
- [ ] A/B slot analysis
- [ ] Boot-chain analysis
- [ ] LG Download Mode detection
- [ ] Qualcomm 9008 detection
- [ ] Device-specific knowledge cards
- [ ] Structured diagnostic findings

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

The alpha now has ADB/Fastboot environment detection, Windows-aware local tool discovery, a visible Environment Check workflow, structured file logging, parser tests, CI test execution, a one-click Deep Read-Only Scan, and evidence-backed capability analysis. Capability analysis deliberately reports Available, Unavailable, Unknown, or RequiresPrivilege rather than pretending that a particular device has a rooting or recovery path when the evidence does not establish one.

The architecture also documents a separate explicit modification workflow. Read-only reconnaissance may run automatically; persistent changes require a known procedure, verified prerequisites, an identified target and artifact, and deliberate confirmation.

## Longer-term research

- Device-specific knowledge cards and firmware/build matching
- Recovery and boot-mode detection
- Qualcomm/LG low-level transport research
- Firmware identification and backup
- RP2040 multifunction bridge
- UART, SPI, I2C, SWD, JTAG/CMSIS-DAP and GPIO adapters
- ESP32/STM32/RP2040 device support
- Serial-console tooling
- Hardware probing and protocol discovery

These are research goals, not promises of universal automatic identification. Devices with no readable identity or standard protocol may require user guidance or specialized hardware.

## Safety rule

Read-only inspection comes first. Any future operation that can alter firmware, boot partitions, calibration data, security state, or other persistent device state must identify the target, explain the risk, verify prerequisites, and require deliberate confirmation.
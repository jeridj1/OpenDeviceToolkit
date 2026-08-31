# Changelog

All notable changes to Open Device Toolkit are documented here.

## Unreleased

### Added

#### Operation guard and safety infrastructure
- OperationGuard.Require: centralized safety gate validating target identification, readiness, and explicit confirmation before any non-read-only operation.
- OperationRisk classification (ReadOnly, StateChange, PersistentWrite) distinguishing reboots from persistent writes.
- PlannedOperation record capturing operation name, risk, readiness, preconditions, and reason.
- OperationBlockedException thrown when a safety precondition is unmet.

#### Firmware artifact handling
- FirmwareArtifactService: device-independent firmware acquisition, SHA-256 hashing, and validation.
- FirmwareArtifact / FirmwareArtifactSpec / FirmwareValidationStatus: artifact model with checksum, size, and model match verification.

#### Fastboot operation provider
- FastbootManager: drives the fastboot CLI via CommandRunner; pure static ParseDevices/ParseGetVar for testability.
- FastbootOperationService: getvar/oem (ReadOnly), reboot (StateChange), flash/erase (PersistentWrite), all gated by OperationGuard.

#### RP2040 guided probe workflow
- ProbeWorkflow: guided connection, observation capture (voltage, logic activity, controller mode), and evidence-based capability inference.
- InferCapabilities: static method inferring safe operations from observed voltage and known chip pinouts.
- 6 tests using MockRp2040Controller (no hardware needed).

#### End-to-end recovery workflow
- RecoveryWorkflow: 6-phase orchestration (identify, detect, validate, backup, guarded operation, verify).
- IRecoveryOperation: injectable interface for mock-testable operations without live devices.

#### Android operation guard wiring
- AndroidOperationService.RebootAsync now funnels through OperationGuard via PlanFor helper.
- OperationRisk.StateChange added to distinguish reboots from persistent writes.

#### UI integration
- RP2040 Bridge button now runs the guided ProbeWorkflow with connection guidance, observations, and capability inference.
- New Validate Firmware button for SHA-256 checksum and artifact validation.
- New Fastboot button for listing devices in fastboot mode via FastbootManager.
- New Recovery Workflow button running the 6-phase end-to-end recovery workflow with phase-by-phase status display.
- 5 new chip pinouts added to PinoutDatabase: nRF52840, SAMD21, STM32F407, ESP8266, CH32V003.
- 10 new PinoutDatabase unit tests covering all new pinouts, USB VID/PID lookup, and case-insensitive search.

### Previous releases

- Initial project roadmap and architecture documentation.
- AI/developer handoff documentation.
- Safety boundary for read-only inspection versus persistent writes.
- Windows-aware ADB/Fastboot tool discovery from the ODT Tools workspace and PATH.
- Environment Check UI with OS, architecture, .NET, workspace, ADB, and Fastboot diagnostics.
- Structured daily application log files under the workspace Logs directory.
- Automated tests for ADB property parsing and tool discovery.
- Windows CI test execution in addition to application builds.
- One-click Deep Read-Only Scan for Android boot, USB, partition, and filesystem diagnostics.
- Dedicated diagnostic reports containing raw probe evidence and execution timing.
- Explicit Experimental Research Engine direction for autonomous hardware reconnaissance.

## Versioning

Until the first stable release, version numbers may use 0.x.y to indicate alpha development.
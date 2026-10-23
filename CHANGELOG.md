# Changelog

All notable changes to Open Device Toolkit are documented here.

## Unreleased

### Added

#### AI assist router
- AiRouter: single analysis entry point that tries configured AI providers in priority order with automatic fallback when a provider fails or is not configured.
- Provider abstraction (IAiProvider) with local Ollama support (no key required) and bring-your-own-key OpenAI-compatible endpoints (Mistral, OpenAI, OpenRouter). Deliberate design decision: no scraping of free web AI tiers.
- Strict response validation: model output must parse as JSON matching the recommendation schema; unknown modes or risk levels are rejected as malformed and the raw response is preserved for audit.
- Operator risk envelope: proposed actions above the authorized envelope (ReadOnly through EWasteMode) are marked blocked and cannot be approved.
- DPAPI-encrypted AI key store: provider keys are encrypted for the current Windows user and stored outside config files and the repository.
- AI configuration section (AppConfig.Ai) with provider priority, models, and Ollama URL.
- Unit tests covering prompt parsing, router fallback, envelope blocking, stubbed provider HTTP handling, and the key store round trip.

#### Dashboard shell and theming
- New dark-mode dashboard entry point with sidebar navigation (Dashboard, AI Assist, Workbench) and status cards for workspace, AI provider, and safety state.
- Shared dark theme module with palette, card factory, and control styling helpers for a consistent visual identity.
- AI Assist panel: provider selection, encrypted key entry, model and endpoint configuration, objective and evidence input, risk-envelope selection with double confirmation for E-Waste mode, risk-colored result cards, approval flow, and audit logging to the Reports directory.

### Fixed

#### Installer workflows
- Repaired build-installer.yml and build-full-installer.yml: the previous YAML was invalid (PowerShell here-string bodies written at column zero broke the block scalars), so every run failed at startup without executing a single job.
- The publish step no longer combines --no-build with a win-x64 runtime that was never restored, and PublishTrimmed is dropped because it is unsupported for WinForms applications.
- Both workflows now re-run when their own workflow file changes so pipeline fixes can be verified.

#### Hardware project cleanup
- Removed duplicate/conflicting RP2040Controller.cs from Rp2040/ subdirectory that caused type conflicts with the split architecture.
- Removed 21 deprecated/unused files from Hardware project:
  - Rp2040BridgeClient.cs, Rp2040BridgeProtocol.cs, Rp2040BridgeMessages.cs
  - Rp2040CaptureClient.cs, Rp2040CaptureParser.cs
  - LogicCaptureAnalyzer.cs (conflicting LogicCapture definition)
  - ProbeAuthorization.cs, ProbeSafety.cs, ProbeExecutionPlanner.cs
  - ProbeModes.cs, ProbeHistory.cs, ProbeEvidence.cs
  - ProbeReport.cs, ProtocolDecoder.cs, ProtocolDetection.cs
  - TargetSession.cs, UartAnalyzer.cs, UartAnalysis.cs
- Removed Compile Remove entries from Hardware.csproj as files are now deleted.
- Added using aliases in ProbeWorkflow.cs and ProbeWorkflowTests.cs to resolve type references across namespaces.

## [0.6.0] - October 2026

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
# Open Device Toolkit

**The digital Swiss Army knife for electronics, firmware, recovery, and embedded-device diagnostics.**

Open Device Toolkit (ODT) is a Windows workbench that identifies, inspects, documents, diagnoses, backs up, and interacts with a wide range of electronic devices from Android phones to RP2040-based microcontroller boards.

> **Current status:** 0.2 Alpha. ODT can discover ADB/Fastboot devices, inspect Android devices, validate firmware artifacts, run guided RP2040 probe workflows, and execute end-to-end recovery workflows all with read-only-first safety guards.

## Implemented capabilities

### Android device operations
- **ADB/Fastboot discovery** Windows-aware tool detection from workspace and PATH
- **Device identification** manufacturer, model, build, security patch, boot state, slot, verified boot status
- **Deep read-only scan** boot, USB, partition, and filesystem diagnostics
- **Reboot operations** system, bootloader, and recovery reboots through OperationGuard
- **Capability planning** evidence-backed next-step recommendations with risk classification

### Firmware handling
- **Artifact acquisition** file size and SHA-256 checksum computation
- **Validation** checksum, size, and model match verification before any write operation
- **Validation status** Verified, HashMismatch, SizeMismatch, ModelMismatch, Missing

### Hardware bridge (RP2040)
- **Guided probe workflow** connection guidance, observation capture (voltage, logic activity), evidence-based capability inference
- **Pinout database** known chip pinouts for STM32F103, RP2040, ESP32, ATmega328P, Snapdragon 855
- **Multi-mode controller** GPIO, UART, SPI, I2C, SWD, JTAG, CMSIS-DAP, logic analyzer, OneWire, CAN

### Safety infrastructure
- **OperationGuard** centralized safety gate for all state-changing operations
- **OperationRisk classification** ReadOnly, StateChange, PersistentWrite
- **Confirmation model** read-only ops need an identified target; state-changing ops need explicit confirmation
- **End-to-end recovery workflow** identify, detect, validate, backup, guarded operation, verify

## First target device

The initial development/test target is an LG V50 ThinQ LM-V450VM running Android 12. The project documentation records known device observations in HANDOFF.md without depending on private conversation history.

## Principles

- **Inspect before modifying.**
- **Show evidence.** Device-state conclusions should be traceable to a command, API, or documented probe.
- **Unknown means unknown.** The toolkit should not guess when evidence is insufficient.
- **Read-only first.** Persistent writes are a separate, explicitly guarded capability.
- **Recoverability matters.** Future firmware operations must identify their target and prerequisites before writing.
- **Portable knowledge.** Device support belongs in providers/plugins rather than being hard-coded into the UI.

## Repository layout

src/
  OpenDeviceToolkit.App/       Windows UI (WinForms)
  OpenDeviceToolkit.Core/      Shared services, research, firmware handling
  OpenDeviceToolkit.Android/   Android/ADB/Fastboot provider, operation guard, recovery workflow
  OpenDeviceToolkit.Hardware/  RP2040 hardware bridge, probe workflow, pinout database

tests/                         Automated tests (xUnit)
docs/                          Developer documentation
.github/workflows/             CI (Windows)

ROADMAP.md                    Feature roadmap
ARCHITECTURE.md               Architecture and design rules
HANDOFF.md                    Continuity guide for future developers/AI
CHANGELOG.md                  Project history

## Build

The application targets .NET 8 / Windows Forms. On Windows with the .NET 8 SDK installed:

dotnet build src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj --configuration Release
dotnet test

CI runs on Windows via GitHub Actions. Core, Android, and Hardware projects treat warnings as errors.

## Safety boundary

ODT is intended for legitimate device repair, diagnostics, development, and research. Write/flash functionality is implemented as explicit capabilities with target identification, prerequisite checks, warnings, confirmation, and verification.

## License

License selection is intentionally deferred until the project owner chooses one.
# Open Device Toolkit

**The digital Swiss Army knife for electronics, firmware, recovery, and embedded-device diagnostics.**

Open Device Toolkit (ODT) is a private-development Windows workbench intended to identify, inspect, document, diagnose, back up, and eventually interact with a wide range of electronic devices. The project starts with Android reconnaissance and is designed to grow through device providers and plugins.

> **Current status:** 0.1 Alpha. The first functional Windows application can discover ADB, inspect connected Android devices, display key device state, and create a text report. It is deliberately read-only at this stage.

## First target device

The initial development/test target is an LG V50 ThinQ LM-V450VM running Android 12. The project documentation records known device observations in `HANDOFF.md` without depending on the user's private conversation history.

## Principles

- **Inspect before modifying.**
- **Show evidence.** Device-state conclusions should be traceable to a command, API, or documented probe.
- **Unknown means unknown.** The toolkit should not guess when evidence is insufficient.
- **Read-only first.** Persistent writes are a separate, explicitly guarded capability.
- **Recoverability matters.** Future firmware operations must identify their target and prerequisites before writing.
- **Portable knowledge.** Device support belongs in providers/plugins rather than being hard-coded into the UI.

## Planned capabilities

### Android and phones

- ADB/Fastboot discovery
- Device identification and property collection
- Partition and A/B-slot inspection
- Boot-chain analysis
- Vendor-specific research tooling
- Safe backup and snapshot workflows
- Firmware identification
- Recovery-mode analysis

### PC tooling

- Driver inventory
- Tool/version detection
- Download management with integrity checks
- Workspace and report management

### Embedded hardware

Long-term research includes ESP32, STM32, RP2040 and other microcontrollers, serial-console tooling, and an RP2040-based multifunction hardware bridge for interfaces such as UART, SPI, I2C, SWD/CMSIS-DAP, GPIO, and potentially JTAG with appropriate hardware support.

The goal is not to pretend that every unknown board can be magically identified. Devices with no readable identity or standard protocol may require user guidance or specialized hardware.

## Repository layout

```text
src/
  OpenDeviceToolkit.App/       Windows UI
  OpenDeviceToolkit.Core/      Shared services and abstractions
  OpenDeviceToolkit.Android/   Android/ADB provider

plugins/                       Future external providers
scripts/                       Development/build helpers
tests/                         Automated tests
docs/                          Developer documentation
.github/workflows/             CI

ROADMAP.md                    Feature roadmap
ARCHITECTURE.md               Architecture and design rules
HANDOFF.md                    Continuity guide for future developers/AI
CHANGELOG.md                  Project history
```

## Local workspace

The default workspace is:

```text
D:\OpenDeviceToolkit\
├── Backups\
├── Drivers\
├── Downloads\
├── Firmware\
├── Logs\
├── Reports\
├── Tools\
└── Workspace\
```

The application will eventually make this configurable. The local workspace is ignored by Git so device data and firmware do not accidentally enter the repository.

## Build

The application targets **.NET 8 / Windows Forms**. On Windows with the .NET 8 SDK installed:

```powershell
dotnet restore src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj
dotnet build src/OpenDeviceToolkit.App/OpenDeviceToolkit.App.csproj --configuration Release
```

The repository also contains a Windows GitHub Actions build workflow under `.github/workflows/build.yml`.

## Continuation / handoff

If development is continued by another person or AI, read these files first:

1. `README.md`
2. `HANDOFF.md`
3. `ROADMAP.md`
4. `ARCHITECTURE.md`
5. `CHANGELOG.md`

Then inspect the actual source tree and recent commits. The repository is the authoritative project state. Do not rely on the original chat as the sole source of requirements.

## Safety boundary

ODT is intended for legitimate device repair, diagnostics, development, and research. Early versions perform read-only inspection. Future write/flash functionality must be implemented as explicit capabilities with target identification, prerequisite checks, warnings, confirmation, and verification rather than exposing unrestricted destructive actions through normal workflows.

## License

License selection is intentionally deferred until the project owner chooses one.
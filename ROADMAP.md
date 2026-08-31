# Open Device Toolkit Roadmap

**Project Goal**: A Windows workbench for identifying, inspecting, documenting, diagnosing, backing up, programming, recovering, reverse-engineering, and repurposing electronic devices.

**Current Focus**: Recovery workflows, expanded pinout database, and full UI integration of all backend capabilities.

---

## Project Goal

Open Device Toolkit (ODT) is a Windows workbench for **autonomous hardware research and repurposing**. The long-term product goal is broader than a fixed list of supported programmers. The operator should be able to:

1. Connect or describe a device
2. State a desired objective (via UI or voice)
3. Let ODT determine the best available path:
   - Known procedures → Guided research → Experimental escalation (when authorized)

**Workflow**: `objective → automatic reconnaissance → known procedures → guided research → experimental escalation (when authorized) → result and durable research record`

The repository is the authoritative project memory. Important requirements and decisions must not depend on private conversation history.

---

## Milestones

### ✅ 0.1 Alpha - Device Reconnaissance **[COMPLETE]**
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

### ✅ 0.2 - PC/Tooling Diagnostics **[MOSTLY COMPLETE]**
- [x] Driver inventory (via USB enumeration)
- [x] ADB/Fastboot tool detection
- [x] Tool version reporting
- [ ] LG/Qualcomm tool detection (stubbed)
- [ ] Download manager with checksums
- [x] USB device inventory (fully implemented via WMI)
- [ ] Windows device/driver problem reporting (partial)

### ✅ 0.3 - Android/LG Research Workbench **[PARTIALLY COMPLETE]**
- [x] Read-only boot/USB state probes
- [x] Read-only named partition inspection
- [x] Read-only kernel partition table inspection
- [x] Read-only filesystem space inspection
- [x] Evidence-backed capability analysis
- [x] Capability and modification workflow documentation
- [x] Controlled Android reboot operations
- [x] Explicit ADB file pull/push service
- [x] Controlled APK installation service
- [ ] Partition map inspection and structured parsing
- [ ] A/B slot analysis (partial)
- [ ] Boot-chain analysis (stubbed)
- [ ] LG Download Mode detection (stubbed)
- [ ] Qualcomm 9008 detection (stubbed)
- [ ] Device-specific knowledge cards
- [ ] Structured diagnostic findings (partial)

### 🎯 0.4 - Safe Backup and Snapshots **[NOT STARTED]**
- [ ] Read-only partition metadata
- [ ] Supported partition backup workflows
- [ ] SHA-256 verification
- [ ] Device snapshots and comparisons
- [ ] Backup manifests

### 🎯 0.5 - Plugin Architecture **[NOT STARTED]**
- [ ] Device/provider interfaces
- [ ] Android provider
- [ ] LG provider
- [ ] Qualcomm provider
- [ ] External plugin loading

### ✅ 0.6 - Experimental Research Engine **[PARTIALLY COMPLETE - MAJOR PROGRESS]**

**Core Research Engine (NEW - Implemented in feature/stabilize-foundation)**:
- [x] Research-session model and persistent experiment history
- [x] Automatic hardware/interface fingerprinting (USB enumeration)
- [x] Progressive passive-to-active probing workflow (RiskLevel system)
- [x] Automatic protocol/interface hypothesis generation
- [x] Evidence-backed hypothesis ranking and deduplication
- [x] AI-friendly research reports and continuation handoff
- [x] Long-running task execution and resumable sessions
- [x] Explicit escalation gates for persistent or destructive operations
- [x] Operator-authorized experimental risk envelope

**Additional Features (Beyond Original Roadmap)**:
- [x] **Voice Interaction**: Hands-free operation with speech recognition (System.Speech)
- [x] **GitHub Search**: Online exploit/datasheet/code search with confidence scoring
- [x] **RP2040 multifunction research instrument integration** (interface + mock implementation)
- [x] **UART/SPI/I2C/JTAG/SWD research workflows** (mode definitions)
- [x] **Device-specific research plans and knowledge cards** (pinout database)
- [x] **Unconventional/undocumented capability research** (hypothesis system)

**Remaining for 0.6**:
- [ ] Additional research sources (XDA Forums, Exploit-DB, local database)
- [ ] Offline hypothesis testing (without online search)
- [ ] RP2040 actual hardware communication (serial/USB implementation)
- [ ] Logic analyzer visualization and protocol decoding
- [ ] Complete device-specific knowledge cards

### 🔮 1.0 - Stable Workbench **[NOT STARTED]**
- [ ] Polished UI
- [ ] Complete documentation
- [ ] Full automated test coverage
- [ ] Reproducible builds
- [ ] Release packaging

---

## Current Implementation (feature/stabilize-foundation)

### ✅ Completed Features

#### Core Infrastructure
- **Configuration System**: `AppConfig.cs` with nested settings classes
  - WorkspaceConfig, AdbConfig, LoggingConfig, VoiceConfig, ResearchConfig
  - JSON-based appsettings.json
  - `Config.Current` for global access
- **Workspace Management**: Configurable root, auto-creates subdirectories
- **Tool Discovery**: Enhanced `ToolLocator` with custom paths, SDK detection, PATH search
- **USB Enumeration**: Full WMI-based device detection with driver info, Android detection
- **Logging**: Structured, timestamped, configurable retention

#### Android Reconnaissance
- **ADB Discovery**: Detects connected devices, parses `adb devices` output
- **Device Inspection**: Reads all properties via `adb shell getprop`
- **Environment Diagnostics**: Checks ADB, Fastboot, .NET, workspace, USB
- **Deep Read-Only Scan**: Boot, USB, partition, filesystem diagnostics
- **Capability Analysis**: Determines available operations with evidence
- **Operation Planning**: Generates workflows with risk assessment

#### Research Engine
- **RiskLevel**: 5-tier classification (ReadOnly → EWasteMode)
- **ResearchSession**: Tracks hypotheses, tests, results, progress
- **ResearchPlan**: Structured step-by-step workflows
- **ResearchResult**: Search results with confidence and risk scoring
- **ResearchHypothesis**: Potential methods with evidence
- **ResearchEngine**: Main coordinator
- **GitHubSearch**: Online search implementation
- **IResearchSource**: Extensible source interface

#### Voice Interaction
- **SpeechService**: Windows.Speech STT and TTS
- **VoiceCommandParser**: Natural language → structured commands
- **VoiceCommand**: Command model with type, device, objective
- **VoiceCommandType**: ScanDevice, GainAccess, GenerateReport, RebootDevice, Help, Exit, CustomObjective
- **Voice Mode**: UI toggle with visual feedback
- **Voice Panel**: Text input fallback

#### RP2040 Hardware Bridge
- **IRp2040Controller**: Interface with 10 protocol modes
- **Rp2040Mode**: GPIO, UART, SPI, I2C, SWD, JTAG, CMSIS-DAP, LogicAnalyzer, 1-Wire, CAN
- **Rp2040ControllerBase**: Base implementation
- **MockRp2040Controller**: For testing without hardware
- **SerialRp2040Controller**: Serial port (stub)
- **UsbRp2040Controller**: USB (stub)
- **Rp2040ControllerFactory**: Automatic controller selection
- **PinoutDatabase**: Known chips (STM32F103, RP2040, nRF52840, SAMD21, STM32F407, ESP8266, CH32V003, ESP32, ATmega328P, Snapdragon 855)
- **ChipPinout, PinInfo, PinType**: Chip and pin models
- **ProgrammingInterface**: Protocol interface definitions
- **LogicCapture, LogicSample**: Signal capture models

#### User Interface
- **MainForm**: Enhanced with all new features
- **Voice Toggle**: Enable/disable voice mode
- **Research Button**: Initiates research workflow
- **RP2040 Button**: Runs guided ProbeWorkflow with observations and capability inference
- **Firmware Button**: Validates firmware artifacts (SHA-256, size, model)
- **Fastboot Button**: Lists devices in fastboot mode
- **Recovery Workflow Button**: Runs 6-phase end-to-end recovery workflow
- **E-Waste Mode**: Checkbox with visual indicator
- **Voice Panel**: Hidden panel for voice input

#### Project Configuration
- All projects target **.NET 8.0**
- **System.Speech** reference added for voice
- **System.Management** for USB enumeration
- **Moq** added for unit testing

#
### Tests
- **ResearchEngineTests**: Unit tests for research components
- **SpeechServiceTests**: Unit tests for voice command parsing

---

## Detailed Design

The detailed design and intended behavior are documented in:
- `docs/ARCHITECTURE.md` - Design principles and runtime layers
- `docs/EXPERIMENTAL_RESEARCH_ENGINE.md` - Research engine design (to be created)
- `docs/AI_CONTINUATION.md` - AI/developer continuation protocol (to be created)
- `docs/IMPLEMENTATION_PROGRESS.md` - Detailed implementation status

---

## Workflow and Operation Model

A structured workflow exposes:
- User objective
- Target device
- Required capabilities and instruments
- Evidence gathered so far
- Prerequisites
- Planned operations
- Risk level
- Authorization state
- Recovery/backup information
- Execution results
- Post-operation verification
- Recommended next step

**Workflow**: The workflow engine is usable by button, script, future voice/plain-language layer, or autonomous research loop without changing the underlying device logic.

---

## Workspace

Default workspace: `D:\OpenDeviceToolkit\` (configurable via appsettings.json)

```text
D:\OpenDeviceToolkit\
├── Backups\
├── Drivers\
├── Downloads\
├── Firmware\
├── Logs\
├── Reports\
├── Tools\
└── Workspace\  (contains WorkspaceData for research sessions)
```

The program makes the workspace configurable. The local workspace is ignored by Git so device data and firmware do not accidentally enter the repository.

---

## Development Direction

The immediate development priority is to **stabilize the foundation** and **complete the Research Engine integration**. Each new capability should be:
1. Demonstratable
2. Tested
3. Documented
4. Recorded (so another developer or AI can continue)

The eventual research engine should investigate devices with little or no public documentation. It should retain:
- Observations
- Measurements
- Hypotheses
- Failures
- Successes
- Artifacts
- Next steps

It should be capable of discovering 
useful programming, debugging, recovery, control, or repurposing paths that are not already encoded as a standard provider procedure.

---

## Safety and Authorization

**Core Principle**: Read-only inspection comes first for unknown devices.

**Persistent or destructive actions require**:
1. Identified target
2. Understood operation
3. Prerequisites
4. Expected effect
5. Risk information
6. Deliberate authorization

**E-Waste Mode**: For disposable or already-failed devices, the operator may explicitly authorize an experimental risk envelope that allows the research engine to continue into potentially irreversible experiments. ODT must:
1. Make the possibility of permanent damage **unmistakable**
2. Obtain explicit confirmation (preferably **twice** for irreversible operations)
3. Record what was authorized and what it attempted

---

## Project Continuity Rule

Future developers or AI agents should:
1. Begin with `docs/AI_CONTINUATION.md` (to be created)
2. Read this roadmap (`ROADMAP.md`)
3. Read `ARCHITECTURE.md`
4. Read `docs/EXPERIMENTAL_RESEARCH_ENGINE.md` (to be created)
5. Review relevant source/tests
6. Read `CHANGELOG.md`

**When implementation status changes**: Update the roadmap  
**When architecture changes**: Update ARCHITECTURE.md  
**When research behavior changes**: Update research-engine documentation  

**Do not leave requirements only in conversation history.**

---

## Current Branch Status

**Branch**: `main`  
**Status**: 0.2 Alpha with full UI integration of recovery workflows, expanded pinout database, and operation guard  
**Next**: Expand test coverage, add LG/Qualcomm tool detection, implement safe backup workflows

---

**Last Updated**: August 31, 2026

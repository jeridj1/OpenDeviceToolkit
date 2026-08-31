# Open Device Toolkit Handoff Guide

**Last Updated**: August 14, 2026  
**Branch**: `feature/stabilize-foundation`  
**Status**: Active Development - Major Features Implemented

---

## What This Project Is

**Open Device Toolkit (ODT)** is a Windows-based device diagnostics, firmware, recovery, and electronics workbench. The project has evolved from a simple Android reconnaissance tool into a **comprehensive hardware research platform** with:

- **Automated device discovery and inspection**
- **Online research capabilities** (GitHub, forums, exploit databases)
- **Offline autonomous research** (protocol probing, hypothesis testing)
- **RP2040-based hardware bridge** (universal programmer + logic analyzer)
- **Voice interaction** (hands-free operation)
- **Experimental research engine** (for unknown device access)

---

## Current Implementation State

### ✅ Fully Implemented (Alpha 0.1 + 0.2 + Partial 0.6)

#### Core Infrastructure
- **Configuration System**: `AppConfig.cs` with JSON-based settings
  - Workspace paths, ADB configuration, logging, voice, research settings
  - `Config.Current` provides global access
- **Workspace Management**: Configurable root directory, auto-creates subdirectories
- **Tool Discovery**: Enhanced `ToolLocator` with SDK path detection
- **USB Enumeration**: Full WMI-based device detection with driver info
- **Logging**: Structured, timestamped logs with configurable retention

#### Android Reconnaissance
- **ADB Discovery**: Detects connected Android devices
- **Device Inspection**: Reads all device properties via `adb shell getprop`
- **Environment Diagnostics**: Checks ADB, Fastboot, .NET, workspace, USB inventory
- **Deep Read-Only Scan**: Boot, USB, partition, filesystem diagnostics
- **Capability Analysis**: Determines available operations for device
- **Operation Planning**: Generates safe workflows with risk assessment

#### Research Engine (Partial Milestone 0.6)
- **Risk Level System**: 5-tier risk classification with confirmation gates
  - `ReadOnly`, `Reversible`, `PersistentWrite`, `PotentialBrick`, `EWasteMode`
  - Extension methods for descriptions, colors, and confirmation requirements
- **Research Sessions**: Tracks hypotheses, tests, results, and progress
- **Research Plans**: Structured workflows for testing hypotheses
- **GitHub Search**: Online exploit/datasheet/code search
- **Hypothesis Generation**: Creates testable hypotheses from search results
- **Plan Execution**: Step-by-step testing with user confirmation

#### Voice Interaction
- **Speech Service**: Windows.Speech-based STT and TTS
- **Command Parser**: Natural language → structured commands
- **Supported Commands**: ScanDevice, GainAccess, GenerateReport, RebootDevice, Help, Exit, CustomObjective
- **Voice Mode**: Toggle on/off, visual feedback, text fallback

#### RP2040 Hardware Bridge
- **Controller Interface**: `IRp2040Controller` with 10 protocol modes (GPIO, UART, SPI, I2C, SWD, JTAG, CMSIS-DAP, LogicAnalyzer, 1-Wire, CAN)
- **Pinout Database**: Known chip configurations (STM32F103, RP2040, etc.)
- **Mock Implementation**: For testing without hardware
- **Serial/USB stubs**: Ready for hardware integration
- **Factory Pattern**: Automatic controller selection

#### User Interface
- **MainForm**: Windows Forms with all features integrated
- **Voice Panel**: Hidden by default, appears when voice mode enabled
- **Research Button**: Initiates research workflow
- **RP2040 Button**: Connects and displays bridge info
- **E-Waste Mode**: Checkbox for irreversible experiments (red when enabled)

---

## Current Test Device Context

Known from user-provided ADB output:
- Model: LM-V450 (LG V50 ThinQ)
- Carrier variant: LM-V450VM / Verizon
- Android: 12
- Software: V450VM40a
- Kernel: 4.14.190-perf+
- Platform: msmnile (Qualcomm Snapdragon 855 family)
- Hardware: flashlm
- Security patch: 2022-05-01
- A/B slots: yes; active slot previously observed as `_a`
- `ro.boot.flash.locked`: `1`
- `ro.boot.verifiedbootstate`: `green`
- `ro.boot.vbmeta.device_state`: `locked`
- `sys.oem_unlock_allowed`: `0`
- `adb reboot edl`: rebooted normally rather than exposing Qualcomm 9008
- `adb reboot bootloader`: rebooted normally
- `/dev/block/by-name` is readable without root and showed A/B boot-chain partitions

---

## Immediate Implementation Target

### Next Steps (Priority Order)

1. **Verify all existing features work**
   - Test ADB discovery with physical device
   - Test environment diagnostics
   - Test deep scan
   - Verify CI build passes

2. **Complete Research Engine**
   - Add more research sources (XDA Forums, Exploit-DB, local database)
   - Implement offline fingerprinting (USB IDs, partitions, bootloader responses)
   - Add hypothesis testing logic

3. **Enhance Voice Interaction**
   - Add more command variations and synonyms
   - Improve natural language parsing
   - Add voice feedback for all major operations

4. **Implement RP2040 Hardware Communication**
   - Serial port connection
   - USB HID communication
   - Mode switching commands
   - Pin configuration

5. **Add Logic Analyzer Visualization**
   - Signal waveform display
   - Protocol decoding (UART, SPI, I2C)
   - Timing analysis

---

## Important Constraints (Still Apply)

✅ **DO**:
- Keep read-only by default
- Require explicit user confirmation for writes
- Log all operations with evidence
- Make evidence traceable
- Use explicit `Unknown` state when uncertain
- Escalate from safe to experimental methods

❌ **DO NOT**:
- Silently modify devices
- Flash partitions without confirmation
- Assume bootloader unlock paths exist
- Invent firmware compatibility
- Guess when evidence is insufficient
- Perform destructive actions without explicit authorization

⚠️ **Experimental Mode**:
- Only enabled when user explicitly checks "E-Waste Mode"
- Requires double confirmation for irreversible actions
- Clearly warns about bricking risk
- Logs all experimental actions
- User accepts full responsibility

---

## Long-Term Ideas (From User Requirements)

### ✅ Now Implemented
- **Automatic online search**: GitHub search for exploits/datasheets
- **Voice interaction**: Hands-free operation with speech recognition
- **RP2040 universal programmer**: Interface abstraction with multiple modes
- **E-Waste mode**: Explicit authorization for irreversible experiments
- **Research engine**: Coordinates device investigation

### 🎯 Next to Implement
- **Offline autonomous research**: When online search fails or is disabled
  - Protocol hypothesis generation
  - Signal analysis and pattern detection
  - Brute-force testing (with user approval)

- **Automatic pinout configuration**: "Tell it what device, it configures RP2040"
  - Chip detection via USB VID/PID
  - Automatic pin mapping from database
  - User confirmation before connecting

- **Closed-loop automation**: "Plug in device, explain goal, it figures out how"
  - Device identification → objective analysis → method selection → execution
  - Progressive escalation from safe to experimental methods
  - User approval gates at each risk level

- **Online research expansion**:
  - XDA Forums search
  - Exploit-DB integration
  - Local exploit database
  - Community-contributed knowledge

---

## Continuation Procedure

### Before Making Changes:

1. **Read this file** (HANDOFF.md)
2. **Read `docs/IMPLEMENTATION_PROGRESS.md`** for detailed status
3. **Read `ROADMAP.md`** for planned features
4. **Read `ARCHITECTURE.md`** for design principles
5. **Inspect the source tree** and recent commits in `feature/stabilize-foundation`
6. **Test current build** before making changes

### Implementation Status Tracking:

- **Completed tasks**: Mark with [x] in this file and ROADMAP.md
- **New features**: Add to appropriate section with status
- **Bug fixes**: Document in CHANGELOG.md
- **Breaking changes**: Update ARCHITECTURE.md if needed

### Commit Guidelines:

- **Small, focused commits**: One logical change per commit
- **Descriptive messages**: Explain what and why, not just what
- **Reference related files**: Mention which components are affected
- **Test before committing**: Ensure CI passes (or will pass with changes)

---

## Current Test Status

### Working Features (Likely Working)
- [ ] Configuration loading from appsettings.json
- [ ] Workspace directory creation
- [ ] ADB discovery and device inspection
- [ ] Environment diagnostics
- [ ] USB device enumeration
- [ ] Deep read-only scan
- [ ] Voice mode toggle
- [ ] Voice command parsing
- [ ] Research session creation
- [ ] GitHub search
- [ ] RP2040 controller connection (mock)
- [ ] E-Waste mode toggle

### Features Needing Verification
- [ ] CI build passes (needs System.Speech on Windows)
- [ ] All unit tests pass
- [ ] Multi-device detection
- [ ] Report generation
- [ ] Workspace opening

---

## Safety Boundary (Reiterated)

ODT is **read-only by default**. All persistent writes require:
1. **Explicit user confirmation** (dialog box with clear description)
2. **Risk disclosure** (what will happen, potential consequences)
3. **Recovery information** (how to undo if possible)
4. **Logging** (what was done, when, by whom)

For **E-Waste Mode** (user accepts bricking):
1. **Double confirmation** required for irreversible actions
2. **Clear warning** about permanent damage
3. **Explicit acceptance** via checkbox
4. **Complete audit trail** in logs
5. **User assumes all risk**

---

## Repository Structure

```text
OpenDeviceToolkit/
├── src/
│   ├── OpenDeviceToolkit.App/          # Windows UI
│   ├── OpenDeviceToolkit.Core/         # Shared services, configuration
│   │   ├── Research/                   # Research Engine components
│   │   └── Speech/                     # Voice interaction
│   ├── OpenDeviceToolkit.Android/      # ADB and Android-specific
│   └── OpenDeviceToolkit.Hardware/     # Hardware bridge, RP2040
│       └── Rp2040/                     # RP2040 controller and pinouts
├── tests/
│   └── OpenDeviceToolkit.Tests/        # Unit tests
│       ├── Research/                   # Research Engine tests
│       └── Speech/                     # Voice interaction tests
├── .github/
│   └── workflows/
│       └── build.yml                   # CI configuration
└── docs/
    ├── IMPLEMENTATION_PROGRESS.md      # Detailed progress tracking
    ├── ARCHITECTURE.md                  # Design principles
    ├── ROADMAP.md                       # Feature roadmap
    └── HANDOFF.md                       # This file
```

---

**Repository is the authoritative project memory.**
**Do not rely on private conversation history.**
**Update documentation as you implement.**

# Open Device Toolkit Architecture

## Product direction

ODT is intended to become a general-purpose hardware reconnaissance, diagnostic, programming, recovery, reverse-engineering, and repurposing workbench. The long-term interface is task-oriented: the operator states an objective and ODT determines the best available path from known procedures through experimental research when necessary.

## Design principles

1. **Inspect before modifying.** Reconnaissance and evidence collection are the default.
2. **Explain decisions.** Device state should be derived from visible commands, APIs, measurements, or documented probes and shown to the user.
3. **Fail safely.** Unknown conditions should produce an explicit unknown state rather than a guess.
4. **Separate device knowledge from UI.** Device support belongs in providers/plugins, not in the presentation layer.
5. **Keep recovery in mind.** Operations that can alter persistent state must have prerequisites, warnings, authorization, and verification.
6. **Make work reproducible.** Reports, logs, versions, hashes, snapshots, measurements, and experiment history should make it possible to understand what happened later.
7. **Prefer useful end-to-end capability.** Architecture work should enable demonstrable hardware workflows rather than becoming an endless prerequisite.
8. **Treat the repository as memory.** Requirements, decisions, implementation status, discoveries, and next steps must be recorded in the repository.

## Initial solution shape

```text
OpenDeviceToolkit/
├── src/
│   ├── OpenDeviceToolkit.App/       # Windows UI and application composition
│   ├── OpenDeviceToolkit.Core/      # Device abstractions, workflows, logging, configuration
│   ├── OpenDeviceToolkit.Android/   # ADB and Android-specific inspection
│   └── OpenDeviceToolkit.Qualcomm/  # Future Qualcomm/EDL research support
├── plugins/                         # Future external providers
├── tests/                           # Automated tests
├── docs/                            # User/developer/research documentation
├── scripts/                         # Build/development helpers
└── tools/                           # Optional local tool integrations
```

## Runtime layers

### App/UI
Responsible for presentation, user interaction, task entry, navigation, results, and confirmation dialogs. It should not contain device-specific probing logic.

### Task/workflow layer
Converts a user objective into a structured workflow or research plan. Future natural-language or voice interfaces belong above this layer. The workflow engine remains authoritative about actual capabilities, prerequisites, evidence, risk, authorization, and execution.

### Core
Provides shared abstractions and services such as device models, capabilities, workflows, experiment records, logging, command execution, configuration, workspace paths, hashing, evidence, and reports.

### Providers and instruments
A provider knows how to communicate with a device family or transport. An instrument/bridge provides physical access to electronics. Providers and instruments should be composable so the application is not locked to one programmer or one bridge board.

### External tools
ADB, Fastboot, vendor utilities, diagnostic tools, firmware utilities, and other external programs are treated as dependencies. ODT should detect versions and paths rather than silently assuming they exist.

## Hardware bridge and research instrumentation

The initial research instrument is an RP2040-based multifunction bridge. The abstraction should remain transport/protocol oriented so other hardware can be added later.

Candidate capabilities include protected GPIO, voltage measurement, digital capture, UART, I2C, SPI, CMSIS-DAP/SWD, JTAG, and other interfaces supported by the physical instrument. Unknown signals should be captured as evidence before being interpreted as fact.

The bridge is not merely a programmer adapter. It is intended to become an instrument that lets ODT observe, test, and characterize unfamiliar hardware.

## Device abstraction

A device provider should be able to answer questions such as:

- What device is connected?
- How was it detected?
- What evidence establishes its identity?
- What capabilities are available, unavailable, unknown, or privilege-gated?
- What information can be read safely?
- Which operations are supported?
- Which interfaces or instruments are required?
- What prerequisites and recovery options exist?

## Experimental Research Engine

When no established procedure is known, ODT should not simply stop at "unsupported." The Experimental Research Engine should progressively fingerprint hardware, observe signals, form protocol/interface hypotheses, test them, compare results against existing knowledge, and retain successful and failed experiments.

Research must distinguish passive observation, non-invasive identification, controlled probing, reversible interaction, persistent modification, and destructive experimentation. The engine may progress automatically within an authorized risk envelope, but it must never silently cross into a materially higher-risk category.

The detailed design and intended behavior are documented in `docs/EXPERIMENTAL_RESEARCH_ENGINE.md`.

## Workflow and operation model

A structured workflow should expose at least:

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

The workflow engine should be usable by a button, script, future voice/plain-language layer, or autonomous research loop without changing the underlying device logic.

## Workspace

The application should default to a user-selected workspace, with the current planned Windows layout:

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

The program must not assume that D: exists forever. Workspace configuration will eventually be user-selectable.

## Handoff protocol

The repository is the authoritative project memory. Future developers and AI agents should read `docs/AI_CONTINUATION.md`, `ROADMAP.md`, `ARCHITECTURE.md`, `docs/EXPERIMENTAL_RESEARCH_ENGINE.md`, relevant source/tests, and `CHANGELOG.md` before continuing.

When a milestone changes, update `ROADMAP.md`. When architecture changes, update this document. When experimental-research behavior changes, update the research-engine document. When user-visible behavior changes, update `CHANGELOG.md`. Do not leave important requirements only in conversation history.

## Build direction

The planned desktop implementation is C#/.NET eight with a Windows desktop UI and single-file publishing for distribution. The exact UI framework can change if implementation evidence supports a better fit.

## Security and authorization boundary

ODT may eventually interact with firmware, bootloaders, recovery environments, debug interfaces, and low-level hardware. Persistent or destructive operations must be explicit capabilities rather than unrestricted arbitrary command execution exposed through normal UI workflows.

For unknown hardware, the operator may explicitly authorize an experimental risk envelope that can include permanent device damage. The application must clearly state that risk and obtain deliberate confirmation, preferably twice for irreversible operations. After authorization, ODT may perform the configured research steps within that envelope and must preserve a complete record of what it attempted and learned.

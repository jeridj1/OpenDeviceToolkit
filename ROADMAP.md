# Open Device Toolkit Roadmap

## Project goal

Open Device Toolkit (ODT) is a Windows workbench for identifying, inspecting, documenting, diagnosing, backing up, programming, recovering, reverse-engineering, and repurposing electronic devices.

The long-term product goal is broader than a fixed list of supported programmers. The operator should be able to connect or describe a device, state a desired outcome, and let ODT determine the best available path. For common hardware, that means using established procedures. For obsolete, damaged, undocumented, or unusual hardware, it means systematically researching what the device exposes and finding a workable path when one exists.

The intended workflow is:

`objective -> automatic reconnaissance -> known procedures -> guided research -> experimental escalation when authorized -> result and durable research record`

The repository is the authoritative project memory. Important requirements and decisions must not depend on private conversation history.

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
- [x] Controlled Android reboot operations
- [x] Explicit ADB file pull/push service
- [x] Controlled APK installation service
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

### 0.6 - Experimental Research Engine
- [ ] Research-session model and persistent experiment history
- [ ] Automatic hardware/interface fingerprinting
- [ ] Progressive passive-to-active probing workflow
- [ ] Automatic protocol/interface hypothesis generation
- [ ] UART/SPI/I2C/JTAG/SWD research workflows
- [ ] RP2040 multifunction research instrument integration
- [ ] Device-specific research plans and knowledge cards
- [ ] Unconventional/undocumented capability research
- [ ] Evidence-backed hypothesis ranking and deduplication
- [ ] AI-friendly research reports and continuation handoff
- [ ] Long-running task execution and resumable sessions
- [ ] Explicit escalation gates for persistent or destructive operations
- [ ] Operator-authorized experimental risk envelope

The detailed design is documented in `docs/EXPERIMENTAL_RESEARCH_ENGINE.md` and the continuation protocol is documented in `docs/AI_CONTINUATION.md`.

### 1.0 - Stable workbench
- [ ] Polished UI
- [ ] Documentation
- [ ] Automated tests
- [ ] Reproducible builds
- [ ] Release packaging

## Current implementation

The repository currently contains a functional .NET eight Windows application and Android reconnaissance/tooling capabilities. It includes ADB/Fastboot environment detection, Windows-aware local tool discovery, environment diagnostics, structured file logging, parser tests, CI configuration, a Deep Read-Only Scan, evidence-backed capability analysis, explicit operation gating, Android reboot controls, ADB file transfer, and controlled APK installation.

The repository also contains architectural and research groundwork for an RP2040 multifunction bridge and an Experimental Research Engine. Those are not yet a finished autonomous universal hardware research/programming system.

Capability analysis deliberately reports `Available`, `Unavailable`, `Unknown`, or `RequiresPrivilege` rather than pretending that an unproven procedure exists.

## Development direction

The immediate development priority is to turn the existing architecture into visible end-to-end hardware workflows while continuing the foundation. Each new capability should be demonstrable, tested, documented, and recorded so another developer or AI can continue without reconstructing the history from conversation.

The eventual research engine should be able to investigate devices that have little or no public documentation. It should retain observations, measurements, hypotheses, failures, successes, artifacts, and next steps. It should be capable of discovering useful programming, debugging, recovery, control, or repurposing paths that are not already encoded as a standard provider procedure.

## Safety and authorization

Read-only inspection comes first for unknown devices. Persistent or destructive actions require an identified target, an understood operation, prerequisites, expected effect, risk information, and deliberate authorization.

For an otherwise disposable or already-failed device, the operator may explicitly authorize an experimental risk envelope that allows the research engine to continue into potentially irreversible experiments. ODT must make the possibility of permanent damage unmistakable and obtain explicit confirmation, preferably twice for irreversible actions. It must then record what was authorized and what it attempted.

## Project continuity rule

Future developers or AI agents should begin with `docs/AI_CONTINUATION.md`, then read this roadmap, `ARCHITECTURE.md`, `docs/EXPERIMENTAL_RESEARCH_ENGINE.md`, relevant source/tests, and `CHANGELOG.md`. When implementation status changes, update the roadmap. When architecture or research behavior changes, update the corresponding documentation. Do not leave requirements only in private conversation history.

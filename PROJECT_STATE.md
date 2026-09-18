# Open Device Toolkit Project State

**State owner:** repository, not conversation history  
**Updated:** 2026-09-18  
**Current branch:** foundation/autonomous-agent-contract

## Mission

ODT is being developed toward a general-purpose Android/Windows-assisted hardware laboratory that can identify, inspect, diagnose, communicate with, recover, program, reverse-engineer, and repurpose unfamiliar electronic devices.

The long-term interaction is objective-driven: the operator connects or describes a device and states what they want accomplished. ODT should select an evidence-backed path through known procedures, guided research, and explicitly authorized experimentation.

## Current reality

The existing application is a .NET eight Windows Forms workbench. It already contains Android/ADB/Fastboot reconnaissance, guarded recovery operations, firmware artifact validation, an RP2040 research-bridge abstraction, pinout data, voice command plumbing, and an experimental research-engine foundation.

The universal autonomous hardware laboratory is **not yet implemented**.

The existing RP2040 serial/USB controllers are still stubs according to the current roadmap. The research engine is groundwork, not a proven autonomous universal hardware investigator. The repository currently targets Windows rather than the desired Android-first phone workflow.

## Engineering priorities

The implementation order is intentionally:

**Phase A: Make continuation reliable.**  
Persistent agent contract, state tracking, evidence rules, anti-loop rules, and clean milestone tracking.

**Phase B: Establish a verified baseline.**  
Build and test the current repository. Fix real regressions before adding major capabilities.

**Phase C: Convert the existing research engine into a real execution framework.**  
Persistent sessions, resumable plans, observation records, hypothesis lifecycle, adapters, experiment results, and explicit risk envelopes must work end-to-end rather than only as models/UI.

**Phase D: Build the hardware transport foundation.**  
USB discovery and descriptors first, then safe serial/HID-style interfaces, followed by adapter-backed UART, SPI, I2C, CAN, SWD/JTAG and logic capture. Hardware access must remain provider/adapter based.

**Phase E: Build the objective-driven operator layer.**  
Natural-language or voice intent becomes a structured objective. The execution engine, not the language parser, owns capability checks, evidence, risk, authorization, and execution.

**Phase F: Android host evolution.**  
Move the practical field workflow toward the S23-class Android host. Preserve the core models and research records so Windows and Android can share the same conceptual workflow.

**Phase G: Autonomous-but-bounded research.**  
Allow automatic progression through approved low-risk experiments, stopping at explicit risk boundaries and recording everything needed for another session to resume.

## Hard gates

Do not advance a phase because a class, button, interface, or placeholder exists.

Advance only after the behavior is demonstrable at the appropriate level.

A phase may be marked complete only when its tests or hardware evidence exist and the repository state reflects that evidence.

## Current next action

Establish the build/test baseline in an environment with .NET/GitHub Actions available, then implement the first concrete execution adapter and USB transport capability. Do not begin a universal hardware rewrite.

## Known constraints

- Physical hardware is not continuously attached to the development environment.
- GitHub access can modify repository state but does not itself provide a physical S23 or electronics bench.
- Hardware-dependent behavior must have mocks/simulators where practical and must distinguish simulated success from hardware validation.
- Build/CI failures must be diagnosed from their actual logs rather than guessed around.

## Session ledger

### 2026-09-18
- Audited the repository's existing continuity, roadmap, architecture, and changelog documentation.
- Confirmed the long-term autonomous hardware-lab direction already exists in the repository.
- Created branch `foundation/autonomous-agent-contract`.
- Added this state file and root-level `AGENTS.md` to make continuation behavior explicit and resistant to scope drift.


## Latest implementation work

- Added `IResearchStepExecutor` and `ResearchExecutionResult`.
- Added `UnsupportedResearchStepExecutor`, which deliberately refuses to report success when no real execution adapter exists.
- Changed `ResearchEngine.ExecuteNextStepAsync` to delegate to a real executor and to prevent `autoConfirmSafe` from authorizing persistent or destructive operations.
- Added tests covering missing executors, risk blocking, and successful execution through a registered test executor.
- CI workflow runs were not exposed for the new commits. A local clone/build could not be performed from the current execution environment because outbound GitHub DNS/network access is unavailable. This is an environment limitation, not a passing-build claim.

# ODT AI / Developer Continuation Guide

## Purpose

This file is the short operational handoff for any developer or AI agent continuing Open Device Toolkit (ODT). The repository, not private conversation history, is the source of truth.

Before changing code, read this file, `ROADMAP.md`, `ARCHITECTURE.md`, `docs/EXPERIMENTAL_RESEARCH_ENGINE.md`, the relevant source files, tests, and `CHANGELOG.md`.

## Ultimate product goal

ODT is intended to become a general-purpose hardware reconnaissance, diagnostic, programming, recovery, reverse-engineering, and repurposing workbench.

The long-term user experience is task-oriented rather than device-list-oriented:

> Connect or describe a device, state the desired outcome, and let ODT determine the best available path.

The device may be common, obsolete, undocumented, damaged, repurposed, or completely unfamiliar. ODT should first use established knowledge and supported procedures. When those do not exist, the Experimental Research Engine should be able to investigate the hardware systematically instead of simply reporting "unsupported."

## Desired autonomous research behavior

For an unknown or poorly documented device, ODT should be able to:

1. Fingerprint the device and everything it can establish about the hardware.
2. Inventory accessible electrical and host interfaces, connectors, buses, processors, memories, firmware/build identifiers, and operating modes.
3. Perform passive observation before active interaction when practical.
4. Generate and rank protocol/interface hypotheses from measurements and captured traffic.
5. Test supported hypotheses methodically and retain both successful and failed experiments.
6. Search existing device knowledge, firmware metadata, protocol signatures, and previous research records before repeating work.
7. Progress from safe observation through controlled experimentation as evidence justifies it.
8. Discover legitimate programming, debugging, recovery, control, or repurposing paths when one exists, including paths that are not publicly documented.
9. Produce a durable research record that another AI or human can resume without starting over.

This is intentionally broader than a fixed list of supported programmers. The architecture should permit new protocols, adapters, providers, algorithms, and research strategies to be added without redesigning the whole application.

## User-directed risk escalation

The operator accepts that experimental work can fail and, when explicitly authorized, may permanently damage or brick a device that was otherwise destined for disposal or recycling.

ODT must still make the boundary unmistakable. Before persistent or destructive experimentation, it should explain what it intends to attempt, the known and unknown risks, the expected outcome, and available recovery options. Require explicit confirmation, preferably with a second confirmation for irreversible actions. Never silently cross the boundary.

After authorization, the research engine may continue through its configured experimental procedures without requiring a new confirmation for every harmless sub-step. It must still stop when a new category of materially greater risk is reached unless that category was covered by the authorization.

## Task execution model

A future task may look like:

> "Figure out how to repurpose this board as a USB serial adapter."

The task engine should convert that request into a structured research/workflow plan. The AI or natural-language layer proposes intent; the workflow engine remains authoritative about capabilities, prerequisites, evidence, risk, and execution.

A long-running research session should survive the application being closed and should record:

- objective
- target identity
- hardware observations
- wiring and instrument configuration
- commands and measurements
- hypotheses considered
- experiments attempted
- results and errors
- artifacts and hashes
- confidence/evidence level
- current state
- next recommended experiments
- authorization state

## Documentation rules for future agents

When functionality is implemented, update the relevant roadmap checkbox and current-status text in the same change. Update `CHANGELOG.md` for user-visible changes. Update architecture or research documentation when an architectural decision or research capability changes.

Do not mark a capability complete merely because an interface or placeholder exists. Mark it complete only when the implemented behavior has been tested to the level appropriate for that milestone.

Keep documentation ordered by lifecycle: goal, architecture, current implementation, next work, longer-term research. Avoid duplicating the same requirement in several places with conflicting wording.

If a requirement changes, update the authoritative document and remove stale wording elsewhere rather than adding another competing note.

## Work continuation rules

At the beginning of a work session:

- Read this guide and the roadmap.
- Inspect the current branch and unmerged PRs before assuming planned work is current.
- Determine what is actually implemented in source and tests.
- Treat unchecked roadmap items as incomplete unless source/tests clearly prove otherwise.
- Prefer completing the smallest demonstrable end-to-end capability over adding invisible infrastructure.

At the end of a work session:

- Update implementation status.
- Record important discoveries and failed approaches.
- Record the next concrete step.
- Add or update tests for behavior that was changed.
- Update the changelog when appropriate.
- Leave the repository understandable without access to the conversation that produced the work.

## Current reality

The repository contains a functional .NET eight Windows application and Android tooling/reconnaissance capabilities, plus substantial groundwork for capability analysis, guarded operations, diagnostic scanning, hardware bridging, and experimental research. The Experimental Research Engine is still a planned/under-development subsystem, not a finished autonomous universal hardware research system.

Never describe planned universal behavior as already implemented. Keep the distinction between current capability, active implementation, and long-term research explicit.

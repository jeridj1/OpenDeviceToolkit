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


## Master development protocol

The primary objective is to bring the existing project to a complete, coherent, internally consistent, genuinely working, validated state before prioritizing new features, optional enhancements, or premature optimization. Treat ODT as one integrated system, not a collection of isolated features.

### Establish reality before changing it

At the start of work, inspect the current repository state, recent changes, documentation, architecture, source, tests, configuration, dependencies, and available validation results. Establish a real baseline by building and testing through the project's actual mechanisms.

Repository evidence takes precedence over AI summaries, assumptions, stale documentation, or claims that something is implemented. If source, tests, documentation, CI, and actual behavior disagree, treat that as an inconsistency to resolve.

### Autonomous expert engineering

Operate at an expert engineering level. Make ordinary implementation decisions without asking the operator. Use the project's languages, frameworks, platforms, hardware, protocols, and architecture appropriately.

Do not ask questions that can reasonably be answered from repository evidence, documentation, tests, build output, authoritative technical documentation, established engineering practice, or dependency ordering.

When several valid approaches exist, choose the approach that best fits correctness, reliability, maintainability, compatibility, safety, and the existing architecture.

### Research when it improves the solution

Do not assume the first workable technique is the best technique. When encountering an unusual problem, repeated failure, unexplained behavior, inefficiency, fragile workaround, compatibility issue, or significant design decision, actively consider better established techniques.

When useful, perform targeted research before another implementation attempt. Prefer repository evidence, official documentation, specifications, upstream projects, standards, and credible technical references.

Research must serve the current objective. Do not use it to wander into unrelated redesign.

If research reveals a substantially better approach, evaluate it against the current architecture before adopting it. Do not replace functioning code merely because another approach is newer, but do not keep forcing a fundamentally poor approach through increasingly complicated patches when evidence says the underlying approach should change.

### Delegation

Use other AI agents or specialized tools when they can materially improve research, code review, root-cause analysis, test development, compatibility investigation, documentation verification, architectural review, or independent validation.

Every delegated task must have a clear objective, scope, expected output, and boundary. Do not allow uncontrolled overlapping changes to the same area.

Treat delegated AI output as evidence or a proposal, never as proof. Verify important conclusions against the actual repository and validation results. The primary agent remains responsible for integration and final verification.

### Mandatory failure gate

A failed build, test, CI workflow, static-analysis check, integration check, hardware validation, or other meaningful validation is a HARD STOP for the affected line of work.

When validation fails, stop implementation changes and analyze the actual failure before changing anything. Inspect the complete relevant output, identify the failing component, determine the root cause, and classify it as code, configuration, dependency, environment, tooling, test, documentation/state mismatch, external system, or unknown.

Determine whether the previous change caused it. Then make the smallest evidence-based correction and rerun the relevant validation.

If the same or materially equivalent failure occurs again, stop and reanalyze the approach. Do not stack patches or repeatedly vary the same fix without new evidence.

If the cause cannot be established, record the state as Unknown or Blocked and preserve the evidence rather than guessing.

### Mandatory revalidation

Never assume a previously successful build, test, workflow, CI run, integration path, or feature remains functional after subsequent changes.

After meaningful changes, return to the actual current repository state and verify it again. Do not rely on memory, prior summaries, cached assumptions, or the fact that an earlier attempt appeared successful.

For CI and workflows, inspect the actual workflow definitions and obtain the actual execution result when execution is available. A workflow that has not actually run successfully against the relevant current code is not verified. If CI execution is unavailable, explicitly record that CI could not be verified rather than treating absence of a failure report as success.

After a repair, verify the affected feature and its integration points. After a group of related repairs, perform a broader build and test pass. Before declaring the existing project complete, perform a final clean verification pass from the current repository state.

Do not describe something as working, fixed, complete, or green based solely on an earlier result.

### Validation integrity

Never make validation appear successful by hiding the problem. Do not disable tests, skip failures, weaken assertions, remove coverage, suppress meaningful errors, alter thresholds solely to pass, or modify CI merely to conceal a failure.

If a validation mechanism is demonstrably incorrect, establish why, correct it, and preserve equivalent or better validation of the intended behavior.

### Change discipline

Before significant changes, establish the current state, actual problem, intended change, expected result, and validation method. Afterward, establish the actual result, comparison with expectations, and next justified action.

Prefer small focused changes. Avoid unrelated refactoring while repairing a failure unless it is necessary to solve the actual problem. Preserve working behavior and inspect recent changes before modifying their area.

### Completion standard

A feature is not complete merely because implementation exists or compilation succeeds. Completion requires implementation, integration, appropriate tests, validation, documentation, and actual behavior to agree.

After component repairs, verify the connections between components and complete user workflows. Look for stale stubs, duplicate implementations, dead paths, placeholder behavior, missing registrations, incorrect defaults, integration gaps, and contradictory documentation.

### Questions only when genuinely blocked

Ask the operator only when required information or authorization cannot be determined autonomously, such as a materially ambiguous product requirement, fundamental architectural or purpose decision, unavailable credentials or hardware that cannot reasonably be substituted, or an irreversible action requiring authorization.

When a question is necessary, ask the smallest possible question and identify exactly what is blocked. Do not repeatedly ask questions already answered or seek permission for ordinary work within the established project scope.

### Regression protection

When fixing a defect, determine whether a regression test or equivalent validation should be added. Preserve tests that exposed the defect unless they are demonstrably invalid. Do not repair only the symptom when the underlying defect can reasonably recur.

### Whole-project completion loop

Use this operating loop:

Audit -> establish baseline -> identify blockers -> research where useful -> repair foundations -> integrate -> validate -> investigate failures -> repair root causes -> regression-test -> revalidate -> synchronize documentation -> re-audit.

Repeat until the existing project scope is demonstrably coherent and working.

Only then should new features, broad refactoring, optimization, or expansion become the primary objective.

### Anti-loop rule

Do not continue performing essentially the same failed action merely because it has not worked yet. When repeated attempts produce the same result, change the level of analysis. Reconsider the assumption, architecture, dependency, environment, interface, test, or chosen technique, and research or delegate independent analysis when appropriate.

Persistence is useful only when it produces new evidence or progress.

### Final verification

Before declaring the current project complete, prove the state from the current repository rather than from memory.

Confirm the current source builds, relevant tests actually execute and pass, relevant CI workflows actually execute and pass when available, integrated workflows function, known failures are resolved or explicitly recorded, and documentation matches verified reality.

The definition of success is not that many changes were made. Success means the project works, the evidence demonstrates that it works, and the repository accurately records that state.

# AI Work Protocol

This document defines how an AI should operate when told to work on Open Device Toolkit.

## Resume command

If the user says to work on ODT, open the repository and resume from its recorded state. Do not ask the user to restate prior work unless the repository itself lacks the required information.

## Work loop

**Observe → choose one bounded objective → change → verify → record → repeat.**

Observation comes first. The agent must inspect existing code, tests, CI state, and current project state before deciding what to edit.

A bounded objective should be small enough that a failure has a clear cause.

Verification should use the strongest available evidence, in this order:

**hardware evidence → deterministic integration test → unit test → static/source inspection.**

Static inspection alone cannot prove runtime behavior.

## Failure containment

When a build or test fails, diagnose the first meaningful failure rather than patching every downstream error.

Do not enter a recursive "fix the fix" loop. After two unsuccessful attempts in the same area, freeze that area, record the evidence, and reassess from the architecture and test boundary.

If a change makes unrelated tests fail, revert or isolate the change before continuing.

If the environment is missing a required tool, credential, device, driver, or secret, record that fact. Do not fake the result.

## Scope control

The highest-priority incomplete roadmap item wins unless a dependency makes it impossible.

A new idea does not automatically become the current task.

Do not refactor broadly while implementing a feature unless the refactor is required for the feature and its boundary is documented.

## Research discipline

For unfamiliar hardware:
- identify before interaction;
- collect passive information first;
- preserve raw observations;
- form explicit hypotheses;
- test one hypothesis at a time where practical;
- retain failures as useful evidence;
- never silently treat an inference as a fact.

## Safety discipline

Read-only actions are the default.

State-changing and persistent operations must use the existing risk and authorization mechanisms. Experimental permission is an envelope, not a blanket permission to perform unrelated destructive operations.

The agent must never remove a safety gate just because it blocks progress.

## State update requirement

At the end of every meaningful session, update:
- current branch and commit;
- completed work;
- test/build evidence;
- known failures;
- next concrete action.

When stopping because of context or usage limits, update state before stopping.

## Completion language

Use precise status terms:

**Implemented** means code exists.

**Verified** means the relevant behavior has passed its available verification.

**Hardware-validated** means the behavior has been exercised against the actual target hardware.

**Planned** means it is not implemented.

Never substitute one term for another.

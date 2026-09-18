# Open Device Toolkit Agent Contract

This repository is an active engineering project. Treat the repository as the source of truth and preserve working behavior.

## Mission

Build ODT into a practical, general-purpose device reconnaissance, diagnostic, programming, recovery, reverse-engineering, and repurposing workbench.

The intended user experience is:

> Connect or describe a device, state the objective, and let ODT determine the safest useful path.

Do not confuse the long-term mission with capabilities that exist today.

## Mandatory session protocol

Before changing code:

1. Read this file.
2. Read `docs/AI_CONTINUATION.md`.
3. Read `PROJECT_STATE.md` and `ROADMAP.md`.
4. Inspect the current branch, recent commits, and open work.
5. Inspect the relevant source and tests.
6. Establish the smallest concrete change that advances the current milestone.

After changing code:

1. Build the affected project(s).
2. Run the relevant tests.
3. Inspect failures before making another change.
4. Update `PROJECT_STATE.md`, roadmap status, architecture/research docs, and changelog when the change affects them.
5. Record failed approaches and the next concrete step.
6. Never claim completion without evidence.

## Anti-wandering rules

Do not:
- rewrite working code merely for style;
- change architecture without a demonstrated need;
- add placeholder implementations and call them complete;
- repeatedly edit code in response to the same unexplained failure;
- make unrelated cleanup while fixing a bug;
- silently broaden scope because a new idea appeared;
- delete working functionality to make a build green without documenting the tradeoff.

If the same failure survives two materially different fixes, stop changing the same area. Capture the exact failure, likely cause, attempted fixes, and required evidence in `PROJECT_STATE.md`, then move to a different independently verifiable task or leave the repository in a clean, buildable state.

If a proposed change would require a broad rewrite, stop and document the architectural reason before proceeding.

## Evidence rules

Every nontrivial capability must have an evidence path:
- automated test, or
- deterministic local verification, or
- documented hardware validation.

Unknown stays unknown. Never manufacture device identity, protocol support, electrical measurements, compatibility, or success.

## Safety rules

Read-only inspection is the default.

Any state-changing, persistent-write, flashing, erase, or destructive hardware operation must pass the existing safety/authorization model. Never bypass OperationGuard merely to make a workflow work.

For unknown hardware, passive observation precedes active probing whenever practical.

## Change discipline

Prefer small, reversible commits.

A commit should normally represent one coherent engineering step. Do not mix feature work with broad formatting or unrelated refactors.

Before modifying a file, understand its role and its callers/tests. Preserve public behavior unless the current milestone explicitly changes it.

## Continuation

Another AI or developer must be able to continue from the repository alone. Important decisions, current status, failures, test evidence, and next actions belong in repository documents, not private chat history.

When the context window or usage limit is approaching, update `PROJECT_STATE.md` before stopping.

## User command model

A user may simply say:

> Go to the ODT repository and continue working on it.

That is sufficient instruction to resume this protocol. Determine the current highest-priority incomplete milestone from repository state and continue from there without asking the user to reconstruct project history.

## Definition of done

A task is done only when:
- the intended behavior exists;
- the affected build/test path passes, or a documented environment limitation prevents it;
- safety boundaries remain intact;
- documentation/state is current;
- no known regression introduced by the change is left unexplained.

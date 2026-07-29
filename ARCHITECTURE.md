# Open Device Toolkit Architecture

## Design principles

1. **Inspect before modifying.** Reconnaissance and evidence collection are the default.
2. **Explain decisions.** Device state should be derived from visible commands, APIs, or documented probes and shown to the user.
3. **Fail safely.** Unknown conditions should produce an explicit unknown state rather than a guess.
4. **Separate device knowledge from UI.** Device support belongs in providers/plugins, not in the presentation layer.
5. **Keep recovery in mind.** Operations that can alter persistent state must have prerequisites, warnings, and verification.
6. **Make work reproducible.** Reports, logs, versions, hashes, and snapshots should make it possible to understand what happened later.

## Initial solution shape

```text
OpenDeviceToolkit/
├── src/
│   ├── OpenDeviceToolkit.App/       # Windows UI and application composition
│   ├── OpenDeviceToolkit.Core/      # Device abstractions, logging, configuration
│   ├── OpenDeviceToolkit.Android/   # ADB and Android-specific inspection
│   └── OpenDeviceToolkit.Qualcomm/  # Future Qualcomm/EDL research support
├── plugins/                         # Future external providers
├── tests/                           # Automated tests
├── docs/                            # User/developer documentation
├── scripts/                         # Build/development helpers
└── tools/                           # Optional local tool integrations
```

## Runtime layers

### App/UI
Responsible for presentation, user interaction, navigation, and confirmation dialogs. It should not contain device-specific probing logic.

### Core
Provides interfaces and shared services such as logging, command execution, device models, configuration, workspace paths, hashing, and report models.

### Providers
A provider knows how to communicate with a device family. Android is the first provider. LG and Qualcomm capabilities will build on it without coupling the UI to those technologies.

### External tools
ADB, Fastboot, vendor utilities, and diagnostic tools are treated as external dependencies. The toolkit should detect versions and paths rather than silently assuming they exist.

## Proposed device abstraction

A device provider should be able to answer questions such as:

- What device is connected?
- How was it detected?
- What capabilities are available?
- What information can be read safely?
- Which operations are supported?
- What prerequisites are required?

A capability should expose evidence and status, for example `Available`, `Unavailable`, `Unknown`, or `RequiresPrivilege`.

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

Every significant development step should leave enough information for another developer or AI to continue:

- Update `ROADMAP.md` when milestone status changes.
- Record architectural decisions in `docs/` or an ADR when appropriate.
- Keep commits focused and descriptive.
- Update `CHANGELOG.md` for user-visible changes.
- Never rely on private conversation context as the only source of project requirements.
- Treat the repository documentation and source code as the authoritative project state.

## Build direction

The planned desktop implementation is C#/.NET 8 with a Windows desktop UI and single-file publishing for distribution. The exact UI framework can be changed before implementation if testing shows a better fit.

## Security boundary

ODT may eventually interact with firmware, bootloaders, recovery environments, and low-level hardware. The architecture therefore separates read-only analysis from state-changing operations. Persistent writes should be implemented as explicit capabilities rather than generic arbitrary command execution exposed through normal UI workflows.
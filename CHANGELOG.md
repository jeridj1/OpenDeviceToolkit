# Changelog

All notable changes to Open Device Toolkit are documented here.

## Unreleased

### Added
- Initial project roadmap and architecture documentation.
- AI/developer handoff documentation.
- Safety boundary for read-only inspection versus persistent writes.
- Windows-aware ADB/Fastboot tool discovery from the ODT Tools workspace and PATH.
- Environment Check UI with OS, architecture, .NET, workspace, ADB, and Fastboot diagnostics.
- Structured daily application log files under the workspace Logs directory.
- Automated tests for ADB property parsing and tool discovery.
- Windows CI test execution in addition to application builds.
- One-click Deep Read-Only Scan for Android boot, USB, partition, and filesystem diagnostics.
- Dedicated diagnostic reports containing raw probe evidence and execution timing.

### Improved
- Device scans now resolve the current ADB executable instead of assuming `adb` is only on PATH.
- Diagnostic output includes tool paths and command evidence where available.
- The alpha remains read-only with no flashing, unlocking, erasing, or other persistent device writes.

### Next
- USB and Windows driver inventory.
- Qualcomm/LG transport detection.
- Structured partition and A/B slot analysis.
- Evidence-backed diagnostic findings and device knowledge cards.

## Versioning

Until the first stable release, version numbers may use `0.x.y` to indicate alpha development. Milestone names in `ROADMAP.md` describe capability maturity rather than guarantees of release dates.
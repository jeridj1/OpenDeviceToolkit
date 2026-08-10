# Changelog

All notable changes to Open Device Toolkit are documented here.

## Unreleased

### Added
- Initial project roadmap and architecture documentation.
- AI/developer handoff documentation.
- Dedicated AI/developer continuation guide defining repository authority, work-session rules, and status tracking.
- Safety boundary for read-only inspection versus persistent writes.
- Explicit operator authorization model for high-risk experimental research.
- Windows-aware ADB/Fastboot tool discovery from the ODT Tools workspace and PATH.
- Environment Check UI with OS, architecture, .NET, workspace, ADB, and Fastboot diagnostics.
- Structured daily application log files under the workspace Logs directory.
- Automated tests for ADB property parsing and tool discovery.
- Windows CI test execution in addition to application builds.
- One-click Deep Read-Only Scan for Android boot, USB, partition, and filesystem diagnostics.
- Dedicated diagnostic reports containing raw probe evidence and execution timing.
- Explicit Experimental Research Engine direction for autonomous hardware reconnaissance, protocol hypothesis generation, long-running research sessions, and device repurposing.

### Improved
- Device scans now resolve the current ADB executable instead of assuming `adb` is only on PATH.
- Diagnostic output includes tool paths and command evidence where available.
- Project documentation now distinguishes implemented capabilities from planned research capabilities.
- Repository documentation now defines a consistent continuation process so future developers and AI agents can recover project intent without private conversation history.

### Next
- USB and Windows driver inventory.
- Qualcomm/LG transport detection.
- Structured partition and A/B slot analysis.
- Evidence-backed diagnostic findings and device knowledge cards.
- First real, resumable Experimental Research Engine workflow.
- RP2040 multifunction bridge integration for hardware observation and controlled probing.

## Versioning

Until the first stable release, version numbers may use `0.x.y` to indicate alpha development. Milestone names in `ROADMAP.md` describe capability maturity rather than guarantees of release dates.

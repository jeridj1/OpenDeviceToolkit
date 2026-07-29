# Open Device Toolkit Handoff Guide

This file exists so another developer or AI can continue the project without needing access to the original conversation.

## What this project is

Open Device Toolkit (ODT) is intended to become a Windows-based device diagnostics, firmware, recovery, and electronics workbench. The first concrete target is Android device reconnaissance, with an LG V50 ThinQ LM-V450VM serving as the initial test device.

## Current test device

Known from user-provided ADB output:

- Model: LM-V450
- Carrier variant: LM-V450VM / Verizon
- Android: 12
- Software: V450VM40a
- Kernel: 4.14.190-perf+
- Platform: msmnile (Qualcomm Snapdragon 855 family)
- Hardware: flashlm
- Security patch: 2022-05-01
- A/B slots: yes; active slot previously observed as `_a`
- `ro.boot.flash.locked`: `1`
- `ro.boot.verifiedbootstate`: `green`
- `ro.boot.vbmeta.device_state`: `locked`
- `sys.oem_unlock_allowed`: `0`
- `adb reboot edl`: rebooted normally rather than exposing Qualcomm 9008
- `adb reboot bootloader`: rebooted normally
- `/dev/block/by-name` is readable without root and showed A/B boot-chain partitions including `abl_a/b`, `laf_a/b`, `xbl_a/b`, `boot_a/b`, `vbmeta_a/b`, `system_a/b`, and `vendor_a/b`

A complete `getprop` dump was collected by the user as `phone_properties.txt` and stored locally under the user's planned `D:\OpenDeviceToolkit\Reports\` workspace. Do not assume that file is present in the GitHub repository unless the user explicitly commits it.

## Current implementation state

The repository now contains the first functional .NET 8 Windows Forms application and provider layers:

- `OpenDeviceToolkit.Core`: command execution and workspace services.
- `OpenDeviceToolkit.Android`: ADB discovery, `adb devices` parsing, `getprop` parsing, typed Android device state, and report generation.
- `OpenDeviceToolkit.App`: light-theme Windows UI that scans automatically at startup and can manually rescan, generate a report, and open the workspace.
- `.github/workflows/build.yml`: Windows CI restore/build workflow.
- `scripts/Build-Release.ps1`: self-contained win-x64 single-file publish helper.

The application is intentionally read-only. It does not flash, unlock, erase, or write to the connected phone.

## Immediate implementation target

Continue the first milestone in small, testable increments.

### Alpha 0.1 remaining work

1. Add robust ADB path discovery, including the ODT local Tools directory.
2. Add structured application logging.
3. Add basic Windows/tool environment diagnostics.
4. Add automated parser/state tests.
5. Confirm the Windows CI build succeeds.
6. Improve report output and capture raw command evidence.
7. Add explicit device capability/state reporting rather than guesses.

### Next milestone

After 0.1 is stable, begin driver/tool inventory and Android/LG research features. Keep persistent writes out until the read-only diagnostic layer is mature.

## Important constraints

- Do not silently modify the phone.
- Do not flash partitions in the first milestone.
- Do not assume a bootloader unlock path exists.
- Do not invent firmware compatibility.
- Prefer evidence and an explicit `Unknown` state over guesses.
- Any future persistent-write feature must show exactly what it will change and require deliberate confirmation.

## Long-term ideas

The user has proposed a broader hardware workbench, including an RP2040-based multifunction bridge capable of acting as different interfaces such as serial/UART, CMSIS-DAP/SWD, SPI, I2C, and potentially JTAG with suitable hardware. This is a future research direction, not part of Alpha 0.1.

## Continuation procedure

Before making changes:

1. Read `README.md`, `ROADMAP.md`, `ARCHITECTURE.md`, this file, and `CHANGELOG.md` if present.
2. Inspect the actual source tree and recent commits.
3. Determine implementation status from source, not this document alone.
4. Update documentation as implementation changes.
5. Keep commits small enough that a later contributor can understand them.
6. Run or inspect CI where possible before claiming a build is verified.

When a milestone is completed, update the roadmap and changelog and record any deviations from the planned architecture.

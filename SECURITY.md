# Security Policy

Open Device Toolkit (ODT) is a workbench for identifying, inspecting, diagnosing,
backing up, and-progressively-modifying electronic devices. Several ODT operations
can alter persistent device state (firmware, boot partitions, calibration or security
state). Security and safety are therefore first-class concerns.

## Reporting a Vulnerability

Please report security vulnerabilities **privately** - do not open a public issue.

- Open a private security advisory on this repository (Security tab ->
  "Report a vulnerability"), or contact the maintainer directly if you have no other
  channel.

Include:
- A clear description of the issue and its impact.
- Affected version/commit and how to reproduce.
- Any proof-of-concept, sanitized of sensitive data.

You should receive an acknowledgement within a few days. Please allow reasonable
time for a fix before any public disclosure. We credit responsible reporters.

## Scope

In scope:
- Anything that lets ODT bypass its explicit-confirmation guardrails for
  write/flash/erase/reboot operations.
- Anything that mishandles firmware artifacts (missing checksum/hash validation,
  path traversal in firmware/backup paths, unvalidated external firmware sources).
- Command injection or argument escaping issues in tool invocation (adb/fastboot/serial).
- Issues in the USB/serial enumeration or RP2040 bridge protocol handling.

Out of scope:
- The behavior of third-party devices, firmware, or vendor tools themselves.
- Social-engineering or physical-attack scenarios.

## Safety Principles Enforced in Code

1. Read-only first. Inspection and evidence collection never modify device state.
2. Target identification required. No operation runs without an identified target
   (serial/interface). Ambiguous targets are rejected.
3. Explicit confirmation. Reversible and destructive operations require deliberate
   confirmation before execution.
4. Evidence over assumption. Capabilities are derived from observed evidence, not
   assumptions about a device.
5. No automatic destructive action. Flash/erase/wipe never run unattended.
6. Backup before modify. Recoverable state is backed up before high-impact writes
   where the interface permits.

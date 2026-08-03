# Device Capability Schema

ODT uses a conservative capability model so device identification and modification planning are separate concerns.

A capability finding contains a name, status, evidence, and explanation. Status is one of `Available`, `Unavailable`, `Unknown`, or `RequiresPrivilege`.

`Available` means the current evidence demonstrates that an interface or fact is usable. `Unavailable` means the current device state prevents it. `Unknown` means ODT has insufficient evidence and must not infer support. `RequiresPrivilege` means the capability may exist but requires a deliberate privilege transition or other prerequisite.

The analyzer should never turn a model name alone into a claim that a rooting method, exploit, bootloader unlock, or flashing path exists. Device-specific actions should be selected only after matching the exact model, carrier/variant, firmware/build, boot state, and applicable prerequisites.

## Planned capability families

Android transport, USB debugging, bootloader state, Verified Boot, A/B slots, recovery/boot modes, partition layout, backup capability, firmware identification, OEM unlock state, Qualcomm diagnostic transport, LG Download Mode, and supported hardware bridges.

## Modification boundary

ODT may eventually expose explicit user-authorized modification operations such as backup, restore, recovery installation, or firmware flashing where a documented and technically supported procedure exists. Each operation should show its target, source artifact, prerequisites, expected effect, and recovery path before execution.

Exploit research can be represented as evidence and candidate knowledge, but an unknown vulnerability must never be treated as an automatic rooting recipe.

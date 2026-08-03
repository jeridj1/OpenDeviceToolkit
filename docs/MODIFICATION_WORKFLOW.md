# Explicit Modification Workflow

ODT should treat device modification as a separate workflow from reconnaissance.

The intended sequence is identify the exact device, collect evidence, determine supported interfaces, select a known procedure, verify prerequisites, create or verify a recovery artifact when possible, show the proposed operation, and require explicit confirmation immediately before a persistent write.

The operation record should include the device serial or equivalent identifier, manufacturer/model, firmware/build identifiers, boot and security state, source artifact and checksum, selected operation, expected changes, and a rollback or recovery path when one exists.

Read-only reconnaissance can run automatically. Persistent changes must never be inferred merely from a device being connected or from a capability being detected.

This architecture is intended to support legitimate repair, recovery, experimentation, and owner-authorized modification of devices while keeping destructive actions deliberate and auditable.

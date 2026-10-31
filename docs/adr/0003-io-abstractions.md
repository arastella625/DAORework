# ADR-0003: Abstract file system, clock and processes

- **Status:** Accepted (P0-01, 2026-09-29)
- **Date:** 2026-09-28

## Context
The Application and Core layers call `File`, `Directory`, `FileInfo`,
`DateTimeOffset.UtcNow` and `Environment` directly, so the logic cannot be tested
without a real machine state (audit A2, A3).

## Decision
- File system: **System.IO.Abstractions** (`IFileSystem`). Tests use `MockFileSystem` from `TestableIO.System.IO.Abstractions.TestingHelpers`.
- Clock: the BCL **`TimeProvider`**. Tests use `FakeTimeProvider`.
- Processes, registry and known folders: small project-owned ports (`IProcessLauncher`, `IRegistryReader`, `IKnownFolders`), implemented in Infrastructure.

## Consequences
Use cases become unit-testable. There is one more package, which is mature, widely
used and MIT-licensed.

## Alternatives considered
- *A hand-written `IFileSystem`:* it grows into a worse copy of the library.
- *Integration tests only:* slow, flaky, and they cannot cover error paths such as access denied.

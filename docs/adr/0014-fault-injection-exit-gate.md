# ADR-0014: The Phase 2 exit gate kills the real CLI before every file-system write

- **Status:** Accepted
- **Date:** 2026-10-01

## Context
The Phase 2 exit gate (ROADMAP) asks for "a fault-injection test suite [that] kills the process at every step of
apply and proves it recovers to either the before-state or the after-state, never a mix". The P2-05 tests
(`ApplyRecoveryTests`) already show the journal's promise, but only partly:
- they stop the apply with an exception at the six writer steps, so `finally` blocks still run and buffered
  writes are still flushed;
- they never stop inside the backup, the journal's first write, a step being logged, the record being saved or
  the journal being deleted;
- they run on `MockFileSystem`, not a real disk.

A real kill does none of that clean-up. The gate has to show the promise holds anyway.

## Decision
- A new test project, `DAORework.FaultInjection.Tests`, is also its own host program. Run as
  `dotnet DAORework.FaultInjection.Tests.dll host <killAt> <appData> <cli arguments…>`, it builds the CLI exactly as
  `Program.cs` does, except that `IFileSystem` is wrapped by `KillingFileSystem`. It turns off the test SDK's
  generated entry point so it can do this.
- `KillingFileSystem` counts every call that can change the disk: each write, append, move, copy, delete and create
  on `IFile` and `IDirectory`, every stream opened for writing, and every `Write` on such a stream. Immediately
  before call number `killAt` it kills its own process with `Process.Kill`. No `finally` block or flush runs. The
  wrapping uses `System.Reflection.DispatchProxy`, so a call added later is counted without a change here.
- Each scenario is a real CLI command against real folders in a temporary directory: a documents folder, an
  install folder and DAORework's app-data. The tests:
  1. run the command once without a kill, to learn its write count K and its after-state;
  2. for every n from 0 to K-1, copy the starting state, run the command with `killAt = n` and check it was killed;
  3. when a journal is left behind, settle it twice, from two copies: `recover --finish` must give exactly the
     after-state, and `recover --rollback` exactly the before-state. When no journal is left, the folders must
     already be exactly the before-state or the after-state;
  4. in every case, check that a later `status` reads the record and a `rollback` undoes the last change that
     really happened.
- "State" means the SHA-256 of every file under the documents and install folders, plus the record's active list.
  Temporary files DAORework leaves must be gone once the state is settled.
- The scenarios cover activating mods (copies, an `AddIns.xml` edit), deactivating them (deletes), switching to a
  mod that replaces another's file, applying and removing the 4 GB patch on a made-up executable, and applying,
  upgrading and removing DXVK from made-up releases that carry its version marker. Kill points run in parallel, so
  the suite stays within a few minutes of CI. It runs on every build like the other test projects.

## Consequences
- Every new write in the mod or fix pipeline is a kill point automatically; the gate cannot go stale.
- A kill models the process dying, not the machine losing power. Data the OS has accepted survives. Flushing to
  the physical disk (`fsync`) is out of scope, as it is for every other program the game ships with.
- The suite can only prove what its scenarios exercise, so each new kind of change (a new fix, mod profiles)
  adds a scenario.

## Alternatives considered
- *Kill from the outside after a delay:* timing-dependent, and it cannot promise to hit every step.
- *`Environment.FailFast`:* it writes a crash report, and on Linux it can produce a core dump, for every one of
  hundreds of runs.
- *More exceptions in the existing in-process tests:* `finally` blocks and flushes still run, which is exactly
  what the gate must not rely on.

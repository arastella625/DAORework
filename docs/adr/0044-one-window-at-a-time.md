# ADR-0044: One window at a time

- **Status:** Proposed
- **Date:** 2026-11-06

## Context
Testing on the owner's library opened a second copy of the window by accident, and nothing stopped it. Both copies read the
same Library and could both apply a change to the same game folder. Plans are made from what was on disk when they were
worked out, so a plan from one window could undo what the other just did. A player double-clicking the app while it is slow
to appear can do this without knowing.

## Decision
1. **A window takes a lock for its data folder before it reads anything.** It is a named mutex, named from a hash of the data
   folder, behind `IWindowLock`.
2. **A second copy doesn't open.** It shows *DAORework is already open* and closes.
3. **Only the window takes it.** `dao` commands aren't stopped: they are run on purpose, one at a time, and `dao` and the
   window already meet at the apply journal.

## Consequences
- The system lets a mutex go when the process ends, so a crash can't leave the app refusing to open.
- A copy built to use another data folder, as read-only checks do, still opens beside the player's.
- A copy run as administrator holds a name an ordinary copy can't open. That reads as already open, which is true.
- The second copy doesn't bring the first to the front. That needs a way to talk between copies, and can come later.
- `dao` running beside the window is still possible.
- The name is per Windows session, so one person signed in twice on one PC can still open two.
- On Linux a named mutex lives in a shared folder that can belong to someone else, and any refusal reads as already open.
  Phase 2.7 must check this before the window ships there.

## Alternatives considered
- A lock file: it can be left behind, or fail for reasons that have nothing to do with another copy.
- Locking only while a change applies: two windows would still plan from what the other is about to change.
- Bringing the first window forward: it needs Windows calls and a channel between copies, for one less click.

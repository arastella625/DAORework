# ADR-0004: Result<T> for expected failures, exceptions for bugs

- **Status:** Accepted (P0-01, 2026-09-29)
- **Date:** 2026-09-28

## Context
Today, failures show up in three different ways: `null` returns,
`DaoLaunchResult.Failed`, and swallowed exceptions that return sentinel values
(audit C3). Callers cannot tell from a signature whether something can fail.

## Decision
- Add `Result` / `Result<T>` and `Error(Code, Message, Recommendation?)` in `Core/Common`. They are small and project-owned, with no library.
- Use cases return `Task<Result<T>>` for outcomes a user can cause or fix: not found, invalid input, missing executable.
- Exceptions are for programmer errors and unexpected I/O. They are caught only at the presentation boundary and turned into exit code 70.
- Empty catches and sentinel return values are forbidden (Standards §4).

## Consequences
Signatures state honestly what can fail, and the CLI maps failures to exit codes
consistently. The cost is a little ceremony at call sites.

## Alternatives considered
- *Exceptions everywhere:* costly on hot paths, and they hide expected outcomes.
- *A third-party library (FluentResults, ErrorOr):* more surface than we need, and one more dependency to track.

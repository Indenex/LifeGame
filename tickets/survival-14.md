# survival-14: Thirst eases in the first hour (survival-10 option C)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: done
Branch: claude/survival-thirst-ease
PR: merged, first commit 4ff363bb

## Goal
Justin picked option C from survival-10: on the env-2 land the walk to water from the middle spawn is median 38 m / worst 50 m, so a new character's thirst falls 20% slower during their first hour online. Vitals gets ThirstEaseLeft/ThirstEaseRate (default 0, migration-safe, mirrors the arrival buff); Needs.ThirstEaseSeconds = 3600, ThirstEaseFactor = 0.8. Not after a death.

## Progress log
- 2026-10-16: implemented; module builds, publishes over an existing local world without --delete-data (upgrade path OK).

## Open questions / blockers
- (none)

## Next step
Done and merged; nothing further on this ticket.

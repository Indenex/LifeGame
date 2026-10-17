# skills-22: JournalCheck
Lane: Journal & skills
Owner: Journal & skills thread
Status: done
Branch: claude/journal-skills-22
PR: #687 (merged)

## Goal
`tools/JournalCheck` fails if any `Skills.AllProcessIds()` id has no skill, or cannot be reached through the Things-to-try hints. Run: `dotnet run --project tools/JournalCheck`.

## Progress log
- 2026-10-17: written; on main it fails only on carve.spade, with #683 merged it prints OK (76 of 76).

## Open questions / blockers
- Integration owns build.yml: ask them to run it in CI after merge.

## Next step
None. Add a Things-to-try line whenever a lane announces a new process; run `dotnet run --project tools/JournalCheck` before pushing.

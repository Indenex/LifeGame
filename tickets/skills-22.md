# skills-22: JournalCheck
Lane: Journal & skills
Owner: Journal & skills thread
Status: in_review
Branch: claude/journal-skills-22
PR: (opening)

## Goal
`tools/JournalCheck` fails if any `Skills.AllProcessIds()` id has no skill, or cannot be reached through the Things-to-try hints. Run: `dotnet run --project tools/JournalCheck`.

## Progress log
- 2026-10-17: written; on main it fails only on carve.spade, with #683 merged it prints OK (76 of 76).

## Open questions / blockers
- Integration owns build.yml: ask them to run it in CI after merge.

## Next step
After #683 merges, merge main, open the PR, send the PM the green sha, and ask Integration to wire it into CI.

# skills-25: ProcessDef names its own skill
Lane: Journal & skills
Owner: Journal & skills thread
Status: in_review
Branch: claude/journal-skills-25
PR: (opening)

## Goal
`ProcessDef.Skill` (optional Specialization id) wins over the guess in `Skills.SpecFor`; null keeps today's inference, so no behaviour changes. JournalCheck fails if a Def names an unknown skill. Agreed with Crafting (session_01N9APthjRDfjknUvCweqzzN): small hunk in the ProcessDef class only, their crafting-devices #285 adds defs in the same file.

## Progress log
- 2026-10-17: field, SpecFor hook, JournalCheck guard, CONTENT.md row written; JournalCheck and ProgressionCheck pass.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the PM the green sha.

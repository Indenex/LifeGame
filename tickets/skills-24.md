# skills-24: Skill tooltips say what a skill improves
Lane: Journal & skills
Owner: Journal & skills thread
Status: in_review
Branch: claude/journal-skills-23
PR: (opening)

## Goal
Hovering a skill bar in the journal shows its blurb plus one line per effect: the value at the current rank and at Master. The text comes from `Progression.Effects` through `Progression.Describe`, so it cannot drift from the numbers.

## Progress log
- 2026-10-17: Progression.EffectsFor/Describe added; JournalPanel tooltip uses it; ProgressionCheck asserts every skill lists effects and every effect is listed.

## Open questions / blockers
- Proposal B (ProcessDef carries its own skill) sent to Crafting (session_01N9APthjRDfjknUvCweqzzN); waiting on 1, 2 or 3.

## Next step
Wait for CI on the PR, send the PM the green sha.

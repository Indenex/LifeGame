# skills-5: Crafting rows show what a Master does by heart
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg
PR: (opening 2026-10-15 23:05)

## Goal
The crafting panel tells a Master that a hands-on row is done "by heart" and that holding Ctrl (`craft_play_minigame`, UX/UI's Controls.cs) works it by hand (D-047).

## Progress log
- 2026-10-15 23:04: crafting row cue done (`HandsOn(def)` in CraftingUi.Refresh via a `Mastered` partial hook); committed locally, pushing after #24 merges. Crafting told about the one-line Refresh change (their #20 also touches Refresh).

## Open questions / blockers
- (none)

## Follow-up (next ticket)
- Dug clay in one place: skill-based on normal levels, 80 on the stick's extra level, folding `GiveDugClay` (Skills.cs), Ceramics' `ClayRules.DugClayQuality` and Crafting's stick depth together once #20/#21 are on main.

## Next step
Wait for CI on the skills-5 PR and send the green sha to the PM.

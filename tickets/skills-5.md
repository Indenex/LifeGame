# skills-5: Mastery cues and dug clay in one place
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg
PR: (after #24 merges)

## Goal
The crafting panel tells a Master that a hands-on row is done "by heart" and that holding Ctrl (`craft_play_minigame`, UX/UI's Controls.cs) works it by hand (D-047). Dug clay quality lives in one place: skill-based on normal levels, 80 on the stick's extra level, folding together `GiveDugClay` (Skills.cs), Ceramics' `ClayRules.DugClayQuality` and Crafting's stick depth once Ceramics' and Crafting's PRs land.

## Progress log
- 2026-10-15 23:04: crafting row cue done (`HandsOn(def)` in CraftingUi.Refresh via a `Mastered` partial hook); committed locally, pushing after #24 merges. Crafting told about the one-line Refresh change (their #20 also touches Refresh).

## Open questions / blockers
- Dug-clay fold-in waits on Ceramics' and Crafting's PRs (#20/#21) reaching main.

## Next step
After #24 merges: push, open the skills-5 PR; when #20/#21 are on main, do the dug-clay fold-in.

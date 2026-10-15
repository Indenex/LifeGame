# skills-6: Dug clay quality in one place
Lane: Journal and skills
Owner: Journal and skills thread
Status: blocked
Branch: claude/journal-skills-iseewg
PR: (none yet)

## Goal
One function decides the quality of dug clay: Earthwork skill sets it on normal dig levels (`Skills.DugClayQuality`, 50 up to 85, steps of 5, plus the bonus-lump chance), and the digging stick's extra level gives 80 (D-041). Today the numbers are split between `GiveDugClay` (server/spacetimedb/Skills.cs), the literal 80 in `Reducers.Dig`, and Ceramics' `ClayRules.DugClayQuality` (coming in #21). Done when `Reducers.Dig` makes one call and Ceramics' rule and the skill rule agree.

## Progress log
- 2026-10-15 23:12: ticket written. #20 (stick depth) is on main; waiting on Ceramics' #21 for `ClayRules.DugClayQuality`.

## Open questions / blockers
- Waiting on Ceramics' #21 to reach main (PM 2026-10-15 23:03).

## Next step
When #21 is on main: merge main, read `ClayRules.DugClayQuality`, fold the 80 and the skill rule into one call from `Reducers.Dig`, run tools/ClayCheck locally, open the PR.

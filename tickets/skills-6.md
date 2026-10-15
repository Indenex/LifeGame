# skills-6: Dug clay quality in one place
Lane: Journal and skills
Owner: Journal and skills thread
Status: done
Branch: claude/journal-skills-iseewg
PR: none (done by Ceramics in #21, 85a1572)

## Goal
One function decides the quality of dug clay: Earthwork skill sets it on normal dig levels (`Skills.DugClayQuality`, 50 up to 85, steps of 5, plus the bonus-lump chance), and the digging stick's extra level gives 80 (D-041). Today the numbers are split between `GiveDugClay` (server/spacetimedb/Skills.cs), the literal 80 in `Reducers.Dig`, and Ceramics' `ClayRules.DugClayQuality` (coming in #21). Done when `Reducers.Dig` makes one call and Ceramics' rule and the skill rule agree.

## Progress log
- 2026-10-15 23:12: ticket written. #20 (stick depth) is on main; waiting on Ceramics' #21 for `ClayRules.DugClayQuality`.

- 2026-10-15 23:33: Ceramics' #21 (85a1572) folded it: `GiveDugClay(ctx, player, depth)` takes Max(ClayRules.DugClayQuality(depth) [80 on the stick level], Skills.DugClayQuality(rank)), one call from Reducers.Dig. Checked on main; nothing left for this lane.

## Open questions / blockers
- (none)

## Next step
Done.

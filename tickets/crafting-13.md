# crafting-13: Crafting numbers against D-075 (start low, grow slowly)
Lane: Crafting
Owner: Crafting thread
Status: blocked
Branch: crafting-numbers
PR: (none yet)

## Goal
D-075: every number starts low and grows slowly, with faster gains through about the first 15% of progression, then a slow climb. Review Crafting's numbers, send the PM options, and apply whatever Justin picks.

## Where things stand (main 7c65c0f)
- Base process times are 3 to 15 s (basket 15 s, coil pot 12 s, stone axe 10 s). Skill cuts them to 40% at rank 100, linearly (Journal: `Skills.DurationScale`).
- The quality cap is 35 at rank 0 and rises linearly to 100 (`Skills.QualityCap`). Rank gain is already front-loaded (`Skills.Gain`, quadratic falloff).
- A tool's quality has no effect on anything.
- Fires: a new fire burns 15 min (D-054), a stick adds 90 s, a log 360 s (#70). Trees give 3 logs (D-060) and regrow in 2 h.
- Yields: grit x2, grog x2, tempered clay x2 (grit) or x3 (grog), daub x2.

## Proposal sent to the PM (2026-10-16 17:45)
1. Shared curve `Progress.Curve(p)`, p 0 to 1: 40% of the gain comes in the first 15% of p, then a straight climb. Journal swaps it into QualityCap, DurationScale and MinigameSteps.
2. Base times: double tier 2 and up; tiers 0 and 1 stay as they are.
3. Tool quality: a better tool works faster, from 0% (crude) to 25% (excellent), on the same curve.
4. Fire and yield numbers stay: D-054 and D-060 set them, and yields grow through skills.

## Progress log
- 2026-10-16 17:45: listed Crafting's numbers and sent the proposal to the PM.

## Open questions / blockers
- Waiting on Justin's picks for 1 to 4 (via the PM).

## Next step
When the PM relays the answers, apply them here: shared/Processes.cs Seconds; add `Progress.Curve` in shared/Processes.cs; tool speed in Crafting.cs StartCraft. Then open a PR.

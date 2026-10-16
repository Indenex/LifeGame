# crafting-13: Crafting numbers against D-075 (start low, grow slowly)
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-numbers
PR: #125

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

## D-092 craft-time proposal (sent to the PM 2026-10-16 18:40; waiting on Justin)
time = Base × Q × S × T
- Base = TierSeconds[tier] × Effort. TierSeconds: tier 0 3 s, tier 1 6 s, tier 2 12 s, tier 3 20 s. Effort runs from 0.5 (shred tinder, crush grog) to 3 (storage jar).
- Q (target quality q, the quality the job will produce, known at start) = 0.8 + 0.4 × q/100: crude 20 → 0.88, decent 60 → 1.04, excellent 95 → 1.18.
- S (skill) = Journal's linear Skills.DurationScale, 1 − 0.6 × rank/100 (docs/PROGRESSION.md: rank effects stay linear because Skills.Gain already front-loads): rank 0 → 1.00, 15 → 0.91, 50 → 0.70, 100 → 0.40.
- T (tool, D-089) = 1 − ToolSpeedup, up to 15%.

Example, with no tool bonus and a typical quality per rank (35, 45, 60, 75):
| Recipe (base) | rank 0 | 15 | 50 | 100 | today |
|---|---|---|---|---|---|
| Basket (t2 × 2.5 = 30 s) | 28 s | 27 s | 22 s | 13 s | 15 s |
| Stone axe (t2 × 1.5 = 18 s) | 17 s | 16 s | 13 s | 8 s | 10 s |
| Coil pot (t3 × 2 = 40 s) | 38 s | 36 s | 29 s | 18 s | 12 s |
| Storage jar (t3 × 3 = 60 s) | 56 s | 54 s | 44 s | 26 s | 16 s |
Fires (D-090) are separate: 20 min at lighting, and the cap grows from 20 to 40 min with Firecraft (PR #122).

## Progress log
- 2026-10-16 17:45: listed Crafting's numbers and sent the proposal to the PM.
- 2026-10-16 17:50: PM: drop Q1 (linear rank effects stay), keep Q4; Q2 and Q3 go to Justin.
- 2026-10-16 18:20: D-089 (Q3): better tools work faster, 0% for the crudest to 15% for a perfect one, applied in StartCraft after the skill's DurationFor. Tools without quality, like a stone used as a hammer, give nothing. Server and client build, and CraftCheck passes.
- 2026-10-16 18:50: per docs/PROGRESSION.md (PM), tool speed is now a power curve, 15% × (q/100)^0.73: about 7% at quality 35, 9% at 50 and 15% at 100. Growth() is removed. The D-092 proposal's skill factor is back to linear.

## Open questions / blockers
- Q3 answered: D-089. Q1 dropped and Q4 kept as is (PM, 2026-10-16 17:43: D-086 mastery stays about 6 h, D-081 fun first with some realism). Q2 became D-092 (craft-time formula): proposal below, waiting on Justin in the PM's next batch; until then the current numbers stay.

## Next step
When the PM relays the answers, wait for CI on the D-089 PR; apply Q2 (Seconds in shared/Processes.cs) when the PM relays Justin's answer.

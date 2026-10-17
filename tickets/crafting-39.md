# crafting-39: First pot run (stage timeline)
Lane: Crafting
Owner: Crafting thread (Sonnet, D-159)
Status: in_progress
Branch: crafting-first-pot
PR: (to open)

## Goal
PM ask: a brand-new character starts with nothing at spawn and, using only normal reducers at human pace, reaches a fired clay pot (gathering, first tools, fire, clay, shaping, drying, firing). The check asserts it succeeds and logs real-time minutes and step counts per stage. Findings go to the PM: blocked steps, stage pace against D-075, and steps with no Things-to-try hint.

ClayCheck's default mode already makes this whole journey (stone axe, log, claim, potter's bench, tempered clay, two pots, hand-drill fire, drying, firing, a pot of water). This ticket adds the stage timeline to it rather than a second bot: `Timeline` in `tools/ClayCheck/Program.cs` records each `STAGE` line's real minutes and the player actions (crafts, digs, pick-ups, tends) since the stage before, and prints the table when the run passes.

## Progress log
- 2026-10-17 14:30: branch off main 69c6705a; timeline added to ClayCheck, builds.
- Hint coverage by reading client/scripts/journal/NextSteps.cs against the processes the run uses: every process on the path has a Things-to-try line (gather.tinder, fire.hand_drill, knap.hand_axe, cord.twist, lash.stone_axe, chop.log, claim, bench, dig, clay.clean, clay.grit, clay.temper_grit, clay.pinch_pot, pit fire, fill).

## Open questions / blockers
- (none)

## Next step
Run ClayCheck locally (fresh db), paste the timeline here, open the PR, send the PM the findings and the green sha.

# skills-18: Swimming skill and repair.tool (skill homes)
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_review
Branch: claude/journal-skills-18
PR: (see below)

## Goal
Justin (via PM): "Both, but make wading train swimming." Tool repair trains Toolmaking through a new built-in process `repair.tool`; wading and floating train a new Swimming skill through `move.wade`, on the one shared formula (D-175), at the slow category pace (0.5, same as combat), with two small effects whose ceilings are single raisable constants (D-197).

## Progress log
- 2026-10-17 03:00: built. Skills: `swimming` (basic, trains exact id move.wade; exact id beats the move family so Endurance is unchanged), `repair.tool` joins Toolmaking. Built-in processes Wade and Repair in the journal table with hint lines and how-tos. Progression: SlowPaceFactor (renamed from CombatPaceFactor), IsSlow = combat + swimming; effects WadeSpeed 0.6 -> 0.75 and FloatSpeed 0.5 -> 0.7 (Ceiling constants). Wading.SpeedFactor(depth, swimmingRank = 0) uses them; rank 0 is exactly today's numbers. tools/ProgressionCheck covers the mappings, the slow pace and the wading effect. Hint harness 68/68.

## Open questions / blockers
- Reporters: Crafting must report `Skills.Repair` ("repair.tool") from RepairTool; Bug fixes must report `Skills.Wade` ("move.wade") from the wading movement (throttled, like move.run every ~20 s in water) and pass the player's Swimming rank into `Wading.SpeedFactor` (client Main.cs hook, Integration/Bug fixes).
- Effect values are proposals for Justin via the PM.

## Next step
Wait for CI on the PR and send the green sha to the PM; once Crafting and Bug fixes confirm their reporters, nothing else is needed here.

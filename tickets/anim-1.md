# anim-1: Player animation pass
Lane: Animation
Owner: Animation session (Opus)
Status: todo
Branch: (record the harness-assigned branch here)
PR: (once opened)

## Goal
The player figure (`client/scripts/Avatar.cs`) moves with a sine arm swing and bob. Done means: readable, grounded animation for the player: idle (breathing, weight shift), walk, sneak, and work poses for the main actions (gather/pick up, dig, knap, fire drill, chop once #70 lands, eat, strike), blended smoothly and driven from existing state (movement, CraftJob/process id, Use actions), visible on other players too. Stays cheap enough for 32 players. Coordinate with the Modeling lane (`model-1`), which may change the player mesh: agree a skeleton or part hierarchy first. Justin sees short clips from the Render lane before it merges.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).

## Open questions / blockers
- (none)

## Next step
Read `Avatar.cs` and how it is driven, agree the rig/part hierarchy with Modeling, then build idle and walk first.

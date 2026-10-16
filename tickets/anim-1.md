# anim-1: Player animation pass
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: in_progress
Branch: claude/anim-1-player-animation
PR: (once opened)

## Goal
The player figure (`client/scripts/Avatar.cs`) moves with a sine arm swing and bob. Done means: readable, grounded animation for the player: idle (breathing, weight shift), walk, sneak, and work poses for the main actions (gather/pick up, dig, knap, fire drill, chop once #70 lands, eat, strike), blended smoothly and driven from existing state (movement, CraftJob/process id, Use actions), visible on other players too. Stays cheap enough for 32 players. Coordinate with the Modeling lane (`model-1`), which may change the player mesh: agree a skeleton or part hierarchy first. Justin sees short clips from the Render lane before it merges.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).
- 2026-10-16 17:30: started on claude/anim-1-player-animation. Sent Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) the rig proposal: rigid segmented parts, no skinning; named pivots Avatar > Hips > Spine > Neck > Head, Shoulder/Elbow/Hand L/R (+ ToolR attach), Hip/Knee/Foot L/R; rest pose arms down, +Z forward; Animation writes only pivot rotations and Hips position. Modeling to supply `client/scripts/models/PlayerModel.cs`; until then Avatar.cs builds the same tree from capsules.

## Open questions / blockers
- Rig contract: waiting for Modeling's yes/changes (default: the proposal above).

## Next step
Build the named-pivot rig in Avatar.cs plus procedural idle and walk in `client/scripts/animation/`.

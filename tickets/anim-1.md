# anim-1: Player animation pass
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: in_progress
Branch: claude/anim-1-player-animation
PR: #100 (draft until Justin sees clips)

## Goal
The player figure (`client/scripts/Avatar.cs`) moves with a sine arm swing and bob. Done means: readable, grounded animation for the player: idle (breathing, weight shift), walk, sneak, and work poses for the main actions (gather/pick up, dig, knap, fire drill, chop once #70 lands, eat, strike), blended smoothly and driven from existing state (movement, CraftJob/process id, Use actions), visible on other players too. Stays cheap enough for 32 players. Coordinate with the Modeling lane (`model-1`), which may change the player mesh: agree a skeleton or part hierarchy first. Justin sees short clips from the Render lane before it merges.

## Progress log
- 2026-10-16: ticket written by the PM (D-073).
- 2026-10-16 17:30: started on claude/anim-1-player-animation. Sent Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) the rig proposal: rigid segmented parts, no skinning; named pivots Avatar > Hips > Spine > Neck > Head, Shoulder/Elbow/Hand L/R (+ ToolR attach), Hip/Knee/Foot L/R; rest pose arms down, +Z forward; Animation writes only pivot rotations and Hips position. Modeling to supply `client/scripts/models/PlayerModel.cs`; until then Avatar.cs builds the same tree from capsules.

- 2026-10-16 17:55: 94e645a: Avatar.cs now builds the figure as named joint pivots (thigh/shin/foot, upper arm/forearm/hand, spine, neck, head) and `client/scripts/animation/PlayerRig.cs` + `PlayerAnimator.cs` pose it procedurally: idle (breathing, weight shifting foot to foot with a relaxed knee, glancing around) and walk (stride length from speed, knee fold, heel-toe, hip drop/sway/twist, chest counter-twist, arm swing, lean into turns), blended by speed; stroll-to-brisk gait by speed. Remote players drive it from their smoothed position. No allocations per frame; hidden avatars skip it. Client builds locally.
- Render requested 94e645a.

- 2026-10-16 18:10: 6c06fec: sneak crouch (knees bent with feet planted, chest forward, head up, hands ready), added over idle and walk. Local player: Shift. Other players: inferred when they keep moving at about sneak speed for 0.4 s (server has no sneak flag), so a remote sneaker stands up when they stop.

- 2026-10-16 18:35: 6ed8294: work poses. `animation/WorkMotion.cs` maps process ids to motions (Gather, Knap, HandDrill, BowDrill, Twist, Knead, Scrape, Wrap, Carve, Tend): kneel on the right knee and loop an arm motion; blends in while a job runs and you stand still. `animation/AvatarWork.cs` sets your own avatar's motion from my_craft_job; one-line hook in Main.cs after `crafting.Attach(conn)` (told Integration). Other players' work poses wait for the public craft_work table (atmosphere-12, branch claude/atmosphere-12-work-sounds): when it merges, subscribe in AvatarWork to CraftWork insert/delete and set `other(id).Work`.
- Render requested 6ed8294 (work poses).

## Open questions / blockers
- Rig contract: waiting for Modeling's yes/changes (default: the proposal above).
- Sent PM 2026-10-16: (a) walk speed 4.2 m/s is jogging pace, animated as a brisk walk-jog (default) vs slow walk to ~3 m/s; (b) sync a sneak flag for other players (default: infer from speed).

## Next step
Wait for CI on #100 and Render clips, tune from them; then one-shot gestures (pick up, dig, eat, strike) and remote work poses once craft_work merges.

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

- 2026-10-16 17:45: PM: D-081/D-082, this is milestone M1; scope adds held tools and items in the hand, on other players too. a908055: `animation/HeldItems.cs` picks, from the process id, the tool (simplest item with the NeedsTool tag) for the right hand and the first consumed material for the left; Avatar.Hold shows them on attach points ToolR/ToolL (capsule figure has both; asked Modeling for ToolL on PlayerModel). Uses ItemLook ground meshes at 0.75 scale until Modeling's held models land. Asked PM for Justin's choice: (a, default) hold what the job uses / (b) chosen held item / (c) always carry best tool.

- 2026-10-16 17:50: Animals (D-074): clothing exists, public table worn_item {Owner, PlayerId, Slot, Item, Quality}, slots in shared/Clothing.cs (branch claude/animals-clothing-5gkntn, PR after #90). Proposed to Modeling: garments as meshes skinned to the player Skeleton3D via e.g. PlayerModel.Wear(skeleton, slot, item). Animation's part: subscribe worn_item and call Wear for each avatar, once PlayerModel and the clothing branch are on main.

- 2026-10-16 17:46: Modeling fd5c17a (claude/model-1-style): ToolR is now a Node3D under BoneAttachment3D "HandRAttach" in the fist (FindChild works); `ItemModels.HeldMesh(Item)` returns meshes already in the ToolR frame. When model-1 is on main: in Avatar.Hold use ItemModels.HeldMesh instead of ItemLook.MeshFor and drop the 0.75 scale; ToolL requested from Modeling.

## Open questions / blockers
- Rig contract AGREED with Modeling 2026-10-16 17:32: same joint names/tree/rest pose, but as Skeleton3D bones with rigid weights (one draw call per player). Modeling provides `client/scripts/models/PlayerModel.cs` `static Skeleton3D Build()` (branch claude/model-1-style), ToolR = BoneAttachment3D on HandR. 27543ee: PlayerRig accepts either a Skeleton3D or the Node3D pivot tree; swap in Avatar.BuildFigure when PlayerModel lands. 17:35: Modeling pushed PlayerModel.Build() (claude/model-1-style 067ed1c); a local trial merge with `figure = PlayerModel.Build();` compiles. Kept capsules in #100 until Justin picks a model style and model-1 merges; whichever of the two merges second makes the one-line swap.
- Sent PM 2026-10-16: (a) walk speed 4.2 m/s is jogging pace, animated as a brisk walk-jog (default) vs slow walk to ~3 m/s; (b) sync a sneak flag for other players (default: infer from speed).
- PM 2026-10-16 17:31: remote sneak takes (b), a server sneak flag through Integration. Asked Integration (session_01Rf5965nk4Lvd3f2QDteZSq) to add public table player_sneaking {PlayerId PK} (row while sneaking) + reducer SetSneaking(bool) + delete on disconnect + subscription. When it lands, AvatarWork calls SetSneaking on local Shift changes and drives remote avatars from it, replacing the speed guess in Avatar.cs. Walk speed question goes to Justin.

## Next step
Wait for CI on #100 and Render clips, tune from them (incl. held-item placement); then one-shot gestures (pick up, dig, eat, strike) and remote work poses once craft_work merges.

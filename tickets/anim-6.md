# anim-6: Wading and floating, and poses that fit the chibi body
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-6
PR: #485 (merged)

## Goal
PM 01:51 (D-145): keep busy on the new chibi base (Modeling, claude/model-6-chibi: Bramble and Pebble, 1.35 m, 2.8 heads, same 16-bone rig) and the new water movement (#414, shared/Wading.cs).
- Wading: knees lift higher and arms ride out to the sides, scaling with depth.
- Afloat (past Wading.FloatDepth): tread water at rest (leg circles, sculling hands, bob) and do a slow breaststroke when moving.
- Every pose fits a 1.35 m big-headed body as well as the 1.75 m adult: hand-to-mouth reaches come from the body's own arm lengths, and hip drops scale with leg length.

## Progress log
- 2026-10-17 02:05: built on claude/anim-5.
  - PlayerRig.Measure() reads UpperArm, Forearm, ShoulderL/R and Mouth from the rest pose. Avatar sets Mouth from the Head bone plus PlayerModel.MouthAt and re-measures on SetLook, in case a new look brings a new body.
  - WorkMotions.ToMouth is a two-bone reach to the mouth, checked numerically: FK lands the hand exactly on target. The chibi elbow folds about 0.9 rad where the adult folds about 1.9, so the old fixed -2.3 would have put the hand inside a chibi's head. Eat and Drink use it.
  - Dig, PickUp and Feed hip drops now scale with LegLength.
  - PlayerAnimator.WaterDepth comes from the avatar's height below Look.WaterLevel (minus any air height); Wade() and Swim() poses blend in.
  - Client builds.

## Open questions / blockers
- Remote avatars' height in Main.OnPlayerPos uses terrain height, not Wading.FeetY, so a floating player appears at the bed for others. Told Integration (Main.cs is theirs).
- Nameplate height from the figure (model-6 plan item 4): when PlayerModel switches to ChibiFigure.

## Next step
When #398 merges: merge main into claude/anim-6, build, open the PR, send the green sha to the PM; ask Render for wading, floating and swimming clips.

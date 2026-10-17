# anim-13: Idle fidgets and hop-and-squish movement (cutesy #15, #17)
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-13 (on main since #600 merged)
PR: #613

## Goal
Justin's cutesy picks #15 and #17, kept gentle (D-313).
- #15: after a few seconds standing still (first at 4 s, then every 8 to 16 s), the figure stretches, yawns (hand over the mouth, head back, then an arm stretched out), looks around or rolls its shoulders. It waves at the nearest player within 6 m in front of it (at most once a minute), sways happily with palms to the heat by a burning fire, and rubs its hands in the cold.
- #17: the walk has a little hop in each step. It's full on the chibi (hips under 0.6 m) and only a trace on a tall figure. Landing a jump squashes the body and springs it back, keeping its volume; taking off stretches it slightly.
Everything is client-side from state every client already has (positions, the fire table, jump state), so other players see it at no network cost.

## Design
- `PlayerAnimator`:
  - Gesture enum gets Yawn, Wave and Sway (appended); `LengthOf(gesture)`.
  - `PickGesture` asks `Surroundings` (warmth, friend yaw) only when a fidget starts. `MoreGestures` holds the Yawn (uses `WorkMotions.ToMouth`, now internal) and the Wave.
  - Walk: `hop` lift as the legs pass, spine dip as the foot lands; `Bounce` sets the strength.
- `Avatar`:
  - static list of avatars and `Surroundings()` (nearest other avatar within 6 m and 80° of facing).
  - static `FireWarmthAt`, set in AvatarWork from the Fire table using `Fires.WarmthFrom` on fires whose FuelUntil is still ahead.
  - `Squish` spring on `figure.Scale`, driven from SetJump.

## Progress log
- 2026-10-17 07:40: built; client builds.
- 2026-10-17 07:50: pushed e2a67c28, opened #613 (base claude/anim-12); Render C clip requested.
- 2026-10-17 07:40 UTC: CI on 37cd95aa green (run 39013074722: clients pass, server jobs skipped as the diff over anim-12 is client-only).

## Open questions / blockers
- #16 (blinking and expressions) needs switchable face parts from Modeling. I proposed `PlayerModel.SetFace(figure, Face {Open, Closed, Happy, Sleepy})` to Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm); waiting on their answer. That will be anim-14.

## Next step
Wait for #600 to merge, retarget #613 to main, send the green sha; fix anything the clip shows.

# anim-20: Pebble waddles
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-20 (from main)
PR: #710

## Goal
Pebble, the shell-backed chibi, walks with a gentle waddle: its hips rock side to side over each foot. The chest counters part of the rock, so the head stays fairly steady and the feet stay planted. Other races walk as before. It fades out at a run. A follow-up to anim-10 (#466, chibi default).

## Design
- `PlayerAnimator.Waddle` (0 to 1) adds to the walk's hip roll. The existing Legs() compensation keeps the feet down; the spine counters 70%.
- `Avatar.SetLook` sets it to 1 for ChibiFigure.Race.Pebble when the chibi body is in use.

## Progress log
- 2026-10-17 10:30: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha; ask Render C to include a Pebble walk in a later clip.

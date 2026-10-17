# anim-21: Pebble's arms keep clear of its shell
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-21 (from main)
PR: #714

## Goal
Pebble (the shell-backed chibi) no longer pushes its arms through its shell:
- Walking and running, its arms swing back less (at most 0.25 rad behind) and a little wider.
- Sitting, it never picks the lean-back-on-the-hands fidget; it hugs its knees instead.
Other races are unchanged.

## Design
- `PlayerAnimator.Shell` is set by `Avatar.SetLook` for Pebble on the chibi body, next to Waddle.
- Walk: the backward shoulder swing is clamped and widened when Shell is set. SitFidgets: LeanBack is swapped for HugKnees.
- Also marks anim-20 done.

## Progress log
- 2026-10-17 10:40: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha.

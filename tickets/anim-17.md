# anim-17: Chibi doggy paddle
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-17 (from main)
PR: (see below)

## Goal
A small round chibi swimming through deep water does a doggy paddle instead of the adult breaststroke: head held high and bobbing side to side, hands paddling in turn under the chin, quick little kicks. Treading water at rest is unchanged. Option (b) from the PM's list of next items.

## Design
- `PlayerAnimator.Swim` calls `DoggyPaddle(p, t, moving)` when `Bounce >= 1` (hips under 0.6 m, the chibi). Tall figures keep the breaststroke.

## Progress log
- 2026-10-17 09:45: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha; add a swim to the next Render C clip.

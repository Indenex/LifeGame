# anim-22: Chibi tiptoe sneak
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-22 (from main)
PR: (see below)

## Goal
When a chibi sneaks (Ctrl), it goes up on tiptoe instead of the adult's deep crouch. Knees soft, heels raised, chest leaning forward with the head up, and the hands held up in front like paws. Tall figures keep the crouch, and the landing dip after a jump is unchanged. This is the PM's option (a), sent to Justin as my recommendation.

## Design
- `PlayerAnimator.Crouch(p, w, sneak)` calls `Tiptoe` when sneaking on a small round body (`Bounce >= 1`).
- Tiptoe: a light knee bend (0.3), the hips lifted by the heel rise, feet rotated onto the toes, and paw arms.
- Also marks anim-21 done.

## Progress log
- 2026-10-17 10:50: built; client builds.

## Open questions / blockers
- Justin may pick b (jump wind-up) or c (dozing sway) instead. In that case this waits.

## Next step
Wait for CI and send the PM the green sha.

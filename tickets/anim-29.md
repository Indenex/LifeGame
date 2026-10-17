# anim-29: jump wind-up (prototype, waits on Justin)
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: blocked
Branch: claude/anim-jump-windup
PR: none until Justin picks; it changes jump feel, so the PM said no PR before then.

## Goal
Make jumping feel springier. Pressing jump dips the knees and swings the arms back for 0.1 s (`JumpMotion.WindUpSeconds`), and then the feet leave the ground. For the first 0.16 s in the air the legs straighten and the toes point (push-off), then the usual tuck follows. Setting `WindUpSeconds = 0` keeps the push-off with no delay: a visual-only option.

Options for Justin (via the PM):
- (a) 0.1 s wind-up + push-off [recommended]
- (b) push-off only, no delay
- (c) keep today's jump

## Progress log
- 2026-10-17 12:10: prototype on claude/anim-jump-windup. JumpMotion holds a pending take-off for the wind-up, Main passes `jump.WindUp` to `Avatar.SetJump`, and PlayerAnimator adds WindUpPose and PushOff. Client builds. Render C has a clip request.
- 2026-10-17 12:15: merged main (resolved a PlayerAnimator conflict with the anim-28 comment move). This branch also carries anim-28 marked done (#756), which needs to reach main through the next Animation PR if this one is dropped.
- 2026-10-17 14:35: wrap-up. Head d96b8111 on claude/anim-jump-windup, with no PR and no CI run (client builds locally). Render C has a side-view clip request (prototype vs main) for the PM. Render C note: to light a fire in their renders, use StartCraft("fire.hand_drill", 1.0f), because a score under 0.35 silently fails.
- 2026-10-17 14:45: Render C clips (prototype at d96b8111, left, vs main, right; profile; standing then walking jump), also sent to the PM. Real time: https://github.com/Indenex/LifeGame/raw/claude/render-c-clips/renders/anim-29-jump-side-by-side.mp4. 0.25x: https://github.com/Indenex/LifeGame/raw/claude/render-c-clips/renders/anim-29-jump-side-by-side-slow025.mp4. They were recorded at 12 fps, so the 0.1 s dip is about one frame. Toe direction in the push-off is not yet verified; check the slow clip. If the dip reads too faintly, consider 0.12 to 0.15 s and ask Justin.

## Open questions / blockers
- Justin's pick (a/b/c).
- Known prototype gaps if (a) is picked:
  - The `Jump` reducer is still called on the press, so other players see the hop 0.1 s before your own take-off. It should move to the take-off frame.
  - Other players' jumps (JumpView) don't show the wind-up dip yet.
  - JumpMotion belongs to the jump owner (bugfix-25); tell them before merging.

## Next step
Wait for Justin's pick (a/b/c) through the PM. On (a) or (b): move conn.Reducers.Jump() to the take-off frame, show the dip on other players in JumpView, tell the jump owner (bugfix-25), merge main, open the PR, send the green sha. On (c): drop the branch, and bring the anim-28 done-marking to main in the next PR.

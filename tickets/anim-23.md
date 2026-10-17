# anim-23: Dozing on its feet at night
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-23 (from main)
PR: (see below)

## Goal
Late at night (Atmosphere.Night > 0.75), a figure standing still sometimes nods off on its feet, as one of its idle fidgets. Over about 3.5 s its head sinks forward, its shoulders slump and its body sways gently, with its eyes closed. Then it jerks awake with a little start and straightens, eyes open. This is option (c) from the PM's question to Justin. Client only, so others see it too.

## Design
- `Gesture.Doze` is appended to PlayerAnimator's gesture list, with a length of 5 s.
- `PickGesture` picks it at night for half the picks that aren't cold or warm.
- Its pose is in `GesturePose`. `Expressions` shows Closed until the start, then Open.
- Also marks anim-19 and anim-22 done.

## Progress log
- 2026-10-17 10:55: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha.

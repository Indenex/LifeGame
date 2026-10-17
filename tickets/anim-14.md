# anim-14: Blinking and expressions (cutesy #16)
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_progress
Branch: claude/anim-14 (stacked on claude/anim-13, #613, with Modeling's claude/model-15-faces merged in)
PR: (not yet)

## Goal
The chibi blinks, looks happy while eating, waving or swaying by a fire, and looks sleepy when lying down, yawning or standing idle late at night. Asleep (lying), its eyes are closed. Other players see it because the face is worked out on each client from public state (acts, rest, gestures, Atmosphere.Night); nothing new goes over the network.

## Design
- `PlayerAnimator.Expressions` picks `Face` (enum Expression {Open, Closed, Happy, Sleepy}, the same order as Modeling's Face).
- Blinks come every 2.5 to 6 s (slower when sleepy), last 0.13 s, and are sometimes doubled. Blinks only show over Open and Sleepy.
- Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) is building `PlayerModel.SetFace(Skeleton3D, Face)`: a visibility flip, a no-op without face parts, cheap to call every frame. Expected around 09:00 to 10:00 UTC.

## Progress log
- 2026-10-17 07:40: face selection logic built in PlayerAnimator; client builds. Not wired to the model yet.
- 2026-10-17 08:00: merged Modeling's claude/model-15-faces (e372053b). PlayerAnimator.Face now uses their Face enum; Avatar._Process calls PlayerModel.SetFace every frame. Client builds.

## Open questions / blockers
- Merges after Modeling's faces PR and #613.

## Next step
Open the PR (base claude/anim-13) and request a Render C clip of blink, eat, wave and lie down.

# anim-15: Sitting life
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_progress
Branch: claude/anim-15 (stacked on claude/anim-13, #613)
PR: #633

## Goal
Sitting (anim-12) feels alive and cosy:
- Pressing H to sit near a burning fire (within 5 m) turns you to face it first.
- Once settled, a sitter shifts into a fidget every 8 to 16 s and holds it for 7 to 12 s:
  - hugging the knees
  - leaning back on the hands
  - looking up at the sky (more likely at night)
  - palms out to the fire when one is warm nearby
- On a raised seat, only looking up and warming the hands are used.
- All client-side from state every client has, so others see it too.

## Design
- `RestPoses.SitFidget` and `RestPoses.Fidget(p, f, w, t)` lay a fidget over the Sit pose by weight.
- `PlayerAnimator.SitFidgets` picks and times them, using `Surroundings` for warmth and `Atmosphere.Night`.
- `AvatarWork.NearestFire` (burning fires within Fires.WarmthRadius); the RestRequested handler calls Face before Rest.

## Progress log
- 2026-10-17 07:55: built; client builds.

## Open questions / blockers
- (none)

## Next step
Wait for #600 and #613 to merge (retarget each in turn), then send the PM the green sha. Render C clip requested.

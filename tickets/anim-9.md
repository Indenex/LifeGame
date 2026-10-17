# anim-9: Idle life (stretch, look around, shoulder roll, cold hands and shiver)
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-9
PR: #508 (merged)

## Goal
A player standing still for a while comes alive. Every 12 to 24 s (its own timing per figure, so a crowd never moves in step) it plays one of these:
- a stretch, arms up and onto the toes
- a slow look over each shoulder
- two shoulder rolls
- in the cold, rubbing its hands
In the cold it also shivers finely while standing. Any movement or work blends a gesture out at once. Cheap: one extra pose and a few sines, only while idle.

## Progress log
- 2026-10-17 04:00: started (option c, offered to the PM while the chibi wiring waits on Modeling). PlayerAnimator.Gestures and GesturePose are upper body only; HipsOffset only on the stretch. Cold comes from Breath.Coldness (Needs.Breath from Atmosphere's air temperature). Client builds.
- 2026-10-17 04:05: PM: do (c), subtle and cosy. Softened: gestures every 12 to 24 s, a smaller stretch (arms to -2.3, less lean, less toe lift) and a smaller look-around.

## Open questions / blockers
- (none)

## Next step
Done (#508 merged 04:13).

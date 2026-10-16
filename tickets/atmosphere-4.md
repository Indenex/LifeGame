# atmosphere-4: Firelight and early night
Lane: World atmosphere and sound
Owner: Atmosphere thread (session_01PQgMLoZLFAprUyZzmQuSUX)
Status: done
Branch: claude/atmosphere-sound-hlqfke
PR: #32

## Goal
A campfire at night throws a wide, soft, warm pool of light with a glow in the mist and a living flicker, and the hour after sunset is dusky rather than pitch black. Justin sees it in night screenshots.

## Progress log
- 2026-10-15 23:24: added SoundScape.MusicVolume (0..1) and Atmosphere.Sound for UX's Esc-menu music slider (#33), via PM.
- 2026-10-15 23:15: night render showed a small light pool and a near-black early night (0.79: the moon sat 10 degrees up, so it only grazed the ground). Moon now rides at least 16 degrees up, twilight ambient raised a little; FireView light range 7 to 10 m, softer falloff, double glow in the mist, and its flicker now comes from Atmosphere.FireFlicker (breathing, licks, the odd dip as a log shifts). FireView is Crafting's file: 4 lines changed, Crafting told.

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #32 and Justin's OK via the PM (look change); send the green sha to the PM, who merges.

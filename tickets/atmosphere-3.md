# atmosphere-3: Campfire sound
Lane: World atmosphere and sound
Owner: Atmosphere thread (session_01PQgMLoZLFAprUyZzmQuSUX)
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke
PR: #27

## Goal
Campfires sound alive: a soft breathing roar under quick crackles and the odd sap pop, louder the closer you are and the better the fire burns, fading to quiet embers as fuel runs out, with sizzles when rain falls on it. Justin hears it in game near any lit fire.

## Progress log
- 2026-10-15 23:05: started (PM asked lanes to pick up their next ticket while #16 waits). FireVoice in SoundMix; Atmosphere listens to conn.Db.Fire (own handlers, FireView untouched) and mixes fires within 18 m, panned to the nearest. SoundPreview scenes `fire` and `firerain`.

## Open questions / blockers
- (none)

## Next step
Get PR #27 green (CI), then merge it (D-032: sound only, no look change) and send the commit to the PM.

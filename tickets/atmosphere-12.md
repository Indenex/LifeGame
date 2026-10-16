# atmosphere-12: Work sounds
Lane: World atmosphere and sound
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-12-work-sounds (stacked on claude/atmosphere-sound-hlqfke / #66)
PR: (after #66)

## Goal
Lane default while #66 waits on Justin (proposed to the PM as options, recommended first). Every craft job makes a sound that fits it while it runs, synthesized like the rest of SoundMix: knapping is stone clicks, drills are a wooden friction whirr that turns to a hiss as the ember catches, twisting cord and weaving are soft fibre rustles, clay work is wet pats, scraping hide is a rasp, carving is wood shaves, lashing is cord creaks. A job ends with a small success thunk, or a dull snap when it failed. Sounds sit under the ambience like footsteps (StepGain). Only your own jobs for now: the server shows each client only its own CraftJob. Done: a SoundPreview clip of each process family sent to the Render lane/PM, and a PR green after #66.

## Progress log
- 2026-10-16: started from 9430732 (#66 merged with main). Client listens to conn.Db.MyCraftJob insert/delete (process id, FinishAt, Failed); SoundMix gets a WorkVoice.

## Open questions / blockers
- Sent the PM: next atmosphere ticket options (work sounds recommended; tree-fall crash for D-060; other players' work sounds needs a public job table).

## Next step
Implement WorkVoice in client/scripts/atmosphere/audio/SoundMix.cs and hook MyCraftJob in Atmosphere.cs.

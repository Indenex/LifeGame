# atmosphere-14: Weather you see coming
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: todo
Branch: (set when started; stack on the previous atmosphere branch if it hasn't merged)
PR: (none yet)

## Goal
Queued by Justin (D-085): forest light (atmosphere-13) first, then weather you see coming (atmosphere-14), then the cosy night (atmosphere-15). Rain curtains hanging under distant clouds, the sky darkening on the side a storm comes from before it arrives, and a soft rainbow opposite a low sun when rain clears. Mostly the sky shader and Rain.cs; cheap. Done: Render lane shots of a storm approaching, rain in the distance seen from a dry spot, and a rainbow after rain; a PR green. Pair with Environment (D-078) where a change crosses both lanes. Sounds for it go to the Audio lane as requests (D-079).

## Progress log
- 2026-10-16: queued (D-085).

## Open questions / blockers
- Idea to offer the PM with this ticket (D-059 options): after Environment's env-2 reshape (#175: stream 45 m west of spawn, ponds, gentler hills, water still at -1 m), mist that lies in valleys and over ponds and the stream at dawn and dusk, using World.BaseHeight and the existing atmosphere-11 Mist (which already pools in hollows by depth). Options to send: A (rec) extend Mist to read terrain height so valleys and water fill first; B leave dawn mist as is.

## Next step
Start after atmosphere-13 is in review: create the branch, set Status in_progress, record it here.

# atmosphere-14: Weather you see coming
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-14-weather-ahead
PR: #217

## Goal
Queued by Justin (D-085): forest light (atmosphere-13) first, then weather you see coming (atmosphere-14), then the cosy night (atmosphere-15). Rain curtains hanging under distant clouds, the sky darkening on the side a storm comes from before it arrives, and a soft rainbow opposite a low sun when rain clears. Mostly the sky shader and Rain.cs; cheap. Done: Render lane shots of a storm approaching, rain in the distance seen from a dry spot, and a rainbow after rain; a PR green. Pair with Environment (D-078) where a change crosses both lanes. Sounds for it go to the Audio lane as requests (D-079).

## Progress log
- 2026-10-16: queued (D-085).
- 2026-10-16 20:00: started from main after #173 (seasons) merged.

- 2026-10-16 20:10: new AtmosphereFronts.cs: reads Weather.TargetCloud/TargetRain (the weather the server is turning to); front amount = smoothstep(0.05, 0.35, how much worse it is about to get), eased over 8 s. Sky shader: on the upwind side (opposite WindDirection), low in the sky, cloud cover rises to the target and clouds darken with target rain; grey rain curtains under them down to the horizon. Rainbow at 42 degrees from the antisolar point, red outside, for 1 to 4 min after rain while the sun is out and between about 6 and 35 degrees high. Args --front=1, --rainbow=1. Client builds; render requested.

## Open questions / blockers
- Idea to offer the PM with this ticket (D-059 options): after Environment's env-2 reshape (#175: stream 45 m west of spawn, ponds, gentler hills, water still at -1 m), mist that lies in valleys and over ponds and the stream at dawn and dusk, using World.BaseHeight and the existing atmosphere-11 Mist (which already pools in hollows by depth). Options to send: A (rec) extend Mist to read terrain height so valleys and water fill first; B leave dawn mist as is.

## Next step
Wait for the Render lane's shots; open the PR (renders don't block merges, D-142) once CI is green.

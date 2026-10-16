# atmosphere-21: Storms and lightning
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-21-storms
PR: (none yet)

## Goal
Justin's pick (2026-10-16, via the PM), order 19 warm homes, 20 wet world, 21 storms. Rare thunderstorms: a darker, heavier sky, harder rain, wind, and lightning flashes that light the land, with thunder rumbling after (Audio does the sounds; SoundMix already has thunder, Atmosphere already triggers it with a delay by distance). Builds on the existing Lightning(dt) and storm weather; makes storms rarer and more dramatic so shelter feels cosy. The server's weather mix may need a storm weighting (WorldClock.cs is Atmosphere's). Done: Render lane clip of a storm rolling in and a flash, and a PR green.

## Progress log
- 2026-10-16 21:45: ticket opened.
- 2026-10-16 22:35: first build (server and client 0 errors). New client/scripts/atmosphere/AtmosphereStorms.cs, with Lightning moved there from Atmosphere.cs:
  - `storminess` (eased from rain 0.75-0.95) darkens storms further: sky and fog x(1 - 0.3), sun x(1 - 0.5), ambient x(1 - 0.3), fog light toward slate. Storm clouds are bruised blue-grey underneath (sky `storm` uniform).
  - Strikes every 10-30 s at a storm's height, each in a random direction. Every flash lights the clouds from inside toward that direction (sky `flash`/`flash_dir`).
  - Close strikes (about 1 in 4) draw a jagged bolt with 1-3 forks 250-700 m away. It is drawn as camera-facing additive ribbons, unaffected by fog, and flickers with the double flash. A second DirectionalLight from the strike side casts hard blue-white shadows, on only during the flash.
  - Server (WorldClock.NextWeather): storms are weighted by season, as a share of rainy fronts: summer 16%, spring and autumn 7%, winter 3%. That is about 1 front in 45, 50 and 150. Winter ones mostly fall as snow, and snow throws no lightning. No schema change.
  - `--lightning=seconds` forces a close strike that often, for renders.

## Open questions / blockers
- Audio: any new thunder variants (send them a request when the visuals land).

## Next step
Open the PR, send the Render lane a request (storm at dusk, --rain=1 --wind=0.9 --cloud=1 --lightning=6), and ask Audio via the PM whether thunder needs anything new.

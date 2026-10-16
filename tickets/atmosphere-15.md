# atmosphere-15: A cosy night
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-15-cosy-night
PR: #245

## Goal
Queued by Justin (D-085): forest light (atmosphere-13) first, then weather you see coming (atmosphere-14), then the cosy night (atmosphere-15). Firelight warming nearby trunks and ground (a soft warm light per burning fire, capped to the nearest few), clearer starry nights when the sky is clear, softer moonlight. Done: Render lane shots of a campfire in the woods at night, a clear starry sky and a cloudy night; a PR green. Pair with Environment (D-078) where a change crosses both lanes. Sounds for it go to the Audio lane as requests (D-079).

## Progress log
- 2026-10-16: queued (D-085).
- 2026-10-16 20:28: started, stacked on #217.

- 2026-10-16 20:35: firelight already exists (FireView's Campfire OmniLight, range 10, warmed in atmosphere-4; that file is Crafting's), and the night moods were approved in atmosphere-4, so this ticket is the sky. Sky shader: a faint mottled galaxy band across clear nights, denser stars inside it; stars twinkle at their own pace, tinted pale blue to warm white; a 22-degree halo round the moon behind thin cloud (cover 0.25 to 0.95). Possible follow-up for Crafting: fire light reaching a little further at night (offer as options).

- 2026-10-16 20:37: pushed 6e85469 (client builds). Render request and the fire-reach idea (A: 10 m by day to about 14 m at night via an Atmosphere night value plus a one-line change in Crafting's FireView; B: keep) sent to the PM to route.

- 2026-10-16 20:40: #217 merged; merged main into this branch (clean), builds. Opening the PR.

- 2026-10-16 20:35: Justin picked A, campfire light reaches about 14 m at night (10 m by day). Added Atmosphere.Night (0 day to 1 night, updated every frame); Crafting (session_01N9APthjRDfjknUvCweqzzN) adds `light.OmniRange = 10f + 4f * Atmosphere.Night;` in FireView's Campfire._Process after #245 merges.

- 2026-10-16 21:38: #245 CI green on f771ef8 (https://github.com/Indenex/LifeGame/actions/runs/38963008470), merged by the PM. Crafting told to add the FireView line (light.OmniRange = 10f + 4f * Atmosphere.Night).

## Open questions / blockers
- (none)

## Next step
None; done. Tune from the Render lane's shots in a small follow-up if they show a need.

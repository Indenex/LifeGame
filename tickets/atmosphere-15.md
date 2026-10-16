# atmosphere-15: A cosy night
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-15-cosy-night (stacked on claude/atmosphere-14-weather-ahead / #217)
PR: (none yet)

## Goal
Queued by Justin (D-085): forest light (atmosphere-13) first, then weather you see coming (atmosphere-14), then the cosy night (atmosphere-15). Firelight warming nearby trunks and ground (a soft warm light per burning fire, capped to the nearest few), clearer starry nights when the sky is clear, softer moonlight. Done: Render lane shots of a campfire in the woods at night, a clear starry sky and a cloudy night; a PR green. Pair with Environment (D-078) where a change crosses both lanes. Sounds for it go to the Audio lane as requests (D-079).

## Progress log
- 2026-10-16: queued (D-085).
- 2026-10-16 20:28: started, stacked on #217.

- 2026-10-16 20:35: firelight already exists (FireView's Campfire OmniLight, range 10, warmed in atmosphere-4; that file is Crafting's), and the night moods were approved in atmosphere-4, so this ticket is the sky. Sky shader: a faint mottled galaxy band across clear nights, denser stars inside it; stars twinkle at their own pace, tinted pale blue to warm white; a 22-degree halo round the moon behind thin cloud (cover 0.25 to 0.95). Possible follow-up for Crafting: fire light reaching a little further at night (offer as options).

- 2026-10-16 20:37: pushed 6e85469 (client builds). Render request and the fire-reach idea (A: 10 m by day to about 14 m at night via an Atmosphere night value plus a one-line change in Crafting's FireView; B: keep) sent to the PM to route.

## Open questions / blockers
- (none)

## Next step
When #217 merges: merge main into this branch and open the atmosphere-15 PR; if Justin picks A, add Atmosphere.Night and ask Crafting for the FireView line.

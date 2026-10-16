# atmosphere-11: Sky that moves
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke after #66 merges (local atmosphere-11-wip, stacked on atmosphere-10-wip)
PR: (after #66)

## Goal
From the PM, 2026-10-16: slow drifting clouds whose cover follows the weather, soft cloud shadows sliding across the land on fair days, and low mist pooling in hollows at dawn that burns off by mid-morning. Subtle, crossfaded, Valheim-like. Done: screenshots to the PM, a PR green after #66 merges.

## Progress log
- 2026-10-16 05:10: drifting sky clouds with cover from the weather already existed (Atmosphere SkyShader, cloud_offset drifting with wind). Cloud shadows existed too but at 0.004 scale (~250 m features) and a threshold of 1-cover, so on fair days (cover 0.2-0.4) there were almost none. Now 0.01 scale (sky mapping changed to match: 120 * 0.01), threshold mix(0.68, 0.2, cover), strength 0.5: patches a few dozen meters across slide over the land. New Mist.cs: two unshaded planes at y -0.45 and -0.1 following the player; alpha from the depth buffer's ground height (thicker where the ground dips), two drifting noise scales for wisps, edge fade at 45-68 m. Amount: Mist.Amount(time, wind, rain) peaks just after sunrise (0.24-0.29), gone by 0.38, less in wind and rain; color is the fog light warmed by the sun. Screenshots /mnt/project-files/screenshots/atmosphere-19-{mist-dawn-pond,mist-dawn-meadow,mist-burning-off,fair-day-cloud-shadows}.png.

## Open questions / blockers
- (none)

## Next step
When #66 merges: merge main into atmosphere-11-wip, push to claude/atmosphere-sound-hlqfke, open the PR.

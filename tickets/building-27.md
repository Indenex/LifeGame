# building-27: Who built it, and a house-warming
Lane: House building
Owner: House building thread (session_01DzgG4copWqifKWUcydbjBZ)
Status: in_progress
Branch: claude/building-ht3tnl (after building-26)
PR: (not yet)

## Goal
"Home and friends" focus. When friends build together, everyone sees who helped: an unfinished site's line reads "Built so far by Ana 12, you 6", and standing sheltered in a finished home reads "Sheltered in the log cabin, built by Ana, Bo and you". The server records each builder's item count in a new public `structure_credit` table (one row per builder per structure, removed with the structure). Claim markers are not credited. Justin picked "names plus a house-warming" (option 2, via the PM 2026-10-17): when a home (lean-to, hut or house) built by two or more people finishes, each helper gets the note "You and Ana finished the log cabin. Welcome home!" and helpers within 25 m hear a quiet warm cheer (CueDefs.HouseWarming in client/scripts/building/CueDefsHouseWarming.cs: two knocks on the door post, a few loose claps, a soft low chord; no Audio lane running, so House building made it). Client only; no play effect.

## Progress log
- 2026-10-17 17:30: server table + RecordCredit (called from RecordBuilder), client BuildingSystem.Credit.cs, site and shelter lines, BuildTest BuildTogether checks credits sum to the full cost (skipped for claim markers).
- 2026-10-17 17:45: BuildTest green locally on fresh servers: lean-to,basket,fence (119 ok; one earlier run missed "clear ground either side of the fence" in a crowded world, fence alone passes), coop,stone-hearth,sundial (98 ok), clay-hut (101 ok). Committed on local building-27-wip; patches in /mnt/project-files/patches/building-27/.
- 2026-10-17 18:05: house-warming note and cue (BuildingSystem.HouseWarming.cs, CueDefsHouseWarming.cs); `--building-showcase --building-housewarming` shows the note and plays the cheer offline. Shot: /mnt/project-files/screenshots/building-27/housewarming.png. Client builds; rebased onto building-26.

## Open questions / blockers
- (none; Justin picked option 2)

## Next step
After #847 and building-26 land on claude/building-ht3tnl, apply /mnt/project-files/patches/building-27/ on top, regenerate bindings, rebuild, push and open the PR.

# bugfix-8: Plain loops for the Use-key finders
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: done
Branch: claude/bugfix-lyle6o
PR: #59

## Goal
The Use-key finders that run every frame stop allocating garbage, which feeds the GC hitches measured in bugfix-6. CeramicsView.NearestKiln and Carrying, Journal.NearestNote, BuildingSystem.NearestSite and the crafting.take_roast Use handler (new CraftingUi.OldestDoneRoast) become plain foreach loops instead of LINQ chains. Behaviour is unchanged: same nearest pick, same ranges, same prompts. Justin sees nothing different except fewer stutters. Survival does its own finders.

## Progress log
- 2026-10-16 02:10: loops written on a local branch while #57 was in review (patch in /mnt/project-files/bugfix/bugfix-8/).
- 2026-10-16 02:45: #57 merged (main 3c8c32b); branch restarted from main and the commit cherry-picked cleanly.
- 2026-10-16 02:50: local server, bindings and client build green; pushed and opened PR #59.

## Open questions / blockers
- (none)

## Next step
None; merged.

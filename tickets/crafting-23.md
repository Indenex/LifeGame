# crafting-23: Batch crafting at dedicated devices (D-195)
Lane: Crafting (Ceramics: clay processes; Modeling: device models; UX: the panel)
Owner: Crafting thread (Sonnet, D-159)
Status: in_review
Branch: crafting-devices
PR: (to open)

## Goal
Justin (D-195, revised by D-209): cleaning clay, crushing stone, tempering and similar simple hand processes get dedicated devices you load up so you don't have to do them one at a time. A loaded device runs on its own, but unattended output takes a large quality penalty; if you stay and play the minigame you get quality and quantity bonuses, so playing is worth it. Doing it by hand, one at a time, stays exactly as it is. Package A picked: batch cap 10; unattended x0.6 quality, 1.5x hand time, 25% skill gain; attended up to x1.1 quality, +20% quantity, 0.8x time, full skill gain.

## Design
- Devices (first three): settling tub (clay.clean), crushing block (clay.grit, clay.grog; a hammer in the bag), tempering trough (clay.temper_grit, clay.temper_grog, clay.daub). Later: cordage frame (cord.twist), brick mould frame (clay.brick), Animals' hide frames.
- Each device is a StructureKind with a StationKind (claim only, like the tables). shared/Devices.cs holds the process lists and every number.
- LoadDevice(structureId, processId, count): takes inputs (bag, nearby storage, stash), makes a DeviceJob (one per device) that works in rounds of 5 units. WorkDevice(structureId, score) while a round runs: the player stands at the device after playing the minigame (Strike for the block, Shape for tub and trough); that round becomes attended. DeviceOutput rows hold what is made; CollectDevice moves it to the bag.
- Skill: unattended rounds report once per 4 units, attended once per unit (OnProcessPerformed).

## Progress log
- 2026-10-16: PM relayed Justin's D-195 and package A. Branch created off main.

- 2026-10-16: built and verified locally on crafting-devices: shared/Devices.cs, server/spacetimedb/Devices.cs (DeviceJob, DeviceTimer, DeviceOutput; LoadDevice, WorkDevice, FinishDeviceRound, CollectDevice), StationKind ClayTub/CrushBlock/TemperTrough and StructureKind SettlingTub/CrushingBlock/TemperingTrough with shapes and costs, client DeviceUse.cs (Use key loads, works, collects) and CraftingUi.Devices.cs (plays the minigame for a device), subscriptions. CraftCheck builds a tub, runs one unit unattended (quality 30) and five attended with a perfect score (6 clean clay at 55), and checks the refusals. Upgrade check passes.

- 2026-10-16: Justin's D-209 (via PM): processes with no minigame by hand (clean clay, grit, grog, temper, daub; all three first devices) are plain batch queues: same output, quality and time as by hand, full skill, nothing to play (WorkDevice refuses). Package A's penalty and bonuses (shared/Devices.cs) stay in the code for future processes that have a minigame. Kinds merge order per PM: #271 LogCabin, then #267 RepairTable, then #285; each re-appends after the last kind. Ceramics blessed the costs; Modeling's meshes are #287.

## Open questions / blockers
- Quality on flat-quality materials: clean clay, grit, grog, tempered clay and daub have no quality of their own (they always come out of the hands at 50), so the device's penalty and bonus are measured against 50 and carry into the pots made from them (unattended 30, attended up to 55) but no grade shows on the item. Making them show a grade (HasQuality) would also change what hand-crafting gives new players (the rank-0 skill cap is 35, below today's flat 50), so it needs Ceramics and the PM first. Asked the PM.
- Unattended skill gain is reported once per four units; Journal's hook takes no weight.
- A device taken down mid-batch hands the round's output straight to its owner; output already waiting in a removed device is orphaned (no cleanup yet).
- Output stays in the device until collected; auto-spilling into nearby storage is not built.
- Kinds: StructureKind appends after the last kind on main when this merges (RepairTable in #267, LogCabin in #271 are ahead of it).
- Costs are placeholders for Ceramics and House building to bless: tub 6 sticks + 2 cordage + 1 log; block 1 log + 3 stones; trough 1 log + 4 sticks + 2 cordage.
- Batch size, time and skill gain go through Progression.Effect once Journal's shared/Progression.cs is on main (one placeholder function until then).

## Next step
Merge order set by the PM (2026-10-17): #285 goes LAST in its round, after ClayTrough 17 (#417), HenCoop 18 (#479) and StoneHearth 19 (#518). Then: git merge origin/main, renumber the three device kinds to 20, 21, 22 (StructuresCrafting.cs `Kind =` and their positions in StructureKind in server/spacetimedb/Building.cs: append after the others), rebuild, run tools/CraftCheck and tools/upgrade-check.sh origin/main, push, send the PM the green sha. (SpacetimeDB enums cannot carry explicit values, so an id is the enum position.) Do not merge as 17-19.

## Update 2026-10-17: refreshed on main
- Merged origin/main (ClayTrough is kind 17 on main). The three devices are provisionally kinds 18, 19, 20 so the enum order matches. PM: HenCoop 18, StoneHearth 19 and the sundial 20 land first, so the final numbers are 21, 22, 23. At that merge: re-merge main, move `Kind =` in `shared/structures/StructuresCrafting.cs` and the positions in `StructureKind` (`Building.cs`) to follow the last kind on main, keep `Category = "Crafting"`, rerun CraftCheck and `tools/upgrade-check.sh origin/main`.
- Local: build, CraftCheck (incl. the settling-tub case) and the upgrade check pass.

## Note 2026-10-17: subscriptions moved (#527)
Client subscription queries no longer go in `Subscriptions.cs`. When main is next merged into this branch, move `device_job` and `device_output` out of that list into a `[SubscriptionQueries] static IEnumerable<string> CraftingQueries(string range, string farRange)` in `client/scripts/net/Subscriptions.Crafting.cs` (see docs/CONTENT.md).

## Note 2026-10-17: showcase views moved (#679)
New showcase views go in `client/scripts/models/Showcase.Crafting.cs` as `[ShowcaseView("name")]` methods, not in `Showcase.cs`'s list. If this branch adds device views to `Showcase.cs`, move them there when main is next merged (with the renumber to 21–23 and the `Subscriptions.Crafting.cs` move above).

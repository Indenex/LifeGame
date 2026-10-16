# crafting-23: Batch crafting at dedicated devices (D-195)
Lane: Crafting (Ceramics: clay processes; Modeling: device models; UX: the panel)
Owner: Crafting thread (Sonnet, D-159)
Status: in_review
Branch: crafting-devices
PR: (to open)

## Goal
Justin (D-195): cleaning clay, crushing stone, tempering and similar simple hand processes get dedicated devices you load up so you don't have to do them one at a time. A loaded device runs on its own, but unattended output takes a large quality penalty; if you stay and play the minigame you get quality and quantity bonuses, so playing is worth it. Doing it by hand, one at a time, stays exactly as it is. Package A picked: batch cap 10; unattended x0.6 quality, 1.5x hand time, 25% skill gain; attended up to x1.1 quality, +20% quantity, 0.8x time, full skill gain.

## Design
- Devices (first three): settling tub (clay.clean), crushing block (clay.grit, clay.grog; a hammer in the bag), tempering trough (clay.temper_grit, clay.temper_grog, clay.daub). Later: cordage frame (cord.twist), brick mould frame (clay.brick), Animals' hide frames.
- Each device is a StructureKind with a StationKind (claim only, like the tables). shared/Devices.cs holds the process lists and every number.
- LoadDevice(structureId, processId, count): takes inputs (bag, nearby storage, stash), makes a DeviceJob (one per device) that works in rounds of 5 units. WorkDevice(structureId, score) while a round runs: the player stands at the device after playing the minigame (Strike for the block, Shape for tub and trough); that round becomes attended. DeviceOutput rows hold what is made; CollectDevice moves it to the bag.
- Skill: unattended rounds report once per 4 units, attended once per unit (OnProcessPerformed).

## Progress log
- 2026-10-16: PM relayed Justin's D-195 and package A. Branch created off main.

- 2026-10-16: built and verified locally on crafting-devices: shared/Devices.cs, server/spacetimedb/Devices.cs (DeviceJob, DeviceTimer, DeviceOutput; LoadDevice, WorkDevice, FinishDeviceRound, CollectDevice), StationKind ClayTub/CrushBlock/TemperTrough and StructureKind SettlingTub/CrushingBlock/TemperingTrough with shapes and costs, client DeviceUse.cs (Use key loads, works, collects) and CraftingUi.Devices.cs (plays the minigame for a device), subscriptions. CraftCheck builds a tub, runs one unit unattended (quality 30) and five attended with a perfect score (6 clean clay at 55), and checks the refusals. Upgrade check passes.

## Open questions / blockers
- Quality on flat-quality materials: clean clay, grit, grog, tempered clay and daub have no quality of their own (they always come out of the hands at 50), so the device's penalty and bonus are measured against 50 and carry into the pots made from them (unattended 30, attended up to 55) but no grade shows on the item. Making them show a grade (HasQuality) would also change what hand-crafting gives new players (the rank-0 skill cap is 35, below today's flat 50), so it needs Ceramics and the PM first. Asked the PM.
- Unattended skill gain is reported once per four units; Journal's hook takes no weight.
- A device taken down mid-batch hands the round's output straight to its owner; output already waiting in a removed device is orphaned (no cleanup yet).
- Output stays in the device until collected; auto-spilling into nearby storage is not built.
- Kinds: StructureKind appends after the last kind on main when this merges (RepairTable in #267, LogCabin in #271 are ahead of it).
- Costs are placeholders for Ceramics and House building to bless: tub 6 sticks + 2 cordage + 1 log; block 1 log + 3 stones; trough 1 log + 4 sticks + 2 cordage.
- Batch size, time and skill gain go through Progression.Effect once Journal's shared/Progression.cs is on main (one placeholder function until then).

## Next step
Open the PR, send the PM the green sha, tell Ceramics (session_01Sc3N1RCsurSszfX5YwGzMU), Modeling (models for SettlingTub, CrushingBlock, TemperingTrough; shapes in shared/Structures.cs) and UX (a panel to choose process and count; today the Use key loads a full batch of the first thing the device can make).

# ceramics-6: Storage jar
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: in_progress
Branch: claude/clay-ceramics-kfr4kr
PR: (none yet)

## Goal
A big fired-clay storage jar, the reward for building a kiln. Done when:
1. Crafting panel has "Build a storage jar" (process `clay.storage_jar`): 4 tempered clay, the coil minigame, sets a wet jar down to dry like a coil pot (ClayForm.StorageJar).
2. A dry jar picked up is an "Unfired storage jar" (Item 113). It is too big for a campfire: G at a fire says to fire it in a kiln. The kiln takes it like a pot (one load slot) and gives a "Storage jar" (Item 114), quality +10.
3. B menu has "Storage jar" (StructureKind.StorageJar, appended after BrickHouse): one stage "Set the jar" that takes 1 Storage jar. Finished, it opens like the basket (G "Open the jar"), holds 8 stacks, weathers only after 14 days.
4. Client draws the wet, dry and fired jar (PotMesh profile), the item icon, and the structure (JarModel in client/scripts/ceramics/, one-line hook in StructureModel).
5. ClayCheck --jar runs the chain on a scratch db: shape, dry, kiln fire, place, put and take a stack.
Justin sees screenshots via the Render lane: wet jar drying, the fired jar, the placed jar opened.

Numbers (sent to the PM as defaults, not yet Justin's): 4 tempered clay, kiln only, 8 stacks, weathers after 336 h.

## Progress log
- 2026-10-16: ceramics-2/3/5 closed (merged as #44); kiln blueprint (D-035) was already shipped there. Asked the PM for the next milestone with 4 options; started on the recommended one, this ticket. Toolchain (dotnet 8, spacetimedb-cli, Godot 4.6.2 mono) being installed in the container.

## Open questions / blockers
- PM: next milestone = storage jar? (sent 2026-10-16 with options cooking pot, roof tiles, polish). Working on the jar meanwhile.
- PM/Justin: jar numbers above. I first told the PM "pit firing cracks it 60%"; went with "kiln only" instead (clearer to a player, reason to build the kiln).
- Building lane: new StructureKind.StorageJar appended in shared/Structures.cs and a cost row in Building.cs StageCosts (small hooks, same as the kiln). Tell them when the PR opens.

## Next step
Implement items 1-3 server side (shared/Items.cs, Processes.cs, ClayRules.cs, Ceramics.cs, Kiln.cs, Structures.cs, Building.cs cost row) and build the server module.

# ceramics-6: Storage jar
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: in_review
Branch: claude/clay-ceramics-kfr4kr
PR: #105

## Goal
A big fired-clay storage jar, the reward for building a kiln. Done when:
1. Crafting panel has "Build a storage jar" (process `clay.storage_jar`): 4 tempered clay, the coil minigame, sets a wet jar down to dry like a coil pot (ClayForm.StorageJar).
2. A dry jar picked up is an "Unfired storage jar" (Item 113). It is too big for a campfire: G at a fire says to fire it in a kiln. The kiln takes it like a pot (one load slot) and gives a "Storage jar" (Item 114), quality +10.
3. B menu has "Storage jar" (StructureKind.StorageJar, appended after BrickHouse): one stage "Set the jar" that takes 1 Storage jar. Finished, it opens like the basket (G "Open the jar"), holds 4 stacks at low quality rising to 8 at high (D-093: first 15% of quality gives 40% of the gain; q10 5, q15 6, q50 7, q90 8), weathers only after 14 days.
4. Client draws the wet, dry and fired jar (PotMesh profile), the item icon, and the structure (JarModel in client/scripts/ceramics/, one-line hook in StructureModel).
5. ClayCheck --jar runs the chain on a scratch db: shape, dry, kiln fire, place, put and take a stack.
Justin sees screenshots via the Render lane: wet jar drying, the fired jar, the placed jar opened.

Numbers: Justin approved the storage jar with the lane's numbers (D-066, relayed by the PM 2026-10-16 17:11). As built: 4 tempered clay, kiln only (D-076), 4 to 8 stacks by jar quality (D-093), weathers after 336 h.

## Progress log
- 2026-10-16: ceramics-2/3/5 closed (merged as #44); kiln blueprint (D-035) was already shipped there. Asked the PM for the next milestone with 4 options; started on the recommended one, this ticket. Toolchain (dotnet 8, spacetimedb-cli, Godot 4.6.2 mono) being installed in the container.
- 2026-10-16: items 1-4 done in cf5cc32; server, client build. Bindings: tools/gen-bindings.sh was refused by the sandbox (its rm -rf); ran its generate line directly on a fresh clone.
- 2026-10-16: ClayCheck --jar added (implies --kiln); --ceramics-demo=jar row (wet, leather-hard, dry, kiln-fired jar, coil pot for scale). Running ClayCheck --jar on scratch db c6jar1.
- 2026-10-16 17:11: PM: Justin approved ceramics-6 with these numbers (D-066). Asked the PM to confirm kiln-only vs the 60% pit crack first mentioned. Upgrade check vs origin/main passed locally (no wipe).
- 2026-10-16 18:05: D-093: Structures.JarStacks(quality) / StoreStacksFor; BuildingStorage uses it (StructureProgress.Quality); jar panel shows "n of room" (client subscribes structure_progress via BuildingSystem.Queries). Server, client, ClayCheck build. ClayCheck --jar rerun on c6jar3.
- 2026-10-16 17:57: CI run https://github.com/Indenex/LifeGame/actions/runs/38941856742 GREEN on f9c7f56 (server + load test, Linux and Windows clients). Green sha sent to the PM.
- 2026-10-16 17:45: PR #105 opened; Render requested e9544db (demo=jar row, placed jar opened). Waiting on CI.
- 2026-10-16 17:41: ClayCheck --jar on c6jar2 PASSED end to end (campfire refuses jar, kiln fires 3 whole, jar q60 set down, stores and gives back a stick, 6 stacks). Merged main bdeaf3b; server, client, ClayCheck build.
- 2026-10-16 17:26: ClayCheck --jar on c6jar1 passed through jar-placed (campfire refused the jar, kiln fired 3 whole, jar q60 set down); failed at the store step only because ClayCheck didn't subscribe to basket_item. Fixed; rerunning on c6jar2.
- 2026-10-16 17:25: D-076 kiln only confirmed. D-075 (start low): JarStacks 8 -> 6, options sent to the PM.

## Open questions / blockers
- Kiln only: Justin confirmed (D-076).
- Jar capacity: Justin chose 4 rising to 8 with quality (D-093); done.
- Building lane: new StructureKind.StorageJar appended in shared/Structures.cs and a cost row in Building.cs StageCosts (small hooks, same as the kiln). Messaged House building (session_01RrKHUxWRH973Ndnxsad9nv) 2026-10-16; they OK'd the hooks and take StructureKind 7+. When main has #61 (stage costs move to `Structures.StageCosts` in shared/Structures.cs), move the jar's row there on the main merge and drop it from Building.cs.

## Next step
Wait for the PM to merge PR #105 (green on f9c7f56; later head commits are ticket-only). If main gets Building's #61 first, merge main and move the jar's StageCosts row into Structures.StageCosts. Then ask the PM for the next ceramics milestone.
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
3. B menu has "Storage jar" (StructureKind.StorageJar, appended after BrickHouse): one stage "Set the jar" that takes 1 Storage jar. Finished, it opens like the basket (G "Open the jar"), holds 6 stacks (D-075; was 8), weathers only after 14 days.
4. Client draws the wet, dry and fired jar (PotMesh profile), the item icon, and the structure (JarModel in client/scripts/ceramics/, one-line hook in StructureModel).
5. ClayCheck --jar runs the chain on a scratch db: shape, dry, kiln fire, place, put and take a stack.
Justin sees screenshots via the Render lane: wet jar drying, the fired jar, the placed jar opened.

Numbers: Justin approved the storage jar with the lane's numbers (D-066, relayed by the PM 2026-10-16 17:11). As built: 4 tempered clay, kiln only (D-076), 6 stacks (lowered from 8 for D-075, PM asked), weathers after 336 h.

## Progress log
- 2026-10-16: ceramics-2/3/5 closed (merged as #44); kiln blueprint (D-035) was already shipped there. Asked the PM for the next milestone with 4 options; started on the recommended one, this ticket. Toolchain (dotnet 8, spacetimedb-cli, Godot 4.6.2 mono) being installed in the container.
- 2026-10-16: items 1-4 done in cf5cc32; server, client build. Bindings: tools/gen-bindings.sh was refused by the sandbox (its rm -rf); ran its generate line directly on a fresh clone.
- 2026-10-16: ClayCheck --jar added (implies --kiln); --ceramics-demo=jar row (wet, leather-hard, dry, kiln-fired jar, coil pot for scale). Running ClayCheck --jar on scratch db c6jar1.
- 2026-10-16 17:11: PM: Justin approved ceramics-6 with these numbers (D-066). Asked the PM to confirm kiln-only vs the 60% pit crack first mentioned. Upgrade check vs origin/main passed locally (no wipe).
- 2026-10-16 17:25: D-076 kiln only confirmed. D-075 (start low): JarStacks 8 -> 6, options sent to the PM.

## Open questions / blockers
- Kiln only: Justin confirmed (D-076).
- PM: jar capacity under D-075. Proposed 6 flat (recommended, now in code) / 4 rising to 8 with quality / keep 8. Waiting on the answer.
- Building lane: new StructureKind.StorageJar appended in shared/Structures.cs and a cost row in Building.cs StageCosts (small hooks, same as the kiln). Messaged House building (session_01RrKHUxWRH973Ndnxsad9nv) 2026-10-16; they OK'd the hooks and take StructureKind 7+. When main has #61 (stage costs move to `Structures.StageCosts` in shared/Structures.cs), move the jar's row there on the main merge and drop it from Building.cs.

## Next step
Check the ClayCheck --jar result on c6jar1 (local server on 127.0.0.1:3000; rerun `cd tools/ClayCheck && dotnet run -c Release -- http://127.0.0.1:3000 <scratch db> --jar` after publishing to a new scratch db if the session restarted), then send the Render lane a request and open the PR.

# building-8: Stick fence and gate
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_progress
Branch: claude/house-building-8ueqz2 (pushed after #61 merges)
PR: (not yet)

## Goal
Justin picked the fence first (D-087; then a raised hearth, then a log cabin). From the B build menu (Tab to cycle) a player lays out a 2 m stick fence or a gate. A fence goes up in two stages: posts, then lashed rails. A new length laid near a free end of another fence snaps on, end to end, along its own heading, so T turns corners. A finished gate hangs a woven hurdle, shut. G opens and shuts it for anyone, and Shift+G bars it for whoever laid it out or the claim's members, the same as a hut door. Players can't walk through a finished fence or a shut gate. Sheep stop at fences, shut gates, and hut and house walls; open doorways let them through. That last part needs one line from Animals, calling `StructureBlocksStep`.

Default costs, starting low per D-075 (sent to the PM): fence posts 2 sticks, rails 2 sticks + 1 fiber; gate posts 2 sticks, hurdle 3 sticks + 2 fiber.

## Progress log
- 2026-10-16 18:20: built locally on top of #61's green head fb3e604.
  - Shared: `Structures.cs` gains Fence/Gate shapes and costs, `FenceEnds`, `StepCrossesFence`, `StepCrossesWalls`, and hut/house wall constants. Fences may sit 0.5 m apart, so runs join and corners work.
  - Server: `BuildingFence.cs` with `FinishGate`, which hangs a shut StructureDoor owned by the layer-out, and `StructureBlocksStep`. A closed gate gives no warmth (`ShelterAt` checks `HasDoorway`). "The gate is barred" message.
  - Client: `StructureModel.Fence.cs` (posts, rails, lashings; gate hinges); the gate hurdle reuses the door through `StructureModel.GateDoor`; fence collision `FenceLine`; snapping `SnapFence`; G and Shift+G on gates. The client's `ShelterAt` now ignores things without rain cover, so fences, kilns and markers no longer count as shelter.
  - BuildTest `fence` mode (also in "both"): builds a fence and a gate together, then opens, shuts and bars the gate. It passed locally in 27 s. A scratch geometry check of the crossing helpers passed.
- StructureKind order: Ceramics' storage jar (#105) takes 6. Fence and Gate are appended after it, so whichever merges second, the enum, `Shapes` and `StageCosts` must all keep the same order. Code uses `Structures.IsFence`/`IsGate`, never kind numbers.

- 2026-10-16 18:25: StructureStepHook implemented. Tested on a scratch merge with #115: builds, BuildTest fence passes twice, no server errors with sheep ticking. Fixed a BuildTest flake: builders stood 1.9 m off a 0.3 m-thin fence, and a walk can stop 1 m short, which left them out of reach. They now stand 0.9 m off (70f5850). Backed up on claude/house-building-8ueqz2-fence.
- Sent the PM the cost options and the D-075 shelter options (see building-7).

## Open questions / blockers
- Animals agreed and declared `StructureStepHook(ctx, fromX, fromZ, toX, toZ, ref bool blocked)` in AnimalFires.cs (PR #115, claude/animals-fence-5gkntn 22c0a61). BuildingFence.cs implements it (8ebc64b). A partial implementation without its declaration won't compile, so this PR must merge after #115 (merge main once #115 is in). Animals will live-test sheep at a fence.
- PM: fence and gate costs (defaults above), to confirm with Justin.

## Next step
Once #61 and #115 are merged, on claude/house-building-8ueqz2 merge main and claude/house-building-8ueqz2-fence, push, run the full local checks and open the building-8 PR. Then send a render request for a fenced yard with a gate, open and shut.

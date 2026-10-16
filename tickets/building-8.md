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

## Open questions / blockers
- Animals (session_01SAcVBUUfPFFk1rdXuiQZRU): asked them to add `if (StructureBlocksStep(ctx, fromX, fromZ, toX, toZ)) return true;` in AnimalFires.cs `AnimalStepBlocked`. Waiting for a reply.
- PM: fence and gate costs (defaults above), to confirm with Justin.

## Next step
Once #61 is merged, merge main, push, run the full local checks and open the building-8 PR. Then send a render request for a fenced yard with a gate, open and shut.

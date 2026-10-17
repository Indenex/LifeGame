# building-22: Wooden chest
Lane: House building
Owner: House building thread (Sonnet)
Status: in_progress
Branch: claude/house-building-8ueqz2-next
PR: (see below)

## Goal
The next House-building structure, picked from the options sent to the PM (the PM decides on the recommended one overnight, D-323): a wooden chest. A lidded box of split boards that stores like a basket but holds twice as much (24 stacks vs 12, one raisable constant `Structures.ChestStacks`, D-197), built inside a claim in two stages: Frame (3 logs + 1 cordage) and Boards and lid (3 logs + 1 cordage). Pure data on the shared storage base (D-206): a Def entry plus a model; no new server code, since any kind with stacks opens like the basket.

## Progress log
- 2026-10-17: Def (Kind 21 provisional), `ChestStacks`, StageProcess rule (framing), `StructureModel.Chest.cs` (posts and floor boards, side boards, lid with battens), Showcase view `chest` in `Showcase.House.cs`, BuildTest part `chest` (two players chop logs, build it, store and take; passes locally in 195 s, run alone like log-cabin), upgrade check passes locally, client builds (0 errors).
- Kind number: 21 now (the enum position after Sundial = 20). Crafting's devices are fixed at 21 to 23 and the chest at 24, so renumber when the devices land (the startup check catches a mismatch).

## Open questions / blockers
- PM: confirm the options (chest recommended; smoking rack, straw bed, well, log palisade are the others) and the claim-only default.
- CI's "Building check" matrix runs only lean-to, basket and fence, so the coop, stone hearth, sundial (and town path) and chest checks run only locally; Integration owns the workflow.

## Next step
Wait for CI on the chest PR, then send the green sha to the PM; ask Render B for a chest still.

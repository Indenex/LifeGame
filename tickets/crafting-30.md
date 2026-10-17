# crafting-30: Shared fire-ground check
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-fireground (harness branch claude/...; see PR)
PR: (opened below)

## Goal
The fire-ground rules (level, above water, clear of boulders and trunks) live in `shared/FireGround.cs` as `FireGround.Clear(x, z, depthAt, cleared?)`. The server's `Hearths.ClearForFire` calls it and keeps fire/hearth spacing; a building clearing a cell is passed as `cleared`. CraftCheck picks fire spots with the same check and lights with `StartFireAt`, so it no longer lights on boulders (Integration's #569 census found 7 such failures). UX can use the same call to tint the placement ghost amber.

## Progress log
- 2026-10-17: FireGround added, server and CraftCheck use it; builds pass. CI pending.

## Open questions / blockers
- Local CraftCheck run is blocked by the bot-walk bug (bugfix-34); relying on CI.

## Next step
None: merged (#596). Later sections below are history.

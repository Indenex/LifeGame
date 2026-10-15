# crafting-1: Crafting core, first playable
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-core
PR: https://github.com/Indenex/LifeGame/pull/4

## Goal
Data-driven materials and processes (materials with properties and state, timed processes stored as start time plus duration, quality from choices) and tiers 0 to 2: gathering, hand-drill fire with tinder and fuel, knapping, cordage and lashing, with key-step minigames only. Done when a player can gather, light a fire and knap a tool in a Linux or Windows build, and Justin has seen screenshots.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15: shared/Items.cs and shared/Processes.cs (items, processes, quality); server/spacetimedb/Crafting.cs (CraftJob + scheduled finish, Fire table, FeedFire, WarmthAt, skills hooks OnProcessPerformed / QualityCapFor / DurationFor); client crafting panel, drill and strike minigames, campfire visuals. ItemKind replaced by numeric item ids + quality (breaking: republish with --delete-data).
- 2026-10-15: tools/CraftCheck bot runs every process end to end; passes locally and runs in CI. PR #4 first CI run green (run 38806744447).
- 2026-10-15: merged main; added DurationFor speed hook, item id range comment, dropped macOS from CI; crafting panel grouped into tier tabs (Tab cycles). Screenshots 07-campfire.png and 08-crafting-panel-tabs.png sent to Justin.
- 2026-10-15: fixed HUD crash on duplicate inventory labels. Merged Survival (#5): berries are item 160, Eat takes an item id, crafted fires feed survival warmth via FireWarmthAt (verified near_heat on a live server). Key clash resolved: C craft, V eat, G forage, T feed fire. CraftCheck and the 20-bot load test pass on a wiped server.

## Open questions / blockers
- Crafting panel layout (tier tabs on the right, Tab cycles, number keys within a tab): sent to the PM for Justin, default is what's in the PR.
- World wipe on merge (items changed format): PM is bundling it with the merge request.
- Integration after merges: FireWarmthAt (Survival PR #5) and RequireClaimAccess (Claims PR #8) get wired by whichever PR merges second.

## Next step
Wait for CI on the latest push to PR #4; when green, ask the PM for the merge.

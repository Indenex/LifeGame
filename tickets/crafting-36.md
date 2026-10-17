# crafting-36: Believable minigame scores and abandoned device rounds (D-182)
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-trust
PR: (see below)

## Goal
The PM's answers to the crafting-35 audit: fix gap 2 now and build option (a) for gap 1, leniently (D-081: nudge, never punish).
- **Score plausibility** (`server/spacetimedb/CraftScore.cs`): the client tells the server when a minigame opens (`BeginMinigame(processId)`, called from `CraftingUi.Begin` and `PlayDeviceGame`). `StartCraft`, `StartFireAt` and `WorkDevice` run the reported score through `TrustScore`: a score with no opened minigame, or arriving sooner than a person could play it (`MinigamePlay.MinSeconds`, shared; 1.2 to 2 s), counts at most 0.6; a run of more than 4 perfect scores (>= 0.97) in a row settles toward 0.92 (cap = 0.92 + 0.08 * 0.8^(n-4)); a Master who skips the minigame (sends `Skills.MasterScore`) is taken at it. Honest play is untouched; nobody is refused. Tables: `MinigameStart`, `ScoreStreak` (private).
- **Abandoned device round** (`CancelJob.cs` `CheckDeviceReach`, `DeviceJob.WorkerId`): a worked (attended) round holds only while the worker stays within reach; leaving puts the rest of the round back on the unattended pace and score.
- CraftCheck: opens and plays the minigame before reporting a score (`PlayMinigame`), with a new case: a perfect score with no play behind it comes out lower than a played one. Also fixed a latent flake: the felling check now looks for any felled tree near the bot (the server chops the nearest tree in reach, not necessarily the one walked to). ClayCheck and BuildTest play their minigames the same way.

## Progress log
- 2026-10-17: built; upgrade check and CraftCheck pass locally (the capped case: quality 43 vs 45 played).

## Open questions / blockers
- Numbers (MinSeconds, 0.6 quick cap, streak 4 / floor 0.92 / decay 0.8) are lenient placeholders; tune if play shows honest players hit them.
- UX/Animals/Ceramics minigames all go through `CraftingUi.Begin`, so they call `BeginMinigame` already. A new minigame entry point must too.

## Next step
Wait for CI, send the green sha to the PM.

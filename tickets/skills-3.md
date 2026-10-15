# skills-3: Mastery takes the tedium out of hands-on crafting
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg
PR: (none yet)

## Goal
Skill already makes jobs faster and raises the best quality. This ticket makes the hands-on minigames shrink as you improve, so a practised crafter isn't asked to prove it every time: fewer strikes when knapping (3 down to 1), fewer coils on a coil pot (5 down to 1 just below Master), a hand drill that catches in fewer strokes (about 14 down to 4). At Master a craft is one action: the minigame is skipped and scored as a steady hand (`Skills.MasterScore`). Done when a rank 0 player sees the full minigame, a rank 50 player a shorter one, and a Master none, with the journal showing "Hands-on: 2 strikes" style hints on each discovery.

## How it fits together
- `shared/Skills.cs`: `MinigameSteps(baseSteps, rank)` already exists; add `MasterScore` and `SkipsMinigame(rank)`.
- `client/scripts/journal/Journal.cs`: a static `Journal.RankFor(processId)` from the player's `my_skills` rows.
- Crafting's `client/scripts/crafting/CraftingUi.cs` (Crafting's file) needs two small partial hooks, which this lane implements in `client/scripts/journal/CraftingUi.Skills.cs`:
  - `static partial void SkipMinigame(ProcessDef def, ref float? score);` called at the top of `Begin` before a minigame is built; a non-null score sends `StartCraft` straight away.
  - `static partial void StepsFor(ProcessDef def, ref int steps);` used by StrikeGame (3 strikes), DrillGame (about 14 strokes: heat per stroke becomes 1.05/steps) and Ceramics' CoilGame (5 coils) instead of their constants, with the hint text counting the real number.
- The server already trusts the client score (Crafting's design), so no server change.

## Progress log
- 2026-10-15 22:35: ticket written. Hook shapes proposed to the PM for the Crafting and Ceramics lanes.

## Open questions / blockers
- Crafting lane: add the two partial hooks to `CraftingUi.cs` (or OK this lane adding them as a small hook). Asked through the PM 2026-10-15 22:35.
- Design (Justin, via the PM): does a Master skip the minigame entirely (default), or get a choice to play it for a better result? Default MasterScore 0.8.

## Next step
Write `shared/Skills.cs` MasterScore/SkipsMinigame, `Journal.RankFor` and `client/scripts/journal/CraftingUi.Skills.cs`, then add the hook calls once Crafting agrees.

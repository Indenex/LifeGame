# swim-2: Swimming rank in the wading speed
Lane: Bug fixes (client hook; Journal and skills owns the skill and its numbers)
Owner: Bug fixes thread
Status: in_review
Branch: claude/swim-2
PR: (to open)

## Goal
Justin's Swimming skill (#449) eases wading and floating slow-down with rank (`Wading.SpeedFactor(depth, swimmingRank)`). The client now passes the player's rank, so the effect shows.

## Change
- `client/scripts/movement/Main.Swim.cs`: `SwimmingRank()` reads the "swimming" row of `MySkills` (0 if none).
- `client/scripts/Main.cs` (Walk): the one existing `Wading.SpeedFactor` call gets `SwimmingRank()`.
- Server: nothing to change. It never enforces the wading slow-down (the move budget is a maximum), so there is no rank to give it.

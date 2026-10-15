# skills-3: Mastery takes the tedium out of hands-on crafting
Lane: Journal and skills
Owner: Journal and skills thread
Status: done
Branch: claude/journal-skills-iseewg
PR: #22 (merged 97821e5)

## Goal
Skill already makes jobs faster and raises the best quality. This ticket makes the hands-on minigames shrink as you improve, so a practised crafter isn't asked to prove it every time: fewer strikes when knapping (3 down to 1), fewer coils on a coil pot (5 down to 1 just below Master), a hand drill that catches in fewer strokes (about 14 down to 4). At Master a craft is one action: the minigame is skipped and scored as a steady hand (`Skills.MasterScore`). Done when a rank 0 player sees the full minigame, a rank 50 player a shorter one, and a Master none,. Per D-047 a Master who holds the `craft_play_minigame` action (placeholder Ctrl; UX/UI sets the real key) while starting a craft plays the shortest version instead, for a chance at a better result.

## How it fits together
- `shared/Skills.cs`: `MinigameSteps(baseSteps, rank)` already exists; add `MasterScore` and `SkipsMinigame(rank)`.
- `client/scripts/journal/Journal.cs`: a static `Journal.RankFor(processId)` from the player's `my_skills` rows.
- Two small partial hooks (PM: this lane adds them to Crafting's `CraftingUi.cs` and Ceramics' `CraftingUi.Clay.cs`), implemented in `client/scripts/journal/CraftingUi.Skills.cs`:
  - `static partial void SkipMinigame(ProcessDef def, ref float? score);` called at the top of `Begin` before a minigame is built; a non-null score sends `StartCraft` straight away.
  - `static partial void StepsFor(ProcessDef def, ref int steps);` used by StrikeGame (3 strikes), DrillGame (about 14 strokes: heat per stroke becomes 1.05/steps) and Ceramics' CoilGame (5 coils) instead of their constants, with the hint text counting the real number.
- The server already trusts the client score (Crafting's design), so no server change.

## Progress log
- 2026-10-15 22:35: ticket written. Hook shapes proposed to the PM for the Crafting and Ceramics lanes.
- 2026-10-15 22:36: PM: add the hooks myself and tell Crafting and Ceramics the exact lines. Justin chose "Skip, can play" (D-047): Master skips with 0.8, holding a key plays the shortest version.
- 2026-10-15 22:40: hooks in Begin, DrillGame (stroke = 1.05/Steps(14)), StrikeGame (Steps(3)) and CoilGame (Steps(5)); hints count the real number. Client and server build locally. Steps at rank 0/50/99: strikes 3/2/1, coils 5/3/1, strokes 14/9/4. Committed locally; pushing once #19 merges so its CI isn't restarted.
- 2026-10-15 22:45: PM: don't wait for the key; use a named action. Merged into the branch behind PR #22 and pushed.

## Open questions / blockers
- Which key plays the minigame at Master: UX/UI (owns KEYMAP.md) to bind `craft_play_minigame`; placeholder Ctrl, bound by Journal.cs only if the action doesn't exist yet. (Shift is sneak, D-026.)

## Follow-ups (not in this ticket)
- When Ceramics' PR lands: fold `GiveDugClay` (Skills.cs), Ceramics' `ClayRules.DugClayQuality` and Crafting's stick depth into one place: skill-based quality on normal levels, 80 on the stick's extra level (PM 2026-10-15 22:44).
- `cook.meat` (Crafting #20) already trains Firecraft through the `cook` prefix (D-043).

## Next step
Done: merged into main at 97821e5 on 2026-10-15 22:47. Follow-ups above go in the next skills ticket.
# STATUS

What is happening right now. A fresh session reads this, then `CLAUDE.md`, and knows what to do.

The PM keeps this file current on `main`. Lanes keep their live progress in their own ticket on their own branch (only they edit it, so lanes never conflict); this table points to it.

**Phase:** 1, MVP. One forest region, crafting tiers 0 to 3, house building, sheep, up to 32 players, Linux and Windows.
**Goal:** playtesters reach a fired clay pot on their own and want to keep going.
**Priority:** playable as soon as possible. Small milestones merge early; `main` is the running playtest build.

## PM

| Item | State |
|---|---|
| Waiting on Justin | Merge of PR #3 (these handoff files) |
| Next merge | `crafting-core` once green; merging it wipes Justin's local world (`--delete-data`), so he decides with the merge |
| Next PM action | Merge PR #3 once Justin says yes and CI is green, then tell every lane to follow `WORKER_LOOP.md` |

## Lanes

| Lane | Ticket | Status | Branch | Owns |
|---|---|---|---|---|
| Crafting (integration owner) | `tickets/crafting-1.md` | in_progress | `crafting-core` | `shared/Items.cs`, `shared/Processes.cs`, `Crafting.cs`, `client/scripts/crafting/`, CI |
| Survival needs | `tickets/survival-1.md` | in_progress | lane to record | `Survival.cs`, `client/scripts/survival/` |
| Journal and skills | `tickets/skills-1.md` | in_progress | lane to record | `Knowledge.cs`, `Skills.cs`, `shared/Skills.cs`, `client/scripts/journal/` |
| Claims, chat and PvP rules | `tickets/claims-1.md` | in_progress | lane to record | `Claims.cs`, `Chat.cs`, `client/scripts/social/` |
| World atmosphere and sound | `tickets/atmosphere-1.md` | in_progress | lane to record | `Look.cs`, `WorldClock.cs`, `client/scripts/atmosphere/`, audio |
| House building | `tickets/building-1.md` | in_progress | lane to record | `Building.cs`, `client/scripts/building/` |
| Animals | `tickets/animals-1.md` | in_progress | lane to record | `Animals.cs`, `client/scripts/animals/` |
| Clay and ceramics | `tickets/ceramics-1.md` | in_progress | lane to record (from `crafting-core`) | `Ceramics.cs`, `client/scripts/ceramics/` |

Status values: `todo` · `in_progress` · `blocked` · `in_review` · `done`

## Interfaces between lanes

- Process ids: dotted strings in `shared/Processes.cs` (`gather.*`, `fire.*`, `knap.*`, `cord.*`, `lash.*`, `clay.*`, `build.*`).
- Skills hooks: partial methods on `Module` declared in `Crafting.cs` and implemented by Journal and skills: `OnProcessPerformed(ctx, playerId, processId, quality, failed)`, `QualityCapFor(ctx, playerId, processId, ref cap)`, and a `ref float seconds` speed hook. Quality is 1 to 100; players are `uint playerId`.
- Warmth: `Module.WarmthAt(ctx, x, z)` returns 0 to 1 from burning fires within 5 m; raw data in the public `fire` table.
- House building provides structure placement (Claims uses it for markers) and "is this player sheltered" (Survival reads it).

## Resume prompt

> You are resuming LifeGame work. Read `CLAUDE.md`, then this file. Find your lane's row, check out its branch, open its ticket and do its `Next step`. If you are the PM, do `Next PM action`. Questions for Justin go through the PM.

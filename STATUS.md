# STATUS

What is happening right now. A fresh session reads this, then `CLAUDE.md`, and knows what to do.

The PM keeps this file current on `main`. Lanes keep their live progress in their own ticket on their own branch (only they edit it, so lanes never conflict); this table points to it.

**Phase:** 1, MVP. One forest region, crafting tiers 0 to 3, house building, sheep, up to 32 players, Linux and Windows.
**Goal:** playtesters reach a fired clay pot on their own and want to keep going.
**Priority:** playable as soon as possible. Small milestones merge early; `main` is the running playtest build.

## PM

| Item | State |
|---|---|
| Waiting on Justin | nothing open |
| Next merge | `crafting-core` #4 (approved, world wipe); then skills #7 and claims #8 (both approved, merging main after #4); then ceramics #11, building-2 #10, survival-2 #12 |
| Next PM action | Record lane branch names as lanes report them; bring `crafting-core` merge plus world wipe to Justin when green |

## Lanes

| Lane | Ticket | Status | Branch | Owns |
|---|---|---|---|---|
| Crafting (integration owner) | `tickets/crafting-1.md` | in_progress | `crafting-core` | `shared/Items.cs`, `shared/Processes.cs`, `Crafting.cs`, `client/scripts/crafting/`, CI |
| Survival needs | `tickets/survival-2.md` | in_progress (draft PR #12, lands after #4 and #11) | `claude/survival-needs-g7yrhi` | `Survival.cs`, `client/scripts/survival/` |
| Journal and skills | `tickets/skills-1.md` | in_progress (PR #7, merges with or after crafting-core) | `claude/journal-skills-iseewg` | `Knowledge.cs`, `Skills.cs`, `shared/Skills.cs`, `client/scripts/journal/` |
| Claims, chat and PvP rules | `tickets/claims-1.md` | in_review (PR #8 green) | `claude/claims-chat-pvp-vk2qak` | `Claims.cs`, `Chat.cs`, `client/scripts/social/` |
| World atmosphere and sound | `tickets/atmosphere-2.md` (atmosphere-1 done, PR #9 merged a4ce058) | in_progress (sheep bleats, canopy fade, rain and dawn tweaks) | `claude/atmosphere-sound-hlqfke` | `Look.cs`, `WorldClock.cs`, `client/scripts/atmosphere/`, audio |
| House building | `tickets/building-2.md` (building-1 done, PR #6 merged bae255c) | in_progress (draft PR #10, lands after #4 and #11) | `claude/house-building-8ueqz2` | `Building.cs`, `client/scripts/building/` |
| Animals | `tickets/animals-1.md` | in_progress (sheep AI working, screenshots posted) | lane to record | `Animals.cs`, `client/scripts/animals/` |
| Clay and ceramics | `tickets/ceramics-1.md` | in_review (PR #11, CI running) | `claude/clay-ceramics-kfr4kr` (from `crafting-core`) | `Ceramics.cs`, `client/scripts/ceramics/` |

Status values: `todo` · `in_progress` · `blocked` · `in_review` · `done`

## Interfaces between lanes

- Process ids: dotted strings in `shared/Processes.cs` (`gather.*`, `fire.*`, `knap.*`, `cord.*`, `lash.*`, `clay.*`, `build.*`).
- Skills hooks: partial methods on `Module` declared in `Crafting.cs` and implemented by Journal and skills: `OnProcessPerformed(ctx, playerId, processId, quality, failed)`, `QualityCapFor(ctx, playerId, processId, ref cap)`, and a `ref float seconds` speed hook. Quality is 1 to 100; players are `uint playerId`.
- Warmth: `Module.WarmthAt(ctx, x, z)` returns 0 to 1 from burning fires within 5 m; raw data in the public `fire` table.
- Item id ranges per lane: Crafting 1-99, Ceramics 100-129, House building 130-159, Survival 160-189, Animals 190-219, Claims 220-239, Journal and skills 240-259.
- `Crafting.cs`: `Outcome.Placed` with `OnPlaced(ctx, job, def)` sets an object down at a position; minigames Shape and Coil.
- Fire warmth for Survival: Crafting implements `FireWarmthAt(ctx, x, z, ref warmth)` (0 to 1), declared in `Survival.cs`, by calling `WarmthAt`. Shelter: House building's `Module.ShelterAt(ctx, x, z)` / `ShelterFor(ctx, playerId)` return Shelter(RainCover, Warmth, StructureId); it implements Survival's `ShelterAtHook(ctx, x, z, ref rainCover, ref warmth)`.
- Keys: every key goes in `KEYMAP.md` (proposed, awaiting Justin).
- Placement: `PlaceStructure(kind, x, z, yaw)` and `shared/Structures.cs`; the claim marker is a new `StructureKind` with one stage.
- Weather (`WorldClock.cs`, PR #9): `TimeOfDay`, `CurrentWeather` (Cloud, Rain, Wind, Wetness 0 to 1), `DryingRate`, `AirTemperature` in °C. `RainAt` one-liner goes in whichever of #9 or ceramics merges second.
- Rain: Atmosphere implements `RainAt(ctx, x, z, ref rain)` (0 to 1), declared in `Ceramics.cs`.
- House building provides structure placement (Claims uses it for markers) and "is this player sheltered" (Survival reads it).

## Resume prompt

> You are resuming LifeGame work. Read `CLAUDE.md`, then this file. Find your lane's row, check out its branch, open its ticket and do its `Next step`. If you are the PM, do `Next PM action`. Questions for Justin go through the PM.

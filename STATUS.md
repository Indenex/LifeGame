# STATUS

What is happening right now. A fresh session reads this, then `CLAUDE.md`, and knows what to do.

The PM keeps this file current on `main`. Lanes keep their live progress in their own ticket on their own branch (only they edit it, so lanes never conflict); this table points to it.

**Phase:** 1, MVP. One forest region, crafting tiers 0 to 3, house building, sheep, up to 32 players, Linux and Windows.
**Goal:** playtesters reach a fired clay pot on their own and want to keep going.
**Priority:** playable as soon as possible. Small milestones merge early; `main` is the running playtest build.

## PM

| Item | State |
|---|---|
| Justin's world | Wiped onto main 5adb2c1 on 2026-10-15. From now on every PR must publish over it without `--delete-data` (CI upgrade check, #25). A wipe needs Justin's OK |
| Merged 2026-10-15 | #4, #6, #9, #11, #13, #14, #15, #7, #12, #17, #19, #22, #18, #20, #16, #24 |
| Next merges (send the PM the green sha) | building #10, claims #8, animals #23, ceramics #21 (after #20, folds dug-clay quality), UX #26, atmosphere #27, bugfix #25, skills #28 |
| Waiting on Justin | Water and sky look, #66 atmosphere-10/11: Render lane rendering it, then Justin decides. Still to ask: carry-limit numbers (survival-3, shipped as default); burnt mutton 10% hunger (survival-4); toast only at new titles (skills-8). Answered 2026-10-16: D-060 felling, D-061 mat, D-062 wetness. PM decides: Shift+G bars doors (building-3, UX/UI confirms) |
| Next PM action | All lanes restarted 2026-10-16 17:04 (session ids in the lane table; reach them with send_message). Get Justin's answers on the open questions above; Merge each PR above when its lane sends a green sha; post screenshots to Justin; record his answers in `DECISIONS.md` |

## Lanes

| Lane | Next ticket / PR | Branch | Owns |
|---|---|---|---|
| Integration (D-058) (session_01Rf5965nk4Lvd3f2QDteZSq) | integration-1 take over CI and builds | session_01Rf5965nk4Lvd3f2QDteZSq records its branch | `client/scripts/Main.cs`, `client/scripts/Hud.cs` hooks, `Tables.cs`, `shared/World.cs`, `.github/workflows/`, `tools/gen-bindings.sh`, Use dispatcher, PR merge-readiness |
| Render (D-059) (session_013qe8zPzzBHx9rFPEYoKMr5) | render-1 set up the capture pipeline | session_013qe8zPzzBHx9rFPEYoKMr5 records its branch | screenshots and clips for every lane, /mnt/project-files/screenshots/, capture tooling under `tools/render/` |
| Wiki (D-063, D-064: Opus) (session_01N5zRQUdvDgJjhehqzyPAjB) | wiki-2 done (#87); keep `wiki/` current | `claude/wiki-2` | `wiki/`. Web page (private, Justin shares it): https://claude.ai/artifact/4sKf1fGqBZEQeErZCkibaB; the PM republishes it after each wiki merge with `tools/wiki-page/` |
| Crafting (session_01Y2sA7pDBFch57bWb1uKMTq) | crafting-3 bow drill | `crafting-bow-drill` (lane records) | `shared/Items.cs`, `shared/Processes.cs`, `Crafting.cs`, `client/scripts/crafting/` |
| Survival needs (session_014pHdaxJ7uESzsvWekpjvsH) | carrying limit raised by basket (D-042); numbers to Justin first | `claude/survival-needs-g7yrhi` | `Survival.cs`, `client/scripts/survival/` |
| Journal and skills (session_013QuyGdXAWemhkois2ictmc) | #28 skills-5; then dug-clay fold-in after #21 | `claude/journal-skills-iseewg` | `Knowledge.cs`, `Skills.cs`, `shared/Skills.cs`, `client/scripts/journal/` |
| Claims, chat and PvP rules (session_013ECn821EbTpbj4h7d3PepH) | #8; then claims-2 (D-044) | `claude/claims-chat-pvp-vk2qak` | `Claims.cs`, `Chat.cs`, `client/scripts/social/` |
| World atmosphere and sound (session_01KvVF5vpkYEHTKNRpHC18WN) | #27 campfire sound; firelight at night | `claude/atmosphere-sound-hlqfke` | `Look.cs`, `WorldClock.cs`, `client/scripts/atmosphere/`, audio |
| House building (session_01RrKHUxWRH973Ndnxsad9nv) | building-4 mud-brick house (D-057); building-3 merged as #41 | `claude/house-building-8ueqz2` | `Building.cs`, `Weathering.cs`, `client/scripts/building/` |
| Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) | #23 shearing; then animals-3 kill and butcher (D-039) | `claude/animals-sheep-5gkntn` | `Animals.cs`, `client/scripts/animals/` |
| Clay and ceramics (session_01Diab34HTvew38VmUaqhj5Q) | #21 kiln and deep clay; then kiln blueprint | `claude/clay-ceramics-kfr4kr` | `Ceramics.cs`, `client/scripts/ceramics/` |
| UX and UI (session_01V6qgbNbvTwniqW946KACLr) | #26 quieter HUD (D-045) with F1 key help; right-drag camera (D-050); Strike and Master-play keys | `claude/ux-ui-xlrh2s` | `client/scripts/ui/`, input map, `KEYMAP.md`, Use key registry, `Hud.cs` layout |
| Bug fixes (session_01HddD17VpF2CmiCxHoPH84Y) | #25 upgrade check; bugfix-5 fresh-world playtest | lane records | any file, minimal diffs, PM assigns |

Status values: `todo` · `in_progress` · `blocked` · `in_review` · `done`

## Interfaces between lanes

- Process ids: dotted strings in `shared/Processes.cs` (`gather.*`, `fire.*`, `knap.*`, `cord.*`, `lash.*`, `clay.*`, `build.*`).
- Skills hooks: partial methods on `Module` declared in `Crafting.cs` and implemented by Journal and skills: `OnProcessPerformed(ctx, playerId, processId, quality, failed)`, `QualityCapFor(ctx, playerId, processId, ref cap)`, and a `ref float seconds` speed hook. Quality is 1 to 100; players are `uint playerId`.
- Warmth: `Module.WarmthAt(ctx, x, z)` returns 0 to 1 from burning fires within 5 m; raw data in the public `fire` table.
- Item id ranges per lane: Crafting 1-99, Ceramics 100-129, House building 130-159, Survival 160-189, Animals 190-219, Claims 220-239, Journal and skills 240-259.
- `Crafting.cs`: `Outcome.Placed` with `OnPlaced(ctx, job, def)` sets an object down at a position; minigames Shape and Coil.
- Fire warmth for Survival: Crafting implements `FireWarmthAt(ctx, x, z, ref warmth)` (0 to 1), declared in `Survival.cs`, by calling `WarmthAt`. Shelter: House building's `Module.ShelterAt(ctx, x, z)` / `ShelterFor(ctx, playerId)` return Shelter(RainCover, Warmth, StructureId); it implements Survival's `ShelterAtHook(ctx, x, z, ref rainCover, ref warmth)`.
- Keys: every key goes in `KEYMAP.md` (UX/UI owns it, D-033). World actions register Use handlers; never bind G.
- Client subscriptions: `client/scripts/net/Subscriptions.cs` (#18); add new chunk queries there.
- Placement: `PlaceStructure(kind, x, z, yaw)` and `shared/Structures.cs`; the claim marker is a new `StructureKind` with one stage.
- Weather (`WorldClock.cs`, PR #9): `TimeOfDay`, `CurrentWeather` (Cloud, Rain, Wind, Wetness 0 to 1), `DryingRate`, `AirTemperature` in °C. `RainAt` one-liner goes in whichever of #9 or ceramics merges second.
- Rain: Atmosphere implements `RainAt(ctx, x, z, ref rain)` (0 to 1), declared in `Ceramics.cs`.
- House building provides structure placement (Claims uses it for markers) and "is this player sheltered" (Survival reads it).

## Resume prompt

> You are resuming LifeGame work. Read `CLAUDE.md`, then this file. Find your lane's row, check out its branch, open its ticket and do its `Next step`. If you are the PM, do `Next PM action`. Questions for Justin go through the PM.

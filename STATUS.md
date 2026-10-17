# STATUS

What is happening right now. A fresh session reads this, then `CLAUDE.md`, and knows what to do.

The PM keeps this file current on `main`. Lanes keep their live progress in their own ticket on their own branch (only they edit it, so lanes never conflict); this table points to it.

**Phase:** 1, MVP. One forest region, crafting tiers 0 to 3, house building, sheep, up to 32 players, Linux and Windows.
**Goal:** playtesters reach a fired clay pot on their own and want to keep going.
**Milestones (D-081):** M1 Looks and feels real + M2 The first evening (in parallel) → M3 A place of your own → M4 The long road → M5 Pre-alpha playtest. Survival stays light.
**Priority:** playable as soon as possible. Small milestones merge early; `main` is the running playtest build.

## PM

| Item | State |
|---|---|
| Justin's world | Wiped onto main 5adb2c1 on 2026-10-15. From now on every PR must publish over it without `--delete-data` (CI upgrade check, #25). A wipe needs Justin's OK. One wipe planned with env-2 (D-129); wipe pre-approved for when env-2 is ready (D-143). |
| Justin's launcher | build-680-02153f2 installed; keep-forever launcher comes with #201's release |
| Merged 2026-10-15 | #4, #6, #9, #11, #13, #14, #15, #7, #12, #17, #19, #22, #18, #20, #16, #24 |
| Next merges (send the PM the green sha) | building #10, claims #8, animals #23, ceramics #21 (after #20, folds dug-clay quality), UX #26, atmosphere #27, bugfix #25, skills #28 |
| Waiting on Justin | Water and sky look, #66: Render lane rendering it, then Justin decides. Carry-limit numbers (survival-3) stay as shipped unless he says otherwise. Answered 2026-10-16: D-060 to D-062, D-065 to D-080 |
| Next PM action | All lanes restarted 2026-10-16 17:04 (session ids in the lane table; reach them with send_message). Get Justin's answers on the open questions above; Merge each PR above when its lane sends a green sha; post screenshots to Justin; record his answers in `DECISIONS.md` |

## Lanes

| Lane | Next ticket / PR | Branch | Owns |
|---|---|---|---|
| Integration (D-058) (session_015g3pLB5eJ4qPrXAwJUmkYT) | integration-1 take over CI and builds; in progress. Merge-readiness only: the PM merges (Integration's session can't merge); Sonnet since D-159, PR #209 | `claude/integration-lane` | `client/scripts/Main.cs`, `client/scripts/Hud.cs` hooks, `Tables.cs`, `shared/World.cs`, `.github/workflows/`, `tools/gen-bindings.sh`, Use dispatcher, PR merge-readiness |
| Render (D-059) (session_015S96hNrEu92FrCoNZqZZjs; old session_013qe8zPzzBHx9rFPEYoKMr5 retired) | render-1 capture pipeline (Sonnet, D-159) | `claude/render-1-013qe8` | screenshots and clips for every lane, /mnt/project-files/screenshots/, capture tooling under `tools/render/` |
| Wiki (D-063, D-064: Opus) (session_01N5zRQUdvDgJjhehqzyPAjB) | wiki-2 done (#87); keep `wiki/` current | `claude/wiki-2` | `wiki/`. Web page (private, Justin shares it): https://claude.ai/artifact/4sKf1fGqBZEQeErZCkibaB; the PM republishes it after each wiki merge with `tools/wiki-page/` |
| Modeling (D-073) (session_01JKX3FNjkrEkAqoTaFJiXAm) | model-1 player figure, items, clothes | `claude/model-1-style` | `client/assets/models/`, `client/scripts/models/`; may touch other lanes' mesh files in small PRs |
| Animation (D-073) (session_019roNRWQBPw4ozNLDhzseig) | anim-2 done; on anim-3 (tools in hand while crafting), PR #372 | `claude/anim-3` | `client/scripts/animation/`, `client/scripts/Avatar.cs` |
| Environment (D-078) (session_012hVejTxLoxBpZyX3GQmNL9) | next: ground cover and terrain matched to `docs/art-reference`, prototype first; radius 12 batching comes after; open: #344 and #371 | `claude/environment-env-4`; env-7 #238 on `claude/environment-meander` | `client/scripts/environment/`, `client/scripts/Terrain.cs` |
| Audio (D-079) (session_01SXcxtoyeqDmypiHxkHedU6) | audio-2a+b, then audio-3 (Sonnet, D-159) | session records its branch | `client/scripts/atmosphere/audio/`, `SoundScape.cs`, `tools/SoundPreview/`, music, `client/scripts/audio/` |
| Crafting (session_01N9APthjRDfjknUvCweqzzN) | #160, #203, crafting-19 durability (Sonnet, D-159) | `crafting-durability` (crafting-19) | `shared/Items.cs`, `shared/Processes.cs`, `Crafting.cs`, `client/scripts/crafting/` |
| Survival needs (session_01RedPo6NfcLBPNHQNVhC4qf) | survival-12 PR #206, then survival-10 (Sonnet, D-159) | `claude/survival-needs-g7yrhi` | `Survival.cs`, `client/scripts/survival/` |
| Journal and skills (session_013iTXrpndd1fMtDLqqBPgJ5) | skills-11; PR #193 (Sonnet, D-159) | `claude/journal-skills-iseewg` | `Knowledge.cs`, `Skills.cs`, `shared/Skills.cs`, `client/scripts/journal/` |
| Claims, chat and PvP rules (session_01CAk6p7Z42GfVwi8QMfNMhZ) | claims-12 fences; #191 and #196 merged (Sonnet, D-159) | `claude/claims-chat-pvp-vk2qak` | `Claims.cs`, `Chat.cs`, `client/scripts/social/` |
| World atmosphere (visuals; audio moved to Audio, D-079) (session_01KvVF5vpkYEHTKNRpHC18WN) | atmosphere-21 storms #286 and atmosphere-22 far-land fog #289 in CI; atmosphere-19 follow-up waits on House building #271/#272 | `claude/atmosphere-21-storms`, `claude/atmosphere-farland-fog` | `Look.cs`, `WorldClock.cs`, `shared/Weathers.cs`, `client/scripts/atmosphere/` except audio |
| House building (session_01DD9p6EhSc5pJ8CjNmDFTrG) | #202, building-10 (Sonnet, D-159) | `claude/house-building-8ueqz2` | `Building.cs`, `Weathering.cs`, `client/scripts/building/` |
| Animals (session_015wozcQSFB48cUjQ9p676zf) | animals-14 red deer #219; #195, #90, #149 (Sonnet, D-159) | `claude/animals-deer-5gkntn` | `Animals.cs`, `client/scripts/animals/` |
| Clay and ceramics (session_01Sc3N1RCsurSszfX5YwGzMU) | ceramics-7+8, lamp ceramics-9 (Sonnet, D-159) | `claude/clay-ceramics-kfr4kr` | `Ceramics.cs`, `client/scripts/ceramics/` |
| UX and UI (session_01V6qgbNbvTwniqW946KACLr) | #73 ui-8; ui-9 trade window (D-069) | `claude/ux-ui-xlrh2s` | `client/scripts/ui/`, input map, `KEYMAP.md`, Use key registry, `Hud.cs` layout |
| Bug fixes (session_01AeuMsF8LfzDsWW8jUh1Fkp) | #221, then bugfix-23 collision (Sonnet, D-159) | lane records | any file, minimal diffs, PM assigns |
| Scribe (D-102, Sonnet) (session_01He77u9z8pt5DtP7it3NMAQ) | records decisions for the PM | your branch | `DECISIONS.md`, `STATUS.md` (PM's instructions only) |
| Release & Launcher (D-131) (session_01Jxp6a8KwCCg4DsFyCZHC9t) | release-2; PR #201 (Sonnet, D-159) | `claude/release-1` | `launcher/`, `tools/release/`, `.github/workflows/release.yml` (reviewed by Integration) |

Status values: `todo` · `in_progress` · `blocked` · `in_review` · `done`

## Interfaces between lanes

- Process ids: dotted strings in `shared/Processes.cs` (`gather.*`, `fire.*`, `knap.*`, `cord.*`, `lash.*`, `clay.*`, `build.*`).
- Skills hooks: partial methods on `Module` declared in `Crafting.cs` and implemented by Journal and skills: `OnProcessPerformed(ctx, playerId, processId, quality, failed)`, `QualityCapFor(ctx, playerId, processId, ref cap)`, and a `ref float seconds` speed hook. Quality is 1 to 100; players are `uint playerId`.
- Warmth: `Module.WarmthAt(ctx, x, z)` returns 0 to 1 from burning fires within 5 m; raw data in the public `fire` table.
- Item id ranges per lane: Crafting 1-99, Ceramics 100-129, House building 130-159, Survival 160-189, Animals 190-219, Claims 220-239, Journal and skills 240-259.
- `Crafting.cs`: `Outcome.Placed` with `OnPlaced(ctx, job, def)` sets an object down at a position; minigames Shape and Coil.
- Fire warmth for Survival: Crafting implements `FireWarmthAt(ctx, x, z, ref warmth)` (0 to 1), declared in `Survival.cs`, by calling `WarmthAt`. Shelter: House building's `Module.ShelterAt(ctx, x, z)` / `ShelterFor(ctx, playerId)` return Shelter(RainCover, Warmth, StructureId); it implements Survival's `ShelterAtHook(ctx, x, z, ref rainCover, ref warmth)`.
- Keys: every key goes in `KEYMAP.md` (UX/UI owns it, D-033). World actions register Use handlers; never bind E (the Use key, D-188).
- Client subscriptions: `client/scripts/net/Subscriptions.cs` (#18); add new chunk queries there.
- Placement: `PlaceStructure(kind, x, z, yaw)` and `shared/Structures.cs`; the claim marker is a new `StructureKind` with one stage.
- Weather (`WorldClock.cs`, PR #9): `TimeOfDay`, `CurrentWeather` (Cloud, Rain, Wind, Wetness 0 to 1), `DryingRate`, `AirTemperature` in °C. `RainAt` one-liner goes in whichever of #9 or ceramics merges second.
- Rain: Atmosphere implements `RainAt(ctx, x, z, ref rain)` (0 to 1), declared in `Ceramics.cs`.
- House building provides structure placement (Claims uses it for markers) and "is this player sheltered" (Survival reads it).

## Resume prompt

> You are resuming LifeGame work. Read `CLAUDE.md`, then this file. Find your lane's row, check out its branch, open its ticket and do its `Next step`. If you are the PM, do `Next PM action`. Questions for Justin go through the PM.

# bugfix-53: Hidden values (audit item 7 of bugfix-24)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-53 (stage A, PlayerHealth family). Later families get their own branches/PRs (bugfix-54...).
PR: (see PM)

## Goal
A client must not receive values it has no reason to draw. Stage A moves hidden fields of one table family per PR out of public tables. Stage B (later) filters positions and animals by chunk. Stage C (world clock, weather, season) stays public.

Rules agreed with the PM (D-182):
- No world wipe. Every family must pass `tools/upgrade-check.sh origin/main`. Changing a table's access (public to private) migrates fine (checked for PlayerHealth); removing a column does not, so hidden columns on tables that must stay public are zeroed and a private table/view carries the value.
- Anything another player's client draws stays public. Maker names on things set down in the open stay public ("Made by" tooltips are a feature); only Quality and Wear move.
- Tell the owning lane before each PR and cc the PM. Keep their client reads working with the smallest change in their files.

## Stage A families (one PR each)
| Family | What other clients do not need | Plan | Owner lane |
|---|---|---|---|
| PlayerHealth (this PR) | Health, AsOf, LastHitAt, LastStrikeAt, Fed of others. Everyone does need GraceUntil (auto-aim skips newcomers) and MarkedUntil (killer's red name). | `PlayerHealth` becomes private (access change). New public `PlayerStanding` (PlayerId, GraceUntil, MarkedUntil) kept in step by `SaveHealth`. New view `my_health` for the caller's own row. Client: SocialLayer.Fighting.cs reads PlayerStanding and MyHealth; subscriptions in SocialLayer.cs. SocialTest reads victim's own `my_health`. | Survival |
| DeviceJob | Score, InputQuality and other round internals for devices of others | Private table plus per-owner view; keep Remaining/RoundStart/RoundEnd public if the device view draws progress | Crafting |
| Roast | Quality | Quality to a private table served to the roaster | Crafting |
| StructureProgress | Quality, StageQualitySum, StageItems, StageHelpers | Keep public only what the build bar draws; rest private | House building |
| BasketItem | Quality, Wear (Maker stays public) | Private table `BasketItemQuality` (item id -> quality, wear), served by a view to players who can open the basket | Ceramics / Crafting |
| ClayPiece | Quality | As BasketItem | Ceramics |

## Stage B plan (before outside players join; Integration owns the Subscriptions.cs rewrite)
Goal: a client receives positions and animals only for chunks in and near its view.
1. Add chunk-filtered views `near_player_pos(chunkX, chunkZ)`-style are not possible (views take no arguments). So: keep a private `ViewerChunk(PlayerId, ChunkX, ChunkZ)` row per player (updated by the server on Move when the chunk changes) and public views `nearby_player_pos`, `nearby_animal`, `nearby_world_item`... that return, for the caller, rows in the 5x5 chunks around the caller's chunk, read by the existing per-chunk btree index on each table.
2. Client reads change from `conn.Db.PlayerPos` / `Animal` to the views; this touches every lane that reads those tables, so it is one PR by Integration with all lanes listed.
3. Remove the raw public `player_pos` and `animal` tables from the public set last (access change; migrates).
4. Test: a bot far from another bot must not see its row; the load test must keep its traffic figure.
Cost to measure: view recomputation per Move; check LoadTest CPU per player before and after.

## Progress log
- 2026-10-17: PlayerHealth family done on this branch: server builds, bindings regenerate, client builds, SocialTest passes (strike lands, kill mark, full health on wake), Upgrade check passes (origin/main world carries over).

## Open questions / blockers
- Existing PlayerHealth rows get their PlayerStanding row the next time a strike touches them (EnsureHealth); until then a client treats them as not graced and not marked, which is true for almost everyone.

## Next step
Open the PR for the PlayerHealth family, wait for CI, send the PM the green sha, then start DeviceJob/Roast after telling Crafting.

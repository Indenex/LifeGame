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
| BasketItem | Whole contents of baskets in claims you don't belong to; Quality and Wear elsewhere | Moved to stage B (PM, 2026-10-17): no temporary public mirror or second migration. Stage B makes BasketItem private and serves it by view: baskets in the caller's own or member claims plus baskets in nearby chunks outside claims. Note StorageSounds.cs then only hears baskets the caller can see. | Ceramics / Crafting |
| ClayPiece | Quality | As BasketItem | Ceramics |

## Stage B plan (before outside players join; Integration owns the Subscriptions.cs rewrite)
Goal: a client receives positions and animals only for chunks in and near its view.
1. Add chunk-filtered views `near_player_pos(chunkX, chunkZ)`-style are not possible (views take no arguments). So: keep a private `ViewerChunk(PlayerId, ChunkX, ChunkZ)` row per player (updated by the server on Move when the chunk changes) and public views `nearby_player_pos`, `nearby_animal`, `nearby_world_item`... that return, for the caller, rows in the 5x5 chunks around the caller's chunk, read by the existing per-chunk btree index on each table.
2. Client reads change from `conn.Db.PlayerPos` / `Animal` to the views; this touches every lane that reads those tables, so it is one PR by Integration with all lanes listed.
3. Baskets: `nearby_basket_item` (same chunk rule, plus every basket in the caller's own or member claims) replaces public BasketItem; see the Stage A table.
4. Remove the raw public `player_pos` and `animal` tables from the public set last (access change; migrates).
5. Test: a bot far from another bot must not see its row; the load test must keep its traffic figure.
Cost to measure: view recomputation per Move; check LoadTest CPU per player before and after.

## Progress log
- 2026-10-17: PlayerHealth family done on this branch: server builds, bindings regenerate, client builds, SocialTest passes (strike lands, kill mark, full health on wake), Upgrade check passes (origin/main world carries over).

- 2026-10-17: families 2 (#830, bugfix-54), 3 (bugfix-56 clay and kiln) and 4 (bugfix-57 structure stage) written; BasketItem moved to stage B on the PM's call.

## Open questions / blockers
- Existing PlayerHealth rows get their PlayerStanding row the next time a strike touches them (EnsureHealth); until then a client treats them as not graced and not marked, which is true for almost everyone.

## Next step
Open the PR for the PlayerHealth family, wait for CI, send the PM the green sha, then start DeviceJob/Roast after telling Crafting.

## Family 3 plan (BasketItem, ClayPiece) - not started; waits for the new PM
Status of the other families: PlayerHealth merged (#811, 875cb23); DeviceJob and Roast in #819 (see tickets/bugfix-54.md).

Technique that passed the Upgrade check each time: never remove a column. Either make the table private and add a small public table of what clients draw plus a `my_...` view (PlayerHealth), or keep the public table, write 0 into the hidden column and keep the real value in a private side table (Roast.Quality to RoastQuality). Add the rows for already-live data in `BackfillHiddenValues` (HiddenBackfill.cs, runs on every connection) so a running world carries on. Test: tools/upgrade-check.sh origin/main, the lane's check tool, and a throwaway reducer for the backfill (never committed).

Plan for the two tables (decision from the PM: Maker stays public, only Quality and Wear move):
1. `BasketItem` (BuildingStorage.cs: Id, StructureId, Item, Quality, Count, Wear, Maker). Keep the table public with Quality and Wear written 0; private `BasketItemDetail(Id, Quality, Wear)`; a view `my_basket_detail` for the baskets the caller may open (owner, or claim member by the same rule the Take reducer uses). Server reads of Quality and Wear (BasketPut, BasketTake and anything that moves items) switch to the detail table. Client reads: StorageSounds.cs, BuildingSystem.Use.cs, Stations.cs; tools: ClayCheck, BuildTest.
2. `ClayPiece` (Ceramics): same pattern with Quality; client reads CeramicsView.cs and CeramicsView.Slip.cs; tools ClayCheck, BuildTest.

## Open lane questions (relay through the PM; none answered yet)
- Ceramics: what in CeramicsView.cs and CeramicsView.Slip.cs reads ClayPiece.Quality (a tooltip? the piece's colour?). If it is only a tooltip for the maker, a view for the piece's owner is enough.
- Crafting (baskets): does Stations.cs or any tool read BasketItem.Quality or Wear for baskets that are not the player's own?
- Both: is any "quality" shown for another player's basket contents or pot? If yes, it stays public (rule: anything another client draws stays public).

## Stage B (unchanged, after stage A, Integration owns Subscriptions.cs)
See the plan above. Nothing started.

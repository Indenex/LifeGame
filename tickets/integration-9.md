# integration-9: Split the hot shared files
Lane: Integration
Owner: Integration thread
Status: in_progress (plan; no code yet)
Branch: claude/integration-split-*  (one branch per step below)
PR: (see GitHub)

## Goal
Lanes stop colliding in four files. Each lane/feature owns its own file and registers itself; ids are fixed explicit numbers, so merge order no longer matters. No world wipe: every existing numeric value stays.

## Where the collisions are today
| File | Why it collides |
|---|---|
| `server/spacetimedb/Building.cs` `StructureKind` + `shared/Structures.cs` `Shapes` and `StageCosts` | Kinds get their id from list position ("numbered by merge order"). `Shapes` and `StageCosts` are parallel arrays indexed by it, so two lanes adding a kind in one PR order break each other, and the three lists must be edited in lockstep. |
| `shared/Items.cs` `Item` | Already explicit ids in lane ranges. Collisions are textual only (two lanes appending at the same spot). A C# enum cannot be split over files. |
| `client/scripts/net/Subscriptions.cs` `Area` | One big array every lane appends to. |
| `client/scripts/Main.cs` | Every lane adds a node and hooks in `_Ready`/`OnPlayer`/`OnPlayerPos`. |

## Plan (small PRs, in this order; each keeps all current values)
1. **Structures (biggest pain, do first).** Give `StructureKind` explicit values equal to today's positions (LeanTo=0 ... LogCabin=14) and lane ranges for new ones (e.g. House building 100-, Crafting stations 200-, Animals 300-, Claims 400-). Replace the parallel arrays with `StructureDef { Kind, Shape, Stages(costs) }` records in a registry (`Structures.Register`), one `partial class Structures` file per lane (`shared/structures/Crafting.cs` ...). `Structures.Shape(kind)`/stage lookups go through the registry (dictionary keyed by the kind), so order in a file no longer matters. Existing rows migrate by moving the current entries into per-lane files unchanged. Server `Shape(int)` clamp is replaced by a lookup with a clear fallback. Matches D-206 (Def entry plus assets).
2. **Subscriptions.** Core list stays in `Subscriptions.cs`; each lane adds `client/scripts/<feature>/<Feature>Subscriptions.cs` with a method marked `[SubscriptionQueries]` returning its queries for `(range, farRange)`; `Area` collects them by reflection (works in the game and in the plain C# bot tools).
3. **Main.cs hooks.** An `IGameSystem` (Setup(Main), OnPlayer, OnPlayerPos, ...) discovered by reflection and added in a fixed order from an explicit `Order` number; lanes drop one file instead of editing `_Ready`. Move today's nodes over one lane at a time.
4. **Items.** Keep the single enum (explicit ids, lane ranges already documented); put each lane's block in its own `#region` with generous gaps, and move per-item data (name, tags, stack size) out of shared files into per-lane `ItemDef` registrations as part of the Item/Tool conversion in docs/CONTENT.md. Do after 1 because it follows the same registry shape.

## Rules for every step
- No renumbering; `tools/upgrade-check.sh` must pass; bindings regenerate unchanged for existing types.
- Tell the lane owners which file to put their entries in the moment a step merges (and update docs/CONTENT.md).
- Pending kinds (#271 LogCabin, #267 RepairTable, #285 devices) merge first or are rebased onto step 1.

## Open questions
- Lane id ranges for StructureKind (above are proposals; the PM decides, no player-visible change).

## Next step
Send this plan to the PM; start step 1 on a branch off main once #403 is merged.

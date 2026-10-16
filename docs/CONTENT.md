# Content is data on shared bases

Justin's rule (relayed by the PM, owned by the Integration lane, ticket `integration-2`):
all development works like `tool.Axe`, `craftingTable.Pottery`, `animal.Deer`. New content of
an existing family is **a Def entry plus assets** (model, animation, texture, sound). New
code is written only for genuinely new behaviour. When a lane adds to a family that is not
data-driven yet, it converts that family first, or as part of the same PR (the PM's option 3:
gradual, no big stop-the-world rewrite).

This file is the map. It says, for each content family, where it stands on `main` today and
what the target is. Lanes update their row in the PR that changes it.

## The target shape

Every family follows the same pattern:

1. **One `Def` record per family in `shared/`**, read by both the server and the client:
   - `Id` (append-only number, or a dotted string where the family already uses those),
   - `Name`, `Tags` (a flags enum or string set the rules query),
   - **stats** as start and end values only, run through the one progression formula (D-175);
     no family gets its own formula unless absolutely necessary, and every ceiling is one
     raisable constant (D-197),
   - `Model` and `Sound` ids (assets are looked up by id; no per-kind switch in a view class),
   - **behaviour flags** that existing code already knows how to act on (`Fence`, `Indoor`,
     `StoreStacks`, `Station`, ...).
2. **One registry** (`Foo.All`, `Foo.Find(id)`) that is the only list of members. Nothing keeps a
   second, parallel list in the same order.
3. **Server and client use the registry, never the member.** Code says "any item tagged
   Digging", never `Item.DiggingStick`; "this animal's `NoticeRange`", never `SheepNoticeRange`.
4. **Enums stay append-only.** Never renumber, rename or insert before an existing member
   (the stored world depends on it). `tools/upgrade-check.sh origin/main` proves it.
5. **Server is the authority (D-182).** The Def says what is allowed; a reducer checks it.

A new member then touches: its Def row, an asset, a Journal "Things to try" line when it adds
a process id (D-105), and a wiki line. If you find yourself adding a `case` to a switch or a
second list, the family is not converted yet: convert it.

## Where each family stands

"Data-driven" means adding a member is a Def entry plus assets. "Mixed" means part of it is.
"Ad hoc" means code per member. Status is from the code on `main`; file names are starting
points, not a promise.

| Family | Today | Ad-hoc spots to convert first | Target |
|---|---|---|---|
| **Item** | Mixed. `ItemDef` (Name, Plural, FuelSeconds, Tags, HasQuality) in `shared/Items.cs`, `Items.All`, per-lane id ranges. | Client art is per-lane `switch`es: `models/ItemModels.cs`, `crafting/ItemLook.cs`, `ClayItemLook`, `SurvivalItemLook`; `Garments` ids as raw numbers; `Items.All` order is not tied to the enum. | `ItemDef` gains `Model` (and `Icon`) ids; one `ItemModels.For(def)` looks up by id, so a new item needs no switch edit. |
| **Tool** | Mixed. A tool is an item with `ItemTags`; `ProcessDef.NeedsTool` is a tag mask; `BestTool` and `ToolSpeedup` are generic. Wear and repair are data on `ItemDef` (`Wears`, `RepairCost`, `RepairNeedsTool`; rules in `shared/Durability.cs`), so a new wearing tool is a Def entry (crafting-19). | Server code asks for tools by item id (`Combat.cs` `Has(Item.Spear)`, `Reducers.cs`, `Hunting.cs`, `Shearing.cs` each look up a tag themselves); Hammer special case; tag-name `switch` in `CraftingUi.cs`. | `ToolDef` (tags, speedup curve, wear, reach, damage) keyed by item id; rules ask `Tools.Best(tag)`; tag names come from one table. |
| **Process (recipe)** | Mostly data-driven. `ProcessDef` + `Processes.List`; skills and journal pick new ids up. | Ids outside the list (`Skills.Dig`, `Forage`, `Kill`, `Butcher`, `claim.mark`, clay/kiln ids) repeated in `AllProcessIds`, `ExtraNames`, `SpecOverrides`; new `Outcome` or `Minigame` needs code; work sounds map by id prefix in `SoundMix.WorkFor`. | Every process, including built-in actions, is a `ProcessDef`; `Work` sound id lives on the Def. |
| **Station** | Mixed. `StationKind`, `StationInfo.Name/BuildProcess` switches, a `StructureShape.Station` link, kind-generic `Stations.NearestOf` / `StationNear`. | Three switches to extend per station; its structure needs its own Shapes/StageCosts rows and a client model. | A station is a `StructureDef` with `Station = ...` and a list of what it makes; the `StationInfo` switches disappear. |
| **Device** (fire, kiln, raised hearth) | Ad hoc. Each has its own table, timer and view (`Fire`, `Kiln`, `ColdHearth`, `Roast`); raised hearth plugs in through `partial void` hooks. | `ClayRules.KilnStructureKind = 3` mirrors the enum as a number; `FireKind.Kiln` branches; no common "burns fuel" abstraction. | `DeviceDef` (fuel kinds, burn seconds from the formula, heat output, accepts) over one `device` table with a kind; fire, kiln and hearth become Defs. Needed before lamps and further devices (D-195). |
| **Structure / `StructureKind`** | Mixed. `StructureShape` with capability flags in `Structures.Shapes`; parallel `StageCosts`; server enum in `Building.cs`. | Parallel lists that must stay in the same order; `kind` magic numbers in `StructureModel.cs`, `Structures.StageProcess`, `StepCrossesWalls`, `StructureView`, `BuildingSystem`; one hand-written `Piece` builder per kind. | One `StructureDef` carrying shape, flags, stages with costs, and a model id; the enum is only the stable id. Models built from a small kit (walls, roof, post, rail) where shapes repeat. |
| **Animal** | Ad hoc. `AnimalSpecies { Sheep }`; every constant is sheep-named; `Animal` carries sheep fields (Fleece, Growth); client `Sheep.cs`, `Herd` keyed on Sheep; deer have separate client classes. | `SheepWalkSpeed`, `SheepNoticeRange`, ...; `Species == Sheep` in `Herd.cs` and `Atmosphere.cs`; loot dropped by item id; per-species voice classes. | `AnimalDef` (speeds, senses, flock size, diet, loot table, model, voice) and one `Animal` base behaviour; `animal.Deer` and `animal.Sheep` differ by Def. Species-only behaviour (shearing, lambing) stays as small capability flags. |
| **Garment** | Mostly data-driven since #149. `Wearable` records in `shared/Clothing.cs` (slot, insulation, effect line); server `Clothing.cs` and `worn_item` read them. | Client `models/Garments.cs` builds each garment by hand; `ItemModels` has clothing by number. | `Wearable` gains a `Model` id; slots and effects stay as they are. |
| **Food** | Ad hoc. `Needs.Nourishment(Item)` switch, `GoodMeal`, water pots in a second switch. | Every food is a `case`; roast path is hard-wired to mutton items. | `FoodDef` (food, water, good-meal flag, cook result) in the item registry. |
| **Landmark / world feature** | Ad hoc. Pure functions of cell coordinates (`Trees`, `Rocks`, `Landmarks`, `Forest`, `Land`) shared by server and client. | No registry; each feature needs a shared function, a `Terrain.cs` branch and collision; water level defined in several places. | `FeatureDef` (placement rule id, radius, collision, model variants, season behaviour) so Environment adds a feature as data; one `WaterLevel`. Repeated features vary per instance (D-166). |
| **Weather / season event** | Ad hoc. `Seasons` is shared pure functions; `NextWeather` is a hard-coded kind switch; fireflies, leaf fall, mist are separate classes. | Weather kinds in three places (server odds, visuals, `SoundMix`). | `WeatherDef` / `SeasonEventDef` (when, odds by season, visuals, sound id) read by server and client. |
| **Sound cue** | Ad hoc. `enum Cue` plus a synth block per cue in `SoundMix`; `Work` sounds mapped by process prefix. | Each cue is code. | Synth recipes as data (layers, envelopes, filters) keyed by cue id; Audio keeps the engine, not the list. |
| **Skill** | Data-driven. `Specialization` rows, `SpecOverrides`, one progression formula (`Skills.Gain`). | Prefix coupling (`husbandry` uses `animal.`); `ExtraNames` for non-recipe processes. | Skills reference process tags, not name prefixes. |

## How to convert a family (the rule for a lane that is about to add to it)

1. Add the `Def` type and registry in `shared/`, filled from the existing members. Behaviour must not
   change: the same numbers come out.
2. Move one reader at a time from the old switch to the registry (server first, then client).
   Delete the switch as soon as nothing reads it.
3. Prove it: the server builds, the upgrade check passes with no wipe, and the family's checks
   (CraftCheck, BuildTest, ClayCheck, SpawnCheck) behave the same.
4. Add your new member as a Def entry plus assets.
5. Update this table in the same PR.

If converting would be a large change, send the PM the options first (D-059) and keep working on the
smallest conversion that lets your member go in as data.

## Order of conversion (proposal)

Each happens when a lane next adds to that family, not before:

1. **Structure** and **Station** (Building / Crafting / Ceramics / Animals all append here constantly,
   and the parallel lists cost the most merge pain).
2. **Animal** (deer are next, #219).
3. **Device** (lamps, D-195).
4. **Item and Tool** art lookups, and tool durability (crafting-19).
5. **Food**, **Landmark**, **Weather**, **Sound** as Environment, Atmosphere and Audio add to them.

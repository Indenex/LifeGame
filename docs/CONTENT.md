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
| **Process (recipe)** | Mostly data-driven. `ProcessDef` + `Processes.List`; skills and journal pick new ids up. | Ids outside the list (`Skills.Dig`, `Forage`, `Kill`, `Butcher`, `claim.mark`, clay/kiln ids) repeated in `AllProcessIds`, `ExtraNames`, `SpecOverrides`; new `Outcome` or `Minigame` needs code; work sounds map by id prefix in `SoundMix.WorkFor`. Work poses: `ProcessDef.Motion` (shared/WorkMotion.cs) is read first; entries that leave it unset still get the old id-prefix guess in the client's `WorkMotions.For` until set. | Every process, including built-in actions, is a `ProcessDef`; `Work` sound id and `Motion` live on the Def. |
| **Station** | Mixed. `StationKind`, `StationInfo.Name/BuildProcess` switches, a `StructureShape.Station` link, kind-generic `Stations.NearestOf` / `StationNear`. | Three switches to extend per station; its structure needs its own Shapes/StageCosts rows and a client model. | A station is a `StructureDef` with `Station = ...` and a list of what it makes; the `StationInfo` switches disappear. |
| **Device** (fire, kiln, raised hearth) | Ad hoc. Each has its own table, timer and view (`Fire`, `Kiln`, `ColdHearth`, `Roast`); raised hearth plugs in through `partial void` hooks. | the kiln and storage jar are found by `StructureShape.Kiln` / `.Jar` flags, not enum numbers (ceramics-9); `FireKind.Kiln` branches; the clay lamp has its own `lamp` table; no common "burns fuel" abstraction. | `DeviceDef` (fuel kinds, burn seconds from the formula, heat output, accepts) over one `device` table with a kind; fire, kiln and hearth become Defs. Needed before lamps and further devices (D-195). |
| **Structure / `StructureKind`** | Partly converted (building-12, integration-9). A `StructureDef` (fixed id + `StructureShape`, which carries the stage costs) per kind, registered from one file per lane in `shared/structures/`; a kind's id is its `StructureKind` enum ordinal in `Building.cs` (the module rejects explicit enum values), so a new kind is appended to the enum and its def says the next number; no per-lane id ranges. | `kind` magic numbers still in `Structures.StageProcess`, `StepCrossesWalls`; one hand-written `Piece` builder per kind; the enum is still written by hand. step 3 done (building-12): House building's own code finds huts, lean-tos and claim markers by `StructureShape.Hut` / `.Model`, not by enum name (Claims.cs, Town.cs, Kiln.cs and CeramicsView.cs still compare `StructureKind.ClaimMarker` / `.Kiln`: their lanes' files). | A model id on the def (building-12 added `StructureShape.Model` and `.Hut`); models built from a small kit (walls, roof, post, rail) where shapes repeat; `StageProcess` becomes a field of the def. |
| **Animal** | Mostly data-driven since animals-17 (#354). `AnimalDef` in `shared/Animals.cs` (`AnimalDefs.All`, ids match the server `AnimalSpecies` enum): speeds, senses, wander and flee ranges, wounded run-speed and rest rules, capability flags (`Shearable`, `Breeds`, `Bleats`), laying (`Egg`, `EggDays`, `EggCap`; hens, `Eggs.cs`), butcher yields, client model plan, antler share and starting herd spots. Server `Traits(species)`, `EnsureAnimals`, butchering and shearing read it; Atmosphere's bleat, Herd's shear offer and Sheep.cs's model choice read it too. | The `Animal` table still carries sheep fields (`Fleece`, `Growth`, lamb logic in `Lambs.cs`); client `Sheep.cs` is one view with deer branches by `Model` (to split into a generic animal view per model plan); per-species voice classes beyond `Bleats` (Audio's deer cues); the herd decision code assumes a flock of one species. | A generic animal view per model plan and a generic voice id on the Def; fleece and lambs move behind the `Shearable` and `Breeds` flags. A new species is then a Def plus model, animation and sound assets. |
| **Garment** | Mostly data-driven since #149. `Wearable` records in `shared/Clothing.cs` (slot, insulation, effect line); server `Clothing.cs` and `worn_item` read them. | Client `models/Garments.cs` builds each garment by hand; `ItemModels` has clothing by number. | `Wearable` gains a `Model` id; slots and effects stay as they are. |
| **Food** | Data-driven since survival-17/18/19. `FoodDef`, `PotDef` and `RoastDef` in `shared/Foods.cs` (`Foods.All`, `Foods.Pots`, `Foods.Roasts`); `Needs.Nourishment`, `Eat`, `DrinkFrom` and the roast result and recipe read them (survival-25: `Processes.List` builds every roast recipe from `Foods.Roasts`, so they sit together in the list). | Pot dishes (broth, stew, tallow) are still hand-written recipes. | A new food is a `FoodDef` entry; a new roast is a `RoastDef` entry and nothing else. |
| **Landmark / world feature** | Data-driven since env-10. `FeatureDef` (id, ground dry/stream, collision none/trunk/stones, one-per-square size and salt, spread, placement rule, model id) in `shared/Features.cs`, `Features.All` / `Find` / `At`; `Terrain.cs` and `TerrainCollision.cs` read the registry; client builders keyed by model id in `environment/LandmarkProps.cs`. | Placement rules are still functions in `shared/Landmarks.cs` (fine: a rule is behaviour); trees, rocks and undergrowth are per-cell scatter in `Terrain.cs`, not features; season behaviour not on the Def yet; water level defined in several places (`World.WaterLevel`, `Look.WaterLevel`). | A new landmark is a `Features.All` row, a placement rule and a builder. Repeated features vary per instance (D-166). Next: one `WaterLevel`, season flags on the Def when a feature first needs them. |
| **Weather / season event** | Mixed. Weather kinds are data: `WeatherDef` registry `Weathers.All` in `shared/Weathers.cs` (cloud, rain, wind ranges, duration, odds by season), read by the server's `NextWeather` and the client's F6 preview (atmosphere-21). `Seasons` is shared pure functions. Falling things (leaves, blossom) are data too: `SeasonEventDef` registry `SeasonEvents.All` in `shared/SeasonEvents.cs` (when in the year, rate, wind boost, under-trees, colours, size), drawn by the client's `FallingLeaves` (atmosphere-24). Fireflies and mist are still separate classes. | Client visuals (lightning, mist) still key off rain and wind amounts, not the kind; `SoundMix` keys off amounts too; fireflies and mist not yet data. | `WeatherDef` / `SeasonEventDef` (when, odds by season, visuals, sound id) read by server and client. |
| **Sound cue** | Ad hoc. `enum Cue` plus a synth block per cue in `SoundMix`; `Work` sounds mapped by process prefix. | Each cue is code. | Synth recipes as data (layers, envelopes, filters) keyed by cue id; Audio keeps the engine, not the list. |
| **Skill** | Data-driven. `Specialization` rows name what trains them (exact process ids first, else a recipe family such as `knap`); built-in actions (dig, forage, hunt, shoot, build stages...) are one table with their journal names; effects are rows in `Progression.Effects` over the one curve. | Set `ProcessDef.Skill` (a Specialization id) on a new recipe to name its skill; it wins over the guess. Unset ones still match a recipe family (`knap`, `cord`, `sew`, `hide`) by the part before the first dot; the journal order lives in `AllProcessIds`. | Built-in actions and existing recipes name their skill on the Def, so no family match is left. |

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

## Shared registries that are not content families (integration-9)

- **Client subscriptions.** `client/scripts/net/Subscriptions.cs` holds only the core queries. A lane that needs a table subscribed adds a file `client/scripts/net/Subscriptions.<Lane>.cs` with a static method marked `[SubscriptionQueries]` in `partial class Subscriptions`, taking `(string range, string farRange)` and returning its query strings (`range` for live things, `farRange` for slow-changing world state such as terrain and buildings). `Area` adds every such method (found by reflection, in name order); the check tools in `tools/` include `Subscriptions*.cs`, so they subscribe to the same set. Nothing in Main.cs changes.
- **Building kinds.** `shared/structures/Structures<Lane>.cs`, one `StructureDef` per kind (see the Structure row above).

## Order of conversion (proposal)

Each happens when a lane next adds to that family, not before:

1. **Structure** and **Station** (Building / Crafting / Ceramics / Animals all append here constantly,
   and the parallel lists cost the most merge pain).
2. **Animal** (done for deer in #354: a Def per species; the generic view and voice id are next).
3. **Device** (lamps, D-195).
4. **Item and Tool** art lookups, and tool durability (crafting-19).
5. **Food**, **Landmark**, **Weather**, **Sound** as Environment, Atmosphere and Audio add to them.

## Showcase views

`client/scripts/models/Showcase.cs` renders model views for the Render lane. A view is a parameterless instance method of `partial class Showcase` marked `[ShowcaseView("name")]`; `--view=name` finds it by reflection. Add your lane's views in your own `client/scripts/models/Showcase.<Lane>.cs` (`public partial class Showcase : Node3D`), not in the shared file. Read options from the `args` dictionary (e.g. `args.GetValueOrDefault("set", "homes")`).

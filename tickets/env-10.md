# env-10: World features as data
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-features-2 (from main; the first branch, claude/environment-features, was stacked on the dropped far-land work)
PR: (opening)

## Goal
Convert the Landmark / world-feature family to data, per docs/CONTENT.md: one `FeatureDef` per feature in `shared/Features.cs` (append-only `FeatureId`, ground, collision kind, one-per-square size and salt, spread, placement rule, model id) and one registry `Features.All`. Terrain drawing and collision read the registry instead of having one branch per landmark, and client builders are keyed by model id in `LandmarkProps`. Behaviour does not change: the same landmarks stand in the same places, looking the same. After this, a new landmark is a Def row, a placement rule and a builder. Justin sees nothing new.

## Progress log
- 2026-10-16 22:20 UTC: started after Integration's CONTENT.md rule. `shared/Features.cs` (GiantOak, Outcrop, Ford); `Landmarks` keeps only placement rules (`OakFits`, `OutcropFits`, `FordFits`, `IsChosen`, `ChosenIn`); `Terrain.AddVegetation` builds `Features.At(x, z, Dry/Stream)` via `LandmarkProps.For(def).Build`; `TerrainCollision` uses `FeatureBlock.Trunk` (girth from the Prop) and `FeatureBlock.Stones` (stones by square, reach = Spread, generalised from CollideOutcrops). Proof: a dump of every feature cell over x, z in [-1500, 1500) and every outcrop square in [-32, 32) is byte-identical before and after (scratch program compiling the shared files). Client, server module, SpawnCheck and BuildTest build. Committed dd24522; merged main (811fd39); updated the Landmark row in docs/CONTENT.md.
- 2026-10-16 23:55 UTC: replayed onto a fresh branch from main (d1e99bf, 5959528). Merged main's jumping into the feature collision (CollideFeatures takes airHeight; stones a jump clears don't block) and kept main's Food and Weather rows in docs/CONTENT.md. Placement dump against current main's shared files: identical (9501 lines). Client, server module, SpawnCheck and BuildTest build.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR; send the PM the green sha.

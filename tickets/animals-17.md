# animals-17: AnimalDef registry (animals are data, docs/CONTENT.md)
Lane: Animals
Owner: Animals lane (Sonnet)
Status: in_review
Branch: claude/animals-def-5gkntn (stacked on claude/animals-deer-5gkntn / PR #219; retarget to main when #219 merges)
PR: (see below)

## Goal
Justin's rule (docs/CONTENT.md): content is data on shared bases. Species-specific numbers and "if Deer" branches become one `AnimalDef` per species in `shared/Animals.cs`, so a new animal is a Def entry plus assets. Behaviour is unchanged: sheep keep their old numbers, deer theirs.

## How it works
- `shared/Animals.cs`: `AnimalDef` (speeds, senses, wander and flee ranges, wood preference, curious, wary; wounded run-speed scaling and rest threshold; flags Shearable, Breeds, Bleats; butcher yields, hide and extra drop; client model plan and antler share; starting herd spots) and `AnimalDefs.All` / `Get(species)`. Species ids match the server `AnimalSpecies` enum (append-only).
- Server: `Traits(species)` returns the Def (the AnimalTraits class and the sheep/deer constants are gone); `EnsureAnimals` spawns each Def's herds in worlds that lack that species; butchering drops by Def (meat, hide, extra); shearing checks `Shearable`; the wounded-rest and wounded-speed rules read the Def.
- Client: Atmosphere's bleat reads `Bleats`, Herd's shear offer reads `Shearable`, Sheep.cs picks the model by `Model` and the stag share by `AntlerShare`.
- Not converted yet (still named for sheep): the lamb/fleece fields on the Animal table, and the per-species voice classes beyond the Bleats flag. Next: a generic animal view split out of Sheep.cs when the next species arrives.

## Progress log
- 2026-10-16 22:25: written; server and client build, upgrade check against main passed.

## Open questions / blockers
- Stacked on #219.

## Next step
Wait for #219 to merge, then retarget this PR to main (merge main in), and send the PM the green sha; update the Animal row in docs/CONTENT.md once it is on this branch.

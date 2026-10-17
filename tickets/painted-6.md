# painted-6: Items on the painted look
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-6-items
PR: (not opened)

## Goal
The last family of the painted rollout (painted-1 rocks, -2 bushes, -3 ground props, -4 animals, -5 structures, -6 items): tools and items in the hand, items lying on the ground, and the inventory/hotbar icons (ItemIcon renders them at runtime from the ground mesh, so they follow) all draw with the shared painted shader (`Painted.Of`), tuned per material family and kept readable at item size. Justin sees painted axes, knives, stones, cord, hides and food in the world and the inventory.

## Progress log
- 2026-10-17: `client/scripts/models/PaintedItems.cs` (standalone on main; does not need painted-4/-5): families Wood, Stone, Fibre, Hide, Clay, Food, Plain; `Of(material, family, held)` caches a tuned twin of `Painted.Of` per (source, family, held), passes through anything not on Look's shader (pot water, ceramics' own clay shader) and traces an item twin back to its source. Items: ink 0.85 (stone 0.95, food 0.65), dab_size 0.32, relief kept at 0.6 of the source (Painted.Of keeps 0.35) so grain and folds don't wash out. Hide leans 25% toward a tan so pale hide/wool doesn't burn out to white. Held twins have dab_size 60 and no lit dabs, since the brush is in world space and would crawl over a moving tool; they keep the stepped light, violet shade and ink. `Paint(mesh, family)` / `Unpaint(mesh)` swap a mesh's materials (cached).
- Swapped at the sources: `ItemModels.Make` (each kit surface: Wood/Stone/Plant->Fibre/Plain->`FamilyOf(item)`), `HungFromTop` (held fallback, re-traced to held twins), `ArrowFlight` (held twins). Hooks in other lanes' files (one line each): `crafting/ItemLook.MeshFor` paints the fallback meshes (`OldMeshFor` stays today's look for comparisons), `crafting/GroundItems` paints maker-marked pot meshes.
- Showcase: `--view=items --compare` shows 14 items, today's look behind, painted in front. Renders: `renders/painted-6-items.png`, `renders/painted-6-held.png`. Client builds clean; 4 tuning passes.

## Open questions / blockers
- Clay pots, jars, troughs and tiles use Ceramics' own clay shader (`ClayLook`, with stamped maker marks), so they pass through unpainted. Painting them needs the painted light/ink ported into ClayLook's shader: a Ceramics (or Modeling helper) follow-up, options to PM.

## Next step
PM: review renders, then open the PR from claude/painted-6-items once CI is green.

# building-26: The village kit's look on our homes
Lane: House building
Owner: House building thread (session_01DzgG4copWqifKWUcydbjBZ)
Status: in_review
Branch: claude/building-ht3tnl
PR: opened after #847 merged (see Progress log)

## Goal
Justin bought Quaternius's Medieval Village MegaKit (CC0) as a style source (PM, 2026-10-17): merge its style with ours where it fits, within his guidelines (painted, chill, immersive; stone age through clay tiers). Pull the kit's look into the lean-to, log cabin and clay hut: its timber, plaster and stone textures, chunky shapes and trim details; use its pieces directly where they fit our tiers; no medieval buildings beyond our tiers. Modeling takes the kit's props and the Nature kit; shared kit textures under client/assets/quaternius/village/ (proposed to Modeling). Justin sees every look change before it merges (D-154).

## Progress log
- 2026-10-17 16:40: kit textures (wood light and dark bands from T_WoodTrim, plaster, round rocks) resized to 1024 under client/assets/quaternius/village/. Painted.cs gains a `Surface` block (surface_tex, surface_mix, surface_scale, surface_hue, surface_mean): UVs where a mesh has them, else three-sided projection in the mesh's own space. Sticks and logs (StickInto) now carry UVs in metres along the grain. PaintedStructures.WithSurface lays a kit surface on a painted material; `--no-kit` turns it off for before-and-after. StructureModel: Wood takes kit wood grain (hue 0.35), Clay (daub) the kit plaster (hue 0.45). Showcase view `homes` (`--focus=0..2`, `--low`).
- 2026-10-17 16:30: Justin picked "Warm clay" daub (our clay colour with the kit's strokes: plaster hue 0.85) and "Trim and textures" for shapes (D-154 cards via the PM). Textures moved to Modeling's shared layout: client/assets/quaternius/village/textures/ (base colours 1024 px, normals 512 px, plus the wood bands), License_Village_Source.txt beside them, made by tools/art/quaternius/village_textures.py from villagekit-godot.zip. New StructureModel.Trim.cs: TrimWood (kit dark grain), hewn DoorFrame (squared jambs, heavy lintel, sill board) on the cabin (replaces the round posts and lintel log) and the clay hut, and squared CornerPosts on the cabin. Client builds; test shots look right.
- 2026-10-17 17:55: #847 merged; rebased onto main 53ed7755, bindings regenerated, client and server build. Pushed to claude/building-ht3tnl; PR opened; Render asked for before/after (main vs this head).

## Open questions / blockers
- (none; both look questions answered)

## Next step
Wait for CI and Render's before/after on the PR; send the PM the green sha once Justin has seen the renders.

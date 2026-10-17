# building-26: The village kit's look on our homes
Lane: House building
Owner: House building thread (session_01DzgG4copWqifKWUcydbjBZ)
Status: in_progress
Branch: local building-26-wip, stacked on claude/building-ht3tnl (#847); pushed there once #847 merges. Checkpoints: /mnt/project-files/patches/building-26/
PR: (not yet)

## Goal
Justin bought Quaternius's Medieval Village MegaKit (CC0) as a style source (PM, 2026-10-17): merge its style with ours where it fits, within his guidelines (painted, chill, immersive; stone age through clay tiers). Pull the kit's look into the lean-to, log cabin and clay hut: its timber, plaster and stone textures, chunky shapes and trim details; use its pieces directly where they fit our tiers; no medieval buildings beyond our tiers. Modeling takes the kit's props and the Nature kit; shared kit textures under client/assets/quaternius/village/ (proposed to Modeling). Justin sees every look change before it merges (D-154).

## Progress log
- 2026-10-17 16:40: kit textures (wood light and dark bands from T_WoodTrim, plaster, round rocks) resized to 1024 under client/assets/quaternius/village/. Painted.cs gains a `Surface` block (surface_tex, surface_mix, surface_scale, surface_hue, surface_mean): UVs where a mesh has them, else three-sided projection in the mesh's own space. Sticks and logs (StickInto) now carry UVs in metres along the grain. PaintedStructures.WithSurface lays a kit surface on a painted material; `--no-kit` turns it off for before-and-after. StructureModel: Wood takes kit wood grain (hue 0.35), Clay (daub) the kit plaster (hue 0.45). Showcase view `homes` (`--focus=0..2`, `--low`).

## Open questions / blockers
- License_Source.txt: the Source zip isn't in assets-incoming (asked the PM).
- Look choices for Justin via the PM: plaster colour (kit's pale cream vs our clay with kit strokes); chunkier thatch and kit-style trim (geometry).

## Next step
Send the PM the plaster options with test shots, and ask Render for before/after from the game camera once the commit is pushed.

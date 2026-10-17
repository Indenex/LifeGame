# painted-5: Painted structures
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-5-structures (includes painted-1, claude/painted-1-rocks)
PR: (not opened)

## Goal
Fifth family of the painted rollout (painted-1 rocks ... painted-6 items): everything players build (lean-tos, clay huts, log cabins, brick and tiled houses, walls, doors, roofs and thatch, hearths and the stone hearth, stations, fences, kilns, jars, troughs, claim posts, the fire ring) on Painted's shader, so they share the rocks' and trees' stepped light, violet shade and soft ink edge. Materials only: no mesh, server, Look shader or CI changes. Justin sees warm brown wood with violet shade, golden straw thatch in small strokes, and hearth stones like the painted rocks.

## Progress log
- 2026-10-17 05:40: `client/scripts/models/PaintedStructures.cs`: `PaintedStructures.Of(material, family)` gives a cached, tuned painted twin per source material (Wood, Thatch, Stone, Clay, Plain); non-Look materials and twins pass through; `Unpainted(twin)` returns the source for comparisons. `StructureModel` paints its own materials at the source (Wood, PaleWood, Fiber, Thatch, Weave, Stone, Clay, Tile, Brick, Ash, Mud), so doors, bars and the sleeping mat that use them directly go painted too, and `Pieces(kind)` hands every other piece (stations, kiln, jar, trough, claim marker) through as Plain. `HearthModel` ring (Stone) and cold ash (Clay/Wood) painted. `Painted.StructureGhostOf` adds a painted variant of Look's structure ghost (cull_disabled, depth_prepass_alpha) and `StructureView` uses it for the roof/wall cutaway (D-168), falling back to `Look.StructureGhost`. The placement preview keeps its own translucent material (untouched). Showcase `structures` (`--set=homes` lean-to, clay hut, log cabin; `--set=small` table, kiln, stone hearth, fire ring, fence, jar; `--cutaway` fades the roofs half): today's look in the back row, painted in front. Three tuning passes: thatch needed a darker gold albedo and 0.75x relief to keep the straw texture. Renders: /tmp/claude-0/-home-user-LifeGame/cb34691a-9170-5cf0-bc96-a980f47c2dfd/scratchpad/p5/homes-2.png, small-3.png, homes-cut.png (scratchpad, not committed).

## Open questions / blockers
- No chicken coop exists on this branch (not in StructureKind); when it lands it is painted automatically through `StructureModel.Pieces`.
- `client/scripts/building/` belongs to House building: the changes there are small hooks (family wrapper on its materials, one line in `Pieces`, one line in `StructureView.SeeThroughMaterial`).

## Next step
Send the renders to the PM for Justin; open the PR after painted-1 merges (stacked on it).

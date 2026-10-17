# painted-12: Smooth, clean painted surfaces (D-319)
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-12-smooth
PR: #642

## Goal
Justin on the painted structures and items: "The textures of those is awful, the weird pitted look is not cutesy." D-319 asks that every painted family be smooth and clean, in the D-313 cutesy style:
- broad soft colour gradients;
- the soft stepped light bands kept, but softened;
- gentle warm shade, a faint ink edge;
- no small dabs, crevice speckle or fine noise.
The change is made centrally, as one constant set.

## Progress log
- 2026-10-17 08:10: `Painted.Smooth` holds the constant set used by Painted's shader, which covers every painted family: rocks, bushes, logs and birch bark, animals, structures and items.
  - Relief 0: Look's fine bump normals and the noise in the AO are gone. Those were the pitting.
  - Paint 0.15: the fine brush noise in the colour, down from 0.6.
  - DabScale 4: brush patches four times larger, so they make broad drift, not dots.
  - The lighter top-face wash is broad, soft and weaker.
  - Crevice: a gentle, broad hollow darkening instead of a sharp one-tone speckle.
  - Ink 0.5: half the edge darkening, softer.
  - Bands: softer edges (0.1, from 0.03), barely broken by the brush (0.04, from 0.16).
  - A soft top-to-underside gradient.
  - `Declarations` is now a static readonly string with the constants filled in; Environment's ground can still reference it.
- Renders, before (main painted) and after (smooth): renders/painted-12-smooth-{homes,small,items,rocks}.png.

## Open questions / blockers
- PaintedTrees (PAINTED_FOLIAGE) has its own bough painting and wasn't part of the complaint, so it is unchanged. It could be softened in a follow-up if Justin wants.

## Next step
The PM shows Justin; merge on his OK.

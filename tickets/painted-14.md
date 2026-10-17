# painted-14: Smooth painted trees (D-319, option A)
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/painted-14-trees
PR: (not opened; held for Justin's pick)

## Goal
Bring D-319 ("smooth, clean painted surfaces, no pitting") to the coded painted trees. Each bough keeps its one lighter patch, but the patch edge is soft and the small lit dab spots are gone. The light bands blend instead of stepping hard. Only the shader in `PaintedTrees.cs` changes; all the constants are in `PaintedTrees.Smooth`. Justin sees calmer, less speckled pines and broadleaves. Compare in `renders/painted-14-trees-before-after.png`: top is before, bottom is after.

This is held for Justin's pick, since Art R&D's Blender trees (art-rnd-1) may replace the coded trees.

## Progress log
- 2026-10-17: built on main 642ca58; local build has 0 errors; rendered the before/after with the treelod Showcase view.

## Open questions / blockers
- Justin: take the smoother trees (A), or keep the current ones until the Blender trees land?

## Next step
Wait for the PM to pass on Justin's pick; if yes, open a PR to main and send the green sha.

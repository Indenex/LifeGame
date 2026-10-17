# model-28: Boulder variety (D-166)
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-28-rock-variety
PR: (see below)

## Goal
Boulders are the prop that repeats most near spawn. Terrain picks one of 4 shapes per region, so a patch of boulders is one shape at different sizes and turns.
- `PropMeshes.RockVariants` goes from 4 to 8, with `RockKinds = 4`: variants 4 to 7 are a second form of each kind (variant v is kind v % 4).
  - Worn lumps: a whaleback with knuckles.
  - Split stone: one half fallen away, with a chunk at its foot.
  - Slab: stepped like a ledge, moss along the step.
  - Cracked round stone: cracked three ways.
- With RockVariants at 8, Terrain's Regional pick already spreads 8 shapes over the map with no change.
- Asked Environment for a one-line picker like TreeVariant, `kind + RockKinds * (hash < 0.5 ? 0 : 1)`, so each region mixes both forms of its kind.
- Showcase view `rockforms`.
Render: `renders/model-28-rock-forms.png` (first forms at the back, second forms in front).

## Progress log
- 2026-10-17: built; build has 0 errors; rendered.

## Open questions / blockers
- Environment owns Terrain.cs and LandmarkProps.cs for the mixing picker (asked).

## Next step
Wait for CI on the PR, then send the green sha to the PM.

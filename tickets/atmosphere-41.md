# atmosphere-41: No lilac on pale stone at golden hour
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-41-warm-rims
PR: (opening)

## Goal
After #767, pale rocks still read pinkish-lilac at golden hour (Render B's renders/golden2-trio-072.png at 6598cbb). Modeling's #802 (painted-17) warms the props' lavender shade tint at low sun. This is the Atmosphere side:
- `AtmosphereLens.cs`: rim_tint at golden hour is a richer gold (1, 0.82, 0.62) instead of pink-peach (1, 0.80, 0.72), and just before sunset a warm peach (1, 0.74, 0.60) instead of rose (1, 0.72, 0.82). Pink rims read lilac on pale stone.
- `AtmosphereGrade.cs`: the cosy curve's lifted darks are a barely violet grey (0.06, 0.05, 0.075) instead of plum (0.06, 0.045, 0.09).
Also marks atmosphere-40 (#767) done.

## Progress log
- 2026-10-17 13:58: built; the client compiles with 0 errors.

## Open questions / blockers
- None. A before/after can ride with Modeling's #802 render at 0.72.

## Next step
Open the PR; send the PM the green sha.

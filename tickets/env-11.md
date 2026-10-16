# env-11: Heath hotfix (heather amount and colour)
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-heath-hotfix
PR: (opening)

## Goal
Justin, playtesting, hated the hilltop heath: heather carpeted the open ground at an even density in saturated pink-purple. Done means heather grows in a few irregular patches with plenty of open grass between them (about a fifth of the old amount or less), gorse is rarer, and heather reads as muted dusky green-brown most of the year, with only a soft, desaturated mauve in late-summer bloom, close to the grass in brightness. Client only, no wipe.

## Progress log
- 2026-10-16 22:45 UTC: Terrain.cs: heather = 0.26 x heath x patch (two-octave noise, smoothstep 0.62 to 0.74, so roughly a fifth of the heath is patch), was 0.30 x heath everywhere; gorse = 0.03 x heath x its own patch noise, was 0.04 x heath. PropMeshes.BuildHeather (Modeling's): green (0.35, 0.37, 0.24), flower heads faded brown (0.42, 0.35, 0.30), were (0.36, 0.42, 0.26) and saturated purple (0.62, 0.42, 0.62). Look.cs bloom (Atmosphere's): mauve (0.46, 0.38, 0.43) at 0.45, was (0.55, 0.22, 0.52) at 0.75. Also added the missing `namespace LifeGame;` to BuildingSystem.ClaimFences.cs, without which main's client doesn't build. Client builds. Pushed 16503a4.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR; send the PM the green sha. Render the hilltop heath (e.g. (-4,46)) for Justin.

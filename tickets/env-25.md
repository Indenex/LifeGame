# env-25: Subtler see-through fade for trees in the way
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-softer-see-through
PR: (opening)

## Goal
Justin's fog screenshot (renders/justin/2026-10-17-fog.webp at 9311b22e) showed a big pine drawn semi-transparent over a second pine, looking broken. PM: fade only trees that truly block the line to the player, to about 50-60% opacity at most, and dither instead of alpha-blending so overlapping translucent faces don't stack.

## Progress log
- 2026-10-17: Look.PaintedCode GHOST: tree and prop ghosts (no STRUCTURE_GHOST) keep 55% of their surface (only the window round the player opens to 20%) through a 4x4 Bayer screen-door discard; no ALPHA write, so they draw in the opaque pass and overlapping crowns don't stack. Building ghosts (StructureGhostShader) get `#define STRUCTURE_GHOST` and keep their alpha. fade_amount: full fade within 1.5 tree widths of the trunk axis, easing out by 2.1 (was 1.9-2.7). Modeling's PaintedTrees/Painted ghosts inherit it (they define GHOST only). Builds.

- 2026-10-17: Local renders at (2,46) and (-13,3): the tree beside the player thins by a fine dither, no stacking, player visible. Merged main (painted-1, painted-7).

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha to the PM; ping Modeling so painted-5's structure ghost defines STRUCTURE_GHOST.

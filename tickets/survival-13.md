# survival-13: Smaller, fainter cold frost
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-frost-smaller
PR: (to open)

## Goal
Justin's playtest feedback (via PM): the frost overlay is "waaay too big", wants more transparency and ~30-45% smaller. FrostShader in NeedsHud.cs: full-cold reach 0.32 -> 0.46 (and 0.75 -> 0.85 at none), max alpha 0.55 -> 0.36. Creep-in kept. Client only.

## Progress log
- 2026-10-16: change made on top of main; before/after render requested from Render lane.

## Open questions / blockers
- (none)

## Next step
Open PR, wait for CI, send green sha to PM; adjust numbers if Justin sees the render and wants more or less.

# ceramics-11: Tallow in the cooking pot meshes
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU)
Status: in_review
Branch: claude/clay-ceramics-11-kfr4kr
PR: (see progress log)

## Goal
Rendering tallow in the cooking pot looks like rendering tallow: Modeling's CookingMeshes (#287) already draws stones under the pot and floating bits on the liquid for broth and stew; tallow gets its own pale, creamy surface (SimmerView.Tallow) and its own floaters (CookingMeshes.Dish.Tallow).

## Progress log
- 2026-10-16 23:40: #287 and #343 are on main. SimmerView: Tallow colour, surface and floaters by dish. Client builds (after regenerating bindings).

## Open questions / blockers
- Look: the tallow colour (0.80, 0.72, 0.50) is a first guess; Render can show it next to broth and stew if Justin wants to judge it.

## Next step
Open the PR, send the PM the green sha, then extend ClayCheck's kiln run to fire tiles (ceramics-10 follow-up).

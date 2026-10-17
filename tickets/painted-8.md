# painted-8: Clay pots in the painted look
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/painted-8-clay
PR: (see below)

## Goal
Give clay pieces (ClayLook: wet, dry, pit-fired, kiln-fired, cracked, slip and stamp marks) the same painted light as everything else (D-313, D-319):
- Painted.Light's soft shade, mid and lit bands, with the shade leaning violet.
- A faint ink edge, and a touch lighter on top.
- Only a whisper of temper grain.
- Wet clay still glistens, through a soft highlight in light().
Ceramics' colours and marks are unchanged. Justin sees pots that sit with the rocks and logs instead of looking like plain PBR. Compare in `renders/painted-8-clay-before-after.png` (top before, bottom after), drawn from the new Showcase view `pots`.

## Progress log
- 2026-10-17: ClayLook shader got a light() (Painted.Light plus a wet highlight), an ink edge, a top gradient, and less grain. Added the Showcase `pots` view. Local build has 0 errors; before/after rendered.

## Open questions / blockers
- Ceramics lane owns ClayLook.cs; told via the PM. Shading is a little paler than before. If Justin wants more contrast, the shade_tint uniform or Painted.Smooth is the knob.

## Next step
Wait for CI on the PR, then send the green sha to the PM.

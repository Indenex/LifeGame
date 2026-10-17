# atmosphere-40: Bright, warm golden-hour fill
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-40-golden-fill
PR: (opening)

## Goal
Render B's #690 pairs (renders/haze-pair-3.png at bed25e80, dusk 0.74) showed main c7730fe6, which includes #690 and #738, darker at dusk than #690's own head 38ecdf73: a murky brown with lilac-pink rocks and trees in shade. The PM asked why.

Cause: between those two commits the only lighting change is #738. It thinned the fog near the player, which removed the pink veil, but that warm fog had also been lifting the darks. Without it, the low sun left the shade lit only by the cosy lavender ambient, which turned pale rocks lilac.

Fix (`AtmosphereGrade.cs`, cosy layer): at golden hour the shade fills with warm amber (0.72, 0.62, 0.56, blended up to 45%) instead of lavender, ambient energy rises up to +40%, and exposure up to +12% at low sun. The goal is a warm, bright, readable 0.65 to 0.75, with no fog veil back.

## Progress log
- 2026-10-17 12:15: built; the client compiles with 0 errors.

## Open questions / blockers
- Needs a Render before/after (main vs this branch) at 0.65, 0.72 and 0.75, plus noon as a check.

## Next step
Open the PR and ask Render B for the before/after in the haze-pair view; send the PM the green sha.

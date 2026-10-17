# atmosphere-40: Bright, warm golden-hour fill
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-40-golden-fill
PR: #767

## Goal
Render B's #690 pairs (renders/haze-pair-3.png at bed25e80, dusk 0.74) showed main c7730fe6, which includes #690 and #738, darker at dusk than #690's own head 38ecdf73: a murky brown with lilac-pink rocks and trees in shade. The PM asked why.

Cause: between those two commits the only lighting change is #738. It thinned the fog near the player, which removed the pink veil, but that warm fog had also been lifting the darks. Without it, the low sun left the shade lit only by the cosy lavender ambient, which turned pale rocks lilac.

Fix (`AtmosphereGrade.cs`, cosy layer): at golden hour the shade fills with warm amber (0.72, 0.62, 0.56, blended up to 45%) instead of lavender, ambient energy rises up to +40%, and exposure up to +12% at low sun. The goal is a warm, bright, readable 0.65 to 0.75, with no fog veil back.

## Progress log
- 2026-10-17 12:15: built; the client compiles with 0 errors.
- 2026-10-17 12:50: Render B's golden pairs (renders/golden-pair-1..4 at 7961df8f) showed #767 only a little lighter at 0.72 and 0.75. Cause: at 0.72 the sun is 10 degrees up (0.75 is sunset), and Look's ground and grass light squares a wrap of 0.3, so flat ground got about 13% of full light. Added the low-sun wrap Painted.Light already uses (wrap 0.3 to 0.6, a half-linear falloff at low sun) to both Look light() functions. Ground at a 10 degree sun gets about 0.36 instead of 0.13; noon (elevation above 0.55) is unchanged. Merged main; the client builds with 0 errors.
- 2026-10-17 13:00: the PM's target is the 38ecdf73 side of haze-pair-3 at 0.72 to 0.75: readable trees, rocks and grass, warm not pink. The cosy low sun is now soft gold (1, 0.76, 0.50) instead of peach-rose, because rose times the props' lavender shade tint turned pale rocks lilac. Banding: nothing on main since 38ecdf73 changed ground, grass or grade shading (git log covers only props and landmarks); probably 8-bit steps in very dark dusk tones, which the brighter ground should reduce. Asked Render B whether noon on main also shows it.
- 2026-10-17 13:01: CI green on e34d6a46; Render B's trio (golden2-trio-072/075 at 6598cbb) matched the target; merged as #767. Lilac rocks continue in atmosphere-41 and Modeling's #802.

## Open questions / blockers
- Needs a Render before/after (main vs this branch) at 0.65, 0.72 and 0.75, plus noon as a check.

## Next step
None (done); continued in atmosphere-41.

# survival-35: Mushrooms, a second wild food (forage as data)
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: in_review
Branch: claude/survival-mushrooms (stacked on claude/survival-5zelch, #832)
PR: (see below)

## Goal
Small clusters of woodland mushrooms in the shade beside trees, from spring to autumn (none in winter). G "Gather mushrooms" gives 3 (Item.Mushrooms = 164, 0.07 food and 0.01 water each, a nibble like a hazelnut); a picked patch shows two tiny buttons and comes back after 12 minutes (berries take 8). Nothing to tend (D-148). Lane default, sent to the PM as option 1; numbers sit low on the D-075 curve.

Forage is now data (D-206): `shared/Forages.cs` has one `ForageDef` per kind (yield, per pick, regrow, seasons, prompt, refusals, reach radius). `ForageBush` gains an additive `Kind` column (0 berry or hazel bush, 1 mushrooms); hazels stay one bush in three by row id. A new wild food is one Def plus its look.

## Progress log
- 2026-10-17 15:45: built. Server: `Forage` reads the Def; `EnsureSurvivalWorld` seeds mushroom patches once (also into an existing world): one try-up-to-12 per chunk for dry ground with a standing tree within 3 m and none on the spot. Offline simulation over shared/: 59 patches in 64 chunks (119 berry bushes). Client: `MushroomPatch.cs` (stand-in look: 4 to 7 pale-stemmed brown caps, two buttons that stay when picked), `SurvivalItemLook` stand-in cap for the item. SurvivalCheck: berry/hazel count filters by Kind, every patch is beside and not on a tree, and a mushroom feeds 0.07. ClayCheck and FirstEvening count only berry bushes. Server compiles; ctx-sender gate ok.

## Open questions / blockers
- Modeling: a proper mushroom item and patch model (asked via the PM).
- Justin: whether mushrooms are the next wild food at all (PM question, option 1 of 3).

## Next step
Wait for CI on the mushrooms PR; send the green sha to the PM after #832 merges.

# survival-35: Mushrooms, a second wild food (forage as data)
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: done
Branch: claude/survival-mushrooms (merged); follow-up on claude/survival-5zelch
PR: #837 (merged into main 272feaf); FirstEvening follow-up PR on claude/survival-5zelch

## Goal
Small clusters of woodland mushrooms in the shade beside trees, from spring to autumn (none in winter). G "Gather mushrooms" gives 3 (Item.Mushrooms = 164, 0.07 food and 0.01 water each, a nibble like a hazelnut); a picked patch shows two tiny buttons and comes back after 12 minutes (berries take 8). Nothing to tend (D-148). Lane default, sent to the PM as option 1; numbers sit low on the D-075 curve.

Forage is now data (D-206): `shared/Forages.cs` has one `ForageDef` per kind (yield, per pick, regrow, seasons, prompt, refusals, reach radius). `ForageBush` gains an additive `Kind` column (0 berry or hazel bush, 1 mushrooms); hazels stay one bush in three by row id. A new wild food is one Def plus its look.

## Progress log
- 2026-10-17 15:45: built. Server: `Forage` reads the Def; `EnsureSurvivalWorld` seeds mushroom patches once (also into an existing world): one try-up-to-12 per chunk for dry ground with a standing tree within 3 m and none on the spot. Offline simulation over shared/: 59 patches in 64 chunks (119 berry bushes). Client: `MushroomPatch.cs` (stand-in look: 4 to 7 pale-stemmed brown caps, two buttons that stay when picked), `SurvivalItemLook` stand-in cap for the item. SurvivalCheck: berry/hazel count filters by Kind, every patch is beside and not on a tree, and a mushroom feeds 0.07. ClayCheck and FirstEvening count only berry bushes. Server compiles; ctx-sender gate ok.

- 2026-10-17 15:25: Modeling's painted mushrooms are on #836 (model-34: `Mushrooms.Patch(v)`, `Mushrooms.Picked(v)`, `ItemModels.MushroomItem`). The wiring commit is ready as /mnt/project-files/survival/survival-35/0001-survival-35-draw-mushrooms-with-Modeling-s-painted-p.patch: MushroomPatch draws the two meshes (Ripe picks which), SurvivalClient.Setup sets `ItemModels.MushroomItem = Item.Mushrooms`, the stand-in cap leaves SurvivalItemLook. It won't compile until #836 is on main.

- 2026-10-17 15:45: #836 merged; merged main and applied the model wiring (Modeling's painted Patch/Picked meshes, ItemModels.MushroomItem set in SurvivalClient.Setup, stand-in cap gone).

- 2026-10-17 16:05: #837 green at 0f5dc91e, CI run https://github.com/Indenex/LifeGame/actions/runs/39047892208 (every job passed). Offline FirstEvening re-run with mushrooms: /mnt/project-files/survival/survival-35/firstevening-2026-10-17.md (first spawn: nearest wild food of any kind median 6 m, worst 15; rim respawn median 5, worst 14). No tuning proposed.
- 2026-10-17 16:20: #837 merged (main 272feaf). Follow-up: tools/FirstEvening now lists mushroom patches and prints "nearest mushroom patch" and "nearest wild food of any kind" per spawn kind.

## Open questions / blockers
- Justin: whether mushrooms are the next wild food at all (PM question, option 1 of 3).

## Next step
Wait for CI on the FirstEvening follow-up PR and send its green sha to the PM.

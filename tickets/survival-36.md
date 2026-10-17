# survival-36: Wild roots, a winter-safe wild food dug with a tool
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: done (models pending)
Branch: claude/survival-roots
PR: #856 (merged, main 53ed775)

## Goal
Leafy patches of wild roots (a rosette with one or two white-flowered stalks, like wild carrot) in open meadow, no standing tree within 3 m. With a digging tool equipped (digging stick or spade), G "Dig up roots" gives 2 wild roots (Item.WildRoots = 165, 0.09 food and 0.01 water each) in any season, so winter is not foodless; without one the server says "You need a digging stick to dig up roots". A dug patch shows a mound of turned earth and comes back after 20 minutes. Digging wears the tool a little and plays the dig motion. Lane default (option 2 earlier); numbers low on the D-075 curve. It is one more ForageDef (D-206): `ForageDef` gains `Tool`/`NoTool`, and `ForageBush.Kind` 2 is roots.

## Progress log
- 2026-10-17 16:40: built. Server: `Forage` requires and wears an equipped tool when the Def names one; `EnsureSurvivalWorld` seeds root patches once (also into an existing world), one try-up-to-12 per chunk for dry ground with `TreeCrowding(.., RootOpenRadius=3) == 0`. Client: `RootPatch.cs` stand-in look (ripe rosette and stalks / dug mound), `SurvivalItemLook` stand-in root. SurvivalCheck: every root patch has no tree within 3 cells, and a root feeds `Needs.RootFood`. Server compiles; ctx-sender gate ok. Offline sim (20 seeds): 54 patches in 64 chunks; nearest roots from a first spawn median 6 m (worst 15), from a rim respawn median 8 (worst 25).

- 2026-10-17 16:37: #856 green at b90563e8, CI run https://github.com/Indenex/LifeGame/actions/runs/39049249575 (all 14 jobs). Justin chose "dug with a tool"; merged 16:39.
- 2026-10-17 16:45: winter pacing check (by hand, from shared/ numbers). Food drains full to empty in 90 min (Needs.FoodDrain); a new character starts at 0.8 (72 min), a respawn at 0.6 (54 min). A digging stick is tier 1 (one stick plus a flake to cut with), a few minutes in. One dig gives 0.18 food, about 16 min of hunger, and the patch is back in 20 min, so one patch alone almost keeps pace (0.18 vs 0.22 drained per 20 min) and two patches in rotation more than do. Patches sit about one per chunk, with the nearest a median of 6 m from a first spawn. Winter is lean but never starving, which fits D-081. No tuning proposed.

## Open questions / blockers
- Modeling: a painted root patch and root item to replace the stand-ins (request sent through the PM).

## Next step
When Modeling's painted root patch and root item land on main, swap RootPatch.cs and SurvivalItemLook to them (as survival-35 did for mushrooms), then open a PR and send the green sha to the PM.

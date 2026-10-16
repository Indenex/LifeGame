# ui-8: Crafting panel moves the G prompt aside; clay items look different (playtest3)
Lane: UX and UI
Owner: UX and UI thread (session cse_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_review
Branch: claude/ux-ui-xlrh2s
PR: (opening)

## Goal
From playtest3. First, with the crafting panel open, "G Drink" sat half under the panel's left edge at 1280x720 (screenshots/playtest3-use-prompt-under-crafting-panel.png). Second, clay, clean clay, tempered clay and daub were identical brown lumps in the hotbar (playtest3-clay-items-look-alike.png). Done means the prompt steps aside while the panel is up, and each clay stage reads apart at a glance.

## Progress log
- 2026-10-16 05:10: `Panels.Side` (ui/Panels.cs) is a side-panel signal. Main sets it from `crafting.PanelOpen`, a new one-line property in CraftingUi.cs, and the Use prompt is blocked while it is true. The inventory row stays, since you craft from it. KEYMAP documents it.
- 2026-10-16 05:10: in ceramics/ClayItemLook.cs (Ceramics' file), each clay stage has its own shape: dug clay is a lumpy clod with pebbles, clean clay a smooth wedged block, tempered clay the same block flecked with pale grit, and daub a flat pat with straw. The same meshes lie on the ground. `--screenshot-clay` with `--screenshot-hud` shows them in the hotbar. Build 0 errors. Screenshots are ui-13-after-*.png. The before images are the playtest3 screenshots.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.

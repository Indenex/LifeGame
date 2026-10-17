# bugfix-38: Panels open behind the Esc menu
Lane: Bug fixes
Owner: Bug fixes thread (cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: (to open)

## Goal
With the Esc menu open, C, J, B and K still opened the crafting panel, journal,
build placement and claim panel behind it, so menus stacked on top of each other
(screenshot playtest5-pause-stack-before.png). Done: while the Esc menu is open
`PauseMenu._ShortcutInput` handles every key except Esc. Shortcut input runs after the
GUI (so the Controls page's focus, arrows, Enter and key capture still work) but before
any lane's unhandled input, so panel keys stop there. The menu's own rows take no GUI
focus, so it feeds their keys to its own `_UnhandledInput`. Esc closes it as before
(playtest5-pause-stack-after.png).

## Progress log
- 2026-10-16 06:30: found in the two-player playtest; added PauseMenu._Input; checked Esc then C, J, B, K: only the menu shows, and S still moves its focus; after Esc, J opens the journal again.
- 2026-10-17 03:20: UX review: swallowing in `_Input` runs before the GUI, so it ate menu, slider and Controls page keys. Their suggestion, swallowing in `_UnhandledInput`, does not work: Main does `MoveChild(menu, 0)` so the menu gets unhandled input last, after the lanes already opened panels. Moved to `_ShortcutInput`. Checked: C, J, B, K blocked; Up/Down on the rows, Left/Right on Volume, Enter opens Controls; arrow focus on the Controls page; Enter on a key shows "Press a key" with no chat opening; F8 rebind captured; Esc backs out of the page and closes the menu; J opens the journal after.
- 2026-10-17 04:10: renumbered from bugfix-19 (another session used 17-36) and ported to main 50495cc, which added an Options page; the menu's own keys still stop while it or Controls is open. Re-checked on main: C, J, B, K blocked with the menu open (screenshots/bugfix-38-esc-menu-after-cjbk.png); Down/Enter opens Controls; arrows move focus there; Enter then F8 rebinds Walk left (screenshots/bugfix-38-controls-rebind-f8.png); Esc backs out and closes; C opens crafting after.

## Open questions / blockers
- Not this fix: the Options page never takes keyboard focus on main, so its sliders can't be moved with the arrow keys at all (mouse only). Told the PM for UX.

## Next step
Wait for the PR's CI, then send the PM the green sha.

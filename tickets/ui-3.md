# ui-3: Esc menu
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress (plan with the PM)
Branch: claude/ux-ui-xlrh2s
PR: (none yet)

## Goal
Esc in the world (with no panel, chat, minigame or building placement open) opens a small, soft menu in the middle of the screen over a darkened world, in the quieter-HUD style (no boxes, soft-shadowed text). The world keeps running behind it (it's multiplayer). Entries: Resume, Keys (the F1 list, in place), Sound (master volume, music volume, music on or off), Quit. Mouse and keyboard both work (W/S or arrows, Enter, Esc to close). Settings persist in user://settings.cfg and load at start. Done when Justin has seen a screenshot and it works in a build.

## Plan
- client/scripts/ui/PauseMenu.cs (CanvasLayer, above the HUD): the menu and its sliders.
- Esc ownership: the menu opens from _UnhandledInput only when nothing else is open. Main passes one predicate (crafting panel or minigame, building placement, journal, claims panel, chat) as a small hook, so lanes that don't mark Esc handled still don't double-fire.
- Volume: Master bus for overall volume; music volume through a small hook in Atmosphere's SoundScape (ask the Atmosphere lane for a `MusicVolume` property, or a "Music" audio bus) so this lane doesn't edit their mixer.
- Movement and the Use key ignore input while the menu is open (UseKey.Blocked and Main.Walk gate).

## Progress log
- 2026-10-15 23:15: plan sent to the PM. Main CI after #26 (with Claims #8) building; local client build on main passes.

## Open questions / blockers
- To PM: plan OK? Music volume hook from Atmosphere.

## Next step
Build PauseMenu.cs with the Esc predicate hook in Main.cs, then screenshot it for Justin.

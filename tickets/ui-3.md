# ui-3: Esc menu
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: done
Branch: claude/ux-ui-xlrh2s
PR: #33 (merged 2026-10-15)

## Goal
Esc in the world (with no panel, chat, minigame or building placement open) opens a small, soft menu in the middle of the screen over a darkened world, in the quieter-HUD style (no boxes, soft-shadowed text). The world keeps running behind it (it's multiplayer). Entries: Resume, Keys (the F1 list, in place), Sound (master volume, music volume, music on or off), Quit. Mouse and keyboard both work (W/S or arrows, Enter, Esc to close). Settings persist in user://settings.cfg and load at start. Done when Justin has seen a screenshot and it works in a build.

## Plan
- client/scripts/ui/PauseMenu.cs (CanvasLayer, above the HUD): the menu and its sliders.
- Esc ownership: the menu opens from _UnhandledInput only when nothing else is open. Main passes one predicate (crafting panel or minigame, building placement, journal, claims panel, chat) as a small hook, so lanes that don't mark Esc handled still don't double-fire.
- Volume: Master bus for overall volume; music volume through a small hook in Atmosphere's SoundScape (ask the Atmosphere lane for a `MusicVolume` property, or a "Music" audio bus) so this lane doesn't edit their mixer.
- Movement and the Use key ignore input while the menu is open (UseKey.Blocked and Main.Walk gate).

## Progress log
- 2026-10-15 23:15: plan sent to the PM. Main CI after #26 (with Claims #8) building; local client build on main passes.
- 2026-10-15 23:30: built ui/PauseMenu.cs: Resume, Keys (pins the F1 list until Esc), Volume (master bus, saved in user://settings.cfg), Music on or off (fires toggle_music; no on/off state shown until Atmosphere exposes one), Quit. Esc opens it only when crafting minigame, building placement, chat typing or any focused control isn't open; the menu is Main's first child so lane panels see Esc first. Walking and G wait while open. Screenshot /mnt/project-files/screenshots/ui-06-esc-menu.png.
- 2026-10-15 23:30: added the "Click to strike" OnRequest hint for Animals (d2a885c). CI run https://github.com/Indenex/LifeGame/actions/runs/38823136844 green; PM merged #33 (6ad9b24).
- 2026-10-15 23:35: Atmosphere's PR #32 adds SoundScape.MusicVolume (0..1, eased) and MusicOn (read/write, saved in user://sound.cfg), reached from Main as atmosphere.Sound. Journal and Claims (#31) now mark Esc handled.

## Open questions / blockers
- Music slider and on/off state wait on Atmosphere's PR #32 reaching main.

## Next step
When Atmosphere's #32 is on main, add a Music slider and the on/off state to PauseMenu (atmosphere.Sound.MusicVolume / MusicOn) as a new ticket.

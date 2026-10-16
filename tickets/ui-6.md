# ui-6: Controls page in the Esc menu
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_review
Branch: claude/ux-ui-xlrh2s
PR: #46

## Goal
Esc > Controls lists every action with its key. Click a key and press a new one to change it (a key already in use swaps over). Keys save in user://settings.cfg and load at start; the F1 list, first-time hints ("{key} to walk") and the G prompt read the live keys. Sliders for turn speed (how fast the view eases round) and mouse sensitivity (right-drag), and Reset to defaults. Defaults unchanged. Asked by the PM after Justin said he couldn't tell what anything does.

## Progress log
- 2026-10-16 00:30: started from main ef1c26b.
- 2026-10-16 00:50: built ui/ControlsPage.cs, ui/Settings.cs; Controls.cs gains Rebind/ResetAll/ApplySaved, {key} hints, KeyFor; CameraDrag reads the camera_drag action and a Sensitivity; CameraRig.TurnEase hook. The Esc menu's "Keys" entry became "Controls". Screenshot /mnt/project-files/screenshots/ui-10-controls-page.png.

- 2026-10-16 00:40: PR #46 opened. Sent KeyFor notes to Journal (JournalPanel.cs:53), Claims (ClaimPanel.cs:168), Crafting (CraftingUi.cs:42/232/408), Ceramics (CeramicsView.cs:221); Survival's only one is removed by #39.

## Open questions / blockers
- Rebinding sets one key per action, so the arrow-key extras for walking go when you rebind walking (Reset brings them back).

## Next step
Wait for CI on #46, then send the PM the green sha (PM merges only heads containing current main).

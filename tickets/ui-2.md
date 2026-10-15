# ui-2: Immersive HUD polish
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress
Branch: claude/ux-ui-xlrh2s (restarted from main after #17 merged)
PR: (none yet)

## Goal
Per D-036 (immersion first), the HUD becomes minimal and soft. Mockup: /mnt/project-files/screenshots/ui-03-hud-mockup.png (drawn over a real render; icons in the mockup are placeholders).
- Inventory: a row of small round slots at the bottom centre with a picture of each item (rendered from the item's 3D model via ItemLook.MeshFor) and a small count, no text pills. The row dims to about 40% when nothing has changed for a while and brightens when you pick up or use something; a newly gained item's name floats above its slot and fades.
- No title or connection panel in the top left. Connection trouble shows as one quiet line that fades.
- Notes (toasts) are smaller, italic, without a panel, just above the inventory.
- The G prompt loses its panel: a keycap and words with a soft shadow. The first-time hint is text only.
- Survival rings stay as they are (Survival's).
Done when Justin has seen screenshots of it in game and agrees.

## Progress log
- 2026-10-15 22:30: mockup made and sent to the PM for Justin (script: render with HUD hidden, then draw over it with PIL).

- 2026-10-15 22:35: Justin: "Build it" (D-045).
- 2026-10-15 22:50: built. ui/ItemIcon.cs renders each item's ground mesh once into a small transparent viewport; ui/InventoryBar.cs is the slot row (dims to 45% after 6 s idle, name of a gained item floats up); Hud.cs drops the title and status panels (status is a fading line at top centre), notes are slanted boxless text above the row; G prompt and hints lose their boxes. Main.RefreshInventory passes item ids (hud.SetItems). `--screenshot-hud` fills sample items and a note offline. Screenshot /mnt/project-files/screenshots/ui-04-quiet-hud.png.
- 2026-10-15 23:10: PR #26 opened. Justin asked for a list of all keys: hold F1 shows ui/KeyHelp.cs, built from Controls.World and the live input map (lane bindings show as bound), hidden otherwise; first-time hint "Hold F1 to see every key". Key decisions sent to lanes (cc PM): strike = left click or Z (Animals binds it); craft_play_minigame = hold Ctrl (Skills binds it). Screenshot /mnt/project-files/screenshots/ui-05-key-list.png.
- 2026-10-15 23:12: D-050 right-drag turns the camera (ui/CameraDrag.cs, CameraRig.TurnBy hook; cursor captured while dragging; off while placing a building). Put in #26, not a separate PR: this session may only push claude/ux-ui-xlrh2s. Bug fixes' note: InventoryBar groups by label before the dictionary.

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #26, then send the green sha to the PM.

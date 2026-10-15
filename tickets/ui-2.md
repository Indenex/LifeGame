# ui-2: Immersive HUD polish
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: todo (mockup with Justin via the PM)
Branch: claude/ux-ui-xlrh2s (after ui-1 merges, restart from main)
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

## Open questions / blockers
- Waiting on Justin's view of the mockup (via the PM).

## Next step
When Justin answers, build it in client/scripts/ui/ (InventoryBar.cs, item thumbnails) with small hooks in Hud.cs.

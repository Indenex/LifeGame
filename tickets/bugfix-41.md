# bugfix-41: The bag, build, use and hotbar keys do nothing (look picker)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-41
PR: (to open)

## Report
Justin: "the inventory won't open" (pressing the bag key does nothing).

## Cause (reproduced)
Ran the real client headless against a local server and pressed I (tools/keytest). On main the LOOK PICKER (ui-21) is open: it opens by itself ~3 s after joining whenever the server has no look row for the player, and `Panels.Open()` counted it as a covering panel. While `Panels.Open()` is true the I key (`!Panels.Open()`), the use key (G/E), the build key, the hotbar and the herd/hints are all blocked. Every existing character has no look row, so on every join the keys died until the picker was answered.

## Change
- The look picker no longer counts in `Panels.Open()` (Main.cs), so it never traps input.
- It is a centre window (`Panels.Centre`): opening the bag, journal or build menu puts it away (Esc semantics: you keep the look you had); opening it from Options closes the others.
- It opens by itself only once per install (`look/asked` in settings.cfg), not every session.
- UX's #526 (EquipmentPanel: I opens over other windows, only the Esc menu or visible typing holds it back) covers the other swallowing paths (a text box keeping focus); the two PRs touch different files.
- `tools/keytest`: `run.sh` runs the real client headless and presses I twice; it fails without the fix ("pressing I did not open the bag") and passes with it.

## Next
CI; ask Integration to run tools/keytest in a client job (needs the Godot mono binary the export job already has).

# ui-1: One input map, the G Use key, and a HUD prompt
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress
Branch: claude/ux-ui-xlrh2s
PR: #17

## Goal
Per D-025 and D-033: every key the game binds is declared in one place (`client/scripts/ui/Controls.cs`) that matches `KEYMAP.md`, and G is a single Use key. Any lane registers a Use handler (CanUse, Priority, prompt text, Use, optional hold-to-repeat) from its own files; the UI picks the best handler for what the player faces and shows one prompt near the bottom of the screen, such as "G  Pick berries". The controls help line is generated from the same table, so it can never drift from the real keys. Done when, in a Linux or Windows build, Justin sees the G prompt change as he walks between a fire, a berry bush, water and a building site, G does the right thing at each, and screenshots of the HUD are posted.

## Plan
- `client/scripts/ui/Controls.cs`: the world key table from `KEYMAP.md` (action name, keys, help text). `Main.SetupInput` calls `Controls.Setup()` first; lanes' own `Bind` calls already skip actions that exist, so the table wins without editing lane files.
- `client/scripts/ui/Use.cs`: static registry. A lane calls `Use.Register(new UseHandler { Id, Priority, Find })` where `Find(player)` returns an offer (prompt, distance, action, optional repeat interval for hold actions like adding materials) or null. Highest priority wins, then nearest.
- `client/scripts/ui/UseKey.cs`: the HUD prompt (key cap plus verb), fades in and out, above the inventory row.
- Migrations: feed fire (in `Main.cs`) moves to a Use handler in this ticket. Survival (forage, drink), House building (add materials), Journal (read note) and Ceramics (tend clay) each register a handler in their own files; `KEYMAP.md` gets a short "How to add a Use action" section with the exact code.

## Progress log
- 2026-10-15 22:10: lane started; branch restarted from main after PR #4 merged (64812e0). Client builds locally (dotnet 8 from apt).
- 2026-10-15 22:15: Controls.cs (key table + generated help line), Use.cs (registry), UseKey.cs (G prompt, tap and hold). Feed fire moved from T to a G Use handler in Main.cs; eat rebinds V to X via the table. While a lane still binds G itself, G goes to the Use key first when it has an offer and falls through to the old binding otherwise. Screenshot: /mnt/project-files/screenshots/ui-01-use-prompt.png (`--screenshot-use="Feed the fire"` shows the prompt offline). Local screenshots: `xvfb-run -a -s "-screen 0 1600x900x24" godot --path client --resolution 1600x900 -- --screenshot=shot.png` (about 2 min on llvmpipe).

- 2026-10-15 22:10: PM: each lane migrates its own Use handler in its own files; this lane owns every key in the Controls table (eat on X is fine). After ui-1 is on main, tell the PM, who asks lanes to migrate. Survival #12 and building #10 touch the same keys; whichever lands second keeps Controls.cs as the source of truth. D-034: status updates and screenshots go to the PM by send_message, not as replies in this thread.

## Open questions / blockers
- With Justin via PM (proceeding on defaults): prompt placement (default: fixed below the player); controls line always on vs hide after a few minutes with F1 (default: always on).

## Next step
Wait for CI on PR #17 and record the run here; after PR #14 merges, add a "How to add a Use action" section to KEYMAP.md (code is in the comment at the top of client/scripts/ui/Use.cs).

# ui-1: One input map, the G Use key, and a HUD prompt
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress
Branch: claude/ux-ui-xlrh2s
PR: (none yet)

## Goal
Per D-025 and D-033: every key the game binds is declared in one place (`client/scripts/ui/Controls.cs`) that matches `KEYMAP.md`, and G is a single Use key. Any lane registers a Use handler (CanUse, Priority, prompt text, Use, optional hold-to-repeat) from its own files; the UI picks the best handler for what the player faces and shows one prompt near the bottom of the screen, such as "G  Pick berries". The controls help line is generated from the same table, so it can never drift from the real keys. Done when, in a Linux or Windows build, Justin sees the G prompt change as he walks between a fire, a berry bush, water and a building site, G does the right thing at each, and screenshots of the HUD are posted.

## Plan
- `client/scripts/ui/Controls.cs`: the world key table from `KEYMAP.md` (action name, keys, help text). `Main.SetupInput` calls `Controls.Setup()` first; lanes' own `Bind` calls already skip actions that exist, so the table wins without editing lane files.
- `client/scripts/ui/Use.cs`: static registry. A lane calls `Use.Register(new UseHandler { Id, Priority, Find })` where `Find(player)` returns an offer (prompt, distance, action, optional repeat interval for hold actions like adding materials) or null. Highest priority wins, then nearest.
- `client/scripts/ui/UsePrompt.cs`: the HUD prompt (key cap plus verb), fades in and out, above the inventory row.
- Migrations: feed fire (in `Main.cs`) moves to a Use handler in this ticket. Survival (forage, drink), House building (add materials), Journal (read note) and Ceramics (tend clay) each register a handler in their own files; `KEYMAP.md` gets a short "How to add a Use action" section with the exact code.

## Progress log
- 2026-10-15 22:10: lane started; branch restarted from main after PR #4 merged (64812e0). Client builds locally (dotnet 8 from apt).

## Open questions / blockers
- To PM: should this lane make the one-handler migrations in other lanes' files (Survival, Building, Journal, Ceramics), or should each lane do its own? Default: each lane does its own, this lane does Main.cs and documents the pattern.

## Next step
Write `client/scripts/ui/Controls.cs` and `Use.cs`, hook them into `Main.cs`, build with `cd client && dotnet build`.

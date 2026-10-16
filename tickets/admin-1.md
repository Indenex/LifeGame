# admin-1: admin item panel
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/admin-1
PR: (see below)

## Goal
Justin (the world's admin) presses a hidden key (F10), sees a searchable list of every item with its icon, picks one, sets a quantity (and a quality for made things) and presses Give; the items land in his bag. The server decides who may (D-182): the admin list is private and only the server's `admin_give` reducer adds items, refusing non-admins.

## Progress log
- 2026-10-16: PM asked for it (after #254). Built on main:
  - Server `server/spacetimedb/Admin.cs`: `AdminGive(item, count, quality)` refuses non-admins ("Only an admin can give items"), unknown items, counts outside 1..999 (`AdminRules.GiveMax`, shared/AdminRules.cs); raw materials come at flat quality, made things at the quality asked (1..100); logged with the admin's identity. Private `WorldAdmin` (was public; it sent the admin list to every client) plus a per-sender view `my_admin` so a client sees only its own row; `TimeCommands.IsAdmin` now reads the view. Subscriptions adds `my_admin`.
  - First admin: the publisher runs `spacetime call <db> make_admin '"Character Name"'` (existing reducer; the publisher is admin through ClaimConfig.Admin). Documented in docs/ADMIN.md.
  - Client `client/scripts/admin/AdminPanel.cs`: F10 (only for admins), search box, rows from `Items.All` (so new items appear by themselves) with `ItemIcon` where one renders, quantity and quality boxes, Give; Esc closes; Main blocks walking and the Use key while it is open (4 one-line hooks).
  - Verified with a scratch bot against a local server: non-admin give refused; after the publisher's `make_admin`, the bot sees its `my_admin` row and the give works; unknown item and 5000 refused. Upgrade check: publishing over a database from main works (access of world_admin public to private).
  - NOT verified: the look of the panel in the running game (no display here). UX asked for a quick look review.

## Open questions / blockers
- F10 is not in Controls.cs (hidden, admin-only). UX decides whether KEYMAP.md should mention it.

## Next step
Wait for CI on the PR, then send the PM the green sha.

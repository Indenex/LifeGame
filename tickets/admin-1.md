# admin-1: admin chat commands (give, time, season, fly)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/admin-1
PR: (see below)

## Goal
Justin (the world's admin) types chat commands: `/give` (item menu with search, or `/give <item> [n]`), `/time hh:mm|dawn|noon|dusk|night`, `/season <name or 0-1>`, and `/fly` (free 3D movement, no gravity or collision, super fast). The server decides who may (D-182): the admin list is private and only the server's `admin_give` reducer adds items, refusing non-admins.

## Progress log
- 2026-10-16: PM asked for it (after #254). Built on main:
  - Server `server/spacetimedb/Admin.cs`: `AdminGive(item, count, quality)` refuses non-admins ("Only an admin can give items"), unknown items, counts outside 1..999 (`AdminRules.GiveMax`, shared/AdminRules.cs); raw materials come at flat quality, made things at the quality asked (1..100); logged with the admin's identity. Private `WorldAdmin` (was public; it sent the admin list to every client) plus a per-sender view `my_admin` so a client sees only its own row; `TimeCommands.IsAdmin` now reads the view. Subscriptions adds `my_admin`.
  - First admin: the publisher runs `spacetime call <db> make_admin '"Character Name"'` (existing reducer; the publisher is admin through ClaimConfig.Admin). Documented in docs/ADMIN.md.
  - Client `client/scripts/admin/AdminPanel.cs`: F10 (only for admins), search box, rows from `Items.All` (so new items appear by themselves) with `ItemIcon` where one renders, quantity and quality boxes, Give; Esc closes; Main blocks walking and the Use key while it is open (4 one-line hooks).
  - Verified with a scratch bot against a local server: non-admin give refused; after the publisher's `make_admin`, the bot sees its `my_admin` row and the give works; unknown item and 5000 refused. Upgrade check: publishing over a database from main works (access of world_admin public to private).
  - NOT verified: the look of the panel in the running game (no display here). UX asked for a quick look review.

- 2026-10-16: Justin refined it to chat commands (PM): F10 key dropped. `/give` opens the same menu (AdminPanel.Show) or gives directly; `/time` takes hh:mm or dawn/noon/dusk/night (TimeCommands.ParseTime; /season already took names); hook is one line in SocialLayer's chat send (Claims' file). `/fly` comes after #254 merges: it needs a private admin-only flag the server's Move speed budget respects (admin_fly reducer + table), plus client 3D movement (Space up, sneak down, no collision).

## Open questions / blockers
- (none)

## Next step
Wait for CI on #335; after #254 merges, add /fly (admin_fly reducer, Move allowance for flyers, client 3D movement).

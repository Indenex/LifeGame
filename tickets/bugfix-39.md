# bugfix-39: /time, /season and the clock stopped reaching clients
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-39
PR: (to open)

## Goal
Justin (build 1422+): as admin, /fly and /give work but /time and /season (and the rest) do nothing.

## Cause (my regression from admin-1, #335)
admin-1 made `WorldAdmin` a private table. The client's atmosphere subscription (`world_clock`, `weather`, `world_admin`, `craft_work`, `season_clock`, `season_state`) still listed `world_admin`. A subscription fails as a whole when one query names a private table ("no such table: `world_admin`. ... it may be marked private"), so no client received the world clock, weather or season rows at all after that build: the commands reach the server and move the clock, but the game never sees it. Nothing on the client reads `world_admin` (admin status comes from `my_admin`).

Checked: the server reducers `set_time_of_day` and `set_season` change the rows correctly (called as the publisher on a scratch server); a scan of every `FROM table` in client/scripts and tools/ against the module's schema finds `world_admin` as the only private table that is subscribed.

## Change
- The list moves to `Subscriptions.Atmosphere` (client/scripts/net/Subscriptions.cs) without `world_admin`; Atmosphere.cs uses it.
- SpawnCheck subscribes to that list and checks it applies and the world clock and season clock arrive. It goes red with `world_admin` back in the list (tested), green without.

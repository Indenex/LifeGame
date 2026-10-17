# bugfix-50: reducer rate limit (audit item 9 of bugfix-24, D-182)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-50
PR: (not yet)

## Goal
A hacked client could flood the database by calling `PlaceStructure`, `Drop`, `SendChat`, `StartCraft` and the like as fast as it can. Every reducer that acts for a player goes through `RequirePlayer`, which now spends one call from a per-player bucket (`RateLimit.cs`): a burst of 60, refilled at 30 calls a second. When it is empty the call is refused with "Slow down". A refused call throws, so it rolls back, bucket row included: a refusal costs nothing and the bucket keeps refilling with time, so the sustained rate that gets through is the refill rate. `Move` (its own distance budget) and `Heartbeat` (a timer) skip the bucket (`RequirePlayerUnmetered`). Honest play never meets it: a held key repeats about once a second, busy UI a few calls a second. Design principle: the limit only refuses excess calls; nobody is punished.

## Progress log
- 2026-10-17: implemented; SpawnCheck hammers `set_sneaking` 300 times: 240 refused, play carries on after the bucket refills (3 of 3 local runs, 99-102 s).
- 2026-10-17: LoadTest, SocialTest, CraftCheck and BuildTest (both parts) pass locally with no "Slow down" in any of their logs; only SpawnCheck's deliberate hammer is refused (240 of 300).

## Open questions / blockers
- The numbers (burst 60, 30 a second) are constants in `RateLimit.cs`; raise them if a real client ever sees "Slow down".
- Still open from the audit: per-reducer limits for heavy ones (`PlaceStructure`), a cap on world items per player, and a suspicion log for admins (item 14).

## Next step
Wait for CI on the PR, send the green sha to the PM.

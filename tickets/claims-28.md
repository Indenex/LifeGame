# claims-28: The Treasurer keeps the town's store and its upkeep
Lane: Claims
Owner: Claims thread (session_01T6RYk3zyUHiSncD3ZXrFFm)
Status: in_review
Branch: claude/claims-nxkyjn
PR: #839 (with claims-27 and claims-29)

## Goal
Justin picked both Treasurer options (2026-10-17, via the PM): (1) a town store: one shared store at the town's main marker; any townmate puts in; the Treasurer and the Mayor take out; every move logged. (2) upkeep: the Treasurer keeps up any town claim's lifetime from the store, logged.
Built: the main marker is the standing claim on the roster longest (the founder's at first, the next-oldest if it leaves); the store is reached there (the Town panel opened at that marker). New powers as data: `TownPower.Store` (Treasurer, Mayor) and `TownPower.Upkeep` (Treasurer). Upkeep renews a roster claim as if tended, for 1 stick and 1 plant fiber from the store (`Towns.UpkeepSticks/UpkeepFiber`, raisable; a marker costs 4 and 2). Store holds 40 different stacks (`Towns.StoreStacksMax`). A merge pours the joining town's store into the other; a town that closes spills its store at the last signer's marker.

## Progress log
- 2026-10-17: server `TownStore.cs` (table, `my_town_store` view, `PutInTownStore`/`TakeFromTownStore(claimId, item, quality, count)`), hooks in Town.cs (merge, close). Client `TownPanel.Store.cs`: "The town's store" section with a store grid and your bag (click one, Shift+click stack, or drag), wired in SocialLayer.Town.cs; subscribed in SocialLayer.cs. SocialTest: put by a signer, plain signer refused, out-of-reach refused, Mayor takes, both lines in the log. Not built locally (no dotnet here).
- 2026-10-17: Justin picked store + upkeep. Store moved to the main marker; `KeepUpTownClaim(atClaimId, claimId)` and the Treasurer's "Keep up" list in the store section; SocialTest covers main-marker-only, Treasurer election, upkeep needing stock, upkeep renewing B's claim, log lines.

## Open questions / blockers
- (none; upkeep cost 1 stick + 1 fiber is a lane default, sent to the PM)

## Next step
Wait for CI on #839 and send the green sha to the PM.

# claims-28: The Treasurer keeps the town's store
Lane: Claims
Owner: Claims thread (session_01T6RYk3zyUHiSncD3ZXrFFm)
Status: in_progress
Branch: local `claims-28-town-store` (stacked on claude/claims-nxkyjn); checkpoint patches in /mnt/project-files/claims/claims-28/. Pushes to claude/claims-nxkyjn once #839 (claims-27) merges.
PR: (not yet)

## Goal
The Treasurer office had no power ("Powers to come"). Recommended option sent to Justin via the PM on 2026-10-17 (built while he decides; do not merge before his pick): a town store. One shared store per town, reached at the marker of any claim on its roster (the panel opened there). Anyone on the roster puts things in; the Treasurer and the Mayor take them out (new `TownPower.Store`, as data in shared/Towns.cs). Every put and take is a line in the town's log. Max 40 different stacks (`Towns.StoreStacksMax`, raisable). A merge pours the joining town's store into the other; a town that closes spills its store at the last signer's marker, as a falling claim spills its stash.
Other options offered: the Treasurer tops up town claims' lifetime from the store; or a title only.

## Progress log
- 2026-10-17: server `TownStore.cs` (table, `my_town_store` view, `PutInTownStore`/`TakeFromTownStore(claimId, item, quality, count)`), hooks in Town.cs (merge, close). Client `TownPanel.Store.cs`: "The town's store" section with a store grid and your bag (click one, Shift+click stack, or drag), wired in SocialLayer.Town.cs; subscribed in SocialLayer.cs. SocialTest: put by a signer, plain signer refused, out-of-reach refused, Mayor takes, both lines in the log. Not built locally (no dotnet here).

## Open questions / blockers
- Justin's pick for the Treasurer (via PM).

## Next step
After #839 merges: `git checkout -B claude/claims-nxkyjn origin/main`, cherry-pick the claims-28 commit, push, open the PR marked "waiting on Justin's Treasurer pick", check CI.

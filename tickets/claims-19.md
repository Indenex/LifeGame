# claims-19: Town rosters (Justin: "Add neighbors together via a roster everyone signs")
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review (server, rights and tests); panel UI to follow
Branch: claude/claims-19-town-roster
PR: (see below)

## Goal
Neighbouring claim owners form a town. A claim owner starts a roster (a town, with a name). Anyone already on it invites another claim owner, who signs. Everyone who signed has builder rights (build, dig, pick up, gates) on every signed claim; stashes, tending a marker and the fighting rule stay with each claim's owner and members. Leaving the roster, or a claim falling, removes the rights. All decided on the server (D-182).

## Built (this PR)
- `server/spacetimedb/Town.cs`: tables `Town` and `TownSigner` (public), `TownInvite` (private) with view `MyTownInvites`; reducers `StartTown`, `InviteToTown`, `SignTown`, `DeclineTown`, `LeaveTown`, `RemoveFromTown`.
- Rights: `HasClaimRights` (builder: owner, member, or signer of the claim's roster) and the new strict `IsOwnerOrMember` for the stash, tending (`TendClaim`, tending by work) and the "already a member" check. A claim leaves its roster when it is removed (`TownDropClaim` in `RemoveClaim`).
- `shared/Social.cs`: `TownMaxClaims` 12, `TownInviteMicros` 24 h (one raisable constant each).
- Client: `SocialLayer.HasRights` includes town signers (building, gates, land chip, fence snap); marker tending and the claim panel stay owner and members only. Subscriptions added for `town`, `town_signer`, `my_town_invites`.
- SocialTest: a town section at the very end (two fresh claims, since claims last 30 s in CI): sign needs an invitation; invitations private; rights before and after signing and after leaving; no stash and no tending; closing the town.

## Details sent to the PM as options (recommended version built)
1. Who invites: any signer [built] / founder only.
2. Who removes: the founder removes others, anyone leaves [built]; the founder's role passes to the longest-standing signer; the town closes when the last claim leaves.
3. Touching claims required: no [built] / must touch or be within N m of a signed claim. A cap of 12 claims keeps it small.
4. A signer's claim falling removes them from the roster [built].
5. One roster per claim owner [built]; invitations last 24 h.

## Progress log
- 2026-10-17: server and tests done; the server module builds, bindings generate, SocialTest passes on a fresh database; the client also builds locally (`dotnet build client/LifeGame.csproj`, 0 errors; no Godot needed, the Godot SDK comes from NuGet).
- Toolchain recipe is in tickets/claims-13.md; add the client build line above.

## Open questions / blockers
- PM answers on the details above.
- The panel (a "Town" section in the claim panel: start, invite a neighbour, pending invitations with sign or decline, the roster with leave and, for the founder, remove) is a player-visual job for an Opus helper; not built yet. Until then the reducers work but have no UI.

## Next step
Wait for CI on the PR and send the PM the green sha; then brief an Opus helper for the claim panel's Town section.

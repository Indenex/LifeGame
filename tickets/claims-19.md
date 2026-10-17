# claims-19: Town rosters (Justin: "Add neighbors together via a roster everyone signs")
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review (server: roster, rights, Mayor, voting, distance); other offices, merge and panel wait for the PM
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

## Justin's answers (via the PM, 2026-10-17)
1. Invites: any signer. Built.
2. Removal: "There should be ten roles, that everyone votes for. Mayor for instance. Mayor could remove people." Built so far: the Mayor office (the founder starts as Mayor), open-ballot voting, Mayor-only removal. The other nine offices are proposed to the PM and wait for the go-ahead (see the proposal in the PM thread: Mayor, Deputy, Judge, Herald, Warden, Marshal, Treasurer, Gatekeeper, Scribe, Elder; one data row each, D-206).
3. Distance: "They should be nearby, but the idea is cousins can merge into very large towns" (read as towns). Built: a claim may join if its marker is within 60 m of a signed marker (`Social.TownNearMetres`); soft cap 64 claims. Merging two towns by vote of both: proposed, not built.

Vote rules built: a majority of the signers wins at once; otherwise the term's end (7 days, `Social.TownTermMicros`) goes to the most votes (a tie to the longest-standing signer); no votes means the holder stays; a vacant seat gets 24 h. The hourly `TownTerms` reducer (RequireServer) settles terms.

## Approved (Justin via the PM, 2026-10-17)
Ten offices, open voting, 60 m chains with a soft cap of 64, merge by a majority of each town. Panel look C: map first (a small top-down map of the town's claims; click a circle for owner and role; click a neighbouring unsigned claim to invite them); roster, votes and invitations beside it; painted UI style; Opus helper; send Render stills when it runs.

## Built since
- `shared/Towns.cs`: the ten offices as data (name, one-line job, `TownPower` flags, stand-in). v1 powers: Mayor (remove, rename, merge, announce, clear ruins), Deputy (stands in for a vacant Mayor), Judge (breaks a tied election), Herald (announce), Warden (clear ruins); Marshal, Treasurer, Gatekeeper, Scribe, Elder are titles for now.
- Server reducers: `StartTown`, `InviteToTown(playerId)`, `SignTown(townId)`, `DeclineTown(townId)`, `LeaveTown`, `RemoveFromTown(playerId)`, `VoteTownOffice(office, candidatePlayerId)`, `RenameTown(name)`, `AnnounceToTown(text)`, `ClearTownRuin(structureId)`, `ProposeTownMerge(otherTownId)`, `VoteTownMerge(mergeId, yes)`. Tables: `Town`, `TownSigner`, `TownOffice`, `TownVote`, `TownMerge`, `TownMergeVote` (public), `TownInvite` (private, via view `MyTownInvites`), `TownVoice`, `TownTermTimer`.
- SocialTest covers merge, rename, announce (and its rate limit), a second office (Deputy), Mayor election and removal. (An unrelated trade-open timing flake showed up once in my local runs; it passed on the re-run.)


- 2026-10-17: server and tests done; the server module builds, bindings generate, SocialTest passes on a fresh database; the client also builds locally (`dotnet build client/LifeGame.csproj`, 0 errors; no Godot needed, the Godot SDK comes from NuGet).
- Toolchain recipe is in tickets/claims-13.md; add the client build line above.

## Open questions / blockers
- PM go-ahead on the other nine offices, the merge rule and the panel look (2 to 3 written descriptions sent).
- The panel (a "Town" section in the claim panel: start, invite a neighbour, pending invitations with sign or decline, the roster with leave and, for the founder, remove) is a player-visual job for an Opus helper; not built yet. Until then the reducers work but have no UI.

## Next step
The Opus helper builds the map-first Town panel (branch claude/claims-19-town-panel, stacked on this one); wait for CI on #469 and send the PM the green sha.

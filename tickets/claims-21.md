# claims-21: Town office powers (Marshal, Gatekeeper, Scribe, Elder)
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-21-town-powers (stacked on claims-20, PR #555)
PR: (see below)

## Goal
Justin's picks (PM relay, to be logged as D-295): Marshal bans a player from the town's claims for a set time; Gatekeeper bars and unbars the town's gates; Scribe keeps the town log only (announcements stay with the Mayor); Elder breaks tied votes; Treasurer has no power yet.

## Progress log
- 2026-10-17: server: `TownBan` and `TownLog` tables (`TownPowers.cs`); reducers `ban_from_town(playerId, days)` (default 1 day, at most 7, `Towns.BanDaysDefault/BanDaysMax`; a day is `WorldTime.DayLengthSeconds`), `lift_town_ban`, `write_town_log`. A banned player loses `HasClaimRights` and `IsOwnerOrMember` on the town's claims, never on their own claim. Expired bans swept hourly. The Mayor and the Marshal themselves cannot be banned. The town log gets lines automatically (signed, left, offices, rename, ruins, merge, bans), kept to 200 per town.
- Elder: `TownPower.BreakTies` (shared with the Judge). Gatekeeper: `TownPower.Gates`. Scribe: `TownPower.Log`. `TownPower` widened to ushort (not stored anywhere).
- Gates: barring a gate on a claim (`ToggleLock`) is for the claim's owner and members and the town's Gatekeeper. Plain roster signers can still open and shut a neighbour's gate, but no longer bar it. Flagged to the PM as a small narrowing of claims-19's "gates".
- Client: Town panel card gets "Ban for a day" / "Lift the ban" for the Marshal, a log section with the latest 6 lines (and a note box for the Scribe) and the live bans. Layout unseen in Godot, like the rest of the panel.
- SocialTest passes locally on a fresh database; client builds.

## Open questions / blockers
- Gatekeeper reading ("for everyone"): built as above. Alternative if Justin means outsiders: an open/barred town-gate flag outsiders can pass. Ask Justin.

## Next step
Wait for CI on the PR, send the green sha to the PM, then stop pushing.

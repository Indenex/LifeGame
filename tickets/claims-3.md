# claims-3: Player fighting (PvP)
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/50

## Goal
Players can strike each other in the wild with the Strike key (left click / Z, shared with Animals). A strike is refused on claimed land unless the owner allows fighting, in the waking clearing (20 m around spawn), and against newcomers for their first 10 minutes. Striking someone ends your own grace. Players have 100 health, which shows only as a soft ring once hurt and refills slowly after 10 s without a hit, if fed and watered. Being struck down is the same death as hunger or cold: everything carried drops, you wake at the wilderness edge, and you keep knowledge and skills. Nearby players see "X was struck down by Y" in chat, and the one who struck them down shows a red name to others for 30 minutes (D-053). Being hurt shows as a soft dark-red screen edge instead of a health ring. Damage by the best weapon carried: fists 8.5 (about 12 hits), flake 12, hand axe 17 (about 6), spear 20.

## Progress log
- 2026-10-15 23:45: PvP plan sent to the PM. Justin's card asks "as proposed, harsher, mark killers, or change". Server side is built on the recommended values: `server/spacetimedb/Combat.cs` (PlayerHealth, StrikeEvent event table, CombatConfig, StrikePlayer, SetNewcomerGrace), constants in `shared/Social.cs`, `SayNearby` world lines in Chat.cs, and an `EnsureHealth` call after SurvivalConnected in both PlayerConnected paths (Reducers.cs). Death goes through Survival's `KillPlayer(ctx, victim, "slain")`, and fed/watered comes from `NeedsNow` (survival-3, on main). SocialTest has 8 new fighting checks and all pass locally. CI sets the newcomer grace to 0 before SocialTest.
- 2026-10-16 01:10: Justin chose "mark killers" (D-053): red name for 30 min after striking someone down (`PlayerHealth.MarkedUntil`, `Avatar.SetNameTint`). Client: strike offer on Animals' dispatch, flinch on StrikeEvent, hurt edge, unattributed world lines in chat. Survival's KillPlayer("slain") and NeedsNow. SocialTest: all checks pass locally.

## Decisions / defaults
- Recommended values above are the PM's card option "as proposed". Change them only in `shared/Social.cs`.
- Schema is additive (new tables only).

## Open questions / blockers
- When UX #46 lands: ClaimPanel close hint uses `$"{Controls.KeyFor(\"claim_open\") ?? \"K\"} or Esc to close"` (leave ChatPanel's Enter text as is).
- Done: land chip on Hud.Soft and Panels.Open() (#45); Strike dispatch from Animals (#23) registered as "claims.player".

## Next step
Wait for CI on the claims-3 PR; when it's green, send the head sha and the screenshots to the PM.

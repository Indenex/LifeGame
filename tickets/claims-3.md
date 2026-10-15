# claims-3: Player fighting (PvP)
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_progress (rules waiting on Justin's card from the PM)
Branch: claude/claims-chat-pvp-vk2qak
PR: (not opened yet; it opens once Justin answers)

## Goal
Players can strike each other in the wild with the Strike key (left click / Z, shared with Animals). A strike is refused on claimed land unless the owner allows fighting, in the waking clearing (20 m around spawn), and against newcomers for their first 10 minutes. Striking someone ends your own grace. Players have 100 health, which shows only as a soft ring once hurt and refills slowly after 10 s without a hit, if fed and watered. Being struck down is the same death as hunger or cold: everything carried drops, you wake at the wilderness edge, and you keep knowledge and skills. Nearby players see "X was struck down by Y" in chat. Damage by the best weapon carried: fists 8.5 (about 12 hits), flake 12, hand axe 17 (about 6), spear 20.

## Progress log
- 2026-10-15 23:45: PvP plan sent to the PM. Justin's card asks "as proposed, harsher, mark killers, or change". Server side is built on the recommended values: `server/spacetimedb/Combat.cs` (PlayerHealth, StrikeEvent event table, CombatConfig, StrikePlayer, SetNewcomerGrace), constants in `shared/Social.cs`, `SayNearby` world lines in Chat.cs, and an `EnsureHealth` call after SurvivalConnected in both PlayerConnected paths (Reducers.cs). Death calls Survival's `Die` directly (same partial class). SocialTest has 8 new fighting checks and all pass locally. CI sets the newcomer grace to 0 before SocialTest.

## Decisions / defaults
- Recommended values above are the PM's card option "as proposed". Change them only in `shared/Social.cs`.
- Schema is additive (new tables only).

## Open questions / blockers
- Justin's answer on the PvP card (from the PM).
- When survival-3 lands: switch Combat.cs to Survival's `KillPlayer(ctx, victim, "slain")` and `NeedsNow(ctx, id)` (stable names from the Survival lane) instead of calling Die/Snapshot directly.
- When UX #33 and ui-4 land: land chip uses Hud.Soft and fades out while `Panels.Open()` is true (UX asked; playtest found it showing through the journal). Keep ClaimPanel a direct child of SocialLayer with public IsOpen (UX's Main reads it).
- Strike key: Animals owns the dispatch (client/scripts/animals/Strike.cs, lands with animals-3). Register `Strike.Register("claims.player", (at, facing) => StrikeOffer?)` for the nearest player in front within 1.6 m; Animals moves its weapon-carry check into its own offer so fists work for PvP.

## Next step
Build the client part (strike dispatch with Animals, flinch on StrikeEvent, health ring, world chat lines without a speaker), then open the PR once Justin's answer is in.

# claims-1: Claims, proximity chat and PvP rules
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: done (merged via PR #8)
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/8

## Goal
Proximity text chat; land claims via a marker structure (placed with House building's placement system) that decays if not maintained; shared storage permissions; PvP inside a maintained claim needs the owners' permission. Done when two clients chat nearby, a claim blocks outsiders from digging or taking items, and an abandoned claim decays.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15: server Chat.cs + Claims.cs, shared/Social.cs, client/scripts/social/ (chat log, speech bubbles, marker with ring and stakes, claim panel with members/stash/PvP toggle, land chip). Hooks: RequireClaimAccess in Dig and PickUp, InitChat/InitClaims in Init, SocialLayer in Main, Hud helpers internal.
- 2026-10-15: tools/SocialTest (two bots, 19 checks incl. decay) passes locally; added to CI. Draft PR #8 opened.
- 2026-10-15: claim marker is now StructureKind.ClaimMarker (one stage, 4 sticks + 2 fiber) placed and built through Building; the claim starts when it is finished. Hooks in Building.cs: CheckStructureSite, OnStructureComplete, OnStructureRemoved; StructureModel draws its blueprint via social/ClaimMarkerModel. Dismantling ends the claim; a fallen claim removes the marker. SocialTest (23 checks) and BuildTest pass. First in-game screenshot taken (chat log + bubble + land chip); UI overlap fixed.
- 2026-10-15: merged main (house building #6). Added CheckClaimAccess, so placing or dismantling a structure in someone's claim is refused. Keys moved off building's B/T: K opens the claim panel, L raises a marker for now. BuildTest and SocialTest both pass locally.
- 2026-10-15: Justin approved merging PR #8. Merged main again (survival); claim keys moved to B/T since survival uses C/R. Claim radius set to 20 m (D-023); spawn keep-out is now 20 m + radius so no claim reaches the clearing.
- 2026-10-15: merged main (state files). CI green on 959721b, SocialTest included: https://github.com/Indenex/LifeGame/actions/runs/38809337277. PR marked ready; merge asked of PM.
- 2026-10-15 22:20: merged main (ceramics #11, PM #14) as d367547; CI run https://github.com/Indenex/LifeGame/actions/runs/38815757226 green (server, Linux, Windows); PR #8 merged as a merge commit under D-032.

## Decisions / defaults
- Keys follow KEYMAP.md (D-025): Enter chat, K claim panel. The marker is laid out from House building's B menu.
- Marker cost 4 sticks + 2 fiber; claim radius 20 m (Justin, D-023); 1 claim per player; no claims within 20 m of spawn; marker stands 3 days untended; PvP off inside claims unless owner allows; wild is open PvP; chat range 24 m.

## Open questions / blockers

## Next step
None for claims-1. A follow-up claims ticket comes from the PM.

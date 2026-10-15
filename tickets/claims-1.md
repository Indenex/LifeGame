# claims-1: Claims, proximity chat and PvP rules
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/8

## Goal
Proximity text chat; land claims via a marker structure (placed with House building's placement system) that decays if not maintained; shared storage permissions; PvP inside a maintained claim needs the owners' permission. Done when two clients chat nearby, a claim blocks outsiders from digging or taking items, and an abandoned claim decays.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15: server Chat.cs + Claims.cs, shared/Social.cs, client/scripts/social/ (chat log, speech bubbles, marker with ring and stakes, claim panel with members/stash/PvP toggle, land chip). Hooks: RequireClaimAccess in Dig and PickUp, InitChat/InitClaims in Init, SocialLayer in Main, Hud helpers internal.
- 2026-10-15: tools/SocialTest (two bots, 19 checks incl. decay) passes locally; added to CI. Draft PR #8 opened.
- 2026-10-15: merged main (house building #6). Added CheckClaimAccess, so placing or dismantling a structure in someone's claim is refused. Keys moved off building's B/T: K opens the claim panel, L raises a marker for now. BuildTest and SocialTest both pass locally.
- 2026-10-15: Justin approved merging PR #8. Merged main again (survival); claim keys moved to B/T since survival uses C/R. Claim radius set to 20 m (D-023); spawn keep-out is now 20 m + radius so no claim reaches the clearing.
- 2026-10-15: merged main (state files). CI green on 959721b, SocialTest included: https://github.com/Indenex/LifeGame/actions/runs/38809337277. PR marked ready; merge asked of PM.

## Decisions / defaults
- Keys follow KEYMAP.md (D-025): Enter chat, K claim panel. Raising a marker is on L until it moves into the B build menu.
- Marker cost 4 sticks + 2 fiber; claim radius 20 m (Justin, D-023); 1 claim per player; no claims within 20 m of spawn; marker stands 3 days untended; PvP off inside claims unless owner allows; wild is open PvP; chat range 24 m.

## Open questions / blockers
- Marker placement should move onto House building's PlaceStructure as a new StructureKind once PR #6 merges. Today PlaceClaim places at the player's feet; the code is isolated in Claims.cs PlaceClaim.

## Next step
Merge PR #8 once CI is green on the merged head; then follow-up PR: claim marker as a StructureKind in the B build menu (PlaceStructure), item id from 220-239 if needed.

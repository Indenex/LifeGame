# claims-1: Claims, proximity chat and PvP rules
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/8 (draft)

## Goal
Proximity text chat; land claims via a marker structure (placed with House building's placement system) that decays if not maintained; shared storage permissions; PvP inside a maintained claim needs the owners' permission. Done when two clients chat nearby, a claim blocks outsiders from digging or taking items, and an abandoned claim decays.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15: server Chat.cs + Claims.cs, shared/Social.cs, client/scripts/social/ (chat log, speech bubbles, marker with ring and stakes, claim panel with members/stash/PvP toggle, land chip). Hooks: RequireClaimAccess in Dig and PickUp, InitChat/InitClaims in Init, SocialLayer in Main, Hud helpers internal.
- 2026-10-15: tools/SocialTest (two bots, 19 checks incl. decay) passes locally; added to CI. Draft PR #8 opened.
- 2026-10-15: merged main (state files). CI on first push: clients green, server job running.

## Decisions / defaults (awaiting Justin via PM)
- Marker cost 4 sticks + 2 fiber; claim radius 10 m (PM recommended ~20 m); 1 claim per player; no claims within 20 m of spawn; marker stands 3 days untended; PvP off inside claims unless owner allows; wild is open PvP; chat range 24 m.

## Open questions / blockers
- Claim radius: 10 m (default) vs ~20 m (PM recommendation). Sent to PM.
- Marker placement should move onto House building's PlaceStructure as a new StructureKind once PR #6 merges. Today PlaceClaim places at the player's feet; the code is isolated in Claims.cs PlaceClaim.

## Next step
Get CI green on PR #8 and post in-game screenshots; then switch the marker to PlaceStructure when PR #6 is on main.

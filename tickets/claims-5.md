# claims-5: How long a claim stands, and tending by working the land
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_progress
Branch: claude/claims-chat-pvp-vk2qak
PR: (not opened yet)

## Goal
Players can see how long an untended claim has left. The claim panel reads "Tended 2 days ago · stands 5 more days", and walking up to any marker shows the same calm line floating softly above it, with the owner's name on someone else's marker. Owners and members tend their claim just by working inside it (digging, building, picking up, firing pots), not only with G at the marker, so a lived-in claim never falls.

## Progress log
- 2026-10-16 03:55: started from main 250a715 (PM asked, after #56 merged).

## Decisions / defaults
- Work that tends: anything that already passes the claim-access check (`RequireClaimAccess`/`CheckClaimAccess`) by someone with rights to the claim. Picking things up counts.
- At most one write per claim per minute from work (or a quarter of the lifetime, if that's shorter, for CI's 30 s claims), so busy work doesn't churn the claim row.
- The line under the marker shows within 5 m of it, to anyone.

## Open questions / blockers
- When UX #46 lands: ClaimPanel close hint uses `$"{Controls.KeyFor(\"claim_open\") ?? \"K\"} or Esc to close"`.

## Next step
Add tending-by-work in Claims.cs, the shared "tended … stands …" text in shared/Social.cs, the panel line and the marker label, then SocialTest checks.

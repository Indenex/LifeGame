# claims-4: Add claim members by name
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: in_progress
Branch: claude/claims-chat-pvp-vk2qak
PR: (not opened yet)

## Goal
A claim owner can add a member by typing their name in the claim panel, whether that person is nearby, far away or offline. Members can already build on the claim and lock and unlock its doors (D-048). When someone is added, a calm chat line appears near the marker ("Tobin now shares Justin's land"), and the new member hears it too wherever they are. The nearby "Invite" buttons stay.

## Progress log
- 2026-10-16 02:30: `AddClaimMemberByName` (Claims.cs, shares `AddMember` with the nearby Invite), name box and Add button in ClaimPanel.cs (kept across panel rebuilds; SocialLayer.Typing covers it so Main stops walking while typing; Esc stops typing). No schema change. SocialTest: 4 new checks (unknown name, add by name in any case, the chat line, adding twice); all social checks pass locally.
- 2026-10-16 02:10: started from main d486a87. Plan: `AddClaimMemberByName(string name)` reducer in Claims.cs, a name box and Add button in ClaimPanel.cs, chat lines via SayNearby, SocialTest checks.

## Decisions / defaults
- Names are not unique. Default: matching ignores case and surrounding spaces; if several players share the name and exactly one is online, add that one; otherwise refuse with "More than one person goes by X. Invite them in person, near the marker."
- Unknown name: "Nobody called X has been here."

## Open questions / blockers
- When UX #46 lands: ClaimPanel close hint uses `$"{Controls.KeyFor(\"claim_open\") ?? \"K\"} or Esc to close"`.

## Next step
Open the PR, wait for CI, send the green sha and a claim panel screenshot to the PM.

# claims-4: Add claim members by name
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session cse_01CfATdbRNPF9XXuAmTui6jb)
Status: done (merged via PR #56)
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/56

## Goal
A claim owner can add a member by typing their name in the claim panel, whether that person is nearby, far away or offline. Members can already build on the claim and lock and unlock its doors (D-048). When someone is added, a calm chat line appears near the marker ("Tobin now shares Justin's land"), and the new member hears it too wherever they are. The nearby "Invite" buttons stay.

## Progress log
- 2026-10-16 02:30: `AddClaimMemberByName` (Claims.cs, shares `AddMember` with the nearby Invite), name box and Add button in ClaimPanel.cs (kept across panel rebuilds; SocialLayer.Typing covers it so Main stops walking while typing; Esc stops typing). No schema change. SocialTest: 4 new checks (unknown name, add by name in any case, the chat line, adding twice); all social checks pass locally.
- 2026-10-16 02:10: started from main d486a87. Plan: `AddClaimMemberByName(string name)` reducer in Claims.cs, a name box and Add button in ClaimPanel.cs, chat lines via SayNearby, SocialTest checks.

- 2026-10-16 03:50: SocialTest tends the claim before fighting (decay race, with #59's guard kept). Claim panel screenshot: /mnt/project-files/screenshots/claims-05-add-member-by-name.png. CI green on aff9812: https://github.com/Indenex/LifeGame/actions/runs/38846633596. Merged by the PM (#56).

## Decisions / defaults
- Names are not unique. Default: matching ignores case and surrounding spaces; if several players share the name and exactly one is online, add that one; otherwise refuse with "More than one person goes by X. Invite them in person, near the marker."
- Unknown name: "Nobody called X has been here."

## Open questions / blockers
- When UX #46 lands: ClaimPanel close hint uses `$"{Controls.KeyFor(\"claim_open\") ?? \"K\"} or Esc to close"`.

## Next step
None; take the next claims ticket from the PM.

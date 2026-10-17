# claims-17: Claims for towns (new players find claiming; neighbouring claims work)
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_progress
Branch: claude/claims-17-neighbours
PR: (see below)

## Goal
House-type buildings become claim-only (House building's #362), so claiming is how a player gets to build a home. A new player must be able to find out how to claim, and neighbouring claims must make towns easy (Justin: "players cooperate and build cities").

## Survey (2026-10-17, read of main)
- Discovery: the first-evening steps (journal `NextSteps.cs`) have the line for `claim.mark`: "A marker of sticks and fiber, raised away from the waking clearing, makes the land around it yours." The wiki page building-and-claims.md covers it. Not yet checked: what #362 says to a player who tries a house outside a claim (it should say how to claim).
- Placement rules (Claims.CheckStructureSite): not in the waking clearing; one claim per player (`MaxClaimsPerPlayer` 1); markers at least 2 x ClaimRadius = 40 m apart, so neighbouring claims may touch at the edge but never overlap. Three claims in a triangle leave an unclaimed pocket between them, where only the basics may stand and everything weathers after 48 h.
- Rights are per claim: owner plus a member list the owner fills by name (`AddClaimMemberByName`). For a town of N households, building across each other's land means each owner adds each neighbour by hand: N x (N-1) additions.
- A player who is refused a marker site got "Too close to X's claim" with no hint how far to move.

## Done in this PR
- The refusal now says how far to move: "Too close to X's claim. Move 12 m further from their marker, or the edges just touch at 40 m". SocialTest checks it.

## Proposed for Justin (sent to the PM, 2 to 4 options)
How should neighbours share building rights across claims?
(a) [recommended] Keep as is for now, add a one-click "add all my neighbours" in the claim panel that lists the owners of touching claims and adds the ones you pick. Small, no rule change; every add is still an explicit member grant by the owner.
(b) A "neighbour pact": two owners agree once and each gains builder rights (build, dig, gates; not stash or PvP) on the other's claim. Smoother, but a new rule with new states to explain.
(c) A town claim: owners pool markers into one shared, bigger claim. Biggest change.

## Progress log
- 2026-10-17: survey and refusal message done; server builds; SocialTest passes locally on a fresh database; client not built here.

## Open questions / blockers
- Justin's pick on (a)/(b)/(c) via the PM.
- Check #362's message for house-outside-claim once it is on main.

## Next step
Wait for CI on the PR; send the PM the green sha, then do the pick (and the #362 message check).

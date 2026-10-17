# claims-26: A banned player is told why
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-26-ban-message
PR: #815

## Goal
A player under a Marshal's ban who tries to build, dig, take or lay a station on the town's claims is told "You are kept off <town>'s claims for about N more (min or h)" instead of the generic "This land is claimed by <owner>".

## Progress log
- 2026-10-17: `BanRefusal(ctx, claim, who)` in `TownPowers.cs` (null when no live ban applies); used by the three claim refusals (`RequireClaimAccess` in Claims.cs, `CheckClaimOnly` in Building.cs, the station check in Stations.cs). The time left is real time (minutes under an hour, else rounded hours). SocialTest's banned-A refusal now expects "kept off" and passes locally on a fresh database ("about 10 h more" for the 7-day cap). No schema change.

- 2026-10-17: PR #815 opened; head 06c83e1d. CI run https://github.com/Indenex/LifeGame/actions/runs/39040643041: Client (Linux/Windows), Build server module, Upgrade, Crafting (tools), all Building checks green; "Load, spawn and social checks" and "Crafting check (stations)" were still running at wrap-up. Green sha goes to the PM (D-119) once the roll-up passes. This ticket-only update is the last push.
- Lane state at wrap-up: claims-12 to claims-25 are merged (fences, chat, weathering, towns with offices, terms, ban/log/gates/Scribe, per-character ban test, ClaimGoodsOpenAt). Tickets claims-24 and claims-25 say `PR: (see PR list)`: they are #696 and #810, both merged.

## Open questions / blockers
- Wording of the ban refusal is the PM's/Justin's call (3 options offered; built the recommended one: say why, with the time left). Not blocking.
- Waiting on other lanes: Render's stills of the Town panel (the panel has never run in Godot; fix any layout problems they show). Untested in a real run: a roster signer failing to bar a gate, the Gatekeeper succeeding, the Elder breaking a tie with no Judge (need a second claim plus a town in one BuildTest run; House building has no helper and advised against adding one).
- Waiting on Justin: the Treasurer has no power yet (his pick).

## Next step
Check #815's CI (run above). If green, send the sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU); if only a BuildTest gathering stall is red, say so on the PR and ask the PM to re-run once (lanes do not re-run CI). Then ask the PM for the next Claims item (D-145) or check Render's Town-panel stills.

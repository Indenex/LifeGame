# claims-6: Speech you can see
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: https://github.com/Indenex/LifeGame/pull/71

## Goal
Chat feels like people talking nearby (immersion, D-036). When someone close by says something, a soft bubble rises above their head and fades after a few seconds; if they say more, the older lines drift up and dim instead of vanishing (at most three). In the chat log, a line from farther away fades in dimmer. "/me waves" is an action: it shows in italics, as "Mira waves" in the log and "waves" in a slanted bubble. Nothing pops in or out.

## Progress log
- 2026-10-16 17:16: CI green on c126163 (run https://github.com/Indenex/LifeGame/actions/runs/38937002623). Green sha sent to PM for merge.
- 2026-10-16: CI green on 8098543 (run https://github.com/Indenex/LifeGame/actions/runs/38860350802). Merged main 7c65c0f (no overlap with social files); pushed for a fresh CI run. Render requested 8098543.
- 2026-10-16 05:05: built. `Social.IsEmote/EmoteBody/ChatDim` (shared/Social.cs); SendChat normalises "/ME x" to "/me x" and drops an empty "/me"; bubbles stack up to three (older lift via Label3D offset and dim via transparency), actions use a slanted FontVariation; ChatPanel takes dim and emote. No schema change. SocialTest: 3 new checks; all social checks pass locally.
- 2026-10-16 04:45: started from main (PM asked after #65 merged).

## Decisions / defaults
- Log dimming: full brightness within a third of chat range (8 m), easing to 55% at the edge (24 m).
- Bubbles stack up to three per speaker; older ones dim to 55% and lift by the new bubble's height.
- "/me" with nothing after it sends nothing. "/ME" works the same.

## Open questions / blockers
- (none)

## Next step
None once the PM merges #71; claims-7 (trading) continues on claude/claims-7-hand-item.

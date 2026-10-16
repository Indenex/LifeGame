# claims-8: "Wants to trade" over the asker's head
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-chat-pvp-vk2qak
PR: (not yet)

## Goal
When someone asks you to trade, a soft slanted "wants to trade" fades in over their head, in the same style as chat bubbles. It fades away when you answer, when they cancel, or when the request lapses (30 s). Only the asked player sees it (D-080: bystanders see nothing). Client-only, with no server change. It sits alongside UX/UI's ui-9 request prompt.

## Progress log
- 2026-10-16 18:40: client builds. Pushed; render requested.
- 2026-10-16 18:35: started on the default while the PM decides between claims-8 options (1 trade cue [recommended], 2 whisper/shout, 3 name your camp, 4 stash log). Written in SocialLayer.Trading.cs (RefreshTradeCue on my_trade changes).

## Open questions / blockers
- Sent to PM 2026-10-16: the claims-8 options above.

## Next step
Open the claims-8 PR once the PM confirms option 1 (or switch to the option they pick).

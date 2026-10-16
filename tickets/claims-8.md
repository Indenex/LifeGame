# claims-8: "Wants to trade" over the asker's head
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_review
Branch: claude/claims-chat-pvp-vk2qak
PR: (not yet)

## Goal
When someone asks you to trade, a soft slanted "wants to trade" fades in over their head, in the same style as chat bubbles. It fades away when you answer, when they cancel, or when the request lapses (30 s). Only the asked player sees it (D-080: bystanders see nothing). Client-only, with no server change. It sits alongside UX/UI's ui-9 request prompt.

## Progress log
- 2026-10-16 18:45: D-124, Justin picked this option. The cue now retries until the asker's avatar has loaded. Merged main; build passes and SocialTest passes locally. Opening the PR.
- 2026-10-16 18:40: client builds. Pushed; render requested.
- 2026-10-16 18:35: started on the default while the PM decides between claims-8 options (1 trade cue [recommended], 2 whisper/shout, 3 name your camp, 4 stash log). Written in SocialLayer.Trading.cs (RefreshTradeCue on my_trade changes).

## Open questions / blockers
- (resolved) D-124: Justin picked the trade cue.

## Next step
Wait for CI on the claims-8 PR; when it's green, send the head sha to Integration (D-095) and the PM.

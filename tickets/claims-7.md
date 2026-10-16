# claims-7: Hand an item to someone
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-7-hand-item (stacked on claude/claims-chat-pvp-vk2qak while PR #71 is open)
PR: (not yet)

## Goal
Players can help each other directly. Face a player within arm's reach and press G ("Give to Tam"): a small list of what you carry opens; pick one (or the whole stack). Tam gets a soft note "Mira offers you a flint · G to take" and, facing Mira, G says "Take the flint from Mira". Nothing moves until Tam takes it; the offer lapses after a minute or if either walks away. When it's taken, one chat line nearby says "Mira hands Tam a flint".

## Progress log
- 2026-10-16 17:15: started. D-044 is fully built, so I sent the PM options for claims-7, with this one recommended; building it on that default.

## Decisions / defaults
- The item moves only when the receiver takes it. The giver can cancel by walking away or offering again.
- Give reach is 3 m (the server allows 4 m to cover lag). An offer lasts 60 s. A giver has one open offer at a time; a new offer replaces the old one.
- No weight check: carrying more only slows you down (survival-3).
- Use handlers are registered from Claims' own files, so no Integration hook is needed. No new key: it's G (Use).

## Open questions / blockers
- Sent to PM 2026-10-16: claims-7 options (1 hand an item [recommended], 2 whisper/shout, 3 name your camp, 4 stash log).

## Next step
Write server/spacetimedb/Giving.cs (GiftOffer table, MyGiftOffers view, OfferGift/AcceptGift/DeclineGift reducers), then the client GivePanel in client/scripts/social/.

# ui-9: Trade window
Lane: UX and UI
Owner: UX and UI thread (session session_01V6qgbNbvTwniqW946KACLr)
Status: in_progress
Branch: claude/ux-ui-trade-xlrh2s (stacked on claude/ux-ui-xlrh2s while #73 waits)
PR: (none yet)

## Goal
Two players can trade carried items face to face (D-069). Use (G) on another player asks to trade; they see a request prompt and accept or decline. The window shows both offers side by side; you add items from what you carry. Each side has an Accept button; any change to either offer clears both accepts, and that is clearly visible. A warning shows if the trade would put you over your carry limit. Esc, walking away or death cancels. The client lives in `client/scripts/ui/`; the server side is claims-7 (Claims lane, session_013ECn821EbTpbj4h7d3PepH), which sends the table and reducer contract. Until it lands, the window runs on mock data.

## Progress log
- 2026-10-16 17:16: assigned by the PM. Started the branch from ui-8 (6dd68f5).

- 2026-10-16 17:45: window built against a mock. `ui/Trade.cs` holds `ITradeLink` (the contract the window needs) and `MockTradeLink` (Mara). `ui/TradeWindow.cs` draws the window: both offers side by side, item pictures, an Accept button, green borders and ticks when a side accepts, an amber "The offer changed, so both accepts were cleared" line, a carry-limit warning from `Carry.Of`, and Esc, walking 5 m apart or death to call it off. G on a player within 2.5 m asks; the asked player sees "Mara wants to trade    Esc says no" above the G prompt, and G says yes. Main has a small hook (players, Me, Dead, Panels.Open). Until claims-7 lands, G on a player only does anything with `--mock-trade` or `--screenshot-trade`. Previews: `--screenshot-trade`, `=asked`, `=accepted`, `=changed`. Client builds with 0 errors.

## Open questions / blockers
- Server contract from Claims (claims-7) not received yet; building against a mock meanwhile.

## Next step
Send Render the mockup request for the pushed sha; when Claims sends the claims-7 contract, write a real ITradeLink over the bindings.

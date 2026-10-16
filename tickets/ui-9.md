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
- 2026-10-16 17:50: Render requested 119c068 (four previews). Sent Claims (session_013ECn821EbTpbj4h7d3PepH) the calls and reads the window needs, plus the assumptions in Open questions.
- 2026-10-16 18:05: claims-7 contract received (branch claude/claims-7-hand-item, 857662f, tickets/claims-7.md "Contract"). Reshaped the client to match: one `Set` call with an absolute count (SetTradeItem), Accept carries the version, and the trade has an id. The server owns the apart, death and lapse checks, so the client-side ones are gone, and so is my G handler for asking (Claims' `claims.trade` does it). Asked Claims for `AcceptTrade(tradeId, version, bool accept)` so you can take an Accept back. Build 0 errors.

## Open questions / blockers
- Asked Claims 2026-10-16: can AcceptTrade take a bool so you can take an Accept back? Until they answer, the window keeps a take-back button.
- Bindings for claims-7 not pushed yet; the window still runs on MockTradeLink.

## Next step
When Claims pushes claims-7 with bindings, write ServerTradeLink (ITradeLink over my_trade, my_trade_items, trade_ended and the five reducers) and make Main use it.

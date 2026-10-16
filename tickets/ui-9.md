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

## Open questions / blockers
- Server contract from Claims (claims-7) not received yet; building against a mock meanwhile.

## Next step
Build the trade window against a mock trade model in client/scripts/ui/, then send Render a mockup request.

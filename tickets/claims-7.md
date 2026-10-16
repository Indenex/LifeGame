# claims-7: Trading between players (server side)
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-7-hand-item
PR: (not yet)

## Goal
Justin wants trading (D-069). Claims builds the server side and the "Trade with Tam" G prompt; UX/UI builds the trade window as ui-9 (session_01V6qgbNbvTwniqW946KACLr). Done means:
- Two players near each other can trade, and either side may be empty (a gift).
- Each adds items from what they carry, then both press Accept. Any change to either offer clears both accepts.
- The swap happens all at once, checked by the server.
- The trade is cancelled if they move more than 4 m apart, if either presses Esc, or if either dies.
- Bystanders see nothing in chat when a trade finishes (D-080).

## Contract (for ui-9; names are final unless this section changes)

Reach: `Social.TradeReach = 3f` to start a trade. `Social.TradeBreakDistance = 4f`: the server cancels once the players are farther apart than this. `Social.TradeAskMicros = 30 s` (an unanswered ask lapses). `Social.TradeMaxStacks = 8` per side.

### Tables (server-only; clients read them through views)
`Trade`: one row per trade. A player is in at most one, as A or B.
| field | type | meaning |
|---|---|---|
| Id | ulong PK autoinc | |
| A, B | Identity (indexed) | A asked, B was asked |
| AId, BId | uint | player ids (for avatars and names) |
| AName, BName | string | names when the trade began |
| State | byte | `Social.TradeAsked` = 0 (waiting for B to answer), `Social.TradeOpen` = 1 (both adding items) |
| AAccepted, BAccepted | bool | each side's Accept; cleared by any item change |
| Version | uint | goes up on every item change; Accept must name the version it saw |
| StartedAt | Timestamp | |

`TradeItem`: what each side has put in.
| field | type | meaning |
|---|---|---|
| Id | ulong PK autoinc | |
| TradeId | ulong (indexed) | |
| Side | byte | 0 = A's offer, 1 = B's offer |
| Item | ushort | `LifeGame.Shared.Item` |
| Quality | byte | |
| Count | uint | at least 1 (count 0 deletes the row) |

Items stay in the owner's inventory until the swap; they are not reserved.

### Views and events (subscribe to all three)
- `MyTrade` (`SELECT * FROM my_trade`): the trade you are in, as A or B (0 or 1 rows). Insert means a trade was asked or started; update means a state, accept or version change; delete means it ended (see `TradeEnded`).
- `MyTradeItems` (`SELECT * FROM my_trade_items`): both sides' items for your trade.
- `TradeEnded`: a public event table, `Event = true`. Fields: `TradeId`, `AId`, `BId`, and `Reason` (string): `"done"`, `"declined"`, `"cancelled"` (Esc, or the other player cancelled), `"apart"` (walked apart, died or went offline) or `"lapsed"` (ask unanswered). Clients show only those where they are A or B.

### Reducers
| reducer | who | does | errors (exception text) |
|---|---|---|---|
| `AskTrade(uint playerId)` | anyone | creates a Trade in state Asked, with you as A | "You can't trade with yourself", "They are not here" (offline or unknown), "Step closer to trade" (> TradeReach + 1 m slack), "You are already trading", "They are busy trading" |
| `AnswerTrade(ulong tradeId, bool accept)` | B, while Asked | accept: state Open. decline: deletes it, TradeEnded "declined" | "That trade is gone" |
| `SetTradeItem(ulong tradeId, ushort item, byte quality, uint count)` | A or B, while Open | sets your side's count for that stack (0 removes it); clears both accepts; Version++ | "That trade is gone", "You are not carrying that many", "That's as much as a trade can hold" (more than TradeMaxStacks) |
| `AcceptTrade(ulong tradeId, uint version, bool accept)` | A or B, while Open | accept true: sets your Accept if version matches; when both have accepted, the swap (below). accept false: clears only your own Accept, Version unchanged (asked by ui-9) | "That trade is gone", "The offer changed; look again" (version mismatch), "Nothing to trade" (both sides empty) |
| `CancelTrade(ulong tradeId)` | A or B, any state | deletes it, TradeEnded "cancelled" | none (no-op if gone) |

The swap: in one transaction, recheck reach and that each side still carries every listed stack. If anything is short, the second Accept fails with "<Name> no longer has <item>" and nothing changes. The trade stays open, and the short side fixes it by changing their offer, which clears both accepts. Otherwise the items move, the trade and its items are deleted, and TradeEnded "done" is sent. Nothing is posted in chat (D-080): only the two traders see their own confirmation.

A 1 s server watch runs while any trade exists. It cancels with "apart" when the players are more than TradeBreakDistance apart or either is offline; a death respawns the player far away, so it counts as "apart". It ends asks older than TradeAskMicros with "lapsed".

### Client split
- Claims (SocialLayer.Trading.cs): a G handler `claims.trade` on the nearest other player in reach, "Trade with Tam", which calls AskTrade. It uses `Use.Register` with a Target, the same way strikes find players.
- ui-9 (UX/UI): the ask prompt for B (Accept or Decline, which call AnswerTrade); the trade window (both sides, add or remove from your inventory, Accept and accepted marks, which call SetTradeItem, AcceptTrade and CancelTrade); Esc calls CancelTrade; messages for TradeEnded reasons.
- `SocialLayer.PanelOpen` (already hooked into Main's `Panels.Open` and the PauseMenu `OthersOpen`) can include ui-9's window if UX/UI wants that.

## Progress log
- 2026-10-16 17:45: #71 merged. Merged main in, resolving Main.cs the way Integration asked (kept #73's Panels.Side). Build passes and SocialTest passes all checks locally.
- 2026-10-16 17:40: D-080: removed the bystander chat line; traders get TradeEnded "done".
- 2026-10-16 17:35: ui-9 asked for un-accept, so AcceptTrade now takes `bool accept`. Integration set Priority 5. D-077 recorded.
- 2026-10-16 18:50: server side built to the contract: server/spacetimedb/Trading.cs, Social trade constants, and the claims.trade G handler in SocialLayer.Trading.cs (my_trade, my_trade_items and trade_ended are subscribed there for ui-9). The one-way hand-over code is removed. Local build passes; SocialTest passes all checks (14 trade checks: reach, one at a time, decline, empty, carried only, version, both accept, gift line, change clears accepts, walking apart).
- 2026-10-16 18:20: PM repurposed claims-7 from the one-way hand-over to the server side of trading (D-069). Wrote the contract above first. The earlier hand-over code (Giving.cs, GivePanel, SocialLayer.Giving.cs) will be replaced.
- 2026-10-16 17:55: (one-way hand-over) built, and SocialTest passed locally. Main hook made, with Integration's OK.
- 2026-10-16 17:15: started.

## Decisions / defaults
- PM defaults from D-069 are as in Goal.
- D-077: a trade may overload a player; they walk slower (no hard cap).
- D-080: completed trades post nothing to bystanders. Requests lapse after 30 s.

## Open questions / blockers
- (resolved) Integration: no new hook; claims.trade uses Use.Register with a Target at Priority 5.

## Next step
When PR #71 merges: merge main in, open the claims-7 PR (server side; ui-9 builds the window on it), tell Integration and UX/UI the PR number, then send the PM the green sha.

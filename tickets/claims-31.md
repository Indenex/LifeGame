# claims-31: Trading stall
Lane: Claims
Owner: Claims thread
Status: in_progress
Branch: claude/claims-nxkyjn
PR: #861

## Goal
A trading stall (build menu, Claims tab) stands inside your claim, one per claim. Its owner and
members set out up to 8 lots from their bag, each sold one at a time for an asking price in one
kind of other thing (any quality, at most 99). Anyone who walks up buys, whether or not the owner
is online; the payment waits in the stall's till for the owner and members, and each sale is logged
on the stall ("Wren bought 2 clay pots for 6 sticks"). Taking the stall down spills its lots and till.
Option 1 of three sent to the PM 2026-10-17 (stall / town signpost / friends list); built on the
recommended option until Justin answers.

## Design
- shared/Stalls.cs: numbers. Kind 26 (after Bed 25) in shared/structures/StructuresClaims.cs and StructureKind.
- server/spacetimedb/Stall.cs: StallLot (public), StallTill + StallSale (private, views MyStallTill/MyStallSales for
  owners and members), reducers SetOutOnStall, TakeBackFromStall, BuyFromStall, TakeFromStallTill.
  Claims.cs hooks: CheckStructureSite (one per claim), OnStructureRemoved (SpillStall).
- Model "Stall": needs a Modeling asset; the client falls back to the lean-to until then.

## Progress log
- 2026-10-17: server side and shared def written.
- 2026-10-17: client StallPanel.cs + SocialLayer.Stall.cs (Use: "Look at the stall"), SocialTest stall steps (build, one per claim, price needed, B buys, till for A only, sale logged, A empties the till).

## Open questions / blockers
- Justin's pick among stall / signpost / friends list (asked via the PM).
- Modeling: a stall model (a small wooden counter with a woven awning).

## Next step
Get CI green on #861 (draft until then); then a render request for the stall window and mark #861 ready.

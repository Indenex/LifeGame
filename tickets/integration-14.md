# integration-14: Use dispatcher: second prompt line and quiet hints
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-use-hints
PR: (see STATUS)

## Goal
Crafting's crafting-29 (Justin): a world tool action is offered only with the matching tool equipped; with nothing equipped, E goes to loot and the tree is not offered; with an axe equipped the nearer thing wins and a second dimmer line shows the other option; a soft occasional hint "Equip an axe to chop" appears when looking at a tree with nothing equipped and no loot near. This ticket is the dispatcher side (ui/Use.cs, ui/UseKey.cs); Crafting owns the offers and the hint handler.

## Progress log
- 2026-10-17: `Use.Others(at, facing, best)` (every offer Best passed over, ranked), `Use.RegisterHint/UnregisterHint/Hint` (a hint handler returns text or null; each text shows at most once per 30 s, for 4 s), UseKey shows "also: <prompt>" or the hint as a dimmer second line under the key prompt. Priority between loot and chop: give both handlers the same Priority; the nearer wins, and a thing you face beats one off to the side (already in Best). Client builds, 0 errors.

## Open questions / blockers
- None. Crafting registers `Use.RegisterHint("crafting.equip_axe", at => ...)` in Woodcutting.cs after their PR.

## Next step
Wait for CI; tell Crafting the PR number and the PM the green sha.

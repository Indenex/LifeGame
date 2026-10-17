# crafting-29: World tool actions need the tool equipped
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-equip
PR: (none yet)

## Goal
Justin (via the PM, high priority): "I've noticed when loot is near trees, it's very hard to loot. Make it so for a tool to be used, it needs to be on your bar and equipped."
Rules (from the PM, 2026-10-17):
1. World tool actions need the tool equipped: chop (axe), dig (spade or stick), mine, cut grass and so on only happen when the matching tool is in the selected, equipped hotbar slot. A tool in the bag or on the belt does not count for these. Enforce on the server (D-182): the reducers check the player's equipped slot, not just inventory.
2. No tool equipped (or a non-tool) means interaction goes to looting: E near a tree picks up nearby loot (items, sticks, stones, berries) and never offers the tree action, and the prompt shows the loot. With an axe equipped near a tree, the prompt offers chopping, but loot that is clearly closer or under the cursor still wins; a second prompt line can show the other option.
3. Gentle hint: looking at a tree with nothing equipped and no loot nearby shows a soft hint, "Equip an axe to chop", once in a while, not every time.
4. Crafting recipes that need a tool (bench, cutting edge and so on) keep D-263: belt and bag tools still count there. Say if there is a conflict.
5. Hold-to-repeat (#495) keeps working with the equipped tool.
Coordinate the interaction priority part with Integration (session_015g3pLB5eJ4qPrXAwJUmkYT), who owns the Use dispatcher. Update CraftCheck and BuildTest bots to equip tools before using them. Send the PM the PR and green sha, and ask Render for a clip (loot next to a tree, with and without an axe).

## Progress log
- 2026-10-17: ticket written; reading Hotbar.cs and the world tool reducers (Dig, chop via StartCraft, mining, cutting).

## Open questions / blockers
- CI is on hold while the PM's release is out; no pushes that trigger runs until told.

## Next step
Find where the equipped hotbar slot lives on the server, add a helper `EquippedTool(ctx, tags)`, use it in Dig and the chop craft start, then the client prompt priority with Integration.

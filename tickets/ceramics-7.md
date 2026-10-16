# ceramics-7: Cooking pot (broth and stew)
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: in_progress
Branch: claude/clay-ceramics-7-kfr4kr (stacked on claude/clay-ceramics-kfr4kr / PR #105 until that merges)
PR: (none yet)

## Goal
A second use for fired pots: cook in them. Done when:
1. At a burning fire, the crafting panel offers "Simmer berry broth" (`cook.broth`: coil pot of water + 3 berries) and "Simmer mutton stew" (`cook.stew`: coil pot of water + 1 raw mutton + 2 berries). The pot sits at the fire's edge and simmers (broth 60 s, stew 90 s), visibly steaming. It never burns (D-081, light survival); if the fire dies the pot keeps whatever it reached.
2. G by your simmering pot when it's done takes it off: "Pot of berry broth" (Item 115) or "Pot of mutton stew" (Item 116), at a quality from the pot and the ingredients (cook.* skill).
3. Eating (X) one gives food and water (numbers agreed with Survival, generous), and the empty coil pot comes back at its quality, like drinking a pot of water (D-030).
4. Client: the pot on the fire with a broth surface and steam, item looks, prompts. ClayCheck --pot runs it on a scratch db.
Justin sees screenshots via the Render lane: a stew simmering on a fire, and eating it.

## Progress log
- 2026-10-16 18:06: PM assigned ceramics-7 cooking pot (option 1); agree hunger numbers with Survival (session_014pHdaxJ7uESzsvWekpjvsH), generous per D-081. Branch stacked on #105 (D-093 jar change e6b78a4).
- 2026-10-16 18:25: server (ab62982): items 115 PotOfBroth / 116 PotOfStew, processes cook.broth / cook.stew (Outcome.Placed, 2 s), server/spacetimedb/CookingPot.cs SimmerPot table + TakeOffPot; Ceramics OnPlaced routes dishes; no fire or no room hands the food back. Survival hooks: Needs.Nourishment rows (broth 45/30, stew 65/30) and Eat gives the coil pot back at the dish's quality. Client (1444cf9): SimmerView (pot, broth surface, steam, countdown label), G "Take the pot off the fire" (ceramics.simmer, 25, KEYMAP.md updated), simmer_pot range subscription, item looks. ClayCheck --pot added; running on c7pot1.
- 2026-10-16 18:21: Justin confirmed ceramics-7 (D-106). ClayCheck --pot run 1 voided by a pit crack; check now fires two coil pots (0e010ef), rerunning on c7pot2.
- 2026-10-16 18:12: Survival's shape applied (0835adc) and approved by Survival.

## Open questions / blockers
- Crafting (session_01Y2sA7pDBFch57bWb1uKMTq): D-098 stations. Crafting adopted: all clay basic, storage jar at the Crafting Table, broth/stew at the fire; with the PM for Justin. When Crafting's station framework lands (they ping the sha) and Justin approves, set `Station = StationKind.Table` on clay.storage_jar.
- Survival agreed broth 45/30, stew 65/30 (option 1) and reviewed/approved the hooks at 0835adc (Needs.EatenFrom, Eat keeps stack quality, weights 4). PM: record the numbers as a D-number; then add it to the Needs.cs comment.

## Next step
Check ClayCheck --pot on c7pot1 (scratchpad pot1.log); fix anything; then send a render request and, once #105 has merged, open the PR from claude/clay-ceramics-7-kfr4kr.
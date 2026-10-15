# skills-1: Discovery journal and capped skills
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_review
Branch: claude/journal-skills-iseewg (includes a merge of `crafting-core`, so it lands after or with that branch)
PR: #7

## Goal
Journal records processes learned by experimenting, watching a nearby player, or a note. Skills 0 to 100 per specialization with a 300-point cap; fire, foraging, digging and simple tools sit outside the cap; pottery and building are capped. Ranks shorten processes and raise the quality ceiling; raise, hold or fade at cap; death never costs skill. Done when performing a process records it, raises skill and the journal UI shows it.

## How it fits together
- `shared/Skills.cs`: skill families, the prefix map from process ids to skills, and every tunable number (gain curve, cap, speed and quality per rank, dug clay quality).
- Skills: Foraging (`gather.*`), Earthwork (`earth.*`), Firecraft (`fire.*`), Toolmaking (`knap.*`, `cord.*`, `lash.*`) sit outside the cap; Potter (`clay.*`) and Builder (`build.*`) are capped at 300 total.
- `server/spacetimedb/Knowledge.cs`: journal entries (private, `my_journal` view), notes on the ground (`journal_note`, public by chunk), `WriteNote` / `ReadNote` reducers, watching within 8 m. Implements Crafting's `OnProcessPerformed`.
- `server/spacetimedb/Skills.cs`: skill rows (private, `my_skills` view), `SetSkillMode`, cap and fade logic. Implements Crafting's `QualityCapFor` (35 at rank 0 up to 100 at Master).
- Base-game hooks in `Reducers.cs`: digging reports `earth.dig` and its cooldown shrinks with Earthwork; dug clay quality and the chance of a second lump rise with Earthwork; picking up reports `gather.forage`.
- Client: `client/scripts/journal/` (J opens the journal, R reads the nearest note); hooks in `Main.cs` and `Hud.cs`.

## Progress log
- 2026-10-15 21:10: first version pushed and PR #7 opened; tested locally with three scripted players (experimenting, watching, reading a note all work).
- 2026-10-15 21:35: merged main and `crafting-core`; switched to Crafting's partial hooks, dotted ids and the PM's skill families; `tools/CraftCheck` passes locally with the quality cap active.

- 2026-10-15 21:45: "R  Read <name>'s note" prompt appears only near a note; HUD controls just add "J journal". Reported to PM: Main.RefreshInventory throws when two stacks of one item share a grade label (Crafting's file).

## Open questions / blockers
- Sent to PM for Justin (design): a novice's crafted items are capped at quality 35 ("rough") and the cap reaches 100 at Master. Default: keep 35, since crafted quality was 50 on average before skills.
- Sent to PM for Justin (design): skill gain speed, about rank 10 after 6 uses and rank 50 after about 50. Default: keep.
- Waiting on Crafting: a speed hook (`ref float seconds`) so skill also shortens crafting jobs. `Skills.DurationScale` is ready for it; digging already uses it.

## Next step
Wait for CI on PR #7; when green, ask the PM for the merge (it must land with or after `crafting-core`).

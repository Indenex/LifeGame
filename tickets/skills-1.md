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
- `server/spacetimedb/Skills.cs`: skill rows (private, `my_skills` view), `SetSkillMode`, cap and fade logic. Implements Crafting's `QualityCapFor` (35 at rank 0 up to 100 at Master) and `DurationFor` (job time x1.0 down to x0.4).
- Base-game hooks in `Reducers.cs`: digging reports `earth.dig` and its cooldown shrinks with Earthwork; dug clay quality and the chance of a second lump rise with Earthwork; picking up reports `gather.forage`.
- Client: `client/scripts/journal/` (J opens the journal, R reads the nearest note); hooks in `Main.cs` and `Hud.cs`.

## Progress log
- 2026-10-15 21:10: first version pushed and PR #7 opened; tested locally with three scripted players (experimenting, watching, reading a note all work).
- 2026-10-15 21:20: merged main and `crafting-core`; switched to Crafting's partial hooks, dotted ids and the PM's skill families; `tools/CraftCheck` passes locally with the quality cap active.
- 2026-10-15 21:25: "R  Read <name>'s note" prompt appears only near a note; HUD controls just add "J journal". Reported to PM: Main.RefreshInventory throws when two stacks of one item share a grade label (Crafting's file).
- 2026-10-15 21:30: Justin chose slower skill pacing (about double the uses): gain halved, now about rank 10 after 11 uses, rank 50 after 100, Master after about 540. He kept the novice cap of 35, half-rate learning from failures, and the dug clay quality and bonus lump.
- 2026-10-15 21:35: Justin wants masters rare: skill is now stored in hundredths of a rank with a minimum gain of 0.05 ranks per use, so Master takes about 800 uses (rank 10 still after 11, rank 50 after 100).
- 2026-10-15 21:35: merged crafting-core again and implemented its `DurationFor` speed hook.

## Open questions / blockers
- (none)

## Follow-up (after #7, not in it)
- D-025: one Use key. Once Crafting's crafting-2 Use registry is on main, move reading a note from R to a G Use handler. J for the journal stays.

## Next step
Justin approved merging #4 then #7 (D-028). After #4 merges: merge main into this branch, regenerate client/module_bindings, wait for CI green, merge #7 as a merge commit, send the PM the merge commit.

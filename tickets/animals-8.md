# animals-8: Wool spun into yarn and woven into cloth
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: blocked
Branch: claude/animals-wool-cloth-5gkntn (from main after #76 merged; rebuild was needed because the first animals-8 local branch was lost with an old container)
PR: #90

## Goal
Raw wool from shearing becomes cloth with plain tier-2 crafting: make a drop spindle (stick + stone, using a blade), spin 2 raw wool into wool yarn with it, then weave 4 yarn over 2 sticks into wool cloth. Quality flows through as for other crafts. Done when Justin can shear a sheep and end up with wool cloth in a build. Cloth is the input for animals-9 (clothing).

## How it works
- Items (shared/Items.cs, animals range): DropSpindle 197 (new tag `ItemTags.Spindle` = 512, kept tool), Yarn 198 ("Wool yarn"), WoolCloth 199. All HasQuality.
- Processes (shared/Processes.cs), tier 2:
  - `carve.drop_spindle`: Stick + Stone, needs Cutting, 6 s -> Drop spindle.
  - `cord.spin_wool`: 2 Raw wool, needs Spindle, 10 s -> 1 Wool yarn, Spin minigame (D-070), fails below 0.1. `cord` prefix trains Toolmaking (D-071: until a Weaver specialty exists).
  - `weave.wool_cloth`: 4 Wool yarn + 2 Sticks, 20 s -> 1 Wool cloth. `weave` prefix trains Toolmaking.
- CraftingUi.cs (Crafting's file, tiny hooks; Crafting lane asked 2026-10-16): the tool reads "a spindle"; `AnimalsMinigameFor` partial called after Ceramics' `MinigameFor`. `Minigame.Spin` appended to the enum.
- Spin minigame `client/scripts/animals/CraftingUi.Spin.cs`: Space flicks the spindle to full spin; it slows by 0.32/s. A flick while the spin is in the bright band (0.12-0.35) scores 1; too soon kinks the yarn (0.1-0.6); letting it stop drops the spindle (scores 0, yarn breaks back half a flick). Yarn fills from the spin; 6 flicks (Steps, so skill hooks shorten it), about 14 s; time limit 3 s per flick + 8. A drawn spindle (shaft, stone whorl with turning marks, growing cop of yarn) sits above the spin bar, with a thin yarn bar below.
- So one cloth = 8 raw wool = 4 full shearings. No schema change; no server code.

## Progress log
- 2026-10-16: PR #76 (animals-7) green on 93043c5, sha sent to the PM. Started animals-8; items and processes written. Installing .NET + SpacetimeDB in this container to build.
- 2026-10-16 21:00: D-120 (Justin): HOLD #90 until the Spindle station exists; ship them together (animals-11 work lands in this PR). PR set to draft. Merged main 2026-10-16 to keep current. CI on 4233255 was green (run https://github.com/Indenex/LifeGame/actions/runs/38939775364).
- 2026-10-16 18:30: D-070/D-071 relayed. Spin minigame written; server + client build; simulated timing: 6 flicks, about 14 s, score 1 in the band, 0.45 flicking too soon. Bot test with a score still makes yarn. PM asked to hold #90 for it.
- 2026-10-16 17:20: opened PR #90; Render requested 573a057.
- 2026-10-16 17:10: #76 merged (a5e5b1e); merged main in. Server + client build. Local bot test (scratch WoolCheck with a TEMP give-wool reducer, not committed): flake -> drop spindle q35, 4x spin -> 4 yarn q36-37, weave -> 1 wool cloth q38, spindle kept, no yarn left. Upgrade check against main passed.

## Open questions / blockers
- Blocked on Crafting's station framework (crafting-15, branch crafting-stations, pending Justin's OK). Then add the Spindle station here (see tickets/animals-11.md on claude/animals-clothing-5gkntn).
- Answered: D-070 Spin minigame; D-071 Toolmaking until a Weaver specialty exists; D-120 hold for the station.

## Next step
Keep this branch current with main; when crafting-stations is on a branch, merge it in and make `cord.spin_wool` and `weave.wool_cloth` need the Spindle station.

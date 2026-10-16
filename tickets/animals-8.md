# animals-8: Wool spun into yarn and woven into cloth
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_review
Branch: claude/animals-wool-cloth-5gkntn (from main after #76 merged; rebuild was needed because the first animals-8 local branch was lost with an old container)
PR: #90

## Goal
Raw wool from shearing becomes cloth with plain tier-2 crafting: make a drop spindle (stick + stone, using a blade), spin 2 raw wool into wool yarn with it, then weave 4 yarn over 2 sticks into wool cloth. Quality flows through as for other crafts. Done when Justin can shear a sheep and end up with wool cloth in a build. Cloth is the input for animals-9 (clothing).

## How it works
- Items (shared/Items.cs, animals range): DropSpindle 197 (new tag `ItemTags.Spindle` = 512, kept tool), Yarn 198 ("Wool yarn"), WoolCloth 199. All HasQuality.
- Processes (shared/Processes.cs), tier 2, no minigame (default; see open questions):
  - `carve.drop_spindle`: Stick + Stone, needs Cutting, 6 s -> Drop spindle.
  - `cord.spin_wool`: 2 Raw wool, needs Spindle, 10 s -> 1 Wool yarn. `cord` prefix trains Toolmaking.
  - `weave.wool_cloth`: 4 Wool yarn + 2 Sticks, 20 s -> 1 Wool cloth. `weave` prefix trains Toolmaking.
- CraftingUi.cs: one line so the tool reads "a spindle" (Crafting's file, tiny hook).
- So one cloth = 8 raw wool = 4 full shearings. No schema change; no server code.

## Progress log
- 2026-10-16: PR #76 (animals-7) green on 93043c5, sha sent to the PM. Started animals-8; items and processes written. Installing .NET + SpacetimeDB in this container to build.
- 2026-10-16 17:20: opened PR #90; Render requested 573a057.
- 2026-10-16 17:10: #76 merged (a5e5b1e); merged main in. Server + client build. Local bot test (scratch WoolCheck with a TEMP give-wool reducer, not committed): flake -> drop spindle q35, 4x spin -> 4 yarn q36-37, weave -> 1 wool cloth q38, spindle kept, no yarn left. Upgrade check against main passed.

## Open questions / blockers
- Sent to the PM (D-059 options): spinning feel (no minigame [default] / reuse Drill rhythm / new Spin minigame); skill (Toolmaking via cord/weave prefixes [default] / Husbandry / new Weaver spec).

## Next step
Wait for CI on the animals-8 PR, then send the PM the green sha; then start animals-9 (clothing from wool cloth and leather).

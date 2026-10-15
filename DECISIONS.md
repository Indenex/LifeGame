# DECISIONS

Append-only. Never edit or delete an entry; add a new one that supersedes it. Write each entry the moment the decision is made.

## D-001 — 2026-10-15 — One persistent world, buy once on Steam
**Context:** Scope open questions.
**Decision:** One persistent world split into regions; buy once on Steam, no pay-to-win.
**Who:** Justin

## D-002 — 2026-10-15 — Look and feel is part of the MVP
**Context:** Art direction.
**Decision:** Chill, smooth and beautiful from day one. Direction moved from painted isometric to "less pixel art, more Valheim": full resolution, perspective follow camera, smooth hills, volumetric haze, low warm sun, deep greens, dense wind-swept grass that blends with the ground.
**Consequence:** Visual polish is a requirement, not a later pass.
**Who:** Justin

## D-003 — 2026-10-15 — Capped skills that remove tedium
**Context:** Skill system.
**Decision:** Specializations 0 to 100, 300-point total cap; survival basics (fire, foraging, digging, simple tools, shelter) sit outside the cap. Mastery removes tedium; realism never gets in the way of fun.
**Who:** Justin

## D-004 — 2026-10-15 — Merge PRs #1 and #2
**Decision:** Phase 0 spike and the Valheim-style look merged into `main`; new work branches from `main`.
**Who:** Justin

## D-005 — 2026-10-15 — Questions route through a Project Manager
**Decision:** All lane questions go to the PM, which answers what is already settled and brings the rest to Justin.
**Who:** Justin

## D-006 — 2026-10-15 — MVP split into lanes
**Decision:** Lanes as listed in `STATUS.md`, each owning its own files.
**Who:** PM, at Justin's request

## D-007 — 2026-10-15 — Linux and Windows only for now
**Decision:** No macOS builds or signing for now. Playtests run on Justin's local server; no hosting spend.
**Who:** Justin

## D-008 — 2026-10-15 — House building in the MVP
**Decision:** Simple house building for communities, starting with lean-tos and clay huts. Placing a house puts down a visible see-through blueprint and any player can add materials to it.
**Consequence:** PM default: only the placer or the claim owners can cancel or remove it.
**Who:** Justin (default by PM)

## D-009 — 2026-10-15 — Justin is closely involved in design
**Decision:** Lanes post frequent updates with screenshots and bring design questions to Justin through the PM instead of deciding silently.
**Who:** Justin

## D-010 — 2026-10-15 — Playable as soon as possible; sheep
**Decision:** Get a few players crafting in a living world as soon as possible; merge small milestones early. Add animals with simple AI, starting with sheep.
**Who:** Justin

## D-011 — 2026-10-15 — Lane interfaces
**Decision:** Dotted process ids in `shared/Processes.cs`; Journal and skills implements Crafting's partial hooks; quality 1 to 100; items are `LifeGame.Shared.Item` ids that are never renumbered.
**Who:** PM (technical)

## D-012 — 2026-10-15 — State lives in the repo, not in chat
**Context:** Justin moves work between two Claude accounts; chat history and project memory don't carry across.
**Decision:** `CLAUDE.md`, `STATUS.md`, `tickets/`, `DECISIONS.md` and git are the source of truth. Every lane follows `WORKER_LOOP.md` and writes state as it goes.
**Who:** Justin

## D-013 — 2026-10-15 — Clay pot timings
**Decision:** Drying a pot takes about 8 real minutes in the open (about twice as long in rain, faster beside a fire); pit firing about 4 minutes; kiln firing about 3.
**Who:** Justin

## D-014 — 2026-10-15 — Pot-shaping minigame
**Decision:** The pot turns in your hands; press and hold to raise the wall, release before it gets too thin (wall-thickness bar with a sweet spot). Coil pots: place 4 to 6 coils in rhythm. Better wall, better pot; skippable once skilled.
**Who:** Justin

## D-015 — 2026-10-15 — Cracked pots become grog
**Decision:** A pot fired while damp cracks into potsherds, which can be crushed into grog, a temper that improves the next clay. Failure feeds the next try.
**Who:** Justin

## D-016 — 2026-10-15 — Item id ranges per lane
**Context:** Lanes add items in parallel on separate branches.
**Decision:** Crafting 1-99, Ceramics 100-129, House building 130-159, Survival 160-189, Animals 190-219, Claims 220-239, Journal and skills 240-259.
**Who:** PM (technical)

## D-017 — 2026-10-15 — Gentler survival needs
**Decision:** Full to empty takes about 90 minutes for food, 60 for water, and 16 minutes of cold away from a fire (3 times faster standing in water). Needs pause while offline; after a respawn you have 60% food and water and full warmth.
**Who:** Justin ("Gentler" over the proposed 45/30/8)

## D-018 — 2026-10-15 — Merge Survival needs and House building
**Decision:** PR #5 (survival needs) and PR #6 (house building with see-through blueprints) merge into the playtest build. Neither needs a world wipe.
**Who:** Justin

## D-019 — 2026-10-15 — House costs and limits
**Decision:** Lean-to: 10 sticks, 11 fiber. Clay hut: 22 sticks, 2 stones, 17 fiber, 12 clay (bricks and daub replace raw clay once ceramics lands). 3 open building sites per player; holding G adds one item every 0.65 s.
**Who:** Justin

## D-020 — 2026-10-15 — Slower skill gain
**Decision:** About double the proposed uses: roughly rank 10 after 12 uses, rank 50 after about 100, Master after about 800. Novice quality cap stays 35, Master 100; failures teach at half rate; Earthwork raises dug clay quality 50 to 85 with up to a 50% second lump at Master.
**Who:** Justin ("Slower"; PM had recommended faster)

## D-021 — 2026-10-15 — Night and rain cold
**Decision:** Cold drain scales from 1x at 15 °C to 2x at 0 °C; rain adds 50% unless under cover (about 8 minutes to freeze on a freezing night, 16 by day).
**Who:** Justin

## D-022 — 2026-10-15 — Merge Claims and chat
**Decision:** PR #8 (proximity chat, claims, decay, in-claim PvP toggle) merges into the playtest build.
**Who:** Justin

## D-023 — 2026-10-15 — Bigger claims
**Decision:** The claim size goes from 10 m to 20 m so a group's huts and fire fit in one claim. The other claim rules stay as proposed: a marker costs 4 sticks and 2 fiber, one claim per player, none within 20 m of spawn, 3-day decay when untended, and PvP is off inside a claim unless the owner allows it. The wild stays open PvP.
**Who:** Justin ("Bigger claims", which the PM recommended)

## D-024 — 2026-10-15 — Crafting panel layout
**Decision:** Keep the crafting panel as built: tier tabs on the right, Tab cycles the tabs, number keys pick a row. A "what I can make now" filter may be added later.
**Who:** Justin ("Keep tabs", which the PM recommended)

## D-025 — 2026-10-15 — One Use key and a shared keymap
**Decision:** G is a single Use key that acts on what the player faces: pick berries, feed a fire, drink at water, add materials to a blueprint, read a note. C crafting, X eat, B build menu (claim marker included), K claims, J journal, Enter chat, M music; time and weather previews move to debug-only F5/F6. `KEYMAP.md` is the one list of keys; lanes register keys there before binding.
**Who:** Justin ("One Use key", which the PM recommended). Crafting (integration owner) builds the Use dispatcher; lanes register handlers.

## D-026 — 2026-10-15 — Sneak walk
**Decision:** Holding Shift walks slowly; sheep don't spook at a sneaking player. Animals implements it with a small movement hook in Main.cs, and the server reads the slower speed.
**Who:** Justin ("Add sneak", which the PM recommended)

## D-027 — 2026-10-15 — Sheep fleece colors
**Decision:** Most sheep are cream, some oat-colored, and about one in eight is a dark brown "black sheep". More colors may come with the wool and dye work.
**Who:** Justin ("As shown")

## D-028 — 2026-10-15 — Merge Crafting core and Journal/skills (world wipe)
**Decision:** PR #4 (crafting core) merges, then PR #7 (journal and capped skills) right after, each when CI is green. Item storage moves to numeric ids plus quality, so Justin's local world and survival vitals reset once (`spacetime publish --delete-data`).
**Who:** Justin ("Merge both")

## D-029 — 2026-10-15 — Merge Atmosphere
**Decision:** PR #9 (day and night, weather, rain, lighting, synthesized soundscape) merges into the playtest build when green. No world wipe. Ceramics, merging after it, adds the `RainAt` implementation.
**Who:** Justin ("Merge", after testing in game)

## D-030 — 2026-10-15 — Water pot amounts
**Decision:** Drinking a pinch pot of water quenches 50% thirst, a coil pot 100%; the empty pot comes back at the same quality.
**Who:** Justin ("50% / 100%", which the PM recommended over the lane's 35/70 default)

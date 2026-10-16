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

## D-031 — 2026-10-15 — Hut recipes on real materials
**Decision:** Clay hut walls take 12 daub (12 clay + 6 fiber via clean-clay and mix-daub); the daub stage's extra 3 fiber is dropped. Wattle-and-daub huts use no bricks; bricks wait for a later mud-brick house. The lean-to keeps its 3 fiber lashing.
**Who:** Justin ("As proposed")

## D-032 — 2026-10-15 — PM approves routine merges
**Decision:** The PM approves a PR once CI is green and it matches decisions already recorded here. Justin still approves world wipes, design changes he hasn't decided, and anything that costs money. Every merge is still reported in the PM thread.
**Who:** Justin ("PM approves"), to speed up merging

## D-033 — 2026-10-15 — UX/UI lane owns bindings; lanes stay in their own files; no macOS
**Decision:** A UX/UI lane owns key bindings (the input map and `KEYMAP.md`), the shared Use key registry, and the HUD layout. Other lanes register Use handlers rather than binding keys. Lanes stay in their own files as much as possible to avoid merge conflicts. The macOS build is dropped from CI.
**Who:** Justin

## D-034 — 2026-10-15 — One chat for updates
**Decision:** Lanes send status updates and screenshots to the PM instead of posting them in their own threads; the PM posts them in the PM thread so Justin reads one chat. Lanes still answer Justin in their own thread when he writes there.
**Who:** Justin

## D-035 — 2026-10-15 — Kiln is a blueprint building
**Decision:** The updraft kiln is placed from the B build menu as a see-through blueprint that anyone can add bricks and daub to (StructureKind.Kiln), not crafted and dropped. Firing: up to 6 dry pots, 2 sticks of fuel, 3 minutes, 2% crack, +10 quality. Ceramics owns the kiln logic in its own files via Building's completion hooks.
**Who:** Justin ("Blueprint build", which the PM recommended)

## D-036 — 2026-10-15 — Immersion first; rain tiers and a smooth fade
**Decision:** Immersion is a very high priority in every aspect of the game. Rain has intensity levels (mostly light and easy to see through; rare heavy storms), with sound scaling to match. The see-through fade for trees in front of the player must look smooth and natural, not dithered.
**Who:** Justin

## D-037 — 2026-10-15 — Use prompt placement
**Decision:** The Use prompt (keycap plus verb, such as "G Feed the fire") sits fixed just below the player on a soft panel and fades in and out.
**Who:** Justin ("Below player", which the PM recommended)

## D-038 — 2026-10-15 — No controls line on screen
**Decision:** The bottom-left controls text is removed. Players learn keys from the G Use prompt and hints inside panels; the UX/UI lane adds subtle discoverable cues for keys with no prompt (sneak, journal, claims, chat). Lanes stop adding text to the controls line.
**Who:** Justin ("Drop it"; the PM had recommended fade plus F1)

## D-039 — 2026-10-15 — Sheep for meat and hide
**Decision:** Players can kill sheep (with a weapon or cutting tool; the flock flees) and butcher them for raw mutton and hide; flocks regrow slowly over in-game days and never vanish. Raw mutton is cooked at a fire (Crafting `cook.meat`); cooked mutton feeds about 40% hunger (Survival default). Shearing for wool comes first.
**Who:** Justin ("Yes, slowly", which the PM recommended)

## D-040 — 2026-10-15 — Bug fixes lane
**Decision:** A Bug fixes lane takes cross-lane and integration bugs assigned by the PM, one small PR per bug from main, touching whatever files the fix needs and telling the owning lane. First bug: players saved far from the origin rejoin stuck at spawn. Crafting goes back to crafting content.
**Who:** Justin

## D-041 — 2026-10-15 — Digging stick digs deeper
**Decision:** A digging stick lets you dig one level deeper than bare hands, reaching better clay. It does not double clay or speed up digging.
**Who:** Justin ("Dig deeper"; the PM had recommended double clay)

## D-042 — 2026-10-15 — Carrying limit, raised by a basket
**Decision:** Survival adds a carrying limit, and carrying a woven basket (10 fiber + 3 sticks) raises it. No gather-all pickup. Survival proposes the numbers to Justin with screenshots before they ship.
**Who:** Justin ("Carry limit"; the PM had recommended gather-all)

## D-043 — 2026-10-15 — Cooking trains Firecraft
**Decision:** Cooking on a fire trains Firecraft for now; a separate Cooking skill can come later.
**Who:** Justin ("Firecraft now", which the PM recommended)

## D-044 — 2026-10-15 — Next claim rules
**Decision:** (1) A claim owner can take down any building in their claim, members' included; the materials drop inside the claim. (2) Claim borders show as a soft line on the ground that fades in within about 6 m of the edge. (3) One owner plus members, no co-owner rank yet.
**Who:** Justin ("As proposed", which the PM recommended)

## D-045 — 2026-10-15 — Quieter HUD
**Decision:** The UX/UI lane builds the quieter HUD from its mockup: round slots with real item pictures, minimal text and panels.
**Who:** Justin ("Build it", which the PM recommended)

## D-046 — 2026-10-15 — Doors, storage baskets, and weathering outside claims
**Decision:** Anyone can open a hut door. Anyone can take from a storage basket, except inside a claim, where only members can. All player-crafted things (buildings, storage baskets, crafted items left on the ground) that sit outside a claim weather away if no player has touched them for a while. Inside a claim nothing weathers. PM default timing: weathering starts after 3 real days untouched and the thing is gone 24 hours later; any interaction resets it. One server sweep per hour checks a last-touched timestamp, which keeps the cost low. Building owns `Weathering.cs`; Claims supplies the in-claim check.
**Who:** Justin (weathering rule in his words; timing is the PM's default, his to change)

## D-047 — 2026-10-15 — Masters skip minigames but can play them
**Decision:** At Master rank, crafting minigames are skipped by default and score a steady 0.8. Holding a key while starting the craft plays the shortest version instead, for a chance at a better result. The UX/UI lane picks the key and adds it to KEYMAP.md.
**Who:** Justin ("Skip, can play", which the PM recommended)

## D-048 — 2026-10-15 — Doors are lockable (amends D-046)
**Decision:** Doors can be locked. The PM's defaults: the player who built the door, or inside a claim any member, can lock and unlock it with Use. A locked door opens only for them; to everyone else it stays shut. Unlocked doors open for anyone, as in D-046. There is no lock-breaking yet.
**Who:** Justin ("Doors should be lockable."); the details are PM defaults and his to change

## D-049 — 2026-10-15 — Husbandry is a basic skill
**Decision:** A new Husbandry skill covers shearing now and butchering and herding later. It is a survival basic outside the 300-point cap, like Foraging and Firecraft. Process ids `animal.*` map to it.
**Who:** Justin ("Husbandry, basic"; the PM had recommended a capped specialty)

## D-050 — 2026-10-15 — Right-drag turns the camera
**Decision:** Holding the right mouse button and moving the mouse turns the camera. Q/E still turn it. The UX/UI lane builds it.
**Who:** Justin

## D-051 — 2026-10-16 — Ship firelight and the moonlit early night
**Decision:** Atmosphere's warmer firelight and the moonlit early night (#32) ship as shown.
**Who:** Justin ("Ship it")

## D-052 — 2026-10-16 — Carrying too much slows you down
**Decision:** Over the carrying limit (D-042) you can still pick things up, but you walk slower: easing down to half speed at the overload cap, never slower. This is what survival-3 (#39) already does.
**Who:** Justin ("Slower walk")

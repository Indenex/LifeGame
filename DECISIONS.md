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

## D-053 — 2026-10-16 — Player fighting: as proposed, and killers are marked
**Decision:** PvP follows Claims' proposal: left click strikes a player in reach (fists ~12 hits to kill, hand axe ~6); fighting is open in the wild, blocked inside claims unless the owner allows it and within 20 m of spawn; new players are protected for 10 minutes; dying to a player is like any death (drop what you carry, keep skills); one chat line says who struck whom. Added: a killer's name shows red to others for 30 minutes. Being hurt shows as a soft dark-red screen edge instead of a health ring (Claims' change, shown to Justin).
**Who:** Justin ("Mark killers")

## D-054 — 2026-10-16 — A new fire burns 15 minutes
**Decision:** A newly lit fire burns for 15 minutes before it needs feeding (was 60 s; Crafting had proposed 180 s). Each stick fed still adds 90 s.
**Who:** Justin ("15 minutes for the fire length")

## D-055 — 2026-10-16 — Skill titles stay every 20 ranks
**Decision:** Titles stay Apprentice 20, Journeyman 40, Expert 60, Artisan 80, Master 100 (the design doc's 25/50/75/90 is not used). The Journeyman focus choice opens at rank 50.
**Who:** Justin ("Keep the game's"; the PM had recommended the doc's)

## D-056 — 2026-10-16 — Cleared trees fall in a random direction
**Decision:** When ground is cleared, trees topple and settle in a random direction (seeded per tree so all players see the same), rocks sink, grass thins. The PM had recommended away-from-camera.
**Who:** Justin ("Random")

## D-057 — 2026-10-16 — Mud-brick house costs
**Decision:** The mud-brick house (building-4) costs footings 6 stone + 4 sticks, walls 16 bricks + 6 daub, rafters 12 sticks + 2 cordage, thatch 14 fiber. Full rain cover, warmth 0.85 (0.9 door shut), a fire inside makes it fully warm, weathers after 7 days untouched. The PM had recommended 10 bricks.
**Who:** Justin ("16 bricks"); the other numbers are Building's defaults, shown to him

## D-058 — 2026-10-16 — Dedicated Integration lane; Opus codes, Sonnet does small tasks
**Decision:** Crafting no longer owns integration, CI and builds; a dedicated Integration lane does (shared-file hooks, CI workflows, build and bindings tooling, the Use dispatcher, PR merge-readiness). Crafting and the other feature lanes only build features. Every session or agent that writes or designs code runs on Opus; small, basic tasks run on Sonnet, handed out by a coordinator (the lane session) that breaks its ticket down and reviews the results. Supersedes "Crafting (integration owner)" in D-025; the Bug fixes lane (D-040) still takes cross-lane bugs.
**Who:** Justin

## D-059 — 2026-10-16 — Ideas come as choices; a dedicated Render lane
**Decision:** (1) Whenever the PM or a worker has an idea, it goes to Justin as a short question with 2 to 4 concrete options, recommended first, answerable in a word; work continues on the recommended option meanwhile. (2) A dedicated Render lane makes all screenshots, clips and mockups. Other lanes send it a render request (ticket, branch, sha, what to show, short summary) instead of rendering themselves; when done, the Render lane sends the images and the requester's summary to the PM, who presents them to Justin. Images still go under /mnt/project-files/screenshots/ (D-034).
**Who:** Justin

## D-060 — 2026-10-16 — Chopped trees fall
**Decision:** Chopping a tree with the stone axe fells it after 3 logs; it regrows in 2 hours (`Trees.Felling` on, crafting-9, #70).
**Who:** Justin ("Fell it", which the PM recommended)

## D-061 — 2026-10-16 — Keep the sleeping mat
**Decision:** The woven grass mat (8 fiber, laid inside a finished hut or house) stays: you wake there after dying (building-5, #61).
**Who:** Justin ("Keep it", which the PM recommended)

## D-062 — 2026-10-16 — Wetness numbers as proposed
**Decision:** Heavy rain soaks you in 2 minutes, drizzle in 10, wading in 10 seconds; drying takes 5 minutes, 2.5 by a fire; cold is multiplied by 1 + 0.5 x wetness (survival-5, #75).
**Who:** Justin ("As proposed", which the PM recommended)

## D-063 — 2026-10-16 — A player wiki, light on spoilers
**Decision:** A Wiki lane (a Sonnet worker) writes and keeps current a player wiki under `wiki/`. It gives direction, basic tips and the spirit of play (e.g. "being wet chills you faster; there are ways to dry off quicker") without exact numbers, full recipes or solutions to discoveries. It follows `main`: when a merged decision or PR changes what players experience, the wiki is updated.
**Who:** Justin

## D-064 — 2026-10-16 — The wiki moves to Opus, in a plainer voice (amends D-063)
**Decision:** The Wiki lane runs on Opus, not Sonnet. The wiki is rewritten in a plain, direct voice like a good game manual: no cosy or affectionate tone, no "be kind", no cute one-liners. The spoiler rule from D-063 stays.
**Who:** Justin ("the sonnet worker's writing style is weird and oddly affectionate")

## D-065 — 2026-10-16 — Leatherwork becomes its own specialty later
**Decision:** Hide work (scrape, stretch, dry into leather) will train a new capped Leatherwork specialty once leather has more uses. Until that skill exists, hide work trains Husbandry (PM default, so it trains something meanwhile).
**Who:** Justin ("Leatherwork later"; the PM had recommended Husbandry)

## D-066 — 2026-10-16 — Ceramics builds a storage jar next
**Decision:** ceramics-6 is a large kiln-fired storage jar set down as a store: ~6 stacks, keeps contents dry, 4 tempered clay, coil minigame, pit firing usually cracks it.
**Who:** Justin ("Storage jar", which the PM recommended)

## D-067 — 2026-10-16 — Skill toasts only at new titles
**Decision:** Rank-ups show a toast with a chime only at a new title; other ranks show as dots in the journal (skills-8 as built).
**Who:** Justin ("Titles only", which the PM recommended)

## D-068 — 2026-10-16 — Burnt mutton gives 10% hunger
**Decision:** Burnt mutton restores 10% hunger (roast 40%), as survival-4 built it.
**Who:** Justin ("10%", which the PM recommended)

## D-069 — 2026-10-16 — Player trading
**Decision:** Players can trade. Claims builds the server side (claims-7, replacing the one-way hand-over); UX/UI builds the trade window (ui-9). PM defaults, Justin's to change: face a player and press Use to ask to trade; they accept or decline. A window shows both players' offers side by side; each adds items from what they carry. Both must press Accept; any change to either offer clears both accepts. The swap is all-or-nothing and checked on the server (items still held, carry limits respected, D-042). Walking away (beyond ~4 m), Esc, or either player dying cancels it. An empty side is a gift. One trade at a time per player; a chat line says when a trade completes. Claims writes the reducer and table contract into its ticket first so UX can build against it.
**Who:** Justin ("Do a trading interface"); the flow above is the PM's default

## D-070 — 2026-10-16 — Spinning gets its own minigame
**Decision:** Spinning wool into yarn is a new minigame: flick the spindle and keep it turning (animals-8). Weaving stays a timed craft.
**Who:** Justin ("New spin minigame"; the PM had recommended a plain timed craft)

## D-071 — 2026-10-16 — Weaver becomes its own specialty later
**Decision:** Spinning and weaving will train a capped Weaver specialty that later grows into dyes and clothing, like Leatherwork (D-065). Until it exists they train Toolmaking.
**Who:** Justin ("Weaver later"; the PM had recommended Toolmaking)

## D-072 — 2026-10-16 — Hear other players' work, quieter
**Decision:** Crafting work sounds play for everyone nearby, not only your own; other players' work sounds play about 60% quieter than your own (atmosphere-12). Needs other players' running jobs to be visible to clients; Atmosphere agrees the table or hook with Crafting and Integration.
**Who:** Justin ("Everyone's sounds, but make it other player ones like 60% quieter than your own")

## D-073 — 2026-10-16 — Animation and Modeling lanes
**Decision:** Two new Opus lanes for look and immersion. **Modeling** makes and upgrades the 3D models (player, animals, tools, items, structures, plants); it owns `client/assets/models/` and `client/scripts/models/`, and may change the existing in-code meshes owned by other lanes (e.g. `Sheep.cs`, `StructureModel*.cs`, `PotMesh.cs`, `ItemLook.cs`) in small PRs, telling the owning lane. **Animation** makes characters and creatures move well (player walk/idle/work/gather, sheep, felling, crafting gestures); it owns `client/scripts/animation/` and takes over `client/scripts/Avatar.cs` from Integration. Both send their work to the Render lane, and Justin sees it before it merges. Both keep to Linux and Windows performance budgets (32 players, CI load test).
**Who:** Justin ("start an animation worker, and a dedicated modeler")

## D-074 — 2026-10-16 — Clothes go on through an equipment panel
**Decision:** Players wear clothing through a paper-doll equipment panel (slots such as body and feet), not automatically. Animals builds the worn-slots server side (animals-9); UX/UI builds the panel; Modeling and Animation show worn clothes on the figure.
**Who:** Justin ("Equipment panel"; the PM had recommended auto-worn)

## D-075 — 2026-10-16 — Start low, grow slowly (all numbers)
**Decision:** Every number and stat in the game (gear bonuses, skill gains, yields, speeds, capacities, durability) starts on the low end and grows slowly, so each new piece of equipment or step up is worth having. The game is meant to take a long time to progress through. The curve rises faster through roughly the first 15% of progression, so new players feel early gains, and then flattens into a slow climb. Applies to all new numbers; lanes re-check existing numbers against it and propose changes to the PM. First case: clothing warmth starts small (well under the 25%/10% proposed).
**Who:** Justin ("ALL numbers, Stats, etc. should start on the lower ends and scale up slowly ... faster at the first 15% or so")

## D-076 — 2026-10-16 — Storage jar is kiln-only
**Decision:** The storage jar can only be fired in a kiln; a campfire refuses it.
**Who:** Justin ("Kiln only", which the PM recommended)

## D-077 — 2026-10-16 — Trades may overload you
**Decision:** A trade that takes a player over their carry limit is allowed; they walk slower, as with picking things up.
**Who:** Justin ("Allow, walk slower", which the PM recommended)

## D-078 — 2026-10-16 — Environment lane, paired with Atmosphere for cosy immersion
**Decision:** A new Opus Environment lane builds the world itself: terrain shape and ground cover, trees, bushes, grass, rocks, water edges, paths and natural props, placed so the forest feels lived-in and cosy. It works hand in hand with Atmosphere (light, sky, weather, time of day, mood); the two lanes together own comfy immersion. Environment owns `client/scripts/environment/` and `client/scripts/Terrain.cs`; gameplay rules for trees and resources stay with their lanes (e.g. Crafting's `shared/Trees.cs`), and world-generation changes in `shared/World.cs` go through Integration as small hooks.
**Who:** Justin

## D-079 — 2026-10-16 — Dedicated Audio lane takes all audio
**Decision:** A new Opus Audio lane owns all sound and music: `client/scripts/atmosphere/audio/` (SoundMix), `client/scripts/atmosphere/SoundScape.cs`, `tools/SoundPreview/`, music and any future `client/scripts/audio/`. Other lanes no longer make audio; they send the Audio lane a request (what event, where, how it should feel), as with render requests. Atmosphere hands its in-progress work sounds (atmosphere-12, branch `claude/atmosphere-12-work-sounds`, incl. D-072 other players at about 60% quieter) to Audio and keeps the visual side.
**Who:** Justin ("a dedicated audio worker ... that takes anyone's audio work from them")

## D-080 — 2026-10-16 — Trades are silent; requests lapse after 30 s
**Decision:** Completed trades post nothing to bystanders' chat (the two traders still see their own confirmation). An unanswered trade request lapses after 30 seconds.
**Who:** Justin ("Nothing"; the PM had recommended names only. "30 seconds", which the PM recommended)

## D-081 — 2026-10-16 — Pre-alpha MVP and milestones
**Decision:** MVP: a fresh player wakes in a beautiful forest, reaches fire, food, shelter and a fired pot alone in about 1 to 2 hours, and sees a long road ahead; everything they make looks real on the ground, in their hands and in the inventory. Progression runs on three visible ladders: materials/tools tiers, skills (fast first ~15%, D-075), and home (lean-to to hut to mud-brick house). Survival is light: needs nudge players along the ladders and never punish; death is mild; cold is a reason to build; no death spirals. Fun first, with some realism. Milestones, M1 and M2 worked in parallel:
- **M1 Looks and feels real:** proper models for every item used on the ground, as the icon and held in hand; player figure and animation; cosy forest, light and sound. (Modeling, Animation, Environment, Atmosphere, Audio, UX)
- **M2 The first evening:** a new playtester reaches a fired pot unaided in 1 to 2 h, sees skill gains in session one, always has a next goal. (Crafting, Survival, Skills, Ceramics, Bug fixes, Wiki)
- **M3 A place of your own:** mud-brick house, storage jar, kiln, mat, clothing and equipment panel, fences, trading.
- **M4 The long road:** specialties, tool quality, ~20 h mastery, cooking, wool and leather chains.
- **M5 Pre-alpha playtest:** 8 to 16 outside players, no wipes, stable servers, shareable wiki.
**Who:** Justin ("Adopt, M1 and M2 together"; aesthetics, a world to live in and get lost in, images for all items, survival not super difficult)

## D-082 — 2026-10-16 — Modeling order: player figure, then every item
**Decision:** Modeling does the player figure first, then every item (early-game items first) as one model used on the ground, for the inventory icon and held in hand. Animation adds holding tools and items in hand.
**Who:** Justin ("Player figure first"; the PM had recommended items first)

## D-083 — 2026-10-16 — Ship the water and sky
**Decision:** Atmosphere's water and sky (#66: shimmer, lapping, rain rings, frogs, wind sway, clouds and cloud shadows, dawn mist) ships as rendered.
**Who:** Justin ("Ship it", which the PM recommended)

## D-084 — 2026-10-16 — Groves and glades now, reshaped land after
**Decision:** Environment builds groves and glades first (visual only, no wipe): clustered trees with open glades, undergrowth at grove edges, logs, mossy rocks, flowers, reeds and stones at the pond. After that it reshapes the land: more ponds, a stream, gentler hills. The reshape needs a world wipe; Justin chose it, and the PM confirms the timing with him before any wipe runs.
**Who:** Justin ("1 and 3")

## D-085 — 2026-10-16 — Forest light first, the other skies later
**Decision:** Atmosphere does light through the forest next (sun shafts, dappled light at morning and evening), then weather you can see coming (rain curtains, darkening sky, rainbow), then the cosy night (firelight on trunks, starrier sky, softer moonlight).
**Who:** Justin ("One, but I want all of these eventually")

## D-086 — 2026-10-16 — Skill pace stays about 6 hours to master, for now
**Decision:** Mastering one skill stays at roughly 6 to 7 hours of focused play for pre-alpha; it will be made much longer later. The effects per rank stay linear for now.
**Who:** Justin ("6 hours for now, it will be way longer later"); the PM had recommended ~20 hours

## D-087 — 2026-10-16 — Building order: fence, hearth, cabin
**Decision:** House building does a stick fence and gate next (building-8), then a raised hearth, then a log cabin from felled trees.
**Who:** Justin ("All of these, but the first one first")

## D-088 — 2026-10-16 — Brisk walk, run on Shift, with stamina
**Decision:** Normal movement stays at 4.2 m/s, animated as a brisk walk-jog. Holding Shift runs faster and spends stamina; stamina refills when not running. Stamina is a light need (D-081): it shows only while it is not full, and running out just drops you back to walking. Survival builds stamina; UX/UI moves sneak off Shift (key to be confirmed with Justin); Animation adds the run. Numbers start low per D-075.
**Who:** Justin ("Keep the brisk jog, and add a run tied to shift. It should have a stamina system though.")

## D-089 — 2026-10-16 — Better tools work up to about 15-20% faster
**Decision:** Tool quality speeds work, from 0% for a crude tool to about 15-20% for an excellent one (PM default 15% at the top, open to 20% later), on the D-075 curve.
**Who:** Justin ("Yes, but not 25% ... Maybe 15-20%?")

## D-090 — 2026-10-16 — Full fires refuse fuel; fire length grows with skill (amends D-054)
**Decision:** Feeding a fire at its cap is refused ("The fire is burning as high as it will") and the stick is kept. A newly lit fire burns 20 minutes (was 15, D-054). The most a fire can hold grows with Firecraft from 20 minutes up to 40 minutes (+100%) at Master.
**Who:** Justin ("Refuse. Fires should start at 20 minutes and last longer maybe up to 100% more scaled with skill.")

## D-091 — 2026-10-16 — Sneak moves to held Ctrl
**Decision:** Sneak is held Ctrl (Shift is now run, D-088). Holding Ctrl while starting a craft still plays the minigame at Master (D-047); the two happen at different moments.
**Who:** Justin ("Hold Ctrl", which the PM recommended)

## D-092 — 2026-10-16 — Craft times scale with the item and the skill
**Decision:** Base craft times scale: cheap, simple items are quick; higher-tier items and higher-quality results take incrementally longer. Rising skill shortens craft times, noticeably as you first learn a skill (fast early, per D-075). Crafting proposes the formula and a table of examples to the PM before shipping.
**Who:** Justin ("easier/cheaper items should be faster, and take incrementally longer ... on higher quality items ... Increases in skill level should also lower craft times")

## D-093 — 2026-10-16 — Storage jar holds 4 to 8 stacks by quality
**Decision:** A storage jar holds 4 stacks at low quality, rising to 8 at high quality, faster through the first part of the quality range (D-075).
**Who:** Justin ("4 rising to 8 with quality"; the PM had recommended a flat 6)

## D-094 — 2026-10-16 — I opens the equipment panel
**Decision:** The equipment panel (D-074) opens with I.
**Who:** Justin ("I", which the PM recommended)

## D-095 — 2026-10-16 — Integration executes merges
**Decision:** The PM delegates merge execution to the Integration lane. Integration merges any PR that is green on its current head, merges cleanly, matches decisions already in DECISIONS.md and does not wipe Justin's world, in the order it judges safe, and reports each batch to the PM. Lanes send green shas to Integration, not the PM. Wipes, undecided design changes Justin would notice, and anything the PM has put on hold still go through the PM (and Justin where D-032 requires).
**Who:** PM (within D-032), to keep merges flowing while the PM takes design questions to Justin

## D-096 — 2026-10-16 — Justin sees every option set
**Decision:** Every option set from the PM or any lane goes to Justin, small tuning included. The PM batches them.
**Who:** Justin ("Everything"; the PM had recommended big calls only)

## D-097 — 2026-10-16 — Crafting a little under the music; sound sliders in Options
**Decision:** Your own crafting sounds sit a little under the music. The Esc menu gets an Options page with sound sliders (at least master, music, effects/work, ambience); every slider starts at 50% for a new player. Audio owns the mix and buses; UX/UI builds the page.
**Who:** Justin ("2, but add a sound setting to Options ... Sliders should start at 50% on a new player login")

## D-098 — 2026-10-16 — Hand crafting for basics, crafting stations for the rest
**Decision:** Players hold nothing in their hands while working; small hand crafts play a simple hand-crafting animation. Only basic items and basic material processing can be made from your own crafting menu (C). Everything else needs its station: a **Crafting Table** for general and misc items, a **Spindle** for making yarn and cloth, a **Leather drying rack** for drying and working leather (more stations later as needed). Stations can only be built inside a claim. A station can use items from your inventory or from a chest/storage. Crafting proposes which recipes stay "basic" and which move to which station, for Justin to approve. Supersedes Animation's held-items work for now.
**Who:** Justin (held items: "Nothing ... Other things require the crafting table associated with them ... These tables require a claim though ... They can pull from any item on your character, or your chest though.")

## D-099 — 2026-10-16 — Chill capped at 2x; seasons with snow
**Decision:** Combined chill (cold air times wetness) is capped at 2x, so the worst case from warm to dead is about 8 minutes. Freezing weather is tied to a new seasons system: seasons change temperature and weather, bring snow in winter, and change plants and animals. Start with a fast season cycle for testing; later a real-time multiplier, possibly tied to the real-world calendar. Atmosphere leads (WorldClock), with Environment (snow, foliage), Animals (fauna by season) and Survival (cold).
**Who:** Justin ("1, but tie freezing into a seasons system with snow, and changing fauna ... Start with a fast cycle for now")

## D-100 — 2026-10-16 — First clothes, worn weight, carry limit, run speed
**Decision:** Wool tunic takes 10% off the cold drain and leather foot wraps 4% (14% together at best quality, scaling with quality). Worn clothes do not count toward the carry load. Carry limit: 20 bare, basket +10 (30). Running is 5.6 m/s (walking stays 4.2).
**Who:** Justin (tunic and wraps "10% / 4%"; the PM had recommended 8%/3%. Worn weight "No", carry "20 / +10", run "5.6 m/s", which the PM recommended)

## D-101 — 2026-10-16 — Stamina shown as a bar; running is free; food unchanged
**Decision:** Stamina shows as a small bar under your character, only while not full. Running costs no food or water. Food values stay: berries 12% each, roast mutton 40%, burnt 10%. Stamina duration and refill are set from researched "carrot on a stick" progression curves (see `docs/PROGRESSION.md`, being written), which all lanes then use as the base for number suggestions.
**Who:** Justin ("Small bar under your character"; the PM had recommended an arc by the rings. "No"; "Keep as is". Stamina: "Research common logarithms for fun/invested 'carrot on a stick' values and use them for suggestion bases.")

## D-102 — 2026-10-16 — Scribe lane keeps the decision log
**Decision:** A Sonnet Scribe lane (session_01He77u9z8pt5DtP7it3NMAQ) records Justin's decisions in `DECISIONS.md` and applies the `STATUS.md` changes the PM sends, merging its own docs-only PRs. The PM keeps asking Justin and relaying decisions to the lanes.
**Who:** Justin ("pass decision logging work off to a dedicated sonnet worker, and keep my questions coming")

## D-103 — 2026-10-16 — Campfire warms in 60 s
**Decision:** A basic campfire takes about 60 s to warm you from frozen to full (was 30 s). Later hearths and stone-ringed fires can be faster.
**Who:** Justin ("60 s"; the PM had recommended keeping 30 s)

## D-104 — 2026-10-16 — Chopping trains Foraging
**Decision:** Chopping logs (`chop.log`) trains Foraging.
**Who:** Justin ("Foraging", which the PM recommended)

## D-105 — 2026-10-16 — Things to try hints in the journal
**Decision:** skills-10 adds 2 to 3 gentle "Things to try" hints in the journal pointing at the next undiscovered thing, without recipes (D-063), so a new player always has a next goal. Each lane supplies one hint line per new process.
**Who:** Justin ("Things to try hints", which the PM recommended)

## D-106 — 2026-10-16 — Ceramics makes a cooking pot next
**Decision:** ceramics-7 is a cooking pot: a coil pot of water on a fire makes broth from berries or mutton. Hunger values are agreed with Survival and kept generous (D-081).
**Who:** Justin ("Cooking pot", which the PM recommended)

## D-107 — 2026-10-16 — Effect values stay hidden
**Decision:** The game shows effects in words only ("Keeps a little of the cold off"), never numbers, in the equipment panel and elsewhere. The values live in shared code the client can read, so dataminers can find them; that is accepted, and hiding them from the UI is still the rule.
**Who:** Justin ("I want values to remain hidden I think. If it's not reasonable against people datamining etc. then I suppose we could just show it."); the PM had recommended showing numbers

## D-108 — 2026-10-16 — Interim training for hides and sewing
**Decision:** Until Leatherwork and Weaver exist, hide work trains Husbandry and sewing trains Toolmaking.
**Who:** Justin ("Hides: Husbandry, sewing: Toolmaking", which the PM recommended)

## D-109 — 2026-10-16 — Cold hearths last 1 h outside claims
**Decision:** A burned-out fire leaves a cold hearth that lasts 1 hour outside a claim; inside a claim it follows normal claim rules (it does not weather away).
**Who:** Justin ("1 hour outside a claim, normal weathering inside a claim")

## D-110 — 2026-10-16 — Chat bubbles stay about 3 s plus reading time
**Decision:** Each speech bubble stays about 3 s plus time to read its length; up to 3 stacked.
**Who:** Justin ("About 3 s plus reading time", which the PM recommended)

## D-111 — 2026-10-16 — Stamina numbers
**Decision:** From full, a new player runs 8 s, and the bar refills from empty in 12 s. Refill starts 1 s after you stop running and is faster when the bar is low (Valheim-style). Both improve linearly with Survival skill, to 16 s of running and a 7 s refill at Master (per `docs/PROGRESSION.md`). A good meal gives +25% run time for 10 minutes.
**Who:** Justin ("8 s run / 12 s refill" and "Yes, +25% run for 10 min", both the PM's recommendation, from the researched curves)

## D-112 — 2026-10-16 — Station split (amends D-098)
**Decision:** These stay basic in the C menu: tinder, fires, knapping, digging stick, cordage, bricks, clay preparation (clean, grit, grog, temper, daub), and the first hafted tools (spear, knife, axe, bow drill), so you can survive anywhere. Shaping pots (pinch pot, coil pot, storage jar) moves to a new Potter's bench station. Crafting Table: basket, drop spindle, wool tunic. Spindle: spin wool, weave cloth. Leather drying rack: hide work, leather wraps. Broth, stew and roasting stay at the fire. The Crafting Table costs 4 sticks, 2 cordage and a log (so an axe comes first). A station draws from your inventory, from baskets and jars within 4 m, and from the claim stash.
**Who:** Justin ("Pots at a Potter's bench"; the PM had recommended keeping all clay basic. "Also a log"; the PM had recommended 4 sticks + 2 cordage. "Storage within 4 m + claim stash" and "Hand craft" for first tools, both as recommended)

## D-113 — 2026-10-16 — Craft-time formula
**Decision:** Craft time = tier base (t0 3 s, t1 6 s, t2 12 s, t3 20 s, times a per-recipe effort of 0.5 to 3) × quality (0.8 + 0.4 × target quality/100) × skill (1 − 0.6 × the D-075 curve of rank: ×1.00 at rank 0, ×0.76 at 15, ×0.61 at 50, ×0.40 at Master) × tool (up to 15% faster, D-089). Examples from rank 0 to Master: basket 28→13 s, stone axe 17→8 s, coil pot 38→18 s, storage jar 56→26 s.
**Who:** Justin ("Ship this formula", which the PM recommended)

## D-114 — 2026-10-16 — Seasons: pace, first content, order
**Decision:** The test cycle is one in-game day (90 min) per season, so a year is 6 real hours. Seasons come right after atmosphere-13 (forest light) and before atmosphere-14 and 15. Cold and snow land first: seasonal temperature (about +8 °C in summer, −12 °C in winter, through AirTemperature and under the 2x chill cap), snowfall below about 0.5 °C, snow cover that builds and melts, and winter light. Admin commands `/season` and `/season-speed`.
**Who:** Justin ("1 day per season", "Cold and snow", "Seasons next", all the PM's recommendation)

## D-115 — 2026-10-16 — Visible new-arrival warmth
**Decision:** New characters get a visible "new arrival" warmth buff: for their first 30 minutes cold drains at half speed, and a small visible marker shows it. It does not apply after a death.
**Who:** Justin ("Give them a new arrival warmth buff they can see"; the PM had recommended an invisible version)

## D-116 — 2026-10-16 — Shelter warmth re-cut (amends D-057)
**Decision:** Warmth (the share of cold blocked) is: lean-to 0.2 with rain cover 0.6; hut 0.4 (0.5 with the door shut) with rain cover 0.9; house 0.5 (0.55 with the door shut) with rain cover 1.0. A fire inside the house still makes it fully warm.
**Who:** Justin (as the PM recommended)

## D-117 — 2026-10-16 — Fence and gate costs
**Decision:** Fence posts cost 2 sticks, rails 2 sticks + 1 fiber; gate posts 2 sticks, hurdle 3 sticks + 2 fiber. A 10 m pen comes to about 23 sticks + 6 fiber. A shut gate gives no warmth.
**Who:** Justin ("Cheap", as recommended)

## D-118 — 2026-10-16 — Cooking pot meals
**Decision:** Berry broth (coil pot of water + 3 berries) gives 45% hunger + 30% thirst and takes 60 s; mutton stew (water + 1 raw mutton + 2 berries) gives 65% + 30% and takes 90 s. Eating returns the pot; the pot never burns and keeps simmering if the fire dies; at most 2 pots per fire and 2 per player.
**Who:** Justin ("As proposed", as recommended)

## D-119 — 2026-10-16 — PM keeps merging
**Decision:** Justin allowed the Integration lane to merge, but an agent can't change sessions' permission settings, so the PM keeps executing merges and Integration prepares merge-readiness.
**Who:** Justin ("I do not want to do ANY work if I can help it"; he doesn't want to do any setup work himself)

## D-120 — 2026-10-16 — Hold wool until stations
**Decision:** #90 (wool to cloth plus the spin minigame) waits and ships together with the Spindle station; no temporary hand-craft spinning.
**Who:** Justin ("Hold for stations"; the PM had recommended merging now)

## D-121 — 2026-10-16 — Full fires refuse any overflow
**Decision:** A fire refuses fuel if any of the item's burn time would go past the cap; the fuel is kept.
**Who:** Justin ("Refuse any overflow"; the PM had recommended accepting when at least half fits)

## D-122 — 2026-10-16 — Wading chill stays uncapped
**Decision:** Standing in water (3x chill) stays outside the 2x chill cap; you can always step out.
**Who:** Justin ("Leave it", as recommended)

## D-123 — 2026-10-16 — Endurance skill
**Decision:** A new basic skill, Endurance, is trained by running (`move.run` process). Stamina run time and refill (D-111) grow with Endurance rank.
**Who:** Justin ("New 'Endurance' skill", as recommended)

## D-124 — 2026-10-16 — Claims next: wants-to-trade bubble
**Decision:** claims-8 shows a soft "wants to trade" bubble over a player who asks you to trade, in the chat-bubble style.
**Who:** Justin (as recommended)

## D-125 — 2026-10-16 — Options page gets a soft panel
**Decision:** The Options page sits on the same soft dark panel as the other menus.
**Who:** Justin (as recommended)

## D-126 — 2026-10-16 — Craft speed grows steadily with skill (amends D-113)
**Decision:** The skill part of craft time is linear in rank (×1.00 at rank 0, 0.91 at 15, 0.70 at 50, 0.40 at Master), because skill ranks already come quickly early on. Examples at rank 0/15/50/100: basket 28/27/22/13 s, stone axe 17/16/13/8 s, coil pot 38/36/29/18 s, storage jar 56/54/44/26 s.
**Who:** Justin ("Steadily", as recommended)

## D-127 — 2026-10-16 — Potter's bench costs a log and stones
**Decision:** The Potter's bench costs 1 log + 2 stones in one stage, placed inside a claim you have rights to; you work within 2 m of it. That means a stone axe and a felled tree come before the first pot, which lands roughly 20 to 30 min later than with a stick bench. First-evening path: fire, claim marker, stone axe, log, bench, clay, pinch pot, dry, fire.
**Who:** Justin ("1 log + 2 stones"; the PM had recommended 6 sticks + 2 stones + 4 fiber)

## D-128 — 2026-10-16 — Foot wraps ship before the tunic
**Decision:** The leather foot wraps and the equipment panel ship on their own first; the wool tunic follows with the Spindle station.
**Who:** Justin ("Yes, ship wraps first", as recommended)

## D-129 — 2026-10-16 — One world wipe, after the land reshape
**Decision:** Environment builds env-2 (more ponds, a stream, gentler hills) now. There will be exactly one world wipe, landing when env-2 and the in-flight PRs (stations, stamina, trade, clothing) are in. The PM brings Justin a wipe date to confirm before anything wipes. Until then every PR must upgrade the world without a wipe.
**Who:** Justin ("Build the reshape, then one wipe", as recommended)

## D-130 — 2026-10-16 — Wipe PRs pass by label
**Decision:** A `world-wipe` PR label makes CI skip the no-wipe upgrade check and print a loud "this PR wipes the world" notice instead. Only the PM adds it, and only after Justin confirms the wipe date; every other PR stays protected.
**Who:** Justin ("A 'world-wipe' label", as recommended)

## D-131 — 2026-10-16 — Release & Launcher lane
**Decision:** A new Opus Release & Launcher lane builds one-button updating. Every green merge to main becomes a versioned GitHub Release (clients and server module). A launcher for Linux and Windows downloads the latest client and has a server button that publishes the module over the existing world without wiping (refusing world-wipe releases unless Justin confirms). Fully automatic server updates from CI are proposed as options if they need setup or cost. Justin wants zero setup work.
**Who:** Justin ("New Release & Launcher worker"; "I want to be able to open the game, hit button, and it update the game/server")

## D-132 — 2026-10-16 — Raised hearth
**Decision:** A raised hearth is built inside a finished hut or house (4 stone, then 3 daub). A fire lit on it burns 1.5x as long per stick, can't be put out by rain, and makes a hut fully warm, as a fire already does in a house. Smoke rises through the roof. Crafting adds one small hook to read the hearth.
**Who:** Justin ("Better fire", as recommended)

## D-133 — 2026-10-16 — Releases and launcher
**Decision:** Justin's world runs on a local SpacetimeDB server on his own PC. Every green merge to main becomes a public GitHub Release (Linux and Windows clients plus the server module). The launcher has one button: it updates the client, publishes the new module over his local world without wiping (refusing world-wipe releases unless he confirms), starts the server if it's down, then offers Play. No cloud hosting, no cost, no setup.
**Who:** Justin ("Public GitHub Releases" and "Launcher button on your PC", both as recommended)

## D-134 — 2026-10-16 — Snow wets you at half rain's rate (amends D-114)
**Decision:** Falling snow makes you wet at half the rate of rain. Seasons run about +8 °C in summer and −16 °C in winter (winter days near freezing, nights about −8 °C), with Survival's chill still capped.
**Who:** Justin ("Half as fast as rain", as recommended)

## D-135 — 2026-10-16 — Sheep through the seasons
**Decision:** Flocks huddle tighter and graze less in snow, and lambs are born only in spring (up to 2 per flock each spring) instead of all year.
**Who:** Justin ("Behaviour + spring lambing", as recommended)

## D-136 — 2026-10-16 — Milder winter days
**Decision:** Winter days only cool you; the harsh cold comes at night. Atmosphere and Survival tune the winter day temperature or cold drain so winter days aren't near-freezing all day.
**Who:** Justin ("Milder winter days"; the PM had recommended keeping it for now)
**Final values (2026-10-16):** Mid-winter is about 12.6 °C at clear noon (chill about 1.16x), 10 °C overcast, 6 °C in rain and wind, and about −8.6 °C at night (at the 2x cap). Snow falls mostly at night and lasts into the afternoon.

## D-137 — 2026-10-16 — Spindle station
**Decision:** One wool station, a distaff stand with a log stool, built from your drop spindle + 4 sticks + 2 cordage + 1 stone. Both spinning (D-070 minigame) and weaving happen there.
**Who:** Justin ("One wool station", as recommended)

## D-138 — 2026-10-16 — Leather drying rack
**Decision:** A frame of two forked posts and a crossbar holding up to 3 hides; you scrape and stretch at it. It costs 6 sticks + 2 cordage, and existing racks keep drying where they stand.
**Who:** Justin ("Frame holding 3 hides", as recommended)

## D-139 — 2026-10-16 — Foot wraps wait for the rack (amends D-128)
**Decision:** The leather foot wraps (and with them the worn slots) ship once, already made at the Leather drying rack, not first as a hand craft.
**Who:** Justin ("Hold for the rack"; the PM had recommended shipping now)

## D-140 — 2026-10-16 — Name your camp
**Decision:** A claim's owner can name the claim in the claim panel; walking in shows the name softly for a moment ("Mira's camp" by default), and the land chip uses it.
**Who:** Justin ("Name your camp", as recommended)

## D-141 — 2026-10-16 — Forest light ships before the screenshots
**Decision:** atmosphere-13 (sun shafts and dappled light through the canopy, #136) merges without Justin having seen it; it gets tuned afterwards from the Render shots.
**Who:** Justin ("Merge now, tune later"; the PM had recommended waiting for the pictures)

## D-142 — 2026-10-16 — Renders never block merges
**Decision:** A PR that is green and matches decisions merges without waiting for Render shots or for Justin to see them. Renders still get made and shown to Justin afterwards, and any changes he asks for come as follow-up PRs. Wipes still need his confirmation of the date (D-129).
**Who:** Justin ("Don't hold up merges waiting on rendering, they can go in before I see them, but the work should continue and be logged correctly")

## D-143 — 2026-10-16 — Wipe whenever env-2 is ready (amends D-129)
**Decision:** Justin pre-approves the world wipe for whenever env-2 (the land reshape) is green and ready. No separate date confirmation is needed while he is the only playtester. The PM adds the `world-wipe` label (D-130) and merges it; the launcher still shows its wipe warning when Justin updates.
**Who:** Justin ("world wipe can be ready whenever it's ready, it's just me playtesting right now")

## D-144 — 2026-10-16 — First-evening tuning; visible breath in the cold
**Decision:** Survival's next milestone is a first-evening tuning pass on the new land (berries near spawn, water reach, early warmth) so nobody dies before their first fire. Players also show a visible breath puff in cold air; there is no camera shake.
**Who:** Justin ("1, but when cold I do like the idea of seeing your breath")

## D-145 — 2026-10-16 — Workers never sit idle
**Decision:** When a lane finishes its ticket and has no assigned next work, it immediately picks the next most useful item for its lane from the current milestones (`STATUS.md`, D-081) or the design docs, starts it, and tells the PM what it picked. Any design choice Justin would notice goes to the PM as options while the lane works on the recommended one. Waiting on CI or on a merge is not a reason to idle; start the next ticket.
**Who:** Justin ("Workers should not sit idle. If we don't have more work for them, pull from the design doc and figure some out, or ask me")

## D-146 — 2026-10-16 — Streams are wadeable
**Decision:** Streams are shallow enough to walk across (bed at water level minus 0.25 m; you get wet quickly per D-062); ponds stay deep. Every part of the land must be reachable on foot from spawn and from respawn points.
**Who:** Justin ("Wade across", as recommended), after Bug fixes found env-2's stream blocked all crossing

## D-147 — 2026-10-16 — Clay oil lamp
**Decision:** ceramics-9 is a clay oil lamp, pinched at the Potter's bench and pit-fired, fuelled with tallow (1 tallow per 2 raw mutton, rendered at a fire), with a warm glow about 4 m across. Each tallow lasts 30 minutes (3x the proposed 10), and lamps refill automatically from tallow in nearby chests and storage.
**Who:** Justin ("1, but lamp lights should auto-pull from chests if there's tallow in them, make them last 3* as long too")

## D-148 — 2026-10-16 — Crafted things are not chores
**Decision:** A design principle. Once something is crafted or built, it should not become one more thing to tend all the time. Upkeep is low: fuel-like needs pull automatically from nearby storage where sensible, and durations are generous. Lanes check new features against this.
**Who:** Justin ("I don't want things to feel 'chorish'. Once something is crafted it shouldn't become just one more thing to take care of all the time.")

## D-149 — 2026-10-16 — Hearth fires feed themselves
**Decision:** A fire on a built raised hearth (D-132) pulls sticks and logs from nearby chests and storage (baskets, jars, claim stash within reach) so it doesn't need tending. Open campfires still need feeding by hand.
**Who:** Justin ("Hearths only", as recommended), applying D-148

## D-150 — 2026-10-16 — First-time tips
**Decision:** Four one-time tips in the hint corner. (1) How to make a fire: two sticks and tinder, made with C (also shown at dusk if you have no fire). (2) Feed the fire with G. (3) Your claim keeps what you build; press B. (4) The crafting table: press G at it.
**Who:** Justin ("All four", as recommended)

## D-151 — 2026-10-16 — Log cabin
**Decision:** The log cabin is a forest alternative to the brick house, with the same shelter, size and weathering. Costs: sill 4 stone + 4 logs; walls 16 logs; rafters 12 sticks + 2 cordage; roof 14 fiber.
**Who:** Justin (as recommended)

## D-152 — 2026-10-16 — Corners of the forest
**Decision:** env-4 gives the region distinct moods: birch and alder wetland with sedge and cattails along streams, heath and meadow with heather, gorse and lone oaks on hilltops, mixed forest between. Visual only, no wipe.
**Who:** Justin (as recommended)

## D-153 — 2026-10-16 — Foot wraps train Leatherwork; the drop spindle stays Toolmaking
**Decision:** Leather foot wraps train Leatherwork; making the drop spindle trains Toolmaking.
**Who:** Justin (wraps: "Leatherwork", as recommended; spindle: Journal's default)

## D-154 — 2026-10-16 — Crucial look-and-feel choices wait for Justin's pick
**Decision:** Crucial look-and-feel choices (player/creature model style, overall art style, UI look, world palette) are not built past a rough prototype until Justin picks. The lane sends 2 to 4 written descriptions (plus renders if quick) to the PM, who brings them to Justin right away, not held for renders. The lane works on other things meanwhile (D-145 never idle). D-142 (merges don't wait on renders) does not cover these choices. Context: #172 merged style A as a default without Justin's pick; that pick is now being taken.
**Who:** Justin ("For something as crucial as a player model style I want at least the proposed descriptions of things even if a render isn't available.")

## D-155 — 2026-10-16 — Chore check: campfires need feeding, hand tools wear
**Decision:** A follow-up to D-148. Open campfires keep needing feeding (sticks 90 s, logs 6 min). Building tools/stations and structures have no durability, but hand tools and worn equipment do have durability (new; Crafting will propose how it plays, with Animals for clothing).
**Who:** Justin ("open fires need feeding, building tools don't have durability, but hand tools and equipment does.")

## D-156 — 2026-10-16 — Next animal: red deer
**Decision:** The next animal is the red deer (animals-14): shy woodland-edge herds that bolt at 30 m, hunted for venison, hide and antler.
**Who:** Justin (picked "Red deer (Recommended)")

## D-157 — 2026-10-16 — Claim fences
**Decision:** claims-12: members fence the soft border of a claim with a members-only gate, built with House building's Fence/Gate (Building owns the structure and model, Claims owns the membership rule and placement). The lived-in marker was not chosen.
**Who:** Justin (picked "Claim fences")

## D-158 — 2026-10-16 — Model style: Justin picks from renders
**Decision:** Justin will pick the model style (A, B or C) from renders. #172's style A is a placeholder default until then. Animation's player-model swap PR waits for the pick (see D-154).
**Who:** Justin ("I've seen renders of these, go find them since they've already been made.")
**Pick (2026-10-16):** Style A, "Carved", for people. Sheep: "I like a carved, but I want the sheep to be shorter, legs are too long, and I dont like the fluff. It has too much depth/texture and stands out from everything. It should be softer, cartoonier, fluffier. More stylized with our aesthetic."

## D-159 — 2026-10-16 — Sonnet workers do the coding; Opus for player-visual work
**Decision:** Coding can be done by Sonnet workers, and lanes start transitioning over to them. Anything related to modelling, texture work or player-visual things stays with Opus. This replaces D-058's "Opus for any session that writes code". Applied split. Sonnet: Integration, Release, Render, Crafting, Survival, Journal & skills, Claims, House building, Animals, Ceramics, Bug fixes, Audio (the Scribe is already Sonnet). Opus: Modeling, Animation, Environment, Atmosphere, UX/UI (player-visual), Wiki (writing voice, D-064), and the PM. Lanes hand off at a checkpoint (ticket current, `HANDOFF READY` to the PM), then the PM starts a fresh Sonnet session on the ticket. Sonnet lanes may still use an Opus helper for player-visual parts.
**Who:** Justin ("Let's update that coding can be done by sonnet workers, and start transitioning them over to it. Anything that's related to modelling, texture work, or player-visual things can be opus.")

## D-160 — 2026-10-16 — Claim gates are members only
**Decision:** Only the owner and members of a claim can open, bar or unbar a gate in it (claims-12).
**Who:** Justin ("Members only")

## D-161 — 2026-10-16 — Every pickup has a sound
**Decision:** Every pickup plays a soft rustle and knock (audio-3).
**Who:** Justin ("Yes, every pickup")

## D-162 — 2026-10-16 — Claim fences snap to the border
**Decision:** Claim fences snap onto the claim circle (claims-12; extends D-157).
**Who:** Justin ("Snap to border")

## D-163 — 2026-10-16 — Lapsed claim fences stay as ruins
**Decision:** When a claim lapses, its fences stay as ruins: they become ordinary fences, gates open to anyone, and they weather per D-046.
**Who:** Justin ("Stay as ruins")

## D-164 — 2026-10-16 — Heather blooms purple in late summer
**Decision:** Heather blooms purple in late summer (Atmosphere and Environment seasonal visuals).
**Who:** Justin ("Yes, purple bloom")

## D-165 — 2026-10-16 — Sheep shape: A Cloud for now
**Decision:** The PM went with Modeling's recommended sheep shape, A Cloud: soft merged lumps, legs about 30% shorter, no wool relief (follows D-158's sheep feedback). Justin may switch after the renders.
**Who:** Justin ("No preference"); the PM picked Modeling's recommendation, A Cloud

## D-166 — 2026-10-16 — env-5 landmarks and trails vary per instance
**Decision:** env-5 builds landmarks and trails. A general design principle: repeated world features vary strongly per instance. If a landmark or feature appears in the world several times, each instance should look quite different (randomized a bit every time), so a player doesn't instantly recognise a pre-made, tiny-zone landmark.
**Who:** Justin ("I don't want things like it to be repeated constantly. If they go into the world multiple times they should look quite different/randomized a bit every time, so a player doesn't instantly recognize that this is a pre-done rendered type of area/tiny zone landmark.")

## D-167 — 2026-10-16 — Slower thirst at first (survival-10 spawn water)
**Decision:** For survival-10 (spawn water), thirst falls 20% slower in the first hour.
**Who:** Justin ("Slower thirst at first")

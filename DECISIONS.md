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

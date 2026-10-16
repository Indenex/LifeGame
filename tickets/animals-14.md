# animals-14: Red deer
Lane: Animals
Owner: Animals lane (handed off 2026-10-16 by session_01SAcVBUUfPFFk1rdXuiQZRU to a new Sonnet session, D-159)
Status: in_progress
Branch: claude/animals-deer-5gkntn (stacked on claude/animals-seasons-5gkntn / PR #195; #173 is merged and #195 now targets main)
PR: #219 (draft; base claude/animals-seasons-5gkntn, retargets to main when #195 merges)

## Goal
A second species (Q12 a, recommended; pending Justin via the PM): small herds of red deer at the woodland edges, far shyer than sheep. They notice a moving player from about 30 m and bolt into the trees; a hunter has to wait still at the edge or sneak in from downwind... (no wind yet: just slow and still). Hunting gives venison, a deer hide and an antler. Done when Justin sees a herd at a wood's edge in the evening, and can bring one down and butcher it.

## How it works
- Server `Animals.cs`: `AnimalSpecies.Deer` appended. The Sheep* constants became per-species `AnimalTraits` (Walk/Trot/Run, Notice, CalmSpeed, HomeRange, FleeAhead, WanderMin/Max, WoodPref, Curious, StrikeScale, Wary); sheep keep exactly their old numbers.
  - Deer: walk 1.1, trot 2.4, run 6.5 m/s; notice players from 30 m; closing faster than 1.2 m/s alarms; inside Wary = 10 m any movement unsettles them (a still player never does), so hunters wait at the edge for deer to graze near; bolt 12 m ahead; home range 18 m; leaders wander 4-12 m and prefer woodland 0.45 (the edge), sheep keep preferring open grass; never walk up to still players; a strike does 1.6x the damage (a spear kills in one strike).
  - Two herds (4 near (70,45), 3 near (-60,-75), settling at a wood's edge within 22 m). EnsureAnimals adds them to any world that has no deer yet (sheep-only worlds get them on the next connect; additive, no wipe).
- `Hunting.cs`: butchering a grown deer drops 2 Raw venison (202), 1 Deer hide (203) and 1 Antler (204); a fawn 1 venison. `hide.scrape_deer` scrapes a deer hide into the same Scraped hide (hand craft here; moves to the drying rack once #90 is on main).
- Fawns: the lamb logic (spring-only via #195, follow the mother) works for deer unchanged.
- Client `Sheep.cs`: the same skinned node and animation for both species; a deer mesh (slim barrel, deep chest, long neck and legs, big ears, red-brown coat in 3 shades, pale rump/belly/throat, dark nose and hooves) and a stag mesh with antlers (about a third of deer; stags 10% bigger).

## Progress log
- 2026-10-16 23:35: started (D-145) while Q12 is with Justin. Branch from the seasons branch to avoid conflicting with my own Animals.cs changes.

- 2026-10-17 00:20: PR #219 opened (draft). Render requested 987b355. Asked Crafting (cook.venison, antler use), Atmosphere/Audio (sheep-only bleats, deer calls), Modeling (polished deer + item models). Q13 (one-strike kill / venison count) sent to the PM.
- 2026-10-17 00:10: server + client written, build. Local bot test (scratch DeerCheck, TEMP give/teleport reducers, not committed): 2 herds (4 and 3) settle on dry ground at wood edges; walking at 4.2 m/s, sheep bolt at 7.0-7.9 m and deer at 17.6-20.6 m; sneaking at 1.5 m/s, sheep never bolt and deer bolt at 7.4-10.1 m; one spear strike brings a deer down; butchering gives 2 Raw venison, 1 Deer hide, 1 Antler. Upgrade check against the seasons branch passed, and the upgraded sheep-only world gained both deer herds.

- 2026-10-16 20:15 (UTC): Justin picked Q12 (a) deer. Merged #195's main merge (54d2175) in; server + client build; upgrade check against main passed. Crafting answered: add Roast venison 205 + Burnt venison 206 and a `cook.venison` ProcessDef copied from cook.meat (Outcome.Roast, Tier 1, Seconds 1) in this PR; check Roasting.cs's burnt mapping covers venison. Antler use goes to Justin as options (a, rec: antler soft hammer = Hammer tag with +10 knap.* cap / b: pressure flaker tool for a new fine-flake recipe / c: plain material for now). Modeling will model the deer in the carved style (specs sent: same skeleton/gait, DeerHeadAt/DeerHips, 3 coats, stags 1/3 +10% with antlers, items 202-204). Animation will smooth Sheep.cs _Process species-neutrally after Modeling's #197.

- 2026-10-16 20:12 (UTC): Modeling made the carved red deer: client/scripts/models/DeerFigure.cs on claude/model-2-env (lands with Modeling's follow-up PR after #197). Same bones, rests and two surfaces as the stand-in. When it is on main, in Sheep.cs set `deerMesh = DeerFigure.Build(false, plainMaterial); stagMesh = DeerFigure.Build(true, plainMaterial);` and delete BuildDeer (or let Modeling make the swap). Item models for 202-204 are done there too.

- 2026-10-16 20:20 (UTC): Justin answered (via the PM), now the plan for #219:
  - Q13: "One spear strike kills if your sneak skill is high enough"; otherwise it wounds. Give deer health (and sheep too, for consistency). A wounded deer bolts, then slows and stops to rest as it weakens, so it can be chased down: hunting a deer is wounds plus a chase. (Today: StrikeScale 1.6 makes any spear strike kill; replace with a sneak-skill gate. Skills lane owns the sneak skill: ask Journal and skills, session_013QuyGdXAWemhkois2ictmc, how to read it.)
  - Sounds: alarm bark on Run plus stags roaring at autumn dusk; brief sent to Audio (session_01SXcxtoyeqDmypiHxkHedU6).
  - Antler: a plain crafting material, plus an antler hammer or pick recipe, with Crafting (new session_01N9APthjRDfjknUvCweqzzN).
  - Bow and arrows: a new ticket animals-15 with Crafting. Client auto-targets (nearest huntable in a cone in front, within range); the server resolves each shot instantly in one reducer (no projectile simulation; the client animates the arrow). Damage = bowPower x arrowPower x (0.7 + 0.3 bowQ) x (0.7 + 0.3 arrowQ) x rangeFactor x rand(0.85, 1.15); with deer HP 100 the first crude bow and arrows do about 17-33, so 3 to 6 arrows; hit chance falls with distance and target speed. PM is proposing the formula to Justin. Keep #219 on spear plus sneak first.

## Open questions / blockers
- Q13 answered (above): sneak-gated one-strike kill, wounds plus chase otherwise.
- Deer voices: Atmosphere gated Sheep.Seen to sheep only in its PR #217 (294c458), so deer are silent; the deer calls (alarm bark on Run, stags roaring at autumn dusk) are routed via the PM to Audio's new session.
- Justin wants durability on hand tools and worn clothing (tunic, wraps): Crafting (new Sonnet session, tickets/crafting-19.md) proposes how it plays; coordinate for clothing (Clothing.cs / worn_item).

## Lane handoff (2026-10-16, D-159): everything the Animals lane has open
- #90 animals-8 + animals-11 (wool, spin minigame, Spindle station, Leather drying rack): branch claude/animals-wool-cloth-5gkntn, draft, held for Crafting's station framework #160 (D-120). Tickets animals-8.md, animals-11.md on that branch. When #160 merges: merge main, keep SpindleStation and DryingRack at the very END of StructureKind / Structures.Shapes / StageCosts (after CraftingTable 7 and Ceramics' PottersBench 8 if it is on main), run tools/upgrade-check.sh origin/main (check its exit code, do not pipe it), mark #90 ready, send the PM the green sha. Crafting renamed StationInfo.Name(Spindle) to "spindle station" and reports build.spindle / build.rack in #160. Crafting-16 (D-113) will set hide.scrape 12 s, hide.stretch 6 s; it asked for spin/weave effort: answer spin about 1, weave about 2.
- #149 animals-12 (leather foot wraps made at the drying rack, worn_item / Wear / TakeOff, Survival hooks): branch claude/animals-wraps-5gkntn, draft, based on #90's branch (D-139). After #90 merges: merge main, mark ready, send sha; then tell UX (session_01V6qgbNbvTwniqW946KACLr) its equipment panel #151 can follow.
- #195 animals-13 (sheep huddle in snow, spring-only lambing, D-135): branch claude/animals-seasons-5gkntn, base main now (54d2175), CI running at handoff. Green: send the PM the sha.
- #219 animals-14 (this ticket): draft. After #195 merges, merge main; do the venison cooking above; un-draft once Q13 is answered and send the sha.
- animals-9 (wool tunic) on claude/animals-clothing-5gkntn: blocked, ships after #90 (needs wool cloth; sew.wool_tunic at the crafting table per Crafting); that branch still holds old code from before the wraps split: after #149 merges, merge main and keep only the tunic (tickets/animals-9.md there).
- Ceramics (session_01Sc3N1RCsurSszfX5YwGzMU) needs mutton for ClayCheck --lamp: told them the strike-twice-then-butcher recipe works today; still to do: a test-only `DebugGiveMeat(count)` reducer behind a CI-only flag (like set_claim_lifetime), in Animals' files, then tell Ceramics its name.
- Render (new session; queue in tickets/render-1.md on claude/render-1-013qe8) has requests for animals-11 stations (de48c3f), animals-13 seasons (04f4712) and animals-14 deer (987b355); results go to the PM.
- Bug fixes (session_01AeuMsF8LfzDsWW8jUh1Fkp, bugfix-23, D-170) asks: sheep and deer should not walk through tree trunks or boulders. Cheap server check in AnimalStepBlocked (AnimalFires.cs): refuse a step ending inside a trunk circle, Trees.GrowsAt(cell) plus Trees.Trunk, radius about 0.26-0.38 m (+ animal body ~0.3 m), and boulders via Rocks.LiesAt/Rocks.Center/Rocks.Radius; saplings are client-only, ignore them. Rules mirror Terrain.CollideVegetation on claude/bugfix-23. Do it as its own small PR from main.
- Local test bots used (scratch only, not in the repo): wool/clothing/station/season/deer checks with TEMP give/teleport/season reducers. To re-test, write a TEMP reducer file in server/spacetimedb/, publish locally, regenerate bindings, run, then delete the file and regenerate bindings before committing.

## Next step
Add Roast venison 205, Burnt venison 206 and `cook.venison` (copy of cook.meat) to this branch, build, push; then work the Lane handoff list above in order.

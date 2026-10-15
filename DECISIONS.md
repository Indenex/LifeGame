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

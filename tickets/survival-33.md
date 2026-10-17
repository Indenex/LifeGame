# survival-33: Land the unlanded survival work from 2026-10-16
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: in_progress
Branch: claude/survival-5zelch
PR: #832

## Goal
Port what the first Survival thread finished but never opened as a PR (`/mnt/project-files/survival/unlanded-2026-10-16/`) onto current main, one piece at a time:
1. Wake spot: after a death with no waking place, wake on the rim of the starting area under open sky (no standing tree within `Needs.WakeOpenRadius` = 4 m), else the least crowded spot. 4 rings x 24 bearings.
2. Death pointer: moved to its own ticket, survival-34 (waits on Justin's pick).
3. "The water is bitterly cold" note when you step into water that chills you (at most once a minute). The old torso shiver is dropped: PlayerAnimator already shivers in the cold (D-313).
4. Bush reach measured to the leafy edge: `Needs.BushRadius` 0.6, server accepts within `BushPickDistance` (2.5 + 0.6) of the center, the client offers it 0.2 inside that and reports the edge distance to Use (old PM playtest 5 note).
Already on main in another form, not ported: clothing hooks (Clothing.cs, `ClothingWarmthHook`), berry bush merged mesh (BerryBush.cs builds one ArrayMesh).

## Progress log
- 2026-10-17 15:05: wake spot ported (`WildernessEdge` + `TreeCrowding` in Survival.cs, `Needs.WakeOpenRadius`). Offline simulation on the shared tree rule (scratch console app over shared/): an open spot for 360/360 start bearings; the old rule's first dry bearing was open for only 98/360, so most deaths woke within 4 m of a tree. SurvivalCheck gains an AdminDie case at its end checking no tree within 4 m of the waking spot. Server compiles (`dotnet build -p:RuntimeIdentifier= -p:UseAppHost=false`; wasi workload and spacetime CLI are not reachable from this container, so SurvivalCheck runs only in CI).

- 2026-10-17 15:30: cold-water note and bush reach to the leafy edge added to #832. Server compiles.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #832 and send the green sha to the PM. If #826 merges first, merge main into claude/survival-5zelch (SurvivalCheck tail conflicts with #826's stamina cases: keep both).

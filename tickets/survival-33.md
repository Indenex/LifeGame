# survival-33: Land the unlanded survival work from 2026-10-16
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: in_progress
Branch: claude/survival-5zelch
PR: (not yet)

## Goal
Port what the first Survival thread finished but never opened as a PR (`/mnt/project-files/survival/unlanded-2026-10-16/`) onto current main, one piece at a time:
1. Wake spot: after a death with no waking place, wake on the rim of the starting area under open sky (no standing tree within `Needs.WakeOpenRadius` = 4 m), else the least crowded spot. 4 rings x 24 bearings.
2. Death pointer: death card line "Your things lie where you fell, to the northwest." plus a faint shaft of light over the death spot (needs Justin's look approval, D-154 does not apply; screenshot existed: screenshots/17-survival-death-card.png).
3. Cold water shiver note ("The water is bitterly cold", max once a minute).
Already on main in another form, not ported: clothing hooks (Clothing.cs, `ClothingWarmthHook`), berry bush merged mesh (BerryBush.cs builds one ArrayMesh).

## Progress log
- 2026-10-17 15:05: wake spot ported (`WildernessEdge` + `TreeCrowding` in Survival.cs, `Needs.WakeOpenRadius`). Offline simulation on the shared tree rule (scratch console app over shared/): an open spot for 360/360 start bearings; the old rule's first dry bearing was open for only 98/360, so most deaths woke within 4 m of a tree. SurvivalCheck gains an AdminDie case at its end checking no tree within 4 m of the waking spot. Server compiles (`dotnet build -p:RuntimeIdentifier= -p:UseAppHost=false`; wasi workload and spacetime CLI are not reachable from this container, so SurvivalCheck runs only in CI).

## Open questions / blockers
- (none)

## Next step
After PR #826 merges, merge main into claude/survival-5zelch (SurvivalCheck tail conflicts with #826's stamina cases: keep both), open the PR, then port the death pointer.

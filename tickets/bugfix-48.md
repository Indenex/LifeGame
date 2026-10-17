# bugfix-48: giant oaks block on the server (landmarks, part 1)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-48
PR: (see PR list)

## Goal
PM idea after #660: landmarks (giant trees, outcrops) become server obstacles too. Part 1 (this ticket): the giant oak's broad trunk. `Landmarks.GiantGirth` moves to shared (the client's `LandmarkProps.GiantGirth` delegates to it, same seed), and `Obstacles.At` returns the trunk (radius `TrunkRadius(GiantGirth)`) on a feature cell with `FeatureBlock.Trunk`, before the forest roll (a landmark cell has no roll of its own). So the server (`PlayerStepBlocked`), the check bots (`BotGround`) and the client all refuse the same trunk. SpawnCheck probes a giant oak the way it probes trunks and boulders; its first walk west now slides round obstacles.

## Progress log
- 2026-10-17: implemented; SpawnCheck 6 of 6 local runs pass (including the giant oak probe: kept 1.03 m from its middle, radius 0.63).

## Open questions / blockers
- Part 2: outcrop stones (`FeatureBlock.Stones`). Their positions and radii come out of `client/scripts/environment/LandmarkProps.cs` (Outcrop, Rock, Formation, and the stones round a giant oak's foot), which uses Godot types. Plan: move the position/size/radius maths into shared (a `Landmarks` function returning the blocking circles for a feature cell) and have the client builders read it, so client and server cannot drift. LandmarkProps is Environment's file: tell them before touching it.

## Next step
Wait for CI on the PR, send the green sha to the PM, then ask Environment about part 2.

## Part 2 (bugfix-49): the outcrop stones, a pure move
- Branch claude/bugfix-49. `shared/LandmarkBlocks.cs` now holds the outcrop layout (`Outcrop(x, z, atX, atZ)` returns pieces: kind, seeds, position, size, tipped), the boulder and formation proportions (`RockStretch`, `FormationStretch`), their radii, `Circles(feature, x, z)` and `Near(px, pz)` (stones by square, cached). The client's `LandmarkProps.Outcrop` draws from the pieces; `Rock`/`Formation` take their proportions from shared; `Lists.Blocks` is gone; `TerrainCollision.CollideFeatures` reads `LandmarkBlocks.Near`. `Obstacles.StepBlocked` and `PushOut` read it too, so the server and the check bots refuse outcrop stones.
- Pure move, checked two ways: (1) old circles from the unchanged builders (headless Godot, 269 outcrops) against `LandmarkBlocks.Circles`: 1209 circles, 0 differences; (2) every transform and colour the builders hand to the terrain for outcrops and giant oaks over 17 x 17 squares, old main against this branch: identical files (2284 lines).
- The giant oak's foot stones never blocked (only its trunk does) and still do not.
- SpawnCheck probes an outcrop stone: the server kept the player 0.96 m from the middle of a 0.56 m stone; 4 of 4 local runs.
- Environment's #718 (a fourth feature, FallenGiant) can add one case to `LandmarkBlocks.Circles`/a piece list; its geometry was sent by Environment.

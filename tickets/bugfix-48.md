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

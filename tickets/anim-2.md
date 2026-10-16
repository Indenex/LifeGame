# anim-2: Station pose, chopping swing, breath in the cold, sheep motion
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: in_progress
Branch: claude/anim-2
PR: #187 (chop swing + breath scaffold)

## Goal
After anim-1 (#100): (1) the standing "working at a station" pose plays for station crafts (D-098, Crafting #160) and the player faces the station; (2) chop.log plays a two-handed axe swing facing the tree; (3) a visible breath puff from players in cold air (D-144, with Survival); (4) smoother sheep motion (with Animals, who own Sheep.cs). Cheap for 32 players. Justin sees clips via Render (renders never hold up merges, D-142).

## Progress log
- 2026-10-16 19:35: started from #100 head + main (PM, D-145 never idle). Chop: WorkMotion.Chop (wide stance, wind back over the right shoulder, whip across into the trunk, ~0.85 swings/s), chop/fell families map to it, your avatar faces the nearest trunk within 3 m (shared/Trees.cs) when the job starts. Previously chop.log mapped to kneeling Carve. Client builds.
- 2026-10-16 19:40: #100 green on 25764cc (run https://github.com/Indenex/LifeGame/actions/runs/38953389730), sent to PM to merge. Asked Survival for a cold signal and Animals about Sheep.cs.
- Station pose + facing already built on claude/anim-1-stations (needs #160); merge it here after #160 merges.

- 2026-10-16 19:50: bcc7f86: `animation/Breath.cs`, a CpuParticles3D on each avatar's Head (or a "HeadAttach" bone attachment on Modeling's skeleton, asked for): a 6-particle faint white puff every 3-4 s from the mouth, staying in world space, when static `Breath.Coldness` (0..1) > 0. Nothing sets Coldness yet, so it's invisible until Survival's cold signal is wired. Client builds.

## Open questions / blockers
- Breath puff: single source now (Survival 19:36): read `Needs.Breath(Needs.AirCold(Atmosphere.AirCelsius))` (0 at >=6 °C, 1 at 0 °C; shared/Needs.cs in #185 cefba1f; Atmosphere.AirCelsius in #173). Needs both on main. Atmosphere asks the puff fade in heavy snow / at night if too bright (`Atmosphere.Snowfall`); they'll review the look.
- Sheep: asked Animals what they want smoothed and how to hook into Sheep.cs.

## Next step
Wait for CI on #187, send the green sha to the PM; then wire Breath to SurvivalClient.AirColdness after #185 merges, and the station pose after #160 merges.

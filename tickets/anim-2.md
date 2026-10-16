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

- 2026-10-16 19:55: Modeling's #172 merged (PlayerModel.Build/Wear on main). b0ec9d4 on branch claude/anim-2-playermodel (stacked on claude/anim-2 / #187): Avatar uses PlayerModel.Build() (Skeleton3D, one draw call per player) and Wear(Body, 0) for the leaf wrap; capsule figure removed. Justin picked style A Carved (PM 19:58); PR opened. Breath attaches to "HeadAttach" once Modeling's #197 lands; then place it at PlayerModel.MouthAt instead of (0, 0.03, 0.17). Render requested b0ec9d4.
- 2026-10-16 19:55: #187 merged (chop swing + breath scaffold). #185 merged (Needs.Breath on main); breath still waits on Atmosphere's #173 (AirCelsius). Branch claude/anim-2-remote-work from main: other players' work poses from the public craft_work table (insert/delete, and on avatar creation). Client builds.
- 2026-10-16 20:18: #208 (PlayerModel, style A Carved) merged.

- 2026-10-16 20:30: #160 merged (stations). Branch claude/anim-2-stations (on #205's branch + main): station processes (ProcessDef.Station != None, e.g. weave.basket at the Table) play the standing Station pose, and your avatar turns to face Stations.NearestOf when the job starts. Supersedes claude/anim-1-stations. Client builds. PR after #205 merges.

## Open questions / blockers
- Breath puff: single source now (Survival 19:36): read `Needs.Breath(Needs.AirCold(Atmosphere.AirCelsius))` (0 at >=6 °C, 1 at 0 °C; shared/Needs.cs in #185 cefba1f; Atmosphere.AirCelsius in #173). Needs both on main. Atmosphere asks the puff fade in heavy snow / at night if too bright (`Atmosphere.Snowfall`); they'll review the look.
- Sheep hit reaction (Animals, 20:34): public table AnimalHit {AnimalId, X, Z, Crit, Killed} (PR #219). Agreed: Herd.cs calls `Sheep.Flinch(bool crit, Vector3 from)` on insert (stub is in #219, branch claude/animals-deer-5gkntn, marked `// anim: stub`; keep the signature; from = hitter world pos (x,0,z)); Animation fills it in: normal hit = body jerk away + head snap, crit = hind-leg buckle and a 0.5 s stumble before the bolt.
- Sheep: Animals OK (a) 20:08: small PR on Sheep.cs _Process only, species-neutral (deer from #219 share skeleton and _Process; deer run 6.5 m/s, sheep 4.4, longer strides). Build after Modeling #197 (new sheep mesh, lower rest heights). Fix: feet skate at trot (stride from speed), turns snap via LerpAngle (add lean, head leads). Animals now a Sonnet session; follow-ups via tickets/animals-14.md and PM.

## Next step
Get the remote-work PR green and send the sha to the PM; then breath after #173, station pose after #160, PlayerModel swap PR (claude/anim-2-playermodel) after Justin picks a style, sheep after Animals replies.

# env-31: Ghibli ground everywhere (from the BotW / Ghibli prototypes)
Lane: Environment
Owner: Environment thread (session_014EL1FHDJi92N6gKixNv6Q5)
Status: in_progress
Branch: claude/environment-ground-looks (old account); refresh rides claude/environment-np240l after env-37
PR: #833

## Goal
Justin passed on old, A and B+ (env-27) and picked two reference directions to see:
(3) Breath of the Wild / Genshin and (4) Ghibli-like painted, both fitting D-313. Rough
prototypes behind `--ground=botw` and `--ground=ghibli`. The grass is the main lever, and
the PM lifted D-256's shape lock for this comparison only. Paired stills today | botw |
ghibli at the spawn meadow, a path edge and the stream bank, noon plus golden hour, from
the gameplay camera, through Render A (session_015S96hNrEu92FrCoNZqZZjs). Report the
draw and frame cost.

Built on top of env-27 (merged in) so `PaintedTerrain.Chosen` picks the style.
- `client/scripts/environment/GroundLooks.cs`: per-look tuft mesh, tuft cover and flower
  amounts.
- `Look.cs` grass shader: `look` uniform (1 botw, 2 ghibli) colours, plus bigger botw waves.
- botw: old ground beneath (mostly hidden); 169 blades/m² vs 100, 0.36–0.70 m tall.
- ghibli: painted ground A beneath, stepped painted light on the grass, 100 wider blades,
  flower patches at noise > 0.52 (was 0.62) filled 75% (was 45%), mixed lone flowers.

## Progress log
- 2026-10-17 08:40: first build; local renders at (-6,-10) running.

- 2026-10-17 08:50: first local renders at (-6,-10): botw too pale and minty, so I made it
  richer; ghibli grass good, ground too blotchy, so I softened it (SoftPaint); ghibli flowers 1.35x.
  Paired stills requested from Render A.

- 2026-10-17 09:10: merged main (soft edges, 963d4c10). Local renders at (-6,-10) look right
  (botw lush tall green, ghibli soft painted ground and flower clusters). Cost in view at the
  gameplay camera (radius 3, frame stats): draw calls 595 / 595 / 596 (today / botw / ghibli),
  primitives 2.13M / 2.32M (+9%) / 2.14M (+0.7%). Tufts only within 48 m, so the cost stays small.

- 2026-10-17 15:20: new session. Merged current main (local `env-31-local`): Look.cs keeps
  main's golden-hour wrap (atmosphere-40) for the old ground and env-27's stepped light for
  the painted ones; tufts use GroundLooks.Tuft() ?? BuildTuft(GrassDetail.Keep), and the
  Grass detail setting leaves the botw/ghibli tufts alone. PM put the pick to Justin as a
  card, recommending ghibli. Not built here (no Godot); CI builds it when pushed.

- 2026-10-17 15:25: Justin picked Ghibli (via the PM). It is now the default ground:
  PaintedTerrain.Chosen falls back to Ghibli, `--ground=old` draws the old ground for
  before/after renders, botw/painted/tiles stay behind --ground= for comparison. The Ghibli
  tuft follows the Grass detail setting (Low keeps about half the blades, same hash as before).

## Open questions / blockers
- (none)

## Next step
Wait for #833 CI; send the PM the green sha. Render requested 179bc02b (before --ground=old / after, noon and golden hour).

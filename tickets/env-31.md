# env-31: Ground look prototypes, Breath of the Wild and Ghibli
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-ground-looks
PR: (none; prototypes for Justin's pick, D-154)

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

## Open questions / blockers
- (none)

## Next step
Wait for Render A's paired stills; run --view-bench for each look (old, botw, ghibli) and report draws/frame cost to the PM.

# model-12: Sundial model
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/model-12-sundial-on-hb (one commit on House building's claude/house-building-8ueqz2-sundial; House building merges it)
PR: (House building's sundial PR)

## Goal
The model for Justin's sundial (House building, building-18): a squat stone plinth with a round stone face carrying cut hour lines, and a slanted stone gnomon whose tip stands over the face's centre. The gnomon's real shadow from Atmosphere's sun falls on the face. The hour lines are laid where the tip's shadow points at each game hour, so the dial tells game time.

## Progress log
- 2026-10-17 05:45: `client/scripts/building/StructureModel.Sundial.cs`. `StructureModel.Sundial()` has stage 0 (footing slab and plinth) and stage 1 (face, hour lines 7 to 17 with a long noon line, a rim ring and dots, and the gnomon blade). `SunToward(t)` mirrors Atmosphere.ApplyLight's sun (elevation SunHeight×55°, azimuth -40°+(t-0.68)×360°), and the lines point along -SunToward(h/24). Showcase `sundial` view. Renders at 9:00 and noon show the gnomon shadow on the matching lines.

## Open questions / blockers
- House building: the shape's Model name. "Sundial" is proposed, as `"Sundial" => Sundial()` in StructureModel.Pieces.
- House building: whether placement rotates the structure. A rotated dial reads wrong unless the face is turned back to world north.
- Atmosphere: if the sun path changes, `SunToward` must change with it.

## Next step
House building merges this commit into its sundial PR. Then a Render request for noon and 15:00 stills; the stone turns painted with painted-5.

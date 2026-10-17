# env-30: Ambient life (butterflies, pollen and seeds, fireflies by water)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-ambient-life
PR: #656

## Goal
Justin's cutesy pick #12: small life in the air, calm and cosy, client-only and cheap,
density tied to a graphics setting.
- Butterflies by day over the wildflower patches: soft pastel wings (brimstone, cabbage
  white, peach, lilac blue), wobbly flight from flower to flower, settling with wings up.
- Drifting pollen (spring to high summer) and dandelion seeds (late spring, summer) over
  open meadows, carried by the wind.
- Fireflies: Atmosphere's existing Fireflies, given a higher amount at dusk near ponds,
  streams and wet ground in the warm half of the year (not duplicated).
- Falling autumn leaves and snowflakes already exist (Atmosphere's FallingLeaves and Rain),
  so nothing new there.
- Options -> Graphics -> "Ambient life: Off / Some / Full" (graphics/ambient_life, default
  Full). Some halves the butterflies, motes and fireflies; Off turns all of them off.

Code: `client/scripts/environment/AmbientLife.cs` (a 14-instance MultiMesh moved on the
CPU, plus two GPU particle systems in a box round the player). Hooks: three lines in
`atmosphere/Atmosphere.cs` (create, Follow, firefly amount), and the button in
`ui/OptionsPage.cs`. `--life=1` holds everything at full for renders.

## Progress log
- 2026-10-17 07:40: built; local renders at the (-6,-10) meadow show butterflies over the
  flowers. Wingspan 0.34 m (size 0.17), slightly larger than life to read at the default zoom.

## Open questions / blockers
- Butterfly size and how many (14 at Full) are design knobs for Justin via the PM.

## Next step
Wait for PR CI; send the green sha to the PM. Render clip requested (meadow at -6,-10, --life=1).

# ceramics-13: Clay trough (pipe network designed, parked)
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU)
Status: in_progress
Branch: claude/clay-ceramics-13-kfr4kr
PR: (none yet)

## DECISION (2026-10-17, Justin via the PM): option 4, trough and drains only, no pipe network for now.
Build a fired clay **trough** you fill by hand (pour from water pots). It is placeable, holds a good amount, players (and animals, via an Animals hook) can drink from it, and needs no upkeep (D-148). Drain tiles only if they serve a real purpose; if not, leave them out and tell the PM. A Def entry per docs/CONTENT.md, small, and rendered. The pipe-network design below stays on file for later.

## Goal (original, now parked: pipes)
Justin picked clay pipes and drain tiles for water (2026-10-17). Done when a player can fire clay pipe segments, lay them from a water source to a sink in their claim, and have water arrive with no tending (D-148). This ticket is the design note; no code until Justin picks the core loop.

## Design note

### 1. What players use pipes for
- **Water at home (core).** A pipe from a stream or pond to a *cistern* (or trough) in the claim. A cistern is a place to fill pots and drink without walking to the stream: a real chore saved, which is the point of crafted things (D-148).
- **Rain from the roof (extension).** Short pipes (gutters) from a tiled roof to a cistern collect rain. Ties to the roof tiles (ceramics-10); weather-dependent, no terrain slope needed.
- **Drains.** Draining a building's floor or a wet patch: a small cosmetic and comfort effect, later.
- **Gardens (future).** Irrigation feeds beds once a farming lane exists.

### 2. Cheap, server-side water (D-182)
Not fluid physics: a **graph**.
- Nodes: `Source` (a stream or pond tile, a spring), `Pipe` segment, `Sink` (cistern, trough, garden bed).
- Connectivity is computed when a segment is finished or removed (not every tick): walk from each source along adjacent segments and mark each reachable sink `Fed` with that source. Segments connect when their end points are within a snap distance and face-to-face.
- A sink's water is **lazy**: it stores a level and `lastFilledAt`; when read, the server adds `rate * (now - lastFilledAt)` up to the cistern's capacity (rate and capacity start low and grow, D-075). No per-tick work for water.
- Gravity: each segment end carries the ground height; water only flows downhill or level (server checks `World.Height` at both ends; a rising run is refused when laid, with a clear message). A source's reach is limited to a maximum run length to start (for example 12 segments), raisable (D-197).
- Server decides everything (reach, claim rights, slope, length); the client only draws.

### 3. Making and laying
- **Making.** `clay.pipe` at the Potter's bench (like the tile): tempered clay, a shape minigame, a `ClayForm.Pipe`; fired in a kiln only (an even heat, like the jar and tile). 1 tempered clay for 1 one-metre segment to start. Drain tiles are a shorter, flat variant of the same piece.
- **Laying.** Placed as a structure (a Def with a `Water` flag: Source, Pipe, Sink), snapping end to end like fence runs (`SnapFence`) so players don't need to line segments up by hand. Allowed outside a claim only near the source; the cistern and the run into the claim need claim rights (building-13).
- **Above or buried.** Start above ground (pipe lies on the terrain; a low step-over collider like a fence that animals and players can cross). Buried pipes (a trench with the digging stick, a terrain edit) are an extension.
- **No upkeep (D-148).** Pipes never clog or leak. They weather only like fired clay outside a claim (48 h untouched, building-13's rule) and are fine inside one.

### 4. Other lanes touched
- **Environment:** water sources (`Land.StreamDistance`, ponds, springs) and a shared `WaterAt(x, z)` query; where springs are.
- **Survival:** drinking and `FillPots` ask "is there water here?"; they would also count a fed cistern or trough within reach.
- **House building:** placement rules, claim-only flags, the Structure `Water` flag and its row; roof gutters later. building-12 folds `StageCosts` into the shape, so the cistern and pipe are single Def entries after it lands (docs/CONTENT.md, convert-first).
- **Claims:** members of a claim may use its cistern, and a claim with a cistern is easy to see.
- **Animals:** sheep could drink from a trough (a later hook).
- **A future farming lane:** garden sinks.

## Core loop options (sent to the PM 2026-10-17 for Justin)
1. (Recommended) Stream to home cistern: a pipe run from a stream or pond into a cistern in your claim. Free water at home for filling pots and drinking; no tending. Slope and length limit the run; pipes and cistern are fired clay.
2. Rain cistern first: gutters from a tiled roof into a cistern. No terrain slope, simple graph, but only fills when it rains and needs a tiled roof first.
3. Both, in order: option 1 first, then roof gutters on the same pipe pieces.
4. Trough and drains only (no source-to-sink network): a clay trough you fill by hand. Smallest, but keeps the chore this feature is meant to remove.

## Open questions / blockers
- Waiting on Justin's pick of the core loop (via the PM).
- Does Environment have, or can it add, a shared `WaterAt` query and springs?
- Slip decoration is parked as ceramics-14.

## Next step
When the PM relays Justin's pick: ask Environment for the `WaterAt` query, wait for building-12 (so the cistern and pipe are single Def entries), then build `ClayForm.Pipe`, `clay.pipe` and the Water graph for the picked option. Also still to do: re-run `ClayCheck --kiln --jar` once House building's #362 (claim-only kiln and bench) is on main.

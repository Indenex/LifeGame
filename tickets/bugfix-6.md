# bugfix-6: Main CI check and smoothness pass
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_progress
Branch: claude/bugfix-lyle6o
PR: (not opened yet)

## Goal
Confirm main's CI is green after the rapid merges (fix it if not), then profile the client on a busy scene (storm rain, dense grass, a flock, several fires, 3+ bots), fix cheap frame hitches, and report numbers plus anything owned by other lanes to the PM.

## How to profile
`--frame-stats=path.csv` (client user arg) writes one line a second: fps, frame avg/p99/max ms, process ms, draw calls, objects, primitives, nodes, managed KB allocated per second, GC counts, hitches (frames over 2x the median and 20 ms). Busy scene recipe used: publish to a `perf` database, force a storm with `spacetimedb-cli sql ... perf "UPDATE weather SET cloud = 1.0, rain = 1.0, wind = 0.9, wetness = 0.8, target_cloud = 1.0, target_rain = 1.0, target_wind = 0.9"`, light fires with `tools/CraftCheck` (args: uri db fireX fireZ), run `tools/LoadTest 4 120 brotli http://127.0.0.1:3000 perf`, then the client with `LIFEGAME_DB=perf ... -- --profile=perf --frame-stats=out.csv`. dotnet-trace (`--profile dotnet-sampled-thread-time,gc-verbose`) attaches to the Godot process for allocation stacks.

## Progress log
- 2026-10-16 00:00: main run for #10 merge (58d50e0) failed: Kiln.cs defined CeramicsStructureRemoved twice. #40 (cb9d155) fixed it; main green again from #32 (04d505d).
- 2026-10-16 01:00: headless busy scene before: 1.9 MB/s managed garbage, gen2 GC hitches of 200-400 ms about every 45 s, 60-120 ms chunk-build frames. Causes: string literals converted to new StringNames every frame in Atmosphere, Rain, Main.Walk, UseKey, NeedsHud, KeyHelp, FireView, Look.Tick (each tracked by Godot's disposables table, which grows into the large-object heap); a new Vector2[] per audio push in SoundScape; ground mesh corners computed six times each through SurfaceTool interop.
- 2026-10-16 01:30: fixed all of the above (cached StringNames, one reused audio chunk, ground built from arrays with each corner computed once, chunk building time-budgeted at 4 ms a frame). Ground mesh verified identical vertex for vertex (positions, normals, colors, clearing channel, format) and 4x faster. After: 0.9 MB/s garbage, no GCs in a 90 s run, no hitch over 30 ms after startup.

## Open questions / blockers
- None.

## Next step
Rendered run for draw-call numbers, then open the PR and send the PM numbers and the other-lane list.

# atmosphere-12: Work sounds
Lane: World atmosphere and sound
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-12-work-sounds (stacked on claude/atmosphere-sound-hlqfke / #66)
PR: (after #66)

## Goal
Justin chose option C with a tweak (D-072): your own work and other players' work, others about 60% quieter. Every craft job makes a sound that fits it while it runs, synthesized like the rest of SoundMix: knapping is stone clicks, drills are a wooden friction whirr that turns to a hiss as the ember catches, twisting cord and weaving are soft fibre rustles, clay work is wet pats, scraping hide is a rasp, carving is wood shaves, lashing is cord creaks. A job ends with a small success thunk, or a dull snap when it failed. Sounds sit under the ambience like footsteps (StepGain). Other players' jobs come from a public craft_work table (rows only at job start and end). Done: a SoundPreview clip of each process family sent to the Render lane/PM, and a PR green after #66.

## Progress log
- 2026-10-16: started from 9430732 (#66 merged with main). Client listens to conn.Db.MyCraftJob insert/delete (process id, FinishAt, Failed); SoundMix gets a WorkVoice.

- 2026-10-16: pushed af3bd67. Render requested af3bd67 (SoundPreview work-scene clip). SoundMix.WorkVoice (hits for knap/flake/pat/rustle/creak/shave, continuous drill and rasp strokes, thunk/snap/ember endings) on the footstep bus, so StepGain keeps it just above the ambience. SoundMix.WorkFor maps process ids (fire.* Drill, knap.* Knap, cord/weave/gather Fibre, lash/haft/hide.stretch Lash, hide.scrape Rasp, clay.* Clay, carve.* Carve, cook.meat none). New atmosphere/WorkSounds.cs implements Crafting's existing CraftingUi.MinigameSound hook (so Strike, Drill, Shape and Coil minigames sound too) and listens to MyCraftJob insert/delete; Atmosphere.Connect wires it. SoundPreview work scenes: knap, drill, fibre, lash, pat, rasp, carve, and -fail. Levels (morning bed about -42 dBFS rms): peaks -20 to -25 dBFS, max 50 ms windows -32 to -36 dBFS, about the same as footsteps on that bed. Client and SoundPreview build locally.

- 2026-10-16 17:20: PM relayed D-072 (option C, others 60% quieter). A read-only view can't scan CraftJob (no Iter on read-only tables), so: public table CraftWork {PlayerId PK, ProcessId, Failed} in server/spacetimedb/WorkSounds.cs (my file), filled by partial hooks OnCraftStarted/OnCraftEnded, declared and implemented there. Crafting.cs must call them (asked Crafting session_01Y2sA7pDBFch57bWb1uKMTq; told Integration). Client: Atmosphere subscribes "SELECT * FROM craft_work"; WorkSounds plays each other player as its own WorkVoice at gain 0.4 x (1 - smoothstep(3, 30, distance)), panned by direction, re-placed every 0.1 s from PlayerPos. Pushed 3d06b46. tools/upgrade-check.sh origin/main passes locally (new table only). SoundPreview scene "others".

- 2026-10-16 17:30: Crafting took the declarations: crafting-12 (branch crafting-work-hooks, e0ee6b2) declares and calls OnCraftStarted/OnCraftEnded in Crafting.cs (the only CraftJob insert/delete paths; FinishCraft also runs for a player who disconnected mid-job). My WorkSounds.cs keeps the same two declarations until crafting-12 merges. Integration OK'd the table; per its note on stale rows, the client also drops another player's sound after 120 s whatever the table says. New voices for others start silent until placed.

## Open questions / blockers
- Before merging after crafting-12: delete the two `static partial void OnCraft...;` declaration lines in server/spacetimedb/WorkSounds.cs, or the build fails on a duplicate. If atmosphere-12 merges first, Crafting removes theirs instead.

## Next step
When #66 merges: merge main, open the atmosphere-12 PR, send the PM the green sha; then tell Crafting to add the two hook calls.

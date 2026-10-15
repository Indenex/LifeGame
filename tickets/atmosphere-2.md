# atmosphere-2: Sheep sounds, canopy fade, rain and dawn polish
Lane: World atmosphere and sound
Owner: World atmosphere and sound thread
Status: done
Branch: claude/atmosphere-sound-hlqfke
PR: (none yet)

## Goal
From the PM after atmosphere-1: soft sheep bleats, with a startled bleat when the flock bolts, driven by the Animals sheep state (graze, bolt, curious). Tree canopies between the camera and the player fade out so you can see yourself in the woods. From Justin's screenshots: raindrops right in front of the camera look like thick white sticks (make them thinner and fade them up close), and dawn is too brown and hazy. Done when a screenshot shows the canopy fade working and the sheep sounds play in a build.

## Progress log
- 2026-10-15 21:50: started from main a4ce058.

- 2026-10-15 22:05: Justin: rain too shiny and white streaks look awful, wants it closer to WoW Forever's new rain. Replaced streak particles with soft rain veils (three rings of scrolling curtains, depth-faded) and thicker rain fog and mist; wet ground now only slightly darker with barely any sheen. Canopy and trunk fade toward the player, cooler dawn, synthesized sheep bleat (not yet hooked to Animals). Justin: open PRs without waiting on renders.
- 2026-10-15 22:10: PR #16 open; client builds green. Screenshots /mnt/project-files/screenshots/atmosphere-07-rain-veils.png and atmosphere-08-woods-fade.png sent to Justin via the PM.
- 2026-10-15 22:20: SheepVoices (client/scripts/atmosphere/SheepVoices.cs) decides bleats: calm sheep every 15-45 s each, one startled bleat per flock when it bolts (State turns Run), quieter with distance up to 35 m, panned to the camera. Atmosphere.Sheep is ready; not yet fed from the table.

- 2026-10-15 22:45: Justin (D-036, via PM): smoother fade, rain intensity levels so it's not always hard to see, sound scaling with rain; immersion is top priority. Merge held. Reworked: trees in the way are now drawn by a transparent "ghost" MultiMesh (Look.TreeFoliage/TreeBark + Look.GhostOf, hook in Terrain.AddMulti) with a soft screen-space window around the player, no dither. Server rain fronts are now drizzle 38%, light 34%, steady 21%, storm 7%, building/clearing at 0.05 per 20 s tick and capped by cloud. Client: fog/mist mostly only thicken in storms; near veils only in heavy rain; lightning flashes with thunder in storms; rain audio layers patter/hiss/roar, branch drips, muffled rain and roof drumming under shelter (Main hook: atmosphere.Sheltered). F6 steps clear, drizzle, light, steady, storm.

- 2026-10-15 22:55: Ghost shader fixed (no `return` in vertex; `#define GHOST` after shader_type). Renders sent to PM: atmosphere-09-fade-before/after, atmosphere-10..13 rain drizzle/light/steady/storm, audio/atmosphere-02-rain-tiers.mp3. Merged main 5adb2c1 (bindings regenerated with tools/gen-bindings.sh); Animals is on main, so Atmosphere.Connect now feeds conn.Db.Animal to SheepVoices.

- 2026-10-15 23:00: Justin chose "Ship it"; PM merged PR #16.

## Open questions / blockers
- (none)

## Next step
None; done. Next ticket is atmosphere-3.

# atmosphere-2: Sheep sounds, canopy fade, rain and dawn polish
Lane: World atmosphere and sound
Owner: World atmosphere and sound thread
Status: in_review
Branch: claude/atmosphere-sound-hlqfke
PR: (none yet)

## Goal
From the PM after atmosphere-1: soft sheep bleats, with a startled bleat when the flock bolts, driven by the Animals sheep state (graze, bolt, curious). Tree canopies between the camera and the player fade out so you can see yourself in the woods. From Justin's screenshots: raindrops right in front of the camera look like thick white sticks (make them thinner and fade them up close), and dawn is too brown and hazy. Done when a screenshot shows the canopy fade working and the sheep sounds play in a build.

## Progress log
- 2026-10-15 21:50: started from main a4ce058.

- 2026-10-15 22:05: Justin: rain too shiny and white streaks look awful, wants it closer to WoW Forever's new rain. Replaced streak particles with soft rain veils (three rings of scrolling curtains, depth-faded) and thicker rain fog and mist; wet ground now only slightly darker with barely any sheen. Canopy and trunk fade toward the player, cooler dawn, synthesized sheep bleat (not yet hooked to Animals). Justin: open PRs without waiting on renders.
- 2026-10-15 22:10: PR #16 open; client builds green. Screenshots /mnt/project-files/screenshots/atmosphere-07-rain-veils.png and atmosphere-08-woods-fade.png sent to Justin via the PM.
- 2026-10-15 22:20: SheepVoices (client/scripts/atmosphere/SheepVoices.cs) decides bleats: calm sheep every 15-45 s each, one startled bleat per flock when it bolts (State turns Run), quieter with distance up to 35 m, panned to the camera. Atmosphere.Sheep is ready; not yet fed from the table.

## Open questions / blockers
- Sheep bleats need the `animal` table from Animals PR #13 (claude/animals-sheep-5gkntn) on main before they can be hooked.

## Next step
Once Justin has seen atmosphere-07 rain and CI is green, merge PR #16 (D-032). When Animals PR #13 is on main, merge main, regenerate bindings, and in Atmosphere.Connect feed conn.Db.Animal OnInsert/OnUpdate to Sheep.Seen(a.Id, a.X, a.Z, a.State == AnimalState.Run) and OnDelete to Sheep.Gone(a.Id).

# atmosphere-7: A living soundscape
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_review
Branch: claude/atmosphere-sound-hlqfke
PR: #58

## Goal
The ambience follows the world (D-036): a dawn bird chorus, a quiet midday, evening song thinning into dusk, night insects with an owl now and then; wind heard in leaves among trees and in grass in the open, scaling with the weather; footsteps that change with the surface (grass, bare earth, clay, a hut's floor); and indoors, everything outside (rain, wind, birds, animals, fire) is muffled. Everything crossfades; nothing pops.

## Progress log
- 2026-10-16 01:35: SoundMix gains TimeOfDay and Foliage. BirdActivity(t) gives a level (dawn 1, midday 0.35, evening 0.7, night 0) and a dawn-chorus amount that lets up to 7 songs overlap. Night: 6 crickets that join one by one as it darkens plus a faint 6-7 kHz insect shimmer. New FoliageVoice: fluttering leaf rustle (3.4-4.6 kHz) scaled by wind and how wooded it is, grass hiss (1.5-2.2 kHz) in the open; both follow WindVoice's gusts. Outdoor voices render to a bus that is low-passed and lowered under a roof, easing over ~1 s. Roof drumming softened so going inside always quietens the rain (roof -30 dBFS vs steady rain -25). Footsteps: Ground.Clay (heavier, a sucking pull-free, wetter when wet) and Ground.Floor (two board resonances and a click) appended to the enum. Atmosphere: Woodedness() from the terrain's forest noise; step surface from shelter, water, clay, cleared cells.
- Clips: audio/atmosphere-05-day-cycle.mp3 (night, dawn, midday, evening, night), atmosphere-06-wind-meadow-woods.mp3, atmosphere-07-footsteps-and-hut.mp3 (grass, earth, wet clay, steady rain, into a hut, back out).
- 2026-10-16: merged main (#54, #55), bindings regenerated, client builds clean; pushed 0c8a6a8 to claude/atmosphere-sound-hlqfke.

## Open questions / blockers
- (none)

## Next step
Watch the PR's CI; send the PM the clips in /mnt/project-files/audio/atmosphere-0{5,6,7}-*.mp3 and the green sha.

# audio-19: Simmering pots
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-19-simmer
PR: (see below)

## Goal
Pots of broth and stew simmering by fires (ceramics-7) were silent. Soft bubbling now comes from each pot.

## Progress log
- 2026-10-17: `CueDefs.BubbleSmall/BubbleBig/BubblePop` and `PotSounds` (client/scripts/atmosphere/PotSounds.cs), which reads the public `simmer_pot` table and ticks bubbles at random per pot, played where the pot sits (`Sfx.PlayAnimal`, heard out to 14 m). Broth is thin and busy (about 1.8 bubbles a second, mostly small bubbles and pops); stew is thick and slow (about 0.9 a second, mostly big gloops). A pot that is done goes on bubbling at half the rate. Levels kept ambient: loudest 50 ms about -35 dBFS over the bed. `pot:12` SoundPreview scene (broth then stew). Edges and fuzz soak clean; client builds.
- Next: kiln roar (kiln firing), roast sizzle, basket/jar lids.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha, then the kiln roar.

## Level fix (pending)
- The bubble cue gains were on the wrong scale (0.00x; handling cues run 0.03-0.4), so in game they are barely audible. Branch `claude/audio-23-potlevel` (stacked on this one) multiplies them by 4: loudest 50 ms -28.5 dBFS in the `pot:12` scene over a -33 median. Open it as a PR (or fast-forward it into this branch) once #631's CI has settled.

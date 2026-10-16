# audio-3: Feedback sounds for things that just happened
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_progress
Branch: claude/audio-3-cues (stacked on claude/audio-2a-streams)
PR: (after audio-2 lands)

## Goal
Small sounds that make the world answer the player (M1, and M2's "sees skill gains"): something going into your bag, eating, drinking, a fire catching, setting something down, a page turning for a new journal entry, and a gentle two-note chime when a skill goes up. Self-picked under D-145 while audio-2 waits on #173/#175. Done when each plays in the game from its lane's one-line hook, `Sfx.Play(Cue.X)`, and Justin has heard a clip.

## Progress log
- 2026-10-16 22:20: picked (D-145), told the PM. SoundMix.Cues: Pickup (rustle + bag knock), Eat (three crunchy bites), Drink (two gulps + slosh), FireLit (soft whoosh), Place (dull knock + scuff), Learn (page swish), SkillUp (D5 then A5 bell, a little reverb). SoundScape.Play, client/scripts/audio/Sfx.cs (`Sfx.Play(Cue)`). SoundPreview scene `cues`. Loudest 50 ms each -28.2 to -25.8 dBFS (about work-hit level, chime slightly above). Hook points found: InventoryBar.SetItems gained item (UX), SurvivalClient eat/drink (Survival), Journal.SkillChanged and Learned (Journal; journal/Chime.cs already chimes and should move to Audio per D-079), BuildingSystem.Place (Building), Fire.OnInsert near me (Crafting).

## Open questions / blockers
- Journal: replace journal/Chime.cs's Title()/Discovery() with Sfx.Play(Cue.SkillUp)/Sfx.Play(Cue.Learn)? Asked.
- Each lane adds its one-line hook once this merges.

## Next step
Commit and push; send hook requests to UX, Survival, Journal, Building and Crafting after the PR merges; Render clip of `cues`.

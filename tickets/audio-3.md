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
- 2026-10-16 22:35: ported journal/Chime.cs into Cues so the sound stays the same and goes through the Effects slider: SkillUp = its D5 then A5 marimba with two echoes (title change), Learn = its single faint G5 (new journal entry); about -26 and -32 dBFS loudest 50 ms, keeping Chime's 6 dB gap. Other cues -28 to -26.
- 2026-10-16 22:45: HANDOFF (D-159, lane moves to a Sonnet session). Added tools/SoundPreview/measure/ (standing.py, steps.py, windows.py + README) and an AUDIO.md section on how sounds are made and the level targets. Asked the PM for Justin: pickup sound on every item (A, default) / only new kinds (B) / none (C). Asked Journal (session_013QuyGdXAWemhkois2ictmc) to swap chime.Title()/Discovery() for Sfx.Play(Cue.SkillUp/Learn) and delete journal/Chime.cs after this merges.

## Open questions / blockers
- Justin (via PM): pickup sound A every item (in use) / B new kinds only / C none. Awaiting.
- Journal: agree to swap its Chime for Sfx cues after merge. Awaiting reply.
- Branch is stacked on claude/audio-2a-streams -> claude/audio-2b-footsteps -> claude/audio-2-land-seasons (#189). Open the audio-3 PR only after the audio-2 PRs merge (merge main in first).

## Next step
After audio-2's footsteps/streams PR merges: merge main into claude/audio-3-cues, build client and SoundPreview, open the audio-3 PR, send Render a `cues:20` SoundPreview clip request, then send each lane its one-line hook: UX/UI `Sfx.Play(Cue.Pickup)` in ui/InventoryBar.cs SetItems where `gained` is found (skip the first call at login); Survival `Cue.Eat` after conn.Reducers.Eat in survival/SurvivalClient.cs and `Cue.Drink` in the survival.drink UseOffer; Journal the chime swap; Building `Cue.Place` after PlaceStructure in building/BuildingSystem.cs Place(); Crafting `Cue.FireLit` on Fire.OnInsert within ~6 m of the local player (crafting/FireView.cs).

# audio-3: Feedback sounds for things that just happened
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_review
Branch: claude/audio-3-cues-main (cue commits cherry-picked onto main after #189 merged; the old stacked claude/audio-3-cues is superseded)
PR: #210

## Goal
Small sounds that make the world answer the player (M1, and M2's "sees skill gains"): something going into your bag, eating, drinking, a fire catching, setting something down, a page turning for a new journal entry, and a gentle two-note chime when a skill goes up. Self-picked under D-145 while audio-2 waits on #173/#175. Done when each plays in the game from its lane's one-line hook, `Sfx.Play(Cue.X)`, and Justin has heard a clip.

## Progress log
- 2026-10-16 22:20: picked (D-145), told the PM. SoundMix.Cues: Pickup (rustle + bag knock), Eat (three crunchy bites), Drink (two gulps + slosh), FireLit (soft whoosh), Place (dull knock + scuff), Learn (page swish), SkillUp (D5 then A5 bell, a little reverb). SoundScape.Play, client/scripts/audio/Sfx.cs (`Sfx.Play(Cue)`). SoundPreview scene `cues`. Loudest 50 ms each -28.2 to -25.8 dBFS (about work-hit level, chime slightly above). Hook points found: InventoryBar.SetItems gained item (UX), SurvivalClient eat/drink (Survival), Journal.SkillChanged and Learned (Journal; journal/Chime.cs already chimes and should move to Audio per D-079), BuildingSystem.Place (Building), Fire.OnInsert near me (Crafting).
- 2026-10-16 22:35: ported journal/Chime.cs into Cues so the sound stays the same and goes through the Effects slider: SkillUp = its D5 then A5 marimba with two echoes (title change), Learn = its single faint G5 (new journal entry); about -26 and -32 dBFS loudest 50 ms, keeping Chime's 6 dB gap. Other cues -28 to -26.
- 2026-10-16 22:45: HANDOFF (D-159, lane moves to a Sonnet session). Added tools/SoundPreview/measure/ (standing.py, steps.py, windows.py + README) and an AUDIO.md section on how sounds are made and the level targets. Asked the PM for Justin: pickup sound on every item (A, default) / only new kinds (B) / none (C). Asked Journal (session_013QuyGdXAWemhkois2ictmc) to swap chime.Title()/Discovery() for Sfx.Play(Cue.SkillUp/Learn) and delete journal/Chime.cs after this merges.
- 2026-10-16 22:55: Journal agreed (written in its tickets/skills-11.md): once audio-3 is on main it swaps chime.Title()/Discovery() for Sfx.Play(Cue.SkillUp/Learn) and deletes Chime.cs. Send the "audio-3 merged" note to the PM, who passes it to the new Journal session.
- 2026-10-16 23:05: Justin chose A (via PM): a soft rustle and knock on every pickup. #189 merged, so the cues no longer need audio-2a/2b: cherry-picked onto main as claude/audio-3-cues-main (conflicts only at the StreamVoice lines, dropped). Client and SoundPreview build; cues still -28 to -26 dBFS loudest 50 ms; storm peak -6.8.

- 2026-10-16 (Sonnet takeover): audio-2a+2b PR is #216 (branch claude/audio-2a-streams). Merged main and claude/audio-2a-streams into claude/audio-3-cues-main (resolved SoundMix/Program.cs additively); SoundPreview builds, `cues:20` renders (rms -35.2, peak -14.4). Justin chose pickup sound on every item (A).

## Open questions / blockers
- Answered: pickup sound on every item (A).
- Journal: agreed (skills-11); tell the PM when audio-3 merges.

## Next step
When #216 merges: merge main into claude/audio-3-cues-main, build SoundPreview, open the audio-3 PR (base main), request a Render `cues:20` clip, then send each lane its one-line hook (see git history of this ticket for the list: UX InventoryBar.SetItems Pickup; Survival Eat/Drink; Journal chime swap; Building Place; Crafting FireLit) and tell the PM audio-3 merged.

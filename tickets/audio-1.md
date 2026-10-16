# audio-1: Take over audio; finish work sounds
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_review
Branch: claude/audio-1-work-sounds (continues claude/atmosphere-12-work-sounds 9965c81, merged with main 1ade969; stacked on #66)
PR: #120

## Goal
Per D-079 the Audio lane owns all sound and music. Done means: (1) the Atmosphere lane's in-progress work sounds (atmosphere-12, `claude/atmosphere-12-work-sounds`, stacked on #66) are taken over and finished: each craft sounds like itself, plus other players' work at about 60% quieter (D-072), which needs a small public "who is doing what" signal agreed with Crafting and Integration; (2) a short `client/scripts/atmosphere/audio/AUDIO.md` describing how other lanes request sounds and the mix levels; (3) a balanced overall mix (ambience, fire, animals, work, music). The first render (audio clip) of the work sounds was judged quiet: Justin listens with volume up. Justin hears clips via the Render lane before merges.

## Progress log
- 2026-10-16: ticket written by the PM (D-079).
- 2026-10-16 17:30: Audio session started. Took atmosphere-12 at 9965c81 (its ticket state carries over: CraftWork table, WorkVoice, others at 0.4 gain; crafting-12 declares the OnCraftStarted/OnCraftEnded hooks). Branched claude/audio-1-work-sounds, merged main 1ade969. Asked Atmosphere to stop committing to atmosphere-12 and confirm its final sha.
- 2026-10-16 17:40: Atmosphere handed over: final atmosphere-12 sha 68392c7 (merged here). Render request for af3bd67 sent to Render ~17:10, no reply yet. PR must stack after #66 and depends on #97 (crafting-12): when merging main after #97, delete the two OnCraftStarted/OnCraftEnded declaration lines in server/spacetimedb/WorkSounds.cs in that merge commit (else CS0756). Integration wants the PR number. Not yet done: in-game listen; per-strike sounds on minigame keys (MinigameSound only gives open/close). Atmosphere keeps #66 merged with main; #66 audio (WaterVoice, Frogs, StepGain) is now Audio's.

- 2026-10-16 18:00: levels. Measured (SoundPreview, no music): calm bed -41.5 rms; work rose only 0 to 5 dB over it, music about 11 dB above work, so work was nearly lost. Changes: work gets its own bus, WorkGain = 0.55 + 5 x ambience (was StepGain, 0.2 on a calm bed), per-kind trims (drill friction down 3.5 dB, pat 0.6, creak 2, lash rustle 1.3, shave 1.25), master Volume 1.3 -> 1.7 (+2.3 dB). Now: bed -39.7, loudest 50 ms of each work -30.4 to -27.6 dBFS, storm peak -4.9 dBFS. New SoundPreview scene `still`. Wrote audio/AUDIO.md (requests + mix table). Client and SoundPreview build locally.
- 2026-10-16 18:05: Render requested b7fde7a (SoundPreview still, each craft, knap-fail, others, midday with music). Asked PM for Justin: own work vs music, A level with music (default, in use) / B under / C over.
- 2026-10-16 18:10: Render delivered the old af3bd67 clip to the PM (renders/audio/atmosphere-12-af3bd67-work-sounds.mp3 on claude/render-1-013qe8, -38 to -40 dBFS rms). Told the PM it is superseded by the b7fde7a clip.
- 2026-10-16 18:15: Render delivered b7fde7a clip to the PM: renders/audio/audio-1-b7fde7a-work-sounds.mp3 on claude/render-1-013qe8 (87 s, peak -9.7 dBFS; crafts -25 to -28 rms, knap -30.9). Knap's lower rms is intended: sparse clicks every 0.7-1.3 s; its loudest 50 ms (-28.8) matches the others.
- 2026-10-16 19:05: #66 merged; merged main 4dc416c. Merged #97's branch (crafting-work-hooks 927e22a) and dropped the duplicate OnCraft declarations in WorkSounds.cs, so this builds in either merge order. Server, client, SoundPreview build; tools/upgrade-check.sh origin/main passes. Opened PR #120; waiting for CI.
- 2026-10-16 19:20: PM relayed D-097: own work a LITTLE UNDER the music (option B); four sound sliders (master, music, effects/work, ambience) on an Options page in the Esc menu, all 50% for a new player; UX/UI builds the page; green sha goes to Integration (D-095). Done here: WorkGain 0.39 + 3.5 x ambience, max 0.7 (about -3 dB); SoundMix MasterLevel/MusicLevel/EffectsLevel/AmbienceLevel with SliderGain (2s)^2 (0.5 = designed mix), eased; ambience bus scaled after the ambience meter; SoundScape MasterVolume/MusicVolume/EffectsVolume/AmbienceVolume saved under [volume] (old [music] volume dropped so everyone starts at 0.5); SoundPreview --master/--music/--effects/--ambience. Measured: music over calm bed -31 rms, work rms -32 to -39, loudest 50 ms -29 to -32. AUDIO.md has the bus table.

## Open questions / blockers
- Answered D-097: B, a little under the music.
- UX/UI builds the Options page sliders against SoundScape's four properties; agreed with session_01V6qgbNbvTwniqW946KACLr.
- (#66 merged; #97 folded in, either merge order works.)

## Next step
Wait for CI run https://github.com/Indenex/LifeGame/actions/runs/38944796924 on PR #120 (head c6d0187); when green, send the head sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU) for merge.
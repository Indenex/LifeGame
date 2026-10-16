# audio-1: Take over audio; finish work sounds
Lane: Audio
Owner: Audio session (session_01LeXCWTFVBQcCNVEDdhPek3, Opus)
Status: in_progress
Branch: claude/audio-1-work-sounds (continues claude/atmosphere-12-work-sounds 9965c81, merged with main 1ade969; stacked on #66)
PR: (once opened)

## Goal
Per D-079 the Audio lane owns all sound and music. Done means: (1) the Atmosphere lane's in-progress work sounds (atmosphere-12, `claude/atmosphere-12-work-sounds`, stacked on #66) are taken over and finished: each craft sounds like itself, plus other players' work at about 60% quieter (D-072), which needs a small public "who is doing what" signal agreed with Crafting and Integration; (2) a short `client/scripts/atmosphere/audio/AUDIO.md` describing how other lanes request sounds and the mix levels; (3) a balanced overall mix (ambience, fire, animals, work, music). The first render (audio clip) of the work sounds was judged quiet: Justin listens with volume up. Justin hears clips via the Render lane before merges.

## Progress log
- 2026-10-16: ticket written by the PM (D-079).
- 2026-10-16 17:30: Audio session started. Took atmosphere-12 at 9965c81 (its ticket state carries over: CraftWork table, WorkVoice, others at 0.4 gain; crafting-12 declares the OnCraftStarted/OnCraftEnded hooks). Branched claude/audio-1-work-sounds, merged main 1ade969. Asked Atmosphere to stop committing to atmosphere-12 and confirm its final sha.
- 2026-10-16 17:40: Atmosphere handed over: final atmosphere-12 sha 68392c7 (merged here). Render request for af3bd67 sent to Render ~17:10, no reply yet. PR must stack after #66 and depends on #97 (crafting-12): when merging main after #97, delete the two OnCraftStarted/OnCraftEnded declaration lines in server/spacetimedb/WorkSounds.cs in that merge commit (else CS0756). Integration wants the PR number. Not yet done: in-game listen; per-strike sounds on minigame keys (MinigameSound only gives open/close). Atmosphere keeps #66 merged with main; #66 audio (WaterVoice, Frogs, StepGain) is now Audio's.

- 2026-10-16 18:00: levels. Measured (SoundPreview, no music): calm bed -41.5 rms; work rose only 0 to 5 dB over it, music about 11 dB above work, so work was nearly lost. Changes: work gets its own bus, WorkGain = 0.55 + 5 x ambience (was StepGain, 0.2 on a calm bed), per-kind trims (drill friction down 3.5 dB, pat 0.6, creak 2, lash rustle 1.3, shave 1.25), master Volume 1.3 -> 1.7 (+2.3 dB). Now: bed -39.7, loudest 50 ms of each work -30.4 to -27.6 dBFS, storm peak -4.9 dBFS. New SoundPreview scene `still`. Wrote audio/AUDIO.md (requests + mix table). Client and SoundPreview build locally.
- 2026-10-16 18:05: Render requested b7fde7a (SoundPreview still, each craft, knap-fail, others, midday with music). Asked PM for Justin: own work vs music, A level with music (default, in use) / B under / C over.
- 2026-10-16 18:10: Render delivered the old af3bd67 clip to the PM (renders/audio/atmosphere-12-af3bd67-work-sounds.mp3 on claude/render-1-013qe8, -38 to -40 dBFS rms). Told the PM it is superseded by the b7fde7a clip.

## Open questions / blockers
- Justin: own work level vs music, A level (default) / B under / C over. Sent to PM 18:05.
- Depends on #66 (stacked) and #97 (crafting-12 hook declarations); see the 17:40 log line for the merge step.

## Next step
Wait for Render to send the work-sound clip (requested at the pushed sha) to the PM; then when #66 and #97 merge, merge main (drop the duplicate hook declarations) and open the PR.

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

## Open questions / blockers
- (none)

## Next step
Install dotnet 8 + SpacetimeDB 2.12.0, build server and client, then raise the work-sound levels (Justin found the first clip quiet) and re-render via SoundPreview.

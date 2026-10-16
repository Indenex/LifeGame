# skills-8: Quiet mastery moments
Lane: Journal and skills
Owner: Journal and skills thread
Status: done
Branch: claude/journal-skills-iseewg
PR: #37 (merged 2026-10-16 00:10 as 7de231e)

## Goal
Progress feels good without being gamey. Before, every single rank showed a toast ("Earthwork 5"). Now:
- A toast only when a skill earns a new title (every 20 ranks: Apprentice, Journeyman, Expert, Artisan, Master): "You have become an apprentice in earthwork", with a soft two-note wooden chime (an open fifth with a short airy echo).
- A fainter single note when something new goes in the journal (the existing toast stays).
- In the journal, a small dot beside skills whose rank rose and discoveries added since you last closed it.
Preview of both sounds: /mnt/project-files/audio/journal-chime.wav (title chime, a second of quiet, then the discovery note).

## How it fits together
- `client/scripts/journal/Chime.cs`: synthesizes both sounds once at startup into AudioStreamWav (no audio files, Atmosphere's SoundMix untouched); -16 dB and -22 dB.
- `Journal.cs`: SkillChanged marks the skill and only speaks and chimes on a title change; Learned marks the discovery and plays the faint note. Toggling the journal redraws so marks clear on close.
- `JournalPanel.cs`: `Fresh` set, drawn as an accent dot before the name.

## Progress log
- 2026-10-15 23:45: built; client compiles; chime rendered offline to the preview WAV (peak about -8 dBFS in the file, played at -16/-22 dB in game).

- 2026-10-15 23:44: PR #37 opened at cc4c7ed.
- 2026-10-15 23:56: merged main (#36 journal layer, #10 building). Main's server build broke on Kiln.cs `CeramicsStructureRemoved` (#10 made it Building's partial); ported #40's one-word fix (`static partial void`) so this PR builds; it no-ops once #40 lands. Also added Building's ids from skills-7: `build.weave` ("Weaving a storage basket") and `build.door` ("Hanging a door"); no lock (a toggle, no skill).
- 2026-10-16 00:00: merged main again after #40 (cb9d155); took main's Kiln.cs, so the port is gone. Server and client build.
- 2026-10-16 00:09: CI green on 81bbbc7 (contains main cb9d155): https://github.com/Indenex/LifeGame/actions/runs/38826954975
- 2026-10-16 00:10: merged by the PM.

## Open questions / blockers
- Justin may prefer the old per-rank toasts; restoring them is one line in `Journal.SkillChanged`.

## Next step
None; done.

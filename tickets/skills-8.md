# skills-8: Quiet mastery moments
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_review
Branch: claude/journal-skills-iseewg
PR: (opening)

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

## Open questions / blockers
- Justin may prefer the old per-rank toasts; restoring them is one line in `Journal.SkillChanged`.

## Next step
Wait for CI on the skills-8 PR, send the green sha and the chime preview to the PM.

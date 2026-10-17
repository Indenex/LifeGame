# audio-8: Cute, relaxing music (Dofus mood)
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_progress
Branch: claude/audio-8-music
PR: (after Justin picks a style)

## Goal
Justin (via the PM): "music similar to Dofus, a basic theme similar to Grynpyret's Kites, and a song about naps; very cutesy and relaxing, less Valheim-y." Mood reference only: original melodies, nothing copied. Music goes through the synthesized engine as data (pieces and styles are entries), keeping the music slider and on/off. D-154: prototype first, Justin picks a style, then the game's music is converted.

## Progress log
- 2026-10-17: style options A (Sunny picnic: harp, recorder, bells, light percussion, lilting 6/8), B (Music-box storybook), C (Light chiptune-folk) sent to the PM; Justin wants tasters of all three: main theme + nap song in A (30 to 60 s) and 15 s tasters of B and C playing the same melody.
- 2026-10-17: built `client/scripts/atmosphere/audio/Score.cs` (Track + Arrangement data, additive instruments: pluck, recorder flute, bell, music box, pad, pulse/square, triangle, shaker, woodblock, hat; small Schroeder reverb) and `Pieces.cs` (MainTheme in G major 6/8, NapSong in D major 6/8, four arrangements). `dotnet run -c Release -- --pieces=outdir` in tools/SoundPreview writes the prototypes as WAV (convert with ffmpeg). Checked the first note's pitch by FFT (D5 as written). Prototypes published for the PM as an audio page: https://claude.ai/artifact/Jp2midnVmqzgRSddHp3nL8 (private; share from the Share menu).
- Not in the game yet: Score is not connected to SoundMix/SoundScape (prototype only).

## Open questions / blockers
- Justin's pick of style (A, B or C) and feedback on the tunes.

## Next step
When Justin picks: refine that style, then stream Score through SoundMix (replace the old Music class, keep MusicOn and the slider, pick pieces by time of day / sheltered / resting), as a PR. Placement default: main theme and lighter pieces by day, the nap song at dusk, night and while resting.

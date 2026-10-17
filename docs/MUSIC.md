# Adding music

The game plays recorded music (made with Suno or anywhere else) from `client/assets/music/`.
The synthesized sounds (wind, rain, footsteps and so on) are separate and unchanged.

## Add a track (two steps)

1. Put the file in `client/assets/music/` as **.ogg** (Ogg Vorbis). Convert an mp3 or wav with
   `ffmpeg -i song.mp3 -c:a libvorbis -q:a 5 song.ogg`. Keep the name simple: `kites.ogg`.
   The first time Godot opens the project it makes a `.import` file next to it; commit that too.
2. Add one line to `MusicDefs.All` in `client/scripts/atmosphere/audio/MusicDirector.cs`:

   ```csharp
   new("kites", "res://assets/music/kites.ogg", "day"),
   new("naps",  "res://assets/music/naps.ogg",  "dusk night rest", -2f),
   ```

   The parts are: an id (any short name, unique), the file, the moods it suits, and
   optionally a volume trim in decibels (`-2f` is a little quieter; use it to even out tracks
   that were mastered louder or softer).

## Moods

List any of these, separated by spaces:

| Mood | When it is wanted |
|---|---|
| `day` | daytime, outdoors, dry |
| `dusk` | around sunset |
| `night` | after dusk, before dawn |
| `rest` | indoors under a roof, or by a fire in the evening or at night |
| `rain` | outdoors in rain (indoors counts as `rest`) |

A track can have several. If nothing is tagged for the exact mood, it falls back: `rest` and
`rain` fall back to the time of day, `dusk` to `night`, `night` to `dusk`. Day has no fallback,
so if there is no `day` track there is no music in the daytime. Tag a quiet track `rest` and the
nap-like pieces `dusk night rest`.

## How it behaves

- One track plays at a time, then a **silence of 75 to 200 seconds** (never under a minute),
  so music is a visitor, not wallpaper. The first one starts 15 to 45 seconds after you arrive.
- When the mood changes and the playing track does not suit the new one, it **cross-fades**
  over 4 seconds to a track that does (or fades out if none does). A mood must last 6 seconds
  before music reacts, so stepping in and out of a doorway does not restart it.
- The same track is not played twice in a row when there is a choice.
- The **Music slider and on/off** in the Options page control it (0.5 is the track as
  recorded, 1 is up to 6 dB louder). Turning music off fades it out.
- Missing or broken files are skipped with a message in the log; the game carries on.

## Checking it

`cd tools/SoundPreview && dotnet run -c Release -- --music-test` runs the rules (gaps, fades,
moods, no repeats, off means silent) against fake tracks. It does not need audio files.
If a new track sounds too loud or quiet next to the others, set its trim (a few decibels) rather than editing the file.

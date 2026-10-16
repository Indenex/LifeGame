# Audio

The Audio lane owns every sound and all music (D-079). Other lanes never add sounds themselves; they ask.

## How to ask for a sound

Send the Audio lane (see `STATUS.md` for its session) a message with:

1. **What happens**: the event and the ticket, e.g. "sheep is shorn, animals-2".
2. **The hook**: where in your code it fires (a method, a table row insert/delete, a minigame callback). If no hook exists yet, say so; Audio proposes one and you add the one-line call.
3. **Who hears it**: only the player doing it, or everyone nearby (needs a public table or row the client can see).
4. **A feel in a few words**: "dull wooden knock", "wet slap".

Audio synthesizes it in `SoundMix.cs` (no sample files), adds a SoundPreview scene, and sends a clip to Justin through the Render lane and the PM before merging.

## Where things live

- `client/scripts/atmosphere/audio/SoundMix.cs`: the whole mix, plain C#, no engine types.
- `client/scripts/atmosphere/SoundScape.cs`: feeds the mix into Godot; saves the music slider.
- `client/scripts/atmosphere/WorkSounds.cs`: craft jobs (yours from `MyCraftJob`, others' from `craft_work`).
- `server/spacetimedb/WorkSounds.cs`: the public `craft_work` table.
- `tools/SoundPreview/`: renders scenes to a WAV without the game: `dotnet run -c Release -- out.wav still:8 knap:8 --no-music`.

## Mix levels (SoundPreview, master Volume 1.7)

| Bus | Gain | Measured |
|---|---|---|
| Calm morning bed (`still`) | ambience | about -40 dBFS rms |
| Footsteps | `StepGain`: 0.12 + 9 x ambience rms, to 1 | about 5 dB over the bed |
| Your work | `WorkGain`: 0.55 + 5 x ambience rms, to 1 | loudest 50 ms -30 to -27 dBFS, peaks -19 to -13 |
| Other players' work | 0.4 x (1 - smoothstep(3, 30 m)) on the work bus (D-072) | about 8 dB under yours at 3 m |
| Music | `MusicVolume` slider | calm scenes about -27 dBFS rms with music |
| Storm | everything | about -19 dBFS rms, peak about -5 dBFS (soft clip above 0) |

Keep new sounds inside these: a one-off event lands about where work hits do; nothing but thunder should peak above -6 dBFS.

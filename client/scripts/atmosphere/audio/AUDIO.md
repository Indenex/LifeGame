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

## Sliders and buses (D-097)

Four sliders on the Options page (Esc menu), each 0 to 1. UX/UI's `client/scripts/ui/SoundLevels.cs` owns and saves them (0.5 for a new player, the mix as designed) and writes them into `SoundScape`. A slider maps to gain with `SoundMix.SliderGain`: (2 x level)^2, so 0 is silent, 0.5 is 1, and 1 is 4 (+12 dB).

| Slider | Where it lands | What it scales |
|---|---|---|
| Master | Godot's Master bus (UX/UI), same curve | everything |
| Music | `MusicVolume` | the score (the Music on/off toggle stays) |
| Effects | `EffectsVolume` | your footsteps, your work, other players' work |
| Ambience | `AmbienceVolume` | wind, leaves, rain, thunder, birds, night insects, sheep, fire, water, frogs |

Footsteps and work follow how loud the world is, measured before the ambience slider, so turning ambience down does not also quiet your work.

New sounds go on one of these buses; say which in your request if it isn't obvious.

## Mix levels (SoundPreview, all sliders at 0.5)

| Bus | Gain | Measured |
|---|---|---|
| Calm morning bed (`still`) | ambience | about -40 dBFS rms |
| Music over that bed | music | about -31 dBFS rms |
| Footsteps | `StepGain`: 0.12 + 9 x ambience rms, to 1 | about 5 dB over the bed |
| Your work | `WorkGain`: 0.39 + 3.5 x ambience rms, to 0.7 | a little under the music (D-097): rms -32 to -39, loudest 50 ms -29 to -32 dBFS |
| Other players' work | 0.4 x (1 - smoothstep(3, 30 m)) on the work bus (D-072) | about 8 dB under yours at 3 m |
| Storm | everything | about -19 dBFS rms, peak about -5 dBFS (soft clip above 0) |

Keep new sounds inside these: a one-off event lands about where work hits do; nothing but thunder should peak above -6 dBFS.

SoundPreview takes the sliders too: `--master=0.7 --music=0.3 --effects=0.5 --ambience=0`.

## Seasons (audio-2)

`SoundMix.Year` (0 to 1, as `Seasons.YearFraction`) and `SoundMix.SnowCover` drive `SeasonSound.Of(year)`:

| | Spring | Summer | Autumn | Winter |
|---|---|---|---|---|
| Birds | busiest, stronger dawn chorus | normal | fewer | a rare few |
| Leaves | budding in | full | drier, crisper rattle | bare: thin twig whistle |
| Insects | some crickets at night | day drone, a bee now and then, full crickets | crickets thinning | none (owls stay) |
| Frogs | busiest | yes | fading | none |
| Wind | | | | a little stronger and higher; grass hushed under snow |

SoundPreview: `spring`, `summer`, `autumn`, `winter` (and `...night`). Other scenes stay at early summer.

## How sounds are made here (for whoever holds the Audio lane next)

- Everything is synthesized in `SoundMix.cs`, sample by sample, from noise (`Noise`), one-pole lowpasses (`Lowpass`) and state-variable band-passes (`BandPass.Process(x, cutoffHz, damping, rate)`: the third argument is damping = 1/Q; small values ring and add gain, values above about 2 go unstable). No sample files.
- A new sound is a small voice class (see `Cues`, `StreamVoice`, `DayInsects`) rendered into the right buffer: `outL/outR` for the world (ambience bus, muffled under a roof), `workL/workR` for effects (footsteps use `stepL/stepR`), plus `sendL/sendR` for the forest reverb. One-shots come in through a `SoundScape` method that queues onto the synth thread; other lanes call `Sfx.Play(Cue.X)`.
- Every new sound gets a SoundPreview scene, then is measured with `tools/SoundPreview/measure/` and placed inside the level table above before it goes to Render. Justin listens with the volume up; quiet is a bug.
- Typical targets (sliders at 0.5, no music): calm bed about -40 dBFS rms; footsteps and one-shot cues reach about -33 to -26 at their loudest 50 ms; seasonal and water ambience beds -37 to -29 rms standing; storm peak about -5. Nothing but thunder above -6 dBFS peak.
- Toolchain: .NET 8 SDK and SpacetimeDB CLI 2.12.0 (see `.github/workflows/build.yml`). Build `client/` after `tools/gen-bindings.sh`; run SoundPreview with `dotnet run -c Release -- out.wav scene:seconds ...` from `tools/SoundPreview/`.


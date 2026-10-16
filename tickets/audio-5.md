# audio-5: Close thunder crack and a heavy rain-on-roof swell
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_review
Branch: claude/audio-5-storm
PR: (see below)

## Goal
From the PM for Atmosphere's storms (#286): a sharper "crack" for close strikes (`near` > 0.5; far strikes keep the distance-delayed rumble), and a rain-on-roof swell under a roof in heavy rain. Sounds only; no other lane changes.

## Progress log
- 2026-10-16: ThunderVoice: for near above 0.5 (smoothstep 0.5 to 0.9) adds a bright snap (first ~25 ms) and a ripping tear that dies in ~0.5 s over the existing rumble. RainVoice: under cover in heavy rain (level above 0.45) a 150 to 750 Hz noise drumming that swells and falls with slow gusts. SoundPreview scenes `crack` (close, mid, far strike in a storm), `stormroof` (heavy rain under a roof). Levels: roof (steady rain) -31.0, stormroof -25.8, open storm -20.1 dBFS rms, so indoors is always quieter; close crack peak -4.3, far thunder -5.7 dBFS (thunder may peak, per AUDIO.md).
- 2026-10-16: Justin (via PM): the rain sounded like a weird bouncy animal; wants plain rain. Removed everything pitched or periodic from RainVoice: the roof swell's slow sine pulsing, and the drops (they were sine tones that fell in pitch, a boing). Drops are now short unpitched noise bursts at random times, filtered by kind (leaves bright above 2 kHz, drips 400 to 1500 Hz, roof dull below 700 Hz). Heavy rain on a roof adds a steady low noise bed, no pulse. Thunder crack kept. Levels (dBFS rms, main before to now): drizzle -34.5 to -33.2, light -29.5 to -30.6, steady -24.4 to -23.9, storm -20.4 to -20.9, roof -31.5 to -33.6 (a little quieter), heavy roof -28.0.
- Still open (separate): wire deer/hit/crit sounds once #219 is on main (tickets/audio-4.md Next step).

- 2026-10-16: Integration relayed Justin's content-as-data rule (docs/CONTENT.md). Sound cue row is Audio's: today each cue is a code block in SoundMix (Cues.Render switch). Plan in Next step / open questions.

## Open questions / blockers
- Cue recipes as data (docs/CONTENT.md Sound cue row): proposed to the PM as audio-6, converting cues one at a time (see below). Awaiting the PM's pick.

## Next step
Wait for CI on the audio-5 PR; send the PM the green sha and Render a clip request (`crack:24`, `stormroof:24`, `roof:24`).

## Later (audio-6, content as data)
A `CueDef` (id, layers of: sine or noise source, pitch curve, band/low-pass cutoffs, attack/decay envelope, start offset, gain, send) in a data list read by one generic voice in `Cues.Render`; convert Pickup, Place, Hit, Crit, Jump first (simple bursts), then Eat, Drink, FireLit; keep Marimba notes (SkillUp, Learn) as a layer type. Same sounds, measured with SoundPreview `cues:26` before and after (loudest 50 ms within 1 dB). Do it when the next new cue is added so it goes in as data.

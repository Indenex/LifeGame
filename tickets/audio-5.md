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
- Still open (separate): wire deer/hit/crit sounds once #219 is on main (tickets/audio-4.md Next step).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the audio-5 PR; send the PM the green sha and Render a clip request (`crack:24`, `stormroof:24`, `roof:24`).

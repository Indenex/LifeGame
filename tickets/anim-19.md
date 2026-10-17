# anim-19: More emotes (clap, shrug, point)
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_review
Branch: claude/anim-19 (from main)
PR: #705

## Goal
Three more emotes on the emote wheel (UX's, hold Q). All are upper body only, so they play while walking, and everyone nearby sees them:
- Clap: hands meet in front of the chest five times.
- Shrug: shoulders up, palms out, head tipped.
- Point: right arm straight out ahead, head following.

## Design
- WorkMotion gets Clap (28), Shrug (29) and Point (30) appended.
- Each is an EmoteDef entry in shared/Emotes.cs, so UX's wheel lists it automatically, with a star doodle until UX draws one.
- Poses, lengths and ActUsesLegs are in client WorkMotions.
- Server: the existing Emote reducer accepts any Emotes.IsEmote, so nothing new is needed there.
- Audio (#697, EmoteSounds) may want sounds: clap claps at 3.2 per second; shrug and point are silent.

## Progress log
- 2026-10-17 10:25: built; server and client build.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha.

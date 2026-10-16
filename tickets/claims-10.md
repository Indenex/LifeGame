# claims-10: Whisper and shout
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-10-voice (stacked on claude/claims-chat-pvp-vk2qak while PR #174 is open)
PR: (not yet)

## Goal
Chat gets two more voices (PM, D-145; the runner-up Justin saw for claims-9).
- "/w psst" is a whisper: heard only within 4 m. It shows a small, faint bubble, and the log reads "Mira whispers: psst".
- "/s over here!" is a shout: heard within 60 m. It shows a big bubble, and the log reads "Mira shouts: over here!".
- Normal speech stays at 24 m.
- A far line dims relative to its own voice's range.
- "/W" and "/S" work the same; a bare "/w" or "/s" says nothing.
- The chat hint and KEYMAP list the commands.

## Progress log
- 2026-10-16 19:40: built.
  - Shared: `Social.Voice`, `VoiceOf`, `VoiceBody`, `RangeOf`, `WhisperRange` 4, `ShoutRange` 60, and `ChatDim(distance, range)`.
  - Server: SendChat normalises the prefix.
  - Client: OnChat range and log label per voice; bubble size 40/30/24; whisper bubbles at 70% alpha; the chat subscription now listens 4 chunks (64 m) out instead of 2.
  - KEYMAP rows for /me, /w and /s.
  - SocialTest: 3 new checks.
  - Local build passes; all social checks pass.

## Decisions / defaults
- Whispers are filtered by the client, like all chat. The rows still reach clients subscribed to nearby chunks, so a modified client could read them. A truly private whisper would need a per-player view; that is fine for now.
- Shouts share the normal chat cooldown.

## Open questions / blockers
- (none)

## Next step
When #174 merges, merge main in, open the claims-10 PR and send a render request (a shout bubble next to a normal one; a whisper).

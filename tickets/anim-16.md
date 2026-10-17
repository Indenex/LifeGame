# anim-16: Emotes (wave, cheer, dance, bow)
Lane: Animation
Owner: Animation lane (session_019roNRWQBPw4ozNLDhzseig)
Status: in_progress
Branch: claude/anim-16 (from main)
PR: (not yet)

## Goal
Players can wave, cheer, dance or bow on request, and everyone nearby sees it.
- Wave: right arm up, forearm wagging. Upper body only, so it works while walking.
- Cheer: two little hops with both arms flung up.
- Dance: a side-to-side shuffle with knees bobbing.
- Bow: a bow from the hips with a hand to the chest.
UX adds the key or wheel that calls the reducer. Until then, Render C can call `Emote(n)` directly.

## Design
- `shared/WorkMotion.cs`: Wave, Cheer, Dance and Bow appended. `shared/Emotes.cs`: EmoteDef list (data, D-206), IsEmote, CooldownMicros = 1.5 s.
- `server/spacetimedb/Emotes.cs`: reducer `Emote(byte motion)` checks the motion is an emote and the cooldown (private EmoteLast table), then calls NoteAct. Server-validated (D-182).
- Client `WorkMotions`: IsAct covers emotes; ActSeconds, ActUsesLegs (Wave is upper body only) and the four poses. AvatarWork's existing PlayerAct handler plays them.
- Motion numbers: Wave 24, Cheer 25, Dance 26, Bow 27.

## Progress log
- 2026-10-17 08:10: built; server and client build.

## Open questions / blockers
- Waiting on the PM/Justin's pick of the next Animation item (emotes recommended).
- UX: an emote wheel or keys calling `conn.Reducers.Emote((byte)WorkMotion.X)`.

## Next step
Push, open the PR, ask UX for the wheel, and request a Render C clip.

# survival-24: Several characters per login
Lane: Survival (server side); UX builds the menu
Owner: Survival thread
Status: in_review
Branch: claude/survival-characters
PR: (see STATUS)

## Goal
Justin: "a character menu when logging in, and a way to delete/create new characters." One login can hold up to `Characters.MaxPerLogin` (3, raisable) characters, each with its own position, bag, skills, needs, claims, mats and town seat. No world wipe; every existing player becomes character #1 of their login.

## Design (approved by the PM)
- A character's **key** is an Identity value. Character #1 of a login is keyed by the login's own Identity, so every existing row is already right (no migration). Further characters get a random key starting `CC01` that is no real login.
- `Me(ctx)` (server/spacetimedb/Characters.cs) is the key of the caller's active character and replaces `ctx.Sender` everywhere (170 uses). `tools/check-ctx-sender.sh` fails if raw `ctx.Sender` shows up outside Characters.cs and Reducers.cs.
- Tables: private `Character{Key, Login, PlayerId, Created}`, private `ActiveCharacter{Login, Key}`. Views: `my_characters`, `my_active_character`, `my_player`, `my_player_pos`.
- Reducers: `CreateCharacter(name, skin, hair, cut, beard)` (steps into the world as the new one), `SelectCharacter(key)`, `ReturnToMenu()`, `DeleteCharacter(key, typedName)`. Create/Select/Delete only work at the menu (the active character is not online). Names unique across all characters (SetName's rule); a deleted character's name is free.
- At the menu `RequirePlayer` refuses every action (not in the world).
- A login with no character gets #1 ("Wanderer-NNNN") and is in the world at connect (bots and first-time players unchanged). A login with characters waits at the menu.
- Delete removes bag, skills, needs, hotbar, journal, worn, mat, look, marks, position, admin status, town seat (DropSigner, which vacates offices) and invites. Claims and what is built stay and weather by the existing rules.
- Client: `client/scripts/net/Me.cs` (`Me.Key(conn)`, `Me.Player`, `Me.PlayerId`, `Me.InWorld`, `Me.Characters`). Use `Me.Key(conn)` instead of `conn.Identity` for "is this mine". Main.cs `AtCharacterMenu()` is a stand-in that plays your last character until UX's menu replaces it. `Subscriptions.Self` now subscribes the views (and still the old `player WHERE identity = :sender`, which only matches a login's first character, so bots keep working).

## Progress log
- 2026-10-17 06:00: server side built (Characters.cs, mechanical ctx.Sender -> Me(ctx), connect/disconnect through EnterWorld/LeaveWorld), pushed.
- 2026-10-17 06:20: client Me helper + Subscriptions + Main stand-in, tools/CharacterCheck written.

- 2026-10-17 07:00: local fresh world: CharacterCheck, SurvivalCheck, SpawnCheck, SocialTest, LoadTest, upgrade check (origin/main) and full BuildTest (429 s) pass. BuildTest/SpawnCheck/ClayCheck/Puppet follow the menu (a returning login calls SelectCharacter). Fixed SurvivalCheck's bot name (digits were refused by the name rules since #558, so make_admin found nobody).

## Open questions / blockers
- Race: the game has no race yet, so CreateCharacter takes the look only.
- tools/render/Puppet does not build on main (AcceptTrade needs the `accept` argument; not from this change). Render lane to fix.
- Admin status is per character (WorldAdmin is keyed by the key).

## Next step
Wait for CI (PM re-runs after the release), send the PM the green sha; ask Integration to add CharacterCheck and tools/check-ctx-sender.sh to CI; UX replaces Main.AtCharacterMenu with the menu.

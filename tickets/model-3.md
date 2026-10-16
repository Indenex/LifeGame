# model-3: Each player looks like their own person
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-3-people
PR: (see below)

## Goal
Players no longer all look the same. Within the carved style Justin picked (A), the player picks their own look (option B, D-233): one of 5 muted skin tones, 5 hair colours (dark brown, black, auburn, sandy, ash grey), 4 cuts (locks, cropped, long, tied back in a knot) and no beard or a short one. The server stores the choice (`player_look` table, `SetLook` reducer, validated with `LifeGame.Shared.Looks.Valid`), so it is the same on every client and survives logging off. A player with no row gets the look derived from their player id (`Looks.Default`, the same hash as before). UX builds the picker (ui-21). Clothes and the leaf wrap still fit, since the skeleton and body are the same. Justin sees a crowd of different people (Showcase `crowd`, and in game).

## Progress log
- 2026-10-16 23:50: `HumanFigure.Appearance` (Skin, Hair, Cut, Beard), with `Appearance.For(playerId)` (an integer hash, so every client agrees) and `Appearance.Default` (today's look). Palettes are `HumanFigure.Skins` and `Hairs`. `Build(style, look)` caches per look; the body MeshInstance3D is named "Body". `PlayerModel.SetLook(figure, look)` swaps the body mesh in place, keeping the skeleton, animator and worn clothes. Hooks: `Avatar.SetLook(playerId)` (Animation's file, one line). Main.cs calls it for others when their avatar is made and for you once your player id is known (Integration's file, two small calls). Showcase `crowd` shows ten people. Client builds.
- 2026-10-17 00:00: Justin chose B (D-233). Merged origin/main (clean). Added `shared/Looks.cs` (counts, names, `Valid`, `Default`), server `Looks.cs` (public table `player_look`, reducer `SetLook(skin, hair, cut, beard)`), `Appearance.From(...)`, `Avatar.SetLook(Appearance)`, a `player_look` subscription and `OnPlayerLook` hook in Main.cs (chosen look wins over the id default, also when an avatar is created later). Server and client build; `tools/upgrade-check.sh origin/main` passes (new table, no world wipe).

## Open questions / blockers
- (none) Picker UI is UX's (ui-21); until it exists nobody has a row, so everyone keeps the id default.

## Next step
UX builds the first-join picker on SetLook; then mark the PR ready.

# model-3: Each player looks like their own person
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_review
Branch: claude/model-3-people
PR: (see below)

## Goal
Players no longer all look the same. Within the carved style Justin picked (A), each player gets a stable look from their player id: one of 5 muted skin tones, 5 hair colours (dark brown, black, auburn, sandy, ash grey), 4 cuts (locks, cropped, long, tied back in a knot) and sometimes a short beard. The look is the same on every client and never changes. Clothes and the leaf wrap still fit, since the skeleton and body are the same. Justin sees a crowd of different people (Showcase `crowd`, and in game).

## Progress log
- 2026-10-16 23:50: `HumanFigure.Appearance` (Skin, Hair, Cut, Beard), with `Appearance.For(playerId)` (an integer hash, so every client agrees) and `Appearance.Default` (today's look). Palettes are `HumanFigure.Skins` and `Hairs`. `Build(style, look)` caches per look; the body MeshInstance3D is named "Body". `PlayerModel.SetLook(figure, look)` swaps the body mesh in place, keeping the skeleton, animator and worn clothes. Hooks: `Avatar.SetLook(playerId)` (Animation's file, one line). Main.cs calls it for others when their avatar is made and for you once your player id is known (Integration's file, two small calls). Showcase `crowd` shows ten people. Client builds.

## Open questions / blockers
- Sent to the PM as a choice for Justin (D-154/D-059): A (recommended) a look from the player id; B the player picks skin, hair, cut and beard at first join (needs UX, uses the same Appearance); C everyone looks the same, as now. Building A until he answers; the PR can wait for his pick.

## Next step
Wait for Justin's pick via the PM and for CI on the PR; send the green sha when he picks A (or adapt for B).

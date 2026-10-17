# anim-4: Drink, eat, pluck, pick up, and seamless transitions
Lane: Animation
Owner: Animation session (Opus), session_019roNRWQBPw4ozNLDhzseig
Status: done
Branch: claude/anim-4
PR: #382 (merged)

## Goal
Justin (via the PM, 2026-10-17, high priority): "when drinking from a river, make it so the character bends down and scoops the water up with their mouth with seamless transitions. Other activities should have similar animations; crafting something, chopping wood, mining, picking berries, eating berries, a very basic animation when picking things up off the ground since it happens constantly".
Done means:
- Drinking at water: kneel at the bank, scoop with cupped hands to the mouth, rise. Another sip while drinking loops back to the water smoothly.
- Eating: hand to mouth with a small chew.
- Picking berries: reach into the bush, pluck, berries in hand.
- Picking things up: a quick 0.45 s bend-and-grab that plays over walking and never locks movement.
- Every action blends in and out (0.12 to 0.3 s). Walking off ends a whole-body action by blending out. Switching crafts eases out of one pose and into the next.
- Everyone sees it, your own avatar included, from the server's player_act event, sent only when the action succeeds.
- Render clips go to Justin before polishing (a feel choice).

## Progress log
- 2026-10-17 00:15: started (after #372).
  - Server: server/spacetimedb/Acts.cs, an event table PlayerAct {PlayerId, Motion, Item} with NoteAct(ctx, playerId, motion, item). One-line hooks in Survival.Drink/Eat/Forage and Reducers.PickUp. shared WorkMotion appends Drink, Eat, Pluck, PickUp.
  - Client: Subscriptions adds "SELECT * FROM player_act". WorkMotions.ActPose/ActSeconds/ActFade/ActLoop/ActUsesLegs hold the per-action data. PlayerAnimator has an act layer that blends over everything; PickUp keeps the legs striding. Avatar.Act(motion, item) shows the food, berries or find in the right hand for its part of the action. AvatarWork routes player_act to the right avatar.
  - Work-to-work crossfade fixed (no snap), work blend rate 5 -> 8 (~0.3 s).
  - Built on claude/anim-3 (#372) for Avatar.Hold. Server, client and tools build.

## Open questions / blockers
- Mining: the game has no mining yet (no mine.* process or rock-strike reducer). It's ready for a Mine motion when one exists.
- "Scoops the water up with their mouth": PM confirmed (a) cupped hands at 00:21. Hooks in Survival.cs and Reducers.cs approved; the PM tells Survival and Integration.
- Sounds synced to the motions: Audio builds them on PlayerAct.OnInsert once #382 is on main. Tell Audio (session_01SXcxtoyeqDmypiHxkHedU6) when it merges.
- Render clips requested; Render moved to session_015S96hNrEu92FrCoNZqZZjs.

## Next step
Done (#382 merged). Follow-ups live in later anim tickets.

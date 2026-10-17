# survival-37: A shared pot at the hearth
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: in_progress
Branch: claude/survival-5zelch
PR: (once opened)

## Goal
"Home and friends": a finished broth or stew pot on a fire can be ladled by its owner's group. Each pot holds 3 bowls (`Needs.PotBowls`); G "Ladle a bowl of mutton stew" gives a third of the dish's food and water and counts as a good meal (well fed). After the third bowl the pot is gone from the fire and the owner gets the empty pot back (even if offline). The owner can still take the whole pot off as before; once any bowl has been ladled, taking it off returns only the empty pot, so bowls can't be double-counted. Nothing to tend (D-148). Lane default (option 1 of 3 sent to the PM): group members only; the alternatives were anyone within reach, or the owner serving a named person.

## Progress log
- 2026-10-17 17:50: built. Server: `SimmerPot.Served` (additive, default 0), `LadlePot` in `server/spacetimedb/SharedPot.cs` (reach, group, done, not full, cooldown), `TakeOffPot` returns just the pot after any bowl. Client: a `survival.ladle` Use offer in `SurvivalClient` for finished pots of group-mates (your own pot keeps "Take the pot off the fire"). ClayCheck: an early ladle is refused as "simmering". The full ladle flow needs two players in a group around a pot, which no tool has yet. Server compiles; ctx-sender gate ok.

## Open questions / blockers
- Justin: who may ladle (group only / anyone near / owner serves). Sent to the PM.
- Not CI-covered: three ladles ending in the pot returning, and the group check. A two-bot cooking case is the follow-up.

## Next step
Open the PR from claude/survival-5zelch, wait for CI and send the green sha plus a render request (two players at a fire, ladling) to the PM.

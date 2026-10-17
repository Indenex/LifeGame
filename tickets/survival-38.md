# survival-38: A signal fire, and felled trees give sticks and fiber
Lane: Survival
Owner: Survival thread (session_01BMpUvVCwwzu1SEuBf16iFd)
Status: done
Branch: claude/survival-signal
PR: #877 (merged)

## Goal
Two requests from Justin (via the PM).
1. A separate signal fire: crafted from fiber alone (`fire.signal`, 2 fiber, lit with the same drill minigame as a campfire), burns 4 minutes (a campfire starts at 20), can't be fed, and puts up the thick, tall smoke column for as long as it burns. It leaves no cold hearth. The thick-smoke column code already exists (atmosphere-32, for any fire stoked past 75%); this only adds a way to light a fire that always signals. Bug fixes is thinning the normal campfire smoke separately; nothing here touches that path.
2. A felled tree gives 3 sticks and 2 bark fiber on top of its logs (when it comes down, i.e. on the third log; `Trees.FellSticks/FellFiber`), about a campfire's worth, so a tree feels rewarding while ground sticks and fiber still matter (D-075).
Numbers are lane defaults, sent to the PM.

## Progress log
- 2026-10-17 20:00: built. Server: `Fire.Signal` (additive, default false), `ProcessDef.Signal`, `LightFire(.., signal)` (4 min, no feeding, no top-up from a hearth, no cold hearth), `SweepSignalFires` lists a signal fire for as long as it burns, `ChopTree` gives sticks and fiber on felling. Client: Atmosphere keeps a set of signal fires and `FindSignals` holds their column to the last ten seconds; FireView draws a signal fire as a small blaze on a heap of fiber (no stone ring); Recipes text. CraftCheck: felling gives the sticks and fiber; a signal fire lights, burns `SignalSeconds`, refuses fuel. Server compiles; ctx-sender gate ok.

- 2026-10-17 20:50: #877 merged. JournalCheck needed a hint line (added); merged main after #876 so only the signal fire raises the column (`CanSignal`; the server lists only signal fires). CI run https://github.com/Indenex/LifeGame/actions/runs/39063034788 green on 498ea883.

## Open questions / blockers
- Journal and skills lane: Things to try line for `fire.signal` (D-105): "A bundle of fiber set alight burns fast and smokes thick, and carries a long way." (Sent to the PM.)
- Justin: the numbers (2 fiber, 4 min, 3 sticks + 2 fiber per tree) and the look (a small blaze on a fiber heap).

## Next step
None; ticket done. Render shot of the signal fire beside a campfire is with the PM.

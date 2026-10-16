# ceramics-9: Clay oil lamp
Lane: Clay and ceramics
Owner: Clay and ceramics lane (handed off 2026-10-16 ~20:00 to a fresh Sonnet session, D-159; read tickets/ceramics-8.md 'Handoff' first)
Status: in_progress
Branch: claude/clay-ceramics-9-kfr4kr (stacked on claude/clay-ceramics-8-kfr4kr until that merges)
PR: (none yet)

## Goal
A small fired-clay lamp for cosy nights (D-081). Done when:
1. "Render tallow" (`cook.tallow`): a coil pot of water + 2 raw mutton simmered by a fire (the ceramics-7 SimmerPot, 60 s). Taken off, it gives 1 Tallow (Item 119) and the empty coil pot back at its quality.
2. "Pinch a lamp" (`clay.lamp`, at the Potter's bench): 1 tempered clay, the shape minigame, ClayForm.Lamp. It dries and pit-fires like a pinch pot and becomes a "Clay lamp" (Item 118); unfired it is Item 117.
3. Use with a clay lamp and tallow, near a burning fire or a lit lamp (within 8 m): "Set down a lamp". It burns 30 min per tallow (D-147) with a warm 4 m glow. It refills itself: once a minute a lamp about to go out takes a tallow from baskets/jars within StorageReach or the claim stash its owner may use (D-147: no upkeep for crafted things). G by a lamp adds tallow (up to 3) or, once out, picks it back up.
4. Client: lamp mesh (shallow bowl with a pinched spout), a small flame and a flickering warm OmniLight3D, item looks, prompts. ClayCheck --lamp.
Justin sees screenshots via the Render lane: a lit lamp inside a hut at dusk.

Numbers: Justin picked the lamp (D-147) with 30 min per tallow and self-refill from storage. Others as proposed: 2 raw mutton -> 1 tallow, max 3 tallow in a lamp, 4 m light.

## Progress log
- 2026-10-16 19:48: ClayCheck --lamp PASSED on c9lamp1 (no bench refused, bench built, 2 lamps pinched at it, one cracked one whole in the pit, set-down refused without tallow). Not yet run: rendering tallow, lighting, refill (need raw mutton from Animals; CLAYCHECK_TALLOW=1). Added --ceramics-demo=lamp (three lit lamps and a cold one) for the Render lane.
- 2026-10-16 19:58: D-147: 30 min per tallow; LampTimer (60 s) refills lamps about to go out from nearby stores or the claim stash.
- 2026-10-16 19:50: server b88e065 (items 117-119, ClayForm.Lamp, clay.lamp at the bench, cook.tallow via SimmerPot giving the pot back empty, Lamps.cs SetLamp/TendLamp); client 6cb60fd (LampView flame + flickering OmniLight3D 4 m, G set down 19 / feed-light-pick up 24, KEYMAP updated, looks); ClayCheck --lamp (tallow part needs CLAYCHECK_TALLOW=1 and mutton from Animals). Running on c9lamp1.
- 2026-10-16 19:32: PM asked for ceramics-9 options (D-145); sent lamp (recommended) / roof tiles / slip decoration; started the lamp.

- 2026-10-16 21:45: ClayCheck --lamp with CLAYCHECK_TALLOW=1 PASSED on c9lamp8 (bench, 2 lamps + coil pot fired, refused without tallow, bot hunts a sheep with a spear (Animals' route 1), butchers, renders tallow in the pot, sets down and lights the lamp). Refill/G-feed not yet covered by the bot. Animals will add DebugGiveMeat later; the hunt is the fallback. Branch will need main merged again (#256 now kind 13) before the lamp PR.

## Open questions / blockers
- Animals answered 2026-10-16 20:08: use spear x2 + ButcherCarcass today (done in ClayCheck); DebugGiveMeat later.
- Animals: Tallow takes Item 119 from the Ceramics range since it's rendered in a Ceramics pot; fine to move if Animals prefers.
- Atmosphere: should a lit lamp count as light for anything at night (e.g. darkness checks)? Not needed for the first version.

## Next step
After the ceramics-7+8 PR merges: merge origin/main into claude/clay-ceramics-9-kfr4kr, build, run ClayCheck --lamp on a fresh db (and with CLAYCHECK_TALLOW=1 once Animals answers about a mutton hook), open the lamp PR, send the PM the green sha.
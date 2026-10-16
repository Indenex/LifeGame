# ceramics-9: Clay oil lamp
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: in_progress
Branch: claude/clay-ceramics-9-kfr4kr (stacked on claude/clay-ceramics-8-kfr4kr until that merges)
PR: (none yet)

## Goal
A small fired-clay lamp for cosy nights (D-081). Done when:
1. "Render tallow" (`cook.tallow`): a coil pot of water + 2 raw mutton simmered by a fire (the ceramics-7 SimmerPot, 60 s). Taken off, it gives 1 Tallow (Item 119) and the empty coil pot back at its quality.
2. "Pinch a lamp" (`clay.lamp`, at the Potter's bench): 1 tempered clay, the shape minigame, ClayForm.Lamp. It dries and pit-fires like a pinch pot and becomes a "Clay lamp" (Item 118); unfired it is Item 117.
3. Use with a clay lamp and tallow, near a burning fire or a lit lamp (within 8 m): "Set down a lamp". It burns 10 min per tallow with a warm 4 m glow. G by your lamp adds tallow (up to 3, i.e. 30 min) or, once out, picks the lamp back up.
4. Client: lamp mesh (shallow bowl with a pinched spout), a small flame and a flickering warm OmniLight3D, item looks, prompts. ClayCheck --lamp.
Justin sees screenshots via the Render lane: a lit lamp inside a hut at dusk.

Numbers (sent to the PM as defaults, D-075 low start): 2 raw mutton -> 1 tallow; 10 min light per tallow, max 3 tallow; 4 m light radius.

## Progress log
- 2026-10-16 19:50: server b88e065 (items 117-119, ClayForm.Lamp, clay.lamp at the bench, cook.tallow via SimmerPot giving the pot back empty, Lamps.cs SetLamp/TendLamp); client 6cb60fd (LampView flame + flickering OmniLight3D 4 m, G set down 19 / feed-light-pick up 24, KEYMAP updated, looks); ClayCheck --lamp (tallow part needs CLAYCHECK_TALLOW=1 and mutton from Animals). Running on c9lamp1.
- 2026-10-16 19:32: PM asked for ceramics-9 options (D-145); sent lamp (recommended) / roof tiles / slip decoration; started the lamp.

## Open questions / blockers
- PM: confirm the lamp and its numbers.
- Animals (session unknown to this lane; via PM): Tallow takes Item 119 from the Ceramics range since it's rendered in a Ceramics pot; fine to move if Animals prefers.
- Atmosphere: should a lit lamp count as light for anything at night (e.g. darkness checks)? Not needed for the first version.

## Next step
Check ClayCheck --lamp on c9lamp1 (scratchpad lamp1.log); then a render request (lamp lit inside a hut at dusk) once ceramics-7/8 has merged.
# atmosphere-35: Cosy look tune (D-313)
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-35-cosy
PR: (opening)

## Goal
Justin's art direction D-313: "cutesy, relaxing, with a hint of realism". He likes today's lighting and shadows (renders/unity-spike-*-godot.png on claude/unity-spike) but wants them less realistic, to match the chibi characters. A small look tune layered on the atmosphere-25 grade:
- lower contrast;
- a gentle saturation lift, so greens stay fresh rather than yellow mud;
- more fill light, so shade is a soft lavender-blue;
- a softer plum-cream curve;
- a peach-rose dusk instead of brown haze;
- thinner god-ray air;
- a pastel sky with a rosy horizon at dawn and dusk;
- a gentler night.
On by default; `--cosy=0` shows the earlier look for before/after renders. Sun shadow stability and softness belong to the Unity-spike worker's atmos-shadows-1; this PR doesn't touch the shadow settings.

## Progress log
- 2026-10-17 07:20: cosy layer in AtmosphereGrade.cs plus a `cosy` sky uniform in Atmosphere.cs. The client builds with 0 errors.

## Open questions / blockers
- Needs a before/after from Render (`--cosy=0` vs default) at noon, dusk and night to tune the numbers.

## Next step
Open the PR and send Render the before/after request; tune from the stills.

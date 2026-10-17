# building-23: Straw bed
Lane: House building
Owner: House building thread (Sonnet)
Status: done
Branch: claude/house-building-8ueqz2-bed
PR: #781 (merged)

## Goal
A straw bed: a sturdier, nicer rest spot built in a claim, wide enough for two to lie side by side. Justin decided resting is "just a pose", so it gives no rest bonus: it has exactly the mat's waking-place and lie-down rules, by data. Claim-only structure (Kind 25), two stages: Frame (8 sticks + 2 cordage) then Straw tick (12 fibre), more than the mat's 8 fibre (D-075). One `RestSpotDef { Id = 2, Name = "straw bed", StructureKind = 25 }` in shared/RestSpots.cs; Survival's existing server rules (RestSpotStands, SetRespawn, LieOnRestSpot) already handle structure-kind spots.

## Progress log
- 2026-10-17 13:00: PM approved the straw bed with the constraint above (no bonus; coordinate with Survival on the hook; propose any bonus as options only). Def, enum member `Bed` (25), StageProcess rule (framing then weaving), model `StructureModel.Bed.cs` (pole frame with a headboard, slats, nine straw rolls and a bolster), RestSpots entry, BuildTest part `bed` (two players build it, A chooses it as a waking place, both lie down on it; passes locally in 104 s), upgrade check passes, client and server build.
- Asked Survival (session_01RedPo6NfcLBPNHQNVhC4qf) to extend `RestSpotView` to offer "Wake here after a fall?" / "Lie down" on a finished structure-kind rest spot; the bed works server-side without it.

- 2026-10-17 12:57: #781 merged (CI run 39034004397 green, 13 jobs). Integration's #783 adds the Building check entry "straw bed" (`-- bed`). Render B has the still request.

## Open questions / blockers
- Survival (session_01RedPo6NfcLBPNHQNVhC4qf) will extend `RestSpotView` for structure-kind rest spots (the "Wake here after a fall?" / "Lie down" offer on a built bed) after their survival-31; do not touch RestSpotView. Until then the bed works on the server but has no client prompt.
- Optional, waiting on Justin only if the PM wants it: no bed bonus was proposed (resting is just a pose).

## Next step
Nothing for House building here. Next House item: PM's pick (smoking rack needs a spoilage rule from Survival; well needs water rules). Check that Render B's chest and bed stills reached the PM.

# atmosphere-12: Work sounds
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke after #66 merges (local atmosphere-12-wip, stacked on atmosphere-11-wip)
PR: (after #66 merges, or onto #66 if Justin says yes first)

## Goal
From the PM, 2026-10-16, for Crafting's chopping (#70): rhythmic axe thunks with wood-chip ticks, a creak as the tree starts to lean, a rushing swish and a heavy ground thud when it lands, and a soft leaf rustle settling. Also knapping clicks, hand-drill friction and clay squelch. Distance-based, crossfaded, never jarring. Done: clips to the PM, merged.

## Progress log
- 2026-10-16 05:05: built locally. New client/scripts/atmosphere/audio/WorkVoices.cs (SoundMix is now partial):
  - WorkVoice plays the player's own crafting job, picked by process id (SoundMix.WorkFor): chop.* axe (swing whoosh, thunk, blade bite, 3 chip ticks, a breather every 4-6 swings), knap.* and clay.grit/grog stone clicks with a ring and flake ticks, fire.hand_drill / fire.bow_drill friction strokes heating up then two breaths and tinder crackle for the last 40%, other clay.* squelch and pats. It rides the footstep bus, so it scales with the ambience (StepGain). Eases in 0.25 s, out 0.5 s.
  - Felling: one-shot timed to Look's topple (lands at 1.55 s): creak (stick-slip through 340/920 Hz resonances) with fibre cracks, accelerating rush, boom + thump + earth + branch crunch, leaf settle with twig ticks. Gain SoundMix.FellGain(d), air dulling by FellNear(d), reverb send larger when far. At most 3 at once.
  - Hooks: Atmosphere.Connect listens to MyCraftJob insert/delete; Terrain raises TreeFalls(trunk) when a cell with a standing tree is newly cleared (felled by #70's ChoppedTree, or built over), so no dependency on #70's bindings.
  - Levels (SoundPreview, woods by day): axe peaks about -16 dBFS over a -34 dBFS bed; a tree 3 m away peaks -8, 15 m -14, 40 m -23. Knap -18, drill -16, clay -15.
  - Clips: /mnt/project-files/audio/atmosphere-10-chop-and-fell.mp3, atmosphere-10-trees-falling-15m-40m.mp3, atmosphere-10-knap-drill-bowdrill-clay.mp3. SoundPreview scenes chop, fellfar, knap, drill, bowdrill, shape.

## Open questions / blockers
- The Drill, Strike, Shape and Coil minigames are Crafting's files and make no sound yet; the job sound plays after them (or for the whole work when a Master skips the minigame). Crafting could call Atmosphere.Sound.Working(...) while a minigame is open if Justin wants sound during it.

- 2026-10-16 05:30: Crafting's #74 adds `static partial void MinigameSound(ProcessDef def, bool playing)` in CraftingUi (true when any minigame opens, false when it closes). Once #74 is on main, implement it in client/scripts/atmosphere/CraftingUi.Sound.cs: playing → Atmosphere's SoundScape.Working(Drill → HandDrill or BowDrill by def.Id, Strike → Knap, Shape/Coil → Clay, def.Seconds); else Working(Work.None, 0). It needs a static handle to the SoundScape (e.g. a static Atmosphere.Current set in _Ready).

## Next step
Send clips to the PM; push to claude/atmosphere-sound-hlqfke once #66 merges (merge main, gen-bindings, build, push, open PR).

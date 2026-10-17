# anim-27: hands hold the work when lashing and sewing
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: in_progress
Branch: claude/anim-27 (stacked on claude/anim-26 / #746 until that merges)
PR: (not yet)

## Goal
Per-tool polish (option c). When lashing a tool, sewing, lacing a hide or weaving cloth (the Wrap motion), the left hand now holds the piece being worked: the first ingredient, such as the stick, hide, cloth or yarn. This is what knapping and carving already do. When the craft needs no tool, the right hand holds the cord or yarn it winds. Justin sees the actual materials in the hands rather than empty hands miming.

How: `HeldTools.For` adds Wrap to the left-hand rule. The right hand takes the last ingredient when that is Cordage or Yarn and no tool is needed. It is all derived from the ProcessDef, so there are no per-process entries.

## Progress log
- 2026-10-17 11:55: HeldTools change; regenerating bindings after the main merge to build.

## Open questions / blockers
- (none)

## Next step
Finish the client build, commit, push, and open the PR once #746 merges (else base claude/anim-26).

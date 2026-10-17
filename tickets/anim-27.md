# anim-27: hands hold the work when lashing and sewing
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-27
PR: #753

## Goal
Per-tool polish (option c). When lashing a tool, sewing, lacing a hide or weaving cloth (the Wrap motion), the left hand now holds the piece being worked: the first ingredient, such as the stick, hide, cloth or yarn. This is what knapping and carving already do. When the craft needs no tool, the right hand holds the cord or yarn it winds. Justin sees the actual materials in the hands rather than empty hands miming.

How: `HeldTools.For` adds Wrap to the left-hand rule. The right hand takes the last ingredient when that is Cordage or Yarn and no tool is needed. It is all derived from the ProcessDef, so there are no per-process entries.

## Progress log
- 2026-10-17 11:55: HeldTools change; regenerating bindings after the main merge to build.
- 2026-10-17 12:00: client builds (bindings regenerated); merged main; PR opened.
- 2026-10-17 11:51: CI green at 24224ae1 (run 39030088283); merged (#753).

## Open questions / blockers
- (none)

## Next step
Done: merged in #753.

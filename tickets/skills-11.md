# skills-11: Leatherwork and Weaver specialties
Lane: Journal and skills
Owner: Journal and skills lane (handed off 2026-10-16 19:56 by session_013QuyGdXAWemhkois2ictmc to a fresh Sonnet session, D-159)
Status: in_progress
Branch: claude/journal-skills-11 (stacked on #112 and on Crafting's #160)
PR: #193 (draft)

## Goal
D-065 and D-071 promised capped specialties for hide work and wool work once stations made them real (#160 stations, #90 wool and clothing). Two new capped skills, sharing the 300-rank cap with Potter and Builder:
- Leatherwork: scraping, stretching and drying hides (`hide.*`), and sewing leather wraps.
- Weaver: spinning wool (`cord.spin_wool`), weaving wool cloth (`weave.wool_cloth`) and sewing clothes (`sew.*`).
Husbandry keeps shearing, hunting and butchering; Toolmaking keeps cordage, baskets, mats and the drop spindle (making a tool). "Things to try" also learns the stations: building a crafting table, spindle, drying rack and potter's bench, and recipes made at a station wait for it.

## How
- `shared/Skills.cs`: two Specializations (Basic = false); `SpecOverrides` for processes whose prefix belongs to another skill (`cord.spin_wool`, `weave.wool_cloth` -> Weaver; `sew.leather_wraps` -> Leatherwork). Every caller goes through `SpecFor`, so server gains, quality caps and speed all follow.
- Points already earned stay where they were (Husbandry, Toolmaking); new work trains the new skills. No schema change, no wipe.
- Station hints: need #160 merged (ProcessDef.Station) and each station's build to report its own process id (today StageProcess reports build.frame).

## Progress log
- 2026-10-16 19:35: PM assigned skills-11 (D-145 never idle). Specialties and overrides done; harness shows the mapping; server type-checks.
- 2026-10-16 19:37: D-153 (Justin): foot wraps train Leatherwork, drop spindle stays Toolmaking, as built.
- 2026-10-16 19:50: Crafting's #160 (d947764) reports build.table / build.bench / build.spindle / build.rack via StationInfo.BuildProcess. Merged #160 into this branch. Skills.AllProcessIds yields one build id per station kind that exists; journal names; how-to lines ("C beside a crafting table to make it"); Things to try lines for the table, spindle station and drying rack; any recipe with a Station waits for that station to be built. Harness: 47 reachable, crafting table always before the basket; server type-checks.
- 2026-10-16 19:50: #112 merged. Merging main here conflicts in #160's own files (Structures.cs, Building.cs, StructureModel.cs), which are for Crafting to resolve on #160; this branch stays stacked on #160 until it merges.
- 2026-10-16 19:56: handoff (D-159). State: #193 is a draft against main and includes #160's commits; #112 (skills-10) is merged. Local checks: .NET harness (see Testing below) and a server type-check, both green on 9dd7b26/fd50e70.

## Testing without Godot
- Install .NET 8 to /tmp: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir /tmp/dotnet`.
- Server type-check: `cd server/spacetimedb && /tmp/dotnet/dotnet build -p:RuntimeIdentifier= -p:OutputType=Library -p:SelfContained=false`.
- Hint harness: a console project compiling `shared/*.cs` + `client/scripts/journal/NextSteps.cs`, with a stub `namespace LifeGame { public static class Recipes { internal static string Keys(string t) => t; } }`; loop `NextSteps.Next(known)` taking the first (and the third) id until empty and check every `Skills.AllProcessIds()` id gets reached and has a line in NextSteps.Lines.

- 2026-10-16 20:35: #160 and audio-3 (#210) merged. Merged main here (clean), swapped Journal.cs to Sfx.Play(Cue.SkillUp/Learn), deleted Chime.cs. Harness 47/47 (picks 1st and 3rd), server type-checks. Waiting on CI.

## Open questions / blockers
- #160 (Crafting, session_01Y2sA7pDBFch57bWb1uKMTq) conflicts with main in shared/Structures.cs, server/spacetimedb/Building.cs, client/scripts/building/StructureModel.cs; Crafting was told 19:50. Don't resolve those here; wait for #160 to merge.
- Audio (session_01LeXCWTFVBQcCNVEDdhPek3, audio-3, branch claude/audio-3-cues): when it merges, in client/scripts/journal/Journal.cs replace `chime.Title()` with `Sfx.Play(Cue.SkillUp)` and `chime.Discovery()` with `Sfx.Play(Cue.Learn)`, then delete client/scripts/journal/Chime.cs (and its .uid). Audio will say when it has merged.
- Ceramics-8 adds the potter's bench shape; Things to try already handles `build.bench` once it exists.

## Next step
Wait for CI on #193 (head after push), then send the green sha to the PM. Merging #237 (skills-12) first or second needs a small conflict fix in shared/Skills.cs: keep both sets of specializations and SpecOverrides.

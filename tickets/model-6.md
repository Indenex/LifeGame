# model-6: The new player character at about 1.35 m (Justin's pick A)
Lane: Modeling (with Animation, Integration, UX)
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-6-chibi
PR: (one)

## Goal
The picked chibi concept is the player figure in game: cute proportions and big head, about 1.3–1.4 m tall (Justin picked A: not true-small, so stations and doors keep working). Same 16-bone rig and bone names, so Animation's poses carry over; the look picker (player_look) offers the concept's colours and parts. Everything that assumed a 1.75 m adult is retuned, below.

## Plan (what changes for the height)
1. **Figure** [Modeling]: build the concept in `HumanFigure` (or a new `ChibiFigure` with the same `Built` contract): H = 1.35 instead of the `const float H = 1.75f`; every measurement (joints, GripDrop, HeadHeight, HipY, WaistY, ShoulderY, NeckY, PelvisWidth) already derives from H and the style's head count, so garments (`Garments.cs`) and the leaf wrap follow. Head count about 3 to 3.5 for chibi.
2. **Mouth and head attach** [Modeling]: `PlayerModel.MouthAt` uses a literal 1.75 (`float chin = 1.75f - f.HeadHeight`); take H from the built figure.
3. **Held tools** [Modeling]: ItemModels' held meshes are sized for an adult hand; scale the ToolR/ToolL attach nodes by about H / 1.75 x 1.15 (chibi hands are big) so axes and spears still read; check the bow and arrow flight origin.
4. **Nameplate** [Animation/UX, Avatar.cs]: label at y 2.05 -> about H + 0.35.
5. **Camera** [Integration, Main.cs]: `cameraRig.Focus = me.Position + (0, 0.9, 0)` -> about (0, 0.7, 0); follow distance and pitch a touch closer so the smaller figure stays readable; the Showcase `--near` camera is a reference.
6. **Work poses** [Animation]: stations are at 0.66–0.8 m, which is hip-to-chest height on a 1.35 m figure, so hands reach down less; check every WorkMotion (knapping, kneading, potter's bench, crafting table, repair table, devices) and the spindle and drying rack heights.
7. **Breath, carry and sleep** [Animation, Survival]: breath puff follows MouthAt; carried items (Carry) and the sleeping pose on the 1.75 m sleeping mat still fit (the mat can stay).
8. **Server** [none expected]: reach and interaction distances are horizontal (ClayRules.FireReach etc.), not eye height; collision is a radius. Check with Integration that nothing on the server assumes eye height.
9. **Look picker** [UX + Modeling]: `shared/Looks.cs` gains the concept's parts (ears, tail, cap, markings) as data; `HumanFigure.Appearance` grows to match; append-only.
10. Renders of the figure in the combined painted scene and at stations, through the Render lane, before the PR.

## Progress log
- 2026-10-17 04:15: Justin picked A (cute chibi at about 1.3–1.4 m). Plan written; waiting on the concept pick.
- 2026-10-17 05:10: Justin picked the roster: Pebble (tortoise), Kit (fox), Capling (mushroom), Bramble (chibi human), round 4 concepts (claude/character-concepts docs/character-concepts/out/r4-*.png), ~2.8 heads, 1.35 m, shared 16-bone base with swappable parts (D-249/250/254). Started on claude/model-6-chibi: `ChibiFigure` (Race Bramble/Kit, Look(Race, Main, Second, Style)), returns HumanFigure.Built so PlayerModel/Garments take it; pear body, big round head, short limbs, mitten hands, boots; Kit: muzzle, nose, tall or rounded ears with dark insides, cream chest, brush tail with cream tip, blue scarf; Bramble: hair (tousled or cropped), leaf wrap. Showcase `roster`. Not wired into PlayerModel yet.
- 2026-10-17 05:40: Justin: Pebble and Bramble only for now (D-255); Kit's parts stay, unpolished; Capling waits. Built from the roster-1 pack (docs/character-concepts/roster-1, on main): Bramble (hair cap with tuft, tunic with flared hem, trousers, satchel strap, boots, colours from the key) and Pebble (green skin, belly plate with seam lines, shell dome on Spine with a flat lower edge kept behind the shoulders, rim band, daisy, green feet, no wrap). Showcase `roster` shows two looks of each.
- 2026-10-17 06:10: Painted banded look on the chibis: `ChibiLook.Material` (vertex colours lit in three crisp bands, shade leaning violet, a lift on up-facing surfaces, soft dark silhouette line); `ChibiFigure.Painted` (default true) picks it, Showcase `roster --flat` shows today's shader for comparison.
- 2026-10-17 06:50: Race wiring. shared/Looks.cs: `LookRace`/`LookOption`, `Looks.Races` (0 Bramble: Skin 5, Hair 5, Cut Tousled/Cropped; 1 Pebble: Skin 3, Shell 3, Pattern Plain/Scutes), `Valid(race, a, b, c, d)` (unused slots must be 0). Server: `player_look.race` appended with `[Default(0)]` (upgrade check vs origin/main passes locally; clients disconnect on the schema change as usual), new reducer `SetRaceLook(race, a, b, c, d)`; old `SetLook` still works and sets race 0. Client: `Appearance.Race`, Main/LookPicker pass `Race`; `PlayerModel.Chibi` (on with `--chibi` on the command line) builds ChibiFigure by race, swaps Body and wrap on SetLook, body slot shows the race's wrap. ChibiFigure.Race reordered to the byte order (Bramble, Pebble, Kit). Pebble pattern 1: hex scute plates on the shell.
- 2026-10-17 07:00: Justin: 'turn them on'. Chibi is the default player (`PlayerModel.Chibi` unless `--legacy-player`); `PlayerModel.Height` (1.35 chibi, 1.75 legacy) drives the camera focus (CameraRig: 1.2 m scaled by height, ~0.93 m) and the nameplate (Avatar: height + 0.3 m, as Animation's #511). Rows with race 0 and players with no row get Bramble with their skin, hair and cut.
- 2026-10-17 07:20: Chibi garments: `ChibiFigure.Garment(look, slot, item)`: the wool tunic (fitted body, short sleeves, flared skirt, darker hem, cord belt) and the foot wraps (leather over the boot, a cuff on the shin, two cord ties); Pebble wears the tunic as a bib over its belly, clear of the shell. PlayerModel.Wear uses it for the chibi, remembers what each slot wears, and SetLook re-dresses the figure for its new race. Showcase `roster --dressed`.
- 2026-10-17 08:50: Pebble shell pass (Justin: 'much more turtle like'), branch claude/model-11-pebble-shell stacked on #466: a higher, rounder dome (radii 0.29/0.44/0.27, centre further back); a rim of 19 overlapping marginal scutes round the back and sides, flaring out over the body, open at the front; nine hex plates (a row down the spine and two side rows). Scutes: raised pads with a paler bevelled cap and two dark growth rings, on a darker dome so the grooves show. Plain: the same plates low and worn, shell-coloured. Depth by geometry rather than normal maps for now; options sent to the PM.

## Open questions / blockers
- (none)

## Next step
Open the model-6 PR (race wiring + chibi behind --chibi) once CI is green; UX switches the picker to SetRaceLook/Looks.Races; then the 1.35 m tweaks with Integration and Animation (MouthAt done for chibi; camera focus, nameplate, tool scale, poses); then refine Bramble's fringe and Pebble's rim band and shell pattern; render through Render to the PM.

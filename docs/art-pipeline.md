# Art pipeline: how to lift model quality (art-rnd-1)

Written by the Art quality R&D lane, 2026-10-17, after Justin called Pebble's turtle shell (#594) awful and asked whether better modelling methods could raise the look. This page says why our models hit a ceiling, which methods would lift them, ranks those methods by quality gain against effort, and recommends a pipeline. The turtle-shell pilot (art-rnd-1, Task 2) tests the recommendation, and its results are at the end.

## Why today's models look the way they do

Almost every model in the game is built at runtime in C# from primitives (`MeshKit`: `Ellipsoid`, `Loft`, `Tube`, `Blade`), painted with one flat vertex colour per part, and lit by a banded shader (`ChibiLook`, `Painted`). The #594 shell shows the ceiling plainly:

- **It is a pile of primitives, not one surface.** The carapace is one ellipsoid. Each scute is a flattened ellipsoid pressed onto it, the growth rings are tubes floating over it, and the marginal scutes are 19 more ellipsoids in a ring. Where the parts meet, there are hard intersections. There are no grooves cut into the shell, only bumps sitting on top of it.
- **All the detail has to be geometry.** One colour per vertex means that a groove, a growth ring or a painted highlight costs triangles. So detail stays sparse and lumpy.
- **Nothing is painted.** The reference art (Golden Skull, Dofus) gets its richness from the painting: two or three tones per material, dark lines in the crevices, light catching the edges, and brush texture. Our models have the banded light from the shader but no painting on the surface.
- **The shading normals come from the lumpy geometry.** With stepped light, every small bump makes its own band edge, so primitives read as blobs.
- **Nobody checks the model all the way round against a reference** before it reaches Justin.

The C# builders are good for things that need many seeded variants (trees, rocks, bushes, D-166) and for things that must change at runtime. They are a poor tool for a hero piece such as a character's shell, a face or a building facade.

## What the cloud container can do (tested 2026-10-17)

| Tool | Result |
|---|---|
| Blender 4.2.3 LTS | Installs in about a minute from download.blender.org (350 MB tarball, no root needed) to `~/tools/blender`, and runs with `blender -b`. Python (`bpy`, `bmesh`) drives everything: modelling, modifiers, UV unwrapping, baking and glTF export. |
| Cycles (CPU, 4 cores) | Works headless. A 256 px test frame takes about 1 s. Texture bakes (AO, normal, curvature) at 1024 px take seconds to a minute. |
| Eevee | Works under `xvfb-run` after `apt-get install libegl1 libgl1`. It is slow to start (about 45 s of shader compiling on software GL), so it is fine for stills but slow for many frames. Cycles is simpler. |
| Workbench | Works under xvfb, about 1 s per frame. Good for flat silhouette and clay checks. |
| Godot 4.6.2 | `tools/render/setup.sh` and `tools/render/scenes/*.sh` render the `Showcase` scene in Forward+ on llvmpipe, in about a minute per shot. This is the true look, because it uses the game's own shaders and light. |
| Python 3.13 + Pillow + numpy | Compositing painted textures from bake maps, side-by-side sheets and silhouette diffs. |

There is no GPU, no mouse and no human hand. So "hand-painted" has to mean painted by a script: strokes, gradients and masks driven by baked maps. Sculpting means procedural operations (displacement, booleans, remesh, smoothing), not brush sculpting. That is enough for stylised art, which is mostly clean shapes and deliberate painting.

## How the reference games get their look

- **Golden Skull (Max Heyder's packs).** These are 2D painted sprites and tiles (`docs/art-packs/golden-skull/`: 409 PNGs and no 3D files). Each material has a lit, mid and shade tone with crisp edges between them. There is a darker line in every crevice, a pale edge where a form turns towards the light, and a few loose brush strokes for texture (bark stripes, stone cracks). The light is painted in, coming from the top left.
- **Dofus.** The original game is 2D vector art. The newer versions keep the hand-painted 2D look. It uses saturated warm colours, soft dark outlines, light painted into the art, and very readable silhouettes.
- **3D games with this look.** World of Warcraft, Torchlight, Sea of Thieves and many mobile RPGs use the same recipe. The models are low-poly with clean silhouettes, and almost all of the detail is in a hand-painted diffuse texture. That texture has ambient occlusion, cavity darkening, edge highlights and some top light painted in. Lighting in the engine stays simple. The relevant techniques are:
  - **Hand-painted albedo with baked light.** The artist starts from AO, curvature and a top-down light gradient baked from the model, then paints over them. This is the core of the look.
  - **Vertex-colour painting.** Colour and AO are stored per vertex: cheap, no textures, but only as detailed as the mesh.
  - **Gradient maps.** One greyscale painted value map is recoloured through a colour ramp, so one painting gives many colour variants.
  - **Trim sheets.** One painted atlas of strips (planks, mouldings, stone courses, rope) is shared by many props and buildings, so every building gets painted detail without its own texture.
  - **Custom shading normals.** Guilty Gear Xrd and Genshin Impact edit or transfer normals from a smoothed proxy shape, so cel shade bands fall in clean, deliberate shapes instead of following every bump.

## The methods, ranked

Gain is how much better a typical model looks to Justin. Effort is for this lane in this container. Score = gain ÷ effort, roughly.

| # | Method | Gain | Effort | Use it for | Notes |
|---|---|---|---|---|---|
| 1 | **Painted albedo baked from the model** (Blender bakes AO, curvature, a top-light gradient and ID masks; a Python step paints tones, crevice lines, edge highlights and brush strokes from them) | High | Medium | Hero pieces: characters, shells, faces, tools in hand, furniture, building parts | This is where the reference look comes from. The stepped light stays in our shader; only AO, cavities and edges go into the texture, so the light is not painted twice. |
| 2 | **Custom normals from a smooth proxy** (Blender Data Transfer or a smooth copy, exported in the glTF) | High under stepped light | Low | Every authored mesh | Bands fall as smooth, deliberate shapes. This is cheap and makes the biggest single difference with `ChibiLook`'s banding. |
| 3 | **Authoring in Blender with modifiers** (one continuous surface; bevel, subdivision, solidify, boolean or displacement grooves; then decimate or retopologise to a game mesh) | High | Medium | Anything a player looks at up close | This replaces stacked primitives. Scripts stay in the repo as text (`tools/art/*.py`), so any session can change and re-run them. |
| 4 | **Reference-driven checks** (a turntable strip in Godot, side by side with the reference; a black silhouette check at play distance; a written checklist) | Medium to high | Low | Every visible model, before it reaches Justin | This catches "lumpy" and "unreadable" before Justin sees it. The `Showcase --view=turntable` view is added for this. |
| 5 | **Gradient maps for colour variants** (paint a value map once; the shader recolours it from the look's colour) | Medium | Low | Player colour options, animal coats, wood types | This keeps one texture per model while the look still picks shell, fur or skin colour. |
| 6 | **Baked vertex colours** (AO and curvature from Blender written into vertex colours) | Medium | Low | Things that stay procedural or need no texture: crowds, LODs, small props | This works with today's shaders unchanged. It is a good first upgrade for the procedural families. |
| 7 | **Trim sheets and shared painted atlases** | High for buildings | Medium | House building, stations, fences, furniture | Do this after the pilot. It is the main lever for buildings. |
| 8 | **Sculpt to low poly with a baked normal map** | Medium | Medium to high | Large organic forms (stumps, big rocks, creature bodies) | Sculpting here is procedural (displacement, remesh, smoothing). Painted art carries most of the form in the albedo, so a normal map adds less here than in realistic art. |
| 9 | **Normal maps on the painted shader** | Low to medium | Low | Only for broad relief that should catch the stepped light (scute grooves, planks) | At chibi scale and play distance, fine normal detail turns into band noise. If used, bake it from the high mesh at low strength, and paint the fine lines in the albedo instead. |
| 10 | **Parallax and depth maps** | Low | Medium | Nothing for now | It does not suit the painted look, and it costs pixel shader time. Skip it. |
| 11 | **Golden Skull packs, used directly** | Medium (textures), none (models) | Low | Colour picking, brush texture, tiling textures for terrain and stone | The packs hold no 3D models. Their tiles can be projected or sampled as painted brush texture, and their palettes can be measured. The licence forbids redistributing pack files, so derived textures should be clearly transformed, and pack files must not be copied to new places in the repo. |

## Recommended pipeline

**Art direction (D-313, 2026-10-17):** "Cutesy, relaxing, with a hint of realism." The look leans towards the chibi: soft rounded forms, chunky readable silhouettes, a warm pastel-leaning palette and gentle painted detail. Real materials (wood grain, stone facets, shell scutes) show only as a hint, never gritty. For this pipeline that means:
- Simplify real anatomy and structure into a few big, friendly shapes: three plates down a shell instead of five, chunky rims, generous bevels.
- Use soft seams in a warm dark (cocoa, not black), wide pale bevels on the upper edges, and at most one faint growth ring or grain line per plate.
- Keep a pastel palette with high values and low contrast between tones. The banded light and violet shade in the shader do the modelling.
- Use the bakes (AO, normals) for softness, not grit: wide rounded grooves, pillowed plates, and AO kept light.

For hero pieces (characters and what they wear, tools in hand, furniture, building parts):

1. **Reference first.** Collect two or three real references and the matching Golden Skull or Dofus style notes. Write the shape list and the palette (lit, mid and shade per material) in the asset's script header.
2. **Model in Blender by script** (`tools/art/<asset>.py`, run with `blender -b -P`). Build one continuous base mesh and shape it with modifiers. Cut grooves and seams into it rather than stacking parts on it. Fit it to the rig: build it in rig space (chibi: 1.35 m, `HumanRig` joints), and either parent it to one bone (rigid parts such as a shell or a helmet) or weight it to the 16 bones.
3. **Bake from it**: AO, curvature (cavities and edges), a top-light gradient, and an ID mask per region (each scute or plank). Then **paint by script**: per-region tones from the palette with slight hue shifts, dark lines in every crevice, pale lit edges, a few directional brush strokes, and painted growth rings or grain. Keep the result as a value map plus masks when the colour must vary (gradient map), or as a finished albedo when it doesn't.
4. **Shading normals**: transfer them from a smoothed copy, so the bands fall cleanly.
5. **Export a glTF** (`client/assets/models/<asset>.glb` plus a 512 or 1024 px texture, usually under 2,000 triangles for a part like this).
6. **In Godot**, load the glb and apply a painted material: `ChibiLook`'s stepped light, violet shade and dark rim, plus a sampled albedo and a tint uniform. Attach it to the right bone (a `BoneAttachment3D`) or skin it.
7. **Check it** with `Showcase --view=turntable` against the #594 shot and the references, plus a silhouette check, before sending anything to the PM.

For seeded families (trees, rocks, bushes) keep the C# builders, and lift them with methods 2, 5 and 6: smooth shading normals, painted value maps shared across the family, and baked AO. Trim sheets (7) come next for buildings.

**What this needs from other lanes:**
- *Integration:* nothing new. `.glb` files under `client/assets/` must be imported before the client runs or exports, and CI already runs `godot --headless --import` (`.github/workflows/build.yml`), as does `tools/render/lib.sh`. Blender is not needed in CI, because the exported `.glb` and `.png` files are committed. Blender is not needed in CI, because the exported `.glb` and `.png` are committed.
- *Modeling:* it owns `client/scripts/models/` and `client/assets/models/`. The pilot's shell goes to Modeling as a proposal, not as an edit to their open PR.

## Pilot: Pebble's shell

**Result.** The rebuilt shell reads as a real tortoise shell from every side. It is one high, smooth dome. Five hexagonal vertebral scutes run down the spine, with four costal scutes a side. A ring of 24 marginal scutes, the nuchal one centred at the neck, scallops the edge. On the front, the plastron has six pairs of plates: gular, humeral, pectoral, abdominal, femoral and anal. Every plate is painted in three tones, with a dark seam, a pale bevel on its upper edge, growth rings following its outline, and brush strokes along them. The three shell colours and the worn style 0 still work. Before and after turntables are in `renders/art-rnd-1-*.png`.

**How it was made** (`tools/art/pebble_shell/`; `build.sh` runs all of it in about 75 s on 4 CPU cores):

1. `geom.py` (numpy) defines the shape and the scute layout. The dome is parameterised by distance from its top along the surface, so the seams have the same width everywhere and the texture is not stretched on the steep sides. The scutes are Voronoi cells of hand-placed seeds (vertebrals in the middle, costals staggered between them, which makes the vertebrals hexagonal) inside a ring of radial marginal seams.
2. `blender_build.py` (Blender 4.2, `-b`) builds the game mesh: a smooth dome (so the light bands stay clean), the shell's wall and inside, and the plastron fitted to the body's lofts, about 3,400 triangles in all. It also builds a carved high mesh with V-grooved seams and slightly domed plates, about 460k triangles. Cycles bakes the high mesh's AO and normals onto the game mesh, with a stand-in body for contact shadow, and exports a `.glb`.
3. `paint.py` (numpy + Pillow) paints the albedo over the baked AO: per-plate tones, seams, bevels, rings and strokes. It paints at 2048 px and scales down to 1024 px. It makes two textures (scutes and worn) from one script.
4. `ChibiShell.cs` loads the `.glb` and puts it on the Spine bone with a `BoneAttachment3D`, so every pose and animation carries it. It draws the shell with `ChibiLook`'s banded light over the painted texture, plus the baked normals at 0.55 strength. A tint uniform recolours the carapace for the look's shell colour (vertex colour red masks the plastron out). `ChibiShell.Enabled = false` brings back the built shell, and Showcase's `--built-shell` does the same for comparisons.

**What it showed:**
- The quality gain came from three things, in this order: the painted texture, one continuous surface instead of stacked primitives, and checking against real tortoise anatomy. The normal map helps only at the seams.
- Iterating in-engine is quick: change a number, rebuild (75 s), render the turntable (about 90 s). It took three rounds: first too flat and too orange, then higher, then rounder lit centres and cooler browns.
- The painting is the part that most needs an artist's eye, and it is where a script is weakest. The plates are tidier and more regular than a hand-painted shell. The next step would be stamped brush strokes taken from real painted samples (the Golden Skull tiles show the stroke shapes), plus a little asymmetry.
- Cost: one 1024 px texture (about 0.5 MB as PNG) and 3,400 triangles, against roughly 2,000 triangles of ellipsoids before. That is fine for a player character.

**Handing it on.** Modeling owns `client/scripts/models/` and `client/assets/models/`. This branch is a proposal: Modeling can take `ChibiShell.cs` and `tools/art/pebble_shell/` as they are, or fold the method into #594.

# LifeGame: Unity lighting spike (unity-1)

A **comparison spike, not a migration**. The Godot game stays the product. This
folder is a small Unity 6 (URP) project that rebuilds the Godot spawn meadow, the
painted pines, the chibi player, rocks, grass and water. It lights them from the
same numbers the Godot client used, so the two engines' lighting can be compared
side by side.

The cloud sessions can't run Unity: it needs a signed-in licence, and lanes never
handle credentials. So the Unity half of the comparison runs on your PC. It takes
about 15 minutes, most of it the Unity download.

## What to click (first time)

1. **Install Unity Hub** from https://unity.com/download and sign in with your Unity
   account. If Hub asks about a licence, choose **Get a free Personal licence**.
2. **Install the editor.** In Hub: **Installs → Install Editor → Unity 6.3 (LTS)**.
   This project was made for **6000.3.26f1**; any newer 6000.3.x is fine. No extra
   modules are needed.
3. **Get the branch.** In your LifeGame clone: `git fetch origin claude/unity-spike`
   then `git checkout claude/unity-spike`. Or on GitHub, switch to the branch and
   use **Code → Download ZIP**.
4. **Open the project.** In Hub: **Projects → Add → Add project from disk**, pick
   the `unity` folder inside the repo, then click the project. If Hub says the
   editor version differs, choose your 6000.3.x editor and click **Continue**.
5. **Wait for the first import** (a few minutes). A setup script then runs by
   itself: it creates the URP settings, turns on linear colour, adds SSAO and
   opens `Assets/LifeGame/Spike.unity`. The Console should say
   `[spike] Set up: URP asset, SSAO, linear colour and Spike.unity. Press Play.`
   If Unity offers to restart for the colour-space change, say yes.
6. **Match the Godot shots.** In the **Game** tab's resolution dropdown, click
   **+** and add a **Fixed Resolution of 1280 × 720**. The Godot reference images
   are 1280 × 720.
7. **Press Play** (▶ at the top).

## Keys while playing

| Key | What it does |
|---|---|
| **1, 2, 3, ...** | Preset: `noon`, `dusk`, `twilight`, and the low-angle `-low` views (the order is shown on screen) |
| **G** | Grade. **Godot** (default): Godot's own tonemap, contrast, saturation and colour grade, so only the lighting differs. **Unity**: URP's ACES tonemapper and colour adjustments |
| **L** | Lighting. **Godot painted** (default): the game's hand-written light model, ported. **URP PBR**: Unity's stock lighting on the same painted colours |
| **P** or **F12** | Saves a screenshot to `renders/unity-spike-<preset>-<mode>.png` in the repo |
| right-drag + **WASD/QE** | Fly around (hold **Shift** to go faster); **R** returns to the Godot camera |
| **H** | Hides the help box (it is hidden in screenshots anyway) |

The Godot pictures of the same camera are in `renders/unity-spike-*-godot.png`.
Send your Unity screenshots to the PM, or commit them on this branch, and they
will be paired with the Godot ones.

## If something looks wrong

- **Pink objects**: a shader didn't compile on your graphics API. The Console
  names the shader and line; send that line to the PM.
- **"No exports in .../StreamingAssets/spike"**: the branch checkout is missing
  `unity/Assets/StreamingAssets/spike/`. Pull again.
- **Nothing happens on open**: use the menu **LifeGame → Set up spike**, then open
  `Assets/LifeGame/Spike.unity` and press Play.
- **Grey, flat picture**: **Edit → Project Settings → Graphics** should list
  `SpikeURP` as the render pipeline. If not, run **LifeGame → Set up spike**.

## How it is built

| Part | Where it comes from |
|---|---|
| Ground | Built in Unity from `shared/World.cs`, `Forest.cs` and `Landmarks.cs`, the same code the Godot client and the server use. `shared/` compiles unchanged into `Assets/LifeGame/Plugins/LifeGame.Shared.dll` (`SharedBuild~/build.sh`). `Runtime/GroundBuilder.cs` is a line-for-line port of `Terrain.BuildGround`, `TileColor` and `UnderGrass` |
| Trees, rocks, grass, water, player | Exported from the running Godot client (with model-10 painted trees and model-6 chibi merged) by `Tools~/spike_export.gd`: every mesh, instance, material and texture within 140 m of the player |
| Light, sky, fog, grade, camera | Read from the same export: Godot's sun, environment, sky material and camera at that moment |
| Shaders | `Assets/LifeGame/Shaders/`: URP HLSL ports of `Look.cs` (painted ground, rocks and bark, the PAINTED_FOLIAGE trees, grass, water), the Atmosphere sky and `ChibiLook`. The Godot shader is the spec, and the names match |
| Godot grade | `Runtime/GodotGrade.cs` bakes Godot 4.6's ACES, brightness, contrast, saturation and gradient into a URP colour LUT |

Left out of the port (all inactive at noon in summer): seasons and snow, puddles,
wading wetness, felled-tree animation, see-through tree ghosts, rain, fireflies
and other particles, stars and moon.

## Making new exports (cloud or Linux)

```
tools/render/render.sh unity-spike <git ref>            # builds a render checkout once
unity/Tools~/export_scene.sh /tmp/lifegame-render/<sha>/client unity/Assets/StreamingAssets/spike/<name> \
    --time-of-day=0.5 [--render-cam=distance,height,yaw,look]
unity/Tools~/compile_check.sh      # C# against Unity 6.3 + URP 17.3, no licence needed
python3 -I unity/Tools~/hlsl_check.py   # shaders through DXC against URP's shader library
```

A preset folder without `meshes/` holds only lighting and a camera, and reuses
`noon`'s geometry.

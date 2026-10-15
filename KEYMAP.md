# KEYMAP.md

The one list of every key the game uses. Before binding a key, add it here on your branch; if the key is taken, ask the PM. Keys that only work inside an open panel or minigame are listed under that context and may reuse a key from the world list.

Status: DECIDED by Justin on 2026-10-15 (D-025). Crafting builds the shared Use key (crafting-2); each lane moves its keys to this table after that lands.

## In the world

| Key | Action | Lane | Today |
|---|---|---|---|
| W A S D / arrows | Walk | Crafting | same |
| Q / E | Turn | Crafting | same |
| Hold Shift | Sneak (slow walk; sheep stay calm) | Animals (small hook in Main.cs) | new, D-026 |
| Space | Pick up | Crafting | same |
| F | Dig | Crafting | same |
| G | **Use** what you're facing: pick berries, feed a fire, drink at water, add materials to a blueprint, read a note | all | G forage (Survival), G feed fire (Crafting), R drink (Survival), H add materials (Building), R read note (Journal) |
| C | Crafting panel | Crafting | C also eat (Survival) |
| X | Eat the best food you carry | Survival | C |
| B | Build menu (huts and the claim marker) | Building | B also place claim (Claims) |
| K | Claim panel | Claims | T |
| J | Journal | Journal | same |
| Enter | Chat | Claims | same |
| M | Music on/off | Atmosphere | same |

## Inside a panel or mode

| Context | Keys |
|---|---|
| Crafting panel | Tab next tier, 1-9 pick a row, Esc close |
| Build placement | T / Shift+T rotate, Tab next building, Enter place, Esc cancel |
| Minigames (knapping, shaping) | Space, A/D, T (ceramics), Esc quit |

Interim until crafting-2 (Use key) lands: tend clay is V (Ceramics); it then moves to G.

## Debug only (not in player builds)

| Key | Action | Lane | Today |
|---|---|---|---|
| F5 | Hold to fast-forward time | Atmosphere | T |
| F6 | Cycle weather | Atmosphere | Y |

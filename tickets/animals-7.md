# animals-7: Fewer draw calls per sheep
Lane: Animals
Owner: Animals thread (session session_01XS7M1yLy2fLaFdE3HWnisM)
Status: in_progress
Branch: developed locally as animals-7-local on top of animals-6 (#69); goes to claude/animals-sheep-5gkntn once #69 merges
PR: (after #69)

## Goal
Atmosphere counted about 5+ sheep mesh nodes per sheep at spawn. Cut sheep draw calls with no visible change to looks or animation.

## How it works
- `client/scripts/animals/Sheep.cs`: each sheep was 8 MeshInstance3D nodes (skin, fleece, head, wool cap, 4 legs), 8 draw calls plus shadows. Now it is one skinned MeshInstance3D under a Skeleton3D with bones body, fleece, head and 4 legs; the shared ArrayMesh has 2 surfaces:
  - surface 0, wool (fleece, tail, cap) with the sheep's fleece material per instance (surface override), same relief settings as before;
  - surface 1, skin + face + legs with one shared plain material; the part color rides in the vertex color, pre-corrected so the shader's pow(2.2) gives exactly the old sRGB albedo.
- Animation sets bone poses (position/rotation/scale, Euler in Node3D's YXZ order) where it used to set node transforms, so motion is identical: leg swing, bounce, breathing, head pitch/yaw, fleece shrink/regrow, lying down.

## Measurements (gl_compatibility, fresh `perf` DB, `--render-stats=1 --screenshot-delay=20`)
- Before (main + animals-6): draw_calls=2107 objects=2231, Herd=104 nodes.
- After: draw_calls=1593 objects=1717, Herd=17 nodes.

## Progress log
- 2026-10-16 05:00: written, builds, measured. Daytime screenshot screenshots/animals-09-skinned-sheep.png looks the same as animals-07-day-after.png (before).

- 2026-10-16 05:10: PM fold-in: playtest saw sheep crowd a kiln. AnimalFires.cs now also treats a firing kiln as a fire, measured from its wall (0.8 m out). Pit firings were already covered: they need a fueled Fire, which #69 avoids. Server builds; not live-tested (no kiln in the local world).

## Open questions / blockers
- (none)

## Next step
When #69 merges, restart claude/animals-sheep-5gkntn from main, merge animals-7-local, build, push, open the PR.

"""The stone axe, step 2 of 2 (art-rnd-1): paint the texture from the bakes, in the
Golden Skull way with Justin's "clean, not pitted" rule: smooth colour blocks, broad
soft gradients, soft cocoa crevices, gentle highlights and only a few large soft strokes.

    python3 -I tools/art/axe/paint.py <bake dir> <out dir>

Stone: each knapped facet is one flat block in one of three cool grey-violet tones
(chosen by how it faces the light, nudged per facet), with a pale lit line along the
facet ridges that face the light, a soft dark one along those that don't, and a pale
honed band at the cutting edge. Wood: honey tones lit from above, with four long soft
grain streaks that wander down the haft. Cord: straw tones, cocoa where it sinks in.
Writes axe.png.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)
S = 1024
LIGHT = np.array([-0.45, 0.8, 0.4])
LIGHT /= np.linalg.norm(LIGHT)


def c(*rgb):
    return np.array(rgb, float)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def blur(a, r):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(r)), float) / 255


ao = blur(np.asarray(Image.open(os.path.join(src, "axe_ao.png")).convert("L"), float) / 255, 3)
g = lambda a: np.stack([a[..., 0], a[..., 2], -a[..., 1]], -1)
p = g(np.load(os.path.join(src, "axe_pos.npy"))[..., :3])
n = g(np.load(os.path.join(src, "axe_nrm.npy"))[..., :3])
n /= np.maximum(np.linalg.norm(n, axis=-1, keepdims=True), 1e-6)
pid = np.rint(np.load(os.path.join(src, "axe_id.npy"))[..., 0] * 16).astype(int)
s = n @ LIGHT

STONE = dict(lit=c(0.8, 0.77, 0.8), mid=c(0.62, 0.6, 0.67), shade=c(0.45, 0.43, 0.53))
WOOD = dict(lit=c(0.86, 0.66, 0.44), mid=c(0.72, 0.52, 0.34), shade=c(0.55, 0.39, 0.34))
CORD = dict(lit=c(0.95, 0.86, 0.64), mid=c(0.83, 0.71, 0.5), shade=c(0.64, 0.54, 0.46))
COCOA = c(0.36, 0.25, 0.22)


def tone(pal, lit, shade):
    col = np.ones(lit.shape + (1,)) * pal["mid"]
    col = col + (pal["lit"] - pal["mid"]) * lit[..., None]
    return col + (pal["shade"] - col) * shade[..., None]


col = np.ones((S, S, 3)) * STONE["mid"]

# Stone: one tone per facet (flat normals), nudged by a per-facet hash.
st = pid == 1
key = np.round(n * 12) @ np.array([12.9898, 78.233, 37.719])
h = (np.sin(key) * 43758.5453) % 1.0
f = s + (h - 0.5) * 0.35
stone = tone(STONE, smooth(0.42, 0.45, f), 1 - smooth(-0.02, 0.01, f))
# Facet ridges: where the baked normal jumps between neighbouring texels.
dn = np.zeros((S, S))
for ax in (0, 1):
    d = np.linalg.norm(np.diff(n, axis=ax, append=n.take([-1], axis=ax)), axis=-1)
    same = np.diff(pid, axis=ax, append=pid.take([-1], axis=ax)) == 0
    dn = np.maximum(dn, np.where(same & st, d, 0))
ridge = blur((dn > 0.12).astype(float), 1.6)
ridge = smooth(0.08, 0.4, ridge)
facing = smooth(-0.1, 0.3, s)
stone += (STONE["lit"] * 1.1 - stone) * (ridge * facing * 0.7)[..., None]
stone += (COCOA * 0.9 - stone) * (ridge * (1 - facing) * 0.35)[..., None]
# The honed cutting edge: a pale band along +Z.
zmax = p[st][:, 2].max() if st.any() else 0
hone = smooth(zmax - 0.03, zmax - 0.012, p[..., 2])
stone += (c(0.9, 0.88, 0.9) - stone) * (hone * 0.6)[..., None]
col[st] = stone[st]

# Wood: honey tones and four long soft grain streaks.
wd = pid == 2
th = np.arctan2(p[..., 2], p[..., 0])
y = p[..., 1]
streak = np.zeros((S, S))
for k, (a0, ph) in enumerate(((0.3, 0.0), (1.9, 1.3), (3.6, 2.1), (4.9, 0.7))):
    a = a0 + 0.25 * np.sin(y * 7 + ph) + 0.12 * np.sin(y * 17 + ph * 2)
    d = np.abs(((th - a + np.pi) % (2 * np.pi)) - np.pi)
    streak = np.maximum(streak, (1 - smooth(0.05, 0.22, d)) * smooth(-0.6, -0.3, y + 0.05 * k) * (1 - smooth(0.05, 0.12, y)))
wood = tone(WOOD, smooth(0.38, 0.44, s), 1 - smooth(-0.05, 0.0, s))
wood += (WOOD["shade"] - wood) * (streak * 0.45)[..., None]
col[wd] = wood[wd]

# Cord: straw, sinking into cocoa where it wraps tight.
cd = pid == 3
cord = tone(CORD, smooth(0.35, 0.42, s), 1 - smooth(-0.05, 0.0, s))
col[cd] = cord[cd]

# Soft cocoa crevices and a light AO over everything.
line = 1 - smooth(0.35, 0.65, ao)
col += (COCOA - col) * (line * 0.6)[..., None]
col *= (0.88 + 0.12 * ao)[..., None]
Image.fromarray((np.clip(col, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(dst, "axe.png"), optimize=True)
print("painted axe.png")

"""Painted trees, step 2 of 2 (art-rnd-1): paint the albedo from the bakes, in 3D.

    python3 tools/art/trees/paint.py <bake dir> <out dir> <birch|pine>

Reads <bake dir>/<tree>_bake.npz (AO, object-space normal, position and leaf mask per
texel, from blender_build.py) and writes <out>/<tree>_albedo.png. Every stroke is
placed by the texel's position on the tree, not by its UV, so the painting runs across
the UV seams.

The painting is clean and soft (D-313, and Justin: "the weird pitted look is not
cutesy"): smooth colour blocks joined by soft, broad gradients, soft cocoa or
violet-green crevice lines, gentle highlights, and only a few large, soft strokes. No
noise grain, speckle or small dabs anywhere.

Leaves: the crown's form picks the tone (pale lit tops, a fresh mid green, a shade
green, and a violet-leaning deep tone in the undersides and in the crevices between
clumps, from the AO). Over that, a few large leaf clusters (cells of a jittered 3D grid,
about a third of a clump across) each get a soft shade crescent under their lower edge
and a gentle light on their top: the big scalloped leaf-cluster strokes of the Golden
Skull trees. The shader's banded light and violet shade then model it once more.

Bark: birch is warm white with lavender-grey shade, a few soft cocoa marks across the
trunk and a darker foot; pine is warm brown in long vertical plates with soft cocoa
crevices.
"""
import os
import sys

import numpy as np
from PIL import Image

bake, out, TREE = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
d = np.load(os.path.join(bake, f"{TREE}_bake.npz"))


def c(*rgb):
    return np.array(rgb, float) / 255.0


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    return a + (b - a) * np.asarray(t)[:, None]


def hash3(i, j, k, seed):
    h = (i * 73856093) ^ (j * 19349663) ^ (k * 83492791) ^ (seed * 2654435761)
    h = (h ^ (h >> 13)) * 1274126177
    return ((h ^ (h >> 16)) & 0xFFFFFF) / float(0xFFFFFF)


def cells(p, size, seed, jitter=0.8):
    """Jittered-grid Voronoi in 3D: (nearest distance, second distance, offset from the
    nearest seed in metres, a random value per cell). size is the cell size per axis."""
    q = p / size
    base = np.floor(q).astype(np.int64)
    d1 = np.full(len(p), 1e9)
    d2 = np.full(len(p), 1e9)
    off = np.zeros_like(p)
    rnd = np.zeros(len(p))
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dz in (-1, 0, 1):
                g = base + np.array([dx, dy, dz])
                i, j, k = g[:, 0], g[:, 1], g[:, 2]
                seedp = g + 0.5 + (np.stack([hash3(i, j, k, seed), hash3(i, j, k, seed + 1), hash3(i, j, k, seed + 2)], -1) - 0.5) * jitter
                o = q - seedp
                dist = np.linalg.norm(o, axis=1)
                closer = dist < d1
                d2 = np.where(closer, d1, np.minimum(d2, dist))
                d1 = np.where(closer, dist, d1)
                off = np.where(closer[:, None], o * size, off)
                rnd = np.where(closer, hash3(i, j, k, seed + 3), rnd)
    return d1, d2, off, rnd


def ramp(v, tones, edges, soft):
    """Colour blocks with soft joins: tones[i] below edges[i], blended over +-soft."""
    col = np.repeat(tones[:1], len(v), 0)
    for e, t in zip(edges, tones[1:]):
        col = mix(col, np.repeat(t[None], len(v), 0), smooth(e - soft, e + soft, v))
    return col


# Blender bakes -> Godot space (x, y up, z forward).
pos = d["pos"][..., :3]
P = np.stack([pos[..., 0], pos[..., 2], -pos[..., 1]], -1).reshape(-1, 3)
nb = d["normal"][..., :3] * 2 - 1
N = np.stack([nb[..., 0], nb[..., 2], -nb[..., 1]], -1).reshape(-1, 3)
valid = d["normal"][..., :3].reshape(-1, 3).sum(1) > 0.01   # unbaked texels stay 0
N[valid] /= np.linalg.norm(N[valid], axis=1, keepdims=True)
ao = d["ao"][..., 0].reshape(-1)
leaf = d["leaf"][..., 0].reshape(-1) > 0.5
SIZE = d["ao"].shape[0]
img = np.zeros((SIZE * SIZE, 3))

COCOA = c(92, 64, 58)
if TREE == "birch":
    # deep (violet-leaning), shade, mid, lit, highlight
    LEAF = np.array([c(74, 86, 104), c(88, 128, 86), c(120, 166, 82), c(164, 200, 94), c(204, 224, 128)])
    EDGES = [0.2, 0.42, 0.66, 0.9]
    CLUSTER = np.array([0.36, 0.26, 0.36])
    BARK = dict(lit=c(244, 238, 224), mid=c(224, 216, 206), shade=c(182, 174, 196), mark=c(86, 66, 64), foot=c(146, 130, 132))
else:
    LEAF = np.array([c(46, 58, 78), c(50, 86, 74), c(66, 114, 78), c(100, 146, 84), c(138, 176, 102)])
    EDGES = [0.2, 0.42, 0.66, 0.92]
    CLUSTER = np.array([0.3, 0.17, 0.3])
    BARK = dict(lit=c(182, 124, 84), mid=c(148, 96, 68), shade=c(112, 76, 72), mark=c(86, 56, 52), foot=c(120, 84, 66))

# ---- Leaves ----
m = valid & leaf
p, n, a = P[m], N[m], ao[m]
# The crown's form: up-facing is lit, down-facing and occluded is shade.
form = 0.5 + 0.42 * n[:, 1] + 1.0 * (a - 0.85)
d1, d2, off, rnd = cells(p, CLUSTER, 3)
edge = d2 - d1                                  # 0 on a border between clusters
oy = off[:, 1] / CLUSTER[1]                     # about -0.6..0.6, down..up in a cluster
# Each cluster: a gentle light on its top half, a soft shade crescent under its lower
# edge where it tucks over the one below.
v = form + 0.1 * smooth(-0.1, 0.45, oy) * smooth(0.05, 0.3, edge) + 0.04 * (rnd - 0.5)
crescent = smooth(0.24, 0.06, edge) * smooth(0.05, -0.3, oy)
v = v - 0.2 * crescent
col = ramp(v, LEAF, EDGES, 0.035)
# The crevices between clumps: a soft glaze to the deep tone, then a soft cocoa line in
# the deepest of them.
col = mix(col, np.repeat(LEAF[:1], len(a), 0), 0.6 * smooth(0.8, 0.4, a))
col = mix(col, np.repeat(COCOA[None], len(a), 0), 0.35 * smooth(0.5, 0.25, a))
img[m] = col

# ---- Bark ----
m = valid & ~leaf
p, n, a = P[m], N[m], ao[m]
lit = 0.5 + 0.5 * n[:, 1]
col = ramp(lit, np.array([BARK["shade"], BARK["mid"], BARK["lit"]]), [0.42, 0.62], 0.05)
if TREE == "birch":
    # A few soft cocoa marks across the trunk, wider than tall, and a darker foot.
    d1, d2, off, rnd = cells(p, np.array([0.3, 0.13, 0.3]), 31)
    mark = (rnd < 0.3) * smooth(0.5, 0.36, np.linalg.norm(off / np.array([1.0, 0.28, 1.0]), axis=1) / 0.3) * (p[:, 1] > 0.35)
    col = mix(col, np.repeat(BARK["mark"][None], len(a), 0), 0.85 * mark)
    col = mix(col, np.repeat(BARK["foot"][None], len(a), 0), smooth(0.45, 0.12, p[:, 1]))
else:
    # Long vertical plates with soft cocoa crevices and a pale top on each plate.
    d1, d2, off, rnd = cells(p, np.array([0.12, 0.4, 0.12]), 41)
    col = mix(col, np.repeat(BARK["lit"][None], len(a), 0), 0.35 * smooth(0.1, 0.4, off[:, 1] / 0.4) * smooth(0.2, 0.4, d2 - d1))
    col = mix(col, np.repeat(BARK["mark"][None], len(a), 0), 0.8 * smooth(0.16, 0.05, d2 - d1))
col = mix(col, np.repeat(COCOA[None], len(a), 0), 0.45 * smooth(0.8, 0.3, a))
img[m] = col

# Unused texels: the mid leaf green, so mip levels don't bleed black.
img[~valid] = LEAF[2]
im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8).reshape(SIZE, SIZE, 3))
im.save(os.path.join(out, f"{TREE}_albedo.png"), optimize=True)
print("painted", os.path.join(out, f"{TREE}_albedo.png"))

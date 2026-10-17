"""Painted trees, step 2 of 2 (art-rnd-1): paint the albedo from the bakes, in 3D.

    python3 tools/art/trees/paint.py <bake dir> <out dir> <birch|pine>

Reads <bake dir>/<tree>_bake.npz (AO, object-space normal, position and leaf mask per
texel, from blender_build.py) and writes <out>/<tree>_albedo.png. Every stroke is
placed by the texel's position on the tree, not by its UV, so the painting runs across
the UV seams.

Leaves, the Golden Skull way: the crown is covered in leaf clusters (cells of a jittered
3D grid). Each cluster is lit on its upper half and has a dark, violet-leaning crescent
under its lower edge where it overlaps the one below, which gives the scalloped
leaf-cluster strokes. Over that, the crown's own form picks one of four crisp tones:
pale lit tops, a fresh mid green, a shade green and a violet-leaning deep tone in the
undersides and the crevices between clumps (the AO). The shader's banded light and
violet shade then model it once more as a whole.

Bark: birch is warm white with lavender-grey shade, dark horizontal lenticel dashes and
a darker, rougher foot; pine is warm brown in vertical plates with cocoa crevices.
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


def hash3(i, j, k, seed):
    h = (i * 73856093) ^ (j * 19349663) ^ (k * 83492791) ^ (seed * 2654435761)
    h = (h ^ (h >> 13)) * 1274126177
    return ((h ^ (h >> 16)) & 0xFFFFFF) / float(0xFFFFFF)


def cells(p, size, seed, jitter=0.85):
    """Jittered-grid Voronoi in 3D: (nearest distance, second distance, offset from the
    nearest seed, a random value per cell). size is the cell size per axis."""
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


def noise3(p, scale, seed):
    """Cheap smooth-ish 3D value noise in 0..1."""
    q = p / scale
    b = np.floor(q).astype(np.int64)
    f = q - b
    f = f * f * (3 - 2 * f)
    acc = 0
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (f[:, 0] if dx else 1 - f[:, 0]) * (f[:, 1] if dy else 1 - f[:, 1]) * (f[:, 2] if dz else 1 - f[:, 2])
                acc = acc + w * hash3(b[:, 0] + dx, b[:, 1] + dy, b[:, 2] + dz, seed)
    return acc


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

if TREE == "birch":
    LEAF = [c(70, 88, 104), c(98, 140, 96), c(146, 188, 98), c(192, 218, 112), c(228, 240, 158)]
    CELL = np.array([0.2, 0.17, 0.2])
    BARK = dict(lit=c(246, 241, 228), mid=c(226, 220, 210), shade=c(184, 178, 198), mark=c(70, 60, 68), foot=c(150, 140, 146))
else:
    LEAF = [c(48, 64, 84), c(62, 100, 86), c(88, 138, 92), c(130, 174, 100), c(176, 206, 124)]
    CELL = np.array([0.17, 0.11, 0.17])
    BARK = dict(lit=c(186, 128, 86), mid=c(150, 98, 68), shade=c(110, 74, 70), mark=c(72, 46, 46), foot=c(120, 84, 66))
LEAF = np.array(LEAF)

# ---- Leaves ----
m = valid & leaf
p, n, a = P[m], N[m], ao[m]
d1, d2, off, rnd = cells(p, CELL, 3)
edge = d2 - d1                                  # 0 on a border between clusters
oy = off[:, 1] / CELL[1]                        # -0.6..0.6 down..up within a cluster
# The crown's form: up-facing is lit, down-facing and occluded is shade.
form = 0.5 + 0.42 * n[:, 1] + 0.9 * (a - 0.82)
# Each cluster: lighter on its upper half, a little random per cluster.
cluster = 0.2 * smooth(-0.25, 0.35, oy) + 0.08 * (rnd - 0.5)
wob = 0.06 * (noise3(p, 0.09, 9) - 0.5)
v = form + cluster + wob
tone = np.digitize(v, [0.18, 0.4, 0.66, 0.9])   # 0 deep .. 4 highlight
# A dark crescent under each cluster's lower edge (where it overlaps the next one
# down), one tone darker, deepest in the shade.
under = (edge < 0.16 + 0.1 * rnd) & (oy < -0.05)
tone = np.where(under, np.maximum(tone - 1 - (form < 0.45), 0), tone)
# Highlights only on the lit tops of clusters, as small strokes.
hi = (tone >= 3) & (oy > 0.18) & (edge > 0.22)
tone = np.where(hi & (form > 0.72), 4, np.minimum(tone, 3))
col = LEAF[tone]
# A soft AO glaze toward the deep tone in the crevices between clumps.
col = col + (LEAF[0] - col) * (0.55 * smooth(0.75, 0.35, a))[:, None]
img[m] = col

# ---- Bark ----
m = valid & ~leaf
p, n, a = P[m], N[m], ao[m]
lit = 0.5 + 0.5 * n[:, 1] + 0.12 * (noise3(p, 0.12, 21) - 0.5)
col = np.where((lit > 0.62)[:, None], BARK["lit"], BARK["mid"])
col = np.where((lit < 0.42)[:, None], BARK["shade"], col)
if TREE == "birch":
    # Horizontal lenticel dashes and a few bigger dark marks; more and darker at the foot.
    f = smooth(0.9, 0.0, p[:, 1])
    d1, d2, off, rnd = cells(p, np.array([0.11, 0.032, 0.11]), 31)
    dash = (rnd < 0.16 + 0.3 * f) & (d1 < 0.55)
    d1b, _, offb, rndb = cells(p, np.array([0.3, 0.16, 0.3]), 37)
    patch = (rndb < 0.14) & (d1b < 0.42 + 0.1 * noise3(p, 0.05, 3)) & (p[:, 1] > 0.6)
    col = np.where((dash | patch)[:, None], BARK["mark"], col)
    foot = (f * (0.6 + 0.6 * noise3(p, 0.07, 5))) > 0.55
    col = np.where((foot & ~dash)[:, None], BARK["foot"], col)
else:
    # Vertical plates with cocoa crevices and a pale stroke on each plate's lit side.
    d1, d2, off, rnd = cells(p, np.array([0.07, 0.26, 0.07]), 41)
    crev = (d2 - d1) < 0.12
    stroke = (off[:, 1] > 0.04) & (rnd > 0.55) & ((d2 - d1) > 0.3) & (lit > 0.45)
    col = np.where(stroke[:, None], BARK["lit"], col)
    col = np.where(crev[:, None], BARK["mark"], col)
col = col * (0.7 + 0.3 * smooth(0.2, 0.85, a))[:, None]
img[m] = col

# Unused texels: the mid leaf green, so mip levels don't bleed black.
img[~valid] = LEAF[2]
im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8).reshape(SIZE, SIZE, 3))
im.save(os.path.join(out, f"{TREE}_albedo.png"), optimize=True)
print("painted", os.path.join(out, f"{TREE}_albedo.png"))

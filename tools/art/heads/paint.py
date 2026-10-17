"""Bramble's and Pebble's heads, step 2 of 2 (art-rnd-1): paint the textures.

    python3 tools/art/heads/paint.py <bake dir> <out dir>

Reads the passes blender_build.py baked (<name>_{id,pos,nrm,curv,ao}.npy) and paints, in
the Golden Skull way under D-313 (warm pastels, two or three crisp tones per material, a
soft cocoa line in crevices, pale lit upper edges, a few strokes, shade leaning violet):

- <out>/<name>_head.png: the head's albedo (skin, hair, daisy).
- <out>/<name>_face.png: the face atlas, 2 x 2 cells of 512 px, one per expression:
  0 open, 1 closed (blink), 2 happy (^ ^ eyes, open smile), 3 sleepy (half-lidded).
  Cell e sits at column e % 2, row e // 2 (UV offset (e % 2, e // 2) * 0.5). The cells
  paint the face in metres across the face patch's frame (FACE_X, FACE_Z below), as
  seen from the front, with transparent background.

The skin and hair are painted in ChibiFigure's default look colours (Skins[0], Hairs[2]
for Bramble; PebbleSkins[0] for Pebble); ChibiFace tints other looks from them.
"""
import os
import sys

import numpy as np
from PIL import Image

SIZE = 1024
HH = 1.35 / 2.8
C = np.array([0.0, -0.01, 1.35 - HH + HH * 0.5])
FACE_X = (-0.19, 0.19)
FACE_Z = (-0.15, 0.11)
SKIN, HAIR, PETAL, HEART, LEAF = 1, 2, 3, 4, 5


def c(*rgb):
    return np.array(rgb, float)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def mix(a, b, t):
    t = np.asarray(t, float)
    return a + (b - a) * (t[..., None] if t.ndim else t)


def noise3(p, seed=0):
    """Smooth 3D value noise, one feature per unit."""
    rng = np.random.default_rng(seed)
    perm = rng.permutation(256)
    vals = rng.random(256)
    pi = np.floor(p).astype(int)
    f = p - pi
    f = f * f * (3 - 2 * f)

    def h(dx, dy, dz):
        i = perm[(perm[(perm[(pi[..., 0] + dx) & 255] + pi[..., 1] + dy) & 255] + pi[..., 2] + dz) & 255]
        return vals[i]

    x0 = h(0, 0, 0) * (1 - f[..., 0]) + h(1, 0, 0) * f[..., 0]
    x1 = h(0, 1, 0) * (1 - f[..., 0]) + h(1, 1, 0) * f[..., 0]
    x2 = h(0, 0, 1) * (1 - f[..., 0]) + h(1, 0, 1) * f[..., 0]
    x3 = h(0, 1, 1) * (1 - f[..., 0]) + h(1, 1, 1) * f[..., 0]
    y0 = x0 * (1 - f[..., 1]) + x1 * f[..., 1]
    y1 = x2 * (1 - f[..., 1]) + x3 * f[..., 1]
    return y0 * (1 - f[..., 2]) + y1 * f[..., 2]


def voronoi_edge(p, cell, seed=4):
    """Distance (cells) to the nearest Voronoi edge in 3D (F2 - F1), and the cell id."""
    q = p / cell
    qi = np.floor(q).astype(int)
    rng = np.random.default_rng(seed)
    jit = rng.random((32, 32, 32, 3))
    d1 = np.full(q.shape[:-1], 9.0)
    d2 = np.full(q.shape[:-1], 9.0)
    cid = np.zeros(q.shape[:-1], int)
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            for dz in (-1, 0, 1):
                n = qi + np.array([dx, dy, dz])
                k = n & 31
                fp = n + jit[k[..., 0], k[..., 1], k[..., 2]]
                d = np.linalg.norm(q - fp, axis=-1)
                closer = d < d1
                d2 = np.where(closer, d1, np.minimum(d2, d))
                cid = np.where(closer, k[..., 0] * 1024 + k[..., 1] * 32 + k[..., 2], cid)
                d1 = np.minimum(d1, d)
    return d2 - d1, cid


def load(bake, name):
    g = {k: np.load(os.path.join(bake, f"{name}_{k}.npy")) for k in ("id", "pos", "nrm", "curv", "ao")}
    part = np.rint(g["id"][..., 0] * 10).astype(int)
    pos = (g["pos"][..., :3] - 0.5) / 0.25 - C       # relative to the head centre, Blender axes
    nrm = (g["nrm"][..., :3] - 0.5) / 0.5
    nrm /= np.maximum(np.linalg.norm(nrm, axis=-1, keepdims=True), 1e-6)
    # Soften the AO's sampling noise and the curvature's triangle noise, so neither
    # paints grain: the crevice lines and lit edges come out as broad, soft shapes.
    return part, pos, nrm, blur(g["curv"][..., 0], part, 5), blur(g["ao"][..., 0], part, 4)


def blur(a, part, radius):
    """A box blur, three passes (near Gaussian), only within the baked texels."""
    m = (part > 0).astype(float)
    a, w = a * m, m.copy()
    k = 2 * radius + 1
    for _ in range(3):
        for axis in (0, 1):
            pad = [(radius + 1, radius) if i == axis else (0, 0) for i in range(2)]
            cs = np.cumsum(np.pad(a, pad, mode="edge"), axis=axis)
            cw = np.cumsum(np.pad(w, pad, mode="edge"), axis=axis)
            sl_hi = [slice(None)] * 2
            sl_lo = [slice(None)] * 2
            sl_hi[axis] = slice(k, None)
            sl_lo[axis] = slice(0, -k)
            a = (cs[tuple(sl_hi)] - cs[tuple(sl_lo)]) / k
            w = (cw[tuple(sl_hi)] - cw[tuple(sl_lo)]) / k
            a, w = a * m, w * m
    return np.where(w > 1e-6, a / np.maximum(w, 1e-6), 0.5)


def tones(light, shade_c, mid_c, lit_c, lo=0.33, hi=0.68, edge=0.025):
    """Three painted tones with crisp edges from a 0..1 light value."""
    col = mix(shade_c, mid_c, smooth(lo - edge, lo + edge, light))
    return mix(col, lit_c, smooth(hi - edge, hi + edge, light))


# ---- Bramble -----------------------------------------------------------------------
B_SKIN = dict(shade=c(0.86, 0.66, 0.6), mid=c(0.96, 0.8, 0.66), lit=c(0.99, 0.88, 0.75))
BLUSH = c(0.98, 0.62, 0.56)
B_HAIR = dict(shade=c(0.45, 0.27, 0.25), mid=c(0.62, 0.37, 0.23), lit=c(0.76, 0.5, 0.31))
HAIR_LINE = c(0.33, 0.19, 0.18)
HAIR_SHINE = c(0.88, 0.66, 0.44)
CREASE = c(0.55, 0.36, 0.32)


def paint_skin(pos, nrm, ao, pal, n_seed=1):
    # One or two broad, soft value shifts across the head; no grain (Justin: no pitted look).
    wob = (noise3(pos * 6, n_seed) - 0.5) * 0.1
    light = 0.5 + 0.45 * nrm[..., 2] + wob
    col = tones(light, pal["shade"], pal["mid"], pal["lit"], lo=0.12, hi=0.82)
    return col


def bramble(part, pos, nrm, curv, ao):
    img = np.zeros(part.shape + (3,))
    skin = part == SKIN
    col = paint_skin(pos, nrm, ao, B_SKIN)
    # Cheek blush: soft round ovals under the outer corners of the eyes, a crisp core.
    for side in (-1, 1):
        d = np.hypot((pos[..., 0] - side * 0.118) / 0.046, (pos[..., 2] + 0.045) / 0.03)
        front = smooth(0.0, 0.3, -nrm[..., 1])
        col = mix(col, BLUSH, (0.55 * (1 - smooth(0.55, 1.0, d)) + 0.2 * (1 - smooth(0.0, 0.5, d))) * front)
    # The AO, and a soft cocoa line where the hair meets the skin.
    col = col * (0.7 + 0.3 * ao ** 1.2)[..., None]
    col = mix(col, CREASE, (1 - smooth(0.25, 0.5, ao)) * 0.6)
    img[skin] = col[skin]

    hair = part == HAIR
    rel = pos
    # Strands run out from the crown: stripes across the azimuth, stretched down the lock.
    az = np.arctan2(rel[..., 0], -rel[..., 1])
    height = rel[..., 2]
    strand = noise3(np.stack([az * 4, height * 3, np.zeros_like(az)], -1), 5)
    # A few large, very soft strokes, lock-wide; nothing finer.
    fine = noise3(np.stack([az * 5, height * 3, np.ones_like(az)], -1), 6)
    light = 0.18 + 0.42 * ao ** 1.5 + 0.28 * np.clip(nrm[..., 2], -1, 1) + 0.9 * (curv - 0.5) + (strand - 0.5) * 0.25
    col = tones(light, B_HAIR["shade"], B_HAIR["mid"], B_HAIR["lit"], lo=0.32, hi=0.66)
    col = col * (1 + (fine - 0.5) * 0.08)[..., None]
    # A pale lit edge on the convex upper sides of each clump.
    bevel = smooth(0.53, 0.6, curv) * smooth(0.1, 0.5, nrm[..., 2])
    col = mix(col, HAIR_SHINE, bevel * 0.55)
    # A broken shine band round the crown, a few strokes long.
    band = (1 - smooth(0.014, 0.03, np.abs(height - 0.15 - (strand - 0.5) * 0.04))) * smooth(0.4, 0.6, strand) * smooth(0.0, 0.4, nrm[..., 2] - 0.1)
    col = mix(col, HAIR_SHINE, band * 0.7)
    # The crevice line between clumps: cocoa, soft.
    col = mix(col, HAIR_LINE, smooth(0.47, 0.43, curv) * 0.75)
    col = mix(col, HAIR_LINE, (1 - smooth(0.25, 0.55, ao)) * 0.7)
    img[hair] = col[hair]
    return img


# ---- Pebble ------------------------------------------------------------------------
P_SKIN = dict(shade=c(0.53, 0.61, 0.55), mid=c(0.68, 0.78, 0.5), lit=c(0.8, 0.88, 0.6))
BEAK = c(0.84, 0.85, 0.6)
SCALE_LINE = c(0.5, 0.6, 0.42)
PETAL_C = dict(shade=c(0.82, 0.8, 0.9), mid=c(0.96, 0.94, 0.9), lit=c(1.0, 0.99, 0.96))
HEART_C = c(0.97, 0.8, 0.33)
LEAF_C = c(0.55, 0.68, 0.38)


def pebble(part, pos, nrm, curv, ao):
    img = np.zeros(part.shape + (3,))
    skin = part == SKIN
    col = paint_skin(pos, nrm, ao, P_SKIN, 3)
    # Soft scales on the crown and the back of the head: big friendly cells, a faint
    # line round each and a slightly paler middle.
    edge, cid = voronoi_edge(pos, 0.11)
    crown = smooth(-0.02, 0.12, pos[..., 2] + 0.6 * pos[..., 1])
    col = mix(col, SCALE_LINE, (1 - smooth(0.03, 0.12, edge)) * crown * 0.3)
    col = col * (1 + ((cid % 7) / 6 - 0.5) * 0.05 * crown)[..., None]
    # The beak: the snout's front paler and warmer, with a crisp edge.
    beak = smooth(0.012, 0.0, np.hypot((pos[..., 0]) / 0.1, (pos[..., 2] + 0.1) / 0.055) - 1) * smooth(0.0, 0.1, -pos[..., 1] - 0.12)
    col = mix(col, BEAK, beak * 0.6)
    # A pale painted highlight on the upper forehead, as on the turnaround.
    d = np.hypot((pos[..., 0] + 0.08) / 0.065, (pos[..., 2] - 0.14) / 0.04)
    col = mix(col, P_SKIN["lit"] * 1.08, (1 - smooth(0.8, 1.0, d)) * smooth(0.0, 0.3, -nrm[..., 1]) * 0.8)
    # Cheek blush, softer on the green.
    for side in (-1, 1):
        d = np.hypot((pos[..., 0] - side * 0.13) / 0.045, (pos[..., 2] + 0.035) / 0.028)
        col = mix(col, c(0.95, 0.66, 0.6), 0.45 * (1 - smooth(0.6, 1.0, d)) * smooth(0.0, 0.3, -nrm[..., 1]))
    col = col * (0.7 + 0.3 * ao ** 1.2)[..., None]
    col = mix(col, SCALE_LINE * 0.8, (1 - smooth(0.25, 0.5, ao)) * 0.5)
    img[skin] = col[skin]

    # The daisy: white petals shading lavender towards the heart, a gold heart.
    flower = part == PETAL
    r = np.hypot(pos[..., 0] - 0.035, pos[..., 1])
    pc = tones(smooth(0.015, 0.06, r) * 0.6 + 0.4 * ao + 0.2 * nrm[..., 2], PETAL_C["shade"], PETAL_C["mid"], PETAL_C["lit"], lo=0.45, hi=0.85)
    img[flower] = pc[flower]
    heart = part == HEART
    # A soft warmer centre, no dots.
    hr = np.hypot(pos[..., 0] - 0.035, pos[..., 1])
    hc = mix(c(0.9, 0.6, 0.24), HEART_C, smooth(0.004, 0.014, hr)) * (0.8 + 0.25 * np.clip(nrm[..., 2], 0, 1))[..., None]
    img[heart] = hc[heart]
    leaf = part == LEAF
    lc = LEAF_C * (0.85 + 0.25 * ao)[..., None]
    img[leaf] = lc[leaf]
    return img


# ---- Face atlas ----------------------------------------------------------------------
EYE = c(0.2, 0.13, 0.13)
IRIS = c(0.44, 0.29, 0.24)
WHITE = c(0.99, 0.98, 0.95)
MOUTH = c(0.45, 0.2, 0.2)
TONGUE = c(0.94, 0.55, 0.53)


def seg_dist(x, z, a, b):
    ax, az = a
    bx, bz = b
    vx, vz = bx - ax, bz - az
    t = np.clip(((x - ax) * vx + (z - az) * vz) / (vx * vx + vz * vz), 0, 1)
    return np.hypot(x - ax - t * vx, z - az - t * vz)


def curve_dist(x, z, xc, zc, half, bend):
    """Distance to the arc z = zc + bend * ((x - xc) / half)^2, |x - xc| <= half."""
    t = np.linspace(-1, 1, 41)
    px, pz = xc + t * half, zc + bend * t * t
    d = np.full(x.shape, 9.0)
    for i in range(40):
        d = np.minimum(d, seg_dist(x, z, (px[i], pz[i]), (px[i + 1], pz[i + 1])))
    return d


def face_cell(expr, kind, n=1024):
    """One expression painted at n x n, in the face frame's metres."""
    xs = FACE_X[0] + (np.arange(n) + 0.5) / n * (FACE_X[1] - FACE_X[0])
    zs = FACE_Z[1] - (np.arange(n) + 0.5) / n * (FACE_Z[1] - FACE_Z[0])
    x, z = np.meshgrid(xs, zs)
    rgba = np.zeros((n, n, 4))
    rgba[..., :3] = EYE

    def put(mask, colour):
        m = np.asarray(mask, float)
        rgba[..., :3] = mix(rgba[..., :3], colour, m)
        rgba[..., 3] = np.maximum(rgba[..., 3], m)

    pebble = kind == "pebble"
    brow = c(0.36, 0.45, 0.27) if pebble else c(0.45, 0.27, 0.2)
    ez = 0.004 if pebble else 0.0
    ex, rx, rz = (0.098, 0.029, 0.037)
    mz = -0.094 if pebble else -0.074
    for side in (-1, 1):
        cx = side * ex
        dx, dz = x - cx, z - ez
        if expr in (0, 3):
            inside = (dx / rx) ** 2 + (dz / rz) ** 2 < 1
            if expr == 3:
                # Half-lidded: the lid comes down flat over the top half, slightly drooped.
                lid = ez + 0.004 + side * dx * 0.08
                inside &= z < lid
            put(inside, EYE)
            iris = inside & ((dx / rx) ** 2 + ((dz + rz * 0.55) / (rz * 0.7)) ** 2 < 1)
            put(iris, IRIS)
            if expr == 3:
                put(seg_dist(x, z, (cx - rx * 1.05, ez + 0.004 - side * rx * 0.084), (cx + rx * 1.05, ez + 0.004 + side * rx * 0.084)) < 0.0042, EYE)
                put(np.hypot(dx + 0.008, dz + 0.01) < 0.006, WHITE)
            else:
                put(np.hypot(dx + 0.009, dz - 0.013) < 0.0105, WHITE)
                put(np.hypot(dx - 0.01, dz + 0.017) < 0.0048, WHITE)
        elif expr == 1:
            # Closed: a soft downward arc with a little lash at the outer end.
            put(curve_dist(x, z, cx, ez - 0.006, rx * 1.05, 0.009) < 0.0042, EYE)
            put(seg_dist(x, z, (cx + side * rx * 1.0, ez + 0.003), (cx + side * rx * 1.45, ez + 0.009)) < 0.0032, EYE)
        else:
            # Happy: ^ ^.
            put(curve_dist(x, z, cx, ez + 0.012, rx * 1.0, -0.022) < 0.005, EYE)
        # Brows: short soft strokes, raised when happy, drooping when sleepy.
        bz = ez + 0.058 + (0.01 if expr == 2 else -0.006 if expr == 3 else 0.0)
        tilt = -0.006 if expr == 3 else 0.004
        put(seg_dist(x, z, (cx - side * 0.018, bz - tilt), (cx + side * 0.02, bz + tilt)) < 0.0042, brow)
    # Mouth.
    w = 0.03 if pebble else 0.02
    if expr == 2:
        # An open smile: a D shape, the tongue at the bottom.
        open_ = (z < mz + 0.006) & (((x) / (w * 1.15)) ** 2 + ((z - mz - 0.006) / 0.026) ** 2 < 1)
        put(open_, MOUTH)
        put(open_ & ((x / (w * 0.7)) ** 2 + ((z - mz + 0.024) / 0.012) ** 2 < 1), TONGUE)
    elif expr == 3:
        put(((x / 0.008) ** 2 + ((z - mz) / 0.0065) ** 2) < 1, MOUTH)
    else:
        put(curve_dist(x, z, 0.0, mz, w, 0.008) < 0.0033, MOUTH)
        if pebble:
            # The beak's corners tuck up.
            for side in (-1, 1):
                put(seg_dist(x, z, (side * w, mz + 0.008), (side * (w + 0.006), mz + 0.013)) < 0.0028, MOUTH)
    return rgba


def face_atlas(kind):
    atlas = np.zeros((SIZE, SIZE, 4))
    for e in range(4):
        cell = face_cell(e, kind)
        cell = np.asarray(Image.fromarray((cell * 255 + 0.5).astype(np.uint8), "RGBA").resize((SIZE // 2, SIZE // 2), Image.LANCZOS), float) / 255
        r, col = divmod(e, 2)
        atlas[r * 512:(r + 1) * 512, col * 512:(col + 1) * 512] = cell
    return Image.fromarray((np.clip(atlas, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA")


def main(bake, out):
    os.makedirs(out, exist_ok=True)
    for name, painter in (("bramble", bramble), ("pebble", pebble)):
        part, pos, nrm, curv, ao = load(bake, name)
        img = painter(part, pos, nrm, curv, ao)
        Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(out, f"{name}_head.png"))
        face_atlas(name).save(os.path.join(out, f"{name}_face.png"))
        print("painted", name)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

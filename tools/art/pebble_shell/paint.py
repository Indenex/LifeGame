"""Pebble's shell, step 2 of 2 (art-rnd-1): paint the albedo, as a painter would over
the baked AO, in the Golden Skull way: each plate in three tones (a lit centre, a mid
and a shade band at its edge), a dark line in every seam, a pale bevel on the upper
edges and a darker one on the lower, growth rings following each plate's outline,
loose strokes along them, and the AO laid over the top.

    python3 tools/art/pebble_shell/paint.py <bake dir> <out dir>

Reads <bake dir>/bake_ao.png and shell_normal.png. Writes <out>/shell_scutes.png
(look style 1), <out>/shell_worn.png (style 0, an older shell worn smooth) and a
cleaned <out>/shell_normal.png. The shell colour in the texture is ChibiFigure.Shells[0];
the shader tints the other shell colours from it.
"""
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geom  # noqa: E402

SIZE = 1024
SS = 2                      # supersampling
N = SIZE * SS


def c(*rgb):
    return np.array(rgb, float)


# The palette, after the Golden Skull tiles: warm browns, ochre lights, shade leaning
# violet, seams nearly black-brown.
PLATE = dict(lit=c(0.66, 0.5, 0.31), mid=c(0.49, 0.36, 0.23), shade=c(0.34, 0.24, 0.18), seam=c(0.17, 0.11, 0.12), bevel=c(0.8, 0.67, 0.47))
MARG = dict(lit=c(0.74, 0.6, 0.39), mid=c(0.6, 0.46, 0.29), shade=c(0.42, 0.3, 0.21), seam=c(0.17, 0.11, 0.12), bevel=c(0.86, 0.75, 0.55))
PLAS = dict(lit=c(0.96, 0.9, 0.7), mid=c(0.9, 0.82, 0.58), shade=c(0.75, 0.63, 0.43), seam=c(0.5, 0.38, 0.27), bevel=c(1.0, 0.96, 0.82))
INNER = c(0.3, 0.21, 0.17)
EDGE = c(0.78, 0.64, 0.43)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def value_noise(x, y, seed):
    """Smooth 2D value noise, about one feature per unit."""
    rng = np.random.default_rng(seed)
    grid = rng.random((257, 257))
    xi, yi = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - xi, y - yi
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    xi, yi = xi % 256, yi % 256
    a, b = grid[yi, xi], grid[yi, xi + 1]
    cc, d = grid[yi + 1, xi], grid[yi + 1, xi + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (cc * (1 - fx) + d * fx) * fy


def paint_plates(pal, dist, maxd, direc, region, worn, uvx, uvy, rings=(0.3, 0.46, 0.62, 0.78)):
    """Shared plate painting. dist: m to the nearest seam; maxd: that plate's largest
    dist; direc: unit direction towards the seam (y up)."""
    f = dist / np.maximum(maxd, 1e-6)
    wob = (value_noise(uvx * 60, uvy * 60, 3) - 0.5) * 0.08
    contrast = 0.45 if worn else 1.0
    centre = 0.62
    col = pal["mid"][None] * np.ones_like(dist)[:, None]
    # Three tones with crisp painted edges: shade band at the edge, lit centre.
    col = col + (pal["lit"] - pal["mid"])[None] * (smooth(centre - 0.02, centre + 0.02, f + wob) * contrast)[:, None]
    col = col + (pal["shade"] - pal["mid"])[None] * ((1 - smooth(0.13, 0.17, f + wob * 0.5)) * contrast)[:, None]
    # Bevel: the upper edge of each plate catches the light, the lower edge falls in shade.
    up = direc[:, 1]
    band = 1 - smooth(0.006, 0.011, dist)
    hi = band * smooth(0.25, 0.6, up) * (0.4 if worn else 0.75)
    lo = band * smooth(0.25, 0.6, -up) * (0.2 if worn else 0.35)
    col = col + (pal["bevel"] - col) * hi[:, None]
    col = col * (1 - lo)[:, None]
    # Growth rings following the plate's outline, a little wobbly.
    if not worn:
        for k, at in enumerate(rings):
            line = 1 - smooth(0.0, 0.0022, np.abs(dist - maxd * at - wob * 0.02))
            col = col * (1 - 0.16 * line * (1 - 0.15 * k))[:, None]
    # Strokes along the rings: colour varies across them, little along them.
    stroke = value_noise(dist * 900 + uvx * 3, uvy * 3 + region * 7.3, 5) - 0.5
    col = col * (1 + stroke * (0.05 if worn else 0.08))[:, None]
    # Each plate a touch different.
    # Each plate a touch lighter or darker, a little warmer or cooler.
    rng = np.random.default_rng(11)
    value, warm = rng.normal(0, 0.035, 64), rng.normal(0, 0.012, 64)
    col = col * (1 + value[region % 64])[:, None] + np.stack([warm, warm * 0.3, -warm], -1)[region % 64]
    # The seam: a dark painted line.
    seam_w = 0.0028 if worn else 0.0034
    line = 1 - smooth(seam_w * 0.6, seam_w, dist)
    seam = pal["seam"] if not worn else pal["seam"] * 0.45 + pal["mid"] * 0.55
    col = col + (seam - col) * line[:, None]
    return col


def main(bake_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    ao = np.asarray(Image.open(os.path.join(bake_dir, "bake_ao.png")).convert("L").resize((N, N), Image.BILINEAR), float) / 255
    jj, ii = np.meshgrid(np.arange(N), np.arange(N))
    uvx = (jj + 0.5) / N
    uvy = 1 - (ii + 0.5) / N
    results = {}
    for worn in (False, True):
        img = np.zeros((N, N, 3))
        # Flat patches: the inside of the shell and the cut edge of its wall.
        for centre, colour in ((geom.INNER_UV, INNER), (geom.EDGE_UV, EDGE)):
            m = (np.abs(uvx - centre[0]) < 0.03) & (np.abs(uvy - centre[1]) < 0.03)
            img[m] = colour
        # Carapace.
        L = np.stack([(uvx - geom.CARA_UV[0]) / geom.K_CARA, (uvy - geom.CARA_UV[1]) / geom.K_CARA], -1)
        a = np.linalg.norm(L, axis=-1)
        box = a < geom._ARC[:, -1].max() * 1.03
        idx = np.nonzero(box)
        th = np.arctan2(L[idx][:, 1], L[idx][:, 0])
        rho = np.empty(len(th))
        for s in range(0, len(th), 20000):
            rho[s:s + 20000] = geom.rho_from_arc(a[idx][s:s + 20000], th[s:s + 20000])
        # Paint a little past the rim, so filtering at the edge stays shell coloured.
        region, dist, direc, _ = geom.scutes(np.minimum(rho, 1.0), th)
        maxd = np.zeros(64)
        np.maximum.at(maxd, region, np.where(rho <= 1, dist, 0))
        marg = region >= 13
        col = np.empty((len(th), 3))
        for sel, pal in ((~marg, PLATE), (marg, MARG)):
            col[sel] = paint_plates(pal, dist[sel], maxd[region[sel]], direc[sel], region[sel], worn, uvx[idx][sel], uvy[idx][sel])
        # Painted light: the top of the dome a touch warmer and lighter.
        col = col * (1 + 0.07 * (1 - np.minimum(rho, 1) ** 2))[:, None]
        # The AO over everything, then the rim's underside a little darker.
        col = col * (0.6 + 0.4 * ao[idx] ** 1.4)[:, None]
        col = col * (1 - 0.18 * smooth(0.93, 1.0, rho))[:, None]
        img[idx] = col
        # Plastron.
        loc = np.stack([(uvx - geom.PLAS_UV[0]) / geom.PLAS_K, (uvy - geom.PLAS_UV[1]) / geom.PLAS_K], -1)
        thp = np.arctan2(loc[..., 1] / geom.PH, loc[..., 0] / geom.PW)
        rp = np.hypot(loc[..., 0] / geom.PW, loc[..., 1] / geom.PH) / geom.plas_outline(thp)
        pm = rp < 1.06
        pidx = np.nonzero(pm)
        x, yo = loc[pidx][:, 0], loc[pidx][:, 1]
        sd, plate = geom.plas_seams(x, yo)
        edge = (1 - np.minimum(rp[pidx], 1)) * 0.12
        pdist = np.minimum(sd, edge + 0.004)
        # Towards the nearest seam: across for the middle line, up or down for the others.
        bow = 0.02 * (x / geom.PW) ** 2
        ys = geom.PSEAMS
        near = np.abs(yo[:, None] + bow[:, None] - ys).argmin(-1)
        pdir = np.stack([np.zeros_like(x), np.sign(ys[near] - yo - bow)], -1)
        pdir = np.where((np.abs(x) < np.abs(yo + bow - ys[near]))[:, None], np.stack([-np.sign(x), np.zeros_like(x)], -1), pdir)
        pregion = 40 + plate * 2 + (x > 0)
        pmax = np.zeros(64)
        np.maximum.at(pmax, pregion, pdist)
        pcol = paint_plates(PLAS, pdist, pmax[pregion], pdir, pregion, worn, uvx[pidx], uvy[pidx], rings=(0.45,))
        pcol = pcol * (0.6 + 0.4 * ao[pidx] ** 1.4)[:, None]
        img[pidx] = pcol
        out = Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).resize((SIZE, SIZE), Image.LANCZOS)
        name = "shell_worn.png" if worn else "shell_scutes.png"
        out.save(os.path.join(out_dir, name))
        results[name] = out
        print("painted", name)
    # The normal map: flat on the patches (their UVs have no area, so the bake left noise).
    nm = np.asarray(Image.open(os.path.join(bake_dir, "shell_normal.png")).convert("RGB")).copy()
    u = (np.arange(SIZE) + 0.5) / SIZE
    for centre in (geom.INNER_UV, geom.EDGE_UV):
        cols = np.abs(u - centre[0]) < 0.03
        rows = np.abs((1 - u) - centre[1]) < 0.03
        nm[np.ix_(rows, cols)] = (128, 128, 255)
    Image.fromarray(nm).save(os.path.join(out_dir, "shell_normal.png"))
    print("cleaned shell_normal.png")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

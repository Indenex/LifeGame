"""The sheep, step 2 of 2 (art-rnd-1): paint the two textures from the bakes, in the
Golden Skull way (D-313: cutesy, relaxing, a hint of realism).

    python3 -I tools/art/sheep/paint.py <bake dir> <out dir>

Wool: every puff is painted as its own little round cloud: a lit cap towards the light
(top left front), a mid tone and a violet-grey shade where it turns away, a soft
violet-cocoa line in the crevices between puffs (from the AO), and a curl stroke
spiralling out from each puff's crown. Skin: a soft dark cocoa face and legs with a
gentle lit top, a pale cream muzzle with a little nose and smile, big dark eyes with two
catchlights, pink cheeks and inner ears, darker hooves with a lit rim, and the pink
shorn body. Reads <tex>_{ao.png,pos,nrm,id.npy} and layout.json; writes wool.png, skin.png.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)
lay = json.load(open(os.path.join(src, "layout.json")))
LIGHT = np.array([-0.45, 0.85, 0.4])
LIGHT /= np.linalg.norm(LIGHT)


def c(*rgb):
    return np.array(rgb, float)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def noise3(p, scale, seed):
    """Cheap smooth-ish 3D noise from a few sines (enough to wobble painted edges)."""
    rng = np.random.default_rng(seed)
    v = np.zeros(len(p))
    for _ in range(4):
        d = rng.normal(size=3)
        d /= np.linalg.norm(d)
        v += np.sin(p @ d * scale * (0.7 + rng.random()) + rng.random() * 6.28)
    return v / 4


def load(tex):
    # Blurred: the bake's sample noise would read as pitting (Justin: no grain, no grit).
    ao = np.asarray(Image.open(os.path.join(src, f"{tex}_ao.png")).convert("L").filter(ImageFilter.GaussianBlur(4)), float) / 255
    pos = np.load(os.path.join(src, f"{tex}_pos.npy"))[..., :3]
    nrm = np.load(os.path.join(src, f"{tex}_nrm.npy"))[..., :3]
    pid = np.rint(np.load(os.path.join(src, f"{tex}_id.npy"))[..., 0] * 16).astype(int)
    # Blender (x, y, z up) -> Godot (x, z, -y).
    g = lambda a: np.stack([a[..., 0], a[..., 2], -a[..., 1]], -1)
    nrm = g(nrm)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=-1, keepdims=True), 1e-6)
    return ao.reshape(-1), g(pos).reshape(-1, 3), nrm.reshape(-1, 3), pid.reshape(-1)


def tone(pal, lit, shade):
    col = np.ones((len(lit), 1)) * pal["mid"]
    col += (pal["lit"] - pal["mid"]) * lit[:, None]
    col += (pal["shade"] - col) * shade[:, None]
    return col


def save(col, name):
    img = np.clip(col.reshape(1024, 1024, 3), 0, 1)
    Image.fromarray((img * 255 + 0.5).astype(np.uint8)).save(os.path.join(dst, name), optimize=True)
    print("painted", name)


# ---------------------------------------------------------------- wool
WOOL = dict(lit=c(0.95, 0.92, 0.84), mid=c(0.86, 0.81, 0.73), shade=c(0.71, 0.66, 0.72), line=c(0.55, 0.43, 0.38), curl=c(0.78, 0.72, 0.68))
ao, p, n, pid = load("wool")
col = np.ones((len(ao), 3)) * WOOL["mid"]
for which, key in ((1, "puffs"), (2, "tuft")):
    m = pid == which
    if not m.any():
        continue
    P = np.array(lay[key])
    cen, rad = P[:, :3], P[:, 3]
    q = p[m]
    # The puff each texel belongs to: the one whose surface it is nearest to (or inside).
    dist = np.linalg.norm(q[:, None, :] - cen[None], axis=-1) - rad[None]
    k = np.argmin(dist, 1)
    d = q - cen[k]
    d /= np.maximum(np.linalg.norm(d, axis=-1, keepdims=True), 1e-6)
    wob = noise3(q, 14, 3) * 0.05
    s = 0.6 * (d @ LIGHT) + 0.4 * (n[m] @ LIGHT) + wob
    a = ao[m]
    lit = smooth(0.4, 0.46, s)
    shade = 1 - smooth(-0.1, -0.04, s - (0.75 - a) * 0.8)
    pc = tone(WOOL, lit, shade * 0.9)
    # Curl stroke: a spiral out from each puff's crown (its outward-facing pole).
    crown = cen[k] - np.array(lay["center"] if which == 1 else lay["head"])
    crown /= np.maximum(np.linalg.norm(crown, axis=-1, keepdims=True), 1e-6)
    e1 = np.cross(crown, np.array([0.3, 1.0, 0.2]))
    e1 /= np.maximum(np.linalg.norm(e1, axis=-1, keepdims=True), 1e-6)
    e2 = np.cross(crown, e1)
    rho = np.arccos(np.clip(np.sum(d * crown, -1), -1, 1))
    th = np.arctan2(np.sum(d * e2, -1), np.sum(d * e1, -1)) / (2 * np.pi) + k * 0.37
    ph = (rho * 1.6 - th) % 1.0
    stroke = (1 - smooth(0.02, 0.11, np.abs(ph - 0.5))) * smooth(0.12, 0.3, rho) * (1 - smooth(0.9, 1.25, rho))
    curl = np.where(lit[:, None] > 0.5, WOOL["mid"], WOOL["curl"])
    pc += (curl - pc) * (stroke * 0.55)[:, None]
    # A pale lit lip on the upper edge of each puff, where it turns down towards the light.
    lip = smooth(0.6, 0.75, s) * smooth(0.1, 0.3, rho)
    pc += (WOOL["lit"] * 1.02 - pc) * (lip * 0.5)[:, None]
    # Soft cocoa line in the crevices, and the AO laid lightly over.
    line = 1 - smooth(0.4, 0.66, a)
    pc += (WOOL["line"] - pc) * (line * 0.75)[:, None]
    pc *= (0.86 + 0.14 * a)[:, None]
    col[m] = pc
save(col, "wool.png")

# ---------------------------------------------------------------- skin
FACE = dict(lit=c(0.27, 0.21, 0.2), mid=c(0.19, 0.15, 0.15), shade=c(0.13, 0.11, 0.14), line=c(0.08, 0.06, 0.08))
MUZ = dict(lit=c(0.98, 0.9, 0.82), mid=c(0.9, 0.79, 0.72), shade=c(0.76, 0.65, 0.66), line=c(0.5, 0.38, 0.38))
PINK = c(0.95, 0.66, 0.66)
NOSE = c(0.36, 0.22, 0.24)
EYE = c(0.07, 0.05, 0.07)
HOOF = dict(lit=c(0.3, 0.25, 0.25), mid=c(0.16, 0.13, 0.14), shade=c(0.1, 0.08, 0.1))
BODY = dict(lit=c(0.96, 0.8, 0.74), mid=c(0.88, 0.7, 0.66), shade=c(0.74, 0.6, 0.64))

ao, p, n, pid = load("skin")
s = n @ LIGHT
lit = smooth(0.5, 0.56, s)
shade = 1 - smooth(-0.05, 0.01, s - (0.7 - ao))
col = tone(FACE, lit, shade * 0.85)

head = pid == 3
mz = np.array(lay["muzzle"])
mc, mr = mz[:3], mz[3:]
# Muzzle: the front of the head inside a slightly grown muzzle ellipsoid.
md = np.linalg.norm((p - mc) / (mr * 1.12), axis=-1)
muz = head & (md < 1.0) & (p[:, 2] > mc[2] - mr[2] * 0.35)
col[muz] = tone(MUZ, lit[muz], shade[muz] * 0.7)
# Soft edge line where the muzzle meets the face.
edge = head & (np.abs(md - 1.0) < 0.06) & (p[:, 2] > mc[2] - mr[2] * 0.35)
col[edge] += (FACE["mid"] * 1.3 - col[edge]) * 0.35
# Nose and smile on the front of the muzzle.
front = head & (p[:, 2] > mc[2] + mr[2] * 0.4)
x, y = p[:, 0], p[:, 1]
ny = mc[1] + 0.022
nose = front & (((x / 0.024) ** 2 + ((y - ny) / 0.014) ** 2) < 1) & (y > ny - 0.014 + np.abs(x) * 0.25)
col[nose] = NOSE + (MUZ["lit"] - NOSE) * 0.25 * smooth(0.0, 0.01, y[nose] - ny)[:, None]
phil = front & (np.abs(x) < 0.0028) & (y < ny - 0.008) & (y > ny - 0.03)
smile = front & (np.abs(x) < 0.026) & (np.abs(y - (ny - 0.034 + 25 * (np.abs(x) - 0.013) ** 2)) < 0.0028)
col[phil | smile] = NOSE
# Eyes: big dark ovals with a white catchlight up towards the light and a small second one.
for i, e in enumerate(np.array(lay["eyes"])):
    q = p - e
    de = np.sqrt((q[:, 0] / 0.037) ** 2 + (q[:, 1] / 0.044) ** 2 + (q[:, 2] / 0.037) ** 2)
    eye = head & (de < 1.0)
    col[eye] = EYE
    rim = head & (de >= 1.0) & (de < 1.18) & (q[:, 1] > -0.005)
    col[rim] += (FACE["line"] - col[rim]) * 0.6
    side = np.sign(e[0])
    c1 = e + np.array([-side * 0.004, 0.014, 0.012])
    c2 = e + np.array([side * 0.008, -0.012, 0.008])
    col[eye & (np.linalg.norm(p - c1, axis=-1) < 0.011)] = c(0.98, 0.97, 0.95)
    col[eye & (np.linalg.norm(p - c2, axis=-1) < 0.005)] = c(0.85, 0.82, 0.88)
    # Pink cheek below and behind the eye.
    ck = e + np.array([side * 0.012, -0.05, -0.012])
    cheek = head & ~muz & (np.linalg.norm(p - ck, axis=-1) < 0.028)
    col[cheek] += (PINK * 0.62 - col[cheek]) * 0.55
# Inner ears: pink on the ear's front face.
for e in np.array(lay["ears"]):
    de = np.linalg.norm((p - e) / np.array([0.07, 0.03, 0.036]), axis=-1)
    inner = head & (de < 1.05) & (n[:, 2] > 0.25)
    col[inner] = tone(dict(lit=PINK, mid=PINK * 0.88, shade=PINK * 0.7), lit[inner], shade[inner] * 0.6)
# Legs: face colour with a darker hoof and a lit rim on its top edge.
legs = (pid >= 5) & (pid <= 8)
hoof = legs & (p[:, 1] < 0.058)
col[hoof] = tone(HOOF, lit[hoof], shade[hoof])
rim = legs & (np.abs(p[:, 1] - 0.06) < 0.005)
col[rim] += (FACE["lit"] * 1.4 - col[rim]) * 0.6
# Shorn body.
body = pid == 4
col[body] = tone(BODY, lit[body], shade[body])
# Crevice lines and light AO.
line = 1 - smooth(0.3, 0.55, ao)
col += (FACE["line"] - col) * (line * 0.6)[:, None]
col *= (0.88 + 0.12 * ao)[:, None]
save(col, "skin.png")

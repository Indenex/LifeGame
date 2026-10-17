#!/usr/bin/env python3
"""Fast storyboard mockups of 10 player-character concepts (not game code).

Writes SVGs into out/ next to this file; render.sh turns them into PNGs with
headless Chromium. All geometry is in metres, feet at y=0, up is -y.
Painted look: each shape gets a dark violet-shifted rim, a shade band on the
lower right and a soft highlight on the upper left (docs/art-reference).
"""
import math, os

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out")
os.makedirs(OUT, exist_ok=True)


def rgb(h):
    h = h.lstrip("#")
    return [int(h[i:i + 2], 16) for i in (0, 2, 4)]


def hexc(c):
    return "#%02x%02x%02x" % tuple(max(0, min(255, int(v))) for v in c)


def mix(a, b, f):
    a, b = rgb(a), rgb(b)
    return hexc([a[i] + (b[i] - a[i]) * f for i in range(3)])


def dark(c, f=0.32):
    return mix(c, "#4a3f78", f)


def rim(c):
    return mix(c, "#2e2440", 0.55)


def light(c, f=0.4):
    return mix(c, "#fff6dc", f)


class D:
    uid = 0

    def __init__(self):
        self.s = []

    def nid(self):
        D.uid += 1
        return "c%d" % D.uid

    def ell(self, cx, cy, rx, ry, col, rot=0, hl=True, sw=0.012, band=0.14):
        i = self.nid()
        tr = ' transform="rotate(%.1f %.4f %.4f)"' % (rot, cx, cy) if rot else ""
        self.s.append('<g%s><clipPath id="%s"><ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f"/></clipPath>' % (tr, i, cx, cy, rx, ry))
        self.s.append('<ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f" fill="%s" stroke="%s" stroke-width="%.3f"/>' % (cx, cy, rx, ry, dark(col), rim(col), sw))
        self.s.append('<g clip-path="url(#%s)"><ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f" fill="%s"/>' % (i, cx - rx * band, cy - ry * band, rx * 0.96, ry * 0.94, col))
        if hl:
            self.s.append('<ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f" fill="%s" opacity="0.75"/>' % (cx - rx * 0.38, cy - ry * 0.45, rx * 0.28, ry * 0.18, light(col)))
        self.s.append("</g></g>")

    def path(self, d, col, sw=0.012, off=0.025, hl=None):
        i = self.nid()
        self.s.append('<clipPath id="%s"><path d="%s"/></clipPath>' % (i, d))
        self.s.append('<path d="%s" fill="%s" stroke="%s" stroke-width="%.3f" stroke-linejoin="round"/>' % (d, dark(col), rim(col), sw))
        self.s.append('<g clip-path="url(#%s)"><path d="%s" fill="%s" transform="translate(%.3f %.3f)"/>' % (i, d, col, -off, -off * 0.7))
        if hl:
            self.s.append(hl)
        self.s.append("</g>")

    def flat(self, svg):
        self.s.append(svg)

    def eye(self, x, y, r=0.03, squash=1.0, col="#2b2033"):
        self.s.append('<ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f" fill="%s"/>' % (x, y, r * 0.85 * squash, r * 1.1, col))
        self.s.append('<circle cx="%.4f" cy="%.4f" r="%.4f" fill="#fffaf0"/>' % (x - r * 0.3 * squash, y - r * 0.45, r * 0.32))
        self.s.append('<circle cx="%.4f" cy="%.4f" r="%.4f" fill="#fffaf0" opacity="0.8"/>' % (x + r * 0.25 * squash, y + r * 0.4, r * 0.14))

    def cheek(self, x, y, r=0.022, col="#f08a86"):
        self.s.append('<ellipse cx="%.4f" cy="%.4f" rx="%.4f" ry="%.4f" fill="%s" opacity="0.55"/>' % (x, y, r * 1.3, r * 0.75, col))

    def mouth(self, x, y, w=0.025, col="#5a2e3a"):
        self.s.append('<path d="M%.4f %.4f Q%.4f %.4f %.4f %.4f" fill="none" stroke="%s" stroke-width="0.009" stroke-linecap="round"/>' % (x - w, y, x, y + w * 0.9, x + w, y, col))

    def svg(self):
        return "".join(self.s)


def P(t, x, depth):
    """Screen x of a feature at lateral offset x and forward depth, turned by t (0 front, 1 = 45 deg)."""
    a = math.radians(45 * t)
    return x * math.cos(a) + depth * math.sin(a)


def face(d, t, cx, cy, r, ex=0.36, ey=0.05, er=None, mouth=True, cheeks=True, my=0.32, eye_col="#2b2033"):
    """Two eyes, cheeks and mouth on a round head at (cx, cy) with radius r."""
    er = er or r * 0.16
    for side in (-1, 1):
        x = cx + P(t, side * r * ex, r * 0.9)
        sq = 1.0 - 0.35 * t * (1 if side < 0 else 0)
        d.eye(x, cy + r * ey, er, sq, eye_col)
        if cheeks:
            d.cheek(cx + P(t, side * r * (ex + 0.18), r * 0.85), cy + r * (ey + 0.28), r * 0.13)
    if mouth:
        d.mouth(cx + P(t, 0, r * 0.95), cy + r * my, r * 0.1)


def legs(d, t, x, w, h, col, foot, gap=0.06, footw=None):
    footw = footw or w * 0.9
    for side in (-1, 1):
        lx = x + P(t, side * gap, 0)
        d.ell(lx, -h * 0.55, w * 0.5, h * 0.55, col, hl=False)
        d.ell(lx + 0.012 * t, -footw * 0.45, footw, footw * 0.5, foot, hl=False)


# ---------------------------------------------------------------- concepts
# Each returns (svg, height_m). x=0 is the character's centre.

def bramble(t):  # chibi human
    d = D()
    H = 1.10
    hr = 0.21
    legs(d, t, 0, 0.09, 0.26, "#6a5a4c", "#7b4e33", gap=0.055)
    # satchel strap behind, then body
    d.ell(P(t, -0.13, -0.02), -0.37, 0.07, 0.08, "#8b5e3c")
    d.path("M-0.15 -0.22 Q-0.17 -0.50 0 -0.52 Q0.17 -0.50 0.15 -0.22 Z", "#6f8f4e")  # tunic
    d.flat('<path d="M-0.15 -0.24 L0.15 -0.24" stroke="#4d3a2c" stroke-width="0.03"/>')
    for side in (-1, 1):
        d.ell(P(t, side * 0.16, 0.02), -0.37, 0.045, 0.1, "#f2c9a0", rot=side * 18, hl=False)
    d.flat('<path d="M-0.11 -0.49 L0.12 -0.27" stroke="#8b5e3c" stroke-width="0.022"/>')
    hy = -0.52 - hr * 0.95
    d.ell(0, hy, hr * 1.05, hr, "#f2c9a0")
    # hair: messy chestnut tuft + fringe
    hx = P(t, 0, 0.03)
    d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
        hx - 0.225, hy + 0.02, hx - 0.24, hy - 0.26, hx, hy - 0.235, hx + 0.24, hy - 0.26, hx + 0.225, hy + 0.0,
        hx + 0.10, hy - 0.12, hx + 0.03, hy - 0.06, hx - 0.12, hy - 0.10, hx - 0.225, hy + 0.02), "#9a5a34")
    d.path("M%.3f %.3f q0.03 -0.09 0.08 -0.07 q-0.02 0.04 -0.03 0.08 Z" % (hx - 0.01, hy - 0.22), "#9a5a34")
    face(d, t, 0, hy + 0.03, hr, ey=0.1)
    return d.svg(), H


def pip(t):  # gnome / hearthfolk
    d = D()
    H = 0.80
    legs(d, t, 0, 0.08, 0.13, "#5b4e6e", "#6b4228", gap=0.05)
    d.ell(0, -0.25, 0.15, 0.14, "#5e7fae")  # round coat
    for side in (-1, 1):
        d.ell(P(t, side * 0.15, 0.02), -0.25, 0.04, 0.075, "#5e7fae", rot=side * 20, hl=False)
        d.ell(P(t, side * 0.17, 0.03), -0.18, 0.03, 0.03, "#f0c4a0", hl=False)
    hy = -0.45
    d.ell(0, hy, 0.15, 0.13, "#f0c4a0")
    # fluffy beard
    bx = P(t, 0, 0.12)
    d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
        bx - 0.12, hy + 0.02, bx - 0.13, hy + 0.17, bx, hy + 0.23, bx + 0.13, hy + 0.17, bx + 0.12, hy + 0.02,
        bx, hy + 0.08, bx - 0.12, hy + 0.02), "#f4ede0", off=0.02)
    # tall floppy hat
    hx = P(t, 0, 0.0)
    d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
        hx - 0.17, hy - 0.03, hx - 0.07, hy - 0.30, hx + 0.13, hy - 0.36, hx + 0.03, hy - 0.22, hx + 0.17, hy - 0.03,
        hx, hy - 0.07, hx - 0.17, hy - 0.03), "#c8553d")
    d.ell(hx - 0.17 + 0.34, hy - 0.03, 0.02, 0.02, "#c8553d", hl=False) if False else None
    d.ell(hx + 0.135, hy - 0.36, 0.03, 0.03, "#e7c46a", hl=False)  # pompom
    d.ell(P(t, 0, 0.15), hy + 0.035, 0.035, 0.03, "#f2a98e", hl=False)  # big nose
    for side in (-1, 1):
        d.eye(P(t, side * 0.06, 0.13), hy - 0.01, 0.022, 1 - 0.3 * t * (side < 0))
        d.cheek(P(t, side * 0.085, 0.12), hy + 0.035, 0.02)
    return d.svg(), H


def kit(t):  # fox-folk
    d = D()
    H = 0.95
    # tail behind
    tx = P(t, -0.05, -0.2)
    d.path("M%.3f -0.24 Q%.3f -0.42 %.3f -0.52 Q%.3f -0.30 %.3f -0.14 Z" % (tx, tx - 0.34, tx - 0.17, tx + 0.04, tx + 0.04), "#e8894a")
    d.path("M%.3f -0.47 Q%.3f -0.53 %.3f -0.52 Q%.3f -0.49 %.3f -0.43 Z" % (tx - 0.22, tx - 0.20, tx - 0.17, tx - 0.15, tx - 0.20), "#f6e6c8", off=0.01)
    legs(d, t, 0, 0.075, 0.18, "#c96e3a", "#4a3530", gap=0.05)
    d.ell(0, -0.29, 0.13, 0.14, "#e8894a")
    d.ell(P(t, 0, 0.06), -0.27, 0.07, 0.1, "#f6e6c8", hl=False)
    for side in (-1, 1):
        d.ell(P(t, side * 0.13, 0.02), -0.29, 0.035, 0.08, "#c96e3a", rot=side * 15, hl=False)
    # scarf
    d.path("M%.3f -0.40 Q%.3f -0.36 %.3f -0.40 L%.3f -0.30 L%.3f -0.31 Z" % (P(t, -0.12, .05), P(t, 0, .1), P(t, 0.12, .05), P(t, 0.10, .1), P(t, 0.05, .1)), "#6f9fb4", off=0.012)
    hy = -0.56
    for side in (-1, 1):  # ears
        ex = P(t, side * 0.11, -0.01)
        d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (ex - 0.06, hy - 0.08, ex + side * 0.03, hy - 0.30, ex + 0.06, hy - 0.08), "#e8894a")
        d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (ex - 0.03, hy - 0.10, ex + side * 0.02, hy - 0.24, ex + 0.03, hy - 0.10), "#4a3530", off=0.005)
    d.ell(0, hy, 0.17, 0.15, "#e8894a")
    mx = P(t, 0, 0.13)
    d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (mx - 0.12, hy + 0.02, mx, hy + 0.20, mx + 0.12, hy + 0.02, mx, hy + 0.08, mx - 0.12, hy + 0.02), "#f6e6c8", off=0.01)
    d.ell(P(t, 0, 0.17), hy + 0.06, 0.025, 0.018, "#3a2a2e", hl=False)
    for side in (-1, 1):
        d.eye(P(t, side * 0.075, 0.14), hy - 0.01, 0.026, 1 - 0.3 * t * (side < 0))
    return d.svg(), H


def clover(t):  # rabbit-folk
    d = D()
    H = 1.0
    legs(d, t, 0, 0.09, 0.12, "#e9dccb", "#d8c7b2", gap=0.055, footw=0.07)
    d.ell(0, -0.25, 0.15, 0.15, "#e9dccb")
    # overalls
    d.path("M-0.14 -0.22 Q0 -0.06 0.14 -0.22 L0.11 -0.32 L-0.11 -0.32 Z", "#d9a648", off=0.015)
    for side in (-1, 1):
        d.flat('<path d="M%.3f -0.32 L%.3f -0.39" stroke="#b07f2e" stroke-width="0.018"/>' % (side * 0.08, side * 0.07))
        d.ell(P(t, side * 0.15, 0.02), -0.26, 0.035, 0.065, "#e9dccb", rot=side * 20, hl=False)
    hy = -0.50
    for side in (-1, 1):  # long ears, one flopped
        ex = P(t, side * 0.07, -0.02)
        rot = -8 if side < 0 else 55
        cy = hy - 0.24 if side < 0 else hy - 0.17
        cx = ex if side < 0 else ex + 0.08
        d.ell(cx, cy, 0.05, 0.16, "#e9dccb", rot=rot)
        d.ell(cx, cy + 0.01, 0.025, 0.11, "#f4b8b0", rot=rot, hl=False)
    d.ell(0, hy, 0.16, 0.14, "#e9dccb")
    face(d, t, 0, hy, 0.16, ey=0.12, my=0.42)
    d.ell(P(t, 0, 0.15), hy + 0.045, 0.016, 0.012, "#e48a96", hl=False)
    return d.svg(), H


def mossle(t):  # mole-folk
    d = D()
    H = 0.75
    legs(d, t, 0, 0.08, 0.08, "#6e5755", "#f2a0a8", gap=0.06, footw=0.06)
    d.ell(0, -0.30, 0.21, 0.24, "#8a6f6a")  # pear body = head
    d.ell(P(t, 0, 0.12), -0.22, 0.12, 0.12, "#a88e86", hl=False)  # belly
    for side in (-1, 1):  # big digging paws
        px = P(t, side * 0.2, 0.04)
        d.ell(px, -0.24, 0.06, 0.045, "#f2a0a8", rot=side * 30)
        for k in (-1, 0, 1):
            d.flat('<path d="M%.3f %.3f l%.3f 0.03" stroke="#f6efe4" stroke-width="0.012" stroke-linecap="round"/>' % (px + side * 0.04 + k * 0.012, -0.23 + k * 0.015, side * 0.02))
    # miner cap + lamp
    d.path("M-0.19 -0.43 Q0 -0.62 0.19 -0.43 Q0 -0.47 -0.19 -0.43 Z", "#e0b84f")
    lx = P(t, 0, 0.17)
    d.ell(lx, -0.52, 0.035, 0.03, "#fff0b0")
    d.flat('<ellipse cx="%.3f" cy="-0.52" rx="0.09" ry="0.07" fill="#ffe9a0" opacity="0.25"/>' % lx)
    # snout + tiny squint eyes
    sx = P(t, 0, 0.2)
    d.ell(sx, -0.33, 0.05, 0.035, "#f2a0a8")
    for side in (-1, 1):
        ex = P(t, side * 0.07, 0.18)
        d.flat('<path d="M%.3f -0.39 q0.015 -0.015 0.03 0" stroke="#2b2033" stroke-width="0.012" fill="none" stroke-linecap="round"/>' % (ex - 0.015))
        d.cheek(P(t, side * 0.11, 0.16), -0.35, 0.02)
    return d.svg(), H


def puddle(t):  # frog-folk
    d = D()
    H = 0.72
    for side in (-1, 1):  # splayed feet
        fx = P(t, side * 0.08, 0.02)
        d.ell(fx + side * 0.03, -0.025, 0.07, 0.03, "#7fae4c", hl=False)
    d.ell(0, -0.21, 0.17, 0.17, "#9cc760")
    d.ell(P(t, 0, 0.1), -0.17, 0.11, 0.11, "#e8eaa0", hl=False)
    for side in (-1, 1):
        d.ell(P(t, side * 0.16, 0.03), -0.2, 0.035, 0.07, "#9cc760", rot=side * 25, hl=False)
    # wide head with eye bumps
    hy = -0.45
    for side in (-1, 1):
        d.ell(P(t, side * 0.1, 0.06), hy - 0.09, 0.075, 0.07, "#9cc760")
    d.ell(0, hy, 0.22, 0.13, "#9cc760")
    for side in (-1, 1):
        ex = P(t, side * 0.1, 0.1)
        d.ell(ex, hy - 0.095, 0.05, 0.05, "#fffaf0", hl=False)
        d.eye(ex, hy - 0.09, 0.032, 1 - 0.3 * t * (side < 0))
        d.cheek(P(t, side * 0.15, 0.12), hy + 0.04, 0.025)
    mx = P(t, 0, 0.12)
    d.flat('<path d="M%.3f %.3f Q%.3f %.3f %.3f %.3f" stroke="#3d5a26" stroke-width="0.011" fill="none" stroke-linecap="round"/>' % (mx - 0.11, hy + 0.03, mx, hy + 0.09, mx + 0.11, hy + 0.03))
    # lily-pad hat with flower
    d.ell(P(t, 0, -0.02), hy - 0.13, 0.17, 0.035, "#5f9a4a")
    d.ell(P(t, 0.03, 0), hy - 0.17, 0.035, 0.03, "#f5a6c0")
    return d.svg(), H


def capling(t):  # mushroom sprite
    d = D()
    H = 0.82
    legs(d, t, 0, 0.07, 0.09, "#e9d6b4", "#8b5e3c", gap=0.05, footw=0.055)
    d.path("M-0.12 -0.08 Q-0.15 -0.38 -0.09 -0.46 L0.09 -0.46 Q0.15 -0.38 0.12 -0.08 Q0 -0.04 -0.12 -0.08 Z", "#f1e2c4")
    for side in (-1, 1):
        d.ell(P(t, side * 0.13, 0.02), -0.22, 0.03, 0.06, "#f1e2c4", rot=side * 20, hl=False)
    face(d, t, 0, -0.34, 0.12, ey=0.0, my=0.3)
    # gill frill + big cap
    d.ell(0, -0.48, 0.24, 0.05, "#e7cfa8", hl=False)
    d.path("M-0.32 -0.47 Q-0.30 -0.80 0 -0.82 Q0.30 -0.80 0.32 -0.47 Q0 -0.52 -0.32 -0.47 Z", "#d9604a", off=0.04)
    for (x, y, r) in ((-0.17, -0.60, 0.04), (0.05, -0.70, 0.05), (0.19, -0.56, 0.03), (-0.04, -0.57, 0.025)):
        d.ell(x + 0.05 * t, y, r, r * 0.8, "#f8ecd6", hl=False, sw=0.006)
    return d.svg(), H


def sprout(t):  # seedling sprite
    d = D()
    H = 0.72
    legs(d, t, 0, 0.06, 0.08, "#7fae4c", "#7a5a3a", gap=0.045, footw=0.05)
    # leaf skirt
    for k in (-2, -1, 0, 1, 2):
        d.ell(k * 0.055, -0.12, 0.05, 0.07, "#6fa04a" if k % 2 else "#86b85a", rot=k * 18, hl=False)
    d.ell(0, -0.32, 0.17, 0.2, "#c9e39a")  # bean body/head
    for side in (-1, 1):
        d.ell(P(t, side * 0.17, 0.03), -0.24, 0.04, 0.025, "#9fcc72", rot=side * -25, hl=False)  # leaf hands
    face(d, t, 0, -0.33, 0.17, ey=0.0, my=0.32)
    # stalk + two leaves on top
    sx = P(t, 0, 0.0)
    d.flat('<path d="M%.3f -0.51 Q%.3f -0.60 %.3f -0.64" stroke="#5f8f3a" stroke-width="0.016" fill="none" stroke-linecap="round"/>' % (sx, sx - 0.01, sx + 0.01))
    d.ell(sx - 0.07, -0.66, 0.08, 0.035, "#8fcf6a", rot=-20)
    d.ell(sx + 0.08, -0.68, 0.07, 0.03, "#8fcf6a", rot=25)
    return d.svg(), H


def dumpling(t):  # blob creature
    d = D()
    H = 0.62
    bx = P(t, -0.04, -0.2)
    d.path("M%.3f -0.42 L%.3f -0.42 L%.3f -0.18 L%.3f -0.18 Z" % (bx - 0.12, bx + 0.06, bx + 0.08, bx - 0.14), "#b5784a")  # backpack peeking
    d.flat('<path d="M%.3f -0.44 Q%.3f -0.55 %.3f -0.44" stroke="#8b5e3c" stroke-width="0.02" fill="none"/>' % (bx - 0.10, bx - 0.03, bx + 0.04))
    d.path("M-0.27 -0.02 Q-0.32 -0.44 -0.05 -0.52 Q0.02 -0.62 0.06 -0.52 Q0.31 -0.44 0.27 -0.02 Q0 0.02 -0.27 -0.02 Z", "#c8b6e8", off=0.05)
    d.flat('<ellipse cx="-0.13" cy="-0.38" rx="0.07" ry="0.04" fill="#f3ecff" opacity="0.7"/>')
    for side in (-1, 1):
        d.ell(P(t, side * 0.27, 0.03), -0.15, 0.04, 0.03, "#c8b6e8", hl=False)  # nubby arms
    face(d, t, 0, -0.24, 0.22, ex=0.4, ey=0.0, my=0.25)
    return d.svg(), H


def hearthling(t):  # wild: clay golem with an ember heart
    d = D()
    H = 0.9
    legs(d, t, 0, 0.1, 0.12, "#a9704a", "#8e5c3d", gap=0.07, footw=0.08)
    d.path("M-0.20 -0.10 L-0.23 -0.46 Q0 -0.52 0.23 -0.46 L0.20 -0.10 Q0 -0.06 -0.20 -0.10 Z", "#c98a5e", off=0.04)  # jar-like body
    ex = P(t, 0, 0.2)
    d.flat('<ellipse cx="%.3f" cy="-0.28" rx="0.11" ry="0.10" fill="#ffb04a" opacity="0.3"/>' % ex)
    d.path("M%.3f -0.33 L%.3f -0.27 L%.3f -0.30 L%.3f -0.22 L%.3f -0.28 L%.3f -0.25 Z" % (ex - 0.03, ex + 0.01, ex + 0.04, ex + 0.0, ex - 0.04, ex - 0.02), "#ffb04a", off=0.005)
    for side in (-1, 1):
        d.ell(P(t, side * 0.25, 0.02), -0.30, 0.055, 0.11, "#b67a50", rot=side * 12)
    # moss shoulders & rim lip
    d.ell(0, -0.47, 0.24, 0.045, "#d29a6c", hl=False)
    hy = -0.64
    d.ell(0, hy, 0.17, 0.14, "#c98a5e")
    d.path("M-0.18 %.3f Q-0.12 %.3f 0 %.3f Q0.13 %.3f 0.18 %.3f Q0.1 %.3f 0 %.3f Q-0.1 %.3f -0.18 %.3f Z" % (
        hy - 0.03, hy - 0.2, hy - 0.16, hy - 0.2, hy - 0.04, hy - 0.08, hy - 0.06, hy - 0.08, hy - 0.03), "#7fa34a", off=0.015)
    d.ell(P(t, 0.06, 0.02), hy - 0.15, 0.025, 0.02, "#f6e6c8", hl=False)  # tiny flower
    for side in (-1, 1):  # glowing eye holes
        x = P(t, side * 0.06, 0.14)
        d.flat('<ellipse cx="%.3f" cy="%.3f" rx="%.3f" ry="0.03" fill="#4a2f2a"/><ellipse cx="%.3f" cy="%.3f" rx="0.012" ry="0.016" fill="#ffd27a"/>' % (x, hy + 0.01, 0.024 * (1 - 0.3 * t * (side < 0)), x, hy + 0.01))
    d.flat('<path d="M%.3f %.3f l0.03 0" stroke="#4a2f2a" stroke-width="0.01" stroke-linecap="round"/>' % (P(t, 0, .15) - 0.015, hy + 0.07))
    d.flat('<path d="M0.08 -0.2 l0.03 0.04 l-0.02 0.03" stroke="#8e5c3d" stroke-width="0.007" fill="none"/>')  # crack
    return d.svg(), H


def current(t):  # the current tall carved player, plain grey-blue
    d = D()
    H = 1.8
    for side in (-1, 1):
        d.path("M%.3f 0 L%.3f -0.86 L%.3f -0.86 L%.3f 0 Z" % (side * 0.05 - 0.06, side * 0.08 - 0.07, side * 0.08 + 0.07, side * 0.05 + 0.06), "#7a6f66")
    d.path("M-0.2 -0.84 L-0.23 -1.42 L0.23 -1.42 L0.2 -0.84 Z", "#8b8f9a")
    for side in (-1, 1):
        d.path("M%.3f -1.40 L%.3f -0.86 L%.3f -0.86 L%.3f -1.40 Z" % (side * 0.24 - 0.04, side * 0.27 - 0.04, side * 0.27 + 0.04, side * 0.24 + 0.04), "#d9b08c")
    d.path("M-0.06 -1.42 L-0.05 -1.48 L0.05 -1.48 L0.06 -1.42 Z", "#d9b08c")
    d.path("M-0.11 -1.48 L-0.12 -1.70 L-0.06 -1.80 L0.06 -1.80 L0.12 -1.70 L0.11 -1.48 Z", "#d9b08c")
    d.path("M-0.12 -1.66 L-0.12 -1.76 L-0.06 -1.81 L0.07 -1.81 L0.12 -1.74 L0.12 -1.66 Z", "#5e4430", off=0.01)
    return d.svg(), H


CONCEPTS = [
    ("kit", "Kit", "Fox-folk", kit),
    ("capling", "Capling", "Mushroom sprite", capling),
    ("bramble", "Bramble", "Chibi human", bramble),
    ("clover", "Clover", "Rabbit-folk", clover),
    ("pip", "Pip", "Gnome / hearthfolk", pip),
    ("mossle", "Mossle", "Mole-folk", mossle),
    ("puddle", "Puddle", "Frog-folk", puddle),
    ("sprout", "Sprout", "Seedling sprite", sprout),
    ("dumpling", "Dumpling", "Blob creature", dumpling),
    ("hearthling", "Hearthling", "Wild: clay golem", hearthling),
]

FONT = "font-family='Georgia, serif'"
BG = """<defs><linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#f6ecd6"/><stop offset="1" stop-color="#e9dcc0"/></linearGradient></defs>"""


def ground(w, y):
    s = '<rect x="0" y="%d" width="%d" height="200" fill="#a9bf5a"/>' % (y, w)
    s += '<rect x="0" y="%d" width="%d" height="8" fill="#c5d66e"/>' % (y, w)
    import random
    r = random.Random(4)
    for _ in range(w // 9):
        x = r.uniform(0, w)
        h = r.uniform(6, 16)
        s += '<path d="M%.1f %d q2 -%.1f 5 -%.1f q-1 %.1f 3 %.1f" fill="#86a548"/>' % (x, y + 4, h, h, h, h)
    return s


def fence(x, y, S):
    w = 0.12 * S
    return ('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" rx="3" fill="#7b5a3e" stroke="#3f2c26" stroke-width="2"/>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" fill="#94704d"/>'
            '<path d="M%.1f %.1f l%.1f -%.1f l%.1f %.1f Z" fill="#7b5a3e" stroke="#3f2c26" stroke-width="2"/>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="10" fill="#6a4c34" stroke="#3f2c26" stroke-width="1.5"/>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="10" fill="#6a4c34" stroke="#3f2c26" stroke-width="1.5"/>') % (
        x - w / 2, y - S, w, S, x - w / 2 + 3, y - S + 4, w * 0.35, S - 8, x - w / 2, y - S, w / 2, w * 0.3, w / 2, w * 0.3,
        x - w / 2 - 40, y - S * 0.75, w + 80, x - w / 2 - 40, y - S * 0.4, w + 80)


def ruler(x, y, S, top):
    s = ""
    for m in range(0, int(top * 10) + 1, 1):
        yy = y - m * 0.1 * S
        ln = 10 if m % 5 == 0 else 5
        s += '<path d="M%.1f %.1f l%d 0" stroke="#7a6c5a" stroke-width="1"/>' % (x, yy, ln)
        if m % 5 == 0:
            s += "<text x='%.1f' y='%.1f' font-size='11' fill='#7a6c5a' %s>%.1f m</text>" % (x + 13, yy + 4, FONT, m / 10)
    return s


def char(fn, t, x, y, S):
    svg, _ = fn(t)
    return '<g transform="translate(%.1f %.1f) scale(%.2f)">%s</g>' % (x, y, S, svg)


def write(name, w, h, body):
    with open(os.path.join(OUT, name + ".svg"), "w") as f:
        f.write('<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">%s<rect width="100%%" height="100%%" fill="url(#sky)"/>%s</svg>' % (w, h, w, h, BG, body))


def main():
    # individual sheets: fence, front, three-quarter
    for key, name, kind, fn in CONCEPTS:
        S = 300
        W, Hh, gy = 760, 470, 400
        _, H = fn(0)
        b = ground(W, gy)
        b += fence(90, gy, S)
        b += char(fn, 0, 300, gy, S) + char(fn, 1, 560, gy, S)
        b += "<text x='24' y='38' font-size='28' fill='#3f2c26' %s>%s <tspan font-size='18' fill='#7a5f4a'>· %s · %.2f m</tspan></text>" % (FONT, name, kind, H)
        b += "<text x='300' y='452' font-size='14' fill='#3f2c26' text-anchor='middle' %s>front</text>" % FONT
        b += "<text x='560' y='452' font-size='14' fill='#3f2c26' text-anchor='middle' %s>three-quarter</text>" % FONT
        b += "<text x='90' y='452' font-size='14' fill='#3f2c26' text-anchor='middle' %s>1 m fence post</text>" % FONT
        write(key, W, Hh, b)

    # lineup: fence, current player, then the 10 in recommendation order
    S = 170
    slot = 150
    W = 90 + slot * 12
    Hh, gy = 440, 370
    b = ground(W, gy)
    b += ruler(14, gy, S, 1.9)
    b += fence(105, gy, S)
    b += "<text x='105' y='%d' font-size='13' fill='#3f2c26' text-anchor='middle' %s>fence 1 m</text>" % (gy + 40, FONT)
    b += char(current, 0, 105 + slot, gy, S)
    b += "<text x='%d' y='%d' font-size='13' fill='#3f2c26' text-anchor='middle' %s>current 1.8 m</text>" % (105 + slot, gy + 40, FONT)
    for i, (key, name, kind, fn) in enumerate(CONCEPTS):
        x = 105 + slot * (i + 2)
        _, H = fn(0)
        b += char(fn, 0.5, x, gy, S)
        b += "<text x='%d' y='%d' font-size='15' fill='#3f2c26' text-anchor='middle' %s>%d. %s</text>" % (x, gy + 36, FONT, i + 1, name)
        b += "<text x='%d' y='%d' font-size='11' fill='#5a4636' text-anchor='middle' %s>%s · %.2f m</text>" % (x, gy + 54, FONT, kind, H)
    b += "<text x='%d' y='30' font-size='24' fill='#3f2c26' %s>LifeGame character concepts · lineup at one scale</text>" % (60, FONT)
    write("lineup", W, Hh, b)
    print("ok")


if __name__ == "__main__":
    main()

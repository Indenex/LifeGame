#!/usr/bin/env python3
"""Round 2 storyboard: 10 new concepts plus 4 round-1 redraws, all as part-swaps on ONE shared base.

Shared base = one 16-bone skeleton, one round chibi body (0.84 m to the head top), and the same
animations. Concepts differ only by slots: skin/colour, pattern, ears, face (muzzle/beak),
head-top (hat/cap/crest), back, tail, outfit and a small front add-on.
"""
import os
from make_concepts import (D, P, face, legs, dark, rim, light, mix, ground, fence, ruler, write, FONT, OUT)

HY, HR = -0.66, 0.2           # head centre and radius (shared)
BASE_H = 0.84                 # head top of the bare base


def base(t, sp):
    """Draw the shared base body with this concept's slots filled in. sp is a dict of slot values."""
    d = D()
    skin = sp.get("skin", "#d8c9b4")
    for slot in ("back", "tail"):
        if slot in sp:
            sp[slot](d, t)
    legs(d, t, 0, 0.085, 0.16, sp.get("leg", skin), sp.get("feet", dark(skin, 0.45)), gap=0.06, footw=0.07)
    d.ell(0, -0.30, 0.15, 0.15, sp.get("body", skin))
    if "belly" in sp:
        d.ell(P(t, 0, 0.08), -0.28, 0.09, 0.1, sp["belly"], hl=False)
    out = sp.get("outfit")
    if out:
        kind, col = out
        if kind == "tunic":
            d.path("M-0.15 -0.20 Q-0.17 -0.42 0 -0.44 Q0.17 -0.42 0.15 -0.20 Q0 -0.15 -0.15 -0.20 Z", col)
        elif kind == "overalls":
            d.path("M-0.14 -0.22 Q0 -0.10 0.14 -0.22 L0.11 -0.34 L-0.11 -0.34 Z", col, off=0.015)
            for s in (-1, 1):
                d.flat('<path d="M%.3f -0.34 L%.3f -0.42" stroke="%s" stroke-width="0.018"/>' % (s * 0.08, s * 0.07, dark(col)))
        elif kind == "vest":
            d.path("M-0.14 -0.22 Q-0.16 -0.40 -0.05 -0.44 L0 -0.30 L0.05 -0.44 Q0.16 -0.40 0.14 -0.22 Q0 -0.17 -0.14 -0.22 Z", col)
    arm = sp.get("arm", skin)
    for s in (-1, 1):
        d.ell(P(t, s * 0.155, 0.02), -0.33, 0.042, 0.085, arm, rot=s * 18, hl=False)
        d.ell(P(t, s * 0.178, 0.04), -0.255, 0.034, 0.032, sp.get("hand", arm), hl=False)
    if "ears" in sp:
        sp["ears"](d, t)
    d.ell(0, HY, HR, HR * 0.9, sp.get("head", skin))
    if "pattern" in sp:
        sp["pattern"](d, t)
    if sp.get("face", True):
        face(d, t, 0, HY + 0.02, HR, ey=0.1, er=sp.get("er"), mouth=sp.get("mouth", True), cheeks=sp.get("cheeks", True), my=0.42)
    for slot in ("muzzle", "top", "front"):
        if slot in sp:
            sp[slot](d, t)
    return d.svg(), sp.get("h", BASE_H)


# ------------------------------------------------------------ part library (reusable slot pieces)

def ears_tri(col, inner, size=0.2, spread=0.13):
    def f(d, t):
        for s in (-1, 1):
            ex = P(t, s * spread, -0.01)
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (ex - 0.06, HY - 0.08, ex + s * 0.03, HY - 0.08 - size, ex + 0.06, HY - 0.08), col)
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (ex - 0.03, HY - 0.10, ex + s * 0.02, HY - 0.06 - size * 0.8, ex + 0.03, HY - 0.10), inner, off=0.005)
    return f


def ears_round(col, inner, r=0.06, spread=0.15, y=-0.8):
    def f(d, t):
        for s in (-1, 1):
            x = P(t, s * spread, -0.02)
            d.ell(x, y, r, r, col, hl=False)
            d.ell(x, y + 0.008, r * 0.55, r * 0.55, inner, hl=False)
    return f


def ears_long(col, inner):
    def f(d, t):
        for s in (-1, 1):
            ex = P(t, s * 0.07, -0.02)
            rot, cy, cx = (-8, HY - 0.25, ex) if s < 0 else (55, HY - 0.18, ex + 0.08)
            d.ell(cx, cy, 0.05, 0.16, col, rot=rot)
            d.ell(cx, cy + 0.01, 0.025, 0.11, inner, rot=rot, hl=False)
    return f


def muzzle(col, nose, w=0.08, h=0.055, y=-0.6, whiskers=False):
    def f(d, t):
        x = P(t, 0, 0.18)
        d.ell(x, y, w, h, col, hl=False)
        d.ell(x, y - h * 0.45, 0.022, 0.016, nose, hl=False)
        if whiskers:
            for s in (-1, 1):
                for k in (-1, 1):
                    d.flat('<path d="M%.3f %.3f l%.3f %.3f" stroke="%s" stroke-width="0.005" stroke-linecap="round"/>' % (x + s * w * 0.8, y, s * 0.06, k * 0.012, rim(col)))
    return f


def tail_path(col, tip=None, shape="fox"):
    def f(d, t):
        tx = P(t, -0.05, -0.2)
        if shape == "fox":
            d.path("M%.3f -0.20 Q%.3f -0.38 %.3f -0.48 Q%.3f -0.26 %.3f -0.12 Z" % (tx, tx - 0.32, tx - 0.17, tx + 0.04, tx + 0.04), col)
            if tip:
                d.path("M%.3f -0.44 Q%.3f -0.49 %.3f -0.48 Q%.3f -0.45 %.3f -0.39 Z" % (tx - 0.22, tx - 0.20, tx - 0.17, tx - 0.15, tx - 0.20), tip, off=0.01)
        elif shape == "curl":
            d.flat('<path d="M%.3f -0.14 Q%.3f -0.10 %.3f -0.30 Q%.3f -0.44 %.3f -0.40" stroke="%s" stroke-width="0.05" fill="none" stroke-linecap="round"/>' % (tx, tx - 0.22, tx - 0.20, tx - 0.15, tx - 0.11, rim(col)))
            d.flat('<path d="M%.3f -0.14 Q%.3f -0.10 %.3f -0.30 Q%.3f -0.44 %.3f -0.40" stroke="%s" stroke-width="0.032" fill="none" stroke-linecap="round"/>' % (tx, tx - 0.22, tx - 0.20, tx - 0.15, tx - 0.11, col))
        elif shape == "otter":
            d.path("M%.3f -0.18 Q%.3f -0.14 %.3f -0.02 Q%.3f -0.06 %.3f -0.10 Z" % (tx + 0.04, tx - 0.14, tx - 0.30, tx - 0.12, tx + 0.02), col)
    return f


def scarf(col):
    def f(d, t):
        d.path("M%.3f -0.47 Q%.3f -0.42 %.3f -0.47 L%.3f -0.36 L%.3f -0.37 Z" % (P(t, -0.13, .05), P(t, 0, .1), P(t, 0.13, .05), P(t, 0.11, .12), P(t, 0.05, .12)), col, off=0.012)
    return f


def shell_back(col, rimc):
    def f(d, t):
        x = P(t, 0, -0.12)
        d.path("M%.3f -0.14 Q%.3f -0.56 %.3f -0.52 Q%.3f -0.56 %.3f -0.14 Z" % (x - 0.21, x - 0.2, x, x + 0.2, x + 0.21), col, off=0.03)
        d.flat('<path d="M%.3f -0.15 L%.3f -0.15" stroke="%s" stroke-width="0.025"/>' % (x - 0.21, x + 0.21, rimc))
    return f


# ------------------------------------------------------------ concepts

def mitten(t):  # cat-folk
    def stripes(d, t):
        for k in (-1, 0, 1):
            x = P(t, k * 0.05, 0.12)
            d.flat('<path d="M%.3f %.3f l0 0.05" stroke="#6e6c86" stroke-width="0.016" stroke-linecap="round"/>' % (x, HY - 0.17))
    def bell(d, t):
        d.flat('<path d="M%.3f -0.47 Q%.3f -0.43 %.3f -0.47" stroke="#c8553d" stroke-width="0.022" fill="none"/>' % (P(t, -0.11, .06), P(t, 0, .14), P(t, 0.11, .06)))
        d.ell(P(t, 0, 0.15), -0.43, 0.022, 0.022, "#e7c46a", hl=False)
    return base(t, dict(skin="#a3a4bd", belly="#f3ead8", ears=ears_tri("#a3a4bd", "#f4b8b0", 0.13, 0.12), pattern=stripes,
                        muzzle=muzzle("#f3ead8", "#e48a96", 0.07, 0.045, -0.595, True), tail=tail_path("#a3a4bd", shape="curl"),
                        front=bell, feet="#f3ead8", hand="#f3ead8", mouth=False, h=0.94))


def honey(t):  # bear cub
    def pot(d, t):
        x = P(t, -0.03, -0.18)
        d.ell(x - 0.12, -0.36, 0.08, 0.09, "#c98a5e")
        d.ell(x - 0.12, -0.44, 0.06, 0.018, "#e7b34a", hl=False)
    return base(t, dict(skin="#a8774e", belly="#e9cfa2", ears=ears_round("#a8774e", "#e9cfa2"), back=pot,
                        muzzle=muzzle("#e9cfa2", "#3a2a2e", 0.08, 0.055, -0.59), front=scarf("#c8553d"), mouth=False, h=0.87))


def tuft(t):  # owl
    def tufts(d, t):
        for s in (-1, 1):
            x = P(t, s * 0.14, 0)
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - s * 0.05, HY - 0.12, x + s * 0.07, HY - 0.27, x + s * 0.06, HY - 0.08), "#7a5a40")
    def discs(d, t):
        for s in (-1, 1):
            d.ell(HR * 0.0 + P(t, s * 0.075, 0.17), HY + 0.04, 0.075, 0.07, "#f2e4c8", hl=False)
    def beak(d, t):
        x = P(t, 0, 0.2)
        d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - 0.022, HY + 0.07, x, HY + 0.12, x + 0.022, HY + 0.07), "#e7a84a", off=0.005)
    def chevrons(d, t):
        for k in range(3):
            x, y = P(t, 0, 0.13), -0.32 + k * 0.05
            d.flat('<path d="M%.3f %.3f l0.025 0.015 l0.025 -0.015" stroke="#a88a68" stroke-width="0.008" fill="none"/>' % (x - 0.025, y))
    return base(t, dict(skin="#9a7656", belly="#efe0c4", arm="#7a5a40", ears=tufts, pattern=discs, muzzle=beak, front=chevrons,
                        er=0.045, mouth=False, cheeks=False, feet="#e7a84a", h=1.0))


def quill(t):  # hedgehog
    def spikes(d, t):
        cx = P(t, 0, -0.05)
        pts = []
        n = 11
        for i in range(n + 1):
            a = 3.14159 * (1.05 - 1.1 * i / n)
            r = 0.27 if i % 2 else 0.2
            pts.append((cx + r * __import__("math").cos(a) * 1.05, HY - 0.02 - r * __import__("math").sin(a)))
        dd = "M" + " L".join("%.3f %.3f" % p for p in pts) + " Q%.3f %.3f %.3f %.3f Z" % (cx, HY - 0.06, pts[0][0], pts[0][1])
        d.path(dd, "#7a5d48", off=0.03)
    def nose(d, t):
        d.ell(P(t, 0, 0.21), HY + 0.06, 0.025, 0.02, "#3a2a2e", hl=False)
    def back(d, t):
        x = P(t, 0, -0.15)
        d.path("M%.3f -0.18 L%.3f -0.30 L%.3f -0.32 L%.3f -0.42 L%.3f -0.40 L%.3f -0.48 L%.3f -0.30 Z" % (x - 0.15, x - 0.22, x - 0.17, x - 0.22, x - 0.14, x - 0.12, x), "#6a5040")
    return base(t, dict(skin="#e7c9a0", belly="#f6e6c8", back=back, top=spikes, muzzle=nose, outfit=("vest", "#6f8f4e"), feet="#8b5e3c", h=0.93))


def nutkin(t):  # acorn person
    def cap(d, t):
        x = P(t, 0, -0.01)
        d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.225, HY - 0.04, x - 0.21, HY - 0.25, x, HY - 0.25, x + 0.21, HY - 0.25, x + 0.225, HY - 0.04, x, HY - 0.1, x - 0.225, HY - 0.04), "#8b5e3c", off=0.03)
        for k in range(-3, 4):
            d.flat('<path d="M%.3f %.3f l0.02 0.03 l0.02 -0.03" stroke="#6a4630" stroke-width="0.007" fill="none"/>' % (x + k * 0.05 - 0.02, HY - 0.17 + abs(k) * 0.012))
        d.path("M%.3f %.3f q0.01 -0.07 0.05 -0.08 q-0.02 0.04 -0.02 0.08 Z" % (x - 0.01, HY - 0.24), "#6a4630", off=0.005)
    def freckles(d, t):
        for s in (-1, 1):
            for k in range(3):
                d.flat('<circle cx="%.3f" cy="%.3f" r="0.006" fill="#b07a48"/>' % (P(t, s * (0.1 + k * 0.018), 0.15), HY + 0.06 + (k % 2) * 0.012))
    return base(t, dict(skin="#e0b07a", outfit=("tunic", "#7fa34a"), top=cap, pattern=freckles, feet="#6a4630", h=0.99))


def nimbus(t):  # cloud sprite
    def cloud(d, t):
        x = P(t, 0, -0.02)
        for (dx, dy, r) in ((-0.17, -0.06, 0.07), (0.17, -0.07, 0.07), (-0.1, -0.16, 0.08), (0.09, -0.17, 0.085), (0, -0.21, 0.08)):
            d.ell(x + dx, HY + dy, r, r * 0.9, "#fbf8f2")
        d.path("M%.3f %.3f l0.012 0.025 l0.027 0.002 l-0.02 0.017 l0.007 0.027 l-0.024 -0.014 l-0.023 0.014 l0.006 -0.027 l-0.02 -0.017 l0.027 -0.002 Z" % (x + 0.13, HY - 0.15), "#f2cf5a", off=0.003)
    def puff(d, t):
        for s in (-1, 0, 1):
            d.ell(P(t, s * 0.07, 0.08), -0.24 + abs(s) * 0.02, 0.06, 0.05, "#fbf8f2", hl=False)
    return base(t, dict(skin="#cfe0f2", top=cloud, front=puff, feet="#fbf8f2", hand="#fbf8f2", h=0.95))


def wick(t):  # lantern folk
    def flame(d, t):
        x = P(t, 0, 0)
        d.flat('<ellipse cx="%.3f" cy="%.3f" rx="0.16" ry="0.16" fill="#ffd27a" opacity="0.28"/>' % (x, HY - 0.24))
        d.ell(x, HY - 0.15, 0.1, 0.03, "#b8873e", hl=False)
        d.flat('<path d="M%.3f %.3f L%.3f %.3f" stroke="#4a3530" stroke-width="0.008"/>' % (x, HY - 0.17, x, HY - 0.21))
        d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (x - 0.035, HY - 0.21, x - 0.03, HY - 0.28, x, HY - 0.33, x + 0.035, HY - 0.26, x + 0.035, HY - 0.21), "#ffb04a", off=0.008)
        d.ell(x, HY - 0.235, 0.014, 0.022, "#fff3c0", hl=False)
    def drips(d, t):
        for dx in (-0.12, 0.05, 0.14):
            d.flat('<path d="M%.3f %.3f q0.01 0.05 0 0.06 q-0.012 -0.01 0 -0.06" fill="#fff6e6"/>' % (P(t, dx, 0.12), HY - 0.15))
    return base(t, dict(skin="#f1e3c8", top=flame, pattern=drips, outfit=("vest", "#5e6aa0"), feet="#b8873e", h=1.05))


def dusk(t):  # moth
    def wings(d, t):
        x = P(t, 0, -0.12)
        for s in (-1, 1):
            d.ell(x + s * 0.2, -0.43, 0.13, 0.09, "#c8b6e8", rot=s * -30)
            d.ell(x + s * 0.17, -0.25, 0.09, 0.065, "#b5a2da", rot=s * 25)
            d.ell(x + s * 0.24, -0.45, 0.03, 0.03, "#f2cf5a", hl=False)
    def antennae(d, t):
        for s in (-1, 1):
            x = P(t, s * 0.06, 0.05)
            d.flat('<path d="M%.3f %.3f Q%.3f %.3f %.3f %.3f" stroke="#6e5a80" stroke-width="0.01" fill="none"/>' % (x, HY - 0.16, x + s * 0.04, HY - 0.3, x + s * 0.12, HY - 0.33))
            d.ell(x + s * 0.12, HY - 0.33, 0.045, 0.022, "#e7dcc8", rot=s * -20)
    def ruff(d, t):
        for k in range(-3, 4):
            d.ell(P(t, k * 0.04, 0.08), -0.45 + abs(k) * 0.008, 0.035, 0.035, "#f6f0e2", hl=False)
    return base(t, dict(skin="#d9cfe8", back=wings, ears=antennae, front=ruff, er=0.04, feet="#8e7aa8", h=1.03))


def ripple(t):  # otter
    def mask(d, t):
        d.ell(P(t, 0, 0.12), HY + 0.06, 0.15, 0.1, "#efe0c8", hl=False)
    def shell(d, t):
        x = P(t, 0, 0.17)
        d.path("M%.3f -0.30 Q%.3f -0.38 %.3f -0.30 Q%.3f -0.24 %.3f -0.30 Z" % (x - 0.05, x, x + 0.05, x, x - 0.05), "#f5a6c0", off=0.008)
        for k in (-1, 0, 1):
            d.flat('<path d="M%.3f -0.33 L%.3f -0.27" stroke="#c97a98" stroke-width="0.005"/>' % (x + k * 0.02, x))
    def nose(d, t):
        x = P(t, 0, 0.2)
        d.ell(x, HY + 0.07, 0.028, 0.018, "#3a2a2e", hl=False)
        for s in (-1, 1):
            for k in (0, 1):
                d.flat('<circle cx="%.3f" cy="%.3f" r="0.005" fill="#8a6248"/>' % (x + s * (0.045 + k * 0.015), HY + 0.095))
    return base(t, dict(skin="#8a6248", belly="#efe0c8", ears=ears_round("#8a6248", "#5e4030", 0.035, 0.17, -0.71), pattern=mask, muzzle=nose,
                        tail=tail_path("#7a5440", shape="otter"), front=shell, mouth=False, h=0.86))


def pebble(t):  # tortoise
    def flower(d, t):
        x = P(t, 0.05, 0.02)
        d.flat('<path d="M%.3f %.3f l0.01 -0.05" stroke="#5f8f3a" stroke-width="0.01"/>' % (x, HY - 0.17))
        for k in range(5):
            import math
            a = k * 1.2566
            d.ell(x + 0.01 + 0.025 * math.cos(a), HY - 0.22 + 0.025 * math.sin(a), 0.018, 0.018, "#f6f0e2", hl=False, sw=0.005)
        d.ell(x + 0.01, HY - 0.22, 0.014, 0.014, "#f2cf5a", hl=False, sw=0.005)
    def plate(d, t):
        x = P(t, 0, 0.1)
        d.flat('<path d="M%.3f -0.33 L%.3f -0.33 M%.3f -0.25 L%.3f -0.25 M%.3f -0.38 L%.3f -0.20" stroke="#b8a46a" stroke-width="0.007"/>' % (x - 0.07, x + 0.07, x - 0.07, x + 0.07, x, x))
    return base(t, dict(skin="#a8c27a", belly="#e8d9a0", back=shell_back("#8a6a48", "#c9a46a"), top=flower, front=plate, feet="#8fa866", h=0.91))


# round-1 ideas redrawn on the same base
def kit2(t):
    return base(t, dict(skin="#e8894a", belly="#f6e6c8", ears=ears_tri("#e8894a", "#4a3530"), tail=tail_path("#e8894a", "#f6e6c8"),
                        muzzle=muzzle("#f6e6c8", "#3a2a2e", 0.09, 0.06, -0.595), front=scarf("#6f9fb4"), feet="#4a3530", mouth=False, h=1.04))


def capling2(t):
    def cap(d, t):
        x = P(t, 0, -0.01)
        d.ell(x, HY - 0.05, 0.27, 0.045, "#e7cfa8", hl=False)
        d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.33, HY - 0.04, x - 0.31, HY - 0.34, x, HY - 0.36, x + 0.31, HY - 0.34, x + 0.33, HY - 0.04, x, HY - 0.1, x - 0.33, HY - 0.04), "#d9604a", off=0.04)
        for (dx, dy, r) in ((-0.17, -0.17, 0.04), (0.05, -0.26, 0.05), (0.19, -0.13, 0.03), (-0.04, -0.14, 0.025)):
            d.ell(x + dx, HY + dy, r, r * 0.8, "#f8ecd6", hl=False, sw=0.006)
    return base(t, dict(skin="#f1e2c4", top=cap, feet="#8b5e3c", h=1.02))


def bramble2(t):
    def hair(d, t):
        hx = P(t, 0, 0.03)
        hy = HY
        d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            hx - 0.215, hy + 0.02, hx - 0.23, hy - 0.25, hx, hy - 0.225, hx + 0.23, hy - 0.25, hx + 0.215, hy,
            hx + 0.10, hy - 0.11, hx + 0.03, hy - 0.05, hx - 0.12, hy - 0.09, hx - 0.215, hy + 0.02), "#9a5a34")
        d.path("M%.3f %.3f q0.03 -0.09 0.08 -0.07 q-0.02 0.04 -0.03 0.08 Z" % (hx - 0.01, hy - 0.21), "#9a5a34")
    def strap(d, t):
        d.flat('<path d="M-0.11 -0.42 L0.12 -0.22" stroke="#8b5e3c" stroke-width="0.02"/>')
    return base(t, dict(skin="#f2c9a0", outfit=("tunic", "#6f8f4e"), top=hair, front=strap, leg="#6a5a4c", feet="#7b4e33", arm="#6f8f4e", hand="#f2c9a0", h=0.93))


def clover2(t):
    return base(t, dict(skin="#e9dccb", ears=ears_long("#e9dccb", "#f4b8b0"), outfit=("overalls", "#d9a648"),
                        muzzle=lambda d, t: d.ell(P(t, 0, 0.19), HY + 0.06, 0.016, 0.012, "#e48a96", hl=False), feet="#d8c7b2", h=1.1))


def bare(t):
    return base(t, dict(skin="#d8c9b4"))


NEW = [
    ("mitten", "Mitten", "Cat-folk", mitten),
    ("honey", "Honey", "Bear cub", honey),
    ("tuft", "Tuft", "Owl-folk", tuft),
    ("quill", "Quill", "Hedgehog", quill),
    ("ripple", "Ripple", "Otter", ripple),
    ("nutkin", "Nutkin", "Acorn folk", nutkin),
    ("dusk", "Dusk", "Moth", dusk),
    ("wick", "Wick", "Lantern folk", wick),
    ("nimbus", "Nimbus", "Cloud sprite", nimbus),
    ("pebble", "Pebble", "Tortoise", pebble),
]
OLD = [
    ("kit2", "Kit", "Fox (R1)", kit2),
    ("capling2", "Capling", "Mushroom (R1)", capling2),
    ("bramble2", "Bramble", "Chibi human (R1)", bramble2),
    ("clover2", "Clover", "Rabbit (R1)", clover2),
]


def char(fn, t, x, y, S):
    svg, _ = fn(t)
    return '<g transform="translate(%.1f %.1f) scale(%.2f)">%s</g>' % (x, y, S, svg)


def text(x, y, s, size=14, col="#3f2c26", anchor="middle"):
    return "<text x='%.1f' y='%.1f' font-size='%d' fill='%s' text-anchor='%s' %s>%s</text>" % (x, y, size, col, anchor, FONT, s)


def base_sheet():
    S = 420
    W, H, gy = 1500, 680, 580
    b = ground(W, gy)
    b += ruler(14, gy, S, 1.1)
    # height band
    for hm, lab in ((BASE_H, "base head top 0.84 m"), (1.10, "max with parts 1.10 m")):
        y = gy - hm * S
        b += '<path d="M60 %.1f L880 %.1f" stroke="#c8553d" stroke-width="1.5" stroke-dasharray="8 6"/>' % (y, y)
        b += text(180, y - 7, lab, 13, "#c8553d", "start")
    b += fence(110, gy, S)
    b += text(110, gy + 40, "1 m fence post")
    # bare base + skeleton
    bx = 360
    b += char(bare, 0, bx, gy, S)
    bones = {"Head": (0, -0.66), "Neck": (0, -0.47), "Spine": (0, -0.33), "Hips": (0, -0.17)}
    for s, n in ((-1, "L"), (1, "R")):
        bones["UpperArm" + n] = (s * 0.13, -0.40)
        bones["LowerArm" + n] = (s * 0.165, -0.32)
        bones["Hand" + n] = (s * 0.18, -0.25)
        bones["UpperLeg" + n] = (s * 0.06, -0.16)
        bones["LowerLeg" + n] = (s * 0.06, -0.08)
        bones["Foot" + n] = (s * 0.065, -0.02)
    links = [("Hips", "Spine"), ("Spine", "Neck"), ("Neck", "Head")]
    for n in ("L", "R"):
        links += [("Neck", "UpperArm" + n), ("UpperArm" + n, "LowerArm" + n), ("LowerArm" + n, "Hand" + n),
                  ("Hips", "UpperLeg" + n), ("UpperLeg" + n, "LowerLeg" + n), ("LowerLeg" + n, "Foot" + n)]
    sk = bx + 300
    b += char(bare, 0, sk, gy, S).replace("<g transform", "<g opacity='0.35' transform", 1)
    for a, c in links:
        (x1, y1), (x2, y2) = bones[a], bones[c]
        b += '<path d="M%.1f %.1f L%.1f %.1f" stroke="#3f2c26" stroke-width="3"/>' % (sk + x1 * S, gy + y1 * S, sk + x2 * S, gy + y2 * S)
    for n, (x, y) in bones.items():
        b += '<circle cx="%.1f" cy="%.1f" r="6" fill="#c8553d" stroke="#3f2c26" stroke-width="1.5"/>' % (sk + x * S, gy + y * S)
    b += text(sk, gy + 40, "16-bone rig (Hips, Spine, Neck, Head, 3 per arm, 3 per leg)", 13)
    b += text(bx, gy + 40, "bare base (front)", 13)
    # slot callouts on the bare base
    slots = [("head-top: hat, cap, crest, flame", (0, -0.86)), ("ears: tri, round, long, tufts, antennae", (-0.17, -0.80)),
             ("face: eyes, muzzle, beak, pattern", (0.12, -0.62)), ("front: scarf, collar, ruff, held item", (0.0, -0.46)),
             ("outfit: tunic, vest, overalls", (0.1, -0.30)), ("back: wings, shell, pack (static)", (-0.14, -0.38)),
             ("tail: rides on Hips, no bones", (-0.1, -0.18)), ("colour + pattern: skin, belly, hands, feet", (0.06, -0.06))]
    for i, (lab, (x, y)) in enumerate(slots):
        lx, ly = 920, 140 + i * 48
        b += '<path d="M%.1f %.1f L%.1f %.1f" stroke="#7a5f4a" stroke-width="1.2" stroke-dasharray="3 3"/>' % (sk + x * S, gy + y * S, lx - 8, ly - 5)
        b += '<circle cx="%.1f" cy="%.1f" r="4" fill="#7a5f4a"/>' % (sk + x * S, gy + y * S)
        b += text(lx, ly, lab, 16, "#3f2c26", "start")
    b += text(24, 38, "Shared base: one body, one skeleton, one animation set", 28, anchor="start")
    b += text(24, 64, "Every concept is this body plus swappable parts. Height stays in a 0.84–1.10 m band, and the world can scale the whole base uniformly (e.g. to 1.3 m).", 15, "#7a5f4a", "start")
    write("r2-shared-base", W, H, b)


def main():
    base_sheet()
    for key, name, kind, fn in NEW + OLD:
        S = 300
        W, Hh, gy = 760, 470, 400
        _, H = fn(0)
        b = ground(W, gy)
        b += fence(90, gy, S)
        b += char(fn, 0, 300, gy, S) + char(fn, 1, 560, gy, S)
        b += text(24, 38, "%s <tspan font-size='18' fill='#7a5f4a'>· %s · %.2f m · shared base</tspan>" % (name, kind, H), 28, anchor="start")
        for x, lab in ((300, "front"), (560, "three-quarter"), (90, "1 m fence post")):
            b += text(x, 452, lab)
        write("r2-" + key, W, Hh, b)
    S, slot = 210, 140
    allc = NEW + OLD
    W = 120 + slot * (len(allc) + 2)
    Hh, gy = 420, 330
    b = ground(W, gy)
    b += ruler(14, gy, S, 1.2)
    b += '<path d="M60 %.1f L%d %.1f" stroke="#c8553d" stroke-width="1" stroke-dasharray="6 6"/>' % (gy - 1.1 * S, W, gy - 1.1 * S)
    b += fence(110, gy, S) + text(110, gy + 36, "fence 1 m", 13)
    b += char(bare, 0, 110 + slot, gy, S) + text(110 + slot, gy + 36, "shared base", 13)
    for i, (key, name, kind, fn) in enumerate(allc):
        x = 110 + slot * (i + 2)
        if i == len(NEW):
            b += '<path d="M%.1f 70 L%.1f %.1f" stroke="#a89478" stroke-width="1.5" stroke-dasharray="4 4"/>' % (x - slot / 2, x - slot / 2, gy + 60)
            b += text(x + slot * 1.5 - slot / 2, 82, "round 1, redrawn on the same base", 14, "#7a5f4a")
        b += char(fn, 0.5, x, gy, S)
        lab = "%d. %s" % (i + 1, name) if i < len(NEW) else name
        b += text(x, gy + 34, lab, 15)
        b += text(x, gy + 52, kind, 11, "#5a4636")
    b += text(24, 34, "Round 2: 10 new concepts plus 4 from round 1, all on ONE shared base (same skeleton, body and animations)", 22, anchor="start")
    write("r2-lineup", W, Hh, b)
    print("ok")


if __name__ == "__main__":
    main()

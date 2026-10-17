#!/usr/bin/env python3
"""Round 3 storyboard: less cute, closer to the reference art.

One shared base at 1.35 m with about 4 heads of height (instead of chibi). It uses the same
16-bone rig and swaps parts per concept. Painted look: 2-3 tone bands, soft violet-brown rims,
small eyes, worn travelling gear. Every sheet is paired with a crop of the reference it draws from.
"""
import math
from make_concepts import D, P, dark, rim, light, mix, write, FONT

HY, HRX, HRY = -1.17, 0.13, 0.16      # head centre and radii (the head is about 0.32 m tall)
H = 1.35


def limb(d, x1, y1, x2, y2, w, col):
    """A painted limb: soft rim, shade tone, then the lit tone nudged up-left."""
    d.flat('<path d="M%.3f %.3f L%.3f %.3f" stroke="%s" stroke-width="%.3f" stroke-linecap="round"/>' % (x1, y1, x2, y2, rim(col), w + 0.022))
    d.flat('<path d="M%.3f %.3f L%.3f %.3f" stroke="%s" stroke-width="%.3f" stroke-linecap="round"/>' % (x1, y1, x2, y2, dark(col), w))
    d.flat('<path d="M%.3f %.3f L%.3f %.3f" stroke="%s" stroke-width="%.3f" stroke-linecap="round"/>' % (x1 - w * 0.12, y1 - 0.004, x2 - w * 0.12, y2 - 0.004, col, w * 0.62))


def base(t, sp):
    d = D()
    skin = sp.get("skin", "#d9b48e")
    shirt = sp.get("shirt", "#b8a88a")
    trous = sp.get("trousers", "#6e6058")
    boots = sp.get("boots", "#6a4630")
    if "back" in sp:
        sp["back"](d, t)
    if "tail" in sp:
        sp["tail"](d, t)
    # legs and boots
    for s in (-1, 1):
        x = P(t, s * 0.085, 0)
        limb(d, x, -0.62, x + s * 0.01, -0.12, 0.13, trous)
        d.path("M%.3f -0.20 L%.3f -0.20 L%.3f -0.05 Q%.3f 0 %.3f 0 L%.3f 0 Z" % (x - 0.07, x + 0.07, x + 0.075, x + 0.075 + 0.04 * t, x + 0.1 + 0.04 * t, x - 0.075), boots, off=0.02)
        d.flat('<path d="M%.3f -0.19 L%.3f -0.19" stroke="%s" stroke-width="0.02"/>' % (x - 0.07, x + 0.07, light(boots, 0.2)))
    # torso
    sw = sp.get("shoulders", 0.21)
    d.path("M%.3f -0.98 Q0 -1.03 %.3f -0.98 L%.3f -0.62 Q0 -0.58 %.3f -0.62 Z" % (-sw, sw, 0.16, -0.16), shirt, off=0.04)
    if "outfit" in sp:
        sp["outfit"](d, t)
    d.path("M-0.165 -0.66 L0.165 -0.66 L0.165 -0.61 L-0.165 -0.61 Z", sp.get("belt", "#5a3c2a"), off=0.008)
    bx = P(t, 0, 0.16)
    d.flat('<rect x="%.3f" y="-0.665" width="0.05" height="0.06" rx="0.008" fill="#c9a24e" stroke="#6a4a2a" stroke-width="0.006"/>' % (bx - 0.025))
    if "front" in sp:
        sp["front"](d, t)
    # arms
    arm = sp.get("arm", shirt)
    for s in (-1, 1):
        sx = P(t, s * (sw - 0.02), 0)
        limb(d, sx, -0.95, sx + s * 0.05, -0.78, 0.095, arm)
        limb(d, sx + s * 0.05, -0.78, sx + s * 0.055, -0.62, 0.085, sp.get("forearm", arm))
        d.ell(sx + s * 0.055, -0.58, 0.045, 0.05, sp.get("hand", skin), hl=False)
    if "held" in sp:
        sp["held"](d, t)
    # neck + head
    limb(d, 0, -1.0, 0, -1.04, 0.07, skin)
    if "head_back" in sp:
        sp["head_back"](d, t)
    d.ell(P(t, 0, 0.02), HY, HRX, HRY, sp.get("head", skin), band=0.1)
    if "pattern" in sp:
        sp["pattern"](d, t)
    if sp.get("face", True):
        for s in (-1, 1):
            x = P(t, s * 0.05, 0.12)
            sq = 1 - 0.3 * t * (s < 0)
            d.flat('<ellipse cx="%.3f" cy="%.3f" rx="%.4f" ry="0.016" fill="#2e2430"/>' % (x, HY + 0.005, 0.012 * sq))
            d.flat('<circle cx="%.3f" cy="%.3f" r="0.004" fill="#fff6e6"/>' % (x - 0.004, HY - 0.004))
            d.flat('<path d="M%.3f %.3f l%.3f -0.008" stroke="%s" stroke-width="0.009" stroke-linecap="round"/>' % (x - 0.02 * sq, HY - 0.03, 0.04 * sq, sp.get("brow", "#5a3c2a")))
        if sp.get("nose", True):
            d.flat('<path d="M%.3f %.3f q0.012 0.02 -0.004 0.03" stroke="%s" stroke-width="0.008" fill="none" stroke-linecap="round"/>' % (P(t, 0, 0.14), HY + 0.02, dark(skin, 0.5)))
        if sp.get("mouth", True):
            d.flat('<path d="M%.3f %.3f l0.03 -0.004" stroke="%s" stroke-width="0.008" stroke-linecap="round"/>' % (P(t, -0.012, 0.13), HY + 0.085, dark(skin, 0.6)))
    for slot in ("muzzle", "head_top"):
        if slot in sp:
            sp[slot](d, t)
    return d.svg(), sp.get("h", H)


# ------------------------------------------------------------ concepts

def wayfarer(t):  # human: hooded traveller
    def hood_back(d, t):
        x = P(t, 0, -0.02)
        d.path("M%.3f -1.00 Q%.3f -1.36 %.3f -1.37 Q%.3f -1.36 %.3f -1.00 Z" % (x - 0.19, x - 0.2, x, x + 0.2, x + 0.19), "#5f7444", off=0.03)
    def cloak(d, t):
        x = P(t, 0, -0.05)
        d.path("M%.3f -1.0 Q%.3f -0.6 %.3f -0.36 L%.3f -0.32 L%.3f -0.36 Q%.3f -0.6 %.3f -1.0 Z" % (x - 0.2, x - 0.29, x - 0.27, x, x + 0.27, x + 0.29, x + 0.2), "#5f7444", off=0.05)
    def hood_top(d, t):
        x = P(t, 0, 0.0)
        d.path("M%.3f %.3f Q%.3f -1.38 %.3f -1.36 Q%.3f -1.38 %.3f %.3f Q%.3f -1.26 %.3f %.3f Z" % (
            x - 0.155, HY + 0.03, x - 0.15, x, x + 0.15, x + 0.155, HY + 0.03, x, x - 0.155, HY + 0.03), "#6b7a3a", off=0.025)
        d.path("M%.3f -1.03 Q%.3f -0.95 %.3f -1.03 L%.3f -0.98 Q%.3f -0.9 %.3f -0.98 Z" % (P(t, -0.16, .05), P(t, 0, .1), P(t, 0.16, .05), P(t, 0.16, .05), P(t, 0, .1), P(t, -0.16, .05)), "#6b7a3a", off=0.01)
    def staff(d, t):
        x = P(t, 0.26, 0.05)
        d.flat('<path d="M%.3f -1.42 L%.3f 0" stroke="%s" stroke-width="0.032" stroke-linecap="round"/><path d="M%.3f -1.42 L%.3f 0" stroke="#8a6440" stroke-width="0.018" stroke-linecap="round"/>' % (x, x + 0.02, rim("#8a6440"), x - 0.005, x + 0.015))
        d.ell(x, -1.43, 0.03, 0.035, "#6e4a32", hl=False)
    def strap(d, t):
        d.flat('<path d="M-0.17 -0.97 L0.15 -0.65" stroke="#6e4a32" stroke-width="0.026"/>')
        d.path("M%.3f -0.66 L%.3f -0.66 L%.3f -0.50 L%.3f -0.50 Z" % (P(t, 0.13, .1), P(t, 0.25, .1), P(t, 0.24, .1), P(t, 0.14, .1)), "#8a5a3a", off=0.012)
    return base(t, dict(skin="#e0b48c", shirt="#c8b48e", arm="#a8583a", trousers="#6e6058", back=cloak, head_back=hood_back,
                        head_top=hood_top, held=staff, front=strap, h=1.40))


def stonebeard(t):  # small folk: stout smith (dwarf-ish), grey-violet and leather
    def hair(d, t):
        x = P(t, 0, 0.02)
        d.path("M%.3f %.3f Q%.3f -1.36 %.3f -1.35 Q%.3f -1.36 %.3f %.3f Q%.3f -1.24 %.3f %.3f Z" % (x - 0.135, HY - 0.02, x - 0.12, x, x + 0.12, x + 0.135, HY - 0.02, x, x - 0.135, HY - 0.02), "#a87a52", off=0.02)
    def beard(d, t):
        x = P(t, 0, 0.12)
        d.path("M%.3f %.3f Q%.3f %.3f %.3f -0.86 Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.12, HY + 0.03, x - 0.13, HY + 0.2, x, x + 0.13, HY + 0.2, x + 0.12, HY + 0.03, x, HY + 0.1, x - 0.12, HY + 0.03), "#b8865a", off=0.02)
        d.flat('<path d="M%.3f -0.95 l0 0.07" stroke="#7a5236" stroke-width="0.01"/><circle cx="%.3f" cy="-0.875" r="0.012" fill="#c9a24e"/>' % (x, x))
        d.flat('<path d="M%.3f %.3f q0.04 -0.025 0.08 0" stroke="#8a5a3a" stroke-width="0.012" fill="none"/>' % (x - 0.04, HY + 0.07))
    def apron(d, t):
        d.path("M-0.15 -0.96 L0.15 -0.96 L0.17 -0.38 L-0.17 -0.38 Z", "#7a5236", off=0.03)
    def hammer(d, t):
        x = P(t, 0.24, 0.05)
        d.flat('<path d="M%.3f -0.62 L%.3f -0.30" stroke="#6e4a32" stroke-width="0.022" stroke-linecap="round"/>' % (x, x + 0.03))
        d.path("M%.3f -0.36 L%.3f -0.36 L%.3f -0.27 L%.3f -0.27 Z" % (x - 0.03, x + 0.09, x + 0.09, x - 0.03), "#8e8aa0", off=0.01)
    return base(t, dict(skin="#d9a582", shirt="#8e8aa0", trousers="#5a5470", boots="#4a3a30", shoulders=0.24, outfit=apron,
                        head_top=hair, muzzle=beard, held=hammer, mouth=False, brow="#7a5236", h=1.35))


def warden(t):  # armoured traveller after reference 06
    def shield(d, t):
        x = P(t, -0.05, -0.15)
        d.ell(x - 0.12, -0.80, 0.19, 0.22, "#8a6440", band=0.08)
        for k in (-1, 0, 1):
            d.flat('<path d="M%.3f -1.0 L%.3f -0.6" stroke="#5e4030" stroke-width="0.008"/>' % (x - 0.12 + k * 0.07, x - 0.12 + k * 0.07))
        d.ell(x - 0.12, -0.80, 0.04, 0.04, "#c9c6d6", hl=False)
    def plate(d, t):
        d.path("M-0.17 -0.98 Q0 -1.02 0.17 -0.98 L0.15 -0.70 Q0 -0.66 -0.15 -0.70 Z", "#c9c6d6", off=0.035)
        for s in (-1, 1):
            d.ell(P(t, s * 0.21, 0), -0.95, 0.09, 0.065, "#d8d4e2")
    def helm(d, t):
        x = P(t, 0, 0.02)
        d.path("M%.3f %.3f Q%.3f -1.38 %.3f -1.37 Q%.3f -1.38 %.3f %.3f L%.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.145, HY + 0.03, x - 0.15, x, x + 0.15, x + 0.145, HY + 0.03, x + 0.1, HY - 0.05, x, HY - 0.09, x - 0.1, HY - 0.05), "#c9c6d6", off=0.025)
        d.flat('<path d="M%.3f -1.36 L%.3f %.3f" stroke="#8e8aa0" stroke-width="0.012"/>' % (x, x, HY - 0.08))
    def sword(d, t):
        x = P(t, 0.22, 0.08)
        d.flat('<path d="M%.3f -0.58 L%.3f -0.10" stroke="%s" stroke-width="0.04" stroke-linecap="round"/><path d="M%.3f -0.58 L%.3f -0.10" stroke="#e4e2ec" stroke-width="0.024" stroke-linecap="round"/>' % (x, x + 0.05, rim("#c9c6d6"), x, x + 0.05))
        d.flat('<path d="M%.3f -0.60 L%.3f -0.60" stroke="#6e4a32" stroke-width="0.022" stroke-linecap="round"/>' % (x - 0.05, x + 0.05))
    return base(t, dict(skin="#e0b48c", shirt="#4e5a86", arm="#c9c6d6", forearm="#8a5a3a", trousers="#6a6680", boots="#5a4a40",
                        back=shield, outfit=plate, head_top=helm, held=sword, shoulders=0.22, h=1.37))


def fennick(t):  # fox-folk ranger
    def ears(d, t):
        for s in (-1, 1):
            x = P(t, s * 0.08, -0.02)
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - 0.045, HY - 0.1, x + s * 0.025, HY - 0.29, x + 0.045, HY - 0.1), "#c8743e")
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - 0.02, HY - 0.12, x + s * 0.015, HY - 0.24, x + 0.02, HY - 0.12), "#4a3530", off=0.004)
    def snout(d, t):
        x = P(t, 0.03, 0.14)
        d.path("M%.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.1, HY + 0.02, x - 0.02, HY + 0.13, x + 0.06 + 0.07 * t, HY + 0.08, x + 0.07 + 0.07 * t, HY + 0.05, x + 0.02, HY + 0.02, x - 0.04, HY + 0.0, x - 0.1, HY + 0.02), "#efe0c4", off=0.012)
        d.ell(x + 0.065 + 0.07 * t, HY + 0.055, 0.017, 0.013, "#2e2430", hl=False)
    def tail(d, t):
        x = P(t, -0.05, -0.2)
        d.path("M%.3f -0.62 Q%.3f -0.50 %.3f -0.18 Q%.3f -0.30 %.3f -0.55 Z" % (x, x - 0.3, x - 0.2, x - 0.08, x + 0.04), "#c8743e", off=0.03)
        d.path("M%.3f -0.27 Q%.3f -0.22 %.3f -0.18 Q%.3f -0.24 %.3f -0.29 Z" % (x - 0.25, x - 0.24, x - 0.2, x - 0.17, x - 0.22), "#efe0c4", off=0.008)
    def bow(d, t):
        x = P(t, 0, -0.12)
        d.flat('<path d="M%.3f -1.12 Q%.3f -0.80 %.3f -0.40" stroke="%s" stroke-width="0.03" fill="none"/><path d="M%.3f -1.12 Q%.3f -0.80 %.3f -0.40" stroke="#8a6440" stroke-width="0.016" fill="none"/>' % (x - 0.22, x - 0.38, x - 0.1, rim("#8a6440"), x - 0.22, x - 0.38, x - 0.1))
        d.flat('<path d="M%.3f -1.12 L%.3f -0.40" stroke="#e6dcc4" stroke-width="0.005"/>' % (x - 0.22, x - 0.1))
    def scarf(d, t):
        d.path("M%.3f -1.03 Q%.3f -0.95 %.3f -1.03 L%.3f -0.94 Q%.3f -0.88 %.3f -0.94 Z" % (P(t, -0.15, .05), P(t, 0, .1), P(t, 0.15, .05), P(t, 0.14, .06), P(t, 0, .1), P(t, -0.14, .06)), "#4e5a86", off=0.01)
        d.path("M%.3f -0.95 L%.3f -0.95 L%.3f -0.80 L%.3f -0.81 Z" % (P(t, 0.06, .1), P(t, 0.12, .1), P(t, 0.13, .1), P(t, 0.07, .1)), "#4e5a86", off=0.008)
    def pattern(d, t):
        d.ell(P(t, 0, 0.1), HY + 0.06, 0.09, 0.075, "#efe0c4", hl=False)
    return base(t, dict(skin="#c8743e", head="#c8743e", hand="#4a3530", shirt="#7a8a50", arm="#6e4a32", trousers="#8a6a50", boots="#4a3530",
                        head_back=ears, muzzle=snout, tail=tail, back=bow, front=scarf, pattern=pattern, nose=False, mouth=False, brow="#7a4222", h=1.42))


def bramblehide(t):  # bear-folk forager
    def ears(d, t):
        for s in (-1, 1):
            d.ell(P(t, s * 0.1, -0.02), HY - 0.13, 0.045, 0.045, "#7a5236", hl=False)
            d.ell(P(t, s * 0.1, -0.02), HY - 0.125, 0.022, 0.022, "#c8a07a", hl=False)
    def snout(d, t):
        x = P(t, 0, 0.14)
        d.ell(x, HY + 0.06, 0.065, 0.05, "#c8a07a", hl=False)
        d.ell(x, HY + 0.035, 0.024, 0.017, "#2e2430", hl=False)
    def basket(d, t):
        x = P(t, -0.03, -0.18)
        d.path("M%.3f -1.08 L%.3f -1.08 L%.3f -0.62 L%.3f -0.62 Z" % (x - 0.34, x - 0.02, x - 0.05, x - 0.31), "#a8804e", off=0.03)
        for k in range(4):
            d.flat('<path d="M%.3f %.3f L%.3f %.3f" stroke="#7a5a36" stroke-width="0.008"/>' % (x - 0.33, -1.0 + k * 0.1, x - 0.04, -1.0 + k * 0.1))
        for (dx, c) in ((-0.28, "#7fa34a"), (-0.18, "#d9604a"), (-0.1, "#7fa34a")):
            d.ell(x + dx, -1.1, 0.045, 0.035, c, hl=False)
    def vest(d, t):
        d.path("M-0.21 -0.98 Q-0.24 -0.8 -0.18 -0.64 L-0.06 -0.64 L-0.04 -0.98 Z", "#8e8aa0", off=0.02)
        d.path("M0.21 -0.98 Q0.24 -0.8 0.18 -0.64 L0.06 -0.64 L0.04 -0.98 Z", "#8e8aa0", off=0.02)
    def straps(d, t):
        for s in (-1, 1):
            d.flat('<path d="M%.3f -0.98 L%.3f -0.66" stroke="#5a3c2a" stroke-width="0.02"/>' % (s * 0.12, s * 0.13))
    return base(t, dict(skin="#7a5236", head="#8a6040", shirt="#a07a54", arm="#8a6040", forearm="#7a5236", hand="#6a4630", trousers="#5f7444",
                        boots="#4a3a30", shoulders=0.25, head_back=ears, muzzle=snout, back=basket, outfit=vest, front=straps,
                        nose=False, mouth=False, brow="#4a3020", h=1.35))


def capwright(t):  # mushroom folk wanderer
    def cap(d, t):
        x = P(t, 0, 0.0)
        d.ell(x, HY - 0.07, 0.25, 0.04, "#d8c4a0", hl=False)
        d.path("M%.3f %.3f Q%.3f -1.46 %.3f -1.47 Q%.3f -1.46 %.3f %.3f Q%.3f %.3f %.3f %.3f Z" % (
            x - 0.28, HY - 0.06, x - 0.25, x, x + 0.25, x + 0.28, HY - 0.06, x, HY - 0.12, x - 0.28, HY - 0.06), "#a8583a", off=0.035)
        for (dx, dy, r) in ((-0.15, -1.32, 0.025), (0.04, -1.40, 0.03), (0.16, -1.30, 0.02)):
            d.ell(x + dx, dy, r, r * 0.75, "#e6d4b0", hl=False, sw=0.006)
    def coat(d, t):
        d.path("M-0.2 -0.98 L0.2 -0.98 L0.24 -0.30 Q0 -0.26 -0.24 -0.30 Z", "#6b6a8a", off=0.04)
        d.flat('<path d="M%.3f -0.98 L%.3f -0.30" stroke="#4a4a68" stroke-width="0.008"/>' % (P(t, 0, .2), P(t, 0, .2)))
    def lantern(d, t):
        x = P(t, 0.26, 0.05)
        d.flat('<path d="M%.3f -0.58 L%.3f -0.50" stroke="#4a3a30" stroke-width="0.008"/>' % (x, x))
        d.flat('<ellipse cx="%.3f" cy="-0.43" rx="0.1" ry="0.1" fill="#ffd27a" opacity="0.25"/>' % x)
        d.path("M%.3f -0.50 L%.3f -0.50 L%.3f -0.36 L%.3f -0.36 Z" % (x - 0.035, x + 0.035, x + 0.04, x - 0.04), "#ffcf6a", off=0.006)
        d.flat('<path d="M%.3f -0.50 L%.3f -0.50 M%.3f -0.36 L%.3f -0.36" stroke="#4a3a30" stroke-width="0.012"/>' % (x - 0.045, x + 0.045, x - 0.045, x + 0.045))
    return base(t, dict(skin="#e6d4b0", shirt="#6b6a8a", trousers="#5a5470", boots="#5e4030", outfit=coat, head_top=cap, held=lantern,
                        brow="#8a6a4a", h=1.47))


def hollis(t):  # owl-folk scholar
    def tufts(d, t):
        for s in (-1, 1):
            x = P(t, s * 0.1, 0)
            d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - s * 0.04, HY - 0.12, x + s * 0.05, HY - 0.26, x + s * 0.05, HY - 0.08), "#6e5240")
    def discs(d, t):
        for s in (-1, 1):
            d.ell(P(t, s * 0.055, 0.12), HY + 0.0, 0.055, 0.06, "#e6d8bc", hl=False)
        x = P(t, 0, 0.15)
        d.path("M%.3f %.3f L%.3f %.3f L%.3f %.3f Z" % (x - 0.018, HY + 0.04, x, HY + 0.09, x + 0.018, HY + 0.04), "#c9a24e", off=0.004)
        for s in (-1, 1):
            d.flat('<circle cx="%.3f" cy="%.3f" r="0.03" fill="none" stroke="#c9a24e" stroke-width="0.007"/>' % (P(t, s * 0.05, 0.13), HY + 0.005))
    def robe(d, t):
        d.path("M-0.2 -0.98 L0.2 -0.98 L0.23 -0.24 Q0 -0.20 -0.23 -0.24 Z", "#4e5a86", off=0.04)
        for k in range(3):
            d.flat('<path d="M%.3f %.3f l0.03 0.018 l0.03 -0.018" stroke="#c8b48e" stroke-width="0.008" fill="none"/>' % (P(t, 0, .2) - 0.03, -0.9 + k * 0.05))
    def satchel(d, t):
        d.flat('<path d="M0.17 -0.97 L-0.15 -0.62" stroke="#6e4a32" stroke-width="0.022"/>')
        x = P(t, -0.22, 0.1)
        d.path("M%.3f -0.64 L%.3f -0.64 L%.3f -0.46 L%.3f -0.46 Z" % (x - 0.07, x + 0.07, x + 0.07, x - 0.07), "#8a5a3a", off=0.012)
        d.flat('<path d="M%.3f -0.70 L%.3f -0.62" stroke="#e6dcc4" stroke-width="0.02"/>' % (x - 0.02, x - 0.01))
    return base(t, dict(skin="#8a6a50", head="#8a6a50", hand="#c9a24e", shirt="#4e5a86", arm="#6e5240", trousers="#4e5a86", boots="#c9a24e",
                        outfit=robe, head_back=tufts, pattern=discs, front=satchel, face=True, nose=False, mouth=False, brow="#5a4030", h=1.42))


def bare(t):
    return base(t, dict())


CONCEPTS = [
    ("wayfarer", "Wayfarer", "Human traveller", wayfarer, "refs/02-pine.png", "02 forest meadow: moss and olive greens, painted pine tiers"),
    ("warden", "Warden", "Armoured traveller", warden, "refs/06-figure.png", "06 ice dungeon: the armoured figure"),
    ("fennick", "Fennick", "Fox-folk ranger", fennick, "refs/03-paths.png", "03 forest paths: warm earth, bark cliffs, olive canopy"),
    ("stonebeard", "Stonebeard", "Stout smith (small folk)", stonebeard, "refs/06-barrels.png", "06 ice dungeon: barrels, crates, worn dark wood"),
    ("bramblehide", "Bramblehide", "Bear-folk forager", bramblehide, "refs/02-stump-rocks.png", "02 meadow: stump, grey-violet rocks, grass"),
    ("capwright", "Capwright", "Mushroom-folk wanderer", capwright, "refs/01-block.png", "01 grass block: stump, mushrooms, rocks"),
    ("hollis", "Hollis", "Owl-folk scholar", hollis, "refs/05-pines.png", "05 snowy pines: blue-violet shade, warm white"),
]


# ------------------------------------------------------------ scenery in the reference style

def pine(x, y, s, col="#5f7444", haze=0.0):
    c = mix(col, "#b8b4c8", haze)
    out = '<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" fill="%s"/>' % (x - 6 * s, y - 40 * s, 12 * s, 40 * s, mix("#6e4a32", "#b8b4c8", haze))
    for k in range(5):
        w = (70 - k * 11) * s
        ty = y - (40 + k * 34) * s
        dk = dark(c, 0.3)
        pts = "M%.1f %.1f " % (x - w, ty)
        for j in range(6):
            pts += "L%.1f %.1f L%.1f %.1f " % (x - w + (j + 0.5) * w / 3, ty + 8 * s, x - w + (j + 1) * w / 3, ty)
        pts += "L%.1f %.1f Z" % (x, ty - 55 * s)
        out += '<path d="%s" fill="%s" stroke="%s" stroke-width="%.1f" stroke-linejoin="round"/>' % (pts, dk, rim(c), 1.5 * s)
        out += '<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="%s"/>' % (x - w + 6 * s, ty - 2 * s, x, ty - 52 * s, x + w * 0.15, ty - 4 * s, c)
    return out


def rock(x, y, s):
    return ('<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="#6e6a88" stroke="#4a4560" stroke-width="2"/>'
            '<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="#a8a4bc"/>') % (
        x - 40 * s, y, x - 30 * s, y - 32 * s, x + 5 * s, y - 42 * s, x + 38 * s, y - 20 * s, x + 42 * s, y,
        x - 30 * s, y - 30 * s, x + 4 * s, y - 40 * s, x + 10 * s, y - 18 * s, x - 22 * s, y - 10 * s)


def stump(x, y, s):
    return ('<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="#7a5236" stroke="#4a3020" stroke-width="2"/>'
            '<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="#d8b07a" stroke="#4a3020" stroke-width="2"/>'
            '<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="none" stroke="#a8804e" stroke-width="1.5"/>') % (
        x - 28 * s, y, x - 24 * s, y - 40 * s, x + 24 * s, y - 40 * s, x + 28 * s, y, x, y - 40 * s, 24 * s, 8 * s, x, y - 40 * s, 12 * s, 4 * s)


def grass(w, y, seed=3, n=None, cols=("#9cb04a", "#b5c45a", "#7f9a3e")):
    import random
    r = random.Random(seed)
    s = ""
    for _ in range(n or w // 5):
        x = r.uniform(0, w)
        h = r.uniform(10, 26)
        yy = y + r.uniform(-4, 30)
        c = r.choice(cols)
        lean = r.uniform(-6, 6)
        s += '<path d="M%.1f %.1f Q%.1f %.1f %.1f %.1f Q%.1f %.1f %.1f %.1f Z" fill="%s" stroke="%s" stroke-width="0.8"/>' % (
            x - 3, yy, x + lean * 0.5, yy - h * 0.6, x + lean, yy - h, x + 1 + lean * 0.3, yy - h * 0.5, x + 3, yy, c, rim(c))
    return s


def scene(w, h, gy):
    b = '<defs><linearGradient id="haze" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#e8dcc0"/><stop offset="1" stop-color="#d6d2b4"/></linearGradient></defs>'
    b += '<rect width="%d" height="%d" fill="url(#haze)"/>' % (w, h)
    for i in range(0, w, 130):
        b += pine(i + 40, gy - 70, 0.9, haze=0.6)
    for i in range(0, w, 260):
        b += pine(i + 120, gy - 30, 1.15, haze=0.25)
    b += '<rect x="0" y="%d" width="%d" height="%d" fill="#9cb04a"/>' % (gy - 40, w, h)
    b += '<rect x="0" y="%d" width="%d" height="%d" fill="#a88a5c"/>' % (gy - 8, w, 26)
    b += grass(w, gy - 40, 1, w // 6)
    for x in range(200, w, 520):
        b += rock(x, gy - 12, 0.8)
    for x in range(430, w, 700):
        b += stump(x, gy - 14, 0.8)
    return b


def fence(x, y, S):
    w = 0.12 * S
    return ('<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" rx="3" fill="#6e4a32" stroke="#3f2c26" stroke-width="2"/>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="9" fill="#5e4030" stroke="#3f2c26" stroke-width="1.5"/>'
            '<rect x="%.1f" y="%.1f" width="%.1f" height="9" fill="#5e4030" stroke="#3f2c26" stroke-width="1.5"/>') % (
        x - w / 2, y - S, w, S, x - w / 2 - 30, y - S * 0.75, w + 60, x - w / 2 - 30, y - S * 0.4, w + 60)


def table(x, y, S):
    w, h = 0.9 * S, 0.75 * S
    out = '<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" rx="3" fill="#6e4a32" stroke="#3f2c26" stroke-width="2"/>' % (x - w / 2, y - h, w, 0.07 * S)
    for lx in (x - w / 2 + 0.05 * S, x + w / 2 - 0.12 * S):
        out += '<rect x="%.1f" y="%.1f" width="%.1f" height="%.1f" fill="#5e4030" stroke="#3f2c26" stroke-width="2"/>' % (lx, y - h + 0.07 * S, 0.07 * S, h - 0.07 * S)
    return out


def char(fn, t, x, y, S):
    svg, _ = fn(t)
    return '<g transform="translate(%.1f %.1f) scale(%.2f)">%s</g>' % (x, y, S, svg)


def text(x, y, s, size=14, col="#3f2c26", anchor="middle", extra=""):
    return "<text x='%.1f' y='%.1f' font-size='%d' fill='%s' text-anchor='%s' %s %s>%s</text>" % (x, y, size, col, anchor, FONT, extra, s)


def shadow(x, y, S):
    return '<ellipse cx="%.1f" cy="%.1f" rx="%.1f" ry="%.1f" fill="#4a4a78" opacity="0.28"/>' % (x, y, 0.22 * S, 0.04 * S)


def main():
    # per-concept sheets, each paired with its reference crop
    for key, name, kind, fn, ref, refcap in CONCEPTS:
        S = 230
        W, Hh, gy = 1240, 470, 400
        b = scene(820, Hh, gy)
        b += fence(80, gy, S)
        for x, tt in ((300, 0), (560, 1)):
            b += shadow(x, gy, S) + char(fn, tt, x, gy, S)
        b += table(705, gy, S)
        b += '<rect x="20" y="14" width="560" height="38" rx="6" fill="#f3ead8" opacity="0.85"/>'
        b += text(32, 42, "%s <tspan font-size='17' fill='#6a5040'>· %s · %.2f m · shared base</tspan>" % (name, kind, fn(0)[1]), 26, anchor="start")
        for x, lab in ((300, "front"), (560, "three-quarter"), (80, "1 m fence"), (705, "0.75 m table")):
            b += text(x, 452, lab, 14, "#f6efe0")
        b += '<rect x="820" y="0" width="420" height="%d" fill="#2a2630"/>' % Hh
        b += '<image href="%s" x="840" y="40" width="380" height="370" preserveAspectRatio="xMidYMid meet"/>' % ref
        b += text(1030, 28, "Reference", 16, "#f3ead8")
        b += text(1030, 440, refcap, 12, "#d8d0c0")
        write("r3-" + key, W, Hh, b)

    # shared base sheet
    S = 300
    W, Hh, gy = 1240, 620, 540
    b = scene(W, Hh, gy)
    b += fence(80, gy, S) + table(1080, gy, S)
    b += shadow(330, gy, S) + char(bare, 0, 330, gy, S)
    sk = 640
    b += char(bare, 0, sk, gy, S).replace("<g transform", "<g opacity='0.35' transform", 1)
    bones = {"Head": (0, -1.17), "Neck": (0, -1.02), "Spine": (0, -0.82), "Hips": (0, -0.62)}
    for s, n in ((-1, "L"), (1, "R")):
        bones["UpperArm" + n] = (s * 0.19, -0.95)
        bones["LowerArm" + n] = (s * 0.24, -0.78)
        bones["Hand" + n] = (s * 0.245, -0.59)
        bones["UpperLeg" + n] = (s * 0.085, -0.6)
        bones["LowerLeg" + n] = (s * 0.09, -0.32)
        bones["Foot" + n] = (s * 0.095, -0.06)
    links = [("Hips", "Spine"), ("Spine", "Neck"), ("Neck", "Head")]
    for n in ("L", "R"):
        links += [("Neck", "UpperArm" + n), ("UpperArm" + n, "LowerArm" + n), ("LowerArm" + n, "Hand" + n),
                  ("Hips", "UpperLeg" + n), ("UpperLeg" + n, "LowerLeg" + n), ("LowerLeg" + n, "Foot" + n)]
    for a, c in links:
        (x1, y1), (x2, y2) = bones[a], bones[c]
        b += '<path d="M%.1f %.1f L%.1f %.1f" stroke="#3f2c26" stroke-width="3"/>' % (sk + x1 * S, gy + y1 * S, sk + x2 * S, gy + y2 * S)
    for (x, y) in bones.values():
        b += '<circle cx="%.1f" cy="%.1f" r="5" fill="#c8553d" stroke="#3f2c26" stroke-width="1.5"/>' % (sk + x * S, gy + y * S)
    for hm, lab in ((1.35, "1.35 m"), (1.0, "1 m"), (0.75, "0.75 m")):
        y = gy - hm * S
        b += '<path d="M20 %.1f L960 %.1f" stroke="#a8432e" stroke-width="1.2" stroke-dasharray="6 5" opacity="0.7"/>' % (y, y)
        b += text(24, y - 5, lab, 12, "#a8432e", "start")
    b += '<rect x="16" y="10" width="1210" height="56" rx="6" fill="#f3ead8" opacity="0.88"/>'
    b += text(28, 34, "Round 3 shared base: 1.35 m, about 4 heads tall, the same 16-bone rig", 22, anchor="start")
    b += text(28, 56, "Slots: head (fur colour, ears, snout or beak), head-top (hood, helm, hair, cap), torso layer (apron, plate, robe, coat), front (straps, scarf), back (cloak, shield, basket, bow), tail on Hips, held item.", 13, "#5a4636", "start")
    b += text(330, gy + 40, "bare base", 14, "#f6efe0") + text(sk, gy + 40, "16-bone rig", 14, "#f6efe0")
    write("r3-shared-base", W, Hh, b)

    # lineup in a reference-style scene
    S, slot = 210, 165
    W = 140 + slot * (len(CONCEPTS) + 1) + 170
    Hh, gy = 520, 440
    b = scene(W, Hh, gy)
    b += fence(70, gy, S)
    xs = [70 + slot * (i + 1) for i in range(len(CONCEPTS) + 1)]
    b += shadow(xs[0], gy, S) + char(bare, 0.5, xs[0], gy, S) + text(xs[0], gy + 50, "shared base", 14, "#f6efe0")
    for i, (key, name, kind, fn, _, _) in enumerate(CONCEPTS):
        x = xs[i + 1]
        b += shadow(x, gy, S) + char(fn, 0.5, x, gy, S)
        b += text(x, gy + 44, "%d. %s" % (i + 1, name), 16, "#fff6e6")
        b += text(x, gy + 62, kind, 11, "#f0e6d0")
    b += table(W - 110, gy, S)
    b += '<rect x="10" y="8" width="900" height="34" rx="6" fill="#f3ead8" opacity="0.85"/>'
    b += text(20, 32, "Round 3: less cute, closer to the reference art. All on one 1.35 m shared base (16-bone rig).", 18, anchor="start")
    write("r3-lineup", W, Hh, b)
    print("ok")


if __name__ == "__main__":
    main()

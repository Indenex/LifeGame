#!/usr/bin/env python3
"""Round 4 storyboard: back to the round-2 animal/humanoid look, slightly less cute.

Justin, after round 3: "this new version is even worse. Go back to the old animal/humanoid look,
just slightly less cute, chibiish is fine".

Same concepts and parts as round 2, re-rendered on a tweaked shared base:
- head scaled to 85% around the neck (about 2.8 heads tall instead of 2.3)
- legs 6 cm longer (in drawing units), so the stance is less toddler-like
- smaller eyes, short brows, fainter blush
Still one body, one 16-bone rig, 1.35 m in game.
"""
import make_round2 as r2
from make_concepts import D, P, face, legs, dark, write
from make_round2 import HY, HR

HEAD_SCALE = 0.85
NECK_Y = -0.47
LEG_EXTRA = 0.06
BASE_TOP = LEG_EXTRA + (-NECK_Y) + (HY - HR * 0.9 - NECK_Y) * -HEAD_SCALE
K = 1.35 / BASE_TOP
BACK = False   # set True to draw the back view: no face or front parts, back and tail drawn over the body


def base(t, sp):
    d = D()
    skin = sp.get("skin", "#d8c9b4")
    up = '<g transform="translate(0 %.3f)">' % -LEG_EXTRA
    if not BACK:
        d.flat(up)
        for slot in ("back", "tail"):
            if slot in sp:
                sp[slot](d, t)
        d.flat("</g>")
    legs(d, t, 0, 0.085, 0.16 + LEG_EXTRA, sp.get("leg", skin), sp.get("feet", dark(skin, 0.45)), gap=0.06, footw=0.07)
    d.flat(up)
    d.ell(0, -0.30, 0.15, 0.15, sp.get("body", skin))
    if "belly" in sp and not BACK:
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
    # head group, scaled about the neck
    d.flat('<g transform="translate(0 %.3f) scale(%.3f) translate(0 %.3f)">' % (NECK_Y, HEAD_SCALE, -NECK_Y))
    if "ears" in sp:
        sp["ears"](d, t)
    d.ell(0, HY, HR, HR * 0.9, sp.get("back_head", sp.get("head", skin)) if BACK else sp.get("head", skin))
    if "pattern" in sp and not BACK:
        sp["pattern"](d, t)
    if sp.get("face", True) and not BACK:
        er = sp.get("er", HR * 0.16) * 0.72
        face(d, t, 0, HY + 0.02, HR, ey=0.1, er=er, mouth=sp.get("mouth", True), cheeks=sp.get("cheeks", True), my=0.42)
        for s in (-1, 1):  # short brows: a bit of attitude, less baby-like
            x = P(t, s * HR * 0.36, HR * 0.9)
            sq = 1 - 0.35 * t * (s < 0)
            d.flat('<path d="M%.3f %.3f l%.3f %.3f" stroke="%s" stroke-width="0.011" stroke-linecap="round"/>' % (
                x - 0.025 * sq, HY + 0.02 - 0.034 - s * 0.003, 0.05 * sq, s * 0.006, sp.get("brow", dark(sp.get("head", skin), 0.6))))
    if "muzzle" in sp and not BACK:  # eyes are smaller now, so the muzzle sits a little lower to keep them clear
        d.flat('<g transform="translate(0 0.035)">')
        sp["muzzle"](d, t)
        d.flat("</g>")
    if "top" in sp:
        sp["top"](d, t)
    d.flat("</g>")
    if "front" in sp and not BACK:
        sp["front"](d, t)
    if BACK:
        for slot in ("back", "tail"):
            if slot in sp:
                sp[slot](d, t)
    d.flat("</g>")
    svg = d.svg().replace('fill="#f08a86" opacity="0.55"', 'fill="#f08a86" opacity="0.28"')
    h = sp.get("h", 0.84)
    return '<g transform="scale(%.4f)">%s</g>' % (K, svg), (LEG_EXTRA + 0.47 + (h - 0.47) * HEAD_SCALE) * K


def bone_table():
    bones = {"Head": (0, NECK_Y - LEG_EXTRA + (HY - NECK_Y) * HEAD_SCALE), "Neck": (0, -0.53), "Spine": (0, -0.39), "Hips": (0, -0.23)}
    for s, n in ((-1, "L"), (1, "R")):
        bones["UpperArm" + n] = (s * 0.13, -0.46)
        bones["LowerArm" + n] = (s * 0.165, -0.38)
        bones["Hand" + n] = (s * 0.18, -0.31)
        bones["UpperLeg" + n] = (s * 0.06, -0.22)
        bones["LowerLeg" + n] = (s * 0.06, -0.11)
        bones["Foot" + n] = (s * 0.065, -0.02)
    return bones


# every round-2 concept now draws on this base
r2.base = base
r2.bone_table = bone_table
r2.K = K
r2.BASE_H = BASE_TOP


def main():
    r2.main()
    import os, glob
    for f in glob.glob(os.path.join(r2.OUT, "r2-*.svg")):
        g = f.replace("r2-", "r4-")
        txt = open(f).read()
        txt = txt.replace("Round 2: 10 new concepts plus 4 from round 1", "Round 4: the round-2 look, slightly less cute. 10 concepts plus 4 from round 1")
        txt = txt.replace("tallest ears or hat about 1.77 m", "tallest ears or hat about %.2f m" % ((LEG_EXTRA + 0.47 + (1.10 - 0.47) * HEAD_SCALE) * K))
        txt = txt.replace("Shared base: one body", "Round 4 shared base: one body")
        open(g, "w").write(txt)
        os.remove(f)
    print("ok")


if __name__ == "__main__":
    main()

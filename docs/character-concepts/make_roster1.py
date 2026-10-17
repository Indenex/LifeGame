#!/usr/bin/env python3
"""Roster 1 handoff pack for Modeling: turnaround sheets for Justin's picks (Pebble, Kit, Capling, Bramble).

Uses the round-4 shared base (Justin locked round 4's level of cute). Views: front, three-quarter,
side and back. Writes SVGs into roster-1/; render.sh turns them into PNGs.
"""
import os
import make_round4 as r4          # patches make_round2 so every concept draws on the round-4 base
import make_round2 as r2
from make_concepts import ground, fence, write as _write, FONT
from make_round2 import table, text

HERE = os.path.dirname(os.path.abspath(__file__))
ROSTER = os.path.join(HERE, "roster-1")
os.makedirs(ROSTER, exist_ok=True)

PICKS = [
    ("pebble", "Pebble", "Tortoise", r2.pebble, [
        ("skin", "#a8c27a"), ("belly plate", "#e8d9a0"), ("shell", "#8a6a48"), ("shell rim", "#c9a46a"),
        ("flower", "#f6f0e2"), ("flower heart", "#f2cf5a"), ("feet", "#8fa866")],
     [("back: shell", "Spine (rigid, no new bone)"), ("front: belly plate", "Spine"), ("head-top: flower", "Head"), ("colour: green skin", "all")]),
    ("kit", "Kit", "Fox", r2.kit2, [
        ("fur", "#e8894a"), ("cream", "#f6e6c8"), ("ear tips, boots", "#4a3530"), ("scarf", "#6f9fb4")],
     [("ears: triangle", "Head"), ("face: muzzle", "Head"), ("tail: brush", "Hips (stiff; one optional bone later)"), ("front: scarf", "Neck / Spine"), ("colour: fur + cream belly", "all")]),
    ("capling", "Capling", "Mushroom", r2.capling2, [
        ("stem skin", "#f1e2c4"), ("cap", "#d9604a"), ("spots", "#f8ecd6"), ("gills", "#e7cfa8"), ("boots", "#8b5e3c")],
     [("head-top: spotted cap + gill ring", "Head"), ("colour: cream stem", "all")]),
    ("bramble", "Bramble", "Chibi human", r2.bramble2, [
        ("skin", "#f2c9a0"), ("hair", "#9a5a34"), ("tunic", "#6f8f4e"), ("trousers", "#6a5a4c"), ("boots", "#7b4e33"), ("strap", "#8b5e3c")],
     [("head-top: hair", "Head"), ("outfit: tunic", "Spine + Hips (skinned)"), ("front: satchel strap", "Spine"), ("colour: skin, hair", "all")]),
]

VIEWS = [("front", 0, False), ("three-quarter", 1, False), ("side", 2, False), ("back", 0, True)]


def write(name, w, h, body):
    _write(name, w, h, body)
    src = os.path.join(r2.OUT, name + ".svg")
    os.replace(src, os.path.join(ROSTER, name + ".svg"))


def draw(fn, t, back, x, y, S):
    r4.BACK = back
    svg, h = fn(t)
    r4.BACK = False
    return '<g transform="translate(%.1f %.1f) scale(%.2f)">%s</g>' % (x, y, S, svg), h


def sheet(key, name, kind, fn, colours, slots):
    S = 220
    W, H, gy = 1560, 560, 460
    b = ground(W, gy)
    b += fence(60, gy, S) + text(60, gy + 40, "1 m fence", 13)
    h = 0
    for i, (lab, t, back) in enumerate(VIEWS):
        x = 240 + i * 250
        g, h = draw(fn, t, back, x, gy, S)
        b += g + text(x, gy + 40, lab, 16)
    b += '<path d="M20 %.1f L1110 %.1f" stroke="#c8553d" stroke-width="1.2" stroke-dasharray="6 5"/>' % (gy - 1.35 * S, gy - 1.35 * S)
    b += text(1105, gy - 1.35 * S - 6, "body 1.35 m", 12, "#c8553d", "end")
    b += text(24, 40, "%s <tspan font-size='18' fill='#7a5f4a'>· %s · roster 1 turnaround · %.2f m to the top</tspan>" % (name, kind, h), 30, anchor="start")
    # colour key + slots panel
    px = 1150
    b += '<rect x="%d" y="70" width="390" height="370" rx="10" fill="#fbf5e8" stroke="#d8c8a8"/>' % (px - 10)
    b += text(px, 98, "Colour key", 17, anchor="start")
    for i, (lab, col) in enumerate(colours):
        y = 112 + i * 26
        b += '<rect x="%d" y="%d" width="34" height="20" rx="4" fill="%s" stroke="#5a4636" stroke-width="1"/>' % (px, y, col)
        b += text(px + 44, y + 15, "%s <tspan fill='#8a7a6a'>%s</tspan>" % (lab, col), 13, anchor="start")
    y0 = 112 + len(colours) * 26 + 24
    b += text(px, y0, "Slots → bone", 17, anchor="start")
    for i, (sl, bone) in enumerate(slots):
        b += text(px, y0 + 22 + i * 21, "%s → <tspan fill='#a8432e'>%s</tspan>" % (sl, bone), 12, anchor="start")
    write("turnaround-" + key, W, H, b)


def lineup():
    S, slot = 240, 230
    W, H, gy = 160 + slot * 5, 520, 430
    b = ground(W, gy) + fence(70, gy, S) + text(70, gy + 40, "1 m fence", 13)
    for i, (key, name, kind, fn, _, _) in enumerate(PICKS):
        x = 70 + slot * (i + 1)
        g, _ = draw(fn, 1, False, x, gy, S)
        b += g + text(x, gy + 40, name, 18) + text(x, gy + 60, kind, 12, "#5a4636")
    b += table(W - 130, gy, S) + text(W - 130, gy + 40, "0.75 m table", 13)
    b += text(24, 40, "Roster 1 (Justin's picks): Pebble, Kit, Capling, Bramble on the round-4 shared base", 24, anchor="start")
    write("roster-1-lineup", W, H, b)


def main():
    for p in PICKS:
        sheet(*p)
    lineup()
    print("ok")


if __name__ == "__main__":
    main()

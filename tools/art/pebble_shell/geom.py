"""Pebble's shell (art-rnd-1 pilot): the shape and the scute layout, shared by the
Blender build (blender_build.py) and the painter (paint.py). numpy only.

Coordinates are Godot's rig space at rest (metres): x right, y up, z forward (the
belly faces +z, the shell sits on the back at -z). ChibiFigure: 1.35 m tall,
hipY 0.4, waistY 0.571, shoulderY 0.828, chin 0.868.

The carapace is one dome over an oval rim, parameterised by rho (0 at the top of the
dome, 1 at the rim) and theta (0 to the figure's right, pi/2 up towards the neck).
Its scutes are laid out in "surface" coordinates L: arc length from the dome's top
along each radial line, in metres. So seams have an even width all over the dome, and
the texture (UV = centre + K * L) has no stretch at the steep sides.

Cute, after D-313 ("cutesy, relaxing, with a hint of realism"): a tortoise's layout
simplified to big friendly plates: 3 vertebral scutes down the spine, 2 costal scutes a
side, and a chunky ring of 16 marginal scutes, the nuchal one centred at the neck.
"""
import numpy as np

# Carapace size and place.
A = 0.345          # half height (y)
B = 0.29           # half width (x)
H = 0.37          # dome height (outwards, -z): a high tortoise dome
YC = 0.525         # centre height
RING_F = 0.76      # fraction of the arc from the top to the rim where the marginal ring starts
N_MARG = 16
SCALLOP = 0.045    # how far each marginal's outer edge bulges between seams
THICK = 0.022      # shell wall

# Texture atlas (1024 px square): the carapace, the plastron, and two flat patches.
CARA_UV = np.array([0.37, 0.5])
PLAS_UV = np.array([0.85, 0.5])
PLAS_K = 0.62
INNER_UV = np.array([0.965, 0.035])   # inside of the shell (dark)
EDGE_UV = np.array([0.965, 0.095])    # the cut edge of the wall (pale)

_MSEAM0 = np.pi / 2 - np.pi / N_MARG  # a seam either side of the nuchal


def _smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def scallop(rho, theta):
    seg = (theta - _MSEAM0) / (2 * np.pi / N_MARG)
    bump = np.sin(np.pi * (seg - np.floor(seg))) ** 2
    return 1.0 + SCALLOP * _smooth(0.72, 1.0, rho) * bump


def cara_point(rho, theta, inner=False):
    """A point on the carapace's outer (or inner) surface."""
    rho = np.asarray(rho, float)
    theta = np.asarray(theta, float)
    sc = scallop(rho, theta)
    x = B * rho * np.cos(theta) * sc
    yy = A * rho * np.sin(theta) * sc
    h = H * np.clip(1 - rho ** 2.4, 0, 1) ** 0.6
    # The rim bends back at the top and bottom, behind the neck and over the hips.
    zr = 0.02 - 0.14 * (rho * np.sin(theta)) ** 2
    if inner:
        x, yy, h = x * (1 - THICK / B), yy * (1 - THICK / A), np.maximum(h - THICK, 0) * 0.98
    return np.stack([x, YC + yy, zr - h], -1)


# Arc length along each radial line, tabulated once.
_NT, _NR = 1440, 600
_T = np.linspace(-np.pi, np.pi, _NT, endpoint=False)
_R = np.linspace(0, 1, _NR)
_P = cara_point(_R[None, :], _T[:, None])
_ARC = np.concatenate([np.zeros((_NT, 1)), np.cumsum(np.linalg.norm(np.diff(_P, axis=1), axis=-1), axis=1)], 1)


def _ti(theta):
    t = (np.asarray(theta) + np.pi) % (2 * np.pi)
    return t / (2 * np.pi) * _NT


def arc(rho, theta):
    """Arc length (m) from the dome's top to (rho, theta)."""
    ti = _ti(theta)
    i0 = np.floor(ti).astype(int) % _NT
    i1 = (i0 + 1) % _NT
    f = ti - np.floor(ti)
    rj = np.clip(np.asarray(rho), 0, 1) * (_NR - 1)
    j0 = np.clip(np.floor(rj).astype(int), 0, _NR - 2)
    g = rj - j0
    a0 = _ARC[i0, j0] * (1 - g) + _ARC[i0, j0 + 1] * g
    a1 = _ARC[i1, j0] * (1 - g) + _ARC[i1, j0 + 1] * g
    return a0 * (1 - f) + a1 * f


def rho_from_arc(a, theta):
    """Inverse of arc() along one radial line (rho clipped to 1 past the rim)."""
    i = np.round(_ti(theta)).astype(int) % _NT
    rows = _ARC[i]                                  # (n, NR)
    j = np.clip((rows < np.asarray(a)[..., None]).sum(-1), 1, _NR - 1)
    lo, hi = np.take_along_axis(rows, (j - 1)[..., None], -1)[..., 0], np.take_along_axis(rows, j[..., None], -1)[..., 0]
    g = np.clip((a - lo) / np.maximum(hi - lo, 1e-9), 0, 1)
    return np.where(a >= rows[:, -1], 1.0 + (a - rows[:, -1]) * 4, _R[j - 1] + g * (_R[1] - _R[0]))


def surface_L(rho, theta):
    a = arc(rho, theta)
    return np.stack([a * np.cos(theta), a * np.sin(theta)], -1)


# Scute seeds in surface coordinates: vertebrals down the middle, costals staggered
# between them so the vertebrals come out hexagonal.
def ring_arc(theta):
    return RING_F * arc(1.0, theta)


_XI = float(ring_arc(0.0))
_YI = float(ring_arc(np.pi / 2))
VERT = np.array([[0.0, f * _YI] for f in (0.62, 0.0, -0.62)])
COST = np.array([[s * 0.68 * _XI, f * _YI] for s in (-1, 1) for f in (0.33, -0.33)])
SEEDS = np.concatenate([VERT, COST])


def scutes(rho, theta):
    """For points on the carapace: (region id, distance to the nearest seam in m,
    direction from the point towards that seam (unit, in L), distance from the
    region's centre normalised 0..1 by its size).

    Ids: 0-2 vertebral (top to bottom), 3-6 costal, 13+ marginal (13 is the nuchal)."""
    rho = np.asarray(rho, float)
    theta = np.asarray(theta, float)
    L = surface_L(rho, theta)
    a_ring = ring_arc(theta)
    a = arc(rho, theta)
    inner = a < a_ring
    # Inner scutes: Voronoi cells of the seeds; distance to the nearest bisector.
    d2 = ((L[..., None, :] - SEEDS) ** 2).sum(-1)
    own = d2.argmin(-1)
    so = SEEDS[own]
    dist_in = np.full(rho.shape, 1e9)
    dir_in = np.zeros(rho.shape + (2,))
    for k in range(len(SEEDS)):
        sk = SEEDS[k]
        sep = sk - so
        n = np.linalg.norm(sep, axis=-1)
        ok = n > 1e-9
        dk = np.where(ok, (d2[..., k] - (d2 * np.eye(len(SEEDS))[own]).sum(-1)) / (2 * np.maximum(n, 1e-9)), 1e9)
        better = ok & (dk < dist_in)
        dist_in = np.where(better, dk, dist_in)
        dir_in = np.where(better[..., None], sep / np.maximum(n, 1e-9)[..., None], dir_in)
    # ...and to the ring boundary.
    radial = np.stack([np.cos(theta), np.sin(theta)], -1)
    dr = a_ring - a
    better = dr < dist_in
    dist_in = np.where(better, dr, dist_in)
    dir_in = np.where(better[..., None], radial, dir_in)
    size_in = np.where(own < len(VERT), 0.5 * _YI * 0.62, 0.5 * 0.5 * _XI)
    cen_in = np.clip(np.sqrt(((L - so) ** 2).sum(-1)) / (size_in * 1.6), 0, 1)
    # Marginal ring: radial seams every 15 degrees, the ring boundary inside.
    step = 2 * np.pi / N_MARG
    seg = (theta - _MSEAM0) / step
    segi = np.floor(seg).astype(int) % N_MARG
    frac = seg - np.floor(seg)
    da = np.minimum(frac, 1 - frac) * step * np.maximum(a, 1e-6)
    tangent = np.stack([-np.sin(theta), np.cos(theta)], -1) * np.where(frac < 0.5, -1, 1)[..., None]
    dring = a - a_ring
    dist_m = np.minimum(da, dring)
    dir_m = np.where((da < dring)[..., None], tangent, -radial)
    rim_len = arc(1.0, theta) - a_ring
    cen_m = np.clip(np.maximum(np.abs(frac - 0.5) * 2, np.abs((a - a_ring) / np.maximum(rim_len, 1e-6) - 0.5) * 2), 0, 1)
    # Ids: the nuchal (segment containing pi/2) is 13, then round the ring.
    mid = 13 + segi
    region = np.where(inner, own, mid)
    dist = np.where(inner, dist_in, dist_m)
    direc = np.where(inner[..., None], dir_in, dir_m)
    cen = np.where(inner, cen_in, cen_m)
    return region, np.maximum(dist, 0), direc, cen


def groove(dist, depth=0.006, width=0.013, pillow=0.009):
    """Relief of the carved shell: a V groove on each seam, each scute a little domed."""
    return -depth * (1 - _smooth(0, width, dist)) + pillow * _smooth(0.0, 0.07, dist)


def cara_normal(rho, theta, inner=False, e=1e-3):
    p = cara_point(rho, theta, inner)
    pr = cara_point(np.clip(rho + e, 0, 1), theta, inner) - cara_point(np.clip(rho - e, 0, 1), theta, inner)
    pt = cara_point(np.maximum(rho, 2e-3), theta + e, inner) - cara_point(np.maximum(rho, 2e-3), theta - e, inner)
    n = np.cross(pt, pr)
    n /= np.linalg.norm(n, axis=-1, keepdims=True) + 1e-12
    if inner:
        n = -n
    return p, n


def carapace(nr, nt, relief=False):
    """Outer carapace mesh: verts (n,3), faces, uvs (n,2). Rings closer together
    towards the rim, where the dome turns steeply."""
    t = np.linspace(0, 1, nr + 1)[1:]
    rho = 1 - (1 - t) ** 1.6
    th = np.linspace(-np.pi, np.pi, nt, endpoint=False)
    R, T = np.meshgrid(rho, th, indexing="ij")
    p, n = cara_normal(R, T)
    if relief:
        _, dist, _, _ = scutes(R, T)
        # Outward is the normal facing away from the body (-z mostly).
        p = p + n * groove(dist)[..., None]
    uv = CARA_UV + surface_L(R, T) * K_CARA
    verts = np.concatenate([cara_point(np.array([0.0]), np.array([0.0])), p.reshape(-1, 3)])
    uvs = np.concatenate([CARA_UV[None], uv.reshape(-1, 2)])
    faces = []
    for j in range(nt):
        faces.append((0, 1 + j, 1 + (j + 1) % nt))
    for i in range(nr - 1):
        for j in range(nt):
            a, b = 1 + i * nt + j, 1 + i * nt + (j + 1) % nt
            faces.append((a, a + nt, b + nt, b))
    return verts, faces, uvs


def wall(nt):
    """The inside of the carapace and the cut edge of its wall."""
    th = np.linspace(-np.pi, np.pi, nt, endpoint=False)
    nr = 6
    rho = np.linspace(0, 1, nr + 1)[1:]
    R, T = np.meshgrid(rho, th, indexing="ij")
    pin = cara_point(R, T, inner=True)
    verts = [cara_point(np.array([0.0]), np.array([0.0]), inner=True)[0]] + list(pin.reshape(-1, 3))
    uvs = [INNER_UV] * len(verts)
    faces = []
    for j in range(nt):
        faces.append((0, 1 + (j + 1) % nt, 1 + j))
    for i in range(nr - 1):
        for j in range(nt):
            a, b = 1 + i * nt + j, 1 + i * nt + (j + 1) % nt
            faces.append((a, b, b + nt, a + nt))
    # The edge band: outer rim to inner rim, its own vertices so it shades flat.
    base = len(verts)
    out_rim = cara_point(np.ones(nt), th)
    in_rim = cara_point(np.ones(nt), th, inner=True)
    for j in range(nt):
        verts += [out_rim[j], in_rim[j]]
        uvs += [EDGE_UV, EDGE_UV]
    for j in range(nt):
        a, b = base + 2 * j, base + 2 * ((j + 1) % nt)
        faces.append((a, b, b + 1, a + 1))
    return np.array(verts), faces, np.array(uvs)


# Plastron: a pale belly shield following the front of the body's lofts.
PW, PH, PY = 0.15, 0.2, 0.55
# Front depth and half width of the body (ChibiFigure's hip and spine lofts) by height.
_BODY_Y = np.array([0.30, 0.393, 0.518, 0.611, 0.70, 0.845])
_BODY_D = np.array([0.13, 0.19, 0.195, 0.175, 0.155, 0.12])
_BODY_W = np.array([0.15, 0.215, 0.22, 0.2, 0.18, 0.14])
# Seams across the plastron (y, top to bottom): four rows of plates, a cute reading of
# the real six (D-313).
PSEAMS = np.array([0.1, -0.03, -0.13])


def plas_outline(theta):
    notch = 1 - 0.1 * np.exp(-((theta + np.pi / 2) / 0.16) ** 2) - 0.05 * np.exp(-((theta - np.pi / 2) / 0.2) ** 2)
    # Squarer at the shoulders and hips than an ellipse.
    c, s = np.cos(theta), np.sin(theta)
    k = (np.abs(c) ** 2.6 + np.abs(s) ** 2.6) ** (-1 / 2.6)
    return k * notch


def plas_point(rho, theta, relief=False):
    rho = np.asarray(rho, float)
    theta = np.asarray(theta, float)
    r = rho * plas_outline(theta)
    x = PW * r * np.cos(theta)
    yo = PH * r * np.sin(theta)
    y = PY + yo
    d = np.interp(y, _BODY_Y, _BODY_D)
    w = np.interp(y, _BODY_Y, _BODY_W)
    z = d * np.sqrt(np.clip(1 - (x / w) ** 2, 0, 1)) + 0.004 + 0.02 * np.sqrt(np.clip(1 - rho ** 2, 0, 1))
    if relief:
        z = z + groove(plas_seams(x, yo)[0], depth=0.004, width=0.009, pillow=0.004)
    return np.stack([x, y, z], -1)


def plas_seams(x, yo):
    """Plastron (local x, y from its centre): distance to the nearest seam, the plate
    index (0 gular .. 5 anal), and distance from the outline."""
    bow = 0.02 * (x / PW) ** 2           # seams bow down a little towards the sides
    ys = PSEAMS[None] if np.ndim(x) == 0 else PSEAMS.reshape((1,) * np.ndim(x) + (-1,))
    dy = np.abs(yo[..., None] + bow[..., None] - ys) if np.ndim(x) else np.abs(yo + bow - PSEAMS)
    dmid = np.abs(x)
    dist = np.minimum(dy.min(-1), dmid)
    plate = (yo[..., None] + bow[..., None] < ys).sum(-1) if np.ndim(x) else int((yo + bow < PSEAMS).sum())
    return dist, plate


def plastron(nr, nt, relief=False):
    rho = np.linspace(0, 1, nr + 1)[1:]
    th = np.linspace(-np.pi, np.pi, nt, endpoint=False)
    R, T = np.meshgrid(rho, th, indexing="ij")
    p = plas_point(R, T, relief)
    loc = np.stack([p[..., 0], p[..., 1] - PY], -1)
    verts = np.concatenate([plas_point(np.array([0.0]), np.array([0.0]), relief), p.reshape(-1, 3)])
    uvs = np.concatenate([PLAS_UV[None], (PLAS_UV + loc * PLAS_K).reshape(-1, 2)])
    faces = []
    for j in range(nt):
        faces.append((0, 1 + (j + 1) % nt, 1 + j))
    for i in range(nr - 1):
        for j in range(nt):
            a, b = 1 + i * nt + j, 1 + i * nt + (j + 1) % nt
            faces.append((a, b, b + nt, a + nt))
    return verts, faces, uvs


# UV scale for the carapace: the longest arc fits a 0.33 radius.
K_CARA = 0.33 / float(_ARC[:, -1].max())

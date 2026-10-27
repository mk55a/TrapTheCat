"""Pixel-art black cat (red collar + bell) built from posable parts.
Frames 32x32, cat faces RIGHT, paws touch row 29. Pivot (0.5, 2/32).
"""
import math
import os
import numpy as np
from PIL import Image

W = H = 32
OUT = '/tmp/cat/out'

C = {
    'ol': (18, 14, 22), 'far': (31, 26, 38), 'base': (43, 36, 51), 'shade': (34, 28, 41),
    'hi': (69, 59, 82), 'spec': (96, 84, 112),
    'eye': (214, 232, 90), 'eyeD': (150, 176, 40), 'pupil': (18, 14, 22), 'glint': (255, 255, 240),
    'ear': (138, 74, 90), 'nose': (208, 122, 138), 'mouth': (90, 31, 42), 'tongue': (224, 120, 138),
    'collar': (217, 87, 63), 'collarD': (154, 51, 36), 'bell': (242, 185, 59), 'bellD': (176, 116, 28),
    'z': (243, 233, 210),
}

YY, XX = np.mgrid[0:H, 0:W].astype(float)


# ---------------------------------------------------------------- primitives

def ellipse(cx, cy, rx, ry, ang=0.0):
    a = math.radians(ang)
    dx, dy = XX - cx, YY - cy
    u = dx * math.cos(a) + dy * math.sin(a)
    v = -dx * math.sin(a) + dy * math.cos(a)
    return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0


def capsule(ax, ay, bx, by, r):
    vx, vy = bx - ax, by - ay
    L2 = vx * vx + vy * vy or 1e-6
    t = np.clip(((XX - ax) * vx + (YY - ay) * vy) / L2, 0, 1)
    px, py = ax + t * vx, ay + t * vy
    return (XX - px) ** 2 + (YY - py) ** 2 <= r * r


def polygon(pts):
    m = np.zeros((H, W), bool)
    n = len(pts)
    inside = np.zeros((H, W), bool)
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        cond = ((y1 > YY) != (y2 > YY))
        xint = (x2 - x1) * (YY - y1) / ((y2 - y1) if y2 != y1 else 1e-6) + x1
        inside ^= cond & (XX < xint)
    return inside | m


def chain(pts, r0, r1):
    """Smooth tapered tube through points (Catmull-Rom)."""
    m = np.zeros((H, W), bool)
    P = [pts[0]] + list(pts) + [pts[-1]]
    samples = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(8):
            t = k / 8
            t2, t3 = t * t, t * t * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2
                       + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2
                       + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            samples.append((x, y))
    samples.append(pts[-1])
    n = len(samples)
    for i in range(n - 1):
        r = r0 + (r1 - r0) * i / max(1, n - 2)
        m |= capsule(*samples[i], *samples[i + 1], r)
    return m


def rot(px, py, cx, cy, ang):
    a = math.radians(ang)
    dx, dy = px - cx, py - cy
    return cx + dx * math.cos(a) - dy * math.sin(a), cy + dx * math.sin(a) + dy * math.cos(a)


def nb(m, dy, dx):
    out = np.zeros_like(m)
    ys = slice(max(0, -dy), H - max(0, dy))
    yd = slice(max(0, dy), H - max(0, -dy))
    xs = slice(max(0, -dx), W - max(0, dx))
    xd = slice(max(0, dx), W - max(0, -dx))
    out[yd, xd] = m[ys, xs]
    return out


def grow(m):
    return m | nb(m, 1, 0) | nb(m, -1, 0) | nb(m, 0, 1) | nb(m, 0, -1)


# ---------------------------------------------------------------- the cat

def leg(joint, paw, r_top=1.7, r_bot=1.2, knee=None):
    m = np.zeros((H, W), bool)
    if knee:
        m |= capsule(*joint, *knee, r_top)
        m |= capsule(*knee, *paw, r_bot)
    else:
        m |= capsule(*joint, *paw, r_bot + 0.2)
    m |= ellipse(paw[0] + 0.5, paw[1] + 0.1, 1.55, 1.0)
    return m


def head_parts(hx, hy, rot_deg=0.0, ear_back=0.0, ear_twitch=0.0):
    head = ellipse(hx, hy, 5.3, 4.4, rot_deg)
    mx, my = rot(hx + 3.0, hy + 1.6, hx, hy, rot_deg)
    head |= ellipse(mx, my, 2.6, 1.9, rot_deg)
    # cheek fluff
    cx_, cy_ = rot(hx - 1.5, hy + 2.4, hx, hy, rot_deg)
    head |= ellipse(cx_, cy_, 3.4, 1.9, rot_deg)

    def ear(b1, b2, tip, extra):
        pts = [rot(*b1, hx, hy, rot_deg), rot(*b2, hx, hy, rot_deg)]
        tx, ty = tip
        tx -= ear_back
        ty += ear_back * 0.6 + extra
        pts.append(rot(tx, ty, hx, hy, rot_deg))
        return polygon([pts[0], pts[2], pts[1]]), pts

    far_ear, _ = ear((hx - 4.4, hy - 1.6), (hx - 1.0, hy - 3.9), (hx - 3.6, hy - 8.2), 0)
    near_ear, pts = ear((hx - 0.4, hy - 4.2), (hx + 3.6, hy - 2.8), (hx + 1.6, hy - 8.4), ear_twitch)
    # inner ear: shrink triangle toward its centroid
    gx = sum(p[0] for p in pts) / 3
    gy = sum(p[1] for p in pts) / 3
    inner = polygon([(gx + (p[0] - gx) * 0.45, gy + (p[1] - gy) * 0.45 + 0.6) for p in pts])
    return head, far_ear, near_ear & ~ellipse(hx, hy, 5.0, 4.1, rot_deg) | (near_ear & False), inner


def render(p):
    """p: pose dict. Returns RGBA Image."""
    layers = []  # (name, mask, kind)  kind: far|base|head

    def add(name, m, kind='base'):
        layers.append((name, m, kind))

    bx, by, brx, bry, bang = p['body']
    if p.get('tail'):
        add('tail', chain(p['tail'], p.get('tail_r', (1.6, 1.0))[0], p.get('tail_r', (1.6, 1.0))[1]), 'base')
    for lg in p.get('far_legs', []):
        add('farleg', leg(*lg), 'far')

    hx, hy = p['head']
    hrot = p.get('head_rot', 0.0)
    head, far_ear, near_ear_outer, inner_ear = head_parts(hx, hy, hrot, p.get('ear_back', 0.0),
                                                          p.get('ear_twitch', 0.0))
    add('farear', far_ear, 'far')

    body = ellipse(bx, by, brx, bry, bang)
    for extra in p.get('body_extra', []):
        body |= ellipse(*extra)
    add('body', body, 'base')
    if p.get('haunch'):
        add('haunch', ellipse(*p['haunch']), 'rim')
    for lg in p.get('near_legs', []):
        add('nearleg', leg(*lg), 'base')
    for lg in p.get('front_legs_over', []):
        add('nearleg2', leg(*lg), 'sep')
    # near ear full triangle (behind head drawing order ok)
    _, _, near_ear_full, _ = head_parts(hx, hy, hrot, p.get('ear_back', 0.0), p.get('ear_twitch', 0.0))
    ne = polygon_cache = None
    hd = head
    add('nearear', near_ear_mask(hx, hy, hrot, p), 'base')
    add('head', hd, 'sep')

    # ---------------- composite
    img = np.zeros((H, W, 3), np.uint8)
    owner = np.full((H, W), -1)
    for i, (name, m, kind) in enumerate(layers):
        owner[m] = i
    sil = owner >= 0
    for i, (name, m, kind) in enumerate(layers):
        mine = owner == i
        if not mine.any():
            continue
        col = np.array(C['far'] if kind == 'far' else C['base'])
        img[mine] = col
        if kind != 'far':
            up_out = mine & ~nb(sil, 1, 0)
            dn_out = mine & ~nb(sil, -1, 0)
            img[dn_out] = C['shade']
            img[up_out] = C['hi']
        if kind == 'rim':
            up_other = mine & ~nb(mine, 1, 0) & nb(sil, 1, 0)
            img[up_other] = C['hi']
        if kind == 'sep':
            earlier = np.isin(owner, [j for j in range(i) if layers[j][2] != 'far' or True])
            border = mine & (nb(earlier & ~mine, 0, 1) | nb(earlier & ~mine, 0, -1) |
                             nb(earlier & ~mine, 1, 0) | nb(earlier & ~mine, -1, 0))
            # only along the lower half (chin / leg front) to avoid cluttering
            img[border & (YY > hy + 1)] = C['ol'] if name == 'head' else C['shade']

    # belly shade: lower part of body darker
    bodyidx = [i for i, l in enumerate(layers) if l[0] == 'body'][0]
    belly = (owner == bodyidx) & (YY > by + bry * 0.35)
    img[belly & ~(np.all(img == C['hi'], axis=2))] = C['shade']

    # inner ear
    ne_i = [i for i, l in enumerate(layers) if l[0] == 'nearear'][0]
    img[inner_ear & (owner == ne_i)] = C['ear']

    rgba = np.zeros((H, W, 4), np.uint8)
    rgba[..., :3] = img
    rgba[..., 3] = sil * 255

    face(rgba, hx, hy, hrot, p, sil)
    collar2(rgba, layers, owner, p)

    # outer outline
    sil2 = rgba[..., 3] > 0
    ol = grow(sil2) & ~sil2
    rgba[ol] = (*C['ol'], 255)
    for (zx, zy, zs) in p.get('zs', []):
        draw_z(rgba, zx, zy, zs)
    return Image.fromarray(rgba, 'RGBA')


def near_ear_mask(hx, hy, hrot, p):
    eb = p.get('ear_back', 0.0)
    et = p.get('ear_twitch', 0.0)
    b1 = rot(hx - 0.4, hy - 4.2, hx, hy, hrot)
    b2 = rot(hx + 3.6, hy - 2.8, hx, hy, hrot)
    t = rot(hx + 1.6 - eb, hy - 8.4 + eb * 0.6 + et, hx, hy, hrot)
    return polygon([b1, t, b2])


def put(rgba, x, y, c):
    x, y = int(round(x)), int(round(y))
    if 0 <= x < W and 0 <= y < H:
        rgba[y, x] = (*c, 255)


def face(rgba, hx, hy, hr, p, sil):
    eyes = p.get('eyes', 'open')
    R = lambda x, y: rot(hx + x, hy + y, hx, hy, hr)
    # eyes: near eye (2 wide), far eye (1 wide, nearer the muzzle edge)
    for ex, w in ((0.6, 2), (3.6, 1)):
        x0, y0 = R(ex, -0.6)
        x0, y0 = int(math.floor(x0)), int(math.floor(y0))
        if eyes == 'open':
            if w == 2:
                put(rgba, x0, y0, C['eye']); put(rgba, x0 + 1, y0, C['pupil'])
                put(rgba, x0, y0 + 1, C['eyeD']); put(rgba, x0 + 1, y0 + 1, C['pupil'])
            else:
                put(rgba, x0, y0, C['eye']); put(rgba, x0, y0 + 1, C['pupil'])
        elif eyes == 'half':
            for dx in range(w):
                put(rgba, x0 + dx, y0 + 1, C['eye'])
            put(rgba, x0 + w - 1, y0 + 1, C['pupil'])
        elif eyes == 'happy':   # ^ ^
            put(rgba, x0, y0 + 1, C['ol'])
            if w == 2:
                put(rgba, x0 + 1, y0, C['ol'])
                put(rgba, x0 + 2, y0 + 1, C['ol'])
        else:  # closed
            for dx in range(w + (1 if w == 2 else 0)):
                put(rgba, x0 + dx - (0 if w == 2 else 0), y0 + 1, C['ol'])
    nx, ny = R(5.4, 0.9)
    put(rgba, nx, ny, C['nose'])
    mouth = p.get('mouth', 'closed')
    if mouth == 'open' or mouth == 'wide':
        h = 2 if mouth == 'open' else 3
        for j in range(h):
            for i in range(2 if j < h - 1 else 1):
                put(rgba, *R(4.0 + i, 2.3 + j), C['mouth'])
        put(rgba, *R(4.0, 2.3 + h - 1), C['tongue'])
    if p.get('tongue'):
        tx, ty = R(5.3, 2.6)
        put(rgba, tx, ty, C['tongue'])
        put(rgba, tx + 1, ty, C['tongue'])
        put(rgba, tx + 1, ty + 1, C['ol'])
    # whiskers (single light pixels beyond cheek)
    if p.get('whiskers', True):
        for (x, y) in ((6.6, 1.4), (6.4, 2.6)):
            wx, wy = R(x, y)
            xi, yi = int(round(wx)), int(round(wy))
            if 0 <= xi < W and 0 <= yi < H and rgba[yi, xi, 3] == 0:
                rgba[yi, xi] = (*C['hi'], 255)


def collar(rgba, p, sil):
    hx, hy = p['head']
    sx, sy = p['neck']
    dx, dy = hx - sx, hy - sy
    L = math.hypot(dx, dy) or 1
    dx, dy = dx / L, dy / L
    nx_, ny_ = sx + dx * p.get('collar_t', 0.45) * L, sy + dy * p.get('collar_t', 0.45) * L
    along = (XX - nx_) * dx + (YY - ny_) * dy
    across = np.abs(-(XX - nx_) * dy + (YY - ny_) * dx)
    band = sil & (np.abs(along) <= 0.85) & (across <= 4.6)
    rgba[band] = (*C['collar'], 255)
    lower = band & (along > 0.2)
    rgba[lower] = (*C['collarD'], 255)
    # bell on the front/lower side of the neck
    px_, py_ = -dy, dx
    if px_ < 0:
        px_, py_ = -px_, -py_
    bxx, byy = nx_ + px_ * 3.2 + dx * -0.6, ny_ + py_ * 3.2 + dy * -0.6
    bxx, byy = int(round(bxx)), int(round(byy)) + 1
    for (x, y, c) in ((bxx, byy, 'bell'), (bxx + 1, byy, 'bell'), (bxx, byy + 1, 'bellD'),
                      (bxx + 1, byy + 1, 'bellD')):
        if 0 <= x < W and 0 <= y < H:
            rgba[y, x] = (*C[c], 255)
    if 0 <= byy < H and 0 <= bxx < W:
        rgba[byy, bxx] = (255, 240, 176, 255)


def draw_z(rgba, x, y, s):
    pat = ["####", "..#.", ".#..", "####"] if s == 2 else ["###", ".#.", "###"]
    pat = ["####", "..#.", ".#..", "####"] if s == 2 else ["###", "..#", "#..", "###"][0:1] + ["..#", ".#.", "###"] if False else pat
    pat = ["####", "..#.", ".#..", "####"] if s == 2 else ["###", ".#.", "###"]
    if s == 1:
        pat = ["###", "..#", ".#.", "###"][0:0] or ["##.", ".#.", ".##"][0:0] or ["###", ".#.", "###"]
        pat = ["###", "..#", ".#.", "#..", "###"][0:0] or ["###", "..#", "###"][0:0] or pat
        pat = ["###", ".##", "##.", "###"][0:0] or ["###", "..#", ".#.", "###"]
    for j, row in enumerate(pat):
        for i, c in enumerate(row):
            if c == '#':
                xx, yy = int(x) + i, int(y) + j
                if 0 <= xx < W and 0 <= yy < H:
                    rgba[yy, xx] = (*C['base'], 255)



def collar2(rgba, layers, owner, p):
    if p.get('no_collar'):
        return
    hi_ = [i for i, l in enumerate(layers) if l[0] == 'head'][0]
    headm = layers[hi_][1]
    hx, hy = p['head']
    bodyish = np.isin(owner, [i for i, l in enumerate(layers) if l[0] in ('body', 'nearear') or l[0] == 'body'])
    bodyish = np.isin(owner, [i for i, l in enumerate(layers) if l[0] == 'body'])
    g1 = grow(headm)
    g2 = grow(g1)
    band = bodyish & g2 & ~headm
    if not band.any():
        return
    rgba[band & g1] = (*C['collar'], 255)
    rgba[band & ~g1] = (*C['collarD'], 255)
    ys, xs = np.nonzero(band)
    k = np.argmax(ys * 1.0 + xs * 0.7)
    bx, by = xs[k] - 1, ys[k]
    for (x, y, c) in ((bx, by, 'bell'), (bx + 1, by, 'bell'), (bx, by + 1, 'bellD'), (bx + 1, by + 1, 'bellD')):
        if 0 <= x < W and 0 <= y < H:
            rgba[y, x] = (*C[c], 255)
    if 0 <= by < H and 0 <= bx < W:
        rgba[by, bx] = (255, 240, 176, 255)

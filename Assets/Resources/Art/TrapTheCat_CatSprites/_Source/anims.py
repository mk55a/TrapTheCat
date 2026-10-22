import math
import os
import sys
sys.path.insert(0, '/tmp/cat')
import numpy as np
from PIL import Image
import cat
from cat import render, W, H, leg, ellipse, grow, nb, C
from poses import stand, sit, lay, G

TAU = math.tau
N = 8

# ---------- allow legs drawn in front of the head (licking paw)
_orig_render = cat.render


def render2(p):
    img = _orig_render(p)
    if not p.get('paw_over'):
        return img
    a = np.array(img)
    joint, paw = p['paw_over']
    m = leg(joint, paw, 1.6, 1.25)
    # don't cover the eye: paw sits below/in front of muzzle
    sil = a[..., 3] > 0
    a[m] = (*C['base'], 255)
    up = m & ~nb(m, 1, 0)
    a[up] = (*C['hi'], 255)
    border = grow(m) & ~m
    a[border] = (*C['ol'], 255)
    if p.get('tongue'):
        hx, hy = p['head']
        tx, ty = cat.rot(hx + 5.0, hy + 2.6, hx, hy, p.get('head_rot', 0))
        for dx, dy in ((0, 0), (1, 0), (1, 1)):
            x, y = int(round(tx)) + dx, int(round(ty)) + dy
            if 0 <= x < W and 0 <= y < H:
                a[y, x] = (*C['tongue'], 255)
    return Image.fromarray(a, 'RGBA')


def quad(bx, by, rx, bang, paws, head, head_rot=0.0, tail=None, knees=None, **kw):
    sx, hipx = bx + rx * 0.74, bx - rx * 0.62
    nf, ff, nh, fh = paws
    kn = knees or {}
    p = dict(
        body=(bx, by, rx, 4.2, bang),
        head=head, head_rot=head_rot,
        neck=(sx, by - 1),
        far_legs=[((sx - 1.6, by + 1.4), ff),
                  ((hipx + 1.4, by + 1.0), fh, 1.7, 1.2, kn.get('fh', (fh[0] + 1.2, by + 4.2)))],
        near_legs=[((sx, by + 1.6), nf),
                   ((hipx, by + 1.0), nh, 2.2, 1.25, kn.get('nh', (nh[0] + 1.4, by + 4.6)))],
        haunch=(hipx + 0.4, by + 0.6, 3.4, 3.6, bang),
        tail=tail,
    )
    p.update(kw)
    return p


# ---------------------------------------------------------------- animations

def idle():
    out = []
    for f in range(N):
        t = TAU * f / N
        out.append(stand(t_tail=t, breathe=0.45 * max(0, math.sin(t)),
                         eyes={5: 'half', 6: 'closed'}.get(f, 'open'),
                         ear_twitch={2: 1.4, 3: 0.6}.get(f, 0.0)))
    return out


def sitting():
    out = []
    for f in range(N):
        t = TAU * f / N
        out.append(sit(t_tail=t, breathe=0.4 * max(0, math.sin(t)),
                       head_dy=0.5 if f in (3, 4) else 0.0,
                       eyes={5: 'half', 6: 'closed'}.get(f, 'open'),
                       ear_twitch={1: 1.2}.get(f, 0.0)))
    return out


def laying():
    out = []
    for f in range(N):
        t = TAU * f / N
        out.append(lay(breathe=0.5 * max(0, math.sin(t)), t_tail=t,
                       eyes={3: 'half', 4: 'closed', 5: 'half'}.get(f, 'open'),
                       ear_twitch={6: 1.3}.get(f, 0.0)))
    return out


def sleeping():
    out = []
    for f in range(N):
        t = TAU * f / N
        zs = []
        for off, size in ((0, 1), (4, 2)):
            k = (f + off) % N
            zs.append((24 + k * 0.8, 10.5 - k * 1.25, size))
        out.append(lay(breathe=0.6 * max(0, math.sin(t)), t_tail=t * 0.5,
                       head=(23.0, 22.4), head_rot=14.0, eyes='closed', ear_back=1.0,
                       neck=(19.5, 23.0), zs=zs, whiskers=False))
    return out


def walk():
    out = []
    phases = dict(nh=0.0, nf=0.25, fh=0.5, ff=0.75)
    base = dict(nf=20.2, ff=17.8, nh=9.0, fh=11.0)
    for f in range(N):
        bob = 0.55 * math.cos(TAU * 2 * f / N)
        by = 20.6 + bob
        pos = {}
        for k, ph in phases.items():
            th = TAU * (f / N - ph)
            x = base[k] - 2.6 * math.cos(th)
            lift = 1.9 * max(0.0, math.sin(th))
            pos[k] = (x, G - lift)
        sway = math.sin(TAU * f / N)
        tail = [(7.6, by - 1.5), (4.6, by - 3.0), (3.2 + sway * 0.4, by - 7.2),
                (3.8 + sway, by - 10.8), (6.0 + sway * 1.2, by - 12.0)]
        out.append(quad(14.0, by, 7.2, 0.0, (pos['nf'], pos['ff'], pos['nh'], pos['fh']),
                        (23.4, 14.8 + bob * 0.6), tail=tail))
    return out


def run():
    out = []
    for f in range(N):
        th = TAU * f / N
        stretch = -math.cos(th)                     # +1 extended, -1 gathered
        rx = 7.2 + 1.1 * stretch
        by = 20.2 - 1.3 * max(0.0, math.sin(th + 0.6))
        bang = -5.0 * math.sin(th)
        bx = 14.8
        sx, hipx = bx + rx * 0.74, bx - rx * 0.62

        def foot(cx, a, lift_amp):
            return (cx + 4.4 * (-math.cos(a)), G - lift_amp * max(0.0, math.sin(a)) - 0.6 * max(0, by < 19.5))

        nf = foot(sx + 0.8, th, 3.0)
        ff = foot(sx - 1.0, th + 0.55, 3.0)
        nh = foot(hipx - 0.5, th + math.pi, 2.4)
        fh = foot(hipx + 1.0, th + math.pi + 0.55, 2.4)
        wave = math.sin(th * 2)
        tail = [(bx - rx + 0.8, by - 1.6), (bx - rx - 2.4, by - 3.0 + wave * 0.4),
                (bx - rx - 4.4, by - 4.6 + wave), (bx - rx - 5.6, by - 6.2 + wave * 1.4)]
        hx = sx + 4.6
        out.append(quad(bx, by, rx, bang, (nf, ff, nh, fh), (hx, by - 5.2 + 0.4 * stretch),
                        head_rot=4.0, tail=tail, ear_back=1.2,
                        knees=dict(nh=((hipx + nh[0]) / 2 + 2.0, by + 4.2),
                                   fh=((hipx + fh[0]) / 2 + 2.4, by + 4.0))))
    return out


def licking():
    # (paw position or None, head_rot, head_dy, tongue)
    keys = [
        (None, 0, 0.0, False),
        ((20.6, 23.0), 4, 0.4, False),
        ((21.4, 18.6), 10, 1.0, False),
        ((21.4, 18.2), 16, 1.6, True),
        ((21.4, 18.4), 10, 1.0, False),
        ((21.4, 18.2), 16, 1.6, True),
        ((21.4, 18.6), 10, 1.0, False),
        ((20.6, 23.0), 4, 0.4, False),
    ]
    out = []
    for f, (paw, hr, hdy, tongue) in enumerate(keys):
        p = sit(t_tail=TAU * f / N * 0.5, head_rot=hr, head_dy=hdy, head_dx=0.6 if paw else 0.0,
                eyes='closed' if tongue else ('half' if paw else 'open'), tongue=False)
        if paw:
            p['near_legs'] = [p['near_legs'][1]]
            p['paw_over'] = ((18.0, 21.4), paw)
            p['tongue'] = tongue
        out.append(p)
    return out


def meow():
    keys = [
        dict(),
        dict(head_rot=-6, eyes='half'),
        dict(head_rot=-12, eyes='closed', mouth='open', ear_back=0.6),
        dict(head_rot=-16, eyes='closed', mouth='wide', ear_back=1.2),
        dict(head_rot=-16, eyes='closed', mouth='wide', ear_back=1.2),
        dict(head_rot=-12, eyes='closed', mouth='open', ear_back=0.6),
        dict(head_rot=-5, eyes='half'),
        dict(),
    ]
    out = []
    for f, k in enumerate(keys):
        out.append(stand(t_tail=TAU * f / N * 0.6, head_dy=-0.6 if 2 <= f <= 5 else 0.0, **k))
    return out


ANIMS = [('Cat-Idle', idle, 8), ('Cat-Laying', laying, 6), ('Cat-Licking', licking, 8),
         ('Cat-Meow', meow, 10), ('Cat-Run', run, 12), ('Cat-Sitting', sitting, 6),
         ('Cat-Sleeping', sleeping, 4), ('Cat-Walk', walk, 10)]

if __name__ == '__main__':
    os.makedirs('/tmp/cat/out/sheets', exist_ok=True)
    os.makedirs('/tmp/cat/out/gifs', exist_ok=True)
    rows = []
    for name, fn, fps in ANIMS:
        frames = [render2(p) for p in fn()]
        sheet = Image.new('RGBA', (W * N, H), (0, 0, 0, 0))
        for i, fr in enumerate(frames):
            sheet.alpha_composite(fr, (i * W, 0))
        sheet.save(f'/tmp/cat/out/sheets/{name}.png')
        rows.append((name, sheet))
        S = 6
        gif = []
        for fr in frames:
            bg = Image.new('RGBA', (W, H), (243, 233, 210, 255))
            bg.alpha_composite(fr)
            gif.append(bg.convert('RGB').resize((W * S, H * S), Image.NEAREST))
        gif[0].save(f'/tmp/cat/out/gifs/{name}.gif', save_all=True, append_images=gif[1:],
                    duration=int(1000 / fps), loop=0)
    S = 5
    contact = Image.new('RGBA', (W * N * S, H * S * len(rows)), (243, 233, 210, 255))
    for r, (name, sh) in enumerate(rows):
        contact.alpha_composite(sh.resize((W * N * S, H * S), Image.NEAREST), (0, r * H * S))
    contact.save('/tmp/cat/contact.png')
    print('ok')

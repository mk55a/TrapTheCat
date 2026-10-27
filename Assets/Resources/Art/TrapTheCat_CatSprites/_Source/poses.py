import math
import sys
sys.path.insert(0, '/tmp/cat')
from cat import render, W, H

G = 28.0  # paw centre y (paw bottom row = 29)
TAU = math.tau


def stand(t_tail=0.0, bob=0.0, breathe=0.0, head_dx=0.0, head_dy=0.0, head_rot=0.0, **kw):
    by = 20.6 + bob
    tail_sway = 1.6 * math.sin(t_tail)
    p = dict(
        body=(14.0, by, 7.2, 4.2 + breathe, 0.0),
        head=(23.2 + head_dx, 14.6 + bob + head_dy),
        head_rot=head_rot,
        neck=(19.5, by - 1.0),
        far_legs=[((17.6, by + 1.5), (17.8, G)), ((10.6, by + 1.0), (11.2, G), 1.7, 1.2, (11.8, by + 4.2))],
        near_legs=[((19.4, by + 1.6), (20.0, G)), ((9.2, by + 1.0), (9.0, G), 2.2, 1.25, (10.4, by + 4.6))],
        haunch=(9.6, by + 0.8, 3.4, 3.6, 0),
        tail=[(7.6, by - 1.5), (4.8, by - 3.5), (3.4 + tail_sway * 0.4, by - 8.0),
              (4.2 + tail_sway, by - 11.5), (6.4 + tail_sway * 1.2, by - 12.6)],
    )
    p.update(kw)
    return p


def sit(t_tail=0.0, head_dx=0.0, head_dy=0.0, head_rot=0.0, breathe=0.0, **kw):
    sway = math.sin(t_tail)
    p = dict(
        body=(15.0, 20.5, 6.6, 4.3 + breathe, -62.0),
        body_extra=[(16.6, 18.0, 3.4, 4.0, -20)],
        head=(18.6 + head_dx, 11.2 + head_dy),
        head_rot=head_rot,
        neck=(16.8, 17.0),
        haunch=(12.0, 24.4, 4.8, 4.2, -10),
        far_legs=[((16.2, 21.0), (16.6, G))],
        near_legs=[((18.2, 21.0), (18.8, G)), ((11.0, 26.0), (14.6, G), 1.6, 1.3, (10.4, 27.0))],
        tail=[(9.0, 27.0), (6.0, 28.0), (5.2 + 0.3 * sway, 28.6), (9.0 + sway, 29.2), (13.0 + sway * 1.6, 28.8 - abs(sway))],
        tail_r=(1.6, 1.2),
    )
    p.update(kw)
    return p


def lay(breathe=0.0, head_dx=0.0, head_dy=0.0, head_rot=0.0, t_tail=0.0, **kw):
    sway = math.sin(t_tail)
    p = dict(
        body=(13.6, 24.2, 8.0, 3.9 + breathe, 0.0),
        head=(23.6 + head_dx, 19.0 + head_dy),
        head_rot=head_rot,
        neck=(20.0, 22.0),
        haunch=(8.4, 24.6, 3.6, 3.4, 0),
        far_legs=[((19.0, 26.5), (23.2, G))],
        near_legs=[((18.0, 27.2), (22.0, G + 0.2))],
        tail=[(5.6, 25.6), (2.6, 27.0), (2.4 + 0.4 * sway, 28.8), (6.4 + sway, 29.2), (10.0 + sway * 1.5, 29.0)],
        tail_r=(1.6, 1.1),
    )
    p.update(kw)
    return p


if __name__ == '__main__':
    from PIL import Image
    imgs = [render(stand()), render(sit()), render(lay()), render(stand(eyes='closed', mouth='open'))]
    S = 10
    sheet = Image.new('RGBA', (len(imgs) * 34 * S, 34 * S), (243, 233, 210, 255))
    for i, im in enumerate(imgs):
        sheet.alpha_composite(im.resize((W * S, H * S), Image.NEAREST), (i * 34 * S + S, S))
    sheet.save('/tmp/cat/test.png')

"""Rebuild the fourth chapter's static rally marker. Requires Pillow and NumPy."""
from math import cos, pi, sin
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Chapter4/Environment/Rally Marker'
MESSAGE = '在這裡會合'
FRONT = (10, 10, 1013, 329)
BACK = (10, 354, 1013, 673)
BARK = (10, 705, 635, 1013)
CORD = (659, 705, 880, 1013)
CUT = (904, 740, 1013, 849)


def albedo():
    rng = np.random.default_rng(1930)
    width, height = 1024, 343
    yy, xx = np.mgrid[:height, :width]
    cloudy = Image.fromarray(rng.integers(0, 255, (9, 27), dtype=np.uint8)).resize((width, height), Image.Resampling.BICUBIC)
    warp = 1.7 * np.sin(xx * 1.95 + .55 * np.sin(yy * .05))
    weft = 1.9 * np.cos(yy * 2.2 + .5 * np.sin(xx * .02))
    wear = (np.asarray(cloudy, dtype=float) - 128) * .073 + rng.normal(0, 2.3, (height, width))
    edge = 9 * np.exp(-((yy - 321) / 14) ** 2) + 6 * np.exp(-((yy - 17) / 8) ** 2)
    fabric = np.array([222, 204, 168]) + (wear + warp + weft - edge)[:, :, None]
    cloth = Image.fromarray(np.uint8(np.clip(fabric, 0, 255)))
    seams = ImageDraw.Draw(cloth)
    for baseline in (21, 316):
        for start in range(2, width, 13):
            y = baseline + round(1.4 * sin(start * .021))
            seams.line((start, y, start + 6, y), fill=(183, 162, 128), width=2)
    atlas = Image.new('RGB', (1024, 1024), (175, 147, 104))
    atlas.paste(cloth, (0, BACK[1] - FRONT[1]))
    ink = Image.new('L', cloth.size)
    writer = ImageDraw.Draw(ink)
    font = ImageFont.truetype(str(ROOT / 'Assets/中文字體/STKAITI.TTF'), 155)
    for index, character in enumerate(MESSAGE):
        writer.text((512 + (index - (len(MESSAGE) - 1) / 2) * 156, 165 + (1, -2, 1, 0, 2)[index % 5]),
                    character, font=font, anchor='mm', fill=255, stroke_width=1)
    ink = Image.fromarray(np.uint8(np.asarray(ink) * np.clip(.955 + wear * .004, .80, 1)))
    cloth.paste((44, 40, 31), mask=ink)
    atlas.paste(cloth, (0, 0))
    by, bx = np.mgrid[:336, :649]
    cambium = bx + np.sin(by * .025 + bx * .037) * 2.6 + by * .04
    grain = np.sin(cambium * .65) * 5 + np.sin(cambium * .17) * 10 + np.cos(cambium * .053) * 7
    fissures = np.maximum(0, np.cos(cambium * .43 + np.sin(by * .09)) - .69) * 77
    bark = np.array([106, 86, 62]) + (grain - fissures + rng.normal(0, 3, bx.shape))[:, :, None]
    atlas.paste(Image.fromarray(np.uint8(np.clip(bark, 0, 255))), (0, 688))
    cy, cx = np.mgrid[:336, :244]
    twist = 11 * np.sin(cx * .43 + cy * .19) + 4 * np.cos(cx * 1.1 + cy * .44)
    cord = np.array([191, 160, 110]) + (twist + rng.normal(0, 2, cx.shape))[:, :, None]
    atlas.paste(Image.fromarray(np.uint8(np.clip(cord, 0, 255))), (647, 688))
    ey, ex = np.mgrid[:132, :132]
    rings = np.hypot(ex - 63, ey - 74) + 2 * np.sin(ey * .09)
    endgrain = np.array([167, 135, 91]) + (8 * np.cos(rings * .61) + rng.normal(0, 2, ex.shape))[:, :, None]
    atlas.paste(Image.fromarray(np.uint8(np.clip(endgrain, 0, 255))), (892, 731))
    atlas.save(OUT / 'Rally marker albedo.png', optimize=True)


def unit(vector):
    return vector / np.linalg.norm(vector)


def geometry():
    vertices, faces = [], []

    def vertex(point, normal, rect, u, v):
        uv = ((rect[0] + (rect[2] - rect[0]) * u) / 1024, 1 - (rect[3] - (rect[3] - rect[1]) * v) / 1024)
        vertices.append((point, normal, uv))
        return len(vertices)

    def tube(points, radii, region, sides=9):
        points = np.asarray(points, dtype=float)
        rings = []
        axes = []
        for index, (center, radius) in enumerate(zip(points, radii)):
            axis = unit(points[min(index + 1, len(points) - 1)] - points[max(0, index - 1)])
            axes.append(axis)
            if index == 0:
                guide = np.eye(3)[np.argmin(np.abs(axis))]
                side = unit(np.cross(axis, guide))
            else:
                side = unit(side - axis * np.dot(axis, side))
            across = np.cross(axis, side)
            ring = []
            for sector in range(sides + 1):
                angle = 2 * pi * sector / sides
                radial = cos(angle) * side + sin(angle) * across
                thickness = radius * (1 + .075 * sin(3 * angle + .6) if region == BARK else 1)
                ring.append(vertex(center + thickness * radial, radial, region, sector / sides, index / (len(points) - 1)))
            rings.append(ring)
        for low, high in zip(rings, rings[1:]):
            for sector in range(sides):
                faces.append((low[sector], low[sector + 1], high[sector + 1], high[sector]))
        for index in (0, len(points) - 1):
            axis = axes[index] * (-1 if not index else 1)
            cap = [vertex(vertices[ref - 1][0], axis, CUT if region == BARK else region,
                          .5 + .47 * cos(2*pi*sector/sides), .5 + .47 * sin(2*pi*sector/sides))
                   for sector, ref in enumerate(rings[index][:-1])]
            faces.append(tuple(cap if index else reversed(cap)))

    left, right = -.91, .93
    tube([(-1.00,-.12,.09),(-.97,.37,.06),(-.925,.82,.03),(left,1.23,.015),(-.875,1.43,.038)], [.071,.062,.055,.050,.039], BARK)
    tube([(.99,-.12,.10),(.97,.32,.065),(.94,.84,.028),(right,1.16,.015),(.955,1.35,.04)], [.070,.062,.053,.047,.041], BARK)
    tube([(-.928,.82,.032),(-1.012,.91,.055),(-1.072,.994,.07)], [.038,.031,.015], BARK, 7)
    tube([(.94,1.02,.029),(1.011,1.107,.068),(1.044,1.147,.091)], [.034,.025,.016], BARK, 7)

    def surface(u, v):
        tension = sin(pi * u)
        top = 1.225 - .068*u - .071*tension
        bottom = .651 - .069*u - .087*tension + .017*sin(5*pi*u+.2) + .025*max(1-u*9,0)
        x = -.822 + 1.667*u + (1-v)*(.034 + .021*sin(pi*u))
        y = bottom*(1-v) + top*v
        z = -.013 - tension * (.023 + .058*(1-v)) + (1-v)*(.028*sin(6*pi*u+.45) + .013*cos(11*pi*u))
        return np.array([x, y, z])

    for back in (False, True):
        rows = []
        for row in range(6):
            v = row/5
            refs = []
            for col in range(25):
                u = col/24
                point = surface(u,v)
                normal = unit(np.cross(surface(u,v+.001)-surface(u,v-.001), surface(u+.001,v)-surface(u-.001,v)))
                if back:
                    point[2] += .005
                    normal = -normal
                refs.append(vertex(point, normal, BACK if back else FRONT, 1-u if back else u, v))
            rows.append(refs)
        for low, high in zip(rows, rows[1:]):
            for col in range(24):
                face = (low[col], high[col], high[col+1], low[col+1])
                faces.append(face[::-1] if back else face)

    for left_end, center_x, height in ((True,left,1.224),(False,right,1.158)):
        sign = 1 if left_end else -1
        spiral = []
        for step in range(42):
            angle = 2*pi * step/14
            spiral.append((center_x + .056*cos(angle), height+.018-.043*step/41, .015+.054*sin(angle)))
        tube(spiral, [.009]*len(spiral), CORD, 6)
        corner = surface(0 if left_end else 1, 1)
        tube([(center_x,height+.012,-.034),(center_x+sign*.059,height+.006,-.030),corner,
              corner+[sign*.026,-.021,-.004]], [.014,.013,.012,.010], CORD, 7)
        tube([(center_x-sign*.028,height-.014,-.033),(center_x-sign*.040,height-.086,-.05),
              (center_x-sign*.066,height-.131,-.038)], [.012,.010,.006], CORD, 7)

    with (OUT/'Rally marker.obj').open('w', encoding='ascii', newline='\n') as obj:
        obj.write('o Rally_marker\n')
        for (x,y,z),_,_ in vertices:
            obj.write(f'v {-x:.5f} {y:.5f} {z:.5f}\n')
        for _,_,(u,v) in vertices:
            obj.write(f'vt {u:.6f} {v:.6f}\n')
        for _,(x,y,z),_ in vertices:
            obj.write(f'vn {-x:.5f} {y:.5f} {z:.5f}\n')
        for face in faces:
            obj.write('f '+' '.join(f'{ref}/{ref}/{ref}' for ref in reversed(face))+'\n')


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    albedo()
    geometry()

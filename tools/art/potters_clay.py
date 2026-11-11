"""Potter's Clay, the Gilded Vale's throwing clay (docs/design/pottery_minigame.md, section 8):
a 16x16 item icon of a wet, grey-brown lump, drawn from scratch. A PLACEHOLDER until the
user picks real art.

The lump: a low body with a rounder top where it was slapped into shape, a wire-cut flat
face on top (lighter, catching the light), lit from the upper left as the game's icons are,
with a wet sheen of two or three near-white pixels. The outline is a darker shade of the
colour it borders (vanillafy.outline_shade's way, cooler here, since clay isn't wood), never
black.

    python tools/art/potters_clay.py [preview.png]

Writes the content pack's assets/PottersClay.png, and with a path, a preview (8x and 3x, on
a dark and a light ground).
"""
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "[CP] Gilded Vale (dev)", "assets", "PottersClay.png")

# wet grey-brown, darkest to lightest
TONES = [(86, 66, 56), (109, 86, 72), (134, 109, 92), (160, 135, 115), (186, 163, 141)]
SHEEN = (232, 222, 206)

FACE = (8.0, 7.4, 6.2, 2.3)          # the wire-cut top: centre x, y, radii


def half_width(cy):
    """The lump's half-width at a row: a soft block, bulging a little at the middle."""
    if cy < FACE[1]:
        return None
    t = (cy - FACE[1]) / (14.6 - FACE[1])
    return FACE[2] + 0.9 * (1 - (2 * t - 1) ** 2) - (1.2 if t > 0.86 else 0)


def in_face(cx, cy):
    return ((cx - FACE[0]) / FACE[2]) ** 2 + ((cy - FACE[1]) / FACE[3]) ** 2 <= 1


def inside(x, y):
    cx, cy = x + 0.5, y + 0.5
    if in_face(cx, cy):
        return True
    hw = half_width(cy)
    return hw is not None and cy <= 14.6 and abs(cx - FACE[0]) <= hw


def tone(x, y):
    """Light from the upper left: the cut face on top lightest, the body's left side lit, its
    right side in shade, and the row it sits on darkest."""
    cx, cy = x + 0.5, y + 0.5
    if in_face(cx, cy):
        return TONES[4] if cx < FACE[0] + 2.2 else TONES[3]
    hw = half_width(cy) or 1
    u = (cx - FACE[0]) / hw                       # -1 left edge .. 1 right edge
    if cy > 13.6:
        return TONES[0]
    if u < -0.45:
        return TONES[3]
    if u < 0.35:
        return TONES[2]
    return TONES[1]


def outline_shade(c):
    r, g, b = c
    return (int(r * 0.6), int(g * 0.56), int(b * 0.56))


def draw():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    for y in range(16):
        for x in range(16):
            if inside(x, y):
                px[x, y] = tone(x, y) + (255,)
    # the cut face's front rim: the body's first row under it, a tone darker, so the flat top reads
    for y in range(1, 16):
        for x in range(16):
            if px[x, y][3] and not in_face(x + 0.5, y + 0.5) and in_face(x + 0.5, y - 0.5):
                c = px[x, y][:3]
                i = TONES.index(c) if c in TONES else 2
                px[x, y] = TONES[max(0, i - 1)] + (255,)
    # the wire's drag marks across the cut face: two short lines a tone darker
    for p in [(6, 7), (7, 7), (8, 7), (9, 8), (10, 8), (11, 8)]:
        c = px[p][:3]
        if px[p][3] and in_face(p[0] + 0.5, p[1] + 0.5) and c in TONES:
            px[p] = TONES[TONES.index(c) - 1] + (255,)
    # the wet sheen on the cut face's upper left, and a glint lower on the body
    for p in [(3, 7), (4, 6), (5, 6), (2, 10), (2, 11)]:
        if px[p][3]:
            px[p] = SHEEN + (255,)
    # two thumb dents on the side, a tone darker
    for p in [(9, 11), (9, 12), (12, 12)]:
        c = px[p]
        if c[3]:
            i = TONES.index(c[:3]) if c[:3] in TONES else 2
            px[p] = TONES[max(0, i - 1)] + (255,)
    # the outline: each empty pixel touching the lump takes a darker shade of what it touches
    edge = {}
    for y in range(16):
        for x in range(16):
            if px[x, y][3]:
                continue
            ring = [px[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < 16 and 0 <= y + dy < 16 and px[x + dx, y + dy][3]]
            if ring:
                base = min((q[:3] for q in ring), key=lambda c: sum(c))   # its darkest neighbour
                if base == SHEEN:
                    base = TONES[3]
                edge[(x, y)] = outline_shade(base) + (255,)
    for k, v in edge.items():
        px[k] = v
    return im


def preview(im, path):
    sheet = Image.new("RGBA", (16 * 8 + 16 * 3 + 40, 16 * 8 * 2 + 8), (0, 0, 0, 0))
    for row, ground in enumerate([(40, 34, 48, 255), (232, 214, 170, 255)]):
        y = row * (16 * 8 + 8)
        bg = Image.new("RGBA", (sheet.width, 16 * 8), ground)
        bg.alpha_composite(im.resize((128, 128), Image.NEAREST), (0, 0))
        bg.alpha_composite(im.resize((48, 48), Image.NEAREST), (136, 40))
        bg.alpha_composite(im, (192, 56))
        sheet.alpha_composite(bg, (0, y))
    sheet.save(path)


if __name__ == "__main__":
    lump = draw()
    lump.save(OUT)
    print("wrote", os.path.normpath(OUT))
    if len(sys.argv) > 1:
        preview(lump, sys.argv[1])
        print("preview", sys.argv[1])

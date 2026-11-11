"""The Slaking Tub, a big craftable (16x32) for the Gilded Vale's pottery: potters slake dry,
rough clay in water and sieve the grit out, and what settles is throwing clay. A PLACEHOLDER
until the user picks real art.

Two frames, side by side, as the game's machines have them (Data/BigCraftables SpriteIndex 0,
ShowNextIndexWhileWorking / WhenReady showing index 1):
  0  idle: the empty tub, a little clear water in the bottom, the round sieve propped on its rim
  1  in use (and ready): the tub full of grey slip, a bubble, slip dripping through the sieve

The tub is staved wood with two copper hoops (the recipe's Copper Bar), lit from the upper
left. Outlines are a darker shade of the colour they border, never black. Nothing is drawn
round it: it stands on whatever floor it's put on.

    python tools/art/slaking_tub.py [preview.png]

Writes the content pack's assets/SlakingTub.png, and with a path, a preview (each frame at 8x
and 3x, on grass and on a floor).
"""
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "[CP] Gilded Vale (dev)", "assets", "SlakingTub.png")

WOOD = [(96, 58, 34), (124, 78, 45), (152, 100, 60), (180, 126, 80)]   # dark .. light
WOOD_IN = (78, 48, 30)                                                  # the tub's inside wall
COPPER = [(140, 74, 44), (184, 104, 62), (222, 146, 96)]
SLIP = [(122, 118, 112), (150, 146, 138), (180, 176, 168), (214, 210, 202)]
WATER = [(70, 96, 104), (104, 138, 146)]
SIEVE_RIM = [(132, 92, 56), (170, 124, 80)]
MESH = (164, 156, 140)

RIM_Y, BOTTOM_Y = 18, 30          # the rim's centre row and the tub's bottom row
CX = 8.0


def half_width(y):
    """Staved tubs taper a little towards the foot."""
    t = (y - RIM_Y) / (BOTTOM_Y - RIM_Y)
    return 7.0 - 0.9 * t


def outline_shade(c):
    r, g, b = c[:3]
    return (int(r * 0.58), int(g * 0.52), int(b * 0.52), 255)


def draw(working):
    im = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    px = im.load()

    def put(x, y, c):
        if 0 <= x < 16 and 0 <= y < 32:
            px[x, y] = tuple(c[:3]) + (255,)

    # the sieve, propped against the back of the rim (drawn first: the tub's front is over it)
    sx, sy, r = 8.0, 12.0, 4.8
    for y in range(5, 20):
        for x in range(16):
            d = ((x + 0.5 - sx) ** 2 + (y + 0.5 - sy) ** 2) ** 0.5
            if d <= r:
                if d > r - 1.2:
                    put(x, y, SIEVE_RIM[1] if (x + 0.5 < sx and y + 0.5 < sy + 1) else SIEVE_RIM[0])
                else:
                    # the mesh: a fine grid over the dark behind it
                    put(x, y, (112, 104, 92) if (x % 2 == 0 and y % 2 == 0) else MESH)
    if working:
        for x, y in [(6, 11), (7, 11), (8, 12), (9, 11), (10, 13)]:
            put(x, y, SLIP[2])                     # slip caught in the mesh

    # the body: staves (a lighter one every third column), lit on the left
    for y in range(RIM_Y, BOTTOM_Y + 1):
        hw = half_width(y)
        for x in range(16):
            u = (x + 0.5 - CX) / hw
            if abs(u) > 1:
                continue
            tone = 2 if u < -0.35 else 1 if u < 0.45 else 0
            if x % 3 == 0 and tone < 3:
                tone = min(3, tone + 1) if u < 0 else tone   # the stave joints catch the light on the lit side
            if y == BOTTOM_Y:
                tone = 0
            put(x, y, WOOD[tone])
    # two copper hoops
    for hy in (21, 27):
        hw = half_width(hy)
        for x in range(16):
            u = (x + 0.5 - CX) / hw
            if abs(u) <= 1:
                put(x, hy, COPPER[2] if u < -0.4 else COPPER[1] if u < 0.5 else COPPER[0])

    # the rim: an ellipse of the staves' tops, the inside showing
    for y in range(RIM_Y - 3, RIM_Y + 3):
        for x in range(16):
            ex, ey = (x + 0.5 - CX) / 7.0, (y + 0.5 - RIM_Y) / 2.7
            if ex * ex + ey * ey > 1:
                continue
            ix, iy = (x + 0.5 - CX) / 5.8, (y + 0.5 - RIM_Y) / 1.9
            if ix * ix + iy * iy > 1:
                put(x, y, WOOD[3] if y < RIM_Y else WOOD[2])        # the rim's top edge
            elif working:
                put(x, y, SLIP[2] if x < CX else SLIP[1])
            else:
                put(x, y, WOOD_IN if y < RIM_Y else WATER[0])
    if working:
        put(5, RIM_Y - 1, SLIP[3]); put(6, RIM_Y - 1, SLIP[3])        # the sheen on the slip
        put(10, RIM_Y, SLIP[0]); put(10, RIM_Y - 1, SLIP[3])          # a bubble
        put(3, RIM_Y + 2, SLIP[1]); put(3, RIM_Y + 3, SLIP[0])        # a drip down the side
    else:
        put(6, RIM_Y, WATER[1])                                       # light on the water

    # outline: each empty pixel touching the tub takes a darker shade of the darkest it touches
    edge = {}
    for y in range(32):
        for x in range(16):
            if px[x, y][3]:
                continue
            ring = [px[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < 16 and 0 <= y + dy < 32 and px[x + dx, y + dy][3]]
            if ring:
                edge[(x, y)] = outline_shade(min(ring, key=lambda c: sum(c[:3])))
    for k, v in edge.items():
        px[k] = v
    return im


def sheet():
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    out.alpha_composite(draw(False), (0, 0))
    out.alpha_composite(draw(True), (16, 0))
    return out


def preview(sh, path):
    w = 32 * 8 + 32 * 3 + 24
    img = Image.new("RGBA", (w, 32 * 8 * 2 + 8), (0, 0, 0, 0))
    for row, ground in enumerate([(98, 152, 64, 255), (176, 134, 92, 255)]):
        bg = Image.new("RGBA", (w, 32 * 8), ground)
        bg.alpha_composite(sh.resize((256, 256), Image.NEAREST), (0, 0))
        bg.alpha_composite(sh.resize((96, 96), Image.NEAREST), (268, 80))
        img.alpha_composite(bg, (0, row * (32 * 8 + 8)))
    img.save(path)


if __name__ == "__main__":
    sh = sheet()
    sh.save(OUT)
    print("wrote", os.path.normpath(OUT))
    if len(sys.argv) > 1:
        preview(sh, sys.argv[1])
        print("preview", sys.argv[1])

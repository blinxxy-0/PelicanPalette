"""Placeholder icons for the Pottery skill (1.5, Debug only): the skill icon
(16x16), the skills-page icon (10x10) and six profession icons (16x16), drawn
by code. Each is a small pot in a terracotta ramp, shaded as a cylinder lit
from the left, with outlines a darker shade of the colour beside them (the
house rule, vanillafy.outline_shade), never black.

They're placeholders for the user to replace with real art.

    python tools/art/make_pottery_skill_icons.py [out_dir]

Writes to ArtStudio/assets/pottery/skill/ by default (the pottery assets
folder, which Release builds leave out).
"""
import os
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from vanillafy import outline_shade  # noqa: E402

# terracotta, from the lit side round to the shadow
CLAY = [(240, 178, 132), (222, 138, 94), (194, 104, 66), (158, 78, 48)]
BLUE = [(150, 190, 235), (96, 140, 205), (70, 106, 170), (50, 78, 130)]
RED = [(240, 130, 110), (205, 70, 56), (170, 50, 42), (128, 36, 32)]
WHEEL = [(196, 196, 204), (164, 164, 176), (136, 136, 150), (110, 110, 124)]
FLAME = [(255, 236, 140), (250, 190, 70), (236, 124, 44)]
GEM = [(170, 230, 190), (90, 180, 130), (60, 130, 96)]


def shade(ramp, x, cx, w):
    """A cylinder lit from the upper left: highlight, light, mid, shadow."""
    u = (x + 0.5 - cx) / max(w, 1)
    return ramp[0] if u < -0.55 else ramp[1] if u < 0 else ramp[2] if u < 0.55 else ramp[3]


def pot(px, cx, top, profile, ramp=CLAY, bands=None):
    """Rows of a pot from the rim down, each a half-width about cx (pixel
    edges, so the pot is an even number of pixels wide). `bands` maps a row
    index to another ramp (a ring of glaze)."""
    for i, w in enumerate(profile):
        y = top + i
        r = (bands or {}).get(i, ramp)
        for x in range(int(cx - w), int(cx + w)):
            c = shade(r, x, cx, w)
            if i == 0:  # the rim's lighter lip
                c = r[0] if x < cx + w - 1 else r[1]
            px[x, y] = c + (255,)


def outline(im):
    """A ring round every shape, each pixel a darker shade of the colour it
    borders (the most common filled neighbour)."""
    px = im.load()
    w, h = im.size
    ring = {}
    for y in range(h):
        for x in range(w):
            if px[x, y][3]:
                continue
            near = [px[x + dx, y + dy][:3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3]]
            if near:
                ring[(x, y)] = outline_shade(max(set(near), key=near.count)) + (255,)
    for k, v in ring.items():
        px[k] = v
    return im


def blank(size):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    return im, im.load()


VASE = [2, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 3]
CUP = [3, 3, 3, 3, 3, 2, 2]


def skill_icon():
    im, px = blank(16)
    pot(px, 8, 2, VASE)
    return outline(im)


def page_icon():
    im, px = blank(10)
    pot(px, 5, 1, [2, 1, 2, 3, 4, 4, 3, 2])
    return outline(im)


def thrower():
    im, px = blank(16)
    pot(px, 8, 2, [3, 3, 3, 3, 3, 4, 4, 4, 3])
    for x in range(1, 15):  # the wheel head, under it
        px[x, 12] = WHEEL[0 if x < 8 else 1] + (255,)
        px[x, 13] = WHEEL[2 if x < 8 else 3] + (255,)
    return outline(im)


def glazer():
    im, px = blank(16)
    pot(px, 8, 2, VASE, bands={6: BLUE, 7: BLUE, 9: BLUE})
    return outline(im)


def production():
    im, px = blank(16)
    pot(px, 4, 5, CUP + [2])
    pot(px, 12, 5, CUP + [2])
    return outline(im)


def master_of_sets():
    im, px = blank(16)
    for cx in (4, 12):
        for top in (2, 9):
            pot(px, cx, top, [3, 3, 3, 2, 2])
    return outline(im)


def kiln_keeper():
    im, px = blank(16)
    pot(px, 8, 1, [2, 2, 3, 4, 4, 4, 3, 3])
    # three tongues of flame below it
    for x, height in ((5, 4), (8, 5), (11, 4)):
        for k in range(height):
            y = 14 - k
            px[x, y] = FLAME[2 if k < 1 else 1 if k < 3 else 0] + (255,)
            if k < height - 2:
                px[x - 1, y] = FLAME[2] + (255,)
                px[x + 1, y] = FLAME[2] + (255,)
    return outline(im)


def alchemist():
    im, px = blank(16)
    pot(px, 10, 2, [2, 2, 3, 4, 4, 4, 3, 3, 2], bands={1: RED, 2: RED, 3: RED, 4: RED})
    # a common mineral beside it: a little faceted stone
    stone = ["..A..", ".ABB.", "ABBBC", ".BCC.", "..C.."]
    for dy, row in enumerate(stone):
        for dx, ch in enumerate(row):
            if ch != ".":
                px[1 + dx, 9 + dy] = GEM["ABC".index(ch)] + (255,)
    return outline(im)


def main():
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "ArtStudio", "assets", "pottery", "skill")
    os.makedirs(os.path.join(out, "professions"), exist_ok=True)
    skill_icon().save(os.path.join(out, "skill_icon.png"))
    page_icon().save(os.path.join(out, "skill_page_icon.png"))
    # SpaceCore's order: the two at level 5, then the four at level 10
    for i, make in enumerate([thrower, glazer, production, master_of_sets, kiln_keeper, alchemist]):
        make().save(os.path.join(out, "professions", f"{i}.png"))
    # a preview sheet at 8x, to look at
    icons = [Image.open(os.path.join(out, "skill_icon.png")), Image.open(os.path.join(out, "skill_page_icon.png"))]
    icons += [Image.open(os.path.join(out, "professions", f"{i}.png")) for i in range(6)]
    sheet = Image.new("RGBA", (len(icons) * 18 * 8, 18 * 8), (246, 226, 180, 255))
    for i, ic in enumerate(icons):
        big = ic.resize((ic.width * 8, ic.height * 8), Image.NEAREST)
        sheet.alpha_composite(big, (i * 18 * 8 + 8, 8))
    preview = os.path.join(root, ".devgame", "pottery_skill_icons_preview.png") if len(sys.argv) <= 2 else sys.argv[2]
    os.makedirs(os.path.dirname(preview), exist_ok=True)
    sheet.save(preview)
    print("wrote", out, "and", preview)


if __name__ == "__main__":
    main()

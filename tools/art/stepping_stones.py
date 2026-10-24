"""Transparent stepping-stone tiles in the vanilla Forest stones' palette
(sampled from Leah's path), for the art shop's yard. They go on a Back2
layer, so they sit cleanly on light or dark grass alike.
"""
from PIL import Image, ImageDraw

SHADOW = (14, 93, 58, 255)
DARK, BASE, MID, LIGHT = (95, 109, 85, 255), (125, 145, 112, 255), (144, 166, 127, 255), (174, 200, 154, 255)

# Each tile: a few slabs as (x, y, w, h) boxes; slightly different per tile.
LAYOUTS = [
    [(2, 3, 11, 9)],
    [(1, 2, 7, 6), (8, 8, 7, 6)],
    [(3, 1, 9, 7), (1, 9, 6, 5)],
    [(2, 4, 12, 8)],
]


def slab(d, x, y, w, h):
    d.rounded_rectangle([x, y + 1, x + w, y + h + 1], radius=3, fill=SHADOW)     # shadow rim below
    d.rounded_rectangle([x, y, x + w - 1, y + h - 1], radius=3, fill=DARK)
    d.rounded_rectangle([x, y, x + w - 2, y + h - 2], radius=3, fill=BASE)
    d.rounded_rectangle([x + 1, y + 1, x + w - 3, y + h - 3], radius=2, fill=MID)
    d.line([x + 2, y + 1, x + w - 4, y + 1], fill=LIGHT)                          # top-left highlight
    d.point([x + 1, y + 2], fill=LIGHT)


def tiles():
    out = []
    for layout in LAYOUTS:
        im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        for box in layout:
            slab(d, *box)
        out.append(im)
    return out

"""Draws the trapdoor in the art shop's floor, down to Sable's room: a
16x16 hatch of boards across the floor's own planks, in the floor's own
browns (sampled from Leah's planks in the shop), with two iron straps and a
ring to lift it by. It sits on Buildings over the floor; the pixels round
its rim stay clear so the floor shows through.

Usage: trapdoor.py <out.png>
"""
import sys
from PIL import Image

OUT = (153, 77, 60)          # the floor's darkest brown: the gap round the hatch
DARK = (179, 99, 56)
MID = (195, 118, 59)
BASE = (217, 143, 59)
LIGHT = (242, 168, 74)
IRON_D, IRON, IRON_L = (68, 52, 58), (104, 90, 92), (150, 140, 136)


def draw():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()

    def put(x, y, c):
        px[x, y] = c + (255,)
    for y in range(1, 15):
        for x in range(1, 15):
            put(x, y, OUT)
    # Three boards across the floor's planks, each with a lit top edge and
    # a shaded bottom; the middle one in two pieces.
    for top in (2, 6, 10):
        for x in range(2, 14):
            put(x, top, LIGHT)
            put(x, top + 1, BASE)
            put(x, top + 2, BASE)
            put(x, top + 3, MID if top < 10 else DARK)
        if top == 6:
            put(8, 7, DARK)
            put(8, 8, DARK)
    # Iron straps, a nail at each board, the hinges over the back edge.
    for sx in (3, 12):
        for y in range(2, 14):
            put(sx, y, IRON)
        for y in (3, 7, 11):
            put(sx, y, IRON_L)
        put(sx, 1, IRON_D)
        put(sx, 13, IRON_D)
    # The ring, lying flat near the front edge, on its staple.
    for x, y in ((7, 9), (8, 9), (6, 10), (9, 10), (6, 11), (9, 11), (7, 12), (8, 12)):
        put(x, y, IRON_D)
    put(7, 9, IRON_L)
    put(6, 10, IRON_L)
    put(7, 8, IRON_D)
    put(8, 8, IRON_D)
    return im


if __name__ == "__main__":
    draw().save(sys.argv[1])

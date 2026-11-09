"""Opal Corrie's walk sheet: Evelyn's own sheet (short and round, a cloud of white curls in a
bun, as Opal is), recoloured into Opal, with a ruby scarf and a pencil in her hair drawn on
(docs/corrie_twins_brief.md, "Look": silver curls held up with a pencil, jewel-coloured
scarves, city clothes; her palette is the colours glass throws on the floor: ruby, cobalt,
amber).

  hair      Evelyn's white and grey to silver, a little cooler
  coat      Evelyn's red dress to a well-cut cobalt coat
  buttons   and the hem's band: amber
  scarf     ruby, knotted at the neck, its tail hanging in front (drawn per facing)
  pencil    yellow, with a pink end, pushed through her curls (drawn per facing)
  shoes     plum-brown, as Evelyn's
  facing left is facing right mirrored, as the game's own sheets are

Usage: opal_walk_build.py <Characters/Evelyn export.png> <out.png>"""
import sys
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA").crop((0, 0, 64, 128))
a = np.asarray(im).astype(int)
H, W = 128, 64
o = np.zeros((H, W, 4), np.uint8)

HAIR = {(103, 69, 85): (78, 80, 104), (130, 112, 112): (136, 138, 160),
        (195, 176, 176): (194, 197, 214), (252, 252, 252): (238, 241, 250)}
COAT = {(100, 3, 30): (20, 28, 82), (155, 6, 46): (34, 58, 152), (213, 10, 63): (62, 98, 206)}
AMBER = {(247, 92, 57): (240, 178, 58)}
SHOE = {(146, 36, 1): (112, 56, 44)}

for y in range(H):
    for x in range(W):
        if a[y, x, 3] == 0:
            continue
        c = tuple(a[y, x, :3])
        n = HAIR.get(c) or COAT.get(c) or AMBER.get(c) or SHOE.get(c) or c
        o[y, x, :3] = n
        o[y, x, 3] = 255

RUBY_D, RUBY, RUBY_L = (104, 8, 38), (178, 20, 62), (226, 66, 100)
PENCIL, PENCIL_D, PINK = (244, 204, 64), (150, 104, 30), (236, 136, 150)


def put(fx, fy, x, y, c):
    o[fy + y, fx + x, :3] = c
    o[fy + y, fx + x, 3] = 255


def opaque(fx, fy, x, y):
    return o[fy + y, fx + x, 3] > 0


def head_top(fx, fy):
    """The first row with a pixel in it: the frames bob as she walks."""
    for y in range(32):
        if o[fy + y, fx:fx + 16, 3].any():
            return y
    return 0


for f in range(4):
    # ---- facing down (row 0) ----
    fx, fy = f * 16, 0
    t = head_top(fx, fy)          # 8 standing, 9 mid-step
    n = t + 11                    # the neck row (19 standing)
    for x in range(5, 11):        # the scarf round her neck
        put(fx, fy, x, n, RUBY_L if x in (6, 7) else RUBY)
    put(fx, fy, 4, n, RUBY_D)
    put(fx, fy, 11, n, RUBY_D)
    for x in range(5, 11):
        put(fx, fy, x, n + 1, RUBY_D if x in (5, 10) else RUBY)
    for y in (n + 2, n + 3):      # its tail, hanging to the left of her buttons
        put(fx, fy, 5, y, RUBY)
        put(fx, fy, 4, y, RUBY_D)
    put(fx, fy, 5, n + 4, RUBY_D)
    # the pencil, sticking up out of her curls on the right
    put(fx, fy, 12, t - 1, PINK)
    put(fx, fy, 11, t, PENCIL)
    put(fx, fy, 10, t + 1, PENCIL_D)

    # ---- facing right (row 1) ----
    fx, fy = f * 16, 32
    t = head_top(fx, fy)
    n = t + 11
    for x in range(5, 10):
        put(fx, fy, x, n, RUBY_L if x == 8 else RUBY)
    for x in range(5, 10):
        put(fx, fy, x, n + 1, RUBY)
    put(fx, fy, 5, n + 1, RUBY_D)
    for y in (n + 2, n + 3):      # the tail at her front
        put(fx, fy, 9, y, RUBY)
        put(fx, fy, 10, y, RUBY_D)
    put(fx, fy, 9, n + 4, RUBY_D)
    # the pencil through her bun, at the back of her head
    put(fx, fy, 0, t + 1, PINK)
    put(fx, fy, 1, t + 2, PENCIL)
    put(fx, fy, 2, t + 3, PENCIL_D)

    # ---- facing up (row 2) ----
    fx, fy = f * 16, 64
    t = head_top(fx, fy)
    n = t + 11
    for x in range(5, 11):
        put(fx, fy, x, n, RUBY_L if x in (7, 8) else RUBY)
    put(fx, fy, 4, n, RUBY_D)
    put(fx, fy, 11, n, RUBY_D)
    for x in range(6, 10):        # the knot
        put(fx, fy, x, n + 1, RUBY_D if x in (6, 9) else RUBY)
    put(fx, fy, 12, t - 1, PINK)
    put(fx, fy, 11, t, PENCIL)
    put(fx, fy, 10, t + 1, PENCIL_D)

# facing left (row 3): facing right, mirrored
for f in range(4):
    o[96:128, f * 16:f * 16 + 16] = o[32:64, f * 16:f * 16 + 16][:, ::-1]

Image.fromarray(o, "RGBA").save(out)
print("wrote", out)

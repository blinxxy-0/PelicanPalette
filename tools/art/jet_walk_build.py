"""Jet Corrie's walk sheet: Kent's own sheet (tall, short hair, clean-shaven, as Jet is),
recoloured into Jet, with a leather apron and safety glasses drawn on
(docs/corrie_twins_brief.md, "Look": close-cropped grey hair, a black roll-neck under a
leather apron, safety glasses pushed up on his head; his palette is the lead between the
glass: black, slate, pewter).

  hair      Kent's crop to grey
  glasses   safety glasses pushed up on his head; their strap behind
  top       Kent's jacket and shirt to one black roll-neck, its collar rolled up his neck
  apron     leather, a bib and a long skirt in front, crossed straps and a tie behind,
            its edge in profile
  trousers  Kent's jeans to slate
  boots     dark brown
  facing left is facing right mirrored, as the game's own sheets are

Usage: jet_walk_build.py <Characters/Kent export.png> <out.png>"""
import sys
import numpy as np
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA").crop((0, 0, 64, 128))
a = np.asarray(im).astype(int)
H, W = 128, 64
o = np.zeros((H, W, 4), np.uint8)

# Kent's colours
H_OUT, H_MID, H_HI, H_DARK = (101, 35, 0), (158, 92, 0), (230, 147, 0), (118, 57, 0)
SKIN, SKIN_M = (255, 200, 140), (239, 143, 91)
J_D, J_M, J_L = (22, 34, 24), (51, 56, 38), (78, 91, 42)
SH_D, SH_L = (135, 135, 135), (204, 204, 204)
JEAN_D, JEAN_M, JEAN_L = (15, 46, 104), (44, 128, 204), (80, 187, 216)
DARK, BELT = (40, 13, 0), (86, 27, 7)

# Jet's
G_OUT, G_MID, G_HI = (70, 68, 72), (132, 132, 138), (184, 186, 190)
TOP_D, TOP_M, TOP_L = (14, 14, 18), (34, 34, 42), (58, 58, 70)
TR_D, TR_M, TR_L = (32, 36, 46), (68, 76, 90), (96, 106, 122)
BOOT_D, BOOT_M = (36, 22, 14), (74, 50, 34)
EYE = (92, 104, 112)
LEATHER_D, LEATHER, LEATHER_L = (58, 32, 18), (118, 72, 38), (154, 100, 56)
LENS, LENS_HI, LENS_RIM = (176, 214, 228), (240, 250, 252), (44, 44, 52)


def recolour(c, fy, y, facing):
    head, body, legs, feet = fy < 13, 13 <= fy < 23, 23 <= fy < 29, fy >= 29
    if c in ((162, 50, 13), (216, 66, 17)):     # Kent's heels, lifted mid-step
        return BOOT_M
    if head:
        if c == H_MID:
            return G_MID
        if c == H_HI:
            return G_HI
        if c == H_OUT:
            return G_OUT
        if c == H_DARK:          # behind, the back of his head; in front, his face's edge
            return G_OUT if facing == "up" else (150, 92, 64)
        if c == SKIN and facing == "up":
            return G_HI
        if c == SKIN and fy <= 6:
            return G_HI           # the shine on the crop
        if c == JEAN_L:
            return EYE
    if body or (head and c in (J_D, J_M, J_L)):
        if c in (J_D,):
            return TOP_D
        if c in (J_M, SH_D):
            return TOP_M
        if c in (J_L, SH_L):
            return TOP_L
        if c == H_HI or c == BELT:
            return TOP_D
    if legs or body:
        if c == JEAN_D:
            return TR_D
        if c == JEAN_M:
            return TR_M
        if c == JEAN_L:
            return TR_L
    if feet:
        if c in (DARK, BELT):
            return BOOT_D
        if c in (H_OUT, H_MID, H_DARK, (162, 50, 13), (216, 66, 17)):
            return BOOT_M
    return c


FACING = ["down", "right", "up", "left"]
for y in range(H):
    for x in range(W):
        if a[y, x, 3] == 0:
            continue
        o[y, x, :3] = recolour(tuple(a[y, x, :3]), y % 32, y, FACING[y // 32])
        o[y, x, 3] = 255


def put(fx, fy, x, y, c):
    o[fy + y, fx + x, :3] = c
    o[fy + y, fx + x, 3] = 255


def is_top(fx, fy, x, y):
    return o[fy + y, fx + x, 3] > 0 and tuple(o[fy + y, fx + x, :3]) in (TOP_D, TOP_M, TOP_L)


def head_top(fx, fy):
    for y in range(32):
        if o[fy + y, fx:fx + 16, 3].any():
            return y
    return 0


for f in range(4):
    # ---- facing down ----
    fx, fy = f * 16, 0
    t = head_top(fx, fy)                        # 1 standing (the spikes of his crop)
    b = t - 1                                   # the bob: 0 standing, 1 mid-step
    g = 3 + b                                   # the safety glasses, up on his crop:
    for x in (5, 6, 9, 10):                     # two lenses, rimmed, with a bridge
        put(fx, fy, x, g, LENS_RIM)
        put(fx, fy, x, g + 3, LENS_RIM)
    for y in (g + 1, g + 2):
        put(fx, fy, 4, y, LENS_RIM)
        put(fx, fy, 11, y, LENS_RIM)
        for x in (5, 6, 9, 10):
            put(fx, fy, x, y, LENS)
    put(fx, fy, 5, g + 1, LENS_HI)
    put(fx, fy, 9, g + 1, LENS_HI)
    put(fx, fy, 7, g + 2, LENS_RIM)
    put(fx, fy, 8, g + 2, LENS_RIM)
    # roll-neck: the collar up his neck
    for x in range(5, 11):
        put(fx, fy, x, 13 + b, TOP_L if x in (6, 7) else TOP_M)
    put(fx, fy, 4, 13 + b, TOP_D)
    put(fx, fy, 11, 13 + b, TOP_D)
    # the apron: neck straps, the bib, the waist tie, the skirt over his knees
    put(fx, fy, 5, 14 + b, LEATHER_D)
    put(fx, fy, 10, 14 + b, LEATHER_D)
    for y in range(15 + b, 22 + b):
        for x in range(5, 11):
            put(fx, fy, x, y, LEATHER_D if x in (5, 10) else (LEATHER_L if x == 6 else LEATHER))
    for x in range(3, 13):
        if is_top(fx, fy, x, 21 + b) or 5 <= x <= 10:
            put(fx, fy, x, 21 + b, LEATHER_D)
    for y in range(22 + b, 27 + b):
        for x in range(4, 12):
            put(fx, fy, x, y, LEATHER_D if x in (4, 11) or y == 26 + b else
                (LEATHER_L if x == 5 else LEATHER))

    # ---- facing right ----
    fx, fy = f * 16, 32
    t = head_top(fx, fy)
    b = t - 1
    g = 3 + b
    for x in range(3, 8):                       # the strap round the back of his head
        put(fx, fy, x, g + 2, LENS_RIM)
    for x in (8, 9, 10):                        # the lens, rimmed, up over his brow
        put(fx, fy, x, g, LENS_RIM)
        put(fx, fy, x, g + 3, LENS_RIM)
        put(fx, fy, x, g + 1, LENS)
        put(fx, fy, x, g + 2, LENS)
    put(fx, fy, 9, g + 1, LENS_HI)
    put(fx, fy, 11, g + 1, LENS_RIM)
    put(fx, fy, 11, g + 2, LENS_RIM)
    for x in range(5, 10):                      # roll-neck
        if o[fy + 13 + b, fx + x, 3] or x in (6, 7, 8):
            put(fx, fy, x, 13 + b, TOP_L if x == 8 else TOP_M)
    # the apron in profile: its edge down his front, the strap round his waist
    for y in range(14 + b, 22 + b):
        put(fx, fy, 10, y, LEATHER_D)
        put(fx, fy, 9, y, LEATHER)
    for x in range(4, 10):
        if is_top(fx, fy, x, 21 + b):
            put(fx, fy, x, 21 + b, LEATHER_D)
    for y in range(22 + b, 27 + b):
        put(fx, fy, 10, y, LEATHER_D)
        put(fx, fy, 9, y, LEATHER)
    put(fx, fy, 9, 26 + b, LEATHER_D)

    # ---- facing up ----
    fx, fy = f * 16, 64
    t = head_top(fx, fy)                        # 2 standing
    b = t - 2
    for x in range(3, 13):                      # the glasses' strap behind
        if o[fy + 6 + b, fx + x, 3]:
            put(fx, fy, x, 6 + b, LENS_RIM)
    for x in range(5, 11):                      # roll-neck behind
        put(fx, fy, x, 13 + b, TOP_M)
    # crossed straps, shoulders to waist, and the tie with its bow
    for i in range(7):
        put(fx, fy, 4 + i, 14 + b + i, LEATHER_D)
        put(fx, fy, 11 - i, 14 + b + i, LEATHER_D)
    for x in range(3, 13):
        if is_top(fx, fy, x, 21 + b):
            put(fx, fy, x, 21 + b, LEATHER_D)
    put(fx, fy, 7, 21 + b, LEATHER)
    put(fx, fy, 8, 21 + b, LEATHER)
    put(fx, fy, 6, 22 + b, LEATHER_D)
    put(fx, fy, 9, 22 + b, LEATHER_D)

for f in range(4):
    o[96:128, f * 16:f * 16 + 16] = o[32:64, f * 16:f * 16 + 16][:, ::-1]

Image.fromarray(o, "RGBA").save(out)
print("wrote", out)

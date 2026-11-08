"""Fern's walk sheet: Leah's own sheet (a small woman with a long braid, as Fern is),
recoloured into Fern, with a straw sun hat drawn on. Higgsfield's edit of the sheet
(fern_edit_b.png) read back as one orange smudge, her straw, blonde, mustard and tan all
alike at 16x32, so it's only the guide here for the hat and the dungarees.

  hair      Leah's orange to sandy blonde; her braid stays
  skin      tanned; eyes green-brown
  shirt     Leah's green top to faded cornflower blue
  dungarees Leah's trousers to mustard, with a bib in front and crossed straps behind
  boots     brown
  hat       straw, a band, a white flower; drawn per facing, bobbing with the walk
  facing left is facing right mirrored, as the game's own sheets are

Usage: fern_walk_build.py <Leah's sheet 64x128 or 12x.png> <out.png>"""
import sys
import numpy as np
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGBA")
if im.width != 64:
    im = im.resize((64, 128), Image.NEAREST)
a = np.asarray(im).astype(int)
bg = tuple(a[0, 0, :3])
H, W = 128, 64
o_ = np.zeros((H, W, 4), np.uint8)

HAIR = {(109, 4, 0): (112, 70, 26), (171, 45, 0): (176, 124, 52), (233, 109, 0): (218, 172, 86),
        (255, 147, 0): (242, 208, 128), (255, 186, 105): (250, 228, 160)}
SKIN = {(255, 198, 147): (234, 170, 118), (252, 160, 120): (220, 144, 98), (232, 105, 78): (188, 104, 68),
        (130, 30, 41): (122, 54, 38)}
EYES = {(113, 0, 165): (58, 66, 28), (149, 25, 183): (112, 122, 52), (163, 137, 163): (150, 118, 96),
        (82, 0, 40): (96, 56, 24), (243, 229, 255): (250, 246, 236)}
SHIRT = {(103, 192, 115): (146, 176, 232), (23, 145, 23): (98, 132, 210), (15, 94, 15): (58, 82, 150)}
MUSTARD = {(122, 104, 122): (236, 188, 60), (86, 71, 86): (200, 146, 34), (40, 33, 40): (124, 82, 22),
           (163, 137, 163): (244, 206, 96)}
BOOT = {(109, 4, 0): (104, 62, 34), (171, 45, 0): (140, 90, 50), (77, 0, 34): (60, 30, 18)}
OUTLINE = {(77, 0, 34): (66, 30, 22), (0, 15, 26): (40, 22, 18)}
MU = (236, 188, 60)
MU_D = (200, 146, 34)

for y in range(H):
    fy = y % 32
    for x in range(W):
        c = tuple(a[y, x, :3])
        if c == bg or a[y, x, 3] == 0:
            continue
        if fy >= 28 and c in BOOT:
            n = BOOT[c]
        elif fy >= 23 and c in MUSTARD:
            n = MUSTARD[c]
        else:
            n = HAIR.get(c) or SKIN.get(c) or EYES.get(c) or SHIRT.get(c) or MUSTARD.get(c) \
                or OUTLINE.get(c) or c
        o_[y, x, :3] = n
        o_[y, x, 3] = 255

BOB = (0, 1, 0, 1)
blue = {v for v in SHIRT.values()}
# dungarees: the bib in front over her shirt, crossed straps behind
for f, o in enumerate(BOB):
    X = f * 16
    for y in range(21 + o, 23 + o):                    # front bib, low, so her shirt shows
        for x in range(6, 10):
            if tuple(o_[y, X + x, :3]) in blue:
                o_[y, X + x, :3] = MU_D if x == 9 else MU
    for y in (19 + o, 20 + o):                         # its straps
        for x in (6, 9):
            if tuple(o_[y, X + x, :3]) in blue:
                o_[y, X + x, :3] = MU
    Y = 64 + o                                         # behind: an X, then the back of the bib
    for y, d in zip(range(18, 22), (0, 1, 2, 2)):
        for x in (5 + d, 10 - d):
            if tuple(o_[Y + y, X + x, :3]) in blue:
                o_[Y + y, X + x, :3] = MU
    for x in range(5, 11):
        if tuple(o_[Y + 22, X + x, :3]) in blue:
            o_[Y + 22, X + x, :3] = MU
    Y = 32 + o                                         # facing right: the strap and bib's edge
    for y in range(18, 23):
        for x in range(8, 11):
            if tuple(o_[Y + y, X + x, :3]) in blue and (x == 9 or y >= 21):
                o_[Y + y, X + x, :3] = MU

# the hat
INK = {"o": (90, 54, 22), "L": (244, 210, 130), "M": (214, 166, 86), "D": (168, 116, 52),
       "B": (164, 84, 44), "W": (252, 250, 240), "Y": (250, 214, 60), "G": (86, 140, 56)}
FRONT = [".....oooooo.....",
         "....oLLLLLMo....",
         "...oLLLLLLMMo...",
         "...oMMMMMMWMo...",
         "...oBBBBBGWYo...",
         ".ooMLLLLLLLWMoo.",
         "oLLLLLLLLLLLLMMo",
         ".ooDDDDDDDDDDoo."]
RIGHT = [".....oooooo.....",
         "....oLLLLLMo....",
         "...oLLLLLLMMo...",
         "...oMMMWMMMMo...",
         "...oBBWYWBBBo...",
         "..oMLLLWLLLLMoo.",
         ".oLLLLLLLLLLLLLo",
         "..ooDDDDDDDDDoo."]
BACK = [".....oooooo.....",
        "....oLLLLLMo....",
        "...oLLLLLLMMo...",
        "...oLLLLLMMMo...",
        "...oBBBBBBBBo...",
        ".ooMLLLLLLLLMoo.",
        "oLLLLLLLLLLLLLMo",
        ".ooDDDDDDDDDDoo."]
TOP = 5
for row, hat in ((0, FRONT), (1, RIGHT), (2, BACK)):
    for f, o in enumerate(BOB):
        X, Y = f * 16, row * 32
        # set low, two rows down, so less of Leah's hair shows under it (the user, 2026-11-08);
        # above the brim only the hat, none of her hair beside the crown
        o_[Y:Y + TOP + 5 + o, X:X + 16] = 0
        for j, line in enumerate(hat):
            for x, ch in enumerate(line):
                if ch != ".":
                    o_[Y + TOP + j + o, X + x, :3] = INK[ch]
                    o_[Y + TOP + j + o, X + x, 3] = 255
# facing left: facing right mirrored
for f in range(4):
    X = f * 16
    o_[96:128, X:X + 16] = o_[32:64, X:X + 16][:, ::-1]
Image.fromarray(o_).save(out)

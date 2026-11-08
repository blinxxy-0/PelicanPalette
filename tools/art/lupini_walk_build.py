"""Lupini's walk sheet, built round his own sprite (the user, 2026-11-08: "it didn't use
lupini's existing front sprite at all").

  front (row 0)  his Night Market sprite (night_market_tilesheet_objects, here at x16 in
                 art/vale_residents_sprites/ref_lupini_ingame_x16.png), head and body
                 exactly as the game draws him; the brown of the boat behind him dropped.
                 That sprite stands behind the boat's rail, so its legs are one block:
                 below the waist each frame takes the walking legs of Higgsfield's edit
                 of Harvey's sheet, in his trousers' colours.
  sides, back    the edit (read back with tools/art/walk_readback.py), every colour moved
                 to the nearest in his sprite's own palette, so all four ways match.

Usage: lupini_walk_build.py <ingame x16.png> <read-back 64x128.png> <out.png>"""
import sys
import numpy as np
from PIL import Image
ingame, rb, out = sys.argv[1], sys.argv[2], sys.argv[3]
g = np.asarray(Image.open(ingame).convert("RGB").resize((32, 48), Image.NEAREST)).astype(int)
bg = g[0, 0]
body = g[10:35, 8:24]                                   # head to waist: 25 rows x 16
BOAT = {(105, 62, 10), (121, 67, 2), (83, 47, 8)}
keep = (np.abs(body - bg).sum(2) > 0) & ~np.array([[tuple(c) in BOAT for c in row] for row in body])
pal = sorted({tuple(c) for c in body[keep]})
OUTLINE, TROUSER, TROUSER_D = (38, 18, 32), (69, 37, 43), (53, 25, 45)
pal = np.array(pal + [TROUSER])

r = np.asarray(Image.open(rb).convert("RGBA")).astype(int)
H, W = r.shape[:2]
outimg = np.zeros((H, W, 4), np.uint8)
# sides and back: the edit, each kind of colour moved to his sprite's own
OUT_D, OUT, JACKET_D, JACKET, HAIR = (38, 18, 32), (53, 25, 45), (73, 23, 59), (107, 52, 69), (137, 76, 79)
SKIN, SKIN_L, LENS, LENS_L = (220, 151, 139), (237, 196, 174), (33, 127, 184), (0, 178, 194)
BOB = (0, 1, 0, 1)
def his(c, head):
    s, (cr, cg, cb) = c.sum(), c
    if cb > cr and cb > 150:                            # the glasses' lenses
        return LENS
    if cr > 180 and cg > 100:                           # skin, his bald crown included
        return SKIN_L if s > 560 else SKIN
    if s < 95:
        return OUT_D
    if s < 130:
        return OUT
    if head:                                            # his hair: the plum of his beard
        return JACKET if s > 200 else JACKET_D
    if s < 175:
        return JACKET_D
    if s < 215:
        return JACKET
    return HAIR
HEAD = {1: 14, 2: 12, 3: 13}                          # rows of head above the collar, per facing
for y in range(32, H):
    row, fy = divmod(y, 32)
    for x in range(W):
        if r[y, x, 3]:
            o = BOB[x // 16]
            head = fy < HEAD[row] + o
            outimg[y, x, :3] = his(r[y, x, :3], head)
            outimg[y, x, 3] = 255
# one head per facing, the standing frame's, bobbing on the steps: the edit drew his
# face a little differently every frame (the user: "his nose bounces around a lot")
for row, hr in HEAD.items():
    Y = row * 32
    head = outimg[Y:Y + hr, 0:16].copy()
    for f, o in enumerate(BOB[1:], 1):
        X = f * 16
        outimg[Y:Y + hr + o, X:X + 16] = 0
        outimg[Y + o:Y + o + hr, X:X + 16] = head
# his red bow tie, at the collar in front of his neck (the edit lost it)
BOW, BOW_D = (248, 76, 58), (137, 25, 46)
for f, o in enumerate(BOB):
    X = f * 16
    outimg[96 + 13 + o, X + 6] = BOW + (255,)          # facing left
    outimg[96 + 13 + o, X + 5] = BOW_D + (255,)
# his head facing left and from behind, drawn here from his sprite (the user: the edit's
# had "some form of tumor on his forehead", and from behind he looked balder than he is):
# plum hair on top receding a little at the temple, round blue glasses, a nose, the plum
# moustache and beard; from behind, hair all over, a rose shine on the crown
INK = {"a": OUT, "#": OUT_D, "d": JACKET_D, "b": JACKET, "c": HAIR, "e": SKIN, "g": SKIN_L,
       "L": LENS, "h": LENS_L}
LEFT = ["....aaaaaa......",
        "...abcccbbda....",
        "..abceecbbdda...",
        "..aeeeebbbddda..",
        "..aeeeeebbddda..",
        "..dLLddddeddda..",
        ".agLhgeeeccdda..",
        ".aeeeeeeecedda..",
        "..abbbeeeeddda..",
        "..adddbbeedda...",
        "...addddeeda...."]
BACK = [".....aaaaa#.....",
        "....abcccbba....",
        "...abccccbbda...",
        "...abcbcbbbda...",
        "...dbbbbbbdda...",
        "...dbdbbdbdda...",
        "...dbdbbdbdda...",
        "...#dbddddbd#...",
        "....#dbbbdda....",
        "....#ddddda#...."]
for row, rows, hr in ((3, LEFT, 13), (2, BACK, 12)):
    for f, o in enumerate(BOB):
        X, Y = f * 16, row * 32
        outimg[Y:Y + hr + o, X:X + 16] = 0
        for j, line in enumerate(rows):
            for x, ch in enumerate(line):
                if ch != ".":
                    outimg[Y + 2 + j + o, X + x] = INK[ch] + (255,)
# facing right is facing left mirrored, frame by frame, as the game's own sheets are
for f in range(4):
    X = f * 16
    outimg[32:64, X:X + 16] = outimg[96:128, X:X + 16][:, ::-1]
# front: his own head and body, the edit's legs
for f in range(4):
    X = f * 16
    for y in range(25):
        for x in range(16):
            if keep[y, x]:
                outimg[1 + y, X + x, :3] = body[y, x]; outimg[1 + y, X + x, 3] = 255
    for y in range(26, 32):
        for x in range(16):
            if r[y, X + x, 3]:
                c = r[y, X + x, :3]
                dark = c.sum() < 120 or y >= 30 or not (r[y, X + x - 1, 3] and r[y, X + x + 1, 3])
                outimg[y, X + x, :3] = OUTLINE if dark else (TROUSER if (x + y) % 5 else TROUSER_D)
                outimg[y, X + x, 3] = 255
Image.fromarray(outimg).save(out)

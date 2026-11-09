"""Studio III's own pieces (Tess Brand's studio, 1.4). Where the game has the thing,
the room uses the game's own (tools/build_vale_buildings.py GAME_FURN and VANILLA: the
iron stove, the walnut dresser, oak chairs, wood and log panels for the timber, Robin's
axe in its stump and log pile). The rest comes from spare candidates of earlier PixelLab
batches (no new generations), edited here, and three small things are drawn:

  workbench        the atelier batch's spare trestle table (brush_table_1_1), its jars and
                   papers cleared off; a block of ash, a mallet, a chisel and shavings on it
  chisel_board     the pottery batch's spare tool board (tool_board_1_12), a row of chisels added
  counter          the pottery batch's spare counter (sales_counter_1_11): the cash tin kept,
                   three small carved animals where its box was
  north_window     the atelier batch's spare window, its ivy gone (Robin's renovation)
  bram_carving_0   drawn: the carving on its stand under a dust sheet (the Gilded Tree,
                   the user 2026-11-09)
  bedroll          drawn: her bedroll, a knitted pillow, a blanket
  shavings         drawn: curls of pale wood on the floor

Usage: python tools/art/vale_studio3_art.py   (writes art/gilded_vale/interiors/studio3/*.png)
"""
import os
from PIL import Image, ImageDraw

OUT = "art/gilded_vale/interiors/studio3"

# the game's wood ramp (outline, shadow, mid, light, highlight)
OL = (58, 32, 22, 255)
W1 = (98, 56, 32, 255)
W2 = (140, 84, 44, 255)
W3 = (178, 116, 60, 255)
W4 = (214, 156, 90, 255)
# other woods for the timber
OAK = [(120, 82, 46, 255), (170, 124, 72, 255), (204, 160, 100, 255)]
WALNUT = [(62, 40, 30, 255), (92, 60, 42, 255), (122, 84, 58, 255)]
CHERRY = [(110, 48, 34, 255), (152, 72, 48, 255), (186, 100, 66, 255)]
ASH = [(150, 132, 104, 255), (196, 180, 146, 255), (226, 214, 184, 255)]
# iron and steel
I0 = (30, 30, 36, 255)
I1 = (52, 52, 60, 255)
I2 = (80, 80, 92, 255)
I3 = (118, 120, 134, 255)
S2 = (150, 158, 172, 255)
S3 = (206, 212, 222, 255)
# cloth: the dust sheets (old linen), her bedroll (moss green, like her jumper), its blanket
L1 = (150, 142, 128, 255)
L2 = (190, 182, 166, 255)
L3 = (222, 216, 200, 255)
L4 = (240, 236, 224, 255)
M1 = (52, 74, 46, 255)
M2 = (78, 108, 64, 255)
M3 = (112, 142, 86, 255)
B1 = (96, 46, 40, 255)
B2 = (140, 70, 58, 255)
B3 = (178, 104, 82, 255)
FIRE = [(150, 40, 20, 255), (226, 102, 34, 255), (252, 178, 64, 255), (255, 232, 140, 255)]
DUST = (196, 186, 170, 255)


def new(w, h):
    im = Image.new("RGBA", (w, h))
    return im, ImageDraw.Draw(im)


def save(im, name):
    im.save(os.path.join(OUT, name + ".png"))


def rect(d, x0, y0, x1, y1, fill, outline=OL):
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline)


def bram_carving_sheeted():
    """The carving under its dust sheet: a stand, and a tall shape under the linen,
    a tree's crown at the top (branches pushing the cloth out in lumps), the sheet
    falling to the stand in folds and crumpling there."""
    im, d = new(32, 48)
    # the stand: a low plinth of two boards
    rect(d, 2, 41, 29, 47, W2)
    d.line([(3, 42), (28, 42)], fill=W3)
    d.line([(3, 46), (28, 46)], fill=W1)
    # the sheet's outline: lumps at the crown, a waist at the trunk, flaring at the foot
    shape = [(15, 1), (19, 2), (21, 1), (25, 4), (27, 8), (29, 11), (28, 15), (29, 19), (26, 23),
             (22, 25), (20, 28), (21, 33), (24, 38), (27, 42), (4, 42), (8, 38), (11, 33), (12, 28),
             (9, 25), (5, 23), (2, 19), (3, 15), (2, 11), (5, 6), (9, 3), (12, 2)]
    d.polygon(shape, fill=L2, outline=OL)
    # shade the right side (the window is to the left)
    mask = Image.new("L", im.size)
    ImageDraw.Draw(mask).polygon(shape, fill=255)
    px, mk = im.load(), mask.load()
    for y in range(48):
        xs = [x for x in range(32) if mk[x, y]]
        if not xs:
            continue
        x0, x1 = min(xs), max(xs)
        for x in xs:
            if x in (x0, x1):
                continue
            t = (x - x0) / max(1, x1 - x0)
            if t > 0.72:
                px[x, y] = L1
            elif t < 0.22:
                px[x, y] = L3
    # the crown's lumps: highlights on their upper left, shadows under them
    for cx, cy in ((9, 6), (16, 4), (6, 13), (12, 11), (20, 9), (9, 19), (16, 17)):
        d.point((cx, cy), fill=L4)
        d.point((cx + 1, cy), fill=L4)
        d.line([(cx, cy + 3), (cx + 3, cy + 2)], fill=L1)
    d.line([(6, 22), (11, 25), (16, 26), (21, 25), (25, 22)], fill=L1)     # where the crown's hem hangs
    # folds down the trunk, flaring out
    d.line([(14, 27), (12, 35), (9, 41)], fill=L3)
    d.line([(17, 27), (18, 35), (20, 41)], fill=L1)
    d.line([(15, 30), (15, 41)], fill=L1)
    # the hem crumpled on the plinth
    d.line([(5, 41), (9, 40), (13, 41), (18, 40), (22, 41), (26, 41)], fill=L4)
    save(im, "bram_carving_0")


def bedroll():
    im, d = new(32, 16)
    # the blanket, rumpled; a knitted pillow at the head (the stove end, left)
    d.rounded_rectangle([1, 3, 31, 14], 2, fill=B2, outline=OL)
    d.line([(9, 4), (30, 4)], fill=B3)
    d.line([(9, 13), (30, 13)], fill=B1)
    for x in (14, 21, 27):
        d.line([(x, 5), (x - 1, 12)], fill=B1)         # its folds
    d.rounded_rectangle([1, 2, 10, 13], 2, fill=M2, outline=OL)
    for y in range(4, 12, 2):
        d.line([(2, y), (9, y)], fill=M3)              # knitted ribs
    d.line([(3, 12), (9, 12)], fill=M1)
    save(im, "bedroll")


def shavings():
    im, d = new(16, 16)
    for sx, sy, c in ((3, 8, ASH[2]), (7, 10, OAK[2]), (9, 6, ASH[1]), (5, 12, OAK[2]), (11, 11, ASH[2])):
        d.arc([sx, sy, sx + 3, sy + 3], 200, 470, fill=c)
        d.point((sx + 1, sy + 3), fill=ASH[0])
    save(im, "shavings")


def carved_animal(d, x, y, kind):
    """A thumb-sized carving, pale wood: rabbit, duck or fox (a few pixels each)."""
    a, b, c = ASH[0], ASH[1], ASH[2]
    if kind == "rabbit":
        d.rectangle([x, y + 2, x + 3, y + 4], fill=b)
        d.line([(x + 3, y), (x + 3, y + 1)], fill=a)     # ears
        d.line([(x + 4, y), (x + 4, y + 1)], fill=b)
        d.point((x, y + 2), fill=c)
    elif kind == "duck":
        d.rectangle([x, y + 2, x + 3, y + 4], fill=b)
        d.rectangle([x + 3, y, x + 4, y + 2], fill=b)
        d.point((x + 5, y + 1), fill=W3)                 # its bill
        d.point((x + 1, y + 2), fill=c)
    else:
        d.rectangle([x, y + 2, x + 4, y + 4], fill=CHERRY[1])
        d.point((x + 5, y + 1), fill=CHERRY[1])
        d.point((x + 4, y + 1), fill=CHERRY[2])
        d.point((x - 1, y + 3), fill=c)                  # the white tip of its tail
    d.line([(x, y + 5), (x + 4, y + 5)], fill=(0, 0, 0, 70))


def north_window():
    im = Image.open("art/gilded_vale/interiors/atelier/atelier_v1/north_window_1_0.png").convert("RGBA")
    bb = im.getbbox()
    # the window is symmetrical and its ivy all on the right: the clean left half, mirrored
    mid = (bb[0] + bb[2]) // 2
    left = im.crop((bb[0], 0, mid, im.height))
    im.paste(left.transpose(Image.FLIP_LEFT_RIGHT), (bb[2] - left.width, 0))
    save(im, "north_window")


INT = "art/gilded_vale/interiors/"


def candidate(path):
    return Image.open(INT + path + ".png").convert("RGBA")


def is_wood(c):
    return c[3] and c[0] > c[1] + 30 and c[0] > c[2] + 40


def refill(px, x0, x1, y):
    """Row y between x0 and x1: every pixel that isn't the furniture's own wood takes the
    nearest wood pixel's colour in the row, so whatever lay there is gone."""
    row = [px[x, y] for x in range(x0, x1 + 1)]
    wood = [i for i, c in enumerate(row) if is_wood(c)]
    if not wood:
        return
    for i, c in enumerate(row):
        if not is_wood(c):
            j = min(wood, key=lambda w: abs(w - i))
            px[x0 + i, y] = row[j]


def workbench():
    im = candidate("atelier/atelier_v1/brush_table_1_1")
    px = im.load()
    bb = im.getbbox()
    x0, top = bb[0], bb[1] + 6                       # the tabletop's back edge
    # everything standing on it goes: above the back edge, and what lies on the top
    for y in range(0, top):
        for x in range(im.width):
            px[x, y] = (0, 0, 0, 0)
    # the top, plank by plank: each row of it takes the row's commonest wood colour, with a
    # darker seam every few pixels, so it reads as clean boards
    from collections import Counter
    for y in range(top + 1, top + 14):
        row = [px[x, y] for x in range(x0 + 1, x0 + 39)]
        wood = Counter(c for c in row if is_wood(c))
        if not wood:
            continue
        main = wood.most_common(1)[0][0]
        seam = tuple(max(0, v - 34) for v in main[:3]) + (255,)
        for x in range(x0 + 1, x0 + 39):
            px[x, y] = seam if (x - x0) % 13 == 0 and y < top + 10 else main
    for x in range(x0 + 1, x0 + 39):                 # the back edge, one outline again
        px[x, top] = OL
    d = ImageDraw.Draw(im)
    # a block of ash, roughed out, at the left end; a mallet; a chisel; shavings
    bx, by = x0 + 3, top - 5
    rect(d, bx, by, bx + 6, top + 2, ASH[1])
    d.line([(bx + 1, by + 1), (bx + 5, by + 1)], fill=ASH[2])
    d.line([(bx + 6, by + 1), (bx + 6, top + 1)], fill=ASH[0])
    d.line([(bx + 2, by + 3), (bx + 4, by + 4)], fill=ASH[0])       # where the gouge has been
    rect(d, x0 + 17, top - 2, x0 + 21, top + 2, W1)                  # the mallet's head
    d.line([(x0 + 22, top + 1), (x0 + 28, top + 1)], fill=W4)        # its handle
    d.line([(x0 + 30, top + 4), (x0 + 33, top + 4)], fill=W4)        # a chisel
    d.line([(x0 + 34, top + 4), (x0 + 36, top + 4)], fill=S3)
    for sx, sy in ((x0 + 11, top + 3), (x0 + 13, top + 6), (x0 + 25, top + 6)):
        d.arc([sx, sy, sx + 3, sy + 2], 180, 360, fill=ASH[2])
    save(im, "workbench")


def chisel_board():
    im = candidate("pottery/pottery_v2/tool_board_1_12")
    d = ImageDraw.Draw(im)
    # five chisels in the empty right half of the board, growing along the row
    for i, x in enumerate(range(17, 27, 2)):
        d.point((x, 10), fill=OL)                                    # its peg
        d.line([(x, 11), (x, 15)], fill=W4)                          # the handle
        d.point((x, 11), fill=W3)
        d.line([(x, 16), (x, 18 + i)], fill=S3)                      # the blade
        d.point((x, 18 + i), fill=S2)
    save(im, "chisel_board")


def counter():
    im = candidate("pottery/pottery_v1/sales_counter_1_11")
    px = im.load()
    # the box on the right goes: above the counter's top, and on it
    for y in range(0, 15):
        for x in range(34, im.width):
            px[x, y] = (0, 0, 0, 0)
    # and on the counter's top: the clean stretch to its left, copied over
    for y in range(15, 25):
        for x in range(34, 46):
            px[x, y] = px[x - 14, y]
    d = ImageDraw.Draw(im)
    carved_animal(d, 18, 10, "rabbit")
    carved_animal(d, 27, 10, "duck")
    carved_animal(d, 37, 10, "fox")
    save(im, "counter")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in (north_window, workbench, chisel_board, counter, bram_carving_sheeted, bedroll, shavings):
        f()
    print("wrote", OUT)

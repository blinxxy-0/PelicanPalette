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
  bram_carving_N   Bram's carving, the Gilded Tree (the user, 2026-11-09), in six states: 0 under its
                   dust sheet, 1 as he left it (the crown a rough block), 2-4 a quarter, half, nearly
                   carved, 5 finished. Higgsfield gpt_image_2_6: the finished one (the user's pick of
                   four, carving/tree_2), then edits of it backwards; each read down to 2x3 tiles with
                   one crop and scale for all six, a pixel taking its cell's commonest colour
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


def carving_states():
    from collections import Counter
    srcs = [Image.open(os.path.join(OUT, "carving", f"state_{n}.png")).convert("RGBA") for n in range(6)]
    cut = []
    for im in srcs:
        px = im.load()
        w, h = im.size
        bg = px[2, 2]
        close = lambda c: sum(abs(c[i] - bg[i]) for i in range(3)) < 48
        seen, todo = set(), [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
        while todo:
            x, y = todo.pop()
            if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not close(px[x, y]):
                continue
            seen.add((x, y))
            todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
        for x, y in seen:
            px[x, y] = (0, 0, 0, 0)
        cut.append(im)
    # one box and one scale for all six, so the carving doesn't jump between states
    boxes = [im.getbbox() for im in cut]
    box = (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))
    W, H = 32, 48
    s = min(W / (box[2] - box[0]), H / (box[3] - box[1]))
    nw, nh = round((box[2] - box[0]) * s), round((box[3] - box[1]) * s)
    for n, im in enumerate(cut):
        ip = im.load()
        out = Image.new("RGBA", (W, H))
        op = out.load()
        ox, oy = (W - nw) // 2, H - nh
        for ty in range(nh):
            for tx in range(nw):
                x0, y0 = box[0] + int(tx / s), box[1] + int(ty / s)
                x1, y1 = max(x0 + 1, box[0] + int((tx + 1) / s)), max(y0 + 1, box[1] + int((ty + 1) / s))
                cells = [ip[x, y] for y in range(y0, min(y1, im.height)) for x in range(x0, min(x1, im.width))]
                opaque = [c for c in cells if c[3] > 128]
                if len(opaque) * 2 < len(cells):
                    continue
                q = Counter((c[0] // 12, c[1] // 12, c[2] // 12) for c in opaque).most_common(1)[0][0]
                pick = [c for c in opaque if (c[0] // 12, c[1] // 12, c[2] // 12) == q]
                op[ox + tx, oy + ty] = tuple(sum(c[i] for c in pick) // len(pick) for i in range(3)) + (255,)
        save(out, f"bram_carving_{n}")
    # The "nearly finished" edit left a rough patch too small to see at game size (the user,
    # 2026-11-09: no difference between 8 and 10 hearts). So state 4 is the finished carving
    # with the top of its crown, slanting larger to the upper right, still Bram's rough block
    # (state 1's pixels): plainly the part she hasn't done.
    done = Image.open(os.path.join(OUT, "bram_carving_5.png")).convert("RGBA")
    rough = Image.open(os.path.join(OUT, "bram_carving_1.png")).convert("RGBA")
    dp, rp = done.load(), rough.load()
    for y in range(48):
        for x in range(32):
            if y <= 8 + max(0, x - 12) // 3 and (rp[x, y][3] or dp[x, y][3]):
                dp[x, y] = rp[x, y]
    save(done, "bram_carving_4")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for f in (north_window, workbench, chisel_board, counter, carving_states, bedroll, shavings):
        f()
    print("wrote", OUT)

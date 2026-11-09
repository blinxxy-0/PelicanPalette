"""Studio II's own pieces, the Corrie twins' (docs/design/vale_newcomers.md, section 5). The six
biggest are PixelLab (Pro Flash, one each, styled on the Vale's approved furniture; the user,
2026-11-09: the hand-drawn ones looked bad): studio2/pixellab/glass_counter, cutting_bench,
light_table, mabel_bench, glass_racks, modern_kiln. Drawn here, in the game's palette, until the
rest get PixelLab art after the allowance refills (Nov 26):

  mabel_bench_used the PixelLab bench in use (Jet's 10 hearts): her pattern book open, a panel half leaded

  offcut_bins      the seedhouse batch's spare seed trays, full of offcuts sorted by colour
  studio_window    drawn: the big studio window, eight plain panes
  mabel_window     drawn: the same window with Mabel's design leaded into it (Jet's 10 hearts):
                   the Vale at dawn, gold trees each side of the river, the sun coming up
  glass_lamp       drawn: their own stained-glass floor lamp, a dome of ruby, amber and cobalt
  drafting_table   drawn: Opal's tilted board, a window design half coloured in
  design_sketches  drawn: her designs pinned up, three of them, coloured in
  leaning_panels   drawn: two finished panels in their frames, leaning back
  colour_pool      drawn: the colour the racks throw on the floor
  sketch_papers    drawn: sketches dropped on the floor
  split_line       drawn: the black line down the middle, on a tile's right edge

Usage: vale_corrie_art.py   (writes art/gilded_vale/interiors/studio2/*.png and a preview)
"""
import os
from PIL import Image, ImageDraw

INT = "art/gilded_vale/interiors"
OUT = os.path.join(INT, "studio2")

# the game's furniture colours
OUTL = (56, 30, 18, 255)
WOOD_D, WOOD, WOOD_L, WOOD_H = (102, 54, 26, 255), (148, 84, 38, 255), (186, 116, 56, 255), (214, 152, 84, 255)
OLD_D, OLD, OLD_L, OLD_H = (88, 60, 40, 255), (128, 92, 62, 255), (160, 124, 88, 255), (192, 162, 122, 255)
LEAD = (44, 44, 52, 255)
STEEL_D, STEEL, STEEL_L, STEEL_H = (70, 78, 90, 255), (124, 134, 148, 255), (168, 178, 190, 255), (214, 222, 230, 255)
BRASS_D, BRASS, BRASS_L = (128, 88, 30, 255), (190, 142, 52, 255), (236, 196, 96, 255)
RUBY = [(120, 14, 40, 255), (184, 28, 64, 255), (232, 82, 110, 255)]
COBALT = [(24, 40, 120, 255), (44, 80, 186, 255), (104, 148, 236, 255)]
AMBER = [(150, 86, 14, 255), (224, 150, 36, 255), (250, 206, 96, 255)]
GREEN = [(28, 96, 52, 255), (52, 150, 76, 255), (126, 206, 116, 255)]
VIOLET = [(76, 34, 118, 255), (122, 66, 170, 255), (178, 132, 220, 255)]
GLASSES = [RUBY, COBALT, AMBER, GREEN, VIOLET]
SKY, SKY_L, SKY_D = (150, 196, 230, 255), (196, 226, 246, 255), (112, 160, 206, 255)
PAPER, PAPER_D = (244, 236, 214, 255), (210, 196, 166, 255)


def new(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def rect(d, x0, y0, x1, y1, fill=None, outline=None):
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=outline)


def load(path):
    return Image.open(os.path.join(INT, path + ".png")).convert("RGBA")


def crop(im):
    return im.crop(im.getbbox())


def panel(d, x0, y0, x1, y1, cells, lead=LEAD):
    """A leaded panel: its cells (x0, y0, x1, y1, colour ramp) inside a lead frame."""
    for cx0, cy0, cx1, cy1, ramp in cells:
        rect(d, cx0, cy0, cx1, cy1, fill=ramp[1])
        d.line([(cx0, cy0), (cx1, cy0)], fill=ramp[2])
        d.point((cx0, cy0 + 1), fill=ramp[2])
        d.line([(cx0, cy1), (cx1, cy1)], fill=ramp[0])
        rect(d, cx0, cy0, cx1, cy1, outline=lead)
    rect(d, x0, y0, x1, y1, outline=lead)


# ------------------------------------------------------------------ windows

def window_frame(w=64, h=32):
    im = new(w, h)
    d = ImageDraw.Draw(im)
    rect(d, 0, 0, w - 1, h - 1, fill=WOOD, outline=OUTL)          # the frame
    d.line([(1, 1), (w - 2, 1)], fill=WOOD_H)
    d.line([(1, h - 4), (w - 2, h - 4)], fill=WOOD_D)
    rect(d, 0, h - 3, w - 1, h - 1, fill=WOOD_L, outline=OUTL)      # the sill
    d.line([(1, h - 3), (w - 2, h - 3)], fill=WOOD_H)
    return im, d


def studio_window():
    im, d = window_frame()
    for i in range(4):                       # eight panes, two rows, mullions between
        for j in range(2):
            x0, y0 = 3 + i * 15, 3 + j * 12
            rect(d, x0, y0, x0 + 12, y0 + 9, fill=SKY)
            d.line([(x0, y0), (x0 + 12, y0)], fill=SKY_L)
            d.line([(x0 + 1, y0 + 2), (x0 + 4, y0 + 2)], fill=SKY_L)
            d.line([(x0, y0 + 9), (x0 + 12, y0 + 9)], fill=SKY_D)
            d.point((x0 + 9, y0 + 6), fill=SKY_L)
    return im


def mabel_window():
    """Her design: the Vale at dawn: a gold sun rising over hills, gold trees each side
    of the blue river running down the middle, all leaded."""
    im, d = window_frame()
    x0, y0, x1, y1 = 3, 3, 60, 26
    # sky in bands, the sun
    rect(d, x0, y0, x1, y1, fill=AMBER[2])
    rect(d, x0, y0, x1, y0 + 5, fill=(246, 168, 120, 255))
    rect(d, x0, y0 + 6, x1, y0 + 10, fill=AMBER[2])
    d.ellipse([27, 6, 36, 15], fill=(255, 236, 160, 255), outline=LEAD)
    # hills
    d.polygon([(x0, 20), (14, 13), (26, 18), (x0, 18)], fill=GREEN[1])
    d.polygon([(x0, y1), (x0, 18), (16, 14), (30, 19), (44, 13), (x1, 18), (x1, y1)], fill=GREEN[1], outline=LEAD)
    d.polygon([(x0, y1), (x0, 21), (20, 19), (x1, 21), (x1, y1)], fill=GREEN[0], outline=LEAD)
    # the river, widening toward us
    d.polygon([(30, 17), (33, 17), (40, y1), (23, y1)], fill=COBALT[1], outline=LEAD)
    d.line([(31, 19), (29, 23)], fill=COBALT[2])
    # gold trees each side
    for tx, ty in ((9, 14), (16, 16), (47, 15), (54, 13)):
        d.ellipse([tx - 4, ty - 4, tx + 4, ty + 3], fill=AMBER[1], outline=LEAD)
        d.point((tx - 2, ty - 2), fill=AMBER[2])
        d.line([(tx, ty + 3), (tx, ty + 7)], fill=LEAD)
    # the leading: the mullions stay, a few lead lines through the sky
    for i in range(1, 4):
        d.line([(3 + i * 15 - 2, y0), (3 + i * 15 - 2, y1)], fill=WOOD_D)
    d.line([(x0, 14), (x1, 14)], fill=WOOD_D)
    for lx in (10, 20, 44, 52):
        d.line([(lx, y0), (lx + 2, 10)], fill=LEAD)
    rect(d, x0, y0, x1, y1, outline=LEAD)
    return im


# ------------------------------------------------------------------ benches and tables

def drafting_table():
    im = new(32, 32)
    d = ImageDraw.Draw(im)
    # legs and the stand
    for lx in (4, 25):
        rect(d, lx, 14, lx + 2, 31, fill=WOOD_D, outline=OUTL)
    d.line([(6, 26), (25, 26)], fill=WOOD_D)
    # the tilted board, a design on its paper, half coloured in
    d.polygon([(1, 18), (30, 18), (27, 2), (4, 2)], fill=WOOD_L, outline=OUTL)
    d.polygon([(4, 16), (27, 16), (25, 4), (6, 4)], fill=PAPER, outline=PAPER_D)
    d.arc([10, 5, 21, 20], 180, 360, fill=LEAD)                       # an arched window, drawn
    d.line([(10, 12), (10, 15)], fill=LEAD)
    d.line([(21, 12), (21, 15)], fill=LEAD)
    d.polygon([(11, 10), (15, 7), (15, 15), (11, 15)], fill=RUBY[1])   # coloured in, one side
    d.polygon([(16, 7), (20, 10), (20, 12), (16, 12)], fill=AMBER[1])
    d.line([(15, 6), (15, 15)], fill=LEAD)
    rect(d, 1, 17, 30, 19, fill=WOOD, outline=OUTL)                   # the ledge, pencils on it
    for k, c in enumerate((RUBY[1], COBALT[1], AMBER[1], GREEN[1], VIOLET[1])):
        d.line([(4 + k * 4, 18), (6 + k * 4, 18)], fill=c)
    return im


def design_sketches():
    im = new(48, 32)
    d = ImageDraw.Draw(im)
    sheets = [(1, 3, 15, 22, -1), (14, 1, 30, 24, 0), (30, 4, 46, 26, 1)]
    for k, (x0, y0, x1, y1, tilt) in enumerate(sheets):
        d.polygon([(x0, y0 + max(tilt, 0)), (x1, y0 - min(tilt, 0)), (x1, y1 - min(tilt, 0)), (x0, y1 + max(tilt, 0))],
                  fill=PAPER, outline=PAPER_D)
        cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        ramps = GLASSES[k:] + GLASSES[:k]
        # each a different window: a rose, a pointed arch, a grid of squares
        if k == 0:
            d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=ramps[0][1], outline=LEAD)
            d.ellipse([cx - 2, cy - 2, cx + 2, cy + 2], fill=ramps[2][2], outline=LEAD)
            d.line([(cx - 5, cy), (cx + 5, cy)], fill=LEAD)
            d.line([(cx, cy - 5), (cx, cy + 5)], fill=LEAD)
        elif k == 1:
            d.polygon([(cx - 5, cy + 7), (cx - 5, cy - 2), (cx, cy - 8), (cx + 5, cy - 2), (cx + 5, cy + 7)],
                      fill=ramps[0][1], outline=LEAD)
            d.polygon([(cx - 5, cy + 7), (cx - 5, cy + 1), (cx + 5, cy + 1), (cx + 5, cy + 7)], fill=ramps[1][1], outline=LEAD)
            d.line([(cx, cy - 8), (cx, cy + 7)], fill=LEAD)
        else:
            for i in range(3):
                for j in range(3):
                    rect(d, cx - 6 + i * 4, cy - 6 + j * 4, cx - 3 + i * 4, cy - 3 + j * 4,
                         fill=ramps[(i + j) % 4][1], outline=LEAD)
        d.point((cx, y0 + 1), fill=RUBY[1])                         # a pin
    return im


def leaning_panels():
    im = new(16, 32)
    d = ImageDraw.Draw(im)
    # two framed panels leaning back on the counter, the front one shorter
    d.polygon([(3, 31), (11, 31), (14, 7), (6, 7)], fill=WOOD_D, outline=OUTL)
    d.polygon([(5, 29), (10, 29), (12, 10), (7, 10)], fill=COBALT[1], outline=LEAD)
    d.line([(6, 20), (11, 20)], fill=LEAD)
    d.polygon([(6, 19), (11, 19), (12, 11), (7, 11)], fill=AMBER[1])
    d.polygon([(1, 31), (8, 31), (10, 16), (3, 16)], fill=WOOD, outline=OUTL)
    d.polygon([(3, 29), (7, 29), (8, 18), (4, 18)], fill=RUBY[1], outline=LEAD)
    d.line([(4, 24), (8, 24)], fill=LEAD)
    d.point((5, 20), fill=RUBY[2])
    return im


def glass_lamp():
    im = new(16, 48)
    d = ImageDraw.Draw(im)
    rect(d, 4, 44, 11, 47, fill=BRASS, outline=BRASS_D)                 # the base
    rect(d, 7, 18, 8, 44, fill=BRASS)                                   # the pole
    d.line([(7, 18), (7, 44)], fill=BRASS_L)
    # the dome: segments of ruby, amber and cobalt, leaded, lit from inside
    d.pieslice([0, 4, 15, 26], 180, 360, fill=AMBER[2], outline=LEAD)
    d.rectangle([0, 14, 15, 18], fill=AMBER[1], outline=LEAD)
    for k, ramp in enumerate((RUBY, AMBER, COBALT, AMBER, RUBY)):
        x = 1 + k * 3
        d.rectangle([x, 15, x + 2, 17], fill=ramp[2])
    d.line([(4, 6), (2, 14)], fill=LEAD)
    d.line([(8, 4), (8, 14)], fill=LEAD)
    d.line([(11, 6), (13, 14)], fill=LEAD)
    d.polygon([(3, 9), (5, 7), (7, 7), (7, 13), (3, 13)], fill=RUBY[2])
    d.polygon([(9, 7), (11, 7), (13, 10), (13, 13), (9, 13)], fill=COBALT[2])
    d.point((8, 2), fill=BRASS_L)
    d.line([(8, 2), (8, 4)], fill=BRASS)
    return im


def colour_pool():
    im = new(32, 32)
    d = ImageDraw.Draw(im, "RGBA")
    # slanting patches of coloured light, softer at the edges
    for k, ramp in enumerate((RUBY, COBALT, AMBER, GREEN)):
        x = 2 + k * 7
        c = ramp[2][:3]
        d.polygon([(x, 2), (x + 6, 2), (x + 9, 29), (x + 3, 29)], fill=c + (105,))
        d.polygon([(x + 1, 6), (x + 5, 6), (x + 7, 25), (x + 3, 25)], fill=c + (95,))
    return im


def sketch_papers():
    im = new(16, 16)
    d = ImageDraw.Draw(im)
    d.polygon([(1, 5), (10, 2), (13, 11), (4, 14)], fill=PAPER, outline=PAPER_D)
    d.arc([5, 5, 10, 11], 200, 340, fill=LEAD)
    d.point((7, 9), fill=RUBY[1])
    d.point((8, 8), fill=COBALT[1])
    d.polygon([(8, 11), (15, 9), (15, 15), (9, 15)], fill=PAPER, outline=PAPER_D)
    d.line([(10, 12), (13, 12)], fill=AMBER[1])
    d.line([(10, 13), (12, 13)], fill=GREEN[1])
    return im


def split_line():
    im = new(16, 16)
    d = ImageDraw.Draw(im)
    rect(d, 14, 0, 15, 15, fill=(30, 28, 32, 255))
    d.point((14, 5), fill=(52, 50, 56, 255))
    d.point((15, 11), fill=(52, 50, 56, 255))
    return im


# ------------------------------------------------------------------ re-dressed candidates

def offcut_bins():
    """The spare seed trays: the seedlings out, offcuts in, sorted: ruby and amber in one,
    cobalt and green in the other, a divider down each."""
    im = crop(load("fern/fern_v1/seed_trays_1_0"))             # 30 x 21: trays at x 1-12 and 16-27
    d = ImageDraw.Draw(im)
    for tx0, tx1, ramps in ((2, 11, (RUBY, AMBER)), (17, 26, (COBALT, GREEN))):
        mid = (tx0 + tx1) // 2
        for y in range(2, 10):
            for x in range(tx0, tx1 + 1):
                ramp = ramps[0] if x < mid else ramps[1]
                im.putpixel((x, y), ramp[(x * 5 + y * 3 + (x * y) % 4) % 3])
        d.line([(mid, 2), (mid, 9)], fill=(40, 40, 46, 255))
    return im


def bench_in_use():
    """Mabel's bench (PixelLab, studio2/pixellab/mabel_bench) in use (Jet's 10 hearts): her
    pattern book open over her pliers, a panel half leaded where the blue offcut was."""
    im = Image.open(os.path.join(OUT, "pixellab", "mabel_bench.png")).convert("RGBA")
    d = ImageDraw.Draw(im)
    rect(d, 5, 5, 15, 11, fill=PAPER, outline=OUTL)
    d.line([(10, 5), (10, 11)], fill=PAPER_D)
    d.arc([6, 6, 9, 10], 180, 360, fill=COBALT[1])
    d.line([(6, 9), (9, 9)], fill=PAPER_D)
    for y in (7, 9):
        d.line([(12, y), (14, y)], fill=PAPER_D)
    panel(d, 17, 5, 27, 11, [(17, 5, 22, 11, AMBER), (22, 5, 27, 8, GREEN), (22, 8, 27, 11, RUBY)])
    return im


PIECES = {
    "studio_window": studio_window, "mabel_window": mabel_window,
    "mabel_bench_used": lambda: bench_in_use(),
    "drafting_table": drafting_table,
    "design_sketches": design_sketches, "leaning_panels": leaning_panels, "glass_lamp": glass_lamp,
    "colour_pool": colour_pool, "sketch_papers": sketch_papers,
    "split_line": split_line, "offcut_bins": offcut_bins,
    
}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    made = {}
    for name, fn in PIECES.items():
        im = fn()
        im.save(os.path.join(OUT, name + ".png"))
        made[name] = im
    # a preview on the floor's tan, at 4x
    k, pad = 4, 8
    widths = [im.width * k + pad for im in made.values()]
    rows, row, x = [], [], 0
    for name, im in made.items():
        if x + im.width * k > 1400:
            rows.append(row)
            row, x = [], 0
        row.append(name)
        x += im.width * k + pad
    rows.append(row)
    H = sum(max(made[n].height for n in r) * k + pad for r in rows)
    sheet = Image.new("RGBA", (1400, H), (222, 200, 160, 255))
    y = 0
    for r in rows:
        x = 0
        for n in r:
            im = made[n]
            sheet.alpha_composite(im.resize((im.width * k, im.height * k), Image.NEAREST), (x, y))
            x += im.width * k + pad
        y += max(made[n].height for n in r) * k + pad
    sheet.save(os.path.join(OUT, "preview.png"))
    print("wrote", len(made), "pieces and", os.path.join(OUT, "preview.png"))

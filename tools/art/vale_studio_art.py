"""The Old Studio's own pieces, made without new PixelLab generations (the user,
2026-11-07: keep the PixelLab allowance for the characters). From candidates
earlier batches made and nothing used, edited to fit, and a few drawn here:

  dusty_window       the atelier batch's spare white window, greyed with dust, two panes cracked, a cobweb
  pinned_sketches    two of the hall batch's spare posters, turned to charcoal on old paper, pinned overlapping
  unfinished_easel   a spare easel, half its canvas still bare sketch lines
  note_table         a spare paint table with a folded note on it
  backward_painting  drawn: a canvas hung facing the wall, its stretcher bars and a torn label
  cobweb, leaves     drawn
  (and Emmett's moss_patch: the moss of a candidate drawn on a floorboard, without the board)

Usage: vale_studio_art.py   (writes art/gilded_vale/interiors/studio/*.png)
"""
import os, random
from PIL import Image, ImageDraw

INT = "art/gilded_vale/interiors"
OUT = os.path.join(INT, "studio")


def load(path):
    return Image.open(os.path.join(INT, path + ".png")).convert("RGBA")


def grey(c, amount, tint=(150, 150, 146)):
    """c faded toward a dusty grey."""
    l = (c[0] * 30 + c[1] * 59 + c[2] * 11) // 100
    return tuple(int(c[k] * (1 - amount) + (l * 0.6 + tint[k] * 0.4) * amount) for k in range(3)) + (c[3],)


def dusty_window():
    im = load("atelier/atelier_v1/north_window_1_11")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3]:
                px[x, y] = grey(px[x, y], 0.55)
    # the panes: glass, not holes (the user, 2026-11-07: "the glass has bricks behind it").
    # Whatever's transparent and not reachable from the picture's edge is inside the frame.
    outside, todo = set(), [(x, y) for x in range(im.width) for y in (0, im.height - 1)] +                            [(x, y) for y in range(im.height) for x in (0, im.width - 1)]
    while todo:
        x, y = todo.pop()
        if (x, y) in outside or not (0 <= x < im.width and 0 <= y < im.height) or px[x, y][3] > 40:
            continue
        outside.add((x, y))
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    top = im.getbbox()[1]
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3] <= 40 and (x, y) not in outside:
                t = (y - top) / max(1, im.height - top)
                px[x, y] = (int(196 - 40 * t), int(206 - 36 * t), int(212 - 30 * t), 255)   # grey daylight, dimmer low
    d = ImageDraw.Draw(im)
    box = im.getbbox()
    # two cracked panes: a jagged pale line each
    for cx, cy in ((box[0] + 14, box[1] + 8), (box[2] - 22, box[1] + 14)):
        pts = [(cx, cy), (cx + 3, cy + 3), (cx + 2, cy + 6), (cx + 5, cy + 9)]
        d.line(pts, fill=(222, 226, 228, 255))
    # a cobweb in the top-left corner
    x0, y0 = box[0] + 2, box[1] + 2
    for i in range(6):
        d.line([(x0, y0), (x0 + 7 - i, y0 + i * 1.2)], fill=(214, 214, 210, 150))
    d.arc([x0 - 4, y0 - 4, x0 + 5, y0 + 5], 0, 90, fill=(214, 214, 210, 150))
    im.save(os.path.join(OUT, "dusty_window.png"))


def charcoal(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a:
                l = (r * 30 + g * 59 + b * 11) // 100
                if l > 150:
                    px[x, y] = (226, 214, 186, a)           # old paper
                else:
                    v = 40 + l // 3                          # charcoal
                    px[x, y] = (v, v - 2, v - 6, a)
    return im


def pinned_sketches():
    a = charcoal(load("hall/hall_v1/poster_1_3"))
    b = charcoal(load("hall/hall_v1/poster_1_8"))
    out = Image.new("RGBA", (48, 32))
    out.alpha_composite(a.crop(a.getbbox()), (1, 2))
    bb = b.crop(b.getbbox())
    out.alpha_composite(bb, (48 - bb.width - 1, 5))
    out.save(os.path.join(OUT, "pinned_sketches.png"))


def unfinished_easel():
    im = load("atelier/atelier_v1/easel_big_2_0")
    px = im.load()
    # the canvas: the bright cream area; its right half goes back to bare canvas and pencil
    cells = [(x, y) for y in range(im.height) for x in range(im.width)
             if px[x, y][3] and px[x, y][0] > 200 and px[x, y][1] > 190 and px[x, y][2] > 150]
    xs = [x for x, _ in cells]
    ys = [y for _, y in cells]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    mid = (x0 + x1) // 2
    for y in range(y0, y1 + 1):
        for x in range(mid, x1 + 1):
            if px[x, y][3] and (px[x, y][0] + px[x, y][1] + px[x, y][2]) > 240:
                px[x, y] = (236, 228, 206, 255)
    d = ImageDraw.Draw(im)
    pencil = (150, 140, 128, 255)
    d.line([(mid, y0 + (y1 - y0) * 2 // 3), (x1 - 1, y0 + (y1 - y0) // 2)], fill=pencil)     # the hills, sketched
    d.line([(mid + 1, y0 + 3), (mid + 3, y0 + 5), (x1 - 2, y0 + 3)], fill=pencil)
    im.save(os.path.join(OUT, "unfinished_easel.png"))


def note_table():
    im = load("atelier/atelier_v1/paint_table_1_9")
    box = im.getbbox()
    d = ImageDraw.Draw(im)
    nx, ny = box[0] + 3, box[1] + 9
    d.rectangle([nx, ny, nx + 4, ny + 2], fill=(244, 238, 222, 255), outline=(150, 132, 112, 255))   # the note, folded
    d.line([(nx + 1, ny + 1), (nx + 3, ny + 1)], fill=(110, 100, 96, 255))
    im.save(os.path.join(OUT, "note_table.png"))


def backward_painting():
    im = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(im)
    canvas, bar, dark, label = (206, 190, 156, 255), (150, 104, 64, 255), (92, 60, 40, 255), (232, 226, 206, 255)
    d.rectangle([3, 5, 28, 26], fill=canvas, outline=dark)          # the back of the canvas
    d.rectangle([4, 6, 27, 8], fill=bar)                            # stretcher bars round it
    d.rectangle([4, 23, 27, 25], fill=bar)
    d.rectangle([4, 6, 6, 25], fill=bar)
    d.rectangle([25, 6, 27, 25], fill=bar)
    d.rectangle([15, 9, 16, 22], fill=bar)                          # and its cross brace
    for y in (8, 25):
        d.line([(4, y), (27, y)], fill=dark)
    d.rectangle([18, 15, 23, 19], fill=label, outline=(170, 160, 140, 255))   # a torn label
    d.point((23, 19), fill=(0, 0, 0, 0))
    d.line([(19, 17), (22, 17)], fill=(120, 110, 100, 255))
    d.line([(15, 1), (8, 5)], fill=(90, 80, 70, 255))                # its cord, on a nail
    d.line([(16, 1), (23, 5)], fill=(90, 80, 70, 255))
    d.point((15, 1), fill=(176, 140, 60, 255))
    im.save(os.path.join(OUT, "backward_painting.png"))


def cobweb():
    im = Image.new("RGBA", (16, 16))
    d = ImageDraw.Draw(im)
    web = (220, 220, 216, 160)
    for i in range(5):
        d.line([(15, 0), (15 - 13 + i * 2, i * 3)], fill=web)
    for r in (5, 9, 13):
        d.arc([15 - r, -r, 15 + r, r], 90, 180, fill=web)
    im.save(os.path.join(OUT, "cobweb.png"))


def leaves():
    rnd = random.Random(4)
    im = Image.new("RGBA", (16, 16))
    px = im.load()
    for _ in range(7):
        x, y = rnd.randrange(1, 14), rnd.randrange(2, 14)
        c = rnd.choice([(176, 96, 40), (150, 70, 34), (196, 132, 52), (122, 76, 40)])
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            if rnd.random() < 0.8:
                px[x + dx, y + dy] = c + (255,)
        px[x + 2, y + 1] = (90, 56, 30, 255)                          # a stalk
    im.save(os.path.join(OUT, "leaves.png"))


def moss_patch():
    """Emmett's floor moss: the batch drew it on a scrap of floorboard; keep the moss."""
    im = load("emmett/emmett_v1/moss_patch_1_6")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and not (g > r + 8 and g > b):
                px[x, y] = (0, 0, 0, 0)
    im.save(os.path.join(INT, "emmett", "moss_patch.png"))


def beam():
    """Emmett's ivy bracket without its ivy: the bare beam (the user, 2026-11-07: by the
    bookcase, where the ivy hung over it)."""
    im = load("emmett/emmett_v1/moss_wall_1_1")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and g > r:
                px[x, y] = (0, 0, 0, 0)
    im.save(os.path.join(INT, "emmett", "beam.png"))


# the beam's own colours (its rows, top down)
WOOD = {"O": (65, 22, 28), "L": (190, 120, 64), "M": (146, 84, 43), "D": (110, 55, 37), "B": (76, 44, 32),
        "H": (226, 168, 104)}
# a carved end for a beam: a round boss where the wood stops, a little scroll bracket under
# it (x 0-6 of the left end; the right end is its mirror)
FINIAL = [".OOOOO.",
          "OLLLLLO",
          "OLHHLLL",
          "OLHLMMM",
          "OLLMMDD",
          "OMMMDDB",
          ".ODDDO.",
          "..OMMO.",
          "..OLDO.",
          "...OMDO",
          "....OO.",
          "...OMO.",
          "...OO.."]


def beam_ends():
    """Ends for Emmett's beams (the user, 2026-11-07): in the nook the wood stops behind its
    lamp (beam_l, beam_r: the bare beam, chopped past the lamp's middle); in his room the
    ivy pieces at the beam's two ends get a carved finial (moss_wall_l, moss_wall_r)."""
    bare = load("emmett/beam")
    for name, keep in (("beam_l", range(7, 16)), ("beam_r", range(0, 9))):
        im = bare.copy()
        px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                if x not in keep:
                    px[x, y] = (0, 0, 0, 0)
        edge = keep[0] if name == "beam_l" else keep[-1]
        for y in range(6):
            if px[edge, y][3]:
                px[edge, y] = WOOD["O"] + (255,)          # the cut end, outlined
        im.save(os.path.join(INT, "emmett", name + ".png"))
    ivy = load("emmett/emmett_v1/moss_wall_1_1")
    for name, mirror in (("moss_wall_l", False), ("moss_wall_r", True)):
        im = ivy.transpose(Image.FLIP_LEFT_RIGHT) if mirror else ivy.copy()
        px = im.load()
        for y in range(13):
            for x in range(7):
                r, g, b, a = px[x, y]
                if a and not g > r:                       # the wood there goes (the ivy stays)
                    px[x, y] = (0, 0, 0, 0)
        for y, row in enumerate(FINIAL):
            for x, c in enumerate(row):
                if c != ".":
                    px[x, y] = WOOD[c] + (255,)
        if mirror:
            im = im.transpose(Image.FLIP_LEFT_RIGHT)
        im.save(os.path.join(INT, "emmett", name + ".png"))


if __name__ == "__main__":
    for f in (beam, beam_ends, moss_patch, dusty_window, pinned_sketches, unfinished_easel, note_table, backward_painting, cobweb, leaves):
        f()
        print("wrote", f.__name__)

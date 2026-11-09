"""Art candidates for the painters' guide, the Gilded Grove and the graveyard
(docs/superpowers/specs/2026-11-08-vale-painters-guide-grove-graveyard-design.md, sections 5 and 6;
the words they must match: docs/superpowers/specs/2026-11-08-vale-guide-text.md).

CANDIDATES only: nothing here is wired into a map or an asset sheet. The user picks first.

Two kinds of piece:
  - PixelLab (art/gilded_vale/props/vale_guide_v1/pixellab/): three headstone bases (Pro Flash, 16x32,
    the game's own tombstone as the style image) and the four echoes in two looks each (Pro Flash,
    32x48, a villager sprite as the scale reference). This script only adds the headstones' carvings.
  - Drawn here (no PixelLab): the carvings, Bram's plain stone, the clay markers, the urns, the wall and
    gate (from the game's own bridge parapet, recoloured), both easels and the return frame (from our
    Easel item's sprite, tools/art/easel.py), the Vale panorama (a render of GildedVale.tmx), the guide
    icon, the painted fish (vale_fish_icons.py's drawing) and the Founders' Frame (frame_styles.py's way).

    python tools/art/vale_guide_candidates.py [<vanilla exports dir>]

Writes each candidate at 1x to art/gilded_vale/props/vale_guide_v1/, and candidates.png: everything at
3x, each row beside the game's tombstone and our easel for scale, labelled with the ids the user picks by.
"""
import math
import os
import subprocess
import sys
import tempfile
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
import vale_fish_icons as fishes          # noqa: E402
from set_frames import recolour as frame_recolour   # noqa: E402
from frame_styles import STYLES as FRAME_STYLES, touch as frame_touch   # noqa: E402
from vanillafy import outline_shade      # noqa: E402

EXPORTS = sys.argv[1] if len(sys.argv) > 1 else os.environ.get(
    "VALE_EXPORTS", "C:/Users/blinx/Desktop/MuseStardew/.devgame/exports_vale")
OUT = os.path.join(ROOT, "art", "gilded_vale", "props", "vale_guide_v1")
PL = os.path.join(OUT, "pixellab")
ASSETS = os.path.join(ROOT, "ArtStudio", "assets")
VALE_TMX = os.path.join(ROOT, "[CP] Gilded Vale (dev)", "assets", "GildedVale.tmx")
GRASS = (104, 148, 72, 255)


# ---- small helpers -------------------------------------------------------------

def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3])


def lum(c):
    return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def outline(im, colour_of=None):
    """A one-pixel outline round the opaque shape: each outline pixel a darker shade of the colour
    it borders (the Vale's rule: outlines as a darker shade of their neighbour, never black)."""
    px = im.load()
    w, h = im.size
    add = {}
    for y in range(h):
        for x in range(w):
            if px[x, y][3]:
                continue
            near = [px[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 255]
            if near:
                base = max(near, key=lambda q: lum(q))[:3] if colour_of is None else colour_of
                add[(x, y)] = outline_shade(base) + (255,)
    for k, v in add.items():
        px[k] = v
    return im


def tufts(im, y, x0, x1, seed=0):
    """A few grass tufts along a piece's foot, in the game's grass greens."""
    px = im.load()
    greens = [(81, 114, 49), (111, 166, 35), (120, 142, 83)]
    for i, x in enumerate(range(x0, x1 + 1)):
        k = (x * 7 + seed * 3) % 5
        if k in (0, 3):
            px[x, y] = greens[k % 3] + (255,)
            if k == 0 and y - 1 >= 0:
                px[x, y - 1] = greens[1] + (255,)
    return im


def save(im, name):
    im.save(os.path.join(OUT, name + ".png"))
    return im


# ---- the carved headstones (A1-A3): PixelLab bases, carvings drawn here --------------

# Each base's smooth face, where the carving goes: (x0, y0, width, height), read off the pixels.
FACES = {"A1": (4, 12, 7, 11), "A2": (6, 15, 6, 9), "A3": (4, 12, 7, 10)}
GLYPHS = {
    "agnes": ["....#", "...#.", "..#..", ".##..", "##..."],         # oils: a brush, mid-stroke
    "hollis": ["#####", "#..##", "#.#.#", "##..#", "#####"],       # woodcuts: a block with a gouge's cut
    "mabel": ["..#..", ".#.#.", "#.#.#", ".#.#.", "..#.."],        # stained glass: a leaded diamond pane
}
MAKER_B = ["#..", "##.", "#.#", "##."]                             # Bram's mark, small, in a corner


def engrave(im, pattern, ox, oy, face, light=True):
    """Cut a pattern into stone: the cut a darker shade of the face, lit along its lower lip."""
    px = im.load()
    cut = set()
    for j, row in enumerate(pattern):
        for i, ch in enumerate(row):
            if ch == "#":
                cut.add((ox + i, oy + j))
    for (x, y) in cut:
        px[x, y] = shade(face, 0.62) + (255,)
    if light:
        for (x, y) in cut:
            if (x, y + 1) not in cut and px[x, y + 1][3]:
                r, g, b, a = px[x, y + 1]
                if abs(lum((r, g, b)) - lum(face)) < 24:
                    px[x, y + 1] = shade(face, 1.16) + (255,)


def headstones():
    out = {}
    for base, (fx, fy, fw, fh) in FACES.items():
        src = Image.open(os.path.join(PL, f"headstone_{base}.png")).convert("RGBA")
        face = src.getpixel((fx + fw // 2, fy + fh // 2))[:3]
        for who, glyph in GLYPHS.items():
            im = src.copy()
            gy = fy + (1 if fh >= 11 else 0)
            engrave(im, glyph, fx + (fw - 5) // 2, gy, face)
            engrave(im, MAKER_B, fx + fw - 3, fy + fh - 4, face, light=False)
            for dy in (fh - 3, fh - 1):                         # the name, as the game's own stones show it
                engrave(im, ["##" + "#" * (fw > 6)], fx, fy + dy, face, light=False)
            out[f"{base}_{who}"] = save(im, f"A_{base}_{who}")
    return out


# ---- Bram's plain stone (B1-B2): never carved, his name scratched on --------------------

STONE = [(51, 51, 51), (71, 73, 69), (99, 98, 98), (133, 133, 133), (151, 151, 151), (170, 170, 170), (196, 196, 194)]
LETTERS = {
    "B": ["##.", "#.#", "##.", "#.#", "##."],
    "R": ["##.", "#.#", "##.", "#.#", "#.#"],
    "A": [".#.", "#.#", "###", "#.#", "#.#"],
    "M": ["#.#", "###", "###", "#.#", "#.#"],
}


def rough_stone(w, h, rows, seed):
    """A rough, uncarved slab: rows[i] = (left, right) of row i from the top, lit from the upper left,
    flecked, never smoothed."""
    im = Image.new("RGBA", (w, h))
    px = im.load()
    top = h - len(rows) - 1
    for j, (l, r) in enumerate(rows):
        y = top + j
        for x in range(l, r + 1):
            u = (x - l) / max(1, r - l)                     # 0 left .. 1 right
            v = j / max(1, len(rows) - 1)                   # 0 top .. 1 bottom
            k = 5 - int(u * 2.6 + v * 1.6)
            if x == l or j == 0 or (j > 0 and (x < rows[j - 1][0] or x > rows[j - 1][1])):
                k = min(6, k + 1)                           # the lit upper and left edges
            if x == r:
                k = max(1, k - 1)
            n = (x * 37 + y * 23 + seed * 11) % 13
            if n == 0:
                k = max(1, k - 1)
            elif n == 5:
                k = min(6, k + 1)
            px[x, y] = STONE[max(1, min(6, k))] + (255,)
    return outline(im), top


def scratch(im, text_rows, ox, oy, base_light):
    """Letters scratched with a palette knife: pale, thin, not very neat (each letter nudged a little)."""
    px = im.load()
    for r, word in enumerate(text_rows):
        x = ox
        for n, ch in enumerate(word):
            wob = (0, 1, 0, -1)[(n + r) % 4] if (n + r) % 3 else 0
            for j, row in enumerate(LETTERS[ch]):
                for i, bit in enumerate(row):
                    if bit == "#":
                        y = oy + r * 6 + j + wob
                        if px[x + i, y][3]:
                            px[x + i, y] = base_light + (255,)
            x += 4
    return im


def bram_stones():
    # B1: an upright fieldstone, lumpy, a little lopsided; the name in two lines
    rows1 = [(5, 9), (4, 11), (3, 12), (3, 12), (2, 12), (2, 13), (2, 13), (2, 13), (2, 13), (2, 13), (2, 12),
             (2, 13), (2, 13), (3, 13), (2, 13), (2, 13), (2, 13), (2, 14), (1, 14)]
    b1, top = rough_stone(16, 32, rows1, 1)
    scratch(b1, ["BR", "AM"], 4, top + 4, (214, 212, 204))
    tufts(b1, 31, 1, 14, 1)
    # B2: a low split slab, wider than tall, the name crammed in under its sloping top
    rows2 = [(3, 8), (2, 11), (1, 13), (1, 14), (1, 14), (1, 14), (1, 14), (1, 14), (1, 14), (1, 14), (1, 14), (0, 15)]
    b2, top = rough_stone(16, 32, rows2, 2)
    scratch(b2, ["BRAM"], 1, top + 3, (214, 212, 204))
    tufts(b2, 31, 0, 15, 2)
    return {"B1": save(b1, "B1_bram"), "B2": save(b2, "B2_bram")}


# ---- the clay markers (C1-C2), three glazes ----------------------------------------------

GLAZES = {   # darkest to lightest: dark, mid, light, highlight
    "hester": [(70, 42, 88), (114, 76, 136), (152, 112, 170), (204, 172, 216)],        # heather purple
    "amos": [(38, 70, 38), (62, 106, 54), (94, 142, 76), (150, 190, 118)],              # hedge green
    "ada": [(66, 100, 92), (104, 146, 130), (146, 186, 164), (204, 228, 210)],          # her own: celadon
    "agnes": [(44, 84, 140), (70, 122, 186), (112, 164, 218), (190, 222, 246)],         # sky blue
    "hollis": [(24, 24, 32), (44, 46, 58), (72, 76, 92), (140, 146, 160)],              # printer's ink black
    "mabel": [(96, 18, 34), (146, 34, 50), (194, 62, 70), (240, 150, 140)],             # ruby glass red
    "bram": [(76, 48, 28), (112, 74, 42), (150, 106, 62), (204, 166, 112)],             # oak brown
}
BISQUE = [(106, 54, 34), (152, 84, 52), (190, 120, 78), (218, 160, 112)]   # the unglazed terracotta foot
TOBIN = {"hester", "amos"}


def clay_marker(shape, who):
    g = GLAZES[who]
    im = Image.new("RGBA", (16, 32))
    px = im.load()
    if shape == "C1":   # a rounded tablet on a flared foot: slimmer and lower than Bram's stones
        rows = [(5, 10), (4, 11)] + [(4, 11)] * 12 + [(3, 12), (3, 12), (3, 12)]
        hole = None
    else:               # a flat-topped tile with a rolled rim and a hole fired through near its top
        rows = [(5, 10)] + [(4, 11)] * 14 + [(3, 12), (3, 12)]
        hole = (7, 17)
    top = 31 - len(rows)
    foot = len(rows) - 3
    for j, (l, r) in enumerate(rows):
        y = top + j
        for x in range(l, r + 1):
            if j >= foot:
                ramp = BISQUE
                k = 2 if x < (l + r) / 2 else 1
            else:
                ramp = g
                u = (x - l) / max(1, r - l)
                k = 2 if u < 0.3 else 1
                if x == l + 1 and j > 1:
                    k = 3                                      # a glossy streak down the lit side
                if x == r:
                    k = 0
                elif (x * 7 + y * 5) % 17 == 0:
                    k = max(0, k - 1)                          # the glaze pooling, a pinhole here and there
            px[x, y] = ramp[k] + (255,)
    # glaze drips over the foot
    for x in range(rows[foot][0], rows[foot][1] + 1):
        if (x * 5 + len(who)) % 4 == 0:
            px[x, top + foot] = g[1] + (255,)
    # the name, slip-trailed in a pale glaze
    for dy, (a, b) in ((5, (7, 10)), (7, (7, 9)), (10, (7, 10))):
        for x in range(a, b + 1):
            if (x + dy) % 5:
                px[x, top + dy] = g[3] + (255,)
    if hole:
        hx, hy = hole
        for x, y in ((hx, hy), (hx + 1, hy)):
            px[x, y] = (0, 0, 0, 0)
    if shape == "C2":                                          # the rolled rim along the top
        for x in range(rows[1][0], rows[1][1] + 1):
            px[x, top + 1] = g[3] + (255,)
            px[x, top + 2] = g[0] + (255,)
    if who in TOBIN:                                           # Tobin's T, pressed into the clay foot
        y = top + foot + 1
        for x in (7, 8, 9):
            px[x, y - 1] = shade(BISQUE[1], 0.7) + (255,)
        px[8, y] = shade(BISQUE[1], 0.7) + (255,)
    outline(im)
    if hole:
        hx, hy = hole
        for x, y in ((hx, hy), (hx + 1, hy)):
            px[x, y] = outline_shade(g[1]) + (255,)
    tufts(im, 31, 3, 12, len(who))
    return im


def clay_markers():
    out = {}
    for shape in ("C1", "C2"):
        for who in ("ada", "hester", "amos"):
            out[f"{shape}_{who}"] = save(clay_marker(shape, who), f"{shape}_{who}")
    return out


# ---- the urns (U1-U2), seven glazes --------------------------------------------------------

URN_SHAPES = {   # half-widths from the top, the urn's centre at x 7.5
    "U1": [1, 3, 3, 2, 3, 4, 4, 4, 4, 3, 2],          # round-bellied, a knob on the lid
    "U2": [2, 3, 2, 2, 3, 3, 4, 4, 4, 3, 3, 2, 2],    # tall and ovoid, a flat lid, a narrow neck
}
LID_ROWS = {"U1": 3, "U2": 2}


def urn(shape, who):
    g = GLAZES[who]
    im = Image.new("RGBA", (16, 16))
    px = im.load()
    rows = URN_SHAPES[shape]
    top = 15 - len(rows)
    for j, hw in enumerate(rows):
        y = top + j
        for x in range(8 - hw, 8 + hw):
            u = (x - (8 - hw) + 0.5) / (2 * hw)            # 0 left .. 1 right
            k = 3 if u < 0.25 and j > LID_ROWS[shape] else (2 if u < 0.5 else (1 if u < 0.85 else 0))
            if j == LID_ROWS[shape]:
                k = 0                                     # the shadow under the lid
            px[x, y] = g[k] + (255,)
    if who == "ada":
        # her lid, glazed 'not yet' in iron brown: two little words' worth of squiggle
        for x, y in ((6, top + 1), (7, top + 2), (8, top + 1), (9, top + 2)):
            if px[x, y][3]:
                px[x, y] = (96, 56, 30, 255)
    outline(im)
    # a shadow at its foot, as the game's pots have
    for x in range(8 - rows[-1] - 1, 8 + rows[-1] + 1):
        if not px[x, 15][3]:
            px[x, 15] = (30, 40, 20, 90)
    return im


URN_ORDER = ["agnes", "hollis", "mabel", "bram", "ada", "hester", "amos"]


def urns():
    out = {}
    for shape in URN_SHAPES:
        for who in URN_ORDER:
            out[f"{shape}_{who}"] = save(urn(shape, who), f"{shape}_{who}")
    return out


# ---- the low wall and gate (W1-W2): the game's own bridge parapet, recoloured ----------------

def town_tile(sheet, tx, ty, w=1, h=1):
    return sheet.crop((tx * 16, ty * 16, (tx + w) * 16, (ty + h) * 16)).copy()


def drop_sand(im):
    """The parapet's end tiles carry a little of the bridge's yellow sand: clear it."""
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and r > 180 and g > 120 and b < 90 and r - b > 120:
                px[x, y] = (0, 0, 0, 0)
    return im


def to_ramp(im, ramp):
    """Recolour by lightness onto a ramp (darkest first), keeping alpha."""
    px = im.load()
    cols = sorted({px[x, y][:3] for y in range(im.height) for x in range(im.width) if px[x, y][3]}, key=lum)
    if not cols:
        return im
    lo, hi = lum(cols[0]), lum(cols[-1])
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a:
                t = (lum((r, g, b)) - lo) / max(1, hi - lo)
                px[x, y] = ramp[min(len(ramp) - 1, int(t * len(ramp)))] + (a,)
    return im


GREY_STONE = [(52, 54, 52), (78, 80, 76), (104, 106, 100), (132, 134, 126), (156, 156, 148), (180, 180, 170), (204, 202, 192)]


def gatepost(ramp):
    """A square stone post, 1x2: block courses and a cap, in the wall's own stone."""
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    d.rectangle([3, 10, 12, 30], fill=ramp[3])
    d.rectangle([3, 10, 4, 30], fill=ramp[4])
    d.rectangle([11, 10, 12, 30], fill=ramp[2])
    for y in (14, 18, 22, 26):
        d.line([(3, y), (12, y)], fill=ramp[1])
        x = 7 if (y // 4) % 2 else 9
        d.line([(x, y - 3), (x, y - 1)], fill=ramp[1])
    d.rectangle([2, 7, 13, 9], fill=ramp[5])               # the cap
    d.line([(2, 9), (13, 9)], fill=ramp[2])
    d.rectangle([5, 5, 10, 6], fill=ramp[5])               # a stone ball's base on the cap
    d.line([(6, 4), (9, 4)], fill=ramp[6])
    return outline(im)


def picket_gate():
    """A low wooden gate, 2 tiles wide, in the game's fence wood: pickets, two rails and a brace."""
    wood = [(62, 39, 36), (110, 66, 34), (162, 112, 52), (204, 152, 76), (226, 186, 110)]
    im = Image.new("RGBA", (32, 16))
    d = ImageDraw.Draw(im)
    for x in range(1, 31, 3):
        d.rectangle([x, 4, x + 1, 15], fill=wood[3])
        d.point((x, 3), fill=wood[4])
        d.line([(x + 1, 5), (x + 1, 15)], fill=wood[2])
    d.rectangle([0, 6, 31, 7], fill=wood[2])
    d.rectangle([0, 12, 31, 13], fill=wood[2])
    d.line([(0, 6), (31, 6)], fill=wood[3])
    d.line([(15, 7), (16, 7)], fill=wood[1])               # where the two leaves meet
    d.line([(15, 13), (16, 12)], fill=wood[1])
    for i in range(0, 5):
        d.point((2 + i * 2, 11 - i), fill=wood[1])
        d.point((29 - i * 2, 11 - i), fill=wood[1])
    return outline(im)


def iron_gate():
    """A low wrought-iron gate, 2 tiles wide: dark bars with little gilt tips, a scroll at the middle."""
    iron = [(28, 28, 34), (48, 48, 58), (76, 76, 88)]
    gilt = (220, 178, 80)
    im = Image.new("RGBA", (32, 16))
    d = ImageDraw.Draw(im)
    for x in range(1, 31, 3):
        d.line([(x, 4), (x, 15)], fill=iron[1])
        d.point((x, 3), fill=gilt)
    d.line([(0, 6), (31, 6)], fill=iron[2])
    d.line([(0, 13), (31, 13)], fill=iron[1])
    d.arc([10, 6, 21, 14], 180, 360, fill=iron[2])
    d.point((15, 8), fill=gilt)
    d.point((16, 8), fill=gilt)
    return im


def wall_run(sheet, ramp, gate):
    left = drop_sand(town_tile(sheet, 12, 29))
    mid = town_tile(sheet, 18, 29)
    right = drop_sand(town_tile(sheet, 21, 29))
    if ramp:
        for t in (left, mid, right):
            to_ramp(t, ramp)
    post = gatepost(ramp or [(70, 52, 42), (104, 80, 60), (140, 110, 80), (176, 140, 100), (204, 170, 120), (226, 196, 146), (240, 218, 172)])
    # a side run: the wall's capstones turned to run north-south, with its face on the right
    side = mid.crop((0, 1, 16, 9)).rotate(90, expand=True)
    vert = Image.new("RGBA", (16, 16))
    vert.paste(side, (4, 0))
    vp = vert.load()
    for y in range(16):
        for x in (12, 13):
            if vp[x - 1, y][3]:
                vp[x, y] = shade(vp[x - 1, y], 0.7) + (255,)
    # [left][mid][mid][post][gate gate][post][mid][right], the posts a tile taller
    run = Image.new("RGBA", (16 * 9, 32))
    run.alpha_composite(left, (0, 16))
    run.alpha_composite(mid, (16, 16))
    run.alpha_composite(mid, (32, 16))
    run.alpha_composite(post, (48, 0))
    run.alpha_composite(gate, (64, 15))
    run.alpha_composite(post, (96, 0))
    run.alpha_composite(mid, (112, 16))
    run.alpha_composite(right, (128, 16))
    return run, {"wall_left": left, "wall_mid": mid, "wall_right": right, "wall_side": vert, "gatepost": post, "gate": gate}


def walls(sheet):
    out = {}
    for wid, ramp, gate in (("W1", GREY_STONE, picket_gate()), ("W2", None, iron_gate())):
        run, parts = wall_run(sheet, ramp, gate)
        out[wid] = save(run, f"{wid}_wall_run")
        for k, im in parts.items():
            save(im, f"{wid}_{k}")
    return out


# ---- the easels (F1-F2, H1-H2) and the return frame (R1-R2): from our Easel item's sprite --------

EASEL_WOOD = [(62, 39, 36), (90, 51, 34), (125, 98, 58), (162, 133, 59), (190, 159, 52)]   # easel.py O D M L H


def easel_base():
    return Image.open(os.path.join(ASSETS, "bigcraftables.png")).convert("RGBA").crop((0, 0, 16, 32))


def rewood(im, ramp, dabs=None):
    px = im.load()
    m = {c: ramp[i] for i, c in enumerate(EASEL_WOOD)}
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in m:
                px[x, y] = m[(r, g, b)] + (a,)
            elif a and dabs and y == 18:                       # the paint dabs on the tray
                px[x, y] = dabs + (a,)
    return im


def empty_easel(ramp):
    """The easel without its canvas: the clamp, a mast down to the tray, the legs rising behind."""
    im = easel_base()
    px = im.load()
    for y in range(3, 18):
        for x in range(0, 16):
            px[x, y] = (0, 0, 0, 0)
    O, D, M, L, H = EASEL_WOOD
    for y in range(3, 18):
        px[6, y] = O + (255,)
        px[7, y] = L + (255,)
        px[8, y] = M + (255,)
        px[9, y] = O + (255,)
    for y in range(8, 18):                                     # the front legs, going up behind the tray
        t = (y - 8) / 9
        xl = round(5 - 2 * t)
        xr = round(10 + 2 * t)
        px[xl, y] = D + (255,)
        px[xr, y] = D + (255,)
    for x in range(4, 12):                                     # a crossbar for the canvas to rest on
        px[x, 16] = O + (255,)
        px[x, 15] = M + (255,)
    return rewood(im, ramp)


def fade(im, ramp):
    """The old easel's paint dabs, long dried and dulled toward its wood."""
    px = im.load()
    for x in range(16):
        r, g, b, a = px[x, 18]
        if a and (r, g, b) not in ramp:
            px[x, 18] = tuple(int(0.4 * c + 0.6 * w) for c, w in zip((r, g, b), ramp[3])) + (a,)
    return im


def with_canvas(im, kind):
    im = im.copy()
    d = ImageDraw.Draw(im)
    if kind == "painting":                 # a finished canvas, glowing: a little golden landscape, anyone's
        d.rectangle([2, 3, 13, 15], fill=(244, 236, 214))
        d.rectangle([3, 4, 12, 14], fill=(246, 214, 140))
        d.point([(10, 5), (11, 5), (10, 6), (11, 6)], fill=(255, 246, 204))
        d.polygon([(3, 11), (6, 8), (9, 10), (12, 8), (12, 14), (3, 14)], fill=(204, 120, 48))
        d.line([(4, 12), (7, 12)], fill=(110, 150, 170))
        d.line([(5, 7), (5, 11)], fill=(92, 52, 30))
        d.point([(4, 7), (5, 6), (6, 7)], fill=(176, 72, 34))
        d.rectangle([2, 3, 13, 15], outline=(150, 112, 70))
    else:                                  # the same canvas, covered with a draped cloth
        cloth = [(150, 132, 108), (196, 178, 148), (226, 212, 184), (244, 234, 212)]
        d.rectangle([2, 2, 13, 16], fill=cloth[2])
        for x in range(2, 14):
            d.point((x, 2), fill=cloth[3])
        for x in (5, 9, 12):                               # folds
            d.line([(x, 4), (x, 16)], fill=cloth[1])
        d.line([(3, 5), (3, 16)], fill=cloth[3])
        for x in range(2, 14, 2):
            d.point((x, 17), fill=cloth[1])                # the hem, uneven
        d.rectangle([2, 2, 13, 17], outline=cloth[0])
    return im


def hidden_easel(ramp, gilt):
    """The hidden easel: a working easel holding a blank canvas, waiting for the view."""
    im = rewood(easel_base(), ramp, dabs=(200, 150, 60))
    d = ImageDraw.Draw(im)
    d.rectangle([2, 4, 13, 16], fill=(244, 236, 214))
    d.line([(13, 5), (13, 16)], fill=(214, 200, 170))
    d.line([(2, 16), (13, 16)], fill=(214, 200, 170))
    if gilt:
        for x, y in ((1, 3), (14, 3), (1, 17), (14, 17)):
            d.point((x, y), fill=(236, 196, 84))
        d.point([(7, 0), (8, 0)], fill=(236, 196, 84))
    else:                                                  # a few leaves caught on it (it's been out here a while)
        for (x, y), c in (((3, 19), (214, 120, 40)), ((12, 23), (176, 72, 34)), ((14, 28), (214, 120, 40))):
            d.point((x, y), fill=c)
    return im


OLD_GREY = [(46, 42, 40), (78, 72, 66), (112, 104, 94), (146, 138, 124), (178, 170, 154)]        # weathered silver wood
FOUNDERS_WOOD = [(78, 44, 22), (122, 74, 36), (164, 110, 52), (198, 146, 72), (224, 180, 102)]  # the Founders' Frame's honey
FALL_WOOD = [(58, 34, 26), (96, 56, 32), (138, 88, 44), (178, 122, 56), (210, 160, 74)]


def easels():
    out = {}
    for fid, ramp in (("F1", OLD_GREY), ("F2", FOUNDERS_WOOD)):
        e = empty_easel(ramp)
        fade(e, ramp)
        if fid == "F2":                                    # carved notches down the mast
            px = e.load()
            for y in range(4, 16, 3):
                px[7, y] = ramp[1] + (255,)
        out[f"{fid}_empty"] = save(e, f"{fid}_founders_empty")
        out[f"{fid}_painting"] = save(with_canvas(e, "painting"), f"{fid}_founders_painting")
        out[f"{fid}_covered"] = save(with_canvas(e, "covered"), f"{fid}_founders_covered")
    out["H1"] = save(hidden_easel(FALL_WOOD, False), "H1_hidden_easel")
    out["H2"] = save(hidden_easel(EASEL_WOOD, True), "H2_hidden_easel")
    return out


def sketch_gate(d, ink, x0=3, y0=12):
    """Faint as a sketch: a low stone wall and a gate (what's through the frame)."""
    d.line([(x0, y0 + 7), (x0 + 9, y0 + 7)], fill=ink)
    d.line([(x0, y0 + 5), (x0 + 3, y0 + 5)], fill=ink)
    d.line([(x0 + 6, y0 + 5), (x0 + 9, y0 + 5)], fill=ink)
    d.line([(x0 + 3, y0 + 2), (x0 + 3, y0 + 7)], fill=ink)
    d.line([(x0 + 6, y0 + 2), (x0 + 6, y0 + 7)], fill=ink)
    for x in (x0 + 4, x0 + 5):
        d.line([(x, y0 + 4), (x, y0 + 7)], fill=ink)
    d.point([(x0 + 1, y0 + 6), (x0 + 8, y0 + 6)], fill=ink)


def return_frames():
    out = {}
    # R1: a gilt frame on a little stand
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    d.rectangle([1, 2, 14, 25], fill=(240, 228, 204))
    sketch_gate(d, (176, 150, 120), 3, 9)
    d.line([(3, 6), (12, 6)], fill=(226, 212, 186))
    d.rectangle([1, 2, 14, 25], outline=(214, 170, 64))
    d.rectangle([0, 1, 15, 26], outline=(150, 102, 30))
    for x, y in ((0, 1), (15, 1), (0, 26), (15, 26)):
        d.point((x, y), fill=(236, 196, 84))
    d.point([(7, 1), (8, 1)], fill=(244, 220, 120))
    d.line([(4, 27), (3, 31)], fill=(122, 74, 36), width=2)
    d.line([(11, 27), (12, 31)], fill=(122, 74, 36), width=2)
    out["R1"] = save(im, "R1_return_frame")
    # R2: a painted wooden frame standing in the grass, its wood in loose gold and umber strokes
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    d.rectangle([2, 6, 13, 29], fill=(236, 222, 190))
    sketch_gate(d, (150, 116, 80), 3, 16)
    for x in range(2, 14):
        d.point((x, 7 + (x % 3 == 0)), fill=(250, 236, 206))
    px = im.load()
    strokes = [(122, 74, 36), (178, 122, 56), (224, 180, 102), (198, 146, 72)]
    for y in range(4, 32):
        for x in range(0, 16):
            on = (x <= 1 or x >= 14 or y <= 5 or y >= 30) and 4 <= y <= 31
            if on:
                px[x, y] = strokes[(x * 2 + y + (y // 3)) % 4] + (255,)
    outline(im)
    tufts(im, 31, 0, 15, 3)
    out["R2"] = save(im, "R2_return_frame")
    return out


# ---- the Vale panorama (V1-V2) ---------------------------------------------------------------

PANORAMA = (22 * 16, 7 * 16)


def render_vale_fall():
    out = os.path.join(tempfile.gettempdir(), "vale_guide_fall.png")
    if not os.path.exists(out):
        subprocess.run([sys.executable, os.path.join(os.path.dirname(HERE), "render_tmx.py"), VALE_TMX, out,
                        "--sheets", os.path.join(EXPORTS, "sheets_fall"), "--scale", "1"], check=True, capture_output=True)
    return Image.open(out).convert("RGBA")


def lip(art):
    pw, ph = art.size
    d = ImageDraw.Draw(art)
    d.rectangle([0, ph - 6, pw, ph], fill=(214, 120, 40, 255))
    d.line([(0, ph - 6), (pw, ph - 6)], fill=(110, 52, 26, 255), width=2)
    for x in range(0, pw, 3):
        d.point((x, ph - 7 - (x * 7 % 3 == 0)), fill=(232, 150, 52, 255))
    return art


def panoramas():
    pw, ph = PANORAMA
    vale = render_vale_fall().crop((8 * 16, 12 * 16, 88 * 16, 44 * 16))
    # V1: the village shrunk, warmed and hazed with distance (the placeholder's way, tidied)
    pic = vale.resize((pw, ph - 6), Image.BOX)
    pic = ImageEnhance.Color(pic).enhance(0.8)
    pic = Image.blend(pic, Image.new("RGBA", pic.size, (255, 200, 110, 255)), 0.28)
    haze = Image.new("RGBA", pic.size)
    hd = ImageDraw.Draw(haze)
    for y in range(pic.height):
        hd.line([(0, y), (pw, y)], fill=(255, 236, 190, int(150 * (1 - y / pic.height) ** 1.6)))
    pic.alpha_composite(haze)
    v1 = Image.new("RGBA", (pw, ph))
    v1.paste(pic, (0, 0))
    out = {"V1": save(lip(v1), "V1_panorama")}
    # V2: the same view as a painting: fewer colours in blobs (brush-sized), a gold glaze, a soft sky band
    pic = vale.resize((pw, ph - 6), Image.BOX).convert("RGB")
    pic = pic.filter(ImageFilter.ModeFilter(3))
    pic = pic.quantize(28, method=Image.MEDIANCUT).convert("RGBA")
    pic = Image.blend(pic, Image.new("RGBA", pic.size, (255, 196, 96, 255)), 0.32)
    sky = Image.new("RGBA", pic.size)
    sd = ImageDraw.Draw(sky)
    for y in range(18):
        sd.line([(0, y), (pw, y)], fill=(255, 226, 160, int(200 * (1 - y / 18))))
    pic.alpha_composite(sky)
    pp = pic.load()
    for y in range(pic.height):                            # horizontal brush-drags: every few pixels, smear one along
        for x in range(1, pw):
            if (x * 7 + y * 13) % 11 == 0:
                pp[x, y] = pp[x - 1, y]
    v2 = Image.new("RGBA", (pw, ph))
    v2.paste(pic, (0, 0))
    out["V2"] = save(lip(v2), "V2_panorama")
    return out


# ---- the guide icon (G1-G2): a thin, paint-spotted book ----------------------------------------

GUIDE = [
    "................",
    "................",
    "...ooooooooooo..",
    "..oSSccccccccCo.",
    "..oSScCCCCCrCCo.",
    "..oSScCyCCCCCCo.",
    "..oSScCCCCCCCdo.",
    "..oSSCCLLLLCCdo.",
    "..oSSCCCCCCCbdo.",
    "..oSSCrCCCCCCdo.",
    "..oSSCCCCCyCCdo.",
    "..oSSCCCCCCCCdo.",
    "..oSSdddddddddo.",
    "..oppppppppppppo",
    "...oooooooooooo.",
    "................",
]


def guide_icon(cover, extra=None):
    m = {"o": outline_shade(cover[1]), "S": cover[0], "C": cover[1], "c": cover[2], "d": shade(cover[1], 0.78),
         "L": (240, 228, 196), "p": (244, 236, 214), "r": (205, 66, 60), "y": (236, 196, 64), "b": (72, 108, 176)}
    im = Image.new("RGBA", (16, 16))
    px = im.load()
    for y, row in enumerate(GUIDE):
        for x, ch in enumerate(row):
            if ch != ".":
                px[x, y] = m[ch] + (255,)
    if extra == "ribbon":                                   # a ribbon bookmark hanging out at the foot
        for x, y in ((11, 14), (11, 15), (12, 15)):
            px[x, y] = (176, 40, 44, 255)
        for x, y in ((7, 4), (13, 11)):                     # and more spatter
            px[x, y] = (88, 158, 58, 255)
    return im


def guide_icons():
    sage = [(70, 96, 70), (104, 138, 96), (140, 170, 124)]
    leather = [(84, 50, 30), (128, 80, 44), (164, 112, 66)]
    return {"G1": save(guide_icon(sage), "G1_guide_icon"), "G2": save(guide_icon(leather, "ribbon"), "G2_guide_icon")}


# ---- the painted fish (P1-P2): vale_fish_icons' drawing, its scales laid on in brushstrokes ------

def brush_pattern(cv, x, y, t, v, pal):
    # strokes along the body (parallel to the spine, where x + y is constant), dashed, in the stroke colours
    k = (x + y) % 3
    seg = ((x - y) // 3) % 2
    if 0.08 < t < 0.92 and k == 0 and seg == 0:
        cv.set(x, y, pal["stroke"][0])
    elif 0.1 < t < 0.9 and k == 1 and seg == 1 and v > -0.5:
        cv.set(x, y, pal["stroke"][1])


fishes.PALETTES["painted_gold"] = dict(body=fishes.hexes("#4a2a12", "#a8641e", "#e0a23a", "#f4d27a", "#fff2c8"),
                                       fin=fishes.hexes("#c2502a", "#e8843c"), stroke=fishes.hexes("#c2502a", "#fff2c8"))
fishes.PALETTES["painted_palette"] = dict(body=fishes.hexes("#2a2438", "#3e7a8c", "#e0a23a", "#f2c6a8", "#fff2c8"),
                                          fin=fishes.hexes("#b8405a", "#e07a8c"), stroke=fishes.hexes("#b8405a", "#7ab8a0"))
PAINTED = {
    "spine": [(13.2, 2.8), (10.4, 5.6), (7.4, 8.6), (4.6, 11.2)],
    "width": fishes.taper(3.4, 7.4, 2.6, 0.38),
    "bright_belly": True,
    "pattern": brush_pattern,
    "fins": [
        {"poly": [(5.2, 10.4), (2.6, 9.2), (0.6, 10.8), (1.6, 13.0), (0.8, 15.2), (3.0, 14.8), (5.6, 12.2)], "shade": 1, "rays": "+"},
        {"poly": [(6.0, 6.6), (7.4, 3.8), (9.6, 3.6), (9.8, 5.2)], "shade": 0},
        {"poly": [(9.0, 10.8), (10.2, 13.2), (11.4, 10.6)], "shade": 1},
    ],
    "eye": (12.4, 3.2), "eye_ring": (1, 0),
}


def painted_fish():
    out = {}
    for pid, pal in (("P1", "painted_gold"), ("P2", "painted_palette")):
        f = dict(PAINTED, palette=pal)
        out[pid] = save(fishes.draw_fish(f, 16, 16, lambda p: p), f"{pid}_painted_fish")
    return out


# ---- the Founders' Frame (FF1-FF3), frame_styles.py's way ----------------------------------------

def leaves(tile, dark):
    """FF2: a fall leaf in each corner (the Grove is always fall), a gold leaf over the top."""
    px = tile.load()
    w, h = tile.size
    for (cx, cy), c in (((1, 1), (214, 120, 40)), ((w - 2, 1), (176, 72, 34)), ((1, h - 2), (176, 72, 34)), ((w - 2, h - 2), (214, 120, 40))):
        if px[cx, cy][3]:
            px[cx, cy] = c + (255,)
    cx = w // 2
    for dx, c in ((0, (236, 196, 84)), (-1, (176, 128, 40))):
        if px[cx + dx, 0][3]:
            px[cx + dx, 0] = c + (255,)
    return tile


def brushed(tile, dark):
    """FF3: the wood laid on in loose strokes, like the echoes' brushwork: every few border pixels a
    darker or lighter dab, running along the frame."""
    px = tile.load()
    w, h = tile.size
    inside = px[w // 2, h // 2][:3]
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if not a or (r, g, b) == inside:
                continue
            along = x if min(y, h - 1 - y) <= min(x, w - 1 - x) else y
            k = (along * 2 + (x + y) // 5) % 5
            if k == 0:
                px[x, y] = shade((r, g, b), 0.78) + (a,)
            elif k == 2:
                px[x, y] = shade((r, g, b), 1.12) + (a,)
    return tile


def founders_frames():
    furniture = Image.open(os.path.join(ASSETS, "furniture.png")).convert("RGBA")
    items = Image.open(os.path.join(ASSETS, "items.png")).convert("RGBA")
    per_row = items.width // 16
    icon_src = items.crop((8 % per_row * 16, 8 // per_row * 16, 8 % per_row * 16 + 16, 8 // per_row * 16 + 16))
    plain = furniture.crop((0, 0, 32, 32))
    founders = dict((n, (p, k)) for n, p, k in FRAME_STYLES)["founders"]
    grove = [(72, 30, 14), (128, 62, 24), (176, 98, 34), (214, 138, 48), (236, 176, 76), (250, 214, 130)]
    sketch = [(96, 62, 36), (140, 98, 58), (182, 140, 92), (212, 176, 128), (232, 206, 162), (246, 230, 196)]
    out = {}
    for fid, palette, how in (("FF1", founders[0], lambda t, d: frame_touch(t, "carved", d)),
                              ("FF2", grove, leaves),
                              ("FF3", sketch, brushed)):
        tile = how(frame_recolour(plain.copy(), palette), palette[0])
        icon = how(frame_recolour(icon_src.copy(), palette, keep_inside=False), palette[0])
        both = Image.new("RGBA", (52, 32))
        both.alpha_composite(tile, (0, 0))
        both.alpha_composite(icon, (36, 16))
        out[fid] = save(both, f"{fid}_founders_frame")
    return out


# ---- the contact sheet ---------------------------------------------------------------------------

S = 3


def big(im):
    return im.resize((im.width * S, im.height * S), Image.NEAREST)


def sheet(rows, tomb, easel):
    font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 15)
    small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 12)
    pad = 14
    ref = [("vanilla", tomb), ("easel", easel)]
    built = []
    for title, ground, groups in rows:
        items = ref + [(gid, ims) for gid, ims in groups]
        width = pad
        height = 0
        layout = []
        for label, ims in items:
            ims = ims if isinstance(ims, list) else [ims]
            gw = sum(big(i).width for i in ims) + 4 * (len(ims) - 1)
            gh = max(big(i).height for i in ims)
            layout.append((label, ims, width, gw, gh))
            width += max(gw, 60) + pad
            height = max(height, gh)
        built.append((title, ground, layout, width, height + 52))
    W = max(b[3] for b in built)
    H = sum(b[4] for b in built)
    img = Image.new("RGBA", (W, H), (40, 36, 44, 255))
    d = ImageDraw.Draw(img)
    y = 0
    for title, ground, layout, width, h in built:
        d.rectangle([0, y, W, y + h - 1], fill=ground)
        d.line([(0, y), (W, y)], fill=(30, 26, 30))
        d.text((pad, y + 4), title, fill=(255, 255, 255), font=small, stroke_width=2, stroke_fill=(30, 26, 30))
        base = y + h - 24
        for label, ims, x, gw, gh in layout:
            cx = x
            for im in ims:
                b = big(im)
                img.alpha_composite(b, (cx, base - b.height))
                cx += b.width + 4
            is_ref = label in ("vanilla", "easel")
            d.text((x, base + 4), label, fill=(230, 230, 230) if is_ref else (255, 236, 120),
                   font=small if is_ref else font, stroke_width=2, stroke_fill=(30, 26, 30))
        y += h
    img.save(os.path.join(OUT, "candidates.png"))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    town = Image.open(os.path.join(EXPORTS, "spring_town.png")).convert("RGBA")
    tomb = town_tile(town, 0, 9, 1, 2)
    easel = easel_base()
    hs = headstones()
    bram = bram_stones()
    clay = clay_markers()
    un = urns()
    wl = walls(town)
    ez = easels()
    rf = return_frames()
    pan = panoramas()
    gi = guide_icons()
    pf = painted_fish()
    ff = founders_frames()
    echoes = {}
    for n, (who, look) in enumerate((w, l) for w in ("agnes", "hollis", "mabel", "bram") for l in ("ink", "oil")):
        echoes[f"E{n + 1}"] = Image.open(os.path.join(PL, f"echo_{who}_{look}.png")).convert("RGBA")
        save(echoes[f"E{n + 1}"], f"E{n + 1}_echo_{who}_{look}")
    grass, grove, night = GRASS, (176, 112, 52, 255), (52, 46, 58, 255)
    rows = [
        ("Carved headstones (PixelLab base, Bram's carving drawn on): each id shows Agnes / Hollis / Mabel", grass,
         [(b, [hs[f"{b}_{w}"] for w in ("agnes", "hollis", "mabel")]) for b in ("A1", "A2", "A3")]),
        ("Bram's plain stone (B), and the clay markers (C): Ada / Hester / Amos", grass,
         [("B1", bram["B1"]), ("B2", bram["B2"])] +
         [(c, [clay[f"{c}_{w}"] for w in ("ada", "hester", "amos")]) for c in ("C1", "C2")]),
        ("Urns: Agnes / Hollis / Mabel / Bram / Ada ('not yet' lid) / Hester / Amos", grass,
         [(u, [un[f"{u}_{w}"] for w in URN_ORDER]) for u in ("U1", "U2")]),
        ("Low wall and gate (the game's bridge parapet, recoloured): run with posts and gate, plus the side piece", grass,
         [(w_, [wl[w_], Image.open(os.path.join(OUT, f"{w_}_wall_side.png"))]) for w_ in ("W1", "W2")]),
        ("Painted echoes (PixelLab): E1/E2 Agnes, E3/E4 Hollis, E5/E6 Mabel, E7/E8 Bram; odd = ink sketch, even = oil sketch", grove,
         [(e, echoes[e]) for e in echoes]),
        ("Founders' easel (empty / finished canvas / covered), hidden easel, return frame", grove,
         [(f, [ez[f"{f}_empty"], ez[f"{f}_painting"], ez[f"{f}_covered"]]) for f in ("F1", "F2")] +
         [("H1", ez["H1"]), ("H2", ez["H2"]), ("R1", rf["R1"]), ("R2", rf["R2"])]),
        ("Vale panorama (22 x 7 tiles)", grove, [("V1", pan["V1"])]),
        ("", grove, [("V2", pan["V2"])]),
        ("Guide icon (G), painted fish (P), Founders' Frame (FF: a 2x2 framed painting and the item icon)", night,
         [("G1", gi["G1"]), ("G2", gi["G2"]), ("P1", pf["P1"]), ("P2", pf["P2"]),
          ("FF1", ff["FF1"]), ("FF2", ff["FF2"]), ("FF3", ff["FF3"])]),
    ]
    sheet(rows, tomb, easel)
    print("wrote", OUT)


if __name__ == "__main__":
    main()

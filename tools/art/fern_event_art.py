"""Placeholder art for Fern Aster's heart events (1.4; docs/vale_events.md, Fern's 2- to 10-heart
events; built by tools/build_fern_events.py). Drawn by script, no PixelLab: every piece here is a
PLACEHOLDER until the real art pass. Outlines are a darker shade of the colour they edge (selout),
never black.

Writes into "[CP] Gilded Vale (dev)/assets/fern/":
  event_props.png   the scene props, loaded as Mods/blinxxy.MuseArt/FernEventProps:
                      (0, 0)   32x32  the Heritage Society's red mower, with its label (2 hearts)
                      (32, 0)  32x32  the bumblebee nest, two frames side by side, placed in the
                                      32x32 as it is under the ring (2 hearts)
                      (96, 0)  32x32  the ring of stakes and string, without the nest (2 hearts)
                      (0, 32)  32x48  the night vine as the room has it (its own tiles), then
                      (32, 32)        half open, then
                      (64, 32)        in flower (8 hearts)
  stakes.png        32x32: the ring of stakes round the nest, for the meadow's map patch
  night_vine.png    32x48: the vine in flower, for the seedhouse's map patch (its 2x3 tiles)
  vine_cutting.png  16x32: the vine cutting, a pot plant (furniture, 1x2)
  path_<season>.png the sculpture garden's one path, swept flagstones over the gravel, as tiles
                    (tools/build_fern_events.py PATH_AREA; the tile list is path_tiles.json)

The night vine and the path are made from the pack's own art: the vine's tiles from
assets/ValeFernInterior.png with open flowers added, the path from the garden's Back2 tiles in
assets/<season>_GildedValeBuildings.png with the gravel's pixels re-laid as flagstones.

Usage: python tools/art/fern_event_art.py   (from the repository root)
"""
import json, os, random, re
from PIL import Image

PACK = "[CP] Gilded Vale (dev)"
ASSETS = os.path.join(PACK, "assets")
OUT = os.path.join(ASSETS, "fern")
CLEAR = (0, 0, 0, 0)
# the garden's one path: the east-west walk through the parterre, x 7-27, rows 41-44
# (tools/build_fern_events.py places the patch over this area)
PATH_AREA = (7, 41, 21, 4)
VINE_TILES = (12, 0, 2, 3)          # ValeFernInterior.png: the vine and its trellis, 2x3 tiles


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (c[3] if len(c) > 3 else 255,)


def selout(im, f=0.62):
    """Every opaque pixel touching transparency on a side becomes a darker shade of itself."""
    px = im.load()
    w, h = im.size
    edge = []
    for y in range(h):
        for x in range(w):
            if px[x, y][3] < 200:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < w and 0 <= ny < h) or px[nx, ny][3] == 0:
                    edge.append((x, y))
                    break
    for x, y in edge:
        px[x, y] = shade(px[x, y], f)
    return im


def rect(px, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = c


# ---------------------------------------------------------------- the mower

def mower():
    im = Image.new("RGBA", (32, 32), CLEAR)
    px = im.load()
    red, red_hi, red_lo = (206, 44, 38, 255), (236, 92, 76, 255), (150, 30, 32, 255)
    steel, steel_lo = (176, 178, 186, 255), (120, 122, 134, 255)
    tyre, hub = (74, 66, 70, 255), (150, 146, 150, 255)
    # the handle: two steel bars rising back to a grip
    for i in range(10):
        px[9 + i // 3, 4 + i] = steel_lo
        px[22 - i // 3, 4 + i] = steel_lo
    rect(px, 8, 3, 23, 4, steel)
    rect(px, 8, 3, 9, 4, (60, 60, 64, 255))       # the grips, black rubber
    rect(px, 22, 3, 23, 4, (60, 60, 64, 255))
    # the deck: a rounded red box
    rect(px, 6, 14, 25, 24, red)
    rect(px, 7, 13, 24, 13, red)
    rect(px, 7, 15, 24, 16, red_hi)                # the lit top edge
    rect(px, 6, 23, 25, 24, red_lo)
    # the engine cap on top
    rect(px, 12, 11, 19, 15, steel)
    rect(px, 12, 11, 19, 11, (214, 216, 222, 255))
    rect(px, 15, 9, 16, 10, steel_lo)
    # the label: a white card tied to the deck
    rect(px, 18, 18, 24, 21, (240, 236, 224, 255))
    rect(px, 19, 19, 23, 19, (150, 140, 120, 255))
    rect(px, 19, 20, 22, 20, (150, 140, 120, 255))
    # the wheels
    for wx in (5, 22):
        rect(px, wx, 22, wx + 4, 27, tyre)
        rect(px, wx + 1, 23, wx + 3, 26, hub)
        px[wx + 2, 24] = tyre
    return selout(im)


# ---------------------------------------------------------------- the nest and the stakes

def nest(frame):
    im = Image.new("RGBA", (16, 16), CLEAR)
    px = im.load()
    rim, soil, hole = (112, 88, 52, 255), (82, 60, 36, 255), (52, 36, 24, 255)
    for y in range(16):
        for x in range(16):
            d = ((x - 7.5) / 6.5) ** 2 + ((y - 9) / 4.5) ** 2
            if d <= 1:
                px[x, y] = rim if d > 0.62 else soil if d > 0.25 else hole
    # dry grass in the rim
    for x, y in ((3, 7), (5, 5), (10, 5), (12, 8), (4, 11), (11, 12)):
        px[x, y] = (168, 150, 88, 255)
    im = selout(im, 0.7)
    px = im.load()
    bees = [(5, 3), (11, 2), (8, 7), (13, 10)] if frame == 0 else [(4, 2), (10, 4), (7, 9), (14, 6)]
    for x, y in bees:
        px[x, y] = (240, 200, 60, 255)
        px[x + 1, y] = (96, 70, 30, 255)
        if y > 0:
            px[x, y - 1] = (226, 236, 240, 200)     # a wing
    return im


STAKES = [(16 + 12 * a, 17 + 9 * b) for a, b in
          ((-1, 0), (-0.71, -0.71), (0, -1), (0.71, -0.71), (1, 0), (0.71, 0.71), (0, 1), (-0.71, 0.71))]


def stakes():
    """Eight garden stakes in a ring, string tied tip to tip. The string first (behind the
    stakes), then the stakes back to front."""
    im = Image.new("RGBA", (32, 32), CLEAR)
    px = im.load()
    string, string_lo = (238, 230, 206, 255), (200, 190, 162, 255)
    tips = [(round(x), round(y) - 6) for x, y in STAKES]
    for i, (x0, y0) in enumerate(tips):
        x1, y1 = tips[(i + 1) % len(tips)]
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for k in range(n + 1):
            t = k / n
            sag = int(round(1.5 * (1 - (2 * t - 1) ** 2)))       # a little slack between stakes
            x, y = round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t) + sag
            if 0 <= x < 32 and 0 <= y < 32:
                px[x, y] = string if y0 + y1 < 2 * 17 - 12 else string_lo
    wood, wood_hi, wood_lo = (196, 150, 96, 255), (226, 186, 128, 255), (140, 100, 60, 255)
    posts = Image.new("RGBA", (32, 32), CLEAR)
    px = posts.load()
    for x, y in sorted(STAKES, key=lambda p: p[1]):
        x, y = round(x), round(y)
        for yy in range(y - 7, y + 1):
            if 0 <= yy < 32:
                px[x, yy] = wood_hi if yy == y - 7 else wood
                if x + 1 < 32:
                    px[x + 1, yy] = wood_lo
    im.alpha_composite(selout(posts, 0.7))       # (the string is too thin to outline)
    return im


# ---------------------------------------------------------------- the night vine

BLOOM_PETAL, BLOOM_LO, BLOOM_EYE = (252, 248, 226, 255), (214, 206, 170, 255), (250, 222, 120, 255)
BLOOMS = [(6, 6), (17, 4), (25, 9), (11, 13), (22, 17), (5, 20), (15, 23), (26, 26), (9, 30), (20, 33)]


def night_vine(blooms):
    """The vine's own tiles, with pale flowers open all over it and a faint glow round each."""
    sheet = Image.open(os.path.join(ASSETS, "ValeFernInterior.png")).convert("RGBA")
    x, y, w, h = VINE_TILES
    vine = sheet.crop((x * 16, y * 16, (x + w) * 16, (y + h) * 16))
    glow = Image.new("RGBA", vine.size, CLEAR)
    gp = glow.load()
    vp = vine.load()
    for bx, by in blooms:
        for yy in range(by - 3, by + 4):
            for xx in range(bx - 3, bx + 4):
                if 0 <= xx < vine.width and 0 <= yy < vine.height and vp[xx, yy][3] and (xx - bx) ** 2 + (yy - by) ** 2 <= 10:
                    gp[xx, yy] = (250, 240, 170, 70)
    vine.alpha_composite(glow)
    vp = vine.load()
    for bx, by in blooms:
        # five petals round a yellow eye; the lower petals shaded
        for dx, dy, c in ((0, -1, BLOOM_PETAL), (-1, 0, BLOOM_PETAL), (1, 0, BLOOM_PETAL),
                          (-1, 1, BLOOM_LO), (1, 1, BLOOM_LO), (0, -2, BLOOM_LO), (-2, 0, BLOOM_LO), (2, 0, BLOOM_LO)):
            vp[bx + dx, by + dy] = c
        vp[bx, by] = BLOOM_EYE
    return vine


# ---------------------------------------------------------------- the cutting

def vine_cutting():
    """A cutting of the night vine in a clay pot, climbing a cane, two flowers open: about the
    size of the game's own house plants."""
    im = Image.new("RGBA", (16, 32), CLEAR)
    px = im.load()
    pot, pot_hi, pot_lo, soil = (176, 98, 52, 255), (210, 134, 80, 255), (132, 70, 40, 255), (70, 40, 26, 255)
    rect(px, 2, 21, 13, 23, pot_hi)            # the rim
    rect(px, 3, 22, 12, 22, pot)
    rect(px, 3, 21, 12, 21, soil)
    for y in range(24, 31):                    # the body, narrowing to its foot
        inset = (y - 24) // 3
        rect(px, 3 + inset, y, 12 - inset, y, pot)
        px[3 + inset, y] = pot_hi
        rect(px, 11 - inset, y, 12 - inset, y, pot_lo)
    for y in range(3, 21):                     # the cane
        px[8, y] = (204, 170, 112, 255)
    leaf, leaf_hi, leaf_lo = (34, 120, 62, 255), (70, 158, 86, 255), (14, 76, 44, 255)
    for x, y in ((7, 20), (9, 19), (9, 18), (7, 17), (7, 16), (9, 15), (9, 14), (7, 13), (7, 12), (9, 11), (9, 10), (7, 9), (7, 8), (9, 7), (8, 6)):
        px[x, y] = leaf_lo
    for lx, ly, d in ((5, 18, -1), (11, 16, 1), (5, 13, -1), (11, 11, 1), (5, 9, -1), (11, 7, 1), (6, 5, -1)):
        for dx, dy, c in ((0, 0, leaf), (d, 0, leaf_hi), (0, 1, leaf_lo), (-d, 0, leaf), (d, -1, leaf_hi), (0, -1, leaf)):
            if 0 <= lx + dx < 16:
                px[lx + dx, ly + dy] = c
    for fx, fy in ((8, 3), (12, 12)):          # two flowers open
        for dx, dy, c in ((0, -1, BLOOM_PETAL), (-1, 0, BLOOM_PETAL), (1, 0, BLOOM_PETAL), (-1, 1, BLOOM_LO), (1, 1, BLOOM_LO)):
            if 0 <= fx + dx < 16:
                px[fx + dx, fy + dy] = c
        px[fx, fy] = BLOOM_EYE
    return selout(im, 0.66)


# ---------------------------------------------------------------- the one path

def gravel(c):
    """The parterre's gravel: warm orange (spring, summer) or brown (fall), with its yellow edge
    pixels; 2 where it lies in a shadow (the birdbath's), else 1, or 0 if it isn't gravel."""
    r, g, b, a = c
    if not a:
        return 0
    if r > 140 and r >= g - 5 and b < g - 30:
        return 1
    if 90 < r <= 140 and r > g + 20 and g > b + 15:
        return 2
    return 0


def flagstones(w, h, seed=7):
    """Swept flagstones in world space: rows of slabs of uneven width, staggered, each a slightly
    different stone, the joints a darker shade of the stone."""
    rnd = random.Random(seed)
    stone = [(176, 168, 150), (166, 160, 146), (184, 174, 156), (160, 152, 138)]
    slab = [[None] * w for _ in range(h)]
    y = 0
    while y < h:
        rh = rnd.choice((6, 7, 7, 8))
        x = -rnd.randint(0, 8)
        while x < w:
            sw = rnd.randint(7, 12)
            c = rnd.choice(stone)
            for yy in range(y, min(y + rh, h)):
                for xx in range(max(x, 0), min(x + sw, w)):
                    joint = yy == y + rh - 1 or xx == x + sw - 1
                    lit = yy == y or xx == x
                    slab[yy][xx] = shade(c, 0.74) if joint else shade(c, 1.08) if lit else c + (255,)
            x += sw
        y += rh
    return slab


PATH_LAYERS = ("Back2", "Buildings")      # the parterre's gravel is painted on both (Buildings: under the birdbath and the statue)
PATH_BAND = (41 * 16 + 7, 44 * 16 + 4)    # world pixel rows: the east-west walk; the side paths leave it here


def band_edge(X, top):
    """Where the swept stone gives way to a side path's gravel: a wavy line round PATH_BAND's
    edge (never a ruler-straight cut across a path; docs/terrain_painting.md, section 1)."""
    import math
    phase = 0.0 if top else 2.1
    wave = 2.4 * math.sin(X / 5.3 + phase) + 1.3 * math.sin(X / 2.2 + phase * 1.7)
    return (PATH_BAND[0] if top else PATH_BAND[1]) + int(round(wave))


def path_tiles():
    """The path's tiles in all four seasons: each tile of PATH_AREA (on Back2 or Buildings) with
    gravel in it within PATH_BAND (as the spring sheet has it), the gravel re-laid as flagstones,
    the path's edge a darker shade of the stone. The side paths stay gravel. Winter: the same
    flagstones, colder, swept clear of the snow."""
    tmx = open(os.path.join(ASSETS, "GildedVale.tmx"), encoding="utf-8").read()
    layers = {}
    for name in PATH_LAYERS:
        data = re.search(rf'name="{name}"[^>]*>\s*<data encoding="csv">(.*?)</data>', tmx, re.S).group(1)
        layers[name] = [[int(v) for v in r.strip().rstrip(",").split(",")] for r in data.strip().split("\n")]
    first = int(re.search(r'firstgid="(\d+)" name="z_blinxxy.MuseArt_GildedValeBuildings"', tmx).group(1))
    ax, ay, aw, ah = PATH_AREA
    sheets = {s: Image.open(os.path.join(ASSETS, f"{s}_GildedValeBuildings.png")).convert("RGBA")
              for s in ("spring", "summer", "fall", "winter")}

    def tile(sheet, gid):
        i = gid - first
        return sheet.crop(((i % 32) * 16, (i // 32) * 16, (i % 32) * 16 + 16, (i // 32) * 16 + 16))

    def ours(gid):
        return first <= gid < first + 10000

    # the masks: the gravel's pixels in spring within the band, each layer's own and all together
    W, H = aw * 16, ah * 16
    masks = {name: [[False] * W for _ in range(H)] for name in PATH_LAYERS}
    loose = [[False] * W for _ in range(H)]      # gravel anywhere in the area, in the band or not
    for name, rows in layers.items():
        for ty in range(ah):
            for tx in range(aw):
                gid = rows[ay + ty][ax + tx]
                if ours(gid):
                    t = tile(sheets["spring"], gid).load()
                    for y in range(16):
                        for x in range(16):
                            X, Y = tx * 16 + x, ty * 16 + y
                            wx, wy = ax * 16 + X, ay * 16 + Y
                            if gravel(t[x, y]) > 0:
                                loose[Y][X] = True
                                if band_edge(wx, True) <= wy < band_edge(wx, False):
                                    masks[name][Y][X] = True
    union = [[any(masks[n][y][x] for n in PATH_LAYERS) for x in range(W)] for y in range(H)]
    stones = flagstones(W, H)
    tiles = []           # (layer, x, y) of each tile in the area that changes, in sheet order
    out = {s: [] for s in sheets}
    for name, rows in layers.items():
        mask = masks[name]
        for ty in range(ah):
            for tx in range(aw):
                gid = rows[ay + ty][ax + tx]
                if not ours(gid):
                    continue
                cells = [(x, y) for y in range(16) for x in range(16) if mask[ty * 16 + y][tx * 16 + x]]
                if len(cells) < 4:
                    continue
                tiles.append((name, ax + tx, ay + ty))
                for season, sheet in sheets.items():
                    t = tile(sheet, gid)
                    px = t.load()
                    for x, y in cells:
                        X, Y = tx * 16 + x, ty * 16 + y
                        c = stones[Y][X]
                        if gravel(tile(sheets["spring"], gid).getpixel((x, y))) == 2:
                            c = shade(c, 0.72)
                        # the stone's edge is darker against grass, hedge or plinth; where a side
                        # path's gravel runs on, the stone just meets it
                        edge = any(not (0 <= X + dx < W and 0 <= Y + dy < H)
                                   or not (union[Y + dy][X + dx] or loose[Y + dy][X + dx])
                                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                        if season == "winter":
                            c = (int(c[0] * 0.92), int(c[1] * 0.96), min(255, int(c[2] * 1.06)), 255)
                            if edge:        # against snow: the snow's own blue shade, not a grey pencil line
                                c = (190, 206, 224, 255)
                        elif edge:
                            c = shade(c, 0.7)
                        px[x, y] = c
                    out[season].append(t)
    for season, ts in out.items():
        im = Image.new("RGBA", (16 * 8, 16 * ((len(ts) + 7) // 8)), CLEAR)
        for n, t in enumerate(ts):
            im.paste(t, ((n % 8) * 16, (n // 8) * 16))
        im.save(os.path.join(OUT, f"path_{season}.png"))
    with open(os.path.join(OUT, "path_tiles.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(tiles, f)
    return tiles


def main():
    os.makedirs(OUT, exist_ok=True)
    props = Image.new("RGBA", (128, 80), CLEAR)
    props.alpha_composite(mower(), (0, 0))
    for f in (0, 1):                               # the nest, placed as in the meadow's ring
        props.alpha_composite(nest(f), (32 + 32 * f + 8, 9))
    ring = stakes()
    props.alpha_composite(ring, (96, 0))
    closed = Image.open(os.path.join(ASSETS, "ValeFernInterior.png")).convert("RGBA").crop(
        (VINE_TILES[0] * 16, VINE_TILES[1] * 16, (VINE_TILES[0] + VINE_TILES[2]) * 16, (VINE_TILES[1] + VINE_TILES[3]) * 16))
    props.alpha_composite(closed, (0, 32))
    props.alpha_composite(night_vine(BLOOMS[::2]), (32, 32))
    vine = night_vine(BLOOMS)
    props.alpha_composite(vine, (64, 32))
    props.save(os.path.join(OUT, "event_props.png"))
    # the meadow's patch: the nest under the ring
    patch = Image.new("RGBA", (32, 32), CLEAR)
    patch.alpha_composite(nest(0), (8, 9))
    patch.alpha_composite(ring)
    patch.save(os.path.join(OUT, "stakes.png"))
    vine.save(os.path.join(OUT, "night_vine.png"))
    vine_cutting().save(os.path.join(OUT, "vine_cutting.png"))
    tiles = path_tiles()
    print("wrote", OUT, f"({len(tiles)} path tiles)")


if __name__ == "__main__":
    main()

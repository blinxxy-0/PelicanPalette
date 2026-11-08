"""Hand fixes to the painted features: their collision, and two bits of art.

vale_feature.walkable() guesses what's solid from the painting's colours, and it guessed
wrong in places (the user, 2026-11-08: "some objects like this fountain i can walk
entirely on top of with no barrier", and "some invisible barriers too"):
- walked through: the parterre's birdbath, its statues, its hedged flowerbeds and the
  top post of its west wall; the tea garden's hedges and its rose arch's posts; Fern's
  potting bench and the pot beside it;
- blocked by nothing: two empty grass tiles beside Fern's cold frames;
- Fern's compost heap is solid, and the winter repaint (vale_feature_winter.py) turned
  it into plain snow on snow, so in winter it was an invisible wall. WINTER_KEEP puts the
  heap back, snow on its top, its soil showing below.
- the tea garden's east hedge was half the west one's thickness and gave out among the
  flowers before the corner (the user, 2026-11-08: "why is the hedge so weak on the right
  side?"). MIRROR paints the west hedge's column, flipped, down the east side.

build_vale.py applies these as it lays each feature (apply_walk, keep_winter). Run this
file on its own to apply them to the committed map in place (its rebuild doesn't
reproduce the committed map exactly):
    python tools/vale_feature_fixes.py "[CP] Gilded Vale (dev)/assets"
"""
import os, re, sys
from PIL import Image

# feature: [(x0, y0, x1, y1) map tiles, inclusive]
SOLID = {
    "parterre": [
        (5, 39, 5, 39),         # the west wall's top post
        (6, 43, 6, 43),         # the west statue's plinth
        (8, 40, 11, 41), (8, 44, 11, 45),       # the west hedged beds
        (19, 40, 23, 41), (19, 44, 23, 45),     # the east hedged beds
        (15, 43, 16, 43),       # the birdbath's foot
        (15, 45, 16, 45),       # the south statue's plinth
        (25, 42, 25, 42),       # the east statue's plinth
    ],
    "tea": [
        (34, 10, 34, 18), (40, 10, 40, 18),     # the hedges down each side
        (35, 18, 36, 19), (38, 18, 40, 19),     # the hedges either side of the south gap
        (35, 11, 35, 11), (37, 11, 37, 11),     # the rose arch's posts, at their feet
    ],
    "nursery": [
        (22, 33, 24, 34),       # the potting bench
        (25, 34, 25, 34),       # the potted plant by it
        (26, 37, 27, 37),       # the compost heap's foot (its art runs 2 px into x 28)
    ],
}
WALK = {
    "nursery": [(25, 36, 25, 37)],      # bare grass by the east cold frame
}
# feature: [boxes] drawn over the farmer and walkable: the tops of tall things. Only a
# thing's foot takes up ground; its top is on Front, as the game's lamp posts are (the
# user, 2026-11-08: "the top half of that structure doesn't need to take up tile space",
# after a solid statue head closed the walk between the birdbath and the south statue).
# A Front tile sorts by its own row, so it covers whoever stands on it or just above it,
# and anyone in front of the foot draws over it.
FRONT = {
    "parterre": [
        (6, 41, 6, 42),         # the west statue
        (15, 41, 16, 42),       # the birdbath's bowl
        (15, 44, 16, 44),       # the south statue
        (25, 41, 25, 41),       # the east statue
    ],
    "tea": [(35, 10, 35, 10), (37, 10, 37, 10)],    # the rose arch's posts
    "nursery": [
        (26, 36, 27, 36),       # the compost heap's top: you walk behind it, between it and
                                # the wheelbarrow (the user, 2026-11-08)
        (27, 34, 28, 34),       # the wheelbarrow's handles
        (20, 36, 21, 36), (23, 36, 24, 36),     # the cold frames' raised lids
    ],
}
# feature: (x0, y0, x1, y1) map tiles whose winter art is the spring art, snowed on
WINTER_KEEP = {"nursery": (26, 36, 28, 37)}
# feature: [((x0, y0, x1, y1) map tiles, x of the column it's copied to, flipped)]
MIRROR = {"tea": [((34, 10, 34, 18), 40)]}


# FRONT tiles that stand on the gravel path: only the thing itself goes on Front, the
# path round it stays underfoot on Back2 (the user, 2026-11-08: walking south behind the
# east statue, "blue path texture gets picked up and you can weirdly walk behind it").
SPLIT = {
    "parterre": FRONT["parterre"],
    "nursery": [(20, 36, 21, 36), (23, 36, 24, 36)],
}


def sand(c):
    """The gravel paths in the spring art."""
    return c[3] and c[0] > 140 and c[0] - c[2] > 60 and c[1] > 80


def snow(c):
    return c[3] and min(c[:3]) > 175 and c[2] >= c[0] - 5


def ground_mask(spring, art, season):
    """{(x, y)} px of a tile that are path, not the thing: gravel in spring (summer and
    fall share its pixels); in winter only where spring had gravel and winter has snow,
    so a statue's snowcap and the winter art's own outline stay with the statue."""
    sp, ar = spring.load(), art.load()
    return {(x, y) for y in range(spring.height) for x in range(spring.width)
            if sand(sp[x, y]) and (season != "winter" or snow(ar[x, y]))}


def split_front(name, arts, left, top):
    """In a feature's season arts (px), lifts each FRONT box's thing out of the art: it
    returns [(x0, y0, {season: image})] to lay on Front, and leaves the path behind."""
    out = []
    split = set(tiles(SPLIT.get(name, ())))
    for x0, y0, x1, y1 in FRONT.get(name, ()):
        box = ((x0 - left) * 16, (y0 - top) * 16, (x1 - left + 1) * 16, (y1 - top + 1) * 16)
        spring = arts["spring"].crop(box)
        lifted = {}
        for season, im in arts.items():
            part = im.crop(box)
            keep = Image.new("RGBA", part.size)
            for tx, ty in tiles([(x0, y0, x1, y1)]):
                c = ((tx - x0) * 16, (ty - y0) * 16, (tx - x0) * 16 + 16, (ty - y0) * 16 + 16)
                g = ground_mask(spring.crop(c), part.crop(c), season) if (tx, ty) in split else set()
                pt, kt = part.crop(c), Image.new("RGBA", (16, 16))
                pp, kp = pt.load(), kt.load()
                for y in range(16):
                    for x in range(16):
                        if (x, y) not in g:
                            kp[x, y], pp[x, y] = pp[x, y], (0, 0, 0, 0)
                keep.paste(kt, c[:2])
                part.paste(pt, c[:2])
            im.paste(part, box[:2])
            lifted[season] = keep
        out.append((x0, y0, lifted))
    return out


def tiles(boxes):
    for x0, y0, x1, y1 in boxes:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                yield x, y


def apply_walk(name, walk, left, top):
    """walk: {(i, j) in the piece: walkable} from vale_feature.walkable(). FRONT tiles
    walk: their tops are lifted onto Front by split_front."""
    for value, table in ((False, SOLID), (True, WALK), (True, FRONT)):
        for x, y in tiles(table.get(name, ())):
            if (x - left, y - top) in walk:
                walk[(x - left, y - top)] = value


def mirror(name, im, left, top):
    """In a feature's piece image (px, any season), each MIRROR column copied across, flipped."""
    for (x0, y0, x1, y1), dx in MIRROR.get(name, ()):
        box = ((x0 - left) * 16, (y0 - top) * 16, (x1 - left + 1) * 16, (y1 - top + 1) * 16)
        im.paste(im.crop(box).transpose(Image.FLIP_LEFT_RIGHT), ((dx - left) * 16, box[1]))


def snowed(spring):
    """The spring art with snow lying on its top: each column's top few pixels white,
    the snow's edge a shade of the snow (never black), green gone to the soil's brown."""
    im = spring.copy()
    px = im.load()
    W, H = im.size
    LIGHT, MID, SHADE = (240, 246, 252, 255), (214, 226, 240, 255), (168, 186, 212, 255)
    solid = lambda x, y: 0 <= x < W and 0 <= y < H and spring.getpixel((x, y))[3] > 0
    for x in range(W):
        for y in range(H):
            r, g, b, a = px[x, y]
            if a and g > r + 8 and g > b + 8:       # sprouts and moss: winter has none
                px[x, y] = (max(60, r - 10), int(r * 0.8), int(b * 0.8), a)
    for x in range(W):
        ys = [y for y in range(H) if solid(x, y)]
        if not ys:
            continue
        top = ys[0]
        depth = 4 if solid(x - 1, top) or solid(x + 1, top) else 2
        for k, y in enumerate(range(top, min(top + depth, H))):
            if not solid(x, y):
                break
            edge = not solid(x - 1, y) or not solid(x + 1, y) or not solid(x, y - 1)
            px[x, y] = SHADE if k == depth - 1 else (MID if edge else LIGHT)
    return im


def keep_winter(name, spring, winter, left, top):
    """In a feature's piece images (px), the WINTER_KEEP box's winter art from spring."""
    if name not in WINTER_KEEP:
        return
    x0, y0, x1, y1 = WINTER_KEEP[name]
    box = ((x0 - left) * 16, (y0 - top) * 16, (x1 - left + 1) * 16, (y1 - top + 1) * 16)
    winter.paste(snowed(spring.crop(box)), box[:2])


# --- applying it to the committed map in place ------------------------------------------

FEATURE_FIRST = 20000


def span_in(text, name):
    return re.search(r'<layer id="\d+" name="' + name + r'"[^>]*>\s*<data encoding="csv">\n(.*?)\n\s*</data>', text, re.S)


def _patch(assets):
    path = os.path.join(assets, "GildedVale.tmx")
    text = open(path, encoding="utf-8").read()

    def span(name):
        return re.search(r'<layer id="\d+" name="' + name + r'"[^>]*>\s*<data encoding="csv">\n(.*?)\n\s*</data>', text, re.S)

    g = {n: [row.rstrip(",").split(",") for row in span(n).group(1).split("\n")] for n in ("Back2", "Buildings")}
    own = lambda v: FEATURE_FIRST <= int(v) < FEATURE_FIRST + 10000
    moved = []
    for table, src, dst in ((SOLID, "Back2", "Buildings"), (WALK, "Buildings", "Back2")):
        for name, boxes in table.items():
            for x, y in tiles(boxes):
                v = g[src][y][x]
                if own(v) and g[dst][y][x] == "0":
                    g[dst][y][x], g[src][y][x] = v, "0"
                    moved.append((name, x, y, dst))
                elif not own(g[dst][y][x]):
                    print("not moved", name, x, y, src, "->", dst, g["Back2"][y][x], g["Buildings"][y][x])
    g["Front"] = [row.rstrip(",").split(",") for row in span("Front").group(1).split(chr(10))]
    for name, boxes in FRONT.items():
        for x, y in tiles(boxes):
            src = next((k for k in ("Buildings", "Back2") if own(g[k][y][x])), None)
            if src and g["Front"][y][x] == "0":
                g["Front"][y][x], g[src][y][x] = g[src][y][x], "0"
                moved.append((name, x, y, "Front"))
            elif not own(g["Front"][y][x]):
                print("not moved to Front", name, x, y)
    for n in ("Back2", "Buildings", "Front"):
        m = span(n)
        text = text[:m.start(1)] + ",\n".join(",".join(r) for r in g[n]) + text[m.end(1):]
    open(path, "w", encoding="utf-8", newline="\n").write(text)
    for name, x, y, dst in moved:
        print(f"{name} {x},{y} -> {dst}")
    # the winter sheet: the kept tiles' winter art, from their spring art
    sheets = {s: Image.open(os.path.join(assets, s + "_GildedValeBuildings.png")).convert("RGBA") for s in ("spring", "winter")}
    cols = sheets["spring"].width // 16
    for name, (x0, y0, x1, y1) in WINTER_KEEP.items():
        region = Image.new("RGBA", ((x1 - x0 + 1) * 16, (y1 - y0 + 1) * 16))
        slots = {}
        for x, y in tiles([(x0, y0, x1, y1)]):
            v = next((int(g[n][y][x]) for n in ("Buildings", "Back2") if own(g[n][y][x])), None)
            if v is None:
                continue
            i = v - FEATURE_FIRST
            sx, sy = (i % cols) * 16, (i // cols) * 16
            slots[(x, y)] = (sx, sy)
            region.paste(sheets["spring"].crop((sx, sy, sx + 16, sy + 16)), ((x - x0) * 16, (y - y0) * 16))
        snow = snowed(region)
        for (x, y), (sx, sy) in slots.items():
            t = snow.crop(((x - x0) * 16, (y - y0) * 16, (x - x0) * 16 + 16, (y - y0) * 16 + 16))
            sheets["winter"].paste(t, (sx, sy))
        print("winter art kept for", name, len(slots), "tiles")
    sheets["winter"].save(os.path.join(assets, "winter_GildedValeBuildings.png"))
    # the FRONT tiles on the path: their path part to a new tile, underfoot on Back2
    seasons = ("spring", "summer", "fall", "winter")
    sh = {se: Image.open(os.path.join(assets, se + "_GildedValeBuildings.png")).convert("RGBA") for se in seasons}
    work = []
    for name, boxes in SPLIT.items():
        for x, y in tiles(boxes):
            v = g["Front"][y][x]
            if own(v) and g["Back2"][y][x] == "0" and g["Buildings"][y][x] == "0":
                i = int(v) - FEATURE_FIRST
                c = ((i % cols) * 16, (i // cols) * 16, (i % cols) * 16 + 16, (i // cols) * 16 + 16)
                if ground_mask(sh["spring"].crop(c), sh["spring"].crop(c), "spring"):
                    work.append((name, x, y, c))
    if work:
        H0 = sh["spring"].height
        for se in seasons:
            big = Image.new("RGBA", (sh[se].width, H0 + 16 * ((len(work) + cols - 1) // cols)))
            big.paste(sh[se], (0, 0))
            sh[se] = big
        for k, (name, x, y, c) in enumerate(work):
            n = H0 // 16 * cols + k
            nc = ((n % cols) * 16, (n // cols) * 16)
            spring = sh["spring"].crop(c)
            for se in seasons:
                t = sh[se].crop(c)
                gm = ground_mask(spring, t, se)
                obj, grd = t.copy(), Image.new("RGBA", (16, 16))
                op, gp = obj.load(), grd.load()
                for (px, py) in gm:
                    gp[px, py], op[px, py] = op[px, py], (0, 0, 0, 0)
                sh[se].paste(obj, c[:2])
                sh[se].paste(grd, nc)
            g["Back2"][y][x] = str(FEATURE_FIRST + n)
            print(f"{name} {x},{y}: the path round it underfoot (tile {FEATURE_FIRST + n})")
        for se in seasons:
            sh[se].save(os.path.join(assets, se + "_GildedValeBuildings.png"))
        text = open(path, encoding="utf-8").read()
        m = span_in(text, "Back2")
        text = text[:m.start(1)] + ",\n".join(",".join(r) for r in g["Back2"]) + text[m.end(1):]
        H1 = sh["spring"].height
        text = re.sub(r'(<tileset firstgid="20000" [^>]*tilecount=")\d+(")', lambda mm: mm.group(1) + str(H1 // 16 * cols) + mm.group(2), text)
        text = re.sub(r'(<image source="spring_GildedValeBuildings.png" width="\d+" height=")\d+(")', lambda mm: mm.group(1) + str(H1) + mm.group(2), text)
        open(path, "w", encoding="utf-8", newline="\n").write(text)
    # the mirrored columns, in every season's sheet
    for season in ("spring", "summer", "fall", "winter"):
        sp = os.path.join(assets, season + "_GildedValeBuildings.png")
        sheet = Image.open(sp).convert("RGBA")
        n = 0
        for name, cols_ in MIRROR.items():
            for (x0, y0, x1, y1), dx in cols_:
                for x, y in tiles([(x0, y0, x1, y1)]):
                    src = next((int(g[k][y][x]) for k in ("Buildings", "Back2") if own(g[k][y][x])), None)
                    dst = next((int(g[k][y][dx + x - x0]) for k in ("Buildings", "Back2") if own(g[k][y][dx + x - x0])), None)
                    if src is None or dst is None:
                        print("no tile to mirror", name, x, y)
                        continue
                    s_, d_ = src - FEATURE_FIRST, dst - FEATURE_FIRST
                    t = sheet.crop(((s_ % cols) * 16, (s_ // cols) * 16, (s_ % cols) * 16 + 16, (s_ // cols) * 16 + 16))
                    sheet.paste(t.transpose(Image.FLIP_LEFT_RIGHT), ((d_ % cols) * 16, (d_ // cols) * 16))
                    n += 1
        sheet.save(sp)
        print("mirrored", n, "tiles in", season)


if __name__ == "__main__":
    _patch(sys.argv[1])

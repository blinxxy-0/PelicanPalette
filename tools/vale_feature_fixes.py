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


# What of a FRONT box goes on Front: only the thing itself. Its painting also holds bits
# of the ground round it (the gravel path, grass tufts, snow), and on Front those drew
# over the farmer (the user, 2026-11-08: walking south behind the east statue, "blue
# path texture gets picked up and you can weirdly walk behind it"; a scan of every
# season then found grass and snow specks round the birdbath, the statues, the frames,
# the heap, the barrow and the arch posts). The rest stays underfoot on Back2.
MIN_PART = 16       # px: a piece smaller than this, apart from the thing, is ground
# What counts as ground round each feature's things, besides loose specks: the parterre's
# gravel and the statues' shadows on it, and grass tufts; the stepping stone and grass at
# the tea garden's arch posts. (Not everywhere: Fern's heap and barrow are sand-brown.)
GROUND = {"parterre": ("gravel", "grass"), "tea": ("stone", "grass")}


def sand(c):
    """The gravel paths in the spring art."""
    return c[3] and c[0] > 140 and c[0] - c[2] > 60 and c[1] > 80


def snow(c):
    return c[3] and min(c[:3]) > 175 and c[2] >= c[0] - 5


def ground(c, kinds):
    if not c[3]:
        return False
    r, g, b = c[:3]
    return (("sand" in kinds and sand(c))
            or ("gravel" in kinds and r > 120 and r - b > 60 and g > 80)          # (its shadow too)
            or ("grass" in kinds and g > r + 15 and g > b + 15 and 0.3 * r + 0.59 * g + 0.11 * b > 70)
            or ("stone" in kinds and abs(r - g) < 12 and abs(g - b) < 18 and 80 < r < 135))


def front_mask(spring, art, season, kinds=("sand",)):
    """{(x, y)} px of a box's art that are the thing (the rest is ground). From the
    spring art: every pixel that isn't gravel, in pieces of MIN_PART px or more (joined
    8 ways), so loose grass tufts and specks go. Summer and fall share spring's pixels.
    In winter: the spring thing's pixels (its snowcap too), and a pixel beside them only
    if it isn't snow (the winter repaint's outline sits a pixel off)."""
    W, H = spring.size
    sp = spring.load()
    solid = {(x, y) for y in range(H) for x in range(W) if sp[x, y][3] and not ground(sp[x, y], kinds)}
    thing, seen = set(), set()
    for p in solid:
        if p in seen:
            continue
        part, stack = [], [p]
        seen.add(p)
        while stack:
            x, y = stack.pop()
            part.append((x, y))
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    q = (x + dx, y + dy)
                    if q in solid and q not in seen:
                        seen.add(q)
                        stack.append(q)
        if len(part) >= MIN_PART:
            thing.update(part)
    if season != "winter":
        return thing
    ar = art.load()
    near = {(x + dx, y + dy) for x, y in thing for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
    return thing | {(x, y) for x, y in near if 0 <= x < W and 0 <= y < H and ar[x, y][3] and not snow(ar[x, y])}


def lift(spring, art, season, kinds=("sand",)):
    """(the thing, the ground) of a box's art, as two images."""
    keep = front_mask(spring, art, season, kinds)
    thing, ground = Image.new("RGBA", art.size), art.copy()
    tp, gp = thing.load(), ground.load()
    for x, y in keep:
        tp[x, y], gp[x, y] = gp[x, y], (0, 0, 0, 0)
    return thing, ground


def split_front(name, arts, left, top):
    """In a feature's season arts (px), lifts each FRONT box's thing out of the art: it
    returns [(x0, y0, {season: image})] to lay on Front, and leaves the ground behind."""
    out = []
    for x0, y0, x1, y1 in FRONT.get(name, ()):
        box = ((x0 - left) * 16, (y0 - top) * 16, (x1 - left + 1) * 16, (y1 - top + 1) * 16)
        spring = arts["spring"].crop(box)
        lifted = {}
        for season, im in arts.items():
            thing, rest = lift(spring, im.crop(box), season, GROUND.get(name, ("sand",)))
            im.paste(rest, box[:2])
            lifted[season] = thing
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
SEASONS = ("spring", "summer", "fall", "winter")
LAYERS = ("Back2", "Buildings", "Front")


def span_in(text, name):
    return re.search(r'<layer id="\d+" name="' + name + r'"[^>]*>\s*<data encoding="csv">\n(.*?)\n\s*</data>', text, re.S)


def _patch(assets):
    path = os.path.join(assets, "GildedVale.tmx")
    text = open(path, encoding="utf-8").read()
    g = {n: [row.rstrip(",").split(",") for row in span_in(text, n).group(1).split("\n")] for n in LAYERS}
    own = lambda v: FEATURE_FIRST <= int(v) < FEATURE_FIRST + 10000

    # collision: SOLID to Buildings, WALK to Back2, FRONT to Front
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
    for name, boxes in FRONT.items():
        for x, y in tiles(boxes):
            src = next((k for k in ("Buildings", "Back2") if own(g[k][y][x])), None)
            if src and g["Front"][y][x] == "0":
                g["Front"][y][x], g[src][y][x] = g[src][y][x], "0"
                moved.append((name, x, y, "Front"))
            elif not own(g["Front"][y][x]):
                print("not moved to Front", name, x, y)
    for name, x, y, dst in moved:
        print(f"{name} {x},{y} -> {dst}")

    sh = {se: Image.open(os.path.join(assets, se + "_GildedValeBuildings.png")).convert("RGBA") for se in SEASONS}
    cols = sh["spring"].width // 16
    slot = lambda v: (((int(v) - FEATURE_FIRST) % cols) * 16, ((int(v) - FEATURE_FIRST) // cols) * 16)

    def whole(se, x, y):
        """A map tile's feature art, its layers stacked."""
        im = Image.new("RGBA", (16, 16))
        for k in LAYERS:
            if own(g[k][y][x]):
                sx, sy = slot(g[k][y][x])
                im.alpha_composite(sh[se].crop((sx, sy, sx + 16, sy + 16)))
        return im

    def region(se, x0, y0, x1, y1):
        im = Image.new("RGBA", ((x1 - x0 + 1) * 16, (y1 - y0 + 1) * 16))
        for x, y in tiles([(x0, y0, x1, y1)]):
            im.paste(whole(se, x, y), ((x - x0) * 16, (y - y0) * 16))
        return im

    def put(se, x, y, im, layer):
        """A tile's art into its slot on that layer, the tile's other slots emptied."""
        for k in LAYERS:
            if own(g[k][y][x]):
                sx, sy = slot(g[k][y][x])
                sh[se].paste(im if k == layer else Image.new("RGBA", (16, 16)), (sx, sy))

    # the winter art kept from spring (Fern's heap), snowed on
    for name, (x0, y0, x1, y1) in WINTER_KEEP.items():
        snowy = snowed(region("spring", x0, y0, x1, y1))
        for x, y in tiles([(x0, y0, x1, y1)]):
            layer = next((k for k in ("Front", "Buildings", "Back2") if own(g[k][y][x])), None)
            if layer:
                put("winter", x, y, snowy.crop(((x - x0) * 16, (y - y0) * 16, (x - x0) * 16 + 16, (y - y0) * 16 + 16)), layer)
        print("winter art kept for", name)

    # each FRONT box: the thing on its Front tiles, the ground round it on Back2 tiles
    # (new tiles at the sheets' end where a tile has none yet)
    nxt = sh["spring"].height // 16 * cols
    work = []
    for name, boxes in FRONT.items():
        for x0, y0, x1, y1 in boxes:
            parts = {}
            spring = region("spring", x0, y0, x1, y1)
            for se in SEASONS:
                parts[se] = lift(spring, region(se, x0, y0, x1, y1), se, GROUND.get(name, ("sand",)))
            for x, y in tiles([(x0, y0, x1, y1)]):
                c = ((x - x0) * 16, (y - y0) * 16, (x - x0) * 16 + 16, (y - y0) * 16 + 16)
                if any(parts[se][1].crop(c).getbbox() for se in SEASONS) and not own(g["Back2"][y][x]):
                    if g["Back2"][y][x] != "0" or g["Buildings"][y][x] != "0":
                        print("no room underfoot at", x, y)
                    else:
                        g["Back2"][y][x] = str(FEATURE_FIRST + nxt)
                        nxt += 1
                work.append((x, y, {se: (parts[se][0].crop(c), parts[se][1].crop(c)) for se in SEASONS}))
            print(f"{name} {x0},{y0}: the thing on Front, the ground round it underfoot")
    H1 = max(sh["spring"].height, (nxt + cols - 1) // cols * 16)
    for se in SEASONS:
        if sh[se].height < H1:
            big = Image.new("RGBA", (sh[se].width, H1))
            big.paste(sh[se], (0, 0))
            sh[se] = big
    for x, y, art in work:
        for se in SEASONS:
            thing, rest = art[se]
            sh[se].paste(thing, slot(g["Front"][y][x]))
            if own(g["Back2"][y][x]):
                sh[se].paste(rest, slot(g["Back2"][y][x]))

    # the mirrored columns
    for se in SEASONS:
        for name, cols_ in MIRROR.items():
            for (x0, y0, x1, y1), dx in cols_:
                for x, y in tiles([(x0, y0, x1, y1)]):
                    src = next((g[k][y][x] for k in ("Buildings", "Back2") if own(g[k][y][x])), None)
                    dst = next((g[k][y][dx + x - x0] for k in ("Buildings", "Back2") if own(g[k][y][dx + x - x0])), None)
                    if src is None or dst is None:
                        print("no tile to mirror", name, x, y)
                        continue
                    sx, sy = slot(src)
                    sh[se].paste(sh[se].crop((sx, sy, sx + 16, sy + 16)).transpose(Image.FLIP_LEFT_RIGHT), slot(dst))

    for se in SEASONS:
        sh[se].save(os.path.join(assets, se + "_GildedValeBuildings.png"))
    for n in LAYERS:
        m = span_in(text, n)
        text = text[:m.start(1)] + ",\n".join(",".join(r) for r in g[n]) + text[m.end(1):]
    text = re.sub(r'(<tileset firstgid="20000" [^>]*tilecount=")\d+(")', lambda mm: mm.group(1) + str(H1 // 16 * cols) + mm.group(2), text)
    text = re.sub(r'(<image source="spring_GildedValeBuildings.png" width="\d+" height=")\d+(")', lambda mm: mm.group(1) + str(H1) + mm.group(2), text)
    open(path, "w", encoding="utf-8", newline="\n").write(text)
    print("wrote", path)


if __name__ == "__main__":
    _patch(sys.argv[1])

"""The Old Studios' signs (the user, 2026-11-08; docs/design/gilded_vale_map.md, sections 1
and 8): the weathered sign listing the studios' past artists, and a carved numeral, filled
with flaking gold, on each studio's plaque over its door (I, II, III).

Patches the committed GildedVale.tmx in place (build_vale.py doesn't reproduce it), with
art of its own on a seasonal sheet of its own, spring_GildedValeSigns.png and its summer_,
fall_ and winter_ siblings:
- the weathered sign: 3 x 3 tiles on the grass south of the studios' lane, by the middle
  bridge. Only its feet (the bottom row) are solid; the board above is on Front2, so you
  walk behind it. Right-click it: blinxxy.MuseArt_Vale.studios.sign.
- the numerals: each plaque's own tiles, copied from that season's building sheet with the
  numeral drawn on, swapped in where the building's tiles were. Right-click the wall under
  a plaque (beside the door): blinxxy.MuseArt_Vale.studios.plaque_<n>.

Later states (For Sale over II and III, then Sold, then the numeral back after Robin's
renovation) can swap the same plaque cells by content pack EditMap.

The building tiles a plaque replaced are kept in art/gilded_vale/signs/plaques.json, so
the script can be run again (it redraws from the originals).

Usage: python tools/vale_studio_signs.py ["[CP] Gilded Vale (dev)/assets"]
"""
import json, os, random, re, sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_tmx import layer_data

ART = "art/gilded_vale/"
SEASONS = ("spring", "summer", "fall", "winter")
SHEET = "spring_GildedValeSigns.png"
SHEET_COLS = 8
FIRST = 31200                          # after the fences (31100 + 66)
TILESET = "z_blinxxy.MuseArt_GildedValeSigns"
BUILD_FIRST = 20000                    # build_vale.py's building sheet
BUILD_COLS = 32
LIFT = 6                               # build_vale.py LIFT: buildings stand 6 px off their last row
RECORD = ART + "signs/plaques.json"

SIGN_LEFT, SIGN_BOTTOM = 78, 45        # on the grass across the lane from the foot of Lupini's stairs
                                       # (x 69-71 had the decor's maple and bush against its post)
SIGN_KEY = "blinxxy.MuseArt_Vale.studios.sign"

# each studio: its building art (as in build_vale.py PICKS), left tile, bottom row, the
# numeral, and its plaque's inside in the art's own pixels (inclusive), measured by eye
STUDIOS = {
    "1": ("buildings/rescaled_v1/studio_large_i400_s1_0", 68, 38, "I", (67, 110, 88, 116)),
    "2": ("buildings/rescaled_v1/studio_b_i400_s1_0", 82, 38, "II", (52, 85, 77, 92)),
    "3": ("buildings/vale_buildings_v4/old_studio_r4_1", 88, 38, "III", (52, 91, 81, 97)),
}
FLAKE = 0.3                            # how much of the gold has flaked off (all three, for now)

FONT = {"I": ["111", "010", "010", "010", "111"]}
INK = (59, 36, 22, 255)
GOLD = (196, 160, 82, 255)
GOLD_D = (150, 116, 58, 255)


# ---- the numerals ---------------------------------------------------------------------

def plaque_origin(art, left, bottom):
    """Where the art's own pixel (0, 0) lands on the map, in map pixels: build_vale.py's
    tile_piece crops a piece to its art, centres it in whole tiles and stands it LIFT px
    above the bottom of its last row."""
    im = Image.open(os.path.join(ART, art + ".png")).convert("RGBA")
    bx0, by0, bx1, by1 = im.getbbox()
    bw, bh = bx1 - bx0, by1 - by0
    cols, rows = -(-bw // 16), -(-(bh + LIFT) // 16)
    ox = left * 16 + (cols * 16 - bw) // 2 - bx0
    oy = (bottom - rows + 1) * 16 + rows * 16 - bh - LIFT - by0
    return ox, oy


def numeral_pixels(text, box, seed):
    """The numeral's pixels in the art's own coordinates: the carved shadow, then the gold."""
    x0, y0, x1, y1 = box
    w = len(text) * 4 - 1
    x, y = x0 + (x1 - x0 + 1 - w) // 2, y0 + (y1 - y0 + 1 - 5) // 2
    cells = []
    for k, ch in enumerate(text):
        for r, row in enumerate(FONT[ch]):
            for c, v in enumerate(row):
                if v == "1":
                    cells.append((x + k * 4 + c, y + r))
    rnd = random.Random(seed)
    out = {}
    for cx, cy in cells:
        out[(cx + 1, cy + 1)] = INK                   # carved: the cut's shadow, down and right
    for cx, cy in cells:
        out[(cx, cy)] = INK if rnd.random() < FLAKE else (GOLD if rnd.random() > 0.25 else GOLD_D)
    return out


# ---- the weathered sign ---------------------------------------------------------------

TINY = {  # 3x5
    "O": ["010", "101", "101", "101", "010"], "L": ["100", "100", "100", "100", "111"],
    "D": ["110", "101", "101", "101", "110"], "S": ["011", "100", "010", "001", "110"],
    "T": ["111", "010", "010", "010", "010"], "U": ["101", "101", "101", "101", "011"],
    "I": ["111", "010", "010", "010", "111"], " ": ["000"] * 5,
}


def tiny_text(img, s, x, y, col):
    p = img.load()
    for ch in s:
        for r, row in enumerate(TINY[ch]):
            for c, v in enumerate(row):
                if v == "1":
                    p[x + c, y + r] = col
        x += 4


def scribble(img, x0, x1, y, col, rnd, fade=0.0):
    p = img.load()
    x, end = x0 + rnd.randint(0, 1), x1 - rnd.randint(0, 4)
    while x < end:
        w = rnd.randint(2, 4)
        for xx in range(x, min(x + w, end)):
            if rnd.random() >= fade:
                p[xx, y] = col
        x += w + 1


def weathered_sign(season):
    """48 x 48: a weathered board on two posts, its feet in the bottom row. The header
    still reads; the names under it have faded to marks, the last of studio I's worn away."""
    rnd = random.Random(7)
    W, H = 48, 48
    img = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(img)
    top = 13                                           # the board's top edge
    # its shadow on the grass, under the posts
    shadow = Image.new("RGBA", (W, H))
    ImageDraw.Draw(shadow).ellipse((4, H - 5, W - 5, H - 1), fill=(0, 0, 0, 60))
    img.alpha_composite(shadow)
    POST, POST_D = (104, 72, 46, 255), (70, 46, 30, 255)
    for px0 in (6, 39):
        d.rectangle((px0, top + 20, px0 + 2, H - 3), fill=POST)
        d.line((px0 + 2, top + 20, px0 + 2, H - 3), fill=POST_D)
    BOARD, BOARD_L, BOARD_D, EDGE = (150, 116, 78, 255), (172, 140, 100, 255), (124, 92, 60, 255), (62, 40, 26, 255)
    GREY = (138, 128, 104, 255)
    d.rectangle((1, top, W - 2, top + 26), fill=BOARD, outline=EDGE)
    for y in range(top + 2, top + 26, 4):              # the planks
        d.line((2, y, W - 3, y), fill=BOARD_D)
    p = img.load()
    for _ in range(150):                               # weathering: bleached and darkened grain
        x, y = rnd.randint(2, W - 3), rnd.randint(top + 1, top + 25)
        if p[x, y] == BOARD:
            p[x, y] = rnd.choice([BOARD_L, BOARD_D, GREY])
    d.rectangle((2, top + 1, W - 3, top + 7), fill=(110, 80, 52, 255))   # the header strip
    tiny_text(img, "OLD STUDIOS", (W - (11 * 4 - 1)) // 2, top + 2, (212, 196, 156, 255))
    FADED = (88, 62, 40, 255)
    # two columns of names: I on the left (its last line all but gone), II and III on the right
    for y in (top + 10, top + 12, top + 14, top + 16):
        scribble(img, 4, 22, y, FADED, rnd)
    scribble(img, 4, 22, top + 18, FADED, rnd, fade=0.75)
    for y in (top + 10, top + 12, top + 17, top + 19):
        scribble(img, 25, 43, y, FADED, rnd)
    d.line((24, top + 9, 24, top + 24), fill=BOARD_D)  # the line between the columns
    # a split in the bottom plank, and a missing corner
    d.line((30, top + 23, 37, top + 24), fill=EDGE)
    p[W - 2, top + 26] = (0, 0, 0, 0)
    p[W - 3, top + 26] = EDGE
    if season == "winter":
        SNOW, SNOW_D = (246, 250, 255, 255), (200, 214, 232, 255)
        for x in range(1, W - 1):                       # snow along the board's top
            depth = 2 + (1 if rnd.random() < 0.4 else 0)
            for k in range(depth):
                p[x, top - k] = SNOW if k < depth - 1 or x % 5 else SNOW_D
            p[x, top + 1] = SNOW_D if p[x, top + 1][3] else p[x, top + 1]
        for px0 in (5, 38):                             # and drifted against the posts
            for x in range(px0, px0 + 5):
                p[x, H - 3] = SNOW
                if px0 < x < px0 + 4:
                    p[x, H - 4] = SNOW
    else:
        MOSS = (96, 120, 52, 255) if season != "fall" else (130, 112, 50, 255)
        for x in (5, 6, 9, 38, 39, 42):                 # moss (in fall, browned) at the posts' feet
            p[x, H - 3] = MOSS
        p[2, top + 25] = MOSS
        p[3, top + 25] = MOSS
    return img


# ---- the map --------------------------------------------------------------------------

def read_layers(text):
    layers = {}
    for m in re.finditer(r'<layer id="\d+" name="([^"]+)" width="(\d+)" height="(\d+)">\s*<data encoding="csv">(.*?)</data>',
                         text, re.S):
        w = int(m.group(2))
        vals = [int(v) for v in m.group(4).replace("\n", "").split(",") if v.strip()]
        layers[m.group(1)] = (w, vals, m.span(4))
    return layers


def write_layers(text, layers, changed):
    # replace from the end, so earlier spans stay valid
    for name in sorted(changed, key=lambda n: -layers[n][2][0]):
        w, vals, (a, b) = layers[name]
        rows = [",".join(str(v) for v in vals[i:i + w]) for i in range(0, len(vals), w)]
        text = text[:a] + "\n" + ",\n".join(rows) + "\n    " + text[b:]
    return text


def tile_of(sheets, season, gid):
    i = gid - BUILD_FIRST
    sx, sy = (i % BUILD_COLS) * 16, (i // BUILD_COLS) * 16
    return sheets[season].crop((sx, sy, sx + 16, sy + 16))


def main(assets):
    tmx = os.path.join(assets, "GildedVale.tmx")
    text = open(tmx, encoding="utf-8").read()
    layers = read_layers(text)
    W = layers["Back"][0]
    build = {s: Image.open(os.path.join(assets, f"{s}_GildedValeBuildings.png")).convert("RGBA") for s in SEASONS}
    record = json.load(open(RECORD)) if os.path.exists(RECORD) else {}
    tiles = {s: [] for s in SEASONS}                  # my sheet's tiles, in order
    changed = set()

    def put(layer, x, y, gid):
        layers[layer][1][y * W + x] = gid
        changed.add(layer)

    def get(layer, x, y):
        return layers[layer][1][y * W + x]

    # an earlier run's sign comes off, wherever it stood
    for layer in ("Buildings", "Front2"):
        for k, g in enumerate(layers[layer][1]):
            if FIRST <= g < FIRST + 100:
                put(layer, k % W, k // W, 0)
    # every plaque tile an earlier run swapped goes back to the building's own, first
    for key, (layer, g) in record.items():
        x, y = map(int, key.split(","))
        put(layer, x, y, g)

    # 1. the weathered sign: 9 tiles, bottom row solid
    arts = {s: weathered_sign(s) for s in SEASONS}
    for j in range(3):
        for i in range(3):
            idx = len(tiles["spring"])
            for s in SEASONS:
                tiles[s].append(arts[s].crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)))
            x, y = SIGN_LEFT + i, SIGN_BOTTOM - 2 + j
            if j == 2:
                put("Buildings", x, y, FIRST + idx)
            else:
                put("Front2", x, y, FIRST + idx)
            for layer in ("Back2", "Paths"):          # no wildflower tufts or markers under it
                if get(layer, x, y):
                    put(layer, x, y, 0)

    # 2. the numerals: each plaque's tiles, copied with the numeral drawn on
    used = {}                                         # (the record keeps only the tiles in use)
    messages = {(SIGN_LEFT + i, SIGN_BOTTOM): SIGN_KEY for i in range(3)}
    for n, (art, left, bottom, numeral, box) in STUDIOS.items():
        ox, oy = plaque_origin(art, left, bottom)
        pix = numeral_pixels(numeral, box, int(n))
        cells = {}
        for (ax, ay), col in pix.items():
            mx, my = ox + ax, oy + ay
            cells.setdefault((mx // 16, my // 16), []).append((mx % 16, my % 16, col))
        for (tx, ty), pts in sorted(cells.items()):
            key = f"{tx},{ty}"
            if key not in record:
                # the building's own tile here: the topmost layer holding a building-sheet tile
                for layer in ("AlwaysFront", "Front2", "Front", "Buildings"):
                    g = get(layer, tx, ty)
                    if BUILD_FIRST <= g < BUILD_FIRST + 10000:
                        record[key] = [layer, g]
                        break
                else:
                    raise SystemExit(f"no building tile under the plaque at {tx},{ty}")
            layer, g = used[key] = record[key]
            idx = len(tiles["spring"])
            for s in SEASONS:
                t = tile_of(build, s, g)
                tp = t.load()
                for x, y, col in pts:
                    tp[x, y] = col
                tiles[s].append(t)
            put(layer, tx, ty, FIRST + idx)
        # its words: on the wall under the plaque, beside the door (the plaque itself is out of reach)
        cols = sorted({tx for tx, _ in cells})
        door = DOORS_AT[n]
        for tx in range(cols[0] - 1, cols[-1] + 2):
            if tx not in door and min(abs(tx - d) for d in door) == 1 and BUILD_FIRST <= get("Buildings", tx, bottom) < BUILD_FIRST + 10000:
                messages[(tx, bottom)] = f"blinxxy.MuseArt_Vale.studios.plaque_{n}"
        print(f"studio {numeral}: plaque tiles {sorted(cells)}, door {door}, message tiles",
              [k for k, v in messages.items() if v.endswith("_" + n)])

    os.makedirs(os.path.dirname(RECORD), exist_ok=True)
    json.dump(used, open(RECORD, "w"), indent=1)

    # the sheets
    rows = -(-len(tiles["spring"]) // SHEET_COLS)
    for s in SEASONS:
        sheet = Image.new("RGBA", (SHEET_COLS * 16, rows * 16))
        for k, t in enumerate(tiles[s]):
            sheet.alpha_composite(t, ((k % SHEET_COLS) * 16, (k // SHEET_COLS) * 16))
        sheet.save(os.path.join(assets, SHEET.replace("spring_", s + "_")))

    text = write_layers(text, layers, changed)

    # the tileset, after the fences
    ts = (f'  <tileset firstgid="{FIRST}" name="{TILESET}" tilewidth="16" tileheight="16" '
          f'tilecount="{rows * SHEET_COLS}" columns="{SHEET_COLS}">\n'
          f'    <image source="{SHEET}" width="{SHEET_COLS * 16}" height="{rows * 16}" />\n  </tileset>\n')
    text = re.sub(r'  <tileset firstgid="%d" name="%s".*?</tileset>\n' % (FIRST, TILESET), "", text, flags=re.S)
    i = text.index("  <layer ")
    text = text[:i] + ts + text[i:]

    # the messages: TileData on the Buildings object layer (as the doors' warps are)
    text = re.sub(r'    <object id="\d+" name="TileData" x="\d+" y="\d+" width="16" height="16">\s*<properties>\s*'
                  r'<property name="Action" type="string" value="Message blinxxy\.MuseArt_Vale\.studios\.[^"]*" />\s*'
                  r'</properties>\s*</object>\n', "", text)
    next_id = int(re.search(r'nextobjectid="(\d+)"', text).group(1))
    objs = ""
    for (x, y), key in sorted(messages.items()):
        objs += (f'    <object id="{next_id}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">\n'
                 f'      <properties>\n        <property name="Action" type="string" value="Message {key}" />\n'
                 f'      </properties>\n    </object>\n')
        next_id += 1
    m = re.search(r'<objectgroup id="\d+" name="Buildings"[^>]*>\n', text)
    text = text[:m.end()] + objs + text[m.end():]
    text = re.sub(r'nextobjectid="\d+"', f'nextobjectid="{next_id}"', text, count=1)
    open(tmx, "w", encoding="utf-8").write(text)
    print("wrote", tmx, "and", rows * SHEET_COLS, "tiles on", SHEET, "and its seasons")


# each studio's door columns on its bottom row: I's is its warp in build_vale.py DOORS;
# II's and III's have no warp yet, read off a render (III's straddles two tiles)
DOORS_AT = {"1": (73,), "2": (86,), "3": (91, 92)}

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "[CP] Gilded Vale (dev)/assets")

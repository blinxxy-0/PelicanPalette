"""Placeholder art for Lupini's heart events (1.4; docs/vale_events.md, tools/build_lupini_events.py),
drawn by script, pixel by pixel: no PixelLab. Outlines are a darker shade of the colour they border,
never black (vanillafy.selout runs over every piece at the end). All of it is PLACEHOLDER, to be
replaced by real art like the rest of the Vale's.

Writes, into [CP] Gilded Vale (dev)/assets/lupini/:
  items.png (64x32), the furniture (data/lupini_events.json, Data/Furniture):
      (0, 0)   16x32  the puddle sketch, framed (his 2-heart event)
      (16, 0)  16x32  his old travelling easel (his 10-heart event; furniture only)
      (32, 0)  32x32  the Vale at dawn, the painting you make beside him (his 10-heart event)
  event_props.png (64x16), the events' props (the easels at the overlook are items.png's):
      (0, 0)   32x16  the puddle in the road, the light lying across it (2 hearts)
      (32, 0)  32x16  the boards he's prised off the Old Studio's door, on the ground (6 hearts)
  map_tiles.png (160x48), map patches (16x16 tiles, 10 columns):
      (0, 0)   64x48  the Vale's sign, repainted (after his 8-heart event): the sign's own tiles
                      (GildedValeBuildings 1381-1384, 1413-1416, 1445-1448), the board repainted
      (64, 0)  16x32  the Old Studio's door, boarded (until his 6-heart event): the door's own
                      tiles (1029, 1061) with planks nailed across
      (80, 0)  16x32  the same door without them (the 6-heart event takes them off on screen)
      (96, 0)  64x32  Bea's washing line (for his 4-heart walk), its posts' feet on the bottom row
  hall_wall.png (144x32), the two pictures on the Exhibition Hall's end wall (8 hearts):
      (0, 0), (32, 0), (64, 0)  32x32  the Old Studio's unfinished painting: restored, finished,
                                       left as it was (your answer at 6 hearts)
      (96, 0), (112, 0), (128, 0)  16x32  his sketch from your walk: the washing line, the bus's
                                       turning circle, Tobin's clay pit (your answer at 4 hearts)

The sign's and the door's tiles are the same in every season (checked: spring_ to winter_
GildedValeBuildings.png), so there's one of each.

Usage: python tools/art/lupini_event_art.py        (from the repository root)
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PACK = "[CP] Gilded Vale (dev)"
OUT = os.path.join(PACK, "assets", "lupini")
VALE_SHEET = os.path.join(PACK, "assets", "spring_GildedValeBuildings.png")

# wood (frames, posts, the easel), from julian_items.py's frame
WOOD, WOOD_LIGHT, WOOD_DARK, WOOD_EDGE = (140, 88, 52), (176, 120, 72), (104, 64, 38), (78, 46, 28)
GILT, GILT_LIGHT, GILT_DARK = (196, 152, 64), (230, 196, 110), (138, 100, 40)
PAPER, PAPER_SHADE = (240, 234, 218), (214, 206, 188)
CHARCOAL, CHARCOAL_LIGHT, CHARCOAL_FAINT = (92, 84, 80), (146, 138, 130), (188, 180, 168)
CANVAS, CANVAS_SHADE = (232, 222, 200), (206, 194, 168)
PENCIL = (170, 160, 146)
PLUM, PLUM_LIGHT = (104, 48, 82), (140, 74, 112)


def new(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def put(im, x, y, c, a=255):
    if 0 <= x < im.width and 0 <= y < im.height:
        im.putpixel((x, y), tuple(c[:3]) + (a,))


def rect(im, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(im, x, y, c)


def line(im, x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        put(im, round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)


def frame(im, x0, y0, x1, y1, outer, mid, inner):
    """A picture frame: an outer edge, its face, an inner lip (all shades of one wood)."""
    for x in range(x0, x1 + 1):
        for y in range(y0, y1 + 1):
            edge = min(x - x0, x1 - x, y - y0, y1 - y)
            if edge == 0:
                put(im, x, y, outer)
            elif edge == 1:
                put(im, x, y, mid if (y - y0 > 1 or x1 - x <= 1) else inner)


def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ---- items.png ----

def puddle_sketch():
    """A page from his sketchbook, mostly white: the road's edge in a few lines, the puddle, and the
    one bright stripe of light lying across the water. In a thin wooden frame."""
    im = new(16, 32)
    frame(im, 0, 0, 15, 31, WOOD_EDGE, WOOD, WOOD_LIGHT)
    rect(im, 2, 2, 13, 29, PAPER)
    # the road's edge: two charcoal strokes running into the page
    line(im, 2, 19, 13, 15, CHARCOAL_LIGHT)
    line(im, 2, 21, 7, 19, CHARCOAL_FAINT)
    # the puddle: a soft grey oval of hatching...
    for y in range(21, 27):
        for x in range(3, 13):
            dx, dy = (x - 7.5) / 5.0, (y - 23.5) / 3.0
            if dx * dx + dy * dy <= 1.0:
                put(im, x, y, CHARCOAL_FAINT if (x + y) % 2 else PAPER_SHADE)
    # ...and the light lying flat across it: left as bright paper, a touch of warm in it
    for x in range(4, 12):
        put(im, x, 23, (252, 246, 222))
    line(im, 3, 24, 12, 24, CHARCOAL_LIGHT)
    # his initial, very small, bottom right
    put(im, 12, 28, CHARCOAL), put(im, 12, 27, CHARCOAL), put(im, 13, 28, CHARCOAL)
    return im


def travelling_easel():
    """His folding field easel: three thin legs, a paint box slung between them on a plum strap, and
    a small canvas clamped at the top. Worn wood, darker than the shop's easel; not the master's."""
    im = new(16, 32)
    leg, leg_light, leg_dark = (150, 104, 62), (184, 134, 84), (104, 68, 40)
    # the back leg, behind
    line(im, 8, 12, 8, 30, leg_dark)
    # the front legs, splayed
    line(im, 6, 12, 3, 31, leg)
    line(im, 7, 12, 4, 31, leg_light)
    line(im, 9, 12, 12, 31, leg)
    line(im, 10, 12, 13, 31, leg_dark)
    # the paint box, across the legs, on its strap
    rect(im, 3, 18, 12, 21, (122, 80, 48))
    rect(im, 3, 18, 12, 18, (156, 108, 66))
    line(im, 3, 22, 12, 22, (92, 58, 34))
    for x in range(4, 12):
        put(im, x, 17, PLUM if x % 3 else PLUM_LIGHT)
    put(im, 5, 20, (226, 84, 70)), put(im, 7, 20, (70, 120, 190)), put(im, 9, 20, (236, 196, 64))
    # the clamp, and the canvas: a small sky and a line of hills, started
    rect(im, 7, 1, 8, 2, leg_dark)
    rect(im, 3, 3, 12, 12, CANVAS)
    rect(im, 3, 3, 12, 3, CANVAS_SHADE)
    rect(im, 4, 4, 11, 7, (168, 196, 226))
    rect(im, 4, 8, 11, 9, (226, 206, 150))
    line(im, 4, 10, 11, 8, (104, 148, 96))
    rect(im, 3, 12, 12, 12, CANVAS_SHADE)
    # the ledge it stands on
    rect(im, 2, 13, 13, 13, leg)
    rect(im, 2, 14, 13, 14, leg_dark)
    return im


def vale_at_dawn():
    """The Vale at dawn, from the overlook: the sun coming up over the falls, the valley still in blue
    shadow, a gold line on the lake. In a plain wooden frame."""
    im = new(32, 32)
    frame(im, 0, 0, 31, 31, WOOD_EDGE, WOOD, WOOD_LIGHT)
    sky = [(64, 82, 142), (92, 98, 160), (150, 118, 170), (214, 140, 150), (242, 170, 128), (250, 204, 120)]
    for y in range(2, 16):
        c = sky[min(len(sky) - 1, (y - 2) * len(sky) // 14)]
        rect(im, 2, y, 29, y, c)
    # the sun, coming up behind the cliff
    for y in range(10, 16):
        for x in range(16, 26):
            if (x - 20.5) ** 2 + (y - 15.5) ** 2 <= 16:
                put(im, x, y, (255, 238, 170))
    # far hills, then the cliff and the falls
    for x in range(2, 30):
        top = 14 - round(2 * abs(((x - 2) % 14) - 7) / 7)
        rect(im, x, top, x, 17, (96, 92, 146))
    rect(im, 17, 13, 24, 19, (74, 76, 120))
    rect(im, 20, 12, 21, 21, (232, 240, 248))
    put(im, 20, 22, (200, 220, 236)), put(im, 21, 22, (200, 220, 236))
    # the valley in shadow, gold trees catching the first light
    for y in range(18, 30):
        rect(im, 2, y, 29, y, mix((70, 96, 110), (56, 84, 84), (y - 18) / 12))
    for x, y in ((5, 19), (8, 20), (11, 19), (26, 20), (28, 19), (6, 25), (24, 26)):
        put(im, x, y, (226, 176, 64)), put(im, x + 1, y, (196, 144, 52)), put(im, x, y - 1, (240, 200, 96))
    # the lake, and the light on it
    rect(im, 8, 24, 23, 28, (70, 98, 150))
    rect(im, 10, 25, 21, 25, (240, 206, 132))
    rect(im, 13, 27, 18, 27, (196, 170, 128))
    return im


# ---- event_props.png ----

def puddle():
    """The puddle in the road after rain: grey-blue water, the sky in it, and the one bright stripe."""
    im = new(32, 16)
    for y in range(16):
        for x in range(32):
            dx, dy = (x - 15.5) / 14.5, (y - 8.5) / 5.5
            d = dx * dx + dy * dy
            if d <= 1.0:
                c = (84, 104, 134) if d > 0.72 else (112, 138, 168) if y < 8 else (100, 124, 154)
                put(im, x, y, c, 235)
    for x in range(7, 26):
        put(im, x, 9, (250, 242, 204), 245)
    for x in range(10, 22):
        put(im, x, 10, (214, 214, 196), 220)
    return im


def boards():
    """The planks he's prised off the door, dropped on the ground beside it."""
    im = new(32, 16)
    for n, (y0, slope) in enumerate(((5, 0), (8, 1), (11, 0))):
        for x in range(2 + n * 2, 28 - n):
            y = y0 + (x // 9 if slope else 0)
            put(im, x, y - 1, (178, 138, 90))
            put(im, x, y, (150, 112, 70))
            put(im, x, y + 1, (110, 78, 48))
        put(im, 5 + n * 3, y0, (96, 96, 104))
    return im


# ---- map_tiles.png ----

def vale_tile(sheet, index):
    cols = sheet.width // 16
    return sheet.crop((index % cols * 16, index // cols * 16, index % cols * 16 + 16, index // cols * 16 + 16))


def lum(c):
    return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def sign_repainted(sheet):
    """The Vale's sign with its board freshly painted (Vale green, the gold letters bright again).
    The posts, the lashings below the board and Fern's flowers stay as they were."""
    im = new(64, 48)
    for r, row in enumerate(((1381, 1382, 1383, 1384), (1413, 1414, 1415, 1416), (1445, 1446, 1447, 1448))):
        for c, i in enumerate(row):
            im.alpha_composite(vale_tile(sheet, i), (c * 16, r * 16))
    ramp = [(30, 58, 54), (42, 80, 74), (56, 104, 94), (72, 128, 114), (96, 156, 138)]
    px = im.load()
    for y in range(11, 34):                     # the board's rows (above them the posts' tops, below the posts)
        for x in range(64):
            p = px[x, y]
            if p[3] == 0:
                continue
            if p[0] > 200 and p[1] > 150:       # the letters
                px[x, y] = (255, 242, 176, 255) if lum(p) > 200 else (246, 196, 70, 255)
                continue
            if not (p[0] >= p[1] >= p[2]):      # browns only
                continue
            l = lum(p)
            k = 0 if l < 40 else 1 if l < 62 else 2 if l < 88 else 3 if l < 112 else 4
            px[x, y] = ramp[k] + (255,)
    return im


def door_boarded(sheet):
    """The Old Studio's door with planks nailed across it: an X and one across the middle."""
    im = new(16, 32)
    im.alpha_composite(vale_tile(sheet, 1029), (0, 0))
    im.alpha_composite(vale_tile(sheet, 1061), (0, 16))
    plank, light, shade, nail = (150, 112, 70), (178, 138, 90), (110, 78, 48), (90, 90, 98)

    def board(x0, y0, x1, y1):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(n + 1):
            x, y = round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n)
            put(im, x, y - 1, light), put(im, x, y, plank), put(im, x, y + 1, plank), put(im, x, y + 2, shade)

    board(0, 5, 11, 25)
    board(11, 5, 0, 25)
    for x in range(0, 13):
        put(im, x, 13, light), put(im, x, 14, plank), put(im, x, 15, plank), put(im, x, 16, shade)
    for x, y in ((1, 6), (10, 6), (1, 24), (10, 24), (1, 14), (11, 14)):
        put(im, x, y, nail)
    return im


def door_plain(sheet):
    im = new(16, 32)
    im.alpha_composite(vale_tile(sheet, 1029), (0, 0))
    im.alpha_composite(vale_tile(sheet, 1061), (0, 16))
    return im


def washing_line():
    """Bea's washing line: two T-posts, the line sagging between them, two sheets and a pillowcase
    pegged out. 4 x 2 tiles; the bottom row is the posts' feet and the sheets' hems (solid on the map)."""
    im = new(64, 32)
    post, post_light, post_dark = (150, 104, 62), (184, 134, 84), (104, 68, 40)
    for x0 in (2, 59):
        rect(im, x0, 3, x0 + 2, 29, post)
        rect(im, x0, 3, x0, 29, post_light)
        rect(im, x0 + 2, 3, x0 + 2, 29, post_dark)
        rect(im, x0 - 2, 3, x0 + 4, 4, post_dark)
        rect(im, x0 - 2, 2, x0 + 4, 2, post_light)
        for x in range(x0 - 2, x0 + 5):                         # its shadow on the ground
            put(im, x, 30, (40, 30, 20), 70)
        for x in range(x0 - 1, x0 + 4):
            put(im, x, 31, (40, 30, 20), 50)
    rope = (206, 198, 176)
    sag = lambda x: 5 + round(3 * (1 - ((x - 32) / 28) ** 2))
    for x in range(4, 60):
        put(im, x, sag(x), rope)
    white, white_shade, white_edge = (246, 244, 236), (220, 216, 204), (182, 176, 162)
    blue, blue_shade, blue_edge = (196, 218, 236), (166, 194, 220), (126, 156, 190)
    pink, pink_edge = (240, 196, 200), (196, 146, 154)

    def cloth(x0, x1, bottom, c, shade, edge, stripes=None):
        for x in range(x0, x1 + 1):
            top = sag(x) + 1
            for y in range(top, bottom + 1):
                k = edge if x in (x0, x1) or y == bottom else (shade if (x - x0) % 5 == 4 else c)
                if stripes and (y - top) % 6 in stripes and x not in (x0, x1):
                    k = stripes[(y - top) % 6]
                put(im, x, y, k)
        for x in (x0 + 1, x1 - 1):
            put(im, x, sag(x), (150, 112, 70))                   # pegs

    cloth(8, 27, 24, white, white_shade, white_edge)
    cloth(31, 47, 22, blue, blue_shade, blue_edge, {2: (232, 238, 244)})
    cloth(51, 56, 14, pink, pink, pink_edge)
    return im


# ---- hall_wall.png ----

def unfinished(state):
    """The Old Studio's painting of the Vale at sunset: the sky done, the valley floor bare canvas
    with its pencil lines. restore: cleaned, the colours bright; leave: as it was found, dull with
    dust; finish: the valley painted in."""
    im = new(32, 32)
    dusty = state == "leave"
    gilt = (GILT_DARK, GILT, GILT_LIGHT) if not dusty else ((120, 98, 62), (160, 136, 92), (182, 162, 120))
    frame(im, 0, 0, 31, 31, gilt[0], gilt[1], gilt[2])
    sky = [(108, 74, 140), (168, 92, 128), (222, 120, 100), (244, 160, 92), (250, 204, 120)]
    for y in range(2, 16):
        c = sky[min(len(sky) - 1, (y - 2) * len(sky) // 14)]
        if dusty:
            c = mix(c, (176, 166, 150), 0.42)
        rect(im, 2, y, 29, y, c)
    sun = (255, 230, 160) if not dusty else (226, 210, 170)
    for y in range(11, 16):
        for x in range(6, 14):
            if (x - 9.5) ** 2 + (y - 15.5) ** 2 <= 14:
                put(im, x, y, sun)
    # the valley floor: bare canvas, or painted in
    for y in range(16, 30):
        rect(im, 2, y, 29, y, CANVAS if state == "restore" else mix(CANVAS, (186, 176, 158), 0.35) if dusty else CANVAS)
    hills = [16 + round(2 * abs(((x - 2) % 12) - 6) / 6) for x in range(2, 30)]
    river = [(16 + round(6 * abs((y - 16) / 13 - 0.5)), y) for y in range(17, 30)]
    if state == "finish":
        for x, top in zip(range(2, 30), hills):
            for y in range(top, 30):
                put(im, x, y, mix((96, 128, 72), (64, 100, 60), (y - top) / 14))
        for x, y in river:
            put(im, x, y, (86, 124, 176)), put(im, x + 1, y, (120, 160, 206))
        for x, y in ((5, 20), (9, 22), (24, 21), (27, 24), (7, 26)):
            put(im, x, y, (226, 176, 64)), put(im, x, y - 1, (244, 206, 104))
    else:
        pencil = PENCIL if not dusty else (176, 166, 150)
        for x, top in zip(range(2, 30), hills):
            put(im, x, top, pencil)
        for x, y in river[::2]:
            put(im, x, y, pencil)
        for x, y in ((6, 22), (10, 24), (23, 22)):
            put(im, x, y, pencil), put(im, x, y - 1, pencil)
    if dusty:
        for x, y in ((4, 4), (17, 8), (25, 13), (12, 27), (22, 19)):
            put(im, x, y, (190, 182, 166))
    return im


def sketch(subject):
    """A page from his sketchbook, pinned in a narrow frame: the place you stopped on his walk."""
    im = new(16, 32)
    frame(im, 0, 0, 15, 31, WOOD_EDGE, WOOD, WOOD_LIGHT)
    rect(im, 2, 2, 13, 29, PAPER)
    c, l, f = CHARCOAL, CHARCOAL_LIGHT, CHARCOAL_FAINT
    if subject == "washing":
        # the line, the sheets on it, the light coming through them
        line(im, 2, 8, 13, 9, c)
        for x0, x1, bottom in ((3, 7, 20), (9, 12, 17)):
            for x in range(x0, x1 + 1):
                for y in range(9, bottom + 1):
                    if x in (x0, x1) or y == bottom:
                        put(im, x, y, l)
                    elif (x + y) % 4 == 0:
                        put(im, x, y, f)
        line(im, 2, 26, 13, 26, f)
    elif subject == "bus":
        # the empty road, the bus shelter, the long shadows
        rect(im, 3, 9, 8, 9, c)
        line(im, 3, 10, 3, 16, l), line(im, 8, 10, 8, 16, l)
        line(im, 2, 18, 13, 18, c)
        line(im, 2, 23, 13, 23, l)
        for x in range(4, 13, 3):
            line(im, x, 19, x - 3, 22, f)
    else:
        # the clay pit, Tobin's barrow and his drying rack
        for x in range(3, 13):
            for y in range(17, 24):
                dx, dy = (x - 7.5) / 5.0, (y - 20.0) / 3.5
                d = dx * dx + dy * dy
                if 0.7 <= d <= 1.0:
                    put(im, x, y, c)
                elif d < 0.7 and (x + y) % 3 == 0:
                    put(im, x, y, f)
        line(im, 9, 12, 13, 12, l), line(im, 10, 13, 10, 15, l), line(im, 13, 13, 13, 15, l)
        rect(im, 3, 9, 3, 15, l), rect(im, 6, 9, 6, 15, l), line(im, 3, 11, 6, 11, f), line(im, 3, 13, 6, 13, f)
    return im


def sheet(size, pieces):
    out = new(*size)
    for (x, y), im in pieces:
        out.alpha_composite(selout(im), (x, y))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    vale = Image.open(VALE_SHEET).convert("RGBA")
    sheet((64, 32), [((0, 0), puddle_sketch()), ((16, 0), travelling_easel()), ((32, 0), vale_at_dawn())]).save(
        os.path.join(OUT, "items.png"))
    sheet((64, 16), [((0, 0), puddle()), ((32, 0), boards())]).save(os.path.join(OUT, "event_props.png"))
    # (the map tiles: the sign's and the door's own pixels stay as they are, so no selout on those)
    tiles = new(160, 48)
    tiles.alpha_composite(sign_repainted(vale), (0, 0))
    tiles.alpha_composite(door_boarded(vale), (64, 0))
    tiles.alpha_composite(door_plain(vale), (80, 0))
    tiles.alpha_composite(selout(washing_line()), (96, 0))
    tiles.save(os.path.join(OUT, "map_tiles.png"))
    sheet((144, 32), [((0, 0), unfinished("restore")), ((32, 0), unfinished("finish")), ((64, 0), unfinished("leave")),
                      ((96, 0), sketch("washing")), ((112, 0), sketch("bus")), ((128, 0), sketch("claypit"))]).save(
        os.path.join(OUT, "hall_wall.png"))
    print("wrote", OUT)


if __name__ == "__main__":
    main()

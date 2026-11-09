"""The Gilded Vale's own fish: item icons and fish-tank sprites, drawn from scratch
(docs/superpowers/specs/2026-11-08-vale-fish-design.md; the user, 2026-11-08: "i dont want you
to just recolor existing fish").

Each fish is described, not traced: a spine (head to tail), a body width along it, fins as
small polygons, a pattern, and its own colours. The drawing follows the game's fish-icon
conventions: 16x16, the fish lying diagonally with its head to the upper right, the back
darker and the belly lighter, a one-pixel outline in the darkest shade of its own colour (not
black), and an eye near the head. The fish tanks get the same fish side-on (24x24, two frames:
swimming, and turning diagonally), laid out like the game's LooseSprites/AquariumFish.

    python tools/art/vale_fish_icons.py <out dir> [--pack]

Writes <out>/fish.png (each icon at 8x, 3x and 1x, on a dark and a light ground, with its tank
sprite) and, with --pack, the content pack's assets/ValeFish.png and assets/ValeFishAquarium.png.
"""
import math
import os
import sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PACK_ASSETS = os.path.join(HERE, "..", "..", "[CP] Gilded Vale (dev)", "assets")


def hexes(*cs):
    return [tuple(int(c[i:i + 2], 16) for i in (1, 3, 5)) for c in cs]


def one(c):
    return hexes(c)[0]


# Colours, darkest first: outline, dark (back), mid, light (belly), highlight; then extras.
PALETTES = {
    "loach": dict(body=hexes("#3d1a10", "#7a3220", "#a8492b", "#cc7148", "#e9a77a"),
                  fin=hexes("#7a3220", "#a8492b"), blotch=one("#5c2416"), barbel=one("#5c2416")),
    "trout": dict(body=hexes("#262a3a", "#4e5a72", "#8796ad", "#c4cfdc", "#f2f6fa"),
                  fin=hexes("#4e5a72", "#8796ad"), stripe=hexes("#a8405d", "#dd7891"), spot=one("#2e3446")),
    "sculpin": dict(body=hexes("#1f242b", "#3c4550", "#5e6a76", "#87929c", "#b4bdc3"),
                    fin=hexes("#3c4550", "#9aa6b0"), speck=one("#c9d0d3"), band=one("#2c333c")),
    "shiner": dict(body=hexes("#0b1757", "#1432a3", "#2462e2", "#3f9dff", "#bdf0ff"),
                   fin=hexes("#1432a3", "#2f7df0"), glint=one("#ffffff")),
    "carp": dict(body=hexes("#4a2806", "#8c5310", "#c98a1a", "#efbd34", "#fff0a0"),
                 fin=hexes("#b5521c", "#e07a2c"), scale=one("#a8701a"), barbel=one("#8c5310")),
}
EYE = one("#141018")
EYE_RING = one("#f4f0e6")


# ---- geometry --------------------------------------------------------------

def lerp(a, b, t):
    return a + (b - a) * t


def spine_point(spine, t):
    """Point and unit tangent at t (0 head .. 1 tail) along a polyline."""
    segs = [(spine[i], spine[i + 1]) for i in range(len(spine) - 1)]
    lens = [math.dist(a, b) for a, b in segs]
    d = t * sum(lens)
    for i, ((a, b), l) in enumerate(zip(segs, lens)):
        if d <= l or i == len(segs) - 1:
            u = min(1.0, d / l) if l else 0
            return (lerp(a[0], b[0], u), lerp(a[1], b[1], u)), ((b[0] - a[0]) / l, (b[1] - a[1]) / l)
        d -= l


def nearest_on_spine(spine, p, samples=160):
    """Distance to the spine, t there, and the side (+ the back, - the belly)."""
    best = None
    for i in range(samples + 1):
        t = i / samples
        q, (tx, ty) = spine_point(spine, t)
        d = math.dist(p, q)
        if best is None or d < best[0]:
            best = (d, t, (p[0] - q[0]) * ty - (p[1] - q[1]) * tx)
    return best


def in_poly(p, poly):
    x, y = p
    inside = False
    for i in range(len(poly)):
        (x1, y1), (x2, y2) = poly[i], poly[(i + 1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            inside = not inside
    return inside


# ---- drawing ---------------------------------------------------------------

class Canvas:
    def __init__(self, w, h):
        self.w, self.h, self.px = w, h, {}

    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[(x, y)] = c

    def get(self, x, y):
        return self.px.get((x, y))

    def outline(self, color):
        filled = set(self.px)
        for x in range(self.w):
            for y in range(self.h):
                if (x, y) not in filled and any((x + dx, y + dy) in filled for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    self.px[(x, y)] = color

    def image(self):
        img = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        for (x, y), c in self.px.items():
            img.putpixel((x, y), c + (255,))
        return img


def draw_fish(f, w, h, transform):
    """f: the fish in a 16x16 design space (head upper right); transform maps design
    coordinates onto this canvas (the icon is the identity; the tanks turn it)."""
    pal = PALETTES[f["palette"]]
    body = pal["body"]
    cv = Canvas(w, h)
    spine = [transform(p) for p in f["spine"]]
    for fin in f.get("fins", []):
        poly = [transform(p) for p in fin["poly"]]
        for x in range(w):
            for y in range(h):
                if in_poly((x + 0.5, y + 0.5), poly):
                    shade = fin.get("shade", 1)
                    # rays: stripes along the fin, "+" for a fin pointing to the lower left
                    # or upper right (x + y constant along a ray), "-" for one pointing to the
                    # upper left or lower right (x - y constant)
                    rays = fin.get("rays")
                    if rays and ((x + y) if rays == "+" else (x - y)) % 3 == 0:
                        shade = 0
                    cv.set(x, y, pal["fin"][shade])
    for x in range(w):
        for y in range(h):
            d, t, side = nearest_on_spine(spine, (x + 0.5, y + 0.5))
            half = f["width"](t) / 2
            if d > half:
                continue
            v = side / max(half, 0.01)       # +1 the back's edge .. -1 the belly's
            if v > 0.45:
                c = body[1]
            elif v > -0.1:
                c = body[2]
            elif v > -0.6 or not f.get("bright_belly"):
                c = body[3]
            else:
                c = body[4]
            cv.set(x, y, c)
            if "pattern" in f:
                f["pattern"](cv, x, y, t, v, pal)
    cv.outline(body[0])
    for detail in f.get("after_outline", []):
        detail(cv, transform, pal)
    ex, ey = (int(c) for c in transform(f["eye"]))
    if "eye_ring" in f:
        cv.set(ex, ey, EYE_RING)
        cv.set(ex + f["eye_ring"][0], ey + f["eye_ring"][1], EYE)
    else:
        cv.set(ex, ey, EYE)
    return cv.image()


def strokes(points, key):
    """Pixels drawn wherever they fall, even off the body (barbels)."""
    def apply(cv, transform, pal):
        for p in points:
            x, y = (int(c) for c in transform(p))
            cv.set(x, y, pal[key])
    return apply


def taper(head, mid, tail, peak=0.3):
    """A body width: `head` at the snout, `mid` at its widest (t = peak), `tail` at the tail root."""
    def w(t):
        if t < peak:
            return lerp(head, mid, (t / peak) ** 0.6)
        return lerp(mid, tail, ((t - peak) / (1 - peak)) ** 1.2)
    return w


# ---- patterns ----------------------------------------------------------------

def loach_pattern(cv, x, y, t, v, pal):
    # dark blotches along the side, like river mud
    if 0.12 < t < 0.92 and -0.35 < v < 0.6 and (x * 2 + y * 3) % 4 == 0:
        cv.set(x, y, pal["blotch"])


def trout_pattern(cv, x, y, t, v, pal):
    if 0.1 < t < 0.85 and -0.35 < v < 0.05:
        cv.set(x, y, pal["stripe"][1] if v > -0.15 else pal["stripe"][0])
    elif 0.1 < t < 0.95 and v > 0.4 and (x + y * 2) % 3 == 0:
        cv.set(x, y, pal["spot"])


def sculpin_pattern(cv, x, y, t, v, pal):
    if (x * 3 + y * 5) % 7 == 0 and v > -0.6:
        cv.set(x, y, pal["speck"])
    elif (0.35 < t < 0.45 or 0.62 < t < 0.7) and v > -0.3:
        cv.set(x, y, pal["band"])


def shiner_pattern(cv, x, y, t, v, pal):
    if 0.12 < t < 0.8 and -0.25 < v < 0.1:
        cv.set(x, y, pal["glint"] if (x + y) % 2 == 0 else pal["body"][4])


def carp_pattern(cv, x, y, t, v, pal):
    # big scales: a lattice of darker edges over the gold
    if 0.18 < t < 0.85 and -0.7 < v < 0.75 and (x + y) % 2 == 0 and (x - y) % 4 == 0:
        cv.set(x, y, pal["scale"])


# ---- the five fish -------------------------------------------------------------
# Design space: 16x16, y down. Head to the upper right, tail to the lower left.

FISH = {
    "loach": {
        "name": "Claybank Loach", "palette": "loach",
        # long and slim with an eel's wiggle, a round tail and whiskers at the mouth
        "spine": [(13.4, 2.4), (11.0, 4.8), (8.6, 7.4), (5.8, 9.4), (3.6, 12.2)],
        "width": taper(1.8, 3.6, 1.8, 0.22),
        "bright_belly": True,
        "pattern": loach_pattern,
        "fins": [
            {"poly": [(4.4, 11.2), (2.6, 10.6), (0.8, 12.0), (0.8, 14.6), (2.6, 15.2), (4.2, 13.6)], "shade": 1, "rays": "+"},  # round tail
            {"poly": [(6.6, 7.6), (6.6, 5.8), (8.4, 6.4)], "shade": 0},                                                         # dorsal, far back
            {"poly": [(10.6, 6.6), (11.2, 8.6), (12.2, 6.8)], "shade": 1},                                                      # pectoral
        ],
        "after_outline": [strokes([(15.2, 2.4), (15.0, 4.0), (14.4, 5.0), (12.6, 0.6)], "barbel")],
        "eye": (12.6, 2.6),
    },
    "trout": {
        "name": "Cascade Trout", "palette": "trout",
        # arched, mid-leap
        "spine": [(13.4, 1.8), (11.8, 5.2), (9.0, 8.2), (5.8, 10.2), (3.6, 11.6)],
        "width": taper(2.2, 5.0, 1.8, 0.32),
        "bright_belly": True,
        "pattern": trout_pattern,
        "fins": [
            {"poly": [(4.0, 10.8), (1.0, 9.6), (2.2, 12.2), (1.4, 14.8), (4.6, 13.0)], "shade": 0, "rays": "+"},  # tail
            {"poly": [(7.4, 6.0), (8.6, 4.2), (10.0, 5.6)], "shade": 0},                                          # dorsal
            {"poly": [(8.4, 11.2), (9.6, 13.0), (10.6, 10.8)], "shade": 1},                                       # pelvic
        ],
        "eye": (12.8, 3.0), "eye_ring": (0, 0),
    },
    "sculpin": {
        "name": "Falls Sculpin", "palette": "sculpin",
        # a broad flat head, a body that tapers fast, and big fanned pectoral fins
        "spine": [(12.8, 3.2), (10.2, 5.8), (7.0, 9.0), (3.8, 12.2)],
        "width": taper(3.6, 5.2, 1.4, 0.18),
        "pattern": sculpin_pattern,
        "fins": [
            {"poly": [(10.8, 6.8), (15.6, 7.6), (15.4, 10.2), (13.4, 11.8), (10.4, 8.0)], "shade": 1, "rays": "-"},    # pectoral fan, belly side
            {"poly": [(9.0, 5.4), (5.8, 3.2), (6.6, 0.6), (9.4, 0.2), (10.4, 4.4)], "shade": 1, "rays": "-"},         # pectoral fan, back side
            {"poly": [(4.4, 11.6), (2.0, 10.8), (0.8, 13.2), (2.4, 15.2), (4.4, 13.6)], "shade": 0, "rays": "+"},    # tail
            {"poly": [(5.4, 8.4), (5.6, 6.4), (7.6, 5.4), (8.0, 7.4)], "shade": 0},                                   # spiny dorsal
        ],
        "eye": (12.4, 2.6),
    },
    "shiner": {
        "name": "Lakeblue Shiner", "palette": "shiner",
        # small and slim, a big eye and a deep forked tail
        "spine": [(12.8, 3.4), (9.8, 6.4), (7.0, 9.0), (5.4, 10.6)],
        "width": taper(1.8, 3.8, 1.2, 0.3),
        "bright_belly": True,
        "pattern": shiner_pattern,
        "fins": [
            {"poly": [(5.8, 10.4), (4.6, 9.6), (1.6, 9.2), (3.8, 11.8)], "shade": 0},       # tail, upper lobe
            {"poly": [(5.8, 10.4), (6.4, 11.6), (6.8, 14.4), (4.2, 12.2)], "shade": 0},     # tail, lower lobe
            {"poly": [(7.2, 6.8), (7.6, 5.4), (9.0, 6.0)], "shade": 1},                     # dorsal
            {"poly": [(9.8, 8.4), (10.2, 10.0), (11.0, 8.4)], "shade": 1},                  # pelvic
        ],
        "eye": (11.8, 3.8), "eye_ring": (1, 0),
    },
    "carp": {
        "name": "Gilt Carp", "palette": "carp",
        # deep-bodied, with big scales and leaf-coloured fins
        "spine": [(13.2, 2.8), (10.4, 5.8), (7.4, 8.6), (4.8, 11.0)],
        "width": taper(3.4, 7.2, 2.6, 0.38),
        "bright_belly": True,
        "pattern": carp_pattern,
        "fins": [
            {"poly": [(1.0, 10.2), (4.0, 10.0), (5.6, 12.0), (4.4, 14.6), (2.0, 15.0), (2.6, 12.6)], "shade": 1, "rays": "+"},  # tail
            {"poly": [(5.0, 6.4), (6.4, 3.8), (8.6, 3.0), (9.6, 4.6)], "shade": 0},                                             # tall dorsal
            {"poly": [(8.8, 11.4), (10.0, 13.6), (11.2, 11.0)], "shade": 1},                                                    # pelvic
        ],
        "after_outline": [strokes([(15.0, 4.0), (14.6, 5.2)], "barbel")],
        "eye": (12.4, 3.2), "eye_ring": (1, 0),
    },
}
ORDER = ["loach", "trout", "sculpin", "shiner", "carp"]


def icon(key):
    return draw_fish(FISH[key], 16, 16, lambda p: p)


def tank_frames(key):
    """24x48: side-on (head right) above, the turning diagonal below."""
    out = Image.new("RGBA", (24, 48), (0, 0, 0, 0))
    c = s = math.sqrt(0.5)

    def side(p):   # the design turned 45 degrees clockwise about its centre, in a 24x24 cell
        x, y = p[0] - 8, p[1] - 8
        return (x * c - y * s + 12, x * s + y * c + 12)

    out.alpha_composite(draw_fish(FISH[key], 24, 24, side), (0, 0))
    out.alpha_composite(draw_fish(FISH[key], 24, 24, lambda p: (p[0] + 4, p[1] + 4)), (0, 24))
    return out


def preview(out):
    cell, pad = 128, 16
    W = len(ORDER) * (cell + pad) + pad
    H = cell + 100
    img = Image.new("RGBA", (W, 2 * H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for gi, ground in enumerate([(52, 46, 58, 255), (233, 211, 160, 255)]):
        oy = gi * H
        d.rectangle([0, oy, W, oy + H], fill=ground)
        ink = "white" if gi == 0 else "black"
        for i, key in enumerate(ORDER):
            ox = pad + i * (cell + pad)
            im = icon(key)
            d.text((ox, oy + 4), FISH[key]["name"], fill=ink)
            img.alpha_composite(im.resize((cell, cell), Image.NEAREST), (ox, oy + 18))
            img.alpha_composite(im.resize((48, 48), Image.NEAREST), (ox, oy + cell + 26))
            img.alpha_composite(im, (ox + 54, oy + cell + 42))
            img.alpha_composite(tank_frames(key).crop((0, 0, 24, 24)).resize((48, 48), Image.NEAREST), (ox + 76, oy + cell + 26))
    img.save(os.path.join(out, "fish.png"))


def write_pack():
    strip = Image.new("RGBA", (16 * len(ORDER), 16), (0, 0, 0, 0))
    tank = Image.new("RGBA", (480, 48), (0, 0, 0, 0))
    for i, key in enumerate(ORDER):
        strip.alpha_composite(icon(key), (16 * i, 0))
        tank.alpha_composite(tank_frames(key), (24 * i, 0))
    strip.save(os.path.join(PACK_ASSETS, "ValeFish.png"))
    tank.save(os.path.join(PACK_ASSETS, "ValeFishAquarium.png"))


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    preview(out)
    if "--pack" in sys.argv:
        write_pack()

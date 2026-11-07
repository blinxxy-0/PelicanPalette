"""A feature painted into the Gilded Vale's map with PixelLab's inpaint
(tools/art/vale_inpaint.py), made ready to lay like the amphitheatre (the user,
2026-11-06: "the way the ampitheatre blends into the terrain is an excellent example").

- cutout(): the painted grass outside the feature goes, so the map's own seasonal
  grass shows through. It is flood-filled from the picture's edges through green
  pixels only, so walls, gravel and paths stop it and the hedges and lawns inside
  are kept. (The model paints its own greens, not the map's palette, so a colour
  match can't find the grass: measured on the parterre, 6% exact.)
- walkable(): per 16 px tile, whether the farmer can walk there: mostly path
  (gravel, paving, bare earth) or open lawn is walkable, anything mostly wall, hedge,
  statue or water is solid.

Usage (preview): python tools/art/vale_feature.py <piece.png> <out preview.png>
"""
import sys
from PIL import Image, ImageDraw


def is_grass(c):
    r, g, b, a = c
    return a and g > r + 12 and g > b + 12


def cutout(im, light_only=True, lawn=False):
    """The piece with the grass round its outside made transparent; with lawn, every
    stretch of plain light lawn inside it too (so the map's own seasonal grass shows
    through), keeping the darker hedges and bushes."""
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    lum = lambda c: 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
    ok = lambda c: is_grass(c) and (not light_only or lum(c) > 70)
    seen = set()
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not ok(px[x, y]):
            continue
        seen.add((x, y))
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    for x, y in seen:
        px[x, y] = (0, 0, 0, 0)
    if lawn:
        for y in range(h):
            for x in range(w):
                c = px[x, y]
                if is_grass(c) and lum(c) > 85:
                    px[x, y] = (0, 0, 0, 0)
    # flecks left on the cut edge (grass shadow, outline the fill stopped at)
    for _ in range(2):
        drop = []
        for y in range(h):
            for x in range(w):
                c = px[x, y]
                if not c[3]:
                    continue
                n = [(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
                bare = [q for q in n if not (0 <= q[0] < w and 0 <= q[1] < h) or not px[q][3]]
                if len(bare) >= 3 or (bare and is_grass(c) and lum(c) > 60):
                    drop.append((x, y))
        for q in drop:
            px[q] = (0, 0, 0, 0)
    return im


def kind(c):
    """What a pixel is, roughly: path (pale, warm or grey ground), green, or other."""
    r, g, b, a = c
    if not a:
        return "empty"
    if is_grass(c):
        return "green"
    lum = 0.3 * r + 0.59 * g + 0.11 * b
    if lum > 150 and abs(r - g) < 40 and b < g:          # pale gravel, sand, flagstones
        return "path"
    if r > 140 and r > g + 10 and g > b + 25 and lum > 105:    # the game's orange dirt and sand
        return "path"
    return "other"


def walkable(im, path_share=0.45):
    """{(tx, ty): True/False} for every tile with art in it."""
    im = im.convert("RGBA")
    out = {}
    for ty in range(im.height // 16):
        for tx in range(im.width // 16):
            t = im.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16))
            ks = [kind(c) for c in t.get_flattened_data()]
            n = len(ks)
            empty = ks.count("empty") / n
            if empty > 0.98:
                continue
            # solid only where the tile is mostly object (wall, hedge, bench, planter):
            # an object's edge or corner poking into a tile doesn't block it (the user,
            # 2026-11-07: stopped short of the nursery by the cold frames' edges)
            # greenery blocks only where it's dense dark hedge; tufts of long grass walk
            dark = sum(1 for c in t.get_flattened_data() if c[3] and is_grass(c) and 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2] < 80) / n
            obj = max(ks.count("other") / n, dark * 0.9) if dark < 0.55 else 1.0
            # a wall or fence running straight through a tile blocks it however thin it is
            # (the parterre's side walls fill under half their tiles)
            cs = list(t.get_flattened_data())
            stone = lambda c: c[3] and abs(c[0] - c[1]) < 22 and abs(c[1] - c[2]) < 28 and 50 < c[0] < 210
            line = any(sum(1 for y in range(16) if stone(cs[y * 16 + x])) >= 14 for x in range(16)) or                 any(sum(1 for x in range(16) if stone(cs[y * 16 + x])) >= 14 for y in range(16))
            out[(tx, ty)] = obj < 0.5 and not line
    return out


def preview(im, walk, path):
    big = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
    bg = Image.new("RGBA", big.size, (60, 90, 60, 255))
    bg.alpha_composite(big)
    d = ImageDraw.Draw(bg, "RGBA")
    for (tx, ty), ok in walk.items():
        if not ok:
            d.rectangle((tx * 48, ty * 48, tx * 48 + 47, ty * 48 + 47), outline=(255, 40, 40, 200), width=2)
    bg.save(path)


if __name__ == "__main__":
    im = cutout(Image.open(sys.argv[1]))
    preview(im, walkable(im), sys.argv[2])


def cutout_diff(piece, before, fray=20, seed=7):
    """Only what the painting changed: pixels close to the bare map's render at the
    same place go (so grass, sand and water stay the map's own, seasonal and
    animated), and water the painting recoloured goes too. Painted ground that runs
    to the frame's edge is frayed back by up to `fray` px, so it can't end in a
    straight line where the painting stopped. piece and before are the same box."""
    import random
    rnd = random.Random(seed)
    im = piece.convert("RGBA")
    px, bp = im.load(), before.convert("RGB").load()
    w, h = im.size
    near = lambda a, b: sum((a[i] - b[i]) ** 2 for i in range(3)) < 900
    water = lambda c: c[2] > c[0] + 40 and c[2] > c[1] - 5
    import math
    lum = lambda c: 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]
    # only light ground frays (lawn, dirt, sand), never hedges or bushes
    ground = lambda c: (is_grass(c) and lum(c) > 85) or kind(c) == "path" or (c[0] > 170 and c[0] > c[1] + 20 and c[1] > c[2] + 30)
    phase = [rnd.random() * 6.3 for _ in range(4)]

    def frayed(x, y):
        """How far in from the nearest frame edge the ground is cut here: a smooth wave
        along that edge, like a worn path's end, not specks."""
        d = [(x, y, 0), (w - 1 - x, y, 1), (y, x, 2), (h - 1 - y, x, 3)]
        dist, along, k = min(d)
        wave = 0.5 + 0.25 * math.sin(along / 7 + phase[k]) + 0.25 * math.sin(along / 3.1 + phase[k] * 2)
        return dist < 4 + (fray - 4) * wave
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if not c[3]:
                continue
            b = bp[x, y]
            sand = lambda q: q[0] > 150 and q[0] > q[1] + 15 and q[1] > q[2] + 30
            # a painted shade of what was there (a shadow square, a paler patch): every
            # channel scaled by about the same amount
            ratios = [(c[i] + 8) / (b[i] + 8) for i in range(3)]
            shade = max(ratios) - min(ratios) < 0.16 and 0.45 < sum(ratios) / 3 < 1.4
            # painted lawn goes too, so the map's own grass (and its seasons) shows
            lawn = is_grass(c) and lum(c) > 85 and is_grass(tuple(b) + (255,))
            if near(c[:3], b) or (water(c) and water(b)) or (sand(c) and sand(b)) or shade or lawn:
                px[x, y] = (0, 0, 0, 0)
            elif ground(c) and frayed(x, y):
                px[x, y] = (0, 0, 0, 0)
    # lone flecks
    for _ in range(2):
        drop = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] and
                sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3]) <= 1]
        for q in drop:
            px[q] = (0, 0, 0, 0)
    return im


def seasonal(im, table, season, tolerance=18):
    """The piece in another season: each pixel near a colour the game recolours that
    season (tools/art/vale_season_colours.py) takes that colour's seasonal colour,
    keeping its own offset from it, so painted grass, sand, dirt and leaves follow the
    map's own (the user, 2026-11-06: sand left spring yellow in fall). Stone, wood,
    glass and flowers aren't in the table and stay as they are."""
    pairs = [(tuple(int(k[i:i + 2], 16) for i in (0, 2, 4)), tuple(int(v[i:i + 2], 16) for i in (0, 2, 4)))
             for k, v in table[season].items()]
    out = im.copy()
    px = out.load()
    w, h = out.size
    # Only ground and greenery: connected stretches of grass, hedge, sand or dirt big
    # enough to be ground, not the odd brown or green pixel of a pot, plank or barrel
    # (recolouring those speckled the winter version with snow).
    def groundish(c):
        r, g, b, a = c
        # bare ground only: dirt, sand, gravel. Never greenery: hedges, potted plants
        # and seedlings under glass changing colour looked wrong (the user, 2026-11-07)
        return a and not is_grass(c) and ((r > 140 and r > g + 15 and g > b + 25) or kind(c) == "path")
    region = set()
    seen = set()
    for sy in range(h):
        for sx in range(w):
            if (sx, sy) in seen or not groundish(px[sx, sy]):
                continue
            comp, stack = [], [(sx, sy)]
            seen.add((sx, sy))
            while stack:
                x, y = stack.pop()
                comp.append((x, y))
                for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= q[0] < w and 0 <= q[1] < h and q not in seen and groundish(px[q]):
                        seen.add(q)
                        stack.append(q)
            if len(comp) >= 60:
                region.update(comp)
    cache = {}
    for (x, y) in region:
            c = px[x, y]
            if not c[3]:
                continue
            rgb = c[:3]
            if rgb not in cache:
                best, d = None, tolerance * tolerance * 3
                for a, b in pairs:
                    e = (a[0] - rgb[0]) ** 2 + (a[1] - rgb[1]) ** 2 + (a[2] - rgb[2]) ** 2
                    if e < d:
                        best, d = (a, b), e
                if best:
                    a, b = best
                    cache[rgb] = tuple(max(0, min(255, b[i] + rgb[i] - a[i])) for i in range(3))
                else:
                    cache[rgb] = rgb
            px[x, y] = cache[rgb] + (255,)
    return out

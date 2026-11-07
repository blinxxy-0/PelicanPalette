"""The Gilded Vale's own road sheet: the bus road in from the west, ending in a
turning circle where Pam's bus stops (the user, 2026-11-06: "a more formal road",
then "Yes, build our sheet", with no new mod dependencies).

The game's only road is the Bus Stop's, straight and east-west: a top edge with a
tall curb (1029), plain asphalt (1055), the dashed centre (1057) and a bottom edge
with a thin lip (1054); outdoors sheet, Town numbering. A curve needs pieces the game
doesn't have, so they're drawn here, pixel by pixel, from those same tiles:

- The road is a shape: the straight road joined to a circle with a smooth fillet. Each
  pixel's signed distance from its edge, and the edge's direction, are worked out.
- Inside is asphalt. Along the edge, the curb is copied from the game's own edge
  tiles, layer by layer outward from the asphalt: the dark seam, the lip, the lit top,
  the face and the grass's shade. Edges facing up get the tall curb (as 1029), edges
  facing down or sideways the thin lip (as 1054), in between something between,
  since the game's view looks down from the south.
- Outside is the season's plain grass tile (176), so the road meets the map's grass
  without a seam.
- Every colour is sampled from the season's own tiles at the same pixel, so summer,
  fall and winter come from their sheets with no extra drawing.

The map is cut into 16 px tiles; tiles that are plain grass are left to the terrain
painter, the rest are packed (deduplicated across all four seasons) onto
<season>_GildedValeRoad.png, and the layout is written as JSON for build_vale.py.

Usage: python tools/art/vale_road.py <spring dir> <summer dir> <fall dir> <winter dir> <assets dir> <layout.json>
  each season dir holds that season's vanilla sheets named spring_*.png (as exported).
"""
import json, math, os, sys
from PIL import Image

# Geometry, in map pixels (16 per tile). The straight road's edge tiles are rows
# TOP_ROW (top curb) and BOTTOM_ROW (bottom lip), as the Bus Stop's rows 6 and 10.
TOP_ROW, BOTTOM_ROW = 5, 9
Y_TOP = (TOP_ROW + 1) * 16          # first asphalt row (the top tile's last row is the dark seam)
Y_BOT = BOTTOM_ROW * 16 + 6         # first row below the asphalt (the bottom tile's seam)
DASH_Y = 7 * 16 + 7                 # the centre line (row 7, pixel 7, as in 1057)
CIRCLE = (14 * 16, (Y_TOP + Y_BOT) / 2, 54)    # the turning circle: centre x, y, radius
FILLET = 14                         # how softly the road widens into the circle
DASH_END = CIRCLE[0] - CIRCLE[2] - 8            # the centre line stops before the circle
REGION = (0, 2, 19, 13)             # tiles x0, y0, x1, y1 (exclusive) the road can touch
SHEET_COLS = 8


def tiles(path):
    im = Image.open(path).convert("RGBA")

    def get(gid):
        i = gid - 1
        return im.crop(((i % 25) * 16, (i // 25) * 16, (i % 25) * 16 + 16, (i // 25) * 16 + 16)).load()
    return get


def sdf(x, y):
    """Signed distance from the road's edge, outward positive."""
    cx, cy, r = CIRCLE
    d_rect = max(Y_TOP - y, y - Y_BOT, x - cx)
    d_circ = math.hypot(x - cx, y - cy) - r
    h = min(1.0, max(0.0, 0.5 + 0.5 * (d_circ - d_rect) / FILLET))
    return d_circ * (1 - h) + d_rect * h - FILLET * h * (1 - h)


def paint(season_dir):
    t = tiles(os.path.join(season_dir, "spring_outdoorsTileSheet.png"))
    top, bot, asphalt, dash, grass = t(1029), t(1054), t(1055), t(1057), t(176)
    x0, y0, x1, y1 = REGION
    im = Image.new("RGBA", ((x1 - x0) * 16, (y1 - y0) * 16))
    px = im.load()
    for py in range(y0 * 16, y1 * 16):
        for pxl in range(x0 * 16, x1 * 16):
            x, y = pxl + 0.5, py + 0.5
            s = sdf(x, y)
            u, v = pxl % 16, py % 16
            if s < 0:
                c = dash[u, 7] if py == DASH_Y and pxl < DASH_END else asphalt[u, v]
            else:
                gx = sdf(x + 0.5, y) - sdf(x - 0.5, y)
                gy = sdf(x, y + 0.5) - sdf(x, y - 0.5)
                n = math.hypot(gx, gy) or 1
                up = max(0.0, -gy / n)              # 1 where the edge faces north (the tall curb)
                light = round(2 * up)               # the curb's lit top: 2 px facing north, 0 else
                face = 1 if up > 0.35 else 0
                shade = round(2 * up)
                k = int(s)                          # which layer outward from the asphalt
                if k == 0:
                    c = top[u, 15]                  # the dark seam
                elif k == 1:
                    c = top[u, 14] if up > 0.5 else bot[u, 7]   # the lip
                elif k < 2 + light:
                    c = top[u, 13 - (k - 2)]         # the lit top, rows 13 then 12
                elif k < 2 + light + face:
                    c = top[u, 11]
                elif k < 2 + light + face + shade:
                    c = top[u, 10 - (k - 2 - light - face)]   # the grass's shade, rows 10 then 9
                else:
                    c = grass[u, v]
            px[pxl - x0 * 16, py - y0 * 16] = c
    return im, grass


def main():
    seasons = dict(zip(("spring", "summer", "fall", "winter"), sys.argv[1:5]))
    assets, layout_path = sys.argv[5], sys.argv[6]
    painted = {s: paint(d) for s, d in seasons.items()}
    x0, y0, x1, y1 = REGION
    index, order, layout = {}, [], {}
    for ty in range(y0, y1):
        for tx in range(x0, x1):
            box = ((tx - x0) * 16, (ty - y0) * 16, (tx - x0) * 16 + 16, (ty - y0) * 16 + 16)
            crops = {s: im.crop(box) for s, (im, _) in painted.items()}
            spring_grass = painted["spring"][1]
            if all(crops["spring"].getpixel((u, v)) == spring_grass[u, v] for u in range(16) for v in range(16)):
                continue        # plain grass: the terrain painter's
            key = tuple(c.tobytes() for c in crops.values())
            if key not in index:
                index[key] = len(order)
                order.append(crops)
            layout[f"{tx},{ty}"] = index[key]
    rows = -(-len(order) // SHEET_COLS)
    for s in seasons:
        sheet = Image.new("RGBA", (SHEET_COLS * 16, rows * 16))
        for i, crops in enumerate(order):
            sheet.paste(crops[s], ((i % SHEET_COLS) * 16, (i // SHEET_COLS) * 16))
        sheet.save(os.path.join(assets, f"{s}_GildedValeRoad.png"))
    json.dump({"columns": SHEET_COLS, "count": rows * SHEET_COLS, "height": rows * 16, "tiles": layout},
              open(layout_path, "w"), indent=1)
    print(len(order), "unique tiles,", len(layout), "cells")


if __name__ == "__main__":
    main()

"""Builds the Vale House (Bea's inn) as one floor plan, the way the game's own
houses are built (docs/interior_design.md; the plan the user approved on
2026-11-07: art/gilded_vale/interiors/vale_house/plan_v1.png).

Every wall and outline piece is the game's, read off Haley and Emily's house
(HaleyHouse, its living room and the doorway down from Haley's room), in its
tile numbering: townInterior from 1, townInterior_2 from 2177. A room is a
rectangle of floor; the generator draws round it:
  - the outline on Front: the top border with chamfered corners (2, 3, 4), the
    side edges (33/65 left, 37/69 right), the bottom border (161-168);
  - the back wall's face: three rows of wallpaper on Back (ours,
    ValeWallpapers.png), its wainscot on Buildings (81, 113), and its ends
    turning at 45 degrees (42/80/112/144 left, 44/82/114/146 right);
  - the floor on Back, its first row the shaded one under the wall.
Rooms stacked one above another join through a doorway (the room above's
bottom border opens, 163/164, and the room below has the game's door frame,
195/57/194, 827/89/827, 156/155, 188/121/187). The front door is a notch in
the bottom border with a mat (97/197/131, 23).

The plan's west snug, tea room and kitchen are one room, zoned by floor and
rugs (Haley's living room joins its side room the same way only at great
length; the snug's fireplace and the kitchen keep their places).

Usage: build_vale_house.py <exports dir> "[CP] Gilded Vale (dev)/assets"
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from build_shop_interior import read
from vanillafy import selout
import build_margot_room as bmr
import build_vale_interiors as bvi

W, H = 31, 32
T2 = 2177                                   # townInterior_2's first gid
VALE, INN_DOOR = "blinxxy.MuseArt_GildedVale", (28, 19)
# Rooms: floor columns a..b, top border row t, last floor row f (its bottom border row)
ROOMS = {
    "guest":   dict(a=2, b=10, t=0, f=8),
    "artists": dict(a=13, b=18, t=0, f=8),
    "bea":     dict(a=21, b=28, t=0, f=8),
    "hall":    dict(a=2, b=28, t=9, f=15),
    "tea":     dict(a=1, b=29, t=16, f=29),
}
DOORS = [("guest", "hall", 6), ("artists", "hall", 15), ("bea", "hall", 25), ("hall", "tea", 15)]
FRONT_DOOR = ("tea", 15)
KITCHEN = (23, 29)                          # the tea room's columns with the kitchen's tiled floor
# Furniture: name -> (x, y of its top-left tile, solid bottom rows); "#n" repeats a piece.
# Wall-backed pieces stand with their base on the first floor row and their top over the wall.
PIECES = {
    # the snug, west: the fireplace in the back wall, the guest-book case and the piano either side
    "guestbook_case": ((2, 18), 1), "piano": ((7, 18), 1),
    "armchair": ((2, 23), 1), "armchair#2": ((7, 23), 1),
    # the tea room: wall shelves and sketches; the counter free-standing, Bea's side behind it
    "sketch_wall": ((11, 17), 0), "teapot_shelf": ((16, 17), 0),
    "tea_counter": ((14, 22), 2),
    "tea_table": ((11, 25), 1), "tea_table#2": ((18, 26), 1), "tea_table#3": ((19, 22), 1), "tea_table#4": ((11, 21), 1),
    # the kitchen, east
    "china_dresser": ((27, 18), 1), "k_counter": ((23, 18), 1), "k_stove": ((25, 18), 1), "k_shelf": ((23, 17), 0),
    # the guest room
    "wardrobe": ((3, 2), 1), "easel": ((5, 2), 1), "nightstand": ((7, 4), 1), "writing_desk": ((3, 5), 1),
    "sketch_wall#2": ((8, 1), 0),
    # the artists' room (locked till Bea's 10 hearts)
    "easel#2": ((14, 2), 1), "sketch_wall#3": ((16, 1), 0),
    # Bea's room
    # the hall: pictures the artists left, between the doors
    "sketch_wall#5": ((10, 10), 0), "sketch_wall#6": ((19, 10), 0),
    "wardrobe#2": ((22, 2), 1), "nightstand#2": ((25, 4), 1), "writing_desk#2": ((22, 6), 1), "sketch_wall#4": ((24, 1), 0),
}
BEDS = {"guest": (8, 4), "artists": (16, 4), "bea": (26, 4)}   # the Saloon's spare bed, top-left
SLEEP_IN = "guest"
RUGS = [(2, 22, 7, 4), (10, 24, 5, 4), (17, 25, 5, 4), (3, 13, 25, 2)]   # (the last: the hall's runner)         # Haley's red rug, 9-sliced: x, y, w, h
FIREPLACE_X = 4
LIGHTS = [(5, 4), (15, 4), (24, 4), (8, 12), (22, 12), (5, 23), (15, 24), (25, 23)]
EXTRA_ART = {"k_counter": "art/margot/room/picks/stardew-margot-kitchen/counter_9.png",
             "k_stove": "art/margot/room/picks/stardew-margot-kitchen/stove_17.png",
             "k_shelf": "art/margot/room/picks/stardew-margot-kitchen/jar_shelf_44.png"}
SHEET = "ValeHouseInterior.png"


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    g = {l: [[0] * W for _ in range(H)] for l in bmr.LAYERS}
    for y in range(H):
        for x in range(W):
            g["Back"][y][x] = g["Buildings"][y][x] = 1            # the void round the house

    # the furniture sheet (the inn's PixelLab pieces, and Margot's kitchen pieces)
    bvi.PICKS.update({k: os.path.relpath(v, bvi.ART)[:-4].replace("\\", "/") for k, v in EXTRA_ART.items()})
    sheet, slots = bvi.pack(bvi.ART)
    furn_first = T2 + 1312
    wall_first = furn_first + (sheet.width // 16) * (sheet.height // 16)
    sheet.save(os.path.join(assets, SHEET))
    paper = Image.open(os.path.join(bvi.ART, bvi.WALLPAPERS[0] + ".png")).convert("RGBA").crop((0, 0, 16, 48))
    paper.save(os.path.join(assets, "ValeWallpapers.png"))

    def floor(x, y, r):
        if r["t"] == ROOMS["tea"]["t"] and KITCHEN[0] <= x <= KITCHEN[1]:
            return 295 if y == r["t"] + 4 else 263               # Sam's kitchen tiles
        if y == r["t"] + 4:
            return 574 if x == r["a"] else (575 if x % 2 else 576)   # the shaded row under the wall
        return (607 if x % 2 else 608) if y % 2 else (639 if x % 2 else 640)

    for name, r in ROOMS.items():
        a, b, t, f = r["a"], r["b"], r["t"], r["f"]
        for x in range(a - 1, b + 2):
            for y in range(t, f + 1):
                g["Back"][y][x] = 1
                g["Buildings"][y][x] = 1 if x in (a - 1, b + 1) or y == t else 0
        # the top border, chamfered at the corners
        for x in range(a, b + 1):
            g["Front"][t][x] = 2 if x == a else 4 if x == b else (2122 if x % 5 == 3 else 3)
        # the back wall: three rows of wallpaper on Back, the wainscot on Buildings, 45-degree ends
        for k in range(3):
            for x in range(a, b + 1):
                g["Back"][t + 1 + k][x] = wall_first + k
        for x in range(a + 1, b):
            g["Buildings"][t + 2][x] = 81
            g["Buildings"][t + 3][x] = 113
        g["Front"][t + 1][a - 1], g["Front"][t + 1][a], g["Front"][t + 1][b], g["Front"][t + 1][b + 1] = 33, 42, 44, 37
        for k, (lg, rg) in enumerate(((80, 82), (112, 114), (144, 146))):
            g["Buildings"][t + 2 + k][a], g["Buildings"][t + 2 + k][b] = lg, rg
        # the floor, and the side edges down to the bottom border
        for y in range(t + 4, f + 1):
            for x in range(a, b + 1):
                g["Back"][y][x] = floor(x, y, r)
        for y in range(t + 2, f):
            g["Front"][y][a - 1] = 2153 if (y - t) % 4 == 3 else 65
            g["Front"][y][b + 1] = 2154 if (y - t) % 4 == 3 else 69
            g["Buildings"][y][a - 1], g["Buildings"][y][b + 1] = 65, 69
        # the bottom border, drawn over the last floor row (which you can walk on)
        g["Front"][f][a - 1], g["Front"][f][a], g["Front"][f][b], g["Front"][f][b + 1] = 161, 162, 167, 168
        for x in range(a + 1, b):
            g["Front"][f][x] = 2121 if x % 6 == 2 else 166

    # doorways between stacked rooms (Haley's, down from her room into the living room)
    for upper, lower, d in DOORS:
        fu, tl = ROOMS[upper]["f"], ROOMS[lower]["t"]
        g["Front"][fu][d - 1], g["Front"][fu][d], g["Front"][fu][d + 1] = 163, 0, 164
        g["Front"][tl][d - 1], g["Front"][tl][d], g["Front"][tl][d + 1] = 195, 57, 194
        g["Front"][tl + 1][d - 1], g["Front"][tl + 1][d], g["Front"][tl + 1][d + 1] = 827, 89, 827
        g["Buildings"][tl][d] = 0
        g["Buildings"][tl + 1][d - 1], g["Buildings"][tl + 1][d + 1] = 156, 155
        g["Buildings"][tl + 2][d - 1], g["Buildings"][tl + 2][d], g["Buildings"][tl + 2][d + 1] = 188, 121, 187
        g["Buildings"][tl + 3][d - 1], g["Buildings"][tl + 3][d], g["Buildings"][tl + 3][d + 1] = 220, 0, 219
        for y in range(tl, tl + 4):
            g["Back"][y][d] = 264 if y % 2 else 232
            if y < tl + 3:
                g["Back"][y][d - 1] = g["Back"][y][d + 1] = wall_first + (y - tl - 1 if y > tl else 0)
        g["Back"][tl + 3][d] = 671

    # the front door: a notch in the bottom border, a mat (Haley's)
    room, d = FRONT_DOOR
    f = ROOMS[room]["f"]
    g["Front"][f][d - 1], g["Front"][f][d], g["Front"][f][d + 1] = 163, 0, 164
    g["Front"][f + 1][d - 1], g["Front"][f + 1][d], g["Front"][f + 1][d + 1] = 97, 197, 131
    g["Back"][f + 1][d], g["Buildings"][f + 1][d] = 23, 0
    g["Back"][f + 2][d] = g["Buildings"][f + 2][d] = 0          # the warp tile, open (Haley's is too)

    # the fireplace, built into the snug's back wall (the game's brick one)
    tt = ROOMS["tea"]["t"]
    for j, pair in enumerate(((667, 668), (699, 700), (731, 732))):
        g["Front"][tt + 1 + j][FIREPLACE_X], g["Front"][tt + 1 + j][FIREPLACE_X + 1] = pair
    for j, pair in enumerate(((763, 764), (795, 796))):
        g["Buildings"][tt + 4 + j][FIREPLACE_X], g["Buildings"][tt + 4 + j][FIREPLACE_X + 1] = pair

    # rugs (Haley's red one, on Back2)
    for x0, y0, w, h in RUGS:
        for j in range(h):
            row = 2761 if j == 0 else 2825 if j == h - 1 else 2793
            for i in range(w):
                g["Back2"][y0 + j][x0 + i] = row + (0 if i == 0 else 3 if i == w - 1 else (1 if i < w - 2 else 2))

    # furniture
    for key, ((px, py), solid) in PIECES.items():
        first, cols, rows = slots[key.split("#")[0]]
        for j in range(rows):
            for i in range(cols):
                gid = furn_first + first + j * bvi.SHEET_COLS + i
                x, y = px + i, py + j
                if solid == -2:
                    g["Back2"][y][x] = gid
                elif j >= rows - solid:
                    g["Buildings"][y][x] = gid
                else:
                    g["Front2" if g["Front"][y][x] else "Front"][y][x] = gid
    saloon = read(os.path.join(exports, "Saloon.tmx"))
    sets, layers = saloon[0], saloon[1]
    for room, (bx, by) in BEDS.items():
        for layer in ("Buildings", "Front"):
            lw, data = layers[layer]
            for j in range(4):
                for i in range(2):
                    gid = data[(bmr.BED_FROM[1] + j) * lw + bmr.BED_FROM[0] + i]
                    if gid:
                        g[layer][by + j][bx + i] = gid      # (the Saloon uses this same numbering)

    bx, by = BEDS[SLEEP_IN]
    tile_props = [("Back", bx + i, by + j, "TouchAction", "Sleep") for i in range(2) for j in (2, 3)]
    # The doorways are doors, as Haley's is ("Action Door Haley" on its foot, 121): the game
    # opens them as you walk up. (Bea's own room and the artists' room get their owners'
    # names, locking them, once the residents exist.)
    for upper, lower, d_ in DOORS:
        tile_props.append(("Buildings", d_, ROOMS[lower]["t"] + 2, "Action", "Door"))
    props = {"Warp": f"{d} {f + 2} {VALE} {INN_DOOR[0]} {INN_DOOR[1] + 1}",
             "Light": " ".join(f"{x} {y} {bmr.LIGHT_TEXTURE}" for x, y in LIGHTS),
             "AllowWakeUpWithoutBed": "T"}
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("indoor2", T2, "townInterior_2", 512, 656),
                ("z_blinxxy.MuseArt_ValeHouseInterior", furn_first, SHEET, sheet.width, sheet.height),
                ("z_blinxxy.MuseArt_ValeWallpapers", wall_first, "ValeWallpapers.png", 16, 48)]
    write_raw(os.path.join(assets, "ValeHouse.tmx"), g, props, tilesets, tile_props)
    for old in ("ValeHouseGuest.tmx", "ValeHouseUpstairs.tmx"):
        if os.path.exists(os.path.join(assets, old)):
            os.remove(os.path.join(assets, old))
    print("wrote ValeHouse.tmx", f"{W}x{H}")


def write_raw(path, g, props, tilesets, tile_props):
    from xml.sax.saxutils import quoteattr
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{2 * len(bmr.LAYERS) + 2}" nextobjectid="{len(tile_props) + 1}">',
             '  <properties>'] + [f'    <property name="{k}" value={quoteattr(v)} />' for k, v in props.items()] + ['  </properties>']
    for name, first, image, iw, ih in tilesets:
        lines += [f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(iw // 16) * (ih // 16)}" columns="{iw // 16}">',
                  f'    <image source="{image}" width="{iw}" height="{ih}" />', '  </tileset>']
    for i, layer in enumerate(bmr.LAYERS):
        lines += [f'  <layer id="{i + 1}" name="{layer}" width="{W}" height="{H}">', '    <data encoding="csv">',
                  ",\n".join(",".join(str(v) for v in row) for row in g[layer]), '    </data>', '  </layer>']
    n = 0
    for li, layer in enumerate(bmr.LAYERS):
        mine = [p for p in tile_props if p[0] == layer]
        if not mine:
            continue
        lines.append(f'  <objectgroup id="{len(bmr.LAYERS) + 1 + li}" name="{layer}">')
        for _, x, y, k, v in mine:
            n += 1
            lines += [f'    <object id="{n}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">',
                      f'      <properties><property name="{k}" type="string" value="{v}" /></properties>', '    </object>']
        lines.append('  </objectgroup>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()

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
# (Our own PixelLab pieces, for the things the game has none of.)
PIECES = {
    # the tea room: the guest book by the stairs' doorway, Bea's china, her counter against
    # the back wall by the kitchen with the teapots on a shelf above it (the user, 2026-11-07:
    # the counter "up towards the right", off the carpet and out of the middle). The counter
    # stands a row out from the wall so Bea serves from behind it, coming in from the
    # kitchen (the user, 2026-11-08: she stood beside it in the kitchen, with no way behind)
    "guestbook_case": ((11, 18), 1),
    "china_dresser": ((17, 18), 1), "teapot_shelf": ((19, 17), 0), "tea_counter": ((19, 20), 2),
    # the tea tables, every one on a rug (the user: "some on carpet and some not doesn't make sense")
    "tea_table": ((11, 22), 1), "tea_table#2": ((11, 25), 1), "tea_table#3": ((18, 24), 1), "tea_table#4": ((18, 27), 1),
    # the hall: the inn's piano
    "piano": ((9, 11), 1),
    # the guest room
    "wardrobe": ((3, 2), 1), "easel": ((5, 2), 1), "nightstand": ((7, 4), 1), "writing_desk": ((3, 5), 1),
    # the artists' room (locked till Bea's 10 hearts): their canvases racked against the wall,
    # two easels mid-work, a stack of blank canvases, a rug to catch the drips
    "rack_canvases": ((13, 2), 1), "easel_lighthouse": ((16, 3), 1),
    "stack_canvases": ((17, 7), 1),                 # leaning on the bed's foot (the user, 2026-11-07)
    # Bea's room
    "wardrobe#2": ((22, 2), 1), "nightstand#2": ((25, 4), 1), "writing_desk#2": ((22, 6), 1),
    # The paintings on the walls: the ones painted for the art shop's gallery, each once
    # (the user, 2026-11-07: "more painting variety", not the same sketches over and over)
    "painting_snow_cabin": ((5, 17), 0), "painting_sunset_sea": ((9, 17), 0),          # the snug
    "painting_lighthouse": ((3, 10), 0), "painting_tree": ((17, 10), 0),               # the hall
    "painting_cabin": ((22, 10), 0),
    "painting_river": ((8, 1), 0),                                                       # the guest room, over the bed
    "painting_mosaic": ((16, 1), 0),                # the artists' room (clear of the wall's 45-degree end)
    "painting_flowers": ((26, 1), 0),                                                    # Bea's, over her bed
    # small ones: the gallery paintings not hung full size, in miniature
    "mini_night": ((8, 10), 0), "mini_blocks": ((21, 10), 0), "mini_fruit_jug": ((7, 17), 0), "mini_stripes": ((24, 1), 0),
}
# The game's own furniture, tile for tile as its houses lay it (townInterior, gids from 1;
# townInterior_2 from 2177). Each piece is its rows, top first; each row names its layers:
# F over the farmer, B solid, F2 small things on top, B2 on the floor (a mat). A B row on
# the wall (a couch's back on the wainscot, as Sam's is) moves the wainscot to Back behind it.
VANILLA = {
    "couch":        [{"B": [493, 494, 495]}, {"B": [525, 526, 527]}],                 # Haley's red one
    "coffee_table": [{"F": [624, 625, 626], "F2": [0, 2611, 2612]},
                     {"B": [656, 657, 658], "F": [2642, 2643, 2644]}, {"B": [688, 689, 690]}],
    "armchair_w":   [{"F": [555, 556]}, {"B": [587, 588]}, {"B": [619, 620]}],     # Haley's, facing west
    "pouf":         [{"B": [492]}],
    "floor_lamp":   [{"F": [1347]}, {"B": [453]}],
    "plant":        [{"F": [504]}, {"B": [536]}],
    "plant_b":      [{"F": [503]}, {"B": [535]}],
    "clock":        [{"F": [550]}, {"B": [582]}],                                   # Josh's
    "hall_table":   [{"B": [485, 486], "F": [335, 482]}, {"B": [517, 518]}],       # Josh's, a plant and a photo on it
    "logs":         [{"B": [897]}],
    # the kids' corner (Jas's and Vincent's things): the big blue bear, a dollhouse, a teddy,
    # a toy, and a little pedestal table for a tea party of their own
    "bear":         [{"F": [1281, 1282]}, {"B": [1313, 1314]}],
    "dollhouse":    [{"F": [1284, 1285]}, {"B": [1316, 1317]}],
    "kids_table":   [{"F": [1217]}, {"B": [1249]}],
    "teddy":        [{"B": [545]}],
    "toy":          [{"B": [1315]}],
    # small framed pictures, hung on the wall rows
    "pic_landscape": [{"F": [176]}], "pic_portraits": [{"F": [177]}], "pic_round": [{"F": [178]}],
    "pic_flower":   [{"F": [208]}], "pic_meadow": [{"F": [210]}], "pic_photo": [{"F": [246]}], "pic_pink": [{"F": [247]}],
    # Haley's kitchen, from the wall down: cupboards and the window shelf, the counters,
    # the stove, the sink and the fridge, a mat in front
    # (without Haley's side table at its end, so it fits between Bea's way in behind the
    # counter and the wall's corner)
    "kitchen":      [{"F": [531, 532, 257, 567, 568]},
                     {"F": [563, 564, 289, 0, 571], "B": [0, 0, 570, 0, 0]},
                     {"B": [599, 600, 601, 602, 603]},
                     {"B": [631, 632, 633, 634, 635], "B2": [0, 0, 0, 0, 2305]},
                     {"B2": [2337, 2337, 2337, 2337, 2337]}],
    "dining":       [{"F": [0, 473, 474, 475, 0]},                                   # Haley's, two chairs
                     {"F": [683, 366, 367, 333, 747], "B": [0, 505, 506, 507, 0]},
                     {"B": [715, 537, 538, 539, 779]}],
}
FURNISH = [
    # the snug: the fire, the couch with its back to the wall facing the room, the coffee
    # table, an armchair turned to it, a pouf, a lamp (the user: "a communal area for hanging out")
    ("logs", 4, 20), ("couch", 5, 19), ("floor_lamp", 8, 19), ("coffee_table", 5, 21),
    ("pouf", 4, 22), ("armchair_w", 8, 21), ("plant", 1, 22), ("plant_b", 9, 27), ("pouf", 6, 24),
    # the kids' corner below it, for visitors' children (the user, 2026-11-07)
    ("bear", 1, 25), ("kids_table", 4, 26), ("pouf", 3, 27), ("pouf", 5, 27), ("dollhouse", 6, 26),
    ("teddy", 3, 28), ("toy", 1, 27),
    # the tea room
    ("plant", 13, 19), ("plant_b", 22, 27),
    # the kitchen
    # (a tile in from the tea room, leaving Bea's way behind her counter at 23)
    ("kitchen", 24, 17), ("dining", 24, 23), ("plant", 29, 27),
    # the hall
    ("hall_table", 3, 12), ("plant", 8, 12), ("clock", 12, 12), ("hall_table", 17, 12),
    ("plant_b", 21, 12), ("hall_table", 22, 12), ("plant", 27, 12),
]
# What right-clicking a piece says (as build_vale_buildings.py's describe): a PIECES name, a
# FURNISH piece as (name, x, y), or "fireplace" -> text key, i18n vale.house.<key>, mapped
# into Strings/StringsFromMaps as blinxxy.MuseArt_Vale.house.<key> (content.json)
DESCRIBE = {
    "guestbook_case": "guestbook", "china_dresser": "china", "teapot_shelf": "teapots", "tea_counter": "counter",
    "tea_table": "tea_table", "tea_table#2": "tea_table", "tea_table#3": "tea_table", "tea_table#4": "tea_table",
    "piano": "piano", "painting_lighthouse": "lighthouse",
    "easel": "guest_easel", "writing_desk": "guest_desk",
    "rack_canvases": "canvases", "easel_lighthouse": "lighthouse_easel", "stack_canvases": "blank_canvases",
    "writing_desk#2": "accounts", "painting_flowers": "flowers",
    ("clock", 12, 12): "clock", ("hall_table", 3, 12): "photo", ("bear", 1, 25): "bear",
    ("dollhouse", 6, 26): "dollhouse", ("kitchen", 24, 17): "kitchen", "fireplace": "fireplace",
}
BEDS = {"guest": (8, 4), "artists": (17, 4), "bea": (26, 4)}   # the Saloon's spare bed, top-left
SLEEP_IN = "guest"
RUGS = [(4, 21, 5, 4), (10, 21, 4, 7), (17, 24, 4, 5), (2, 26, 6, 3), (14, 5, 3, 3)]          # Haley's red rug, 9-sliced: x, y, w, h
FIREPLACE_X = 2
LIGHTS = [(5, 4), (15, 4), (24, 4), (4, 12), (18, 12), (8, 20), (2, 21), (12, 24), (19, 25), (26, 24)]
# Pieces painted for the art shop (its gallery's paintings, the easels and canvases), cut
# from its sheet into the Vale's own (where furniture.json says build_shop_furniture.py packed them)
SHOP_SHEET = "[CP] Pelican Palette/assets/ArtShopFurniture.png"
SHOP_PIECES_JSON = "art/shop_interior/furniture.json"
SHOP_DIR = "art/gilded_vale/interiors/vale_house/from_shop"
FROM_SHOP = [k.split("#")[0] for k in PIECES if k.startswith(("painting_", "easel_", "rack_", "stack_"))]
# Paintings hang centred in the wallpaper's band: from the top of the wall's first row to the
# wainscot's rail, 9 px into the second (the user, 2026-11-07: the big ones hung too low, over
# the rail, the game's small framed pictures too high, against the ceiling's border)
WALL_BAND = 25
EXTRA_ART = {}
SHEET = "ValeHouseInterior.png"


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    g = {l: [[0] * W for _ in range(H)] for l in bmr.LAYERS}
    for y in range(H):
        for x in range(W):
            g["Back"][y][x] = g["Buildings"][y][x] = 1            # the void round the house

    # the furniture sheet (the inn's PixelLab pieces, and the art shop's)
    import json
    shop, where = Image.open(SHOP_SHEET).convert("RGBA"), json.load(open(SHOP_PIECES_JSON))
    os.makedirs(SHOP_DIR, exist_ok=True)
    for name in FROM_SHOP:
        p = where[name]
        shop.crop((p["x"] * 16, p["y"] * 16, (p["x"] + p["w"]) * 16, (p["y"] + p["h"]) * 16)).save(os.path.join(SHOP_DIR, name + ".png"))
        EXTRA_ART[name] = os.path.join(SHOP_DIR, name + ".png")
    for name in [k for k in PIECES if k.startswith("mini_")]:
        mini(where, shop, name[5:]).save(os.path.join(SHOP_DIR, name + ".png"))
        EXTRA_ART[name] = os.path.join(SHOP_DIR, name + ".png")
    for name in [k for k in PIECES if k.startswith(("painting_", "mini_"))]:
        hang(os.path.join(SHOP_DIR, name + ".png"))
    bvi.PICKS.update({k: os.path.relpath(v, bvi.ART)[:-4].replace("\\", "/") for k, v in EXTRA_ART.items()})
    sheet, slots = bvi.pack(bvi.ART, raw={k for k in PIECES if k.startswith(("painting_", "mini_"))})
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
            # the room's own planks run under the door (the user, 2026-11-07: the
            # vanilla threshold's wood was a different colour from the floor)
            g["Back"][y][d] = (607 if d % 2 else 608) if y % 2 else (639 if d % 2 else 640)
            if y < tl + 3:
                g["Back"][y][d - 1] = g["Back"][y][d + 1] = wall_first + (y - tl - 1 if y > tl else 0)

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
    # the game's own furniture
    walls = {y for r in ROOMS.values() for y in range(r["t"] + 1, r["t"] + 4)}
    for name, px, py in FURNISH:
        for j, row in enumerate(VANILLA[name]):
            for code, gids in row.items():
                for i, gid in enumerate(gids):
                    if not gid:
                        continue
                    x, y = px + i, py + j
                    if code == "B":
                        if y in walls and g["Buildings"][y][x]:
                            g["Back"][y][x] = g["Buildings"][y][x]      # the wainscot, behind
                        g["Buildings"][y][x] = gid
                    elif code == "B2":
                        g["Back2"][y][x] = gid
                    elif code == "F2":
                        g["Front2"][y][x] = gid
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
    # "Bed" is what tells the game you're in bed: without it, sleeping darkened the room
    # and the day never ended (Game1.NewDay fades out only a farmer in bed)
    tile_props += [("Back", bx + i, by + j, "Bed", "T") for i in range(2) for j in (1, 2, 3)]
    # The doorways are doors, as Haley's is ("Action Door Haley" on its foot, 121): the game
    # opens them as you walk up. (Bea's own room and the artists' room get their owners'
    # names, locking them, once the residents exist.)
    for upper, lower, d_ in DOORS:
        tile_props.append(("Buildings", d_, ROOMS[lower]["t"] + 2, "Action", "Door"))
    # right-click a piece to read about it: Action Message on its solid tiles (a wall piece's:
    # the wainscot below it, in reach). Standing pieces first, so a wall piece over one
    # (the teapot shelf over the counter) keeps only the wainscot the other doesn't cover.
    said, standing, hung = {(x, y) for _, x, y, k, _ in tile_props if k == "Action"}, [], []
    for key, ((px, py), solid) in PIECES.items():
        text = DESCRIBE.get(key)
        if not text:
            continue
        _, cols, rows = slots[key.split("#")[0]]
        if solid > 0:
            standing.append((text, [(px + i, py + rows - 1 - j) for i in range(cols) for j in range(solid)]))
        else:
            r = next(r for r in ROOMS.values() if r["a"] <= px <= r["b"] and r["t"] < py <= r["t"] + 3)
            hung.append((text, [(px + i, y) for i in range(cols) for y in (r["t"] + 2, r["t"] + 3)]))
    for name, px, py in FURNISH:
        text = DESCRIBE.get((name, px, py))
        if text:
            standing.append((text, [(px + i, py + j) for j, row in enumerate(VANILLA[name])
                                    for i, gid in enumerate(row.get("B", [])) if gid]))
    standing.append((DESCRIBE["fireplace"], [(FIREPLACE_X + i, tt + 4 + j) for i in range(2) for j in range(2)]))
    for text, cells in standing + hung:
        for x, y in cells:
            if g["Buildings"][y][x] and (x, y) not in said:
                said.add((x, y))
                tile_props.append(("Buildings", x, y, "Action", f"Message blinxxy.MuseArt_Vale.house.{text}"))
    props = {"Warp": f"{d} {f + 2} {VALE} {INN_DOOR[0]} {INN_DOOR[1] + 1}",
             "Light": " ".join(f"{x} {y} {bmr.LIGHT_TEXTURE}" for x, y in LIGHTS),
             "AllowWakeUpWithoutBed": "T",
             # passing out here costs no clinic bill: Bea puts you to bed (ValeResidents.PassOutPrefix)
             "PassOutSafe": "T",
             # the game makes a tile a door only if this lists it (x y, sheet 1, tile 120), as Haley's map does
             "Doors": " ".join(f"{d_} {ROOMS[lower]['t'] + 2} 1 120" for _, lower, d_ in DOORS)}
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("indoor2", T2, "townInterior_2", 512, 656),
                ("z_blinxxy.MuseArt_ValeHouseInterior", furn_first, SHEET, sheet.width, sheet.height),
                ("z_blinxxy.MuseArt_ValeWallpapers", wall_first, "ValeWallpapers.png", 16, 48)]
    write_raw(os.path.join(assets, "ValeHouse.tmx"), g, props, tilesets, tile_props)
    for old in ("ValeHouseGuest.tmx", "ValeHouseUpstairs.tmx"):
        if os.path.exists(os.path.join(assets, old)):
            os.remove(os.path.join(assets, old))
    print("wrote ValeHouse.tmx", f"{W}x{H}")


def mini(where, shop, name):
    """A gallery painting in miniature: the picture inside its frame shrunk to 10x8 (narrower
    if it's tall), in a thin wooden frame of its own."""
    p = where["painting_" + name]
    c = shop.crop((p["x"] * 16, p["y"] * 16, (p["x"] + p["w"]) * 16, (p["y"] + p["h"]) * 16))
    c = c.crop(c.getbbox())
    inner = c.crop((3, 3, c.width - 3, c.height - 3))
    tw, th = 10, 8
    if inner.width / inner.height < tw / th:
        tw = max(6, round(th * inner.width / inner.height))
    framed = Image.new("RGBA", (tw + 4, th + 4), (74, 40, 24, 255))
    framed.paste(Image.new("RGBA", (tw + 2, th + 2), (122, 74, 40, 255)), (1, 1))
    framed.paste(inner.resize((tw, th), Image.BOX), (2, 2))
    return framed


def hang(path):
    """Redraws a painting on its own canvas of whole tiles, two rows tall (the wall's first
    two), centred across and hung in the middle of the wallpaper's band."""
    im = Image.open(path).convert("RGBA")
    im = im.crop(im.getbbox())
    cols = -(-im.width // 16)
    out = Image.new("RGBA", (cols * 16, 32))
    out.alpha_composite(im, ((cols * 16 - im.width) // 2, max(0, (WALL_BAND - im.height) // 2)))
    out.save(path)


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
        by_tile = {}
        for _, x, y, k, v in mine:
            by_tile.setdefault((x, y), []).append((k, v))
        for (x, y), kvs in by_tile.items():
            n += 1
            lines += [f'    <object id="{n}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">',
                      '      <properties>' + "".join(f'<property name="{k}" type="string" value="{v}" />' for k, v in kvs) + '</properties>',
                      '    </object>']
        lines.append('  </objectgroup>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()

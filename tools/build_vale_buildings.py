"""Builds the interiors of the Gilded Vale's buildings (1.4, the dev pack only),
one floor plan per building, the way tools/build_vale_house.py builds the inn
(docs/interior_design.md). Each building's plan is drawn first from the same
spec and approved by the user before it's built:

  build_vale_buildings.py plan <building> <out.png>
  build_vale_buildings.py build <building> <exports dir> "[CP] Gilded Vale (dev)/assets"

A room is a rectangle of floor: columns a..b, its top border on row t, its last
floor row f (under its bottom border). Rows t+1..t+3 are its back wall. Rooms
stacked one above another (the lower one's t = the upper one's f + 1) join
through a door. Furniture is (key, x, y): x, y its top-left tile. Wall-backed
pieces stand with their base on the first floor row (t+4) and their top over
the wall.
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))

# The game's own furniture: key -> rows, top first; each row names its layers (F over
# the farmer, B solid, F2 small things on top, B2 on the floor). townInterior from 1,
# townInterior_2 from 2177. (The inn's set, tools/build_vale_house.py, and more.)
VANILLA = {
    "couch":        [{"B": [493, 494, 495]}, {"B": [525, 526, 527]}],
    "coffee_table": [{"F": [624, 625, 626], "F2": [0, 2611, 2612]},
                     {"B": [656, 657, 658], "F": [2642, 2643, 2644]}, {"B": [688, 689, 690]}],
    "armchair_w":   [{"F": [555, 556]}, {"B": [587, 588]}, {"B": [619, 620]}],
    "pouf":         [{"B": [492]}],
    "floor_lamp":   [{"F": [1347]}, {"B": [453]}],
    "plant":        [{"F": [504]}, {"B": [536]}],
    "plant_b":      [{"F": [503]}, {"B": [535]}],
    "clock":        [{"F": [550]}, {"B": [582]}],
    "hall_table":   [{"B": [485, 486], "F": [335, 482]}, {"B": [517, 518]}],
    "logs":         [{"B": [897]}],
    "kitchen":      [{"F": [531, 532, 257, 567, 568, 257]},
                     {"F": [563, 564, 289, 0, 0, 289]},
                     {"B": [599, 600, 601, 602, 603, 489], "F": [0, 0, 0, 0, 0, 514]},
                     {"B": [631, 632, 633, 634, 635, 521], "B2": [0, 0, 0, 0, 2305, 2305]},
                     {"B2": [2337, 2337, 2337, 2337, 2337, 0]}],
    # (in the Vale's houses without 570 and 571, the bits of Haley's wall cupboard over the
    # stove and fridge: on our walls they hung as stray boxes, the user, 2026-11-07)
    "dining":       [{"F": [0, 473, 474, 475, 0]},
                     {"F": [683, 366, 367, 333, 747], "B": [0, 505, 506, 507, 0]},
                     {"B": [715, 537, 538, 539, 779]}],
    "bookcase":     [{"F": [578, 579]}, {"F": [610, 611]}, {"B": [642, 643]}],      # against the wall
    "work_table":   [{"F": [908, 909]}, {"B": [940, 941]}],                         # the clinic's plain table
    "anvil":        [{"B": [934, 935]}, {"B": [966, 967]}],                         # Clint's
    "window":       [{"F": [226]}, {"F": [258]}],                                   # on the wallpaper
    # the brick fireplace (Haley's, the Saloon's), built into a back wall: x, t+1
    "fireplace":    [{"F": [667, 668]}, {"F": [699, 700]}, {"F": [731, 732]}, {"B": [763, 764]}, {"B": [795, 796]}],
    "fireplace_stone": [{"F": [669, 670]}, {"F": [701, 702]}, {"F": [733, 734]}, {"B": [765, 766]}, {"B": [797, 798]}],
}
# Our own PixelLab pieces (the game has none): key -> (tiles wide, tall, solid bottom rows).
# A solid of 0 hangs on the wall; -2 lies on the floor.
ART = {
    "kiln":          (4, 6, 2),     # brick, built into the back wall, its flue up through the outline
    "wheel":         (2, 2, 1),     # a kick wheel with its stool
    "drying_rack":   (2, 3, 1),     # unfired pots drying on slatted shelves
    "pot_shelf":     (2, 3, 1),     # his glazed ware for sale: terracotta, slate, slip white
    "pot_shelf_b":   (2, 3, 1),     # and his slip-white ware
    "glaze_shelf":   (2, 3, 1),     # glaze jars and buckets
    "sales_counter": (3, 2, 1),     # a cash tin and a few bowls on it
    "clay_sacks":    (1, 2, 1),
    "planters":      (2, 2, 1),     # stacked terracotta planters (Robin buys them)
    "slip_bucket":   (1, 1, 1),
    "urns":          (2, 2, 1),     # big storage jars, finished orders waiting
    "price_board":   (2, 2, 0),     # chalked prices, on the wall
    "chest":         (2, 1, 1),     # a blanket chest at the bed's foot
    "tool_board":    (2, 2, 0),     # his ribs, wires and loop tools on pegs
    "clay_basket":   (1, 1, 1),
    "stool":         (1, 1, 1),
    "big_vase":      (1, 2, 1),     # slate blue, dried grass in it
    "packing_crate": (2, 2, 1),     # an order packed in straw
    # the atelier's
    "north_window":  (5, 2, 0),     # a big many-paned studio window on the north wall
    "easel_big":     (2, 3, 1),     # his easel, a half-finished canvas on it, the stool
    "paint_table":   (2, 2, 1),     # a palette, paint tubes, jars of brushes
    "brush_table":   (3, 2, 1),     # a long table of jars, rags, rolled canvas
    "canvas_stack":  (2, 2, 1),     # blank canvases leaning against each other
    "bust":          (1, 2, 1),     # a plaster bust on a plinth, for drawing
    "display_easel": (2, 3, 1),     # a gallery easel with a framed painting
    "bench":         (3, 1, 1),     # a long plain gallery bench
    "hat_stand":     (1, 2, 1),     # his straw hat and smock on it
    "empty_hook":    (1, 2, 0),     # a nail and a paler square of wallpaper
    # the inn's, reused
    "wardrobe":      (2, 3, 1), "nightstand": (1, 2, 1), "writing_desk": (2, 2, 1), "sketch_wall": (2, 2, 0),
}
# The game's own Lupini paintings (Data/Furniture 1838-1854, 2 x 2 on TileSheets/furniture,
# which the map loads straight from the game as ../TileSheets/furniture). Eight hang in his
# atelier; Red Eagle stands on the hall's display easel; one a week is on his boat.
LUPINI = {"painting_1": 1844,       # Clouds
          "painting_2": 1842,       # Solar Kingdom
          "painting_3": 1840,       # Portrait of a Mermaid
          "painting_4": 1854,       # Land of Clay (painted at Tobin's clay pit)
          "painting_5": 1848,       # Three Trees
          "painting_6": 1850,       # The Serpent
          "painting_7": 1852,       # Tropical Fish #173
          "painting_8": 1846}       # 1000 Years From Now
EASEL_PAINTING = 1838               # Red Eagle
BED = (2, 4)                        # the Saloon's spare bed
# The game's rugs that 9-slice (townInterior_2, their top-left tile): ("rug", x, y, w, h)
RUGS = {"rug": 2761,                # Haley's red one
        "rug_brown": 2753}          # the brown patterned one

BUILDINGS = {
    # Tobin's pottery (map doc, section 5: "like the Blacksmith. The workshop and kiln at
    # the front, Tobin's rooms behind"; tobin_brief.md). The kiln stands where the
    # outside's chimney is, its flue rising between his two rooms.
    "pottery": dict(
        title="Tobin's pottery", location="blinxxy.MuseArt_ValePottery", tmx="ValePottery.tmx",
        W=26, H=25, outside=(44, 17),
        rooms={"kitchen":  dict(a=1, b=11, t=0, f=10, label="Kitchen and parlour"),
               "bedroom":  dict(a=18, b=24, t=0, f=10, label="Tobin's bedroom"),
               "workshop": dict(a=1, b=24, t=11, f=22, label="Workshop")},
        doors=[("kitchen", "workshop", 9), ("bedroom", "workshop", 20)],
        front=("workshop", 8),
        # floors: planks unless zoned (x0, y0, x1, y1, floor)
        zones=[(11, 15, 24, 22, "terracotta"), (2, 4, 7, 6, "kitchen tiles")],
        pieces=[
            # the shop corner by the door: his ware for sale, the counter
            ("pot_shelf", 2, 13), ("pot_shelf_b", 4, 13), ("price_board", 6, 12),
            ("rug_brown", 2, 16, 5, 4), ("sales_counter", 3, 17), ("stool", 2, 18), ("big_vase", 1, 16),
            ("planters", 1, 20), ("plant", 6, 20), ("packing_crate", 9, 20),
            # the workshop: the kiln with hardwood stacked by it, the drying rack,
            # the wheels in front (Tobin's, and the one he teaches you on), the
            # wedging table, clay in sacks, glazes
            ("drying_rack", 11, 13), ("kiln", 13, 11), ("logs", 17, 15), ("logs#2", 18, 15), ("tool_board", 17, 12),
            ("glaze_shelf", 22, 13),
            ("wheel", 13, 18), ("slip_bucket", 15, 19), ("wheel#2", 17, 18), ("clay_basket", 12, 19),
            ("work_table", 21, 19), ("clay_sacks", 24, 17), ("clay_sacks#2", 24, 19),
            ("urns", 11, 20), ("plant_b", 19, 20),
            # his kitchen and parlour
            ("kitchen", 2, 1), ("bookcase", 8, 2), ("window", 10, 1), ("plant", 11, 3),
            ("dining", 2, 7), ("armchair_w", 9, 6), ("floor_lamp", 11, 7),
            # his bedroom
            ("wardrobe", 18, 2), ("nightstand", 21, 3), ("bed", 22, 4), ("plant_b", 24, 4),
            ("chest", 22, 8), ("rug", 18, 6, 3, 3), ("sketch_wall", 19, 1), ("window", 23, 1),
        ],
        wall=65, walls={"kitchen": 75, "bedroom": 100},     # the game's wallpapers: limewashed brick, plaster, stripes
        picks={k: "art/gilded_vale/interiors/pottery/pottery_v1/" + v for k, v in {
            "kiln": "kiln_2_6", "drying_rack": "drying_rack_1_14", "pot_shelf": "pot_shelf_1_1",
            "pot_shelf_b": "pot_shelf_1_17", "glaze_shelf": "glaze_shelf_1_0", "sales_counter": "sales_counter_2_0",
            "clay_sacks": "clay_sacks_1_0", "planters": "planters_1_6", "slip_bucket": "slip_bucket_2_8", "price_board": "price_board_1_33",
            "chest": "chest_1_9", "urns": "urns_2_1",
            # (the user, 2026-11-07: the first wheels stood at an angle; these face the front)
            "wheel": "../pottery_v2/wheel_1_0", "tool_board": "../pottery_v2/tool_board_1_5",
            "clay_basket": "../pottery_v2/clay_basket_1_5", "stool": "../pottery_v2/stool_1_1",
            "big_vase": "../pottery_v2/big_vase_1_0", "packing_crate": "../pottery_v2/packing_crate_1_1"}.items()},
        lights=[(5, 5), (21, 5), (4, 17), (9, 19), (14, 16), (18, 20), (22, 17)],
        notes=["Tobin's pottery: one floor plan. Each square is a tile; the front door is at the bottom.",
               "The workshop is the front: his shop corner by the door (his ware on shelves, the counter);",
               "the kiln built into the back wall where the chimney is outside, hardwood by it, the drying rack;",
               "two wheels before it (his, and the one he teaches you on), the wedging table, clay sacks, glazes.",
               "Behind, through doors: his kitchen and parlour (west) and his bedroom (east).",
               "Orange outline = new PixelLab piece; the rest are the game's own. Dotted line = walkway."]),

    # Lupini's atelier (map doc, section 5: "like Leah's cottage. One big room, a north
    # window, his nine canon paintings on the walls"; lupini_brief.md). Eight hang; the
    # ninth's hook is empty, the week it's on his boat at the Night Market.
    "atelier": dict(
        title="Lupini's atelier", location="blinxxy.MuseArt_ValeAtelier", tmx="ValeAtelier.tmx",
        W=22, H=24, outside=(73, 23),
        rooms={"studio": dict(a=1, b=20, t=0, f=12, label="The studio"),
               "hall":   dict(a=2, b=19, t=13, f=20, label="The hall")},
        doors=[("studio", "hall", 11)],
        front=("hall", 7),
        zones=[(2, 17, 19, 20, "parquet")],
        wall=103, walls={"hall": 76},        # plain plaster in the studio; the hall a gallery's deep red
        picks={"empty_hook": "art/gilded_vale/interiors/atelier/empty_hook", **{k: "art/gilded_vale/interiors/atelier/atelier_v1/" + v for k, v in {
            "north_window": "north_window_1_0", "easel_big": "easel_big_1_0", "paint_table": "paint_table_1_0",
            "brush_table": "brush_table_1_0", "canvas_stack": "canvas_stack_1_0", "bust": "bust_1_0",
            "display_easel": "display_easel_1_0", "bench": "bench_1_0", "hat_stand": "hat_stand_1_0",
            "chest": "../../pottery/pottery_v1/chest_1_9"}.items()}},
        lights=[(5, 6), (11, 7), (18, 6), (3, 10), (15, 10), (5, 18), (11, 18), (16, 18)],
        pieces=[
            # the studio's back wall: the fireplace, the north window over the easel, paintings
            ("fireplace", 2, 1), ("painting_1", 5, 1), ("north_window", 8, 1),
            ("painting_2", 14, 1), ("painting_3", 17, 1),
            # by the fire: the couch with its back to the wall, the coffee table, an
            # armchair turned to it, all on one rug; a lamp
            ("rug", 3, 5, 6, 4), ("couch", 4, 3), ("coffee_table", 4, 5), ("armchair_w", 7, 5),
            ("floor_lamp", 8, 3), ("logs", 1, 6),
            # under the north light: the easel with his stool, the paint table, canvases
            ("easel_big", 10, 4), ("paint_table", 12, 6), ("canvas_stack", 1, 10),
            ("brush_table", 13, 9), ("bust", 19, 10), ("plant", 6, 10), ("plant_b", 20, 4),
            # his corner: the bed under two paintings, the nightstand, a trunk at its foot
            ("nightstand", 17, 3), ("bed", 18, 4), ("chest", 18, 8),
            # the hall: the rest of the paintings, the empty hook, a bench to sit and look,
            # this week's painting on a display easel, his hat stand by the door
            ("painting_4", 3, 14), ("painting_5", 6, 14), ("empty_hook", 9, 14),
            ("painting_6", 13, 14), ("painting_7", 16, 14),
            ("bench", 12, 18), ("display_easel", 17, 17), ("hat_stand", 8, 18),
            ("plant", 2, 17), ("plant_b", 19, 19), ("canvas_stack#2", 2, 19),
        ],
        notes=["Lupini's atelier: one floor plan, like Leah's cottage. The front door opens into the hall.",
               "The hall: his canon paintings on the wall, the empty hook of the one on his boat this week,",
               "a bench to sit and look, a display easel with another, his hat stand by the door.",
               "Through the door, the studio: the easel under the big north window, paint and brush tables,",
               "the fireside (couch, coffee table, armchair on one rug), and his bed under two paintings.",
               "Paintings are the game's own Lupini paintings. Orange outline = new PixelLab piece. Dotted line = walkway."]),
}

# the game's floorings (walls_and_floors) for floors that aren't planks or Sam's tiles
FLOORINGS = {"flagstones": 10, "terracotta": 21, "parquet": 16}
FLOOR_COLOURS = {"planks": (196, 140, 82), "flagstones": (150, 146, 138), "terracotta": (186, 98, 60), "parquet": (150, 90, 50), "kitchen tiles": (200, 214, 226),
                 "carpet": (150, 70, 70), "straw": (214, 190, 110), "glass": (170, 210, 190)}


def solid_cells(spec):
    """Every tile a piece blocks (its solid rows), and every wall tile."""
    cells = set()
    for p in spec["pieces"]:
        key, x, y = p[0].split("#")[0], p[1], p[2]
        w, h, s = footprint(p)
        for j in range(h - s, h):
            for i in range(w):
                cells.add((x + i, y + j))
    return cells


def footprint(p):
    key = p[0].split("#")[0]
    if key in RUGS:
        return p[3], p[4], 0
    if key == "bed":
        return BED[0], BED[1], 3
    if key in LUPINI:
        return 2, 2, 0
    if key in VANILLA:
        rows = VANILLA[key]
        w = max(len(g) for r in rows for g in r.values())
        s = 0
        for r in reversed(rows):
            if "B" in r:
                s += 1
            else:
                break
        return w, len(rows), s
    w, h, s = ART[key]
    return w, h, max(s, 0)


def floor_cells(spec):
    cells = {}
    for name, r in spec["rooms"].items():
        for y in range(r["t"] + 4, r["f"] + 1):
            for x in range(r["a"], r["b"] + 1):
                cells[(x, y)] = "planks"
    for x0, y0, x1, y1, kind in spec["zones"]:
        for (x, y) in list(cells):
            if x0 <= x <= x1 and y0 <= y <= y1:
                cells[(x, y)] = kind
    return cells


def walk_cells(spec):
    """Where the farmer can walk: the floor, the doorways, the front door's mat."""
    cells = set(floor_cells(spec))
    for upper, lower, d in spec["doors"]:
        tl = spec["rooms"][lower]["t"]
        for y in range(tl, tl + 4):
            cells.add((d, y))
    room, d = spec["front"]
    f = spec["rooms"][room]["f"]
    cells |= {(d, f + 1), (d, f + 2)}
    return cells - solid_cells(spec)


def route(walk, start, goal):
    from collections import deque
    prev, q = {start: None}, deque([start])
    while q:
        c = q.popleft()
        if c == goal:
            path = []
            while c:
                path.append(c)
                c = prev[c]
            return path[::-1]
        for dx, dy in ((0, -1), (1, 0), (-1, 0), (0, 1)):
            n = (c[0] + dx, c[1] + dy)
            if n in walk and n not in prev:
                prev[n] = c
                q.append(n)
    return None


def plan(name, out):
    spec = BUILDINGS[name]
    T = 30
    W, H = spec["W"], spec["H"]
    font = ImageFont.truetype("arial.ttf", 11)
    bold = ImageFont.truetype("arialbd.ttf", 14)
    small = ImageFont.truetype("arial.ttf", 13)
    notes_h = 22 * len(spec["notes"]) + 16
    im = Image.new("RGB", (W * T, H * T + notes_h), (24, 22, 28))
    d = ImageDraw.Draw(im, "RGBA")

    def box(x, y, w=1, h=1, fill=None, outline=None, width=1):
        d.rectangle([x * T, y * T, (x + w) * T - 1, (y + h) * T - 1], fill=fill, outline=outline, width=width)

    floors = floor_cells(spec)
    for (x, y), kind in floors.items():
        c = FLOOR_COLOURS[kind]
        if kind in ("kitchen tiles", "flagstones") and (x + y) % 2:
            c = tuple(v - 14 for v in c)
        box(x, y, fill=c)
    for r in spec["rooms"].values():
        box(r["a"] - 1, r["t"], r["b"] - r["a"] + 3, 1, fill=(214, 120, 60))         # the top border
        for k in range(3):
            box(r["a"], r["t"] + 1 + k, r["b"] - r["a"] + 1, 1,
                fill=(222, 206, 170) if k < 2 else (120, 72, 40))                   # wallpaper, wainscot
        for y in range(r["t"] + 1, r["f"] + 1):
            box(r["a"] - 1, y, fill=(214, 120, 60))
            box(r["b"] + 1, y, fill=(214, 120, 60))
        d.line([(r["a"] * T, (r["f"] + 1) * T - 2), ((r["b"] + 1) * T, (r["f"] + 1) * T - 2)], fill=(214, 120, 60), width=4)
    for upper, lower, dx in spec["doors"]:
        tl = spec["rooms"][lower]["t"]
        box(dx, spec["rooms"][upper]["f"], 1, 1, fill=FLOOR_COLOURS["planks"])
        box(dx, tl, 1, 4, fill=(236, 200, 120))
        box(dx - 1, tl, 3, 4, outline=(255, 240, 200), width=2)
    room, dx = spec["front"]
    f = spec["rooms"][room]["f"]
    box(dx, f, 1, 2, fill=(236, 200, 120))
    d.text((dx * T + T // 2, (f + 2) * T + 4), "FRONT DOOR", fill=(255, 255, 255), font=bold, anchor="mt")

    # grid
    for x in range(W + 1):
        d.line([(x * T, 0), (x * T, H * T)], fill=(0, 0, 0, 50))
    for y in range(H + 1):
        d.line([(0, y * T), (W * T, y * T)], fill=(0, 0, 0, 50))
    for x in range(0, W, 5):
        d.text((x * T + 2, 1), str(x), fill=(255, 255, 0), font=font)
    for y in range(5, H, 5):
        d.text((2, y * T + 1), str(y), fill=(255, 255, 0), font=font)

    # rugs first, then the furniture
    for p in sorted(spec["pieces"], key=lambda p: p[0] not in RUGS):
        key = p[0].split("#")[0]
        x, y = p[1], p[2]
        w, h, s = footprint(p)
        new = key in ART
        if key in RUGS:
            box(x, y, w, h, fill=(160, 40, 40, 150) if key == "rug" else (170, 110, 50, 150), outline=(110, 20, 20), width=2)
        else:
            if h > s:
                box(x, y, w, h - s, fill=(60, 60, 90, 70))
            if s:
                box(x, y + h - s, w, s, fill=(50, 50, 80, 170))
            box(x, y, w, h, outline=(255, 150, 40) if new else (230, 230, 255), width=3 if new else 1)
        label = key.replace("_", " ")
        words, lines = label.split(), []
        for wd in words:
            if lines and d.textlength(lines[-1] + " " + wd, font=font) < w * T - 4:
                lines[-1] += " " + wd
            else:
                lines.append(wd)
        cy = (y + h / 2) * T - 6 * len(lines)
        for i, ln in enumerate(lines):
            d.text(((x + w / 2) * T, cy + 12 * i), ln, fill=(255, 255, 255), font=font, anchor="mt",
                   stroke_width=2, stroke_fill=(0, 0, 0))

    # the walkways: from the front door to every room's door
    walk = walk_cells(spec)
    start = (dx, f + 1)
    for upper, lower, ddx in spec["doors"]:
        path = route(walk, start, (ddx, spec["rooms"][upper]["f"] - 1))
        if not path:
            print("NO ROUTE to", upper)
            continue
        for (ax, ay), (bx, by) in zip(path, path[1:]):
            for k in range(0, 10, 3):
                px = ax + (bx - ax) * k / 10
                py = ay + (by - ay) * k / 10
                d.ellipse([(px + .5) * T - 2, (py + .5) * T - 2, (px + .5) * T + 2, (py + .5) * T + 2], fill=(255, 255, 255, 200))

    for r in spec["rooms"].values():
        d.text(((r["a"] + r["b"] + 1) / 2 * T, (r["t"] + 1) * T + 6), r["label"], fill=(40, 20, 10), font=bold, anchor="mt")
    for i, ln in enumerate(spec["notes"]):
        d.text((10, H * T + 10 + 22 * i), ln, fill=(230, 230, 230), font=small)
    im.save(out)
    print("wrote", out)


# ---------------------------------------------------------------- building

T2 = 2177                       # townInterior_2's first gid
WF = 2177 + 1312                # the game's walls_and_floors sheet (Maps/walls_and_floors), its first gid
VALE = "blinxxy.MuseArt_GildedVale"
SHEET_COLS = 16
INN_ART = "art/gilded_vale/interiors/vale_house"
INN_PICKS = {"wardrobe": INN_ART + "/vale_house_v2/wardrobe_2_0", "nightstand": INN_ART + "/vale_house_v2/nightstand_1_0",
             "writing_desk": INN_ART + "/vale_house_v2/writing_desk_2_1", "sketch_wall": INN_ART + "/vale_house_v1/sketch_wall_1_2"}


def wallpaper(i):
    """The game's wallpaper i (walls_and_floors): its three rows, top first."""
    top = WF + (i // 16) * 48 + i % 16
    return [top, top + 16, top + 32]


def flooring(i, x, y):
    """The game's flooring i (walls_and_floors, 2 x 2 tiles each, below the wallpapers)."""
    return WF + 336 + (i // 8) * 32 + (i % 8) * 2 + (x % 2) + 16 * (y % 2)


def pack(picks):
    from vanillafy import selout
    images, slots, x, y, row_h = {}, {}, 0, 0, 0
    for name, path in picks.items():
        im = Image.open(path + ".png").convert("RGBA")
        im = im.crop(im.getbbox())
        w, h = ART[name][:2]
        cols, rows = max(w, -(-im.width // 16)), max(h, -(-im.height // 16))
        if (cols, rows) != (w, h):
            print(f"  {name}: art is {cols}x{rows} tiles, planned {w}x{h}")
        tile = Image.new("RGBA", (cols * 16, rows * 16))
        tile.alpha_composite(im, ((cols * 16 - im.width) // 2, rows * 16 - im.height))
        images[name] = selout(tile)
        if x + cols > SHEET_COLS:
            x, y, row_h = 0, y + row_h, 0
        slots[name] = (x, y, cols, rows)
        x += cols
        row_h = max(row_h, rows)
    # and a shadow for the first row of any floor that isn't the game's planks
    shade_row = y + row_h
    sheet = Image.new("RGBA", (SHEET_COLS * 16, (shade_row + 1) * 16))
    for name, (sx, sy, _, _) in slots.items():
        sheet.alpha_composite(images[name], (sx * 16, sy * 16))
    for py in range(16):
        a = max(0, int(110 * (1 - py / 9)))
        for px in range(16):
            sheet.putpixel((px, shade_row * 16 + py), (40, 20, 10, a))
    return sheet, {n: (sy * SHEET_COLS + sx, c, r) for n, (sx, sy, c, r) in slots.items()}, shade_row * SHEET_COLS


def build(name, exports, assets):
    import build_margot_room as bmr
    from build_shop_interior import read
    spec = BUILDINGS[name]
    W, H, rooms = spec["W"], spec["H"], spec["rooms"]
    g = {l: [[0] * W for _ in range(H)] for l in bmr.LAYERS}
    for y in range(H):
        for x in range(W):
            g["Back"][y][x] = g["Buildings"][y][x] = 1            # the void round the house

    picks = {k: v for k, v in {**INN_PICKS, **spec["picks"]}.items()
             if any(p[0].split("#")[0] == k for p in spec["pieces"])}
    sheet, slots, shade = pack(picks)
    furn_first = WF + 16 * 43
    FURN = furn_first + (sheet.width // 16) * (sheet.height // 16)       # the game's furniture sheet
    sheet_name = spec["tmx"][:-4] + "Interior.png"
    sheet.save(os.path.join(assets, sheet_name))

    floors = floor_cells(spec)

    def floor(x, y, r):
        kind = floors[(x, y)]
        first = y == r["t"] + 4
        if kind == "planks":
            if first:
                return 574 if x == r["a"] else (575 if x % 2 else 576)    # the shaded row under the wall
            return (607 if x % 2 else 608) if y % 2 else (639 if x % 2 else 640)
        if kind == "kitchen tiles":
            return 295 if first else 263                               # Sam's kitchen tiles
        return flooring(FLOORINGS[kind], x, y)

    for rn, r in rooms.items():
        a, b, t, f = r["a"], r["b"], r["t"], r["f"]
        for x in range(a - 1, b + 2):
            for y in range(t, f + 1):
                g["Back"][y][x] = 1
                g["Buildings"][y][x] = 1 if x in (a - 1, b + 1) or y == t else 0
        for x in range(a, b + 1):
            g["Front"][t][x] = 2 if x == a else 4 if x == b else (2122 if x % 5 == 3 else 3)
        paper = wallpaper(spec.get("walls", {}).get(rn, spec.get("wall", 0)))
        for k in range(3):
            for x in range(a, b + 1):
                g["Back"][t + 1 + k][x] = paper[k]
        for x in range(a + 1, b):
            g["Buildings"][t + 2][x] = 81
            g["Buildings"][t + 3][x] = 113
        g["Front"][t + 1][a - 1], g["Front"][t + 1][a], g["Front"][t + 1][b], g["Front"][t + 1][b + 1] = 33, 42, 44, 37
        for k, (lg, rg) in enumerate(((80, 82), (112, 114), (144, 146))):
            g["Buildings"][t + 2 + k][a], g["Buildings"][t + 2 + k][b] = lg, rg
        for y in range(t + 4, f + 1):
            for x in range(a, b + 1):
                g["Back"][y][x] = floor(x, y, r)
                if y == t + 4 and floors[(x, y)] not in ("planks", "kitchen tiles"):
                    g["Back2"][y][x] = furn_first + shade
        for y in range(t + 2, f):
            g["Front"][y][a - 1] = 2153 if (y - t) % 4 == 3 else 65
            g["Front"][y][b + 1] = 2154 if (y - t) % 4 == 3 else 69
            g["Buildings"][y][a - 1], g["Buildings"][y][b + 1] = 65, 69
        g["Front"][f][a - 1], g["Front"][f][a], g["Front"][f][b], g["Front"][f][b + 1] = 161, 162, 167, 168
        for x in range(a + 1, b):
            g["Front"][f][x] = 2121 if x % 6 == 2 else 166

    # doors between stacked rooms (Haley's, down from her room into the living room)
    for upper, lower, d in spec["doors"]:
        fu, tl = rooms[upper]["f"], rooms[lower]["t"]
        paper = wallpaper(spec.get("walls", {}).get(lower, spec.get("wall", 0)))
        g["Front"][fu][d - 1], g["Front"][fu][d], g["Front"][fu][d + 1] = 163, 0, 164
        g["Front"][tl][d - 1], g["Front"][tl][d], g["Front"][tl][d + 1] = 195, 57, 194
        g["Front"][tl + 1][d - 1], g["Front"][tl + 1][d], g["Front"][tl + 1][d + 1] = 827, 89, 827
        g["Buildings"][tl][d] = 0
        g["Buildings"][tl + 1][d - 1], g["Buildings"][tl + 1][d + 1] = 156, 155
        g["Buildings"][tl + 2][d - 1], g["Buildings"][tl + 2][d], g["Buildings"][tl + 2][d + 1] = 188, 121, 187
        g["Buildings"][tl + 3][d - 1], g["Buildings"][tl + 3][d], g["Buildings"][tl + 3][d + 1] = 220, 0, 219
        for y in range(tl, tl + 4):
            # the upper room's own planks run under the door (not the vanilla threshold)
            g["Back"][y][d] = (607 if d % 2 else 608) if y % 2 else (639 if d % 2 else 640)
            if y < tl + 3:
                g["Back"][y][d - 1] = g["Back"][y][d + 1] = paper[y - tl - 1 if y > tl else 0]
        g["Back2"][tl + 4][d] = 0

    # the front door: a notch in the bottom border, a mat (Haley's)
    room, d = spec["front"]
    f = rooms[room]["f"]
    g["Front"][f][d - 1], g["Front"][f][d], g["Front"][f][d + 1] = 163, 0, 164
    g["Front"][f + 1][d - 1], g["Front"][f + 1][d], g["Front"][f + 1][d + 1] = 97, 197, 131
    g["Back"][f + 1][d], g["Buildings"][f + 1][d] = 23, 0
    g["Back"][f + 2][d] = g["Buildings"][f + 2][d] = 0

    # rugs (Haley's red one, on Back2), then the furniture
    walls = {y for r in rooms.values() for y in range(r["t"] + 1, r["t"] + 4)}
    saloon = read(os.path.join(exports, "Saloon.tmx"))
    for p in spec["pieces"]:
        key, px, py = p[0].split("#")[0], p[1], p[2]
        if key in RUGS:
            w, h = p[3], p[4]
            for j in range(h):
                row = RUGS[key] + 32 * (0 if j == 0 else 2 if j == h - 1 else 1)
                for i in range(w):
                    g["Back2"][py + j][px + i] = row + (0 if i == 0 else 3 if i == w - 1 else (1 if i < w - 2 else 2))
        elif key == "bed":
            for layer in ("Buildings", "Front"):
                lw, data = saloon[1][layer]
                for j in range(4):
                    for i in range(2):
                        gid = data[(bmr.BED_FROM[1] + j) * lw + bmr.BED_FROM[0] + i]
                        if gid:
                            g[layer][py + j][px + i] = gid
        elif key in LUPINI:
            n = LUPINI[key]
            for j in range(2):
                for i in range(2):
                    g["Front2" if g["Front"][py + j][px + i] else "Front"][py + j][px + i] = FURN + n + 32 * j + i
        elif key in VANILLA:
            for j, row in enumerate(VANILLA[key]):
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
        else:
            first, cols, rows = slots[key]
            solid = ART[key][2]
            fx, fy = first % SHEET_COLS, first // SHEET_COLS
            for j in range(rows):
                for i in range(cols):
                    if not sheet.crop(((fx + i) * 16, (fy + j) * 16, (fx + i + 1) * 16, (fy + j + 1) * 16)).getbbox():
                        continue
                    gid = furn_first + first + j * SHEET_COLS + i
                    x, y = px + i, py + j
                    if solid == -2:
                        g["Back2"][y][x] = gid
                    elif j >= rows - solid:
                        if g["Buildings"][y][x] and y in walls:
                            g["Back"][y][x] = g["Buildings"][y][x]
                        g["Buildings"][y][x] = gid
                    else:
                        g["Front2" if g["Front"][y][x] else "Front"][y][x] = gid
            if key == "display_easel":
                for j in range(2):                  # Red Eagle on it, over its canvas
                    for i in range(2):
                        g["Front2"][py + j][px + i] = FURN + EASEL_PAINTING + 32 * j + i

    tile_props = [("Buildings", d_, rooms[lower]["t"] + 2, "Action", "Door") for _, lower, d_ in spec["doors"]]
    ox, oy = spec["outside"]
    props = {"Warp": f"{d} {f + 2} {VALE} {ox} {oy + 1}",
             "Light": " ".join(f"{x} {y} {bmr.LIGHT_TEXTURE}" for x, y in spec["lights"])}
    if spec["doors"]:
        # the game makes a tile a door only if this lists it (x y, sheet 1, tile 120), as Haley's map does
        props["Doors"] = " ".join(f"{d_} {rooms[lower]['t'] + 2} 1 120" for _, lower, d_ in spec["doors"])
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("indoor2", T2, "townInterior_2", 512, 656),
                ("zz_wallsfloors", WF, "walls_and_floors", 256, 688),
                ("z_blinxxy.MuseArt_" + sheet_name[:-4], furn_first, sheet_name, sheet.width, sheet.height)]
    if any(p[0] in LUPINI or p[0] == "display_easel" for p in spec["pieces"]):
        tilesets.append(("zz_furniture", FURN, "../TileSheets/furniture", 512, 1488))
    write_raw(os.path.join(assets, spec["tmx"]), W, H, g, props, tilesets, tile_props, bmr.LAYERS)
    print("wrote", spec["tmx"], f"{W}x{H}")
    # the walkways, on the built map: from the front door's mat to every room (a door's
    # foot, 121, opens as you walk up)
    walk = {(x, y) for y in range(H) for x in range(W) if g["Buildings"][y][x] in (0, 121) and g["Back"][y][x] != 1}
    for rn, r in rooms.items():
        ok = route(walk, (d, f + 1), (r["a"] + (r["b"] - r["a"]) // 2, r["f"] - 1)) or              any(route(walk, (d, f + 1), (x, r["f"] - 1)) for x in range(r["a"], r["b"] + 1))
        print(f"  route to {rn}: {'ok' if ok else 'BLOCKED'}")


def write_raw(path, W, H, g, props, tilesets, tile_props, LAYERS):
    from xml.sax.saxutils import quoteattr
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{2 * len(LAYERS) + 2}" nextobjectid="{len(tile_props) + 1}">',
             '  <properties>'] + [f'    <property name="{k}" value={quoteattr(v)} />' for k, v in props.items()] + ['  </properties>']
    for tname, first, image, iw, ih in tilesets:
        lines += [f'  <tileset firstgid="{first}" name="{tname}" tilewidth="16" tileheight="16" tilecount="{(iw // 16) * (ih // 16)}" columns="{iw // 16}">',
                  f'    <image source="{image}" width="{iw}" height="{ih}" />', '  </tileset>']
    for i, layer in enumerate(LAYERS):
        lines += [f'  <layer id="{i + 1}" name="{layer}" width="{W}" height="{H}">', '    <data encoding="csv">',
                  ",\n".join(",".join(str(v) for v in row) for row in g[layer]), '    </data>', '  </layer>']
    n = 0
    for li, layer in enumerate(LAYERS):
        mine = [p for p in tile_props if p[0] == layer]
        if not mine:
            continue
        lines.append(f'  <objectgroup id="{len(LAYERS) + 1 + li}" name="{layer}">')
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
    if sys.argv[1] == "plan":
        plan(sys.argv[2], sys.argv[3])
    else:
        build(sys.argv[2], sys.argv[3], sys.argv[4])

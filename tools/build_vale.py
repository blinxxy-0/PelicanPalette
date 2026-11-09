"""Builds the Gilded Vale's GREYBOX (1.4, docs/design/gilded_vale_map.md):
the whole map from the game's own tiles, with plain boxes for the buildings,
so the terraces, stairs, bridges and distances can be walked before any art.

Pieces are stamped from the vanilla Town map (all its tile layers but Paths),
so they're the game's own cliffs, stairs, river, waterfall and bridges, and
change with the seasons (its tilesheets are the spring_ ones, which the game
swaps for the season outdoors):

  CLIFF      Town x 24-33, y 43-48: the ledge north of Pierre's (lip, face, foot)
  STAIRS     Town x 19-22, y 42-50: the stone stairs down that ledge
  RIVER      Town x 89-97, y 15-28: the river below the town's waterfall
  FALLS      Town x 91-97, y 0-6: the waterfall into its pool
  STONE_BR   Town x 69-78, y 51-57: the stone bridge by the clinic road
  LOG_BR     Town x 89-97, y 12-14: the log footbridge under the waterfall

Simplified from the sketch for now: the river runs straight down x 56-64,
the cliffs are straight, and the lake has no banks. Trees go on the Paths
layer (the game plants them on load).

The five signature buildings are the user's picks (2026-11-04) from
art/gilded_vale/buildings/vale_buildings_v2 (PICKS below), outlines softened
(vanillafy.selout), packed onto GildedValeBuildings.png. Each sits with its
base on the given row: its bottom SOLID rows are walls (Buildings), the rest
roof (Front2, over the farmer and over any cliff behind it, as Pierre's roof
hangs over the ledge behind his shop). The rest are still plain boxes.

Usage: build_vale.py <Town.tmx export> "[CP] Gilded Vale (dev)/assets" [vanilla exports dir, default: the Town export's folder]
"""
import collections, os, random, re, sys
from PIL import Image
import xml.etree.ElementTree as ET

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from render_tmx import layer_data
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from vanillafy import selout

ART = "art/gilded_vale"
# The building sheet is seasonal: the game swaps any sheet named spring_* for its
# summer_, fall_ and winter_ siblings (docs/terrain_painting.md, section 2). Each piece
# uses its spring art in every season it has no art of its own for (SEASONAL).
SHEET = "spring_GildedValeBuildings.png"
SHEET_COLS = 32                 # (wide enough for the widest piece: the parterre is 27)
SEASONS = ("spring", "summer", "fall", "winter")
# name: (art, left tile x, bottom tile y, solid[, lift px]); art under ART, without .png.
# solid: how many bottom rows block the farmer (the rest draw over him), or
# (rows, first col, last col) for a piece only solid in its middle, like a tree's trunk.
PICKS = {
    "vale_house": ("buildings/vale_buildings_v2/vale_house_s3_0", 23, 19, 3),
    "pottery": ("buildings/rescaled_v1/pottery_i400_s1_0", 41, 18, 3),       # rescaled 1.17x: doors to the game's size (2026-11-06)
    "atelier": ("buildings/rescaled_v1/atelier_i400_s1_0", 71, 23, 3),       # rescaled 0.81x
    "hall": ("buildings/rescaled_v1/hall_i400_s1_0", 38, 39, 3),             # rescaled 0.88x
    "fern": ("buildings/rescaled_v1/fern120_i400_s1_0", 9, 38, 3),           # the user's pick (v5 r3_3, 2026-11-06), rescaled 1.2x
    # The Old Studios: a row of three empty artists' studios (map doc, section 1); the
    # big one reopens at Lupini's 6 hearts. One west and two east of Lupini's stairs,
    # whose lane runs north between them (x 78-81).
    "studio_a": ("buildings/rescaled_v1/studio_large_i400_s1_0", 68, 38, 3),    # its own design (v7 w1, its wing's end inpainted, 0.9x)
    "studio_b": ("buildings/rescaled_v1/studio_b_i400_s1_0", 82, 38, 3),     # the user's picks v4 r3_1 (rescaled 0.93x)
    "studio_c": ("buildings/vale_buildings_v4/old_studio_r4_1", 88, 38, 3),  # and r4_1
    "emmett": ("buildings/rescaled_v1/emmett_i400_s1_0", 15, 61, 3),         # the user's pick (v3 s2_1), rescaled 0.92x
    # The decor plan's pieces of our own (PixelLab, props/vale_props_v1), set on the grass.
    # 2: the Gilded Tree on its own lawn between the amphitheatre and the lake (out of the
    # plaza: the user, 2026-11-06, "doesn't fit in the center of the town"); gold in fall
    "old_tree": ("props/vale_props_v1/old_tree_1_1", 35, 68, (1, 3, 4), 0),
    # 6: Lupini's painting spots (he paints at a different viewpoint each day): a stool
    # with his paint box, and the rock he wipes his brushes on
    "spot_clearing_stool": ("props/vale_props_v2/painters_stool_1_3", 6, 15, 1, 0),
    "spot_clearing_rock": ("props/vale_props_v2/paint_rock_clean", 9, 18, 1, 0),
    # (by the high street's hedge, side by side: the user, 2026-11-07, "can't be two
    # right next to each other" in the top right)
    "spot_street_stool": ("props/vale_props_v2/painters_stool_1_3", 43, 4, 1, 0),
    "spot_street_rock": ("props/vale_props_v2/paint_rock_clean", 46, 4, 1, 0),
    "spot_atelier_stool": ("props/vale_props_v2/painters_stool_1_3", 79, 16, 1, 0),
    "spot_atelier_rock": ("props/vale_props_v2/paint_rock_clean", 79, 19, 1, 0),
    "spot_meadow_stool": ("props/vale_props_v2/painters_stool_1_3", 28, 65, 1, 0),
    "spot_meadow_rock": ("props/vale_props_v2/paint_rock_clean", 31, 66, 1, 0),
    "spot_lake_stool": ("props/vale_props_v2/painters_stool_1_3", 84, 62, 1, 0),
    "spot_lake_rock": ("props/vale_props_v2/paint_rock_clean", 87, 62, 1, 0),
    "bus_shelter": ("props/vale_props_v1/bus_shelter_1_4", 3, 4, 2, 0),      # A: above the road's straight
    "vale_sign": ("props/vale_sign_gilded", 19, 4, 1, 0),   # gold lettering drawn on; lit at night (map property Light)         # A: where the high street starts
    "noticeboard": ("props/vale_props_v1/noticeboard_1_1", 41, 42, 1, 0),    # H: by the Hall's door
    "firewood_emmett": ("props/vale_props_v1/firewood_1_4", 11, 59, 1, 0),   # L: in his fenced side yard
    "canvases": ("props/vale_props_v1/canvases_1_2", 79, 22, 1, 0),          # F: Lupini's garden
    "sunflowers_1": ("props/vale_props_v1/sunflowers_2_1", 83, 16, 1, 0),
    "sunflowers_2": ("props/vale_props_v1/sunflowers_2_1", 88, 20, 1, 0),
    "rowboat": ("props/vale_props_v1/rowboat_2_1", 53, 63, 2, 0),            # M: on the lake
}
# Every door is the same size, about 36 px with its frame (the game's are 30 to 32), so
# no building's scale contradicts another's (the user, 2026-11-06); measured with ruled
# close-ups, and rescaled with tools/art/vale_resize.py where it wasn't.
# Each piece's art for the seasons it changes in (PixelLab colour-only edits of the
# pick). A version whose size differs from spring's (an edit that added a chimney) is
# refused and spring's used.
SEASONAL = {
    "vale_house": {"winter": "buildings/vale_winter_v2/vale_house_winter_0"},
    "pottery": {"winter": "buildings/vale_winter_v3/pottery_winter_0"},
    "atelier": {"winter": "buildings/vale_winter_v3/atelier_winter_0"},
    "fern": {"winter": "buildings/vale_winter_v3/fern_winter_0"},
    "emmett": {"winter": "buildings/vale_winter_v3/emmett_winter_0"},
    "hall": {"winter": "buildings/vale_winter_v3/hall_winter_0"},
    "studio_b": {"winter": "buildings/vale_winter_v3/studio_b_winter_0"},
    "studio_c": {"winter": "buildings/vale_winter_v3/studio_c_winter_0"},
    "old_tree": {s: f"props/vale_tree_seasons_v1/old_tree_{s}_0" for s in ("summer", "fall", "winter")},
}
# Doors into the interiors: door tile -> "x y Location" (where you arrive inside).
DOORS = {
    (28, 19): "15 28 blinxxy.MuseArt_ValeHouse", (29, 19): "15 28 blinxxy.MuseArt_ValeHouse",   # Bea's inn, its double door
    (44, 17): "8 21 blinxxy.MuseArt_ValePottery",                                                # Tobin's pottery
    (73, 23): "7 19 blinxxy.MuseArt_ValeAtelier",                                                # Lupini's atelier
    (44, 39): "14 21 blinxxy.MuseArt_ValeHall", (45, 39): "14 21 blinxxy.MuseArt_ValeHall",     # the Exhibition Hall, its double door
    (12, 38): "6 19 blinxxy.MuseArt_ValeFern", (17, 38): "21 19 blinxxy.MuseArt_ValeFern",      # Fern's cottage, and her seedhouse
    (18, 61): "10 16 blinxxy.MuseArt_ValeEmmett", (19, 61): "10 16 blinxxy.MuseArt_ValeEmmett",  # Emmett's cottage (its door straddles the two)
    (73, 38): "12 18 blinxxy.MuseArt_ValeStudio",                                               # the big Old Studio (Lupini's 6 hearts open it)
}
# The amphitheatre (the user's pick s1, 2026-11-06): PixelLab inpaint into the lower
# cliff (tools/art/vale_inpaint.py), its art covering map tiles x 31-43, y 46-55.
AMPHITHEATRE = ("amphitheatre/amph_s1_0", 31, 46)
# Features painted into the map with PixelLab inpaint, a real Town garden shown above
# them in the picture so they come out in the game's style (the user, 2026-11-06: the
# first parterre "doesn't have a Stardew Valley look and feel"; "needed to feel more
# like vanilla"). name: (art, crop box px in it, left tile x, top tile y). Their lawn
# is cut away so the map's seasonal grass shows; paths walk, the rest is solid
# (tools/art/vale_feature.py).
# Bits of a painting cut by hand, px boxes in the cut piece: the clay bank's muddy patch,
# a track to nowhere (the user, 2026-11-06).
# The nursery's log ran into the frame's edge and stopped flat: it goes, its glowing
# mushrooms (Fern's "humming" ones) stay, growing straight from the grass.
# Features whose winter is their spring art with bare ground recoloured, not the snow
# edit: nothing in them dies back, and the edit repainted the clay bank's barrow and
# filled its pit (the user, 2026-11-07).
WINTER_RECOLOUR = {"clay"}
# Features allowed to draw over the paving's edge (drawn over the farmer, so the lane
# still walks): the clay bank's drying rack stands on the lane's curb.
OVER_PAVING = set()
# Sand trimmed to a soft, wavy patch round a centre (cx, cy, rx, ry px in the piece):
# straight box cuts left sharp lines in the clay bank's sand (the user, 2026-11-07).
FEATURE_SAND_PATCH = {"clay": (84, 60, 58, 46)}
FEATURE_ERASE = {"clay": [(0, 0, 42, 38, "objects"), (41, 0, 80, 24, "objects")],   # (the barrow and the drying rack removed: the user, 2026-11-07)
                 "nursery": [(100, 159, 176, 176, "keep_blue"), (150, 146, 176, 159, "keep_blue")]}
FEATURES = {
    "parterre": ("features/parterre_v2/parterre_s13_0", (48, 144, 480, 288), 3, 39),   # I: the Lost Parterre (Fern)
    # (diff: cut against the bare render it was painted into, features/<name>_before.png)
    # B: Bea's tea garden; its rose arch rises over the high street's edge, so the rows
    # above y 10 are drawn over the farmer, who walks under them (the user, 2026-11-06)
    "tea": ("features/tea2/tea_s24_0", "diff:tea_before:22:3", 34, 7, 40, 19, 10),
    "clay": ("features/clay/clay_s31_0", "diff:clay_before:34:12", 50, 21, 57, 27),   # G: Tobin's clay bank
    # Fern's nursery, built into the cliff by her seedhouse: the night-flowering vine of her
    # 8-heart event on a trellis up the rock, pots on ledges cut into it (the user, 2026-11-06)
    "nursery": ("features/nursery/nursery_s71_0", "diff:nursery_before:2:24", 19, 28, 29, 38),
    "lakeside": ("features/lakeside2/lakeside_s53_0", "diff:lakeside2_before:30:52", 44, 54, 51, 71),   # M: the lakeside walk
}

W, H = 96, 76
VALE_PIECES = ["vale_sculpture"]           # modules whose patch_file(tmx_path, exports) runs on the written map (tools/vale_patch.py)
LAYERS = ["Back", "Back2", "Back3", "Buildings", "Buildings2", "Paths", "Front", "Front2", "AlwaysFront"]
STAMP_LAYERS = ["Back", "Back2", "Back3", "Buildings", "Buildings2", "Front", "AlwaysFront"]

CLIFF = (24, 43, 10, 6)
STAIRS = (19, 42, 4, 9)
RIVER = (89, 15, 9, 14)
FALLS = (91, 0, 7, 7)        # (from x 91: Town has a fence in the first two columns)
STONE_BR = (69, 51, 10, 7)
LOG_BR = (89, 12, 9, 3)

# Ground tiles, by gid in Town's tilesets (the three commonest grasses in Town,
# its sandy path, open water, and a plank of Pierre's front wall for the boxes).
GRASS, GRASS_MID = 176, 352
DIRT, WATER, WALL = 228, 1230, 2630

RIVER_X = 56                 # the river's left edge where it falls over the cliffs (the falls are 9 wide)
TOP_FALLS_X = 78             # the waterfall it comes in by, in the north-east
CLIFF_A, CLIFF_B = 28, 48    # top rows of the two cliffs (6 tall)
TREE_GIDS = None             # set from the Paths tileset: oak, maple, pine


def load_town(path):
    root = ET.parse(path).getroot()
    layers = {}
    for layer in root.findall("layer"):
        w = int(layer.get("width"))
        data = layer_data(layer.find("data"))
        layers[layer.get("name")] = [data[i * w:(i + 1) * w] for i in range(len(data) // w)]
    text = open(path, encoding="utf-8").read()
    tilesets = re.findall(r"  <tileset .*?</tileset>", text, re.S)
    tilesets = [re.sub(r'source="Maps\\', 'source="', t) for t in tilesets]
    paths_first = int(re.search(r'<tileset firstgid="(\d+)" name="Paths"', text).group(1))
    return layers, tilesets, paths_first


def main():
    town_path, assets = sys.argv[1], sys.argv[2]
    town, tilesets, paths_first = load_town(town_path)
    g = {l: [[0] * W for _ in range(H)] for l in LAYERS}

    def tget(layer, x, y):
        return town.get(layer, [[0]])[y][x] if layer in town else 0

    def stamp(src, x, y, layers=STAMP_LAYERS, clip=None):
        sx, sy, sw, sh = src
        for j in range(sh):
            for i in range(sw):
                tx, ty = x + i, y + j
                if not (0 <= tx < W and 0 <= ty < H) or (clip and not clip(tx, ty)):
                    continue
                for l in layers:
                    v = tget(l, sx + i, sy + j)
                    if l == "Back" or v:
                        g[l][ty][tx] = v
                    elif l != "Back":
                        g[l][ty][tx] = 0

    def fill(layer, x0, y0, x1, y1, gid):
        for y in range(max(0, y0), min(H, y1)):
            for x in range(max(0, x0), min(W, x1)):
                g[layer][y][x] = gid

    grass, dirt, water, wall = GRASS, DIRT, WATER, WALL
    paint_ground(g, sys.argv[3] if len(sys.argv) > 3 else os.path.dirname(town_path))
    lay_road(g)

    # The two cliffs, right across, then the stairs cut through them.
    crnd = random.Random(3)
    for top in (CLIFF_A, CLIFF_B):
        cliff(g, top, crnd, keep_water=True)
    # The stairs are PixelLab art (tools/art/vale_stairs.py), laid with the buildings
    # in place_buildings(); VALE_NO_STAIRS leaves the cliff unbroken, for rendering
    # the bare cliff that art is painted into.

    # Waterfalls over each cliff and the bridges: only the pieces' own tiles (the
    # falling water and its foam, the rock behind it; the bridge's deck, rails and
    # posts), never the Town ground they were cut from (the user, 2026-11-04).
    for top in (CLIFF_A, CLIFF_B):
        waterfall(g, tget, RIVER_X + 2, top)
    stone_bridge(g, tget, 39)        # the plaza to the Old Studios (the lower east side is reached by its own stairs)
    log_bridge(g, 22)
    log_bridge(g, 54)        # planks across the pool under the lower falls (the user's idea, 2026-11-04): hides the
                             # right-angle joins where the lake's top bank meets the river, and links the lower terrace
    bank_depth(g)            # the depth row of the straight banks (turns are paint_shores')

    place_buildings(g, assets)
    # The dressing (the user's decor plan, 2026-11-06): the game's props and the
    # Paths markers it turns into trees, bushes and grass (tools/vale_decor.py).
    from vale_decor import decor
    decor(g, GROUND, paths_first, W, H)
    # Doors: clicking a building's door takes you in (tools/build_vale_interiors.py
    # builds the rooms; each room's own Warp brings you back out here).
    from vale_decor import TILE_PROPS
    for (dx, dy), target in DOORS.items():
        TILE_PROPS[("Buildings", dx, dy)] = {"Action": f"Warp {target}"}

    # Woods all round, on the Paths layer: oak (9), maple (10), pine (11).
    rnd = random.Random(1)
    def tree_ok(x, y):
        return GROUND.get((g["Back"][y][x], g["Buildings"][y][x]), ("?",) * 4) in (("G",) * 4, ("D",) * 4) and not g["Buildings"][y][x]             and not g["Front2"][y][x] and g["Back"][y][x] != water        # (not behind a roof either)
    for y in range(H):
        for x in range(W):
            edge = x < 5 or x > 90 or y < 2 or y > 72
            if edge and (x + y) % 2 == 0 and tree_ok(x, y) and not (3 <= y <= 12 and x < 6):       # the high street runs out west, clear of the woods
                g["Paths"][y][x] = paths_first + rnd.choice((9, 10, 11))
    # No trees on or against a building, none hiding its front, and nothing spawning on
    # its door's approach (the user, 2026-11-08: trees walled in the east Old Studio).
    from vale_yards import clear
    clear(g, paths_first, TILE_PROPS, W, H)

    props = {"Outdoors": "T", "Spring_Objects": "T", "Summer_Objects": "T", "Fall_Objects": "T", "Winter_Objects": "T",
             "BrookSounds": f"{RIVER_X + 4} 3 0",
             "Light": "20 3 4 22 3 4"}             # the entrance sign's gold letters glow at dusk (two sconce lights)
    tmx_path = os.path.join(assets, "GildedVale.tmx")
    write(tmx_path, g, props, tilesets)
    # The pieces patched into the written map in place (tools/vale_patch.py).
    import importlib
    for name in VALE_PIECES:
        importlib.import_module(name).patch_file(tmx_path, sys.argv[3] if len(sys.argv) > 3 else os.path.dirname(town_path))


# The Town ledge's cliff, as a rule rather than a block (learned from Town x 23-46,
# y 43-48, 2026-11-04): the face alternates between two columns; the top edge is
# almost always one piece; three face pieces are rarer variants of their neighbours;
# the three rows under the face are the ground's shading.
CLIFF_LIP = ((103, 0.9), (308, 0.1))
CLIFF_FACE = ((468, 493, 518, 543), (469, 494, 519, 544))            # by column parity, rows 1-4
CLIFF_FACE_VARIANT = {468: 447, 494: 448, 518: 449}                  # an odd stone here and there
CLIFF_SHADE = ((327,), (356, 355), (406, 405, 404))                  # Back, rows 3-5


def is_water(g, x, y):
    return "W" in GROUND.get((g["Back"][y][x], g["Buildings"][y][x]), ())


def cliff(g, top, rnd, keep_water=False):
    """A cliff right across the map, its top edge on row top (6 rows: edge, three face
    rows over the shading, two rows of shading below). Where the river runs under
    it, the water below the face stays (the waterfall's pool)."""
    for x in range(W):
        river = keep_water and GROUND.get((g["Back"][top + 5][x], g["Buildings"][top + 5][x])) == ("W",) * 4
        r = rnd.random()
        beside_stairs = any(sx - 2 <= x <= sx + 5 and st == top for sx, st in STAIR_SPOTS)
        g["Buildings"][top][x] = CLIFF_LIP[0][0] if r < CLIFF_LIP[0][1] or beside_stairs else CLIFF_LIP[1][0]
        if not river:
            g["Back"][top][x] = GRASS      # the edge piece is see-through at the top: grass behind it, never shore
        for k, gid in enumerate(CLIFF_FACE[x % 2]):
            if gid in CLIFF_FACE_VARIANT and rnd.random() < 0.08:
                gid = CLIFF_FACE_VARIANT[gid]
            g["Buildings"][top + 1 + k][x] = gid
            g["Front"][top + 1 + k][x] = 0
        if river:
            continue
        for k, opts in enumerate(CLIFF_SHADE):
            if keep_water and is_water(g, x, top + 3 + k):
                continue                      # the riverbank stays the riverbank
            if any(sx + 1 <= x <= sx + 2 and st == top for sx, st in STAIR_SPOTS) and k == 2:
                continue                      # under the flight's last steps
            g["Back"][top + 3 + k][x] = opts[0]          # the main piece only: its variants go plain white in winter
        g["Back"][top + 1][x] = g["Back"][top + 2][x] = GRASS


SHADOWS = range(5464, 5939)
WATERFALLS = range(5939, 6839)
OUTDOORS2 = range(4344, 5464)
TOWN_SHEET = range(2040, 4344)
STAIR_SPOTS = ((30, 28), (78, 28), (50, 48), (84, 48))   # left column and cliff top of each flight (CLIFF_A 28, CLIFF_B 48)
STAIRS_PICK = "art/gilded_vale/stairs/stairs_s2_0.png"    # the flight, 4 x 9 tiles from the row above the cliff's edge

STAIR_TREAD_SWAP = {2810: 2808}         # a grey, worn tread that showed as a dark spot; its plain neighbour
LOG_DECK = (780, 781, 782, 783)          # the log footbridge: left end, middle, middle, right end
# Banks that run straight repeat one piece along their whole length, so each is the
# piece that joins itself seamlessly (compared side by side, 2026-11-04). The east
# bank's commonest piece is a wedge that repeated as a sawtooth (the user's
# screenshot); this is the mirror of the west bank's (5177, 284).
SHORE_PIN = {("W", "S", "W", "S"): (5178, 285), ("S", "W", "S", "W"): (5177, 284)}
# Cobble edges the learner can't place (in Town they only sit beside pieces it doesn't
# know): the plain top edge, and the top-right corner, the mirror of 2648 (2026-11-06).
COBBLE_PIN = {("G", "G", "C", "C"): (2651, 0), ("G", "G", "C", "G"): (2650, 0)}
LILY_PADS = set(range(1294, 1298)) | set(range(1319, 1323))   # shore overlays with a lily pad (they lined up along banks)


def waterfall(g, tget, x, top):
    """The Town waterfall (Town x 91-97, y 0-6) over one of our cliffs, its top edge on
    our cliff's top row: the falling water and foam (Waterfalls sheet, Front and
    AlwaysFront) and the wet rock behind it (outdoors sheet 2, Buildings). Our own
    cliff stays either side; the face row the pool covers is cleared."""
    sx, sy, sw, sh = FALLS
    for j in range(sh):
        for i in range(sw):
            tx, ty = x + i, top + j
            for layer in ("Front", "AlwaysFront"):
                v = tget(layer, sx + i, sy + j)
                if v in WATERFALLS:
                    g[layer][ty][tx] = v
            v = tget("Buildings", sx + i, sy + j)
            if v in OUTDOORS2 or (j == 0 and v == 126):     # 126: the water spilling over the edge
                g["Buildings"][ty][tx] = v
    for i in range(2, 6):
        g["Buildings"][top + 4][x + i] = 0          # the pool, where our face's last row was


def stairs(g, tget, x, top):
    """The Town's stone stairs down a cliff (Town x 19-22), fitted to our cliff, whose
    edge is the same height both sides (Town's ledge is a row lower left of its stairs
    than right of them, so each side is lined up with its own ledge):
      treads    Town x 20-21, rows 43-51 -> our rows top..top+8 (Back)
      left wall Town x 19, rows 44-48 -> top..top+4, and its cap (row 43) on top-1
      right wall Town x 22, rows 43-47 -> top..top+4
      and the cliff columns either side end as the game ends them: Town x 18 rows
      44-48 on our x-1, Town x 23 rows 43-47 on our x+4.
    The stairs' outer edge tiles (Town x 19 and 22 on Back) are left out: they're the
    town sheet's and go blank white in winter (the user's screenshots, 2026-11-04)."""
    for ty in range(top - 1, top + 6):
        for tx in range(x, x + 4):
            g["Buildings"][ty][tx] = g["Front"][ty][tx] = 0       # our cliff, where the stairs cut it
    for k in range(9):                                           # treads
        for i in (1, 2):
            v = tget("Back", 19 + i, 43 + k)
            if v in TOWN_SHEET or v in OUTDOORS2:
                g["Back"][top + k][x + i] = STAIR_TREAD_SWAP.get(v, v)
    for k in range(5):
        g["Buildings"][top + k][x] = tget("Buildings", 19, 44 + k)          # left wall
        g["Buildings"][top + k][x + 3] = tget("Buildings", 22, 43 + k)      # right wall
        g["Buildings"][top + k][x - 1] = tget("Buildings", 18, 44 + k)      # the cliff's end, left
        g["Buildings"][top + k][x + 4] = tget("Buildings", 23, 43 + k)      # and right
    g["Buildings"][top - 1][x] = tget("Buildings", 19, 43)                # the left wall's cap
    g["Front"][top - 1][x] = tget("Front", 19, 43)


def stone_bridge(g, tget, y):
    """The Town's stone bridge (Town x 70-77, y 52-56: the town sheet's deck, rails and
    the posts in the water), its deck on rows y + 1 and y + 2. Narrowed from 8 columns
    to 6 (its ends, its middle post, and one arch column each side) so it spans our
    river (water x 60-63) bank to bank, x 59-64."""
    sx, sy = STONE_BR[0] + 1, STONE_BR[1] + 1
    cols = (0, 1, 3, 4, 6, 7)
    for j in range(5):
        for i, src in enumerate(cols):
            tx = RIVER_X + 3 + i
            if j < 4:
                g["Buildings"][y + j][tx] = g["Front"][y + j][tx] = 0   # no shore pieces over the deck and rails (the bank below keeps its own)
            for layer in ("Back", "Buildings", "Front"):
                if layer == "Back" and j not in (1, 2):
                    continue                  # the posts stand in the town sheet's own water, a different blue in winter
                v = tget(layer, sx + src, sy + j)
                if v in TOWN_SHEET:
                    g[layer][y + j][tx] = v


def log_bridge(g, y):
    """The log footbridge across the river on row y, from bank to bank."""
    xs = [x for x in range(RIVER_X - 4, RIVER_X + 14) if is_water(g, x, y)]
    if not xs:
        return
    x0, x1 = xs[0] - 1, xs[-1] + 1
    for x in range(x0, x1 + 1):
        g["Buildings"][y][x] = LOG_DECK[0] if x == x0 else LOG_DECK[3] if x == x1 else LOG_DECK[1 + (x - x0) % 2]
        if x0 < x < x1:
            g["Back"][y][x] = WATER      # plain water under the deck, as in Town (some shore tiles aren't walkable)


SHORE_V = []                 # the corner terrain, kept for finding shore corners (tools/art/vale_shore.py)
GROUND = {}                  # (Back, Buildings) stack -> corner pattern, for every stack the painter can lay


def paint_ground(g, exports):
    """The ground painted the way the game's maps are: terrain is set on the corners
    of the tile grid (G grass, D dark grass, S dirt and sand, W water) and each tile
    is a vanilla tile whose four corners match (tools/vale_terrain.py). Paths are
    ragged strokes, the river meanders, water always has a sand rim (the game never
    puts grass straight against water), and the darker grass is soft patches kept
    away from the paths, as in the Forest."""
    from vale_terrain import Table
    table = Table(exports)
    GROUND.update(table.corners)
    GROUND.update({stack: pat for pat, stack in COBBLE_PIN.items()})
    rnd = random.Random(11)
    V = [["G"] * (W + 1) for _ in range(H + 1)]

    def noise(scale, seed):
        r = random.Random(seed)
        gw, gh = W // scale + 3, H // scale + 3
        grid = [[r.random() for _ in range(gw)] for _ in range(gh)]

        def at(x, y):
            fx, fy = x / scale, y / scale
            x0, y0 = int(fx), int(fy)
            tx, ty = fx - x0, fy - y0
            tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
            a = grid[y0][x0] * (1 - tx) + grid[y0][x0 + 1] * tx
            b = grid[y0 + 1][x0] * (1 - tx) + grid[y0 + 1][x0 + 1] * tx
            return a * (1 - ty) + b * ty
        return at

    wobble = noise(3, 5)

    def dist_seg(px, py, ax, ay, bx, by):
        dx, dy = bx - ax, by - ay
        t = 0 if dx == dy == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
        return ((px - ax - t * dx) ** 2 + (py - ay - t * dy) ** 2) ** 0.5

    def stroke(points, radius, cls, rag=0.35):
        xs, ys = [p[0] for p in points], [p[1] for p in points]
        for y in range(max(0, int(min(ys) - radius - 2)), min(H + 1, int(max(ys) + radius + 3))):
            for x in range(max(0, int(min(xs) - radius - 2)), min(W + 1, int(max(xs) + radius + 3))):
                d = min(dist_seg(x, y, *a, *b) for a, b in zip(points, points[1:]))
                if d <= radius + (wobble(x, y) - 0.5) * 2 * rag:
                    V[y][x] = cls

    def area(x0, y0, x1, y1, cls, rag=0.5):
        for y in range(max(0, y0 - 2), min(H + 1, y1 + 3)):
            for x in range(max(0, x0 - 2), min(W + 1, x1 + 3)):
                ex, ey = max(x0 - x, 0, x - x1), max(y0 - y, 0, y - y1)
                if (ex * ex + ey * ey) ** 0.5 <= 0.5 + (wobble(x, y) - 0.5) * 2 * rag:
                    V[y][x] = cls

    def around(x, y, r):
        return [(i, j) for j in range(max(0, y - r), min(H + 1, y + r + 1)) for i in range(max(0, x - r), min(W + 1, x + r + 1))]

    # Water. The game draws diagonal banks only on the north side of water (land above
    # it); a bank that faces south runs straight east-west (the corner pieces a
    # south-facing staircase needs barely exist, and showed as notches and reeds in a
    # row). So the river comes in from under the woods, runs straight with square
    # bends, and the lake has a rounded top and a flat south shore, like the Forest pond.
    def water_rect(x0, y0, x1, y1):
        for y in range(max(0, y0), min(H + 1, y1 + 1)):
            for x in range(max(0, x0), min(W + 1, x1 + 1)):
                V[y][x] = "W"
    water_rect(80, 0, 84, 12)          # in from the north-east, under the woods
    water_rect(60, 8, 84, 12)          # west along the upper terrace, above Lupini's
    water_rect(60, 8, 64, 60)          # down the terraces at the falls' width (water x 60-63)

    def round_corner(cx, cy, r, dx, dy, cls):
        """Cut the square corner at vertex (cx, cy) on a clean 45-degree diagonal (one
        step per row: the rhythm the game's diagonal bank pieces are drawn for; an arc
        stepped unevenly and broke into lumps): the triangle of the r x r square
        reaching (dx, dy) from it, nearest the corner, becomes cls."""
        for j in range(r + 1):
            for i in range(r + 1):
                x, y = cx + dx * i, cy + dy * j
                if 0 <= x <= W and 0 <= y <= H and i + j < r:
                    V[y][x] = cls

    # Only corners where land is above the water can be rounded (the game has the
    # pieces for those shores); corners facing south stay square.
    round_corner(60, 8, 4, 1, 1, "G")       # the river's outer corner where it turns south
    round_corner(80, 8, 4, -1, -1, "W")     # the inner bend below where it comes in
    round_corner(60, 58, 3, -1, -1, "W")    # where the river opens into the lake
    round_corner(64, 58, 3, 1, -1, "W")

    # The lake: a rounded-square dome, long flat runs with short rounded corners (a
    # 45-degree curve came out as a staircase of one-tile steps), widening down to a
    # flat south shore, like the Forest pond.
    # (Now: a rectangle whose top corners are cut on a clean 45-degree diagonal, five
    # steps long, then long straight banks; the bottom corners stay square, as south-
    # facing corners must.)
    LX0, LX1, LY0, LY1, CUT = 50, 82, 57, 71, 5
    # The sides jog in and out a tile every few rows, as the game's vertical banks do
    # (the Town river, the Forest's): a ruler-straight side has no step for the game's
    # rocky pieces to sit on.
    # The jogs only ever step outward going down (the water widening), so every step
    # faces north: a step facing south has no piece in the game (it came out as
    # fragments at the bottom-left).
    def jogs(seed, steps=2):
        r, out, off = random.Random(seed), {}, steps
        first = LY0 + CUT
        rows = LY1 - first + 1
        # steps at least four rows apart: a one-row run between two steps read as a jag
        options = [c for c in __import__("itertools").combinations(range(3, rows - 2), steps)
                   if all(b - a >= 5 for a, b in zip(c, c[1:])) and c[-1] + 1 < rows - 2]
        cuts = list(r.choice(options)) if options else [rows // 2]
        for y in range(LY0, LY1 + 1):
            # each jog is a short 45-degree diagonal, out a tile on two rows running, as the
            # Town river's are: a single step dropping straight again was a right angle
            out[y] = sum(min(2, max(0, y - (first + c) + 1)) for c in cuts)
        return out
    left, right = jogs(41), jogs(43)
    for y in range(LY0, LY1 + 1):
        for x in range(LX0 - 4, LX1 + 5):
            inside = (y - LY0) + (x - LX0) >= CUT and (y - LY0) + (LX1 - x) >= CUT and LX0 <= x <= LX1
            widened = y - LY0 >= CUT and LX0 - left[y] <= x <= LX1 + right[y]
            if inside or widened:
                V[y][x] = "W"

    # A sand rim round all water.
    rim = [(x, y) for y in range(H + 1) for x in range(W + 1)
           if V[y][x] != "W" and any(V[j][i] == "W" for i, j in around(x, y, 1))]
    for x, y in rim:
        V[y][x] = "S"

    # Smooth the water's outline where it would need a shoreline corner the game
    # barely uses (fewer than 15 uses: they showed as hard notches), then regrow the rim.
    def strength(pat):
        c = table.by_pattern.get(pat)
        return c[0][1] if c else 0
    # One pass only (repeating it spread the water across the map), and never on the
    # river's measured stretch down the terraces, which must keep the falls' width.
    flips = []
    for y in range(H):
        for x in range(W):
            if 57 <= x <= 66 and 6 <= y <= 60:
                continue
            corners = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
            pat = tuple(V[j][i] for i, j in corners)
            if set(pat) != {"S", "W"} or strength(pat) >= 15:
                continue
            n = pat.count("W")
            flips += [((i, j), "W" if n == 3 else "S") for (i, j), c in zip(corners, pat) if (n == 3) != (c == "W")]
    for (i, j), c in flips:
        V[j][i] = c
    for y in range(H + 1):
        for x in range(W + 1):
            if V[y][x] in "GD" and any(V[j][i] == "W" for i, j in around(x, y, 1)):
                V[y][x] = "S"

    # The sand rim wanders between one and two tiles wide, as the game's do: at a
    # steady one tile the grass met it in a dead-straight edge (the user, 2026-11-04).
    rim_width = noise(3, 31)
    widen = [(x, y) for y in range(H + 1) for x in range(W + 1)
             if V[y][x] in "GD" and rim_width(x, y) > 0.45
             and any(V[j][i] == "W" for i, j in around(x, y, 2))]
    for x, y in widen:
        V[y][x] = "S"
    # Room for the banks' depth row (bank_depth): two tiles of sand above every north
    # bank, and beside every side bank.
    deepen = []
    for y in range(1, H):
        for x in range(1, W):
            if V[y][x] == "S" and V[y + 1][x] == "W":
                deepen += [(x, y - 1), (x, y - 2)]
            if V[y][x] == "S" and V[y][x + 1] == "W":
                deepen += [(x - 1, y), (x - 2, y)]
            if V[y][x] == "S" and V[y][x - 1] == "W":
                deepen += [(x + 1, y), (x + 2, y)]
    for x, y in deepen:
        if 0 <= x <= W and 0 <= y <= H and V[y][x] in "GD":
            V[y][x] = "S"

    # Paths: ragged strokes of dirt.
    # Dirt is for the wild trails only; the village itself is paved (lanes below).
    for pts, r in (
        ([(57, 19), (57, 23)], 1.2),                                  # the upper bridge's west approach, off the lane
        ([(64.5, 21.5), (65.5, 21.5)], 1.0),                          # and its east one
        ([(54.5, 41.5), (56, 41.5)], 1.0),                            # the middle bridge's approaches
        ([(65, 41.5), (66, 41.5)], 1.0),
        ([(52, 55.4), (52, 56), (47, 56.5)], 1.1),                    # the lower stairs' foot, along the lake's rim
        ([(86, 55.4), (86.5, 64)], 1.0),                              # the east stairs down to the overlook
        ([(8, 12), (6.5, 14), (6.5, 20)], 0.9),                       # the Painter's clearing
    ):
        stroke(pts, r, "S")
    # Each lower flight's approaches: the stairs' width, centred on them, stopping just
    # short of the steps with a ragged end like any path's (the user, 2026-11-04). The
    # upper flights are paved right up to the steps, as the Town square's stairs are.
    for sx, top in STAIR_SPOTS:
        if top == CLIFF_B:
            stroke([(sx + 2, top + 6), (sx + 2, top + 9)], 1.5, "S")        # starting right at the bottom step

    # The paved village (Pelican Town's cobble, tools/vale_terrain.py COBBLE): straight
    # lanes and squares with curbed edges, as the Town square has; the game has no
    # diagonal or ragged cobble, so every lane is a rectangle of grid corners.
    def lane(x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                V[y][x] = "C"
    for box in (
        (19, 6, 54, 9),         # the high street, from the bus road's turning circle (lay_road)
        (52, 6, 54, 21),        # down past the pottery
        (44, 19, 54, 21),       # to the pottery's door
        (20, 9, 22, 22),        # round to the inn
        (20, 20, 34, 22),       # the inn's front
        (31, 20, 33, 29),       # down to the west stairs
        (67, 24, 82, 26),       # the atelier's front, from the upper bridge's bank
        (79, 24, 81, 29),       # Lupini's stairs
        (31, 33, 33, 41),       # the foot of the west stairs
        (31, 40, 54, 46),       # the plaza before the Exhibition Hall
        (29, 40, 31, 42),       # west to the Lost Parterre's gate (its gravel walk leads on to Fern's door)
        (68, 40, 93, 42),       # east of the middle bridge: the Old Studios' row
        (79, 33, 81, 41),       # the foot of Lupini's stairs
        (85, 40, 87, 49),       # on to the east stairs
        (51, 45, 53, 49),       # the plaza to the lower stairs
        (22, 55, 45, 58),       # the amphitheatre's forecourt, up to the stage
        (22, 56, 24, 64),       # down past Emmett's cottage
        (14, 62, 24, 64),       # to his door
    ):
        lane(*box)
    # Cobble and dirt never touch in the game (its curbs meet grass): a strip of grass
    # parts them, so a trail starts a step away from the paving, as the Town square's do.
    for y in range(H + 1):
        for x in range(W + 1):
            if V[y][x] == "S" and any(V[j][i] == "C" for i, j in around(x, y, 1)):
                V[y][x] = "G"

    # Darker grass: soft patches, kept two tiles from dirt and water.
    dark = noise(7, 21)
    import json
    road = {tuple(map(int, k.split(","))) for k in json.load(open(ROAD_LAYOUT))["tiles"]}   # its tiles carry plain grass
    for name, (art, box, left, top, *rest) in FEATURES.items():   # nor under a painted feature (its lawn is cut to the map's)
        x1, y1 = rest[:2] if rest else (left + (box[2] - box[0]) // 16 - 1, top + (box[3] - box[1]) // 16 - 1)
        road |= {(x, y) for x in range(left - 1, x1 + 3) for y in range(top - 1, y1 + 3)}
    for y in range(H + 1):
        for x in range(W + 1):
            near_cliff = any(top - 2 <= y <= top + 9 for top in (CLIFF_A, CLIFF_B))   # plain grass round cliffs and their shadow
            if V[y][x] == "G" and dark(x, y) >= 0.6 and not near_cliff and not any(V[j][i] in "SWC" for i, j in around(x, y, 2))                     and not any((i, j) in road for i, j in around(x, y, 3)):
                V[y][x] = "D"

    # No lone corners: a single corner of one terrain inside another (a tuft of grass
    # in the road, a speck of dirt in the grass) becomes its surroundings.
    for _ in range(2):
        for y in range(1, H):
            for x in range(1, W):
                n = {V[y - 1][x], V[y + 1][x], V[y][x - 1], V[y][x + 1]}
                if len(n) == 1 and V[y][x] not in n and "W" not in n | {V[y][x]}:
                    V[y][x] = n.pop()

    # No checkerboard diagonals: the game has no tile for them.
    for _ in range(3):
        for y in range(H):
            for x in range(W):
                tl, tr, bl, br = V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1]
                if tl == br and tr == bl and tl != tr:
                    keep = next(c for c in "WSCG" if c in (tl, tr))
                    V[y][x] = V[y][x + 1] = V[y + 1][x] = V[y + 1][x + 1] = keep

    misses = collections.Counter()
    for y in range(H):
        for x in range(W):
            pat = (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
            plain = len(set(pat)) == 1
            # Plain ground keeps to its main tile (grass with its tufts), so it
            # doesn't speckle; edges vary among the game's common pieces.
            if pat == ("C",) * 4:
                # The square's paving: a 2 x 2 block of stones repeated (rows of 2776 2777
                # over 2808 2809), with the odd worn stone, as in the Town square.
                r = rnd.random()
                stack = ((2681 if r < 0.08 else 2810 if r < 0.1 else
                          (2776, 2777, 2808, 2809)[(x % 2) + 2 * (y % 2)]), 0)
            elif plain:
                bare = [c for c in table.by_pattern.get(pat, []) if c[0][1] == 0][:2 if pat == ("G",) * 4 else 1]
                stack = bare[0][0] if len(bare) == 1 or rnd.random() < bare[0][1] / (bare[0][1] + bare[1][1]) else bare[1][0]
            elif pat in SHORE_PIN:
                stack = SHORE_PIN[pat]
            elif pat in COBBLE_PIN:
                stack = COBBLE_PIN[pat]
            elif "W" in pat:
                # Shores: the plain version where the game has one (the commonest east
                # bank carries a lily pad, which lined up in a row), else its overlay.
                cands = [c for c in table.by_pattern.get(pat, []) if c[0][1] not in LILY_PADS] or table.by_pattern.get(pat, [])
                bare = [c for c in cands if c[0][1] == 0]
                stack = (bare[0][0] if bare and bare[0][1] >= cands[0][1] * 0.25 else cands[0][0]) if cands else None
                if not cands or cands[0][1] < 5:
                    stack = None     # too rare: its few examples are rocks and reeds; use the nearest common shore
            else:
                # Each kind of edge is always the game's main piece for it, so neighbouring
                # edges are the same shade (variants beside each other showed hard seams).
                bare = [c for c in table.by_pattern.get(pat, []) if c[0][1] == 0]
                stack = bare[0][0] if bare else table.pick(pat, rnd, top=1)
            if stack is None:
                misses[pat] += 1
                best = min((q for q in table.by_pattern if set(q) <= set(pat) and table.by_pattern[q][0][1] >= 5),
                           key=lambda q: sum(a != b for a, b in zip(q, pat)), default=("G",) * 4)
                stack = table.pick(best, rnd, top=1)
            g["Back"][y][x], g["Buildings"][y][x] = stack
    if misses:
        print("patterns with no vanilla tile (nearest used):", dict(misses.most_common(8)))
    # (fit_edges(), choosing edges by their vanilla neighbours, was tried and made the
    # banks worse: the game's curved corners are one-off arrangements, never reused.)
    paint_shores(g, V, table)
    vertical_banks(g, V, table)
    SHORE_V.clear()
    SHORE_V.extend(V)


WATER_BACKS = {1229, 1230, 1243, 1244, 1245, 1246, 1247, 1248, 1249, 1250, 1272, 1273, 1274, 1275, 1280}   # the water tiles the depth pieces are laid over
SHORE_DECOR = set(range(1290, 1326))
MOUNTAIN_DIRT = {609, 610, 611, 709, 710, 711, 809, 810, 811}   # bank tops drawn for the Mountain's darker dirt, not our sand

# The game's vertical banks are a five-tile cycle, read off the Town river and the
# Forest's shores (2026-11-04): together the five draw one continuous wavy rocky edge.
# One piece repeated stacked into separate "pillars" (the user's close-ups).
LEFT_LAND_CYCLE = ((1230, 5242), (1230, 5258), (5211, 5274), (5227, 234), (1230, 234))     # land left, water right
RIGHT_LAND_CYCLE = ((1230, 5290), (1230, 5306), (5210, 5322), (5226, 236), (1230, 236))    # water left, land right


# A run's ends, as the game has them (same rivers): the tile above a run is a cap that
# leads the wave in; a run that ends where the bank steps outward finishes on the
# side bank's plain end piece.
LEFT_LAND_CAP, RIGHT_LAND_CAP = (5180, 234), (1271, 236)
LEFT_LAND_END, RIGHT_LAND_END = (5177, 284), (5178, 285)


def vertical_banks(g, V, table):
    """Every vertical run of bank tiles, laid as the game lays them: its cap above,
    the step recipe below if the bank steps outward there, and in between the run of
    cycle pieces chosen (by dynamic programming) as the sequence the game itself stacks
    most often, from the cap down to whatever is below. The game skips cycle pieces to
    land on the right one: the piece before an end piece is always the cycle's fourth
    (counting blindly left the wrong piece over the end, and a right angle in the
    water, the user's marks, 2026-11-04)."""
    import math
    pat = lambda x, y: (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1]) if 0 <= x < W and 0 <= y < H else None
    at = lambda x, y: (g["Back"][y][x], g["Buildings"][y][x])
    sand = lambda cx, cy: pat(cx, cy) == ("S", "S", "S", "S")
    sides = ((("S", "W", "S", "W"), LEFT_LAND_CYCLE, LEFT_LAND_CAP, LEFT_LAND_END, ("S", "S", "S", "W")),
             (("W", "S", "W", "S"), RIGHT_LAND_CYCLE, RIGHT_LAND_CAP, RIGHT_LAND_END, ("S", "S", "W", "S")))

    def link(a, b):
        k = table.down[(a, b)]
        return math.log(1 + k) if k else -6.0

    for x in range(W):
        for key, cycle, cap, end, cap_pat in sides:
            y = 0
            while y < H:
                if pat(x, y) != key:
                    y += 1
                    continue
                top = y
                while y < H and pat(x, y) == key:
                    y += 1
                n = y - top
                if pat(x, top - 1) == cap_pat:
                    g["Back"][top - 1][x], g["Buildings"][top - 1][x] = cap
                below = pat(x, y)
                step = False
                # The step recipe (the Town river x 89, the Forest x 3), with the land side's
                # depth pieces carrying the rock's top over the sand.
                if key == ("S", "W", "S", "W") and below == ("S", "W", "W", "W"):
                    step = True
                    g["Back"][y][x], g["Buildings"][y][x] = (1267, 0)
                    if pat(x - 1, y) == ("S", "S", "S", "W"):
                        g["Back"][y][x - 1], g["Buildings"][y][x - 1] = (5192, 284)
                    if sand(x - 1, y - 1):
                        g["Back"][y - 1][x - 1], g["Buildings"][y - 1][x - 1] = (1244, 184)
                    if sand(x - 2, y):
                        g["Back"][y][x - 2], g["Buildings"][y][x - 2] = (1244, 184)
                if key == ("W", "S", "W", "S") and below == ("W", "S", "W", "W"):
                    step = True
                    g["Back"][y][x], g["Buildings"][y][x] = (1268, 0)
                    if pat(x + 1, y) == ("S", "S", "W", "S"):
                        g["Back"][y][x + 1], g["Buildings"][y][x + 1] = (5195, 285)
                    if sand(x + 1, y - 1):
                        g["Back"][y - 1][x + 1], g["Buildings"][y - 1][x + 1] = (1246, 186)
                # The run itself: the best-joined sequence from above to below.
                above_st = at(x, top - 1) if top else None
                below_st = at(x, y) if y < H else None
                cands = list(cycle)                    # the end piece only ever goes last, before a step
                cands_last = ([end, (1243, 285)] if key[0] == "W" else [end]) if step else cands
                best = {c: ((link(above_st, c) if above_st else 0.0), [c]) for c in cands} if n > 1 else                        {c: ((link(above_st, c) if above_st else 0.0), [c]) for c in cands_last}
                for i in range(1, n):
                    pool = cands_last if i == n - 1 else cands
                    nxt = {}
                    for c in pool:
                        sc, path = max(((s0 + link(p, c), pth) for p, (s0, pth) in best.items()), key=lambda t: t[0])
                        nxt[c] = (sc, path + [c])
                    best = nxt
                if below_st:
                    best = {c: (sc + link(c, below_st), pth) for c, (sc, pth) in best.items()}
                _, path = max(best.values(), key=lambda t: t[0])
                for i, c in enumerate(path):
                    g["Back"][top + i][x], g["Buildings"][top + i][x] = c


def paint_shores(g, V, table):
    """Every cell near water, chosen by its neighbourhood (Table.by_context): the stacks
    the game uses where the terrain round a cell is the same as here, so straight
    banks get their two-deep pieces and diagonal steps the game's diagonal ones.
    Raster order; among the game's choices for a context, the one it most often puts
    beside what's already left of and above it, so alternating pieces alternate.
    Plain water stays plain (its decorations are left out)."""
    import math
    stats = collections.Counter()
    for y in range(H):
        for x in range(W):
            win = {(i, j): V[min(H, max(0, y - 1 + j))][min(W, max(0, x - 1 + i))] for i in range(4) for j in range(4)}
            if "W" not in win.values():
                continue
            inner = {win[k] for k in table.INNER}
            if inner == {"W"} and "W" == min(win.values()) == max(win.values()):
                continue                                    # open water
            # Straight runs keep their matched pieces (SHORE_PIN, and bank_depth's depth
            # row): by context they came out in broken segments. Context is for turns.
            wl = {k: v == "W" for k, v in win.items()}           # water or land: grass beyond the sand isn't a turn
            rows_same = all(len({wl[(i, j)] for i in range(4)}) == 1 for j in range(4))
            cols_same = all(len({wl[(i, j)] for j in range(4)}) == 1 for i in range(4))
            if rows_same or cols_same:
                continue
            if tuple(win[k] for k in table.INNER) in SHORE_PIN:
                continue          # a vertical bank's own tile keeps its straight piece (its run-start variants drew sharp lips)
            found, lvl = table.by_context(win)
            if not found or lvl != 0:
                # Only an exact match for the whole neighbourhood: the looser levels
                # guessed at corners the game never draws (lumps at the south corners).
                stats["kept"] += 1
                continue
            cands = {c: n for c, n in found.items()
                     if c[0] not in SHORE_DECOR and c[1] not in SHORE_DECOR and c[1] not in LILY_PADS}
            if not cands:
                stats["kept"] += 1
                continue                 # nothing but lily pads and reeds there: keep the plain piece
            left = (g["Back"][y][x - 1], g["Buildings"][y][x - 1]) if x else None
            up = (g["Back"][y - 1][x], g["Buildings"][y - 1][x]) if y else None
            if "W" not in inner:
                # plain land beside a bank: only the bank's own overlay pieces (its depth
                # row), never another map's ground (the Mountain's brown dirt, footprints)
                cands = {c: n for c, n in cands.items() if c[1] and c[0] in WATER_BACKS and c[1] not in MOUNTAIN_DIRT}
                if not cands:
                    continue
            if inner == {"W"}:
                # water beside a bank: the plain water unless the game draws something there
                plain = [c for c in cands if c[1] == 0]
                if plain and sum(cands[c] for c in plain) >= 0.5 * sum(cands.values()):
                    cands = {c: cands[c] for c in plain}

            def score(c):
                s = math.log(1 + cands[c])
                if left:
                    s += 1.5 * math.log(1 + table.right[(left, c)])
                if up:
                    s += 1.5 * math.log(1 + table.down[(up, c)])
                return s
            g["Back"][y][x], g["Buildings"][y][x] = max(cands, key=score)
            stats[lvl] += 1
    print("shore cells by context level (0 = full 3x3):", dict(stats))


def fit_edges(g, V, table, passes=4):
    """Re-pick every edge cell (not plain ground) for its neighbours: of the game's
    stacks with that cell's corners, the one the game most often puts beside the
    stacks now left, right, above and below it. This is what turns a run of one-tile
    steps into the game's own curved, diagonal bank, and keeps every edge joined to
    the next (2026-11-04)."""
    import math
    cell = lambda x, y: (g["Back"][y][x], g["Buildings"][y][x]) if 0 <= x < W and 0 <= y < H else None
    edges = [(x, y) for y in range(H) for x in range(W)
             if len({V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1]}) > 1]
    for _ in range(passes):
        changed = 0
        for x, y in edges:
            pat = (V[y][x], V[y][x + 1], V[y + 1][x], V[y + 1][x + 1])
            if pat in SHORE_PIN:
                continue
            cands = [c for c, n in table.by_pattern.get(pat, []) if n >= 2 and c[1] not in LILY_PADS]
            if len(cands) < 2:
                continue
            l, r, u, d = cell(x - 1, y), cell(x + 1, y), cell(x, y - 1), cell(x, y + 1)

            def score(c):
                s = 0.0
                for n, pair, counts in ((l, (l, c), table.right), (r, (c, r), table.right),
                                        (u, (u, c), table.down), (d, (c, d), table.down)):
                    if n is not None:
                        k = counts[pair]
                        s += math.log(1 + k) if k else -3.0      # never seen side by side: a seam
                return s + 0.25 * math.log(table.use[c])
            best = max(cands, key=score)
            if best != cell(x, y):
                g["Back"][y][x], g["Buildings"][y][x] = best
                changed += 1
        if not changed:
            break


# The game's banks are two tiles deep: beside the rocky bank tile sits a second
# piece, laid over the land, that carries the bank's rocky top and its scalloped,
# indented edge against the sand (the Mountain pond; the user, 2026-11-04: "the
# land immediately before water isn't one ruler-straight line, it has depth").
# Learned from what the vanilla maps put beside each bank piece.
NORTH_BANK, WEST_BANK, EAST_BANK = (1270, 0), (5177, 284), (5178, 285)
NORTH_DEPTH = ((1245, 185), (1245, 212))     # above a north bank, alternating
WEST_DEPTH = (1244, 184)                     # left of a bank with land on its left
EAST_DEPTH = (1246, 186)                     # right of a bank with land on its right
NO_DEPTH_ROWS = set(range(20, 25)) | set(range(38, 45)) | {54}   # the bridges' rows: their approaches stay walkable


def bank_depth(g):
    plain_sand = lambda x, y: 0 <= x < W and 0 <= y < H and GROUND.get((g["Back"][y][x], g["Buildings"][y][x])) == ("S",) * 4         and not g["Front"][y][x] and not g["AlwaysFront"][y][x] and y not in NO_DEPTH_ROWS
    at = lambda x, y: (g["Back"][y][x], g["Buildings"][y][x])
    for y in range(H):
        for x in range(W):
            c = at(x, y)
            if c == NORTH_BANK and plain_sand(x, y - 1):
                g["Back"][y - 1][x], g["Buildings"][y - 1][x] = NORTH_DEPTH[x % 2]
            # (No depth column on vertical banks: the game's run thin, and get their rocky
            # lumps only where the bank jogs a step, from paint_shores. A lump on every
            # tile read as a row of beads, the user's circles, 2026-11-04.)


def stairs_art():
    """The picked flight, with its grass, path and the cliff rock round it left out
    (they're the painted ground's and our cliff's, so the seasons change them; the
    rock stayed olive against the winter cliff): only its grey stone is kept."""
    im = Image.open(STAIRS_PICK).convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            grass = g > r + 12 and g > b + 12
            path = r > 170 and r > g + 25 and g > b + 40
            # the cliff's rock round the walls (olive and brown) is ours, in its season's colours
            rock = b < r * 0.3            # measured: the rock's blue is under a fifth of its red, the stone's about two thirds
            if grass or path or rock:
                px[x, y] = (0, 0, 0, 0)
    # stray outline pixels left on their own once the grass round them is gone
    lone = [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][3]
            and sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < im.width and 0 <= y + dy < im.height and px[x + dx, y + dy][3]) <= 1]
    for x, y in lone:
        px[x, y] = (0, 0, 0, 0)
    # Seat the flight: its bottom step on the cliff's foot (the bottom of the face's
    # last row, 6 rows into the piece, which starts the row above the edge), so the
    # path below runs straight off it; its top then meets the path in the edge row.
    # (Placed as painted it stood a tile high: on the grass above, short of the foot.)
    alpha = im.getchannel("A")
    filled = [y for y in range(im.height) if sum(1 for x in range(im.width) if alpha.getpixel((x, y))) > 10]
    foot = filled[-1] + 1                       # the last row of real content (a stray pixel or two can sit lower)
    seated = Image.new("RGBA", im.size)
    seated.alpha_composite(im.crop((0, 0, im.width, foot)), (0, 6 * 16 - foot))
    # Like the game's own stairs, the lowest steps carry on over the shadow band at the
    # cliff's foot, so the path starts below it: the last tile of steps, once more.
    steps = seated.crop((0, 5 * 16, im.width, 6 * 16))
    seated.alpha_composite(steps, (0, 6 * 16))
    return seated


def lay_stairs(g, im, sx, sy, x, top):
    """One flight: its treads (the middle two columns) on Back2, under the farmer and
    walkable, with our cliff cleared from under them; its side walls (the outer
    columns) on Buildings2, over our cliff, which stays solid there."""
    cols, rows = im.width // 16, im.height // 16
    # The top step is ragged, and what shows between its stones was the cliff's grassy
    # lip, a green line across the top of every flight (the user, 2026-11-06). Where a
    # lane runs up to the flight, its paving carries on under the step instead.
    for i in range(cols):
        above = (g["Back"][top - 1][x + i], g["Buildings"][top - 1][x + i])
        if "C" in GROUND.get(above, ()):
            g["Back"][top][x + i], g["Buildings"][top][x + i] = above
    for j in range(rows):
        ty = top - 1 + j
        for i in range(cols):
            tile = im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16))
            tx = x + i
            if i in (1, 2) and top - 1 <= ty <= top + 6:
                g["Buildings"][ty][tx] = g["Front"][ty][tx] = 0
            if tile.getbbox() is None:
                continue
            gid = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
            g["Back2" if i in (1, 2) else "Buildings2"][ty][tx] = gid


# A building's base ends where the paving's curb begins: the cobble top edge (2651)
# has grass in its top 10 rows and the curb's shadow from row 10, so the art is lifted
# 6 px off the bottom of its last row. Set flush, the stone base sat over the curb
# (the user, 2026-11-06: the inn "overlapping the road").
LIFT = 6


def tile_piece(im, lift=0, box=None):
    """A piece cropped to its art (or to box: a season's version is cropped like its
    spring art, so a bare winter tree lines up with its full canopy), padded to whole
    tiles (centred, its bottom lift px above the last row's bottom), alpha made hard,
    outlines softened."""
    im = im.crop(box or im.getbbox())
    a = im.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
    im.putalpha(a)
    cols, rows = -(-im.width // 16), -(-(im.height + lift) // 16)
    tile = Image.new("RGBA", (cols * 16, rows * 16))
    tile.alpha_composite(im, ((cols * 16 - im.width) // 2, rows * 16 - im.height - lift))
    return selout(tile)


def amphitheatre_art(path):
    """The painted amphitheatre with the ground round it dropped: grass, path and the
    cliff's rock connected to the edge of the painting go, so the map's own seasonal
    ground and our cliff show through (as for the stairs); only stone and stage stay.
    The stage's planks are the path's orange, so the stage is kept whole (cutting the
    path away took a band out of it: the user, 2026-11-06, "a weird line of grass
    through the wooden platform"), and the dark grass flecks left along the bottom go."""
    im = Image.open(path).convert("RGBA")
    px = im.load()
    w, h = im.size
    STAGE = (104, 125, 32, 26)          # the stage's ellipse in the painting: centre x, y, radii

    def stage(x, y):
        return ((x - STAGE[0]) / STAGE[2]) ** 2 + ((y - STAGE[1]) / STAGE[3]) ** 2 <= 1

    def ground(x, y):
        r, g, b, a = px[x, y]
        if not a:
            return False
        if stage(x, y):
            return g > r + 12 and g > b + 12        # only the grass round the stage's rim
        return (g > r + 12 and g > b + 12) or (r > 170 and r > g + 25 and g > b + 40) or b < r * 0.3
    seen, stack = set(), [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or not ground(x, y):
            continue
        seen.add((x, y))
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    for x, y in seen:
        px[x, y] = (0, 0, 0, 0)

    def bare(x, y):
        return not (0 <= x < w and 0 <= y < h) or not px[x, y][3]
    for _ in range(3):
        # greenish pixels on the cut edge (grass shadow and outline the fill stopped at)
        # and pixels left with at most one neighbour
        drop = []
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if not a or stage(x, y):
                    continue
                n = [(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
                edge = any(bare(*q) for q in n)
                if (edge and g >= r and g > b) or sum(1 for q in n if not bare(*q)) <= 1:
                    drop.append((x, y))
        for x, y in drop:
            px[x, y] = (0, 0, 0, 0)
    return im


def reachable_spot(name, im, left, bottom, pieces):
    """The nearest place to (left, bottom) where a small prop stands on open grass the
    farmer can reach: every tile of it and the row in front plain grass (no water, sand,
    paving or cliff), clear of the woods round the edge and of every other piece and
    feature (the user, 2026-11-06: Lupini's stool and rock had landed in the river and
    in the trees; if they're ever a quest, they must be reachable)."""
    cols, rows = im.width // 16, im.height // 16
    taken = set()
    for _, arts, l, b, _ in pieces:
        if l is None:
            continue
        a = arts["spring"]
        taken |= {(x, y) for x in range(l - 1, l + a.width // 16 + 1) for y in range(b - a.height // 16, b + 2)}
    for fname, (art, box, l, t, *rest) in FEATURES.items():
        x1, y1 = rest[:2] if rest else (l + (box[2] - box[0]) // 16 - 1, t + (box[3] - box[1]) // 16 - 1)
        taken |= {(x, y) for x in range(l - 1, x1 + 2) for y in range(t - 1, y1 + 2)}

    from vale_decor import RESERVED
    for x0, y0, x1, y1 in RESERVED:
        taken |= {(x, y) for x in range(x0 - 1, x1 + 2) for y in range(y0 - 1, y1 + 2)}

    def grass(x, y):
        return (6 <= x <= 89 and 3 <= y <= 71 and (x, y) not in taken
                and all(SHORE_V[y + j][x + i] in "GD" for i in (0, 1) for j in (0, 1)))
    for r in range(0, 12):
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if max(abs(dx), abs(dy)) != r:
                    continue
                l, b = left + dx, bottom + dy
                front_ok = all(SHORE_V[b + 1 + j][l + i + k] in "GDC" for i in range(cols) for j in (0, 1) for k in (0, 1))                     and all((l + i, b + 1) not in taken for i in range(cols))   # ground in front to stand on, paving allowed
                if front_ok and all(grass(l + i, b - j) for i in range(cols) for j in range(rows)):
                    if (l, b) != (left, bottom):
                        print(name, f"moved from ({left}, {bottom}) to ({l}, {b}): reachable grass")
                    return l, b
    print(name, "NO reachable spot near", left, bottom)
    return left, bottom


def place_buildings(g, assets):
    """The picked buildings, the stairs and the amphitheatre: packed on the seasonal
    sheets (spring/summer/fall the same, winter with each piece's winter version),
    then laid on the map."""
    pieces = []          # (name, {season: image}, left, bottom, solid)
    if not os.environ.get("VALE_NO_STAIRS"):
        st = stairs_art()
        pieces.append(("stairs", {x: st for x in SEASONS}, None, None, None))
    for name, (art, left, bottom, solid, *rest) in PICKS.items():
        lift = rest[0] if rest else LIFT
        src = Image.open(os.path.join(ART, art + ".png")).convert("RGBA")
        spring = tile_piece(src, lift)
        arts = {x: spring for x in SEASONS}
        for season, path in SEASONAL.get(name, {}).items():
            if not os.path.exists(os.path.join(ART, path + ".png")):
                continue
            im = Image.open(os.path.join(ART, path + ".png")).convert("RGBA")
            if im.size != src.size and 0 <= im.width - src.width < 4 and 0 <= im.height - src.height < 4:
                # padded to multiples of 4 for the edit (centred, bottom kept): crop it back
                ox = (im.width - src.width) // 2
                im = im.crop((ox, im.height - src.height, ox + src.width, im.height))
            im = tile_piece(im, lift, src.getbbox() if im.size == src.size else None)
            if im.size != spring.size:
                print(season, "art for", name, "doesn't match its spring size; using spring")
                continue
            arts[season] = im
        if name.startswith("spot_"):
            left, bottom = reachable_spot(name, spring, left, bottom, pieces)
        pieces.append((name, arts, left, bottom, solid))
    if os.path.exists(os.path.join(ART, AMPHITHEATRE[0] + ".png")):
        amph = amphitheatre_art(os.path.join(ART, AMPHITHEATRE[0] + ".png"))
        pieces.append(("amphitheatre", {x: amph for x in SEASONS}, AMPHITHEATRE[1], AMPHITHEATRE[2], None))
    from vale_feature import cutout, cutout_diff, walkable, seasonal
    import json
    SEASON_COLOURS = json.load(open(os.path.join(ART, "season_colours.json")))
    for name, (art, box, left, top, *rest) in ({} if os.environ.get("VALE_NO_FEATURES") else FEATURES).items():
        src = Image.open(os.path.join(ART, art + ".png")).convert("RGBA")
        if isinstance(box, str):
            _, before, rx, ry = box.split(":")
            x1, y1 = rest[:2]
            box = ((left - int(rx)) * 16, (top - int(ry)) * 16, (x1 - int(rx) + 1) * 16, (y1 - int(ry) + 1) * 16)
            im = cutout_diff(src.crop(box), Image.open(os.path.join(ART, "features", before + ".png")).crop(box))
            px = im.load()
            # the main stretches of sand, for "objects" erasing: a barrow's pale outline
            # passes for sand, but it isn't joined to the bank's sand
            sandy = lambda c: c[3] and c[0] > 140 and c[0] > c[1] + 10 and c[1] > c[2] + 25
            big_sand, seen = set(), set()
            for sy0 in range(im.height):
                for sx0 in range(im.width):
                    if (sx0, sy0) in seen or not sandy(px[sx0, sy0]):
                        continue
                    comp, stack = [], [(sx0, sy0)]
                    seen.add((sx0, sy0))
                    while stack:
                        qx, qy = stack.pop()
                        comp.append((qx, qy))
                        for nq in ((qx + 1, qy), (qx - 1, qy), (qx, qy + 1), (qx, qy - 1)):
                            if 0 <= nq[0] < im.width and 0 <= nq[1] < im.height and nq not in seen and sandy(px[nq]):
                                seen.add(nq)
                                stack.append(nq)
                    if len(comp) >= 200:
                        big_sand.update(comp)
            if name in FEATURE_SAND_PATCH:
                import math
                cx, cy, rx, ry = FEATURE_SAND_PATCH[name]
                for yy in range(im.height):
                    for xx in range(im.width):
                        c = px[xx, yy]
                        if not c[3] or (xx >= cx and not sandy(c)):
                            continue        # (outside the patch: sand goes, and the old muddy track on the left; the shovel's grip stays)
                        a = math.atan2((yy - cy) / ry, (xx - cx) / rx)
                        edge = 1 + 0.10 * math.sin(3 * a + 1.3) + 0.06 * math.sin(7 * a + 0.4) + 0.04 * math.sin(13 * a)
                        if ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 > edge * edge:
                            px[xx, yy] = (0, 0, 0, 0)
            for ex0, ey0, ex1, ey1, *how in FEATURE_ERASE.get(name, ()):
                for yy in range(ey0, min(ey1, im.height)):
                    for xx in range(ex0, min(ex1, im.width)):
                        c = px[xx, yy]
                        if how == ["keep_blue"] and c[3] and (c[2] > c[0] + 50 or (c[2] > 150 and c[1] > 150)):
                            continue        # (keep_blue: the mushrooms)
                        if how == ["objects"] and (xx, yy) in big_sand and c[0] > 200:   # (darker sand is the removed things' shadow)
                            continue        # (objects: the sand under them stays, no straight cut)
                        px[xx, yy] = (0, 0, 0, 0)
        else:
            im = cutout(src.crop(box), lawn=True)
        from vale_feature_fixes import apply_walk, keep_winter, mirror
        mirror(name, im, left, top)             # (the tea garden's east hedge)
        walk = walkable(im)
        apply_walk(name, walk, left, top)        # (hand fixes where the guess was wrong)
        if isinstance(box, tuple) and len(rest) > 2:
            walk["front_above"] = rest[2]
        arts = {"spring": im}
        for season in ("summer", "fall"):
            arts[season] = seasonal(im, SEASON_COLOURS, season)     # (bare dirt and sand only)
        # Winter is its own art (tools/art/vale_feature_winter.py: snow, the flowers gone),
        # cut against the bare map in winter (features/winter/bare_winter.png, rendered
        # with VALE_NO_FEATURES set) the way spring is cut against the bare map in spring.
        wpath = os.path.join(ART, "features", "winter", name + "_0.png")
        if os.path.exists(wpath) and name not in WINTER_RECOLOUR:
            mb = (left * 16, top * 16, left * 16 + im.width, top * 16 + im.height)
            # the spring cut-out's exact footprint, in its winter colours: cutting snow
            # against snow left scattered flecks (the user: winter "extraordinarily messy")
            wim = Image.open(wpath).convert("RGBA")
            if wim.size == im.size:
                wim.putalpha(im.getchannel("A"))
                arts["winter"] = wim
            else:
                print("winter art for", name, "is", wim.size, "not", im.size)
                arts["winter"] = im
        else:
            # bare ground recoloured; painted grass tufts go (there's no grass in winter)
            wim = seasonal(im, SEASON_COLOURS, "winter")
            wpx = wim.load()
            for yy in range(wim.height):
                for xx in range(wim.width):
                    c = wpx[xx, yy]
                    if c[3] and c[1] > c[0] + 12 and c[1] > c[2] + 12:
                        wpx[xx, yy] = (0, 0, 0, 0)
            arts["winter"] = wim
        if arts["winter"] is im:
            arts["winter"] = im.copy()
        keep_winter(name, im, arts["winter"], left, top)   # (Fern's compost heap, snowed on)
        mirror(name, arts["winter"], left, top)
        # the tops of tall things (vale_feature_fixes.py FRONT), lifted out to lay on
        # Front over the farmer; the path round them stays in the feature, underfoot
        from vale_feature_fixes import split_front
        for k, (fx, fy, lifted) in enumerate(split_front(name, arts, left, top)):
            pieces.append((f"featurefront:{name}:{k}", lifted, fx, fy, None))
        pieces.append(("feature:" + name, arts, left, top, walk))
    slots, x, y, row_h = [], 0, 0, 0
    for _, arts, *_ in pieces:
        cols, rows = arts["spring"].width // 16, arts["spring"].height // 16
        if x + cols > SHEET_COLS:
            x, y, row_h = 0, y + row_h, 0
        slots.append((x, y))
        x += cols
        row_h = max(row_h, rows)
    sheets = {season: Image.new("RGBA", (SHEET_COLS * 16, (y + row_h) * 16)) for season in SEASONS}
    for (_, arts, *_), (sx, sy) in zip(pieces, slots):
        for season in SEASONS:
            sheets[season].alpha_composite(arts[season], (sx * 16, sy * 16))
    for season in SEASONS:
        sheets[season].save(os.path.join(assets, SHEET.replace("spring_", season + "_")))
    old = os.path.join(assets, "GildedValeBuildings.png")
    if os.path.exists(old):
        os.remove(old)      # (the sheet before it was seasonal)
    pieces = [(name, arts["spring"], None, left, bottom, solid) for name, arts, left, bottom, solid in pieces]
    for (name, im, _, left, bottom, solid), (sx, sy) in zip(pieces, slots):
        cols, rows = im.width // 16, im.height // 16
        if name == "stairs":
            for x, top in STAIR_SPOTS:
                lay_stairs(g, im, sx, sy, x, top)
            continue
        if name.startswith("feature:"):
            walk = solid
            for j in range(rows):
                for i in range(cols):
                    if (i, j) not in walk:
                        continue
                    tx, ty = left + i, bottom + j
                    if "C" in GROUND.get((g["Back"][ty][tx], g["Buildings"][ty][tx]), ()):
                        if name[len("feature:"):] in OVER_PAVING and not walk[(i, j)]:
                            g["Front"][ty][tx] = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
                        continue        # never under-foot on the paving: the lanes stay the map's, and walkable
                    gid = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
                    # Never over the map's own ground pieces (a shore's rock edge on
                    # Buildings stays, and the art goes above it on Buildings2).
                    if ty < walk.get("front_above", -1):
                        g["Front"][ty][tx] = gid           # over the farmer, who walks under it
                    elif g["Buildings"][ty][tx]:
                        g["Buildings2"][ty][tx] = gid
                    elif walk[(i, j)]:
                        g["Back2"][ty][tx] = gid
                    else:
                        g["Buildings"][ty][tx] = gid
                    g["Paths"][ty][tx] = 0
            print(name, f"{cols}x{rows} tiles at x {left}-{left + cols - 1}, y {bottom}-{bottom + rows - 1},",
                  sum(1 for k, v in walk.items() if isinstance(k, tuple) and not v), "solid")
            continue
        if name.startswith("featurefront:"):
            for j in range(rows):
                for i in range(cols):
                    if im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)).getbbox() is not None:
                        g["Front"][bottom + j][left + i] = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
            continue
        if name == "amphitheatre":
            # Over the cliff and under the farmer (Buildings2); where it fully covers a
            # tile, the cliff's collision comes off, so the tiers walk down to the stage.
            for j in range(rows):
                for i in range(cols):
                    t = im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16))
                    if t.getbbox() is None:
                        continue
                    tx, ty = left + i, bottom + j
                    g["Buildings2"][ty][tx] = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
                    if all(a == 255 for a in t.getchannel("A").getdata()):
                        g["Buildings"][ty][tx] = g["Front"][ty][tx] = 0
            print(name, f"{cols}x{rows} tiles at x {left}-{left + cols - 1}, y {bottom}-{bottom + rows - 1}")
            continue
        top = bottom - rows + 1
        for j in range(rows):
            for i in range(cols):
                t = im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16))
                if t.getbbox() is None:
                    continue
                gid = BUILD_FIRST + (sy + j) * SHEET_COLS + sx + i
                tx, ty = left + i, top + j
                n, c0, c1 = solid if isinstance(solid, tuple) else (solid, 0, cols - 1)
                if j >= rows - n and c0 <= i <= c1:
                    g["Buildings"][ty][tx] = gid
                    g["Front"][ty][tx] = 0
                    if name.startswith("spot_"):
                        # Lupini's painting spots paint the view (ArtStudio/Vale/GildedVale.cs)
                        from vale_decor import TILE_PROPS
                        TILE_PROPS[("Buildings", tx, ty)] = {"Action": "blinxxy.MuseArt_PaintSpot"}
                elif j >= rows - n:
                    g["Back2"][ty][tx] = gid          # (the rest of a solid row, under the farmer: a tree's roots)
                elif PICKS.get(name, ("",))[0].startswith("buildings/") and ty <= bottom - 2:
                    # A building's roof and upper walls, 2+ rows above its base: on
                    # AlwaysFront. The game sorts a Front tile by its own row, so a
                    # roof tile 2 rows above the farmer's feet drew under him, and
                    # walking down behind a building his head poked out through the
                    # roof (the user, 2026-11-08). Nobody standing in front of the
                    # base reaches that high, and the yards keep trees clear
                    # (vale_yards.py), so always-on-top is the building's real depth.
                    g["AlwaysFront"][ty][tx] = gid
                else:
                    g["Front2"][ty][tx] = gid
        print(name, f"{cols}x{rows} tiles at x {left}-{left + cols - 1}, y {top}-{bottom}")
    global BUILD_SHEET_H
    BUILD_SHEET_H = sheets["spring"].height


# The bus road and its turning circle: our own seasonal sheet, drawn from the Bus
# Stop's road by tools/art/vale_road.py (run it first; it writes the sheets into the
# pack and the layout here).
ROAD_LAYOUT = "art/gilded_vale/road/layout.json"
ROAD_SHEET = "spring_GildedValeRoad.png"
ROAD_FIRST = 30000
ROAD = {}


def lay_road(g):
    import json
    ROAD.update(json.load(open(ROAD_LAYOUT)))
    for key, i in ROAD["tiles"].items():
        x, y = map(int, key.split(","))
        g["Back"][y][x] = ROAD_FIRST + i
        g["Buildings"][y][x] = 0


BUILD_FIRST = 20000
BUILD_SHEET_H = 0


def write(path, g, props, tilesets):
    out = ['<?xml version="1.0" encoding="utf-8"?>',
           f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" '
           f'tilewidth="16" tileheight="16" infinite="0" nextlayerid="{2 * len(LAYERS) + 1}" nextobjectid="1000">',
           '  <properties>']
    out += [f'    <property name="{k}" type="string" value="{v}" />' for k, v in props.items()]
    out.append('  </properties>')
    out += tilesets
    out.append(f'  <tileset firstgid="{BUILD_FIRST}" name="z_blinxxy.MuseArt_GildedValeBuildings" tilewidth="16" tileheight="16" '
               f'tilecount="{SHEET_COLS * BUILD_SHEET_H // 16}" columns="{SHEET_COLS}">')
    out.append(f'    <image source="{SHEET}" width="{SHEET_COLS * 16}" height="{BUILD_SHEET_H}" />')
    out.append('  </tileset>')
    if ROAD:
        out.append(f'  <tileset firstgid="{ROAD_FIRST}" name="z_blinxxy.MuseArt_GildedValeRoad" tilewidth="16" tileheight="16" '
                   f'tilecount="{ROAD["count"]}" columns="{ROAD["columns"]}">')
        out.append(f'    <image source="{ROAD_SHEET}" width="{ROAD["columns"] * 16}" height="{ROAD["height"]}" />')
        out.append('  </tileset>')
    from vale_decor import TILE_PROPS, FENCE_FIRST, FENCE_SHEETS
    # The game's fence sprites, straight from its content (never copied into the pack).
    for kind, first in FENCE_FIRST.items():
        out.append(f'  <tileset firstgid="{first}" name="z_fence_{kind}" tilewidth="16" tileheight="16" tilecount="66" columns="3">')
        out.append(f'    <image source="../LooseSprites/{FENCE_SHEETS[kind]}" width="48" height="352" />')
        out.append('  </tileset>')
    for i, layer in enumerate(LAYERS):
        out.append(f'  <layer id="{i + 1}" name="{layer}" width="{W}" height="{H}">')
        out.append('    <data encoding="csv">')
        out.append(",\n".join(",".join(str(v) for v in row) for row in g[layer]))
        out.append('    </data>')
        out.append('  </layer>')
    # Tile properties, as the game's maps keep them: a "TileData" object on an object
    # layer named for the tile layer (e.g. SpawnTree on the orchard's Paths markers).
    oid = 1
    for i, layer in enumerate(LAYERS):
        props = sorted((x, y, p) for (l, x, y), p in TILE_PROPS.items() if l == layer)
        if not props:
            continue
        out.append(f'  <objectgroup id="{len(LAYERS) + 1 + i}" name="{layer}" visible="false">')
        for x, y, p in props:
            out.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            out.append('      <properties>')
            out += [f'        <property name="{k}" type="string" value="{v}" />' for k, v in p.items()]
            out.append('      </properties>')
            out.append('    </object>')
            oid += 1
        out.append('  </objectgroup>')
    out.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(out))
    print("wrote", path)


if __name__ == "__main__":
    main()

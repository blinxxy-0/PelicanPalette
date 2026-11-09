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
    "sconce":       [{"F": [827]}],                                                 # a wall lamp (as by the game's doors)
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
    # the Exhibition Hall's
    "mural":         (6, 2, 0),     # the Vale painted by its artists, faded (repainted after Lupini's 8 hearts)
    "panel":         (2, 3, 1),     # a free-standing gallery panel, a painting hung on it
    "bowl_case":     (2, 2, 1),     # a glass case of Tobin's bowls
    "flower_case":   (2, 2, 1),     # a glass case of Fern's pressed flowers
    "plinth":        (1, 2, 1),     # a sculpture on a plinth
    "plinth_b":      (1, 2, 1), "plinth_c": (1, 2, 1), "panel_b": (2, 3, 1),
    "plaque":        (1, 1, 1),     # a brass label on a short post
    "plaque_wide":   (2, 1, 1),     # and a wide one, for the mural
    "jug_case":      (2, 2, 1), "leaf_case": (2, 2, 1), "plinth_d": (1, 2, 1),
    "sheeted_statue": (1, 2, 1),    # a sculpture under a dust sheet
    "reception":     (3, 2, 1),     # a desk with the visitors' book
    "sheeted":       (2, 2, 1),     # frames under a dust sheet
    "poster":        (2, 2, 0),     # a faded poster for the last exhibition
    "crates":        (2, 2, 1),     # packing crates, a frame sticking out
    "frame_rack":    (2, 3, 1),     # empty frames stacked in a rack
    "ladder":        (1, 3, 1),     # a stepladder against the wall
    # Fern's
    "pressed_flowers": (2, 2, 0),   # pressed wildflowers in frames, on the wall
    "seed_drawers":  (2, 3, 1),     # a cabinet of little labelled seed drawers
    "potting_bench": (3, 3, 1),     # against the glass: pots, soil, a trowel
    "vine_trellis":  (2, 3, 1),     # the night-flowering vine (her 8-heart event)
    "seed_counter":  (3, 2, 1),     # where she sells seeds: packets in a tray, a cash tin
    "seed_trays":    (2, 2, 1),     # seedling trays on a low table
    "mushroom_pot":  (1, 1, 1),     # the mushroom she swears hums
    "soil_sacks":    (1, 2, 1),
    "watering_can":  (1, 1, 1),
    # Emmett's
    "pinned_maps":   (3, 2, 0),     # maps of everywhere he's played, pinned to the wall, string between towns
    "instrument_stand": (2, 2, 1),  # his fiddle on a stand, a concertina with a broken key, Tobin's ocarina
    "music_stand":   (1, 2, 1),     # sheet music, half of it crossed out
    "coat_rack":     (1, 2, 1),     # his long travel-worn coat, the red neckerchief
    "moss_wall":     (1, 2, 0),     # moss and ivy creeping down the plaster from a beam
    "beam":          (1, 2, 0),     # and the beam alone
    "beam_l": (1, 2, 0), "beam_r": (1, 2, 0),             # the beam stopping behind a lamp
    "moss_wall_l": (1, 2, 0), "moss_wall_r": (1, 2, 0),   # the ivy at a beam's carved end
    "moss_patch":    (1, 1, -2),    # moss in a corner of the floorboards
    "stump_stool":   (1, 1, 1),     # a mossy tree stump for a stool
    "fern_pot":      (1, 2, 1),     # a big fern in an old bucket
    "crate_table":   (1, 1, 1),     # an upturned crate for a side table, a candle and a mug
    # the Old Studio's
    "dusty_window":  (5, 2, 0),     # a tall studio window, cracked panes, cobwebs, grey light
    "pinned_sketches": (3, 2, 0),   # charcoal sketches for the unfinished painting, pinned up, curling
    "backward_painting": (2, 2, 0), # a painting hung facing the wall: its stretcher, a torn label
    "rules_board":   (1, 2, 0),     # the studios' house rules, every artist's signature under them
    "cobweb":        (1, 1, 0),     # in a wall's corner
    "unfinished_easel": (2, 3, 1),  # the unfinished painting (Lupini's 6-heart event), a stool before it
    "note_table":    (2, 2, 1),     # a little table: the note, a dried-up palette, brushes stiff in a jar
    "paint_trestle": (3, 2, 1),     # a long trestle crusted with old paint
    "sheeted_chair": (1, 2, 1),     # a chair under a dust sheet
    "sheeted_chair_b": (1, 2, 1),
    "leaves":        (1, 1, -2),    # dead leaves blown in under the door
    "old_cot":       (2, 3, 2),     # a narrow iron cot, a thin mattress, a folded blanket
    "washstand":     (2, 3, 1),     # a jug in a basin, a little mirror
    # Studio II, the Corrie twins' (docs/design/vale_newcomers.md, section 5); hand-drawn by
    # tools/art/vale_corrie_art.py (no PixelLab: the user keeps it for characters)
    "glass_racks":   (2, 3, 1),     # an upright rack of coloured sheet glass, ruby, cobalt, amber, green
    "colour_pool":   (2, 2, -2),    # the colour the racks throw on the floor
    "small_window":  (2, 2, 0),     # a small window behind the racks: where that light comes from
    "design_sketches": (3, 2, 0),   # Opal's window designs, coloured in, pinned up any old how
    "drafting_table": (2, 2, 1),    # her tilted drawing board, a design half coloured, pencils everywhere
    "studio_window": (4, 2, 0),     # the big studio window over Mabel's bench, plain glass
    "mabel_window":  (4, 2, 0),     # ... with Mabel's finished design in it (Jet's 10 hearts)
    "mabel_bench":   (2, 2, 1),     # Mabel's old bench, worn smooth, her grozing pliers laid out, kept, never used
    "mabel_bench_used": (2, 2, 1),  # ... in use: her pattern book open, a panel half leaded, Jet's stool out
    "light_table":   (2, 2, 1),     # a glowing light table, a half-leaded panel on it
    "modern_kiln":   (2, 3, 1),     # a sleek steel city kiln with a dial, far too nice for the Vale
    "glass_lamp":    (1, 3, 1),     # their own stained-glass floor lamp (Opal's 6 hearts; also on the counter)
    "glass_counter": (3, 2, 1),     # the counter: suncatchers hung on a rail, a little lamp, the cash tin
    "cutting_bench": (3, 2, 1),     # Jet's bench, spotless: a cutting mat, tools laid out in rows
    "offcut_bins":   (2, 2, 1),     # offcuts sorted by colour, bin by bin
    "leaning_panels": (1, 2, 1),    # finished panels leaning on the counter's side
    "sketch_papers": (1, 1, -2),    # Opal's sketches dropped on the floor
    "split_line":    (1, 1, -2),    # the line down the middle of the floor: lead came inlaid across the seam,
    "split_line_b":  (1, 1, -2),    # its left half on Opal's side (col 10), its right on Jet's (col 11)
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
# Other paintings of the game's (Data/Furniture), for the Exhibition Hall: "game_<id>" -> size
GAME_PAINTINGS = {1554: (3, 2), 1547: (3, 2), 1541: (2, 2), 1557: (2, 2), 2421: (2, 2), 1565: (2, 2),
                  1567: (1, 2), 2329: (2, 2), 2423: (2, 2), 1802: (2, 2), 1612: (2, 2), 1606: (1, 2)}
for _id, _wh in GAME_PAINTINGS.items():
    LUPINI[f"game_{_id}"] = _id
# The game's own furniture standing on the floor, from TileSheets/furniture: key -> (its sprite
# index, tiles wide, tall, solid bottom rows). The Modern Lamp is the twins' city lamp.
GAME_FURN = {"city_lamp": (1447, 1, 3, 1)}
# wall pieces hung from the top of their cells, BEAM_DROP px down (Emmett's beams: one height, so
# they join, clear of the ceiling's outline)
TOP_ALIGNED = {"beam", "moss_wall", "beam_l", "beam_r", "moss_wall_l", "moss_wall_r"}
BEAM_DROP = 5
BED = (2, 4)                        # the Saloon's spare bed
# The game's rugs that 9-slice (townInterior_2, their top-left tile): ("rug", x, y, w, h)
RUGS = {"rug": 2761,                # Haley's red one
        "rug_brown": 2753}          # the brown patterned one

# Emmett's pieces (some also in the Old Studio)
EMMETT_PICKS = {k: "art/gilded_vale/interiors/emmett/" + v for k, v in {
    "pinned_maps": "emmett_v1/pinned_maps_1_6", "instrument_stand": "emmett_v1/instrument_stand_1_8",
    "music_stand": "emmett_v1/music_stand_1_2", "coat_rack": "emmett_v1/coat_rack_1_0",
    "moss_wall": "emmett_v1/moss_wall_1_1", "moss_patch": "moss_patch",
    "stump_stool": "emmett_v1/stump_stool_1_0", "fern_pot": "emmett_v1/fern_pot_1_0",
    "crate_table": "emmett_v1/crate_table_1_0", "beam": "beam", "beam_l": "beam_l", "beam_r": "beam_r",
    "moss_wall_l": "moss_wall_l", "moss_wall_r": "moss_wall_r"}.items()}

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
        # right-click to read (i18n vale.pottery.<key>); "wheel" is his, "wheel#2" the one he teaches on
        describe={"kiln": "kiln", "logs": "hardwood", "logs#2": "hardwood", "wheel": "wheel", "wheel#2": "wheel_b",
                  "drying_rack": "drying_rack", "glaze_shelf": "glazes", "pot_shelf": "pot_shelf",
                  "pot_shelf_b": "pot_shelf_b", "price_board": "price_board", "sales_counter": "counter",
                  "packing_crate": "crate", "clay_sacks": "clay", "clay_sacks#2": "clay", "tool_board": "tools",
                  "bookcase": "bookcase", "bed": "bed"},
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
            "north_window": "north_window_1_0", "easel_big": "easel_big_1_0", "paint_table": "paint_table_1_5",
            "brush_table": "brush_table_1_8", "canvas_stack": "canvas_stack_1_0", "bust": "bust_1_0",
            "display_easel": "display_easel_1_27", "bench": "../bench_clean", "hat_stand": "hat_stand_1_3",
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
            ("rug", 11, 17, 6, 3), ("bench", 12, 18), ("display_easel", 17, 17), ("hat_stand", 8, 18),
            ("plant", 2, 17), ("plant_b", 19, 18), ("canvas_stack#2", 3, 18),
        ],
        # right-click to read (i18n vale.atelier.<key>): painting_1..7 as in LUPINI, Red Eagle on the easel
        describe={**{f"painting_{i}": f"painting_{i}" for i in range(1, 8)}, "display_easel": "display_easel",
                  "empty_hook": "empty_hook", "easel_big": "easel", "north_window": "window",
                  "hat_stand": "hat_stand", "bust": "bust", "bench": "bench", "paint_table": "paint_table"},
        notes=["Lupini's atelier: one floor plan, like Leah's cottage. The front door opens into the hall.",
               "The hall: his canon paintings on the wall, the empty hook of the one on his boat this week,",
               "a bench to sit and look, a display easel with another, his hat stand by the door.",
               "Through the door, the studio: the easel under the big north window, paint and brush tables,",
               "the fireside (couch, coffee table, armchair on one rug), and his bed under two paintings.",
               "Paintings are the game's own Lupini paintings. Orange outline = new PixelLab piece. Dotted line = walkway."]),

    # The Exhibition Hall (map doc, sections 5 and 8: "like the Community Center's main
    # room"; the Vale's gallery once Lupini's 8-heart event reopens it, its displays
    # changing; the mural on its west wall; the abandoned exhibition in its back room).
    # Built open; the closed version (until the 8 hearts) is the same plan with dust
    # sheets over the displays.
    "hall": dict(
        title="The Exhibition Hall", location="blinxxy.MuseArt_ValeHall", tmx="ValeHall.tmx",
        W=30, H=25, outside=(44, 39),
        rooms={"back":    dict(a=9, b=19, t=0, f=9, label="The back room"),
               "store":   dict(a=22, b=28, t=0, f=9, label="Storeroom"),
               "gallery": dict(a=1, b=28, t=10, f=22, label="The gallery")},
        doors=[("back", "gallery", 14), ("store", "gallery", 25)],
        front=("gallery", 14),
        zones=[(1, 14, 28, 22, "stone tiles")],
        wall=78, walls={"back": 103, "store": 103},     # a museum green; the back rooms faded plaster
        picks={"bench": "art/gilded_vale/interiors/atelier/bench_clean",
               **{k: "art/gilded_vale/interiors/hall/hall_v1/" + v for k, v in {
                   "mural": "../mural_faded", "panel": "panel_1_0", "panel_b": "panel_1_2", "bowl_case": "bowl_case_1_1",
                   "flower_case": "flower_case_2_8", "plinth": "../plinth_slate", "plinth_b": "../plinth_sandstone",
                   "plinth_c": "../plinth_brick", "reception": "reception_1_6",     # (plinths: tools/art/vale_plinths.py)
                   "sheeted": "sheeted_1_5", "poster": "poster_1_0", "crates": "crates_1_7",
                   "frame_rack": "frame_rack_1_1", "ladder": "ladder_1_8", "plaque": "../hall_v2/plaque_1_0",
                   "plaque_wide": "../hall_v3/plaque_wide_1_0", "jug_case": "../hall_v3/jug_case_1_1",
                   "leaf_case": "../hall_v3/leaf_case_1_1", "plinth_d": "../hall_v3/plinth_d_1_5",
                   "sheeted_statue": "../hall_v3/sheeted_statue_1_0"}.items()}},
        lights=[(5, 17), (11, 17), (14, 15), (19, 17), (25, 17), (14, 5), (25, 5)],
        pieces=[
            # the gallery's back wall: the mural at the west end, paintings
            ("mural", 2, 11), ("game_1554", 9, 11), ("game_1541", 16, 11), ("game_1612", 19, 11), ("game_2421", 22, 11),
            ("game_1567", 27, 11),
            # before the mural: two benches on a rug
            ("rug", 2, 16, 6, 5), ("bench", 3, 17), ("bench#2", 3, 19),
            # free-standing panels of the Vale's past artists' work
            ("panel", 9, 15), ("panel_b", 9, 18),
            # the residents' work: Tobin's bowls, Fern's pressed flowers, sculptures
            # (every exhibit its own: the user, 2026-11-07, "we want this to be unique")
            ("bowl_case", 17, 16), ("flower_case", 20, 16), ("jug_case", 17, 19), ("leaf_case", 20, 19),
            ("plinth", 23, 16), ("plinth_b", 27, 16), ("plinth_c", 23, 19), ("plinth_d", 27, 19),
            # by the door: the desk with the visitors' book; palms in the corners
            # a runner up the middle, from the door to the back room
            ("rug#2", 13, 15, 3, 7),
            # (the user, 2026-11-07: the desk at the top of the runner, before the back room)
            ("reception", 13, 15), ("plant", 16, 20), ("plant", 1, 14), ("plant", 1, 20), ("plant_b", 28, 20), ("plant", 28, 14),
            # a brass plaque below each painting on the wall, a wide one centred under the
            # mural (the user, 2026-11-07: fewer than one per exhibit, "too crowded");
            # right-click a plaque, or any exhibit, to read about it
            ("plaque_wide", 4, 14), ("plaque#2", 10, 14), ("plaque#3", 16, 14), ("plaque#4", 19, 14),
            ("plaque#5", 22, 14), ("plaque#6", 27, 14),
            # the back room: the exhibition nobody took down
            ("poster", 10, 1), ("game_2423", 13, 1), ("game_1565", 16, 1),
            ("sheeted", 10, 5), ("sheeted#2", 16, 6), ("crates", 9, 7), ("ladder", 19, 3), ("sheeted_statue", 13, 6),
            # the storeroom
            ("frame_rack", 23, 2), ("game_1606", 26, 1), ("crates#2", 26, 5), ("writing_desk", 22, 6), ("sheeted#3", 27, 7),
        ],
        # what right-clicking a piece (or its plaque) says: piece -> text key (i18n vale.hall.<key>)
        describe={"mural": "mural", "plaque_wide": "mural", "game_1554": "jade_hills", "plaque#2": "jade_hills",
                  "game_1541": "eco_hill", "plaque#3": "eco_hill", "game_1612": "kitemaster", "plaque#4": "kitemaster",
                  "game_2421": "boat", "plaque#5": "boat", "game_1567": "needlepoint", "plaque#6": "needlepoint",
                  "panel": "panel", "panel_b": "panel_b", "bowl_case": "bowls", "jug_case": "jug",
                  "flower_case": "flowers", "leaf_case": "flowers_b", "plinth": "spiral", "plinth_c": "kiln_woman",
                  "plinth_b": "moss_figure", "plinth_d": "heron",
                  "reception": "visitors_book",
                  "poster": "poster", "game_2423": "vista", "game_1565": "calico_falls", "sheeted": "sheeted",
                  "sheeted#2": "sheeted", "crates": "crates", "ladder": "ladder", "sheeted_statue": "unshown",
                  "frame_rack": "frame_rack", "game_1606": "dancing_grass", "crates#2": "crates_b",
                  "sheeted#3": "sheeted", "writing_desk": "ledger"},
        notes=["The Exhibition Hall: one floor plan, like the Community Center's main room. Front door at the bottom.",
               "The gallery: the mural on the west end of the back wall, two benches on a rug before it; panels of",
               "the Vale's past artists' work; glass cases of Tobin's bowls and Fern's pressed flowers; sculptures on",
               "plinths; the visitors' book by the door. Paintings on the walls are the game's own.",
               "Behind: the back room (the abandoned exhibition: sheeted frames, crates, a poster) and a storeroom.",
               "Closed until Lupini's 8 hearts: the same plan with dust sheets over everything. Orange = new PixelLab piece."]),

    # Fern's cottage and seedhouse (map doc, section 5: "like Leah's. With the seedhouse as
    # a glass room on one side"; fern_brief.md). Two doors outside, the cottage's and the
    # seedhouse's, into one room: the cottage end and the glass end, a row of plants
    # between them; her bedroom behind.
    "fern": dict(
        title="Fern's cottage", location="blinxxy.MuseArt_ValeFern", tmx="ValeFern.tmx",
        W=28, H=23, outside=[(12, 38), (17, 38)],
        rooms={"bedroom": dict(a=2, b=10, t=0, f=9, label="Fern's bedroom"),
               "main":    dict(a=1, b=25, t=10, f=20, label="The cottage            The seedhouse")},
        doors=[("bedroom", "main", 8)],
        front=[("main", 6), ("main", 21)],
        zones=[(17, 14, 25, 20, "flagstones")],
        wall_zones=[("main", 17, 25, 57)],        # the seedhouse's back wall is glass
        wall=7, walls={"bedroom": 100},
        picks={"chest": "art/gilded_vale/interiors/pottery/pottery_v1/chest_1_9",
               **{k: "art/gilded_vale/interiors/fern/fern_v1/" + v for k, v in {
                   "pressed_flowers": "pressed_flowers_1_0", "seed_drawers": "seed_drawers_2_0",
                   "potting_bench": "potting_bench_2_0", "vine_trellis": "vine_trellis_2_2",
                   "seed_counter": "seed_counter_1_0", "seed_trays": "seed_trays_1_2",
                   "mushroom_pot": "mushroom_pot_1_4", "soil_sacks": "soil_sacks_1_0",
                   "watering_can": "watering_can_1_0"}.items()}},
        lights=[(4, 16), (12, 16), (20, 16), (23, 18), (5, 6)],
        pieces=[
            # the cottage end: the fire, the couch with its back to the wall, the coffee
            # table and an armchair on one rug; her pressed flowers over the couch;
            # the kitchen and her table
            ("fireplace", 2, 11), ("pressed_flowers", 5, 11), ("couch", 4, 13), ("rug", 4, 15, 5, 4),
            ("coffee_table", 4, 15), ("armchair_w", 7, 15), ("kitchen", 10, 11), ("dining", 11, 17),
            ("plant", 1, 18), ("floor_lamp", 9, 18),
            # between the two ends, a row of her plants
            ("plant_b", 16, 14), ("plant", 16, 18),
            # the seedhouse: seed drawers, the potting bench against the glass, the vine
            # on its trellis; the counter she sells seeds from; seedling trays, soil
            ("seed_drawers", 17, 12), ("potting_bench", 20, 12), ("vine_trellis", 24, 12),
            ("seed_counter", 22, 17), ("seed_trays", 17, 16), ("seed_trays#2", 24, 15),
            ("mushroom_pot", 19, 14), ("soil_sacks", 25, 18), ("watering_can", 19, 18), ("plant_b", 17, 18),
            # her bedroom
            ("wardrobe", 2, 2), ("window", 5, 1), ("bed", 6, 4), ("nightstand", 8, 3),
            ("pressed_flowers#2", 9, 1), ("plant_b", 10, 4), ("rug", 2, 6, 4, 3), ("chest", 6, 8),
        ],
        # right-click to read (i18n vale.fern.<key>); the vine hints at her 8 hearts, no more
        describe={"vine_trellis": "vine", "mushroom_pot": "mushroom", "seed_drawers": "seed_drawers",
                  "potting_bench": "potting_bench", "seed_counter": "seed_counter", "seed_trays": "seed_trays",
                  "seed_trays#2": "seed_trays", "pressed_flowers": "pressed_flowers",
                  "pressed_flowers#2": "pressed_flowers_b", "soil_sacks": "soil", "watering_can": "watering_can",
                  "bed": "bed"},
        notes=["Fern's cottage and seedhouse: one floor plan. Two doors in, as outside: the cottage's and the seedhouse's.",
               "The cottage end: the fireplace, couch, coffee table and armchair on one rug, her pressed flowers;",
               "the kitchen and her table. A row of her plants marks where the glass room starts.",
               "The seedhouse: glass back wall, stone floor; seed drawers, the potting bench, the night-flowering",
               "vine on its trellis, the counter she sells seeds from, seedling trays, the humming mushroom.",
               "Behind, through a door: her bedroom. Orange outline = new PixelLab piece. Dotted line = walkway."]),

    # Emmett's cottage (map doc, section 5: "small. One room: instruments, maps, a bed";
    # emmett_brief.md: a fiddler who has played every town in Ferngill and has no place
    # of his own; rain days here, writing). The room, and his bed in a nook behind it.
    "emmett": dict(
        title="Emmett's cottage", location="blinxxy.MuseArt_ValeEmmett", tmx="ValeEmmett.tmx",
        W=18, H=21, outside=(19, 61),
        rooms={"nook": dict(a=1, b=9, t=0, f=8, label="The nook"),
               "room": dict(a=1, b=15, t=9, f=17, label="Emmett's room")},
        doors=[("nook", "room", 5)],
        front=("room", 10),
        zones=[],
        # plain cream plaster, as outside (the user, 2026-11-07: the interior should reflect
        # the cottage's plaster and beams; and moss: "a rougher more homey guy")
        wall=103,
        picks={"chest": "art/gilded_vale/interiors/pottery/pottery_v1/chest_1_9",
               **EMMETT_PICKS},
        lights=[(4, 15), (9, 15), (13, 15), (4, 6), (2, 1), (4, 1), (6, 1), (8, 1)],
        pieces=[
            # by the fire: logs, his armchair turned to it on a rug, a pouf for a guest
            # by the fire: logs, his armchair turned to it on a rug, a mossy stump for a guest,
            # a crate for a side table
            ("fireplace_stone", 2, 10), ("logs", 1, 13), ("rug", 4, 14, 4, 3), ("armchair_w", 5, 14),
            ("stump_stool", 4, 13), ("crate_table", 7, 16),
            # the maps of everywhere he's been, pinned over the desk where he writes
            ("pinned_maps", 7, 10), ("writing_desk", 8, 12),
            # his songbooks, the window, his instruments and a music stand
            ("bookcase", 11, 11), ("window", 14, 10), ("instrument_stand", 13, 12), ("music_stand", 10, 12),
            ("coat_rack", 15, 13), ("fern_pot", 1, 15), ("chest", 6, 13),
            # moss: creeping down the plaster, and in the corners of the floor
            # one beam along the wall: ivy at its ends, bare wood over the bookcase (the user, 2026-11-07)
            ("moss_wall_l", 10, 10), ("beam", 11, 10), ("beam#2", 12, 10), ("moss_wall_r", 13, 10), ("moss_patch", 11, 14), ("moss_patch#2", 15, 16),
            ("moss_patch#3", 9, 16), ("moss_patch#4", 14, 16),
            # the nook: his bed, a lantern on the nightstand, the trunk he travelled with
            # the nook, under the eaves: one small window in the middle, like the gable's outside
            ("bed", 6, 4), ("nightstand", 8, 3), ("window", 5, 1), ("chest#2", 4, 4), ("fern_pot#2", 2, 4),
            # either side of the window an ivy beam, its wood running on to a lamp at each end
            # (the user, 2026-11-07); the lamp is the game's, hung over the beam's wood
            ("beam_l", 2, 1), ("sconce", 2, 1), ("moss_wall#3", 3, 1), ("beam_r", 4, 1), ("sconce#2", 4, 1),
            ("beam_l#2", 6, 1), ("sconce#3", 6, 1), ("moss_wall#4", 7, 1), ("beam_r#2", 8, 1), ("sconce#4", 8, 1), ("moss_patch#5", 4, 6), ("moss_patch#6", 8, 6),
        ],
        # right-click to read (i18n vale.emmett.<key>); "chest#2" is the trunk he travelled with
        describe={"pinned_maps": "maps", "instrument_stand": "instruments", "music_stand": "music_stand",
                  "coat_rack": "coat", "writing_desk": "desk", "chest#2": "trunk", "bookcase": "songbooks",
                  "stump_stool": "stump", "crate_table": "crate", "bed": "bed"},
        notes=["Emmett's cottage: small, one floor plan, cream plaster walls as outside. The front door opens into his room.",
               "By the stone fireplace (under the chimney outside): his armchair turned to it on a rug, a mossy stump for a guest, a crate for a side table, logs.",
               "Over the desk where he writes on rainy days: maps of everywhere he's played, pinned up.",
               "His songbooks, his instruments on a stand (fiddle, the concertina, Tobin's ocarina), a music stand, his coat.",
               "Behind, through a door: the nook under the eaves, one small window like the gable's, his bed and the trunk he travelled with.",
               "Moss creeps down the plaster in places and grows in the corners of the floor; a fern in an old bucket in each room.",
               "Orange outline = new PixelLab piece. Dotted line = walkway."]),

    # The big Old Studio (map doc, sections 1, 5 and 8; lupini_brief.md: boarded up until
    # Lupini's 6-heart event, the unfinished painting, its sketches and a note; a painting
    # hung backward). Stone outside with a north skylight, a lower wing to the east: the
    # studio, and the artist's back room in the wing.
    "studio": dict(
        title="The Old Studio", location="blinxxy.MuseArt_ValeStudio", tmx="ValeStudio.tmx",
        W=24, H=23, outside=(73, 38),
        rooms={"back":   dict(a=14, b=21, t=0, f=8, label="The back room"),
               "studio": dict(a=1, b=21, t=9, f=19, label="The studio")},
        doors=[("back", "studio", 17)],
        front=("studio", 12),
        zones=[],
        wall=12, walls={"back": 103},           # bare stone, as outside; the back room plastered
        picks={"chest": "art/gilded_vale/interiors/pottery/pottery_v1/chest_1_9",
               "canvas_stack": "art/gilded_vale/interiors/atelier/atelier_v1/canvas_stack_1_5",
               **{k: EMMETT_PICKS[k] for k in ("moss_wall", "moss_patch", "fern_pot")},
               # no new PixelLab (the user, 2026-11-07: saving it for the characters): spare candidates
               # of earlier batches, edited or drawn by tools/art/vale_studio_art.py
               **{k: "art/gilded_vale/interiors/studio/" + k for k in (
                   "dusty_window", "pinned_sketches", "backward_painting", "cobweb", "unfinished_easel", "note_table",
                   "leaves", "rules_board")},
               "paint_trestle": "art/gilded_vale/interiors/atelier/atelier_v1/brush_table_1_13",
               "sheeted_chair": "art/gilded_vale/interiors/hall/hall_v3/sheeted_statue_1_4",
               "sheeted_chair_b": "art/gilded_vale/interiors/hall/hall_v3/sheeted_statue_1_6",
               "washstand": "art/gilded_vale/interiors/vale_house/vale_house_v2/washstand_1_2"},
        lights=[(7, 15), (12, 16), (17, 15), (4, 18), (17, 5)],
        pieces=[
            # the back wall: the tall window (its light where the easel stands), the sketches
            # pinned up, the painting hung backward; ivy and cobwebs getting in
            ("dusty_window", 4, 10), ("pinned_sketches", 10, 10),
            ("backward_painting", 13, 10), ("cobweb", 20, 10), ("cobweb#2", 1, 10),
            # the studios' house rules, signed by the artists who kept them (the user, 2026-11-08)
            ("rules_board", 15, 10),
            # the game's own furniture, left as it was (the user, 2026-11-07: "more vanilla
            # furniture to liven up the place"): the clock that stopped, a hall table under
            # the sketches, a bookcase, a plant by the door through
            ("clock", 2, 12), ("hall_table", 10, 12), ("plant", 15, 12), ("bookcase", 19, 11),
            # in the window's light: the unfinished painting on its easel; the note on a little
            # table beside it; the old paint trestle; canvases left leaning
            # (the user, 2026-11-07: the trestle and the note's table back against the wall, the
            # canvases leaning on the west wall: canvases on the floor always lean on something)
            ("unfinished_easel", 4, 14), ("paint_trestle", 5, 12), ("note_table", 8, 12),
            ("canvas_stack", 1, 15), ("sheeted_chair", 21, 15), ("sheeted_chair_b", 20, 17),
            # the long table the painters ate at, its chairs round it, on a rug
            ("rug#3", 6, 16, 7, 3), ("dining", 7, 16),
            # where the painters sat: the coffee table, an armchair and a pouf on one rug, a lamp
            ("rug", 14, 15, 6, 4), ("coffee_table", 15, 15), ("armchair_w", 18, 15), ("pouf", 14, 17),
            ("floor_lamp", 20, 13), ("plant_b", 1, 17),
            ("fern_pot", 1, 13),
            # leaves blown in under the door, moss in the corners
            ("leaves", 4, 18), ("leaves#2", 13, 17), ("leaves#3", 13, 14), ("moss_patch", 1, 18),
            ("moss_patch#2", 21, 13), ("moss_patch#3", 4, 13),
            # the back room: the artist's cot, a washstand, a trunk, a small window
            ("washstand", 14, 2), ("window", 17, 1), ("bed", 19, 4), ("chest", 17, 4),
            ("rug#2", 16, 5, 3, 3), ("plant", 21, 4),
            ("cobweb#3", 21, 1), ("leaves#4", 21, 7), ("moss_patch#4", 14, 4),
        ],
        describe={"unfinished_easel": "unfinished", "note_table": "note", "pinned_sketches": "sketches",
                  "backward_painting": "backward", "dusty_window": "window", "paint_trestle": "trestle",
                  "bed": "cot", "washstand": "washstand", "chest": "trunk", "rules_board": "rules"},
        notes=["The big Old Studio: one floor plan, bare stone walls as outside. The front door opens into the studio.",
               "Boarded up for years until Lupini's 6-heart event: the tall window lights the unfinished painting on its easel,",
               "the note on a little table beside it, the sketches for it pinned to the wall, a painting hung facing the wall.",
               "An old paint trestle, canvases left leaning, chairs under dust sheets; ivy and cobwebs, leaves blown in.",
               "Behind, in the east wing, through a door: the artist's back room (a cot, a washstand, a trunk).",
               "Right-click the painting, the note, the sketches and the rest to read about them. Orange = new PixelLab piece."]),
    # Studio II, the Corrie twins' (docs/design/vale_newcomers.md, section 5; corrie_twins_brief.md):
    # renovated by Robin, theirs from Spring 1, Year 3. A workshop with a counter, the city's money
    # showing, split down the middle by a black line: Opal's half (west) a riot of colour, Jet's
    # (east) spotless. Mabel's bench on the line under the big window, kept and never used. A bed
    # at each end. The heart stages (STUDIO2_STAGES) swap pieces through content-pack conditions.
    "studio2": dict(
        title="Studio II, the Corries'", location="blinxxy.MuseArt_ValeStudio2", tmx="ValeStudio2.tmx",
        W=22, H=16, outside=(86, 38),
        rooms={"studio": dict(a=1, b=20, t=0, f=12, label="Studio II")},
        doors=[],
        front=("studio", 7),
        # Jet's half floored in the game's grey stone tiles, Opal's the planks (the user, 2026-11-09):
        # the lead line runs where the two floors meet
        zones=[(11, 4, 20, 12, "stone tiles")],
        # Opal's half papered in the game's soft multicolour stripe, Jet's in plain plaster (the user,
        # 2026-11-09: the jewel squares and flat grey first tried looked bad)
        wall=103, wall_zones=[("studio", 1, 10, 97)], zone_colours={97: (236, 222, 214), 103: (214, 190, 170)},
        all_picks=True,         # the sheet holds the heart stages' pieces too (STUDIO2_STAGES)
        picks={**{k: "art/gilded_vale/interiors/studio2/" + k for k in ("colour_pool", "small_window", "sketch_papers", "split_line", "split_line_b",
                                                                 # the windows drawn as the outside's are (vale_corrie_art.py)
                                                                 "studio_window", "mabel_window")},
               # Higgsfield (the user, 2026-11-09: "use higgsfield credits for the objects"), fitted to
               # their footprints by tools/art/object_fit.py (art/gilded_vale/interiors/studio2/README.md)
               **{k: f"art/gilded_vale/interiors/studio2/higgsfield/{k}_{n}_fit" for k, n in (
                   ("design_sketches", 3),
                   ("glass_lamp", 3), ("offcut_bins", 2), ("mabel_bench_used", 1))},
               # PixelLab, Pro Flash (the user, 2026-11-09)
               **{k: "art/gilded_vale/interiors/studio2/pixellab/" + k for k in (
                   "glass_counter", "cutting_bench", "light_table", "mabel_bench", "glass_racks", "modern_kiln",
                   "drafting_table")},     # (the drafting table too, the user 2026-11-09: as detailed as the bench)
               "ladder": "art/gilded_vale/interiors/hall/hall_v1/ladder_1_8"},
        lights=[(3, 3), (10, 3), (13, 4), (18, 3), (4, 9)],
        pieces=[
            # (the beds a row up, their first row on the wainscot: the Saloon bed's top row is a scrap
            # of the Saloon's own wall, which showed on the floor above them, the user 2026-11-09)
            # ---- Opal's half, the west: her bed at the end, a city lamp, the glass racks
            # throwing colour on the floor, her designs pinned up over her drawing board
            # (the small window behind the racks first, so the racks stand in front of it; its
            # daylight through their glass is the colour on the floor, the user 2026-11-09)
            ("bed", 1, 3), ("city_lamp", 3, 2), ("small_window", 4, 1), ("glass_racks", 4, 2), ("colour_pool", 4, 5),
            ("design_sketches", 6, 1), ("drafting_table", 6, 3), ("sketch_papers", 2, 9), ("sketch_papers#2", 8, 6),
            # the counter by the door, where she sells; a rug (the panels that leaned on its side went:
            # at 16 pixels wide they read as a pile of stuff, the user 2026-11-09)
            ("glass_counter", 3, 9), ("rug", 6, 8, 4, 3),
            # ---- the middle: the big window, Mabel's bench under it on the line; the line itself
            ("studio_window", 9, 1), ("mabel_bench", 10, 3),
            *[("split_line" + ("#%d" % y if y > 5 else ""), 10, y) for y in range(5, 13)],
            *[("split_line_b" + ("#%d" % y if y > 5 else ""), 11, y) for y in range(5, 13)],
            # ---- Jet's half, the east: the light table (the big window's light; the small window that
            # was over it went, the user 2026-11-09), the ladder up to
            # the loft, the sleek city kiln, a city lamp, his bed at the end; his cutting bench and
            # the offcuts sorted by colour, nothing on the floor
            ("light_table", 13, 3), ("ladder", 15, 2), ("modern_kiln", 16, 2),
            ("city_lamp#2", 18, 2), ("bed#2", 19, 3),
            ("cutting_bench", 13, 8), ("offcut_bins", 19, 10),
        ],
        describe={"glass_racks": "racks", "design_sketches": "designs", "drafting_table": "drafting",
                  "studio_window": "window", "mabel_bench": "bench", "light_table": "light_table",
                  "modern_kiln": "kiln", "cutting_bench": "cutting_bench", "offcut_bins": "offcuts",
                  "ladder": "ladder", "city_lamp": "city_lamp", "city_lamp#2": "city_lamp"},
        notes=["Studio II, the Corrie twins' home and workshop (from Spring 1, Year 3). One room the size of Leah's cottage.",
               "A black line runs down the middle of the floor. West: Opal's half, a riot of colour: jewel-square wallpaper, glass racks",
               "throwing colour on the floor, her designs pinned up, sketches dropped, a rug, the counter by the door where she sells.",
               "East: Jet's half, spotless grey: the light table under a small window, a sleek city kiln, his cutting bench, offcuts sorted by colour.",
               "The middle: the big studio window and, under it on the line, Mabel's bench, kept and never used. The ladder goes up to the loft.",
               "City money: a Modern Lamp (the game's own) by each bed, the steel kiln. A bed at each end.",
               "Hearts: Opal 6+, their own glass lamps replace the city ones. Jet 8+, the line has a gap by the bench.",
               "Jet 10+, Mabel's bench in use and her finished window in the studio's window. Orange = new hand-drawn piece."]),
}

# art drawn to its tiles' exact size, kept in its place in them (not stood on their bottom)
AS_DRAWN = {"split_line", "split_line_b"}       # (Studio II's line sits on the seam between two tiles)

# The Exhibition Hall closed: as it is until Lupini's 8-heart event reopens it ("closed and
# dusty", map doc section 5; lupini_brief.md). Made from the open hall's spec, so its rooms,
# walls and pieces carry over: in the gallery every display is under a dust sheet of its
# footprint, the benches too; the paintings are down (a paler square where each hung, two
# lots leaning sheeted against the wall), the plaques, palms and the runner gone; the mural
# stays. Cobwebs, leaves and dust. The back room and storeroom as they are. No new PixelLab:
# spare candidates of the hall's batches and tools/art/vale_hall_closed_art.py.
# content.json loads it as the hall's map until the mail flag blinxxy.MuseArt_ValeHallOpen.
HALL_CLOSED_ART = "art/gilded_vale/interiors/hall/"
# a display -> its sheeted shape (key, art, right-click text key: vale.hall_closed.<key>)
HALL_SHEETS = {
    "panel":       ("sheet_panel", "closed/sheet_panel", "panel"),
    "panel_b":     ("sheet_panel_b", "closed/sheet_panel_b", "panel_b"),
    "bowl_case":   ("sheet_case", "hall_v1/sheeted_1_0", "case"),
    "jug_case":    ("sheet_case_b", "hall_v1/sheeted_1_63", "case"),
    "flower_case": ("sheet_case_c", "hall_v1/sheeted_1_12", "case_b"),
    "leaf_case":   ("sheet_case_d", "hall_v1/sheeted_1_60", "case_b"),
    "plinth":      ("sheet_plinth", "hall_v3/sheeted_statue_1_44", "statue_round"),
    "plinth_b":    ("sheet_plinth_b", "hall_v3/sheeted_statue_1_52", "statue_moss"),
    "plinth_c":    ("sheet_plinth_c", "hall_v3/sheeted_statue_1_15", "statue_kiln"),
    "plinth_d":    ("sheet_plinth_d", "hall_v3/sheeted_statue_1_25", "statue_heron"),
    "reception":   ("sheet_desk", "closed/sheet_desk", "desk"),
    "bench":       ("sheet_bench", "closed/sheet_bench", "bench"),         # (by name: each its own sheet)
    "bench#2":     ("sheet_bench_b", "closed/sheet_bench_b", "bench"),
}
HALL_KEEP = {"mural", "rug"}            # (the mural's rug; the runner, rug#2, is rolled up)


def closed_hall(hall):
    gallery_top = hall["rooms"]["gallery"]["t"]
    pieces, describe, picks = [], {}, dict(hall["picks"])
    for p in hall["pieces"]:
        name, key = p[0], p[0].split("#")[0]
        if p[2] < gallery_top:                      # the back room and storeroom: as they are
            pieces.append(p)
            if name in hall["describe"]:
                describe[name] = "hall." + hall["describe"][name]
        elif name in HALL_SHEETS or key in HALL_SHEETS:
            sheet, art, text = HALL_SHEETS.get(name) or HALL_SHEETS[key]
            w, h, s = ART[key]
            bb = Image.open(HALL_CLOSED_ART + art + ".png").getbbox()
            rows = max(h, -(-(bb[3] - bb[1]) // 16))        # a sheet taller than the display rises above it
            ART[sheet] = (w, rows, s)
            picks[sheet] = HALL_CLOSED_ART + art
            pieces.append((sheet, p[1], p[2] - (rows - h)))
            describe[sheet] = text
        elif key in LUPINI:                         # taken down: a paler square, its nail
            n = LUPINI[key]
            hook = f"hook_{n}"
            ART[hook] = (*GAME_PAINTINGS[n], 0)
            AS_DRAWN.add(hook)
            picks[hook] = HALL_CLOSED_ART + "closed/" + hook
            pieces.append((hook, p[1], p[2]))
            describe[hook] = "hook"
        elif name in HALL_KEEP:
            pieces.append(p)
            if key == "mural":
                describe[name] = "mural"
    ART.update({"frames": (2, 2, 1), "frames_b": (2, 2, 1), "cobweb_l": (1, 1, 0), "dust": (1, 1, -2)})
    picks.update({"frames": HALL_CLOSED_ART + "hall_v1/sheeted_1_16", "frames_b": HALL_CLOSED_ART + "hall_v1/sheeted_1_58",
                  "cobweb": "art/gilded_vale/interiors/studio/cobweb", "cobweb_l": HALL_CLOSED_ART + "closed/cobweb_l",
                  "leaves": "art/gilded_vale/interiors/studio/leaves", "dust": HALL_CLOSED_ART + "closed/dust"})
    pieces += [
        # the paintings, taken down and leaning sheeted against the wall under where two hung
        ("frames", 16, 14), ("frames_b", 22, 14),
        # cobwebs in the corners, the back rooms' too
        ("cobweb_l", 1, 11), ("cobweb", 28, 11), ("cobweb#2", 19, 1), ("cobweb#3", 28, 1),
        # leaves blown in under the front door, dust drifted along the walls
        ("leaves", 13, 21), ("leaves#2", 16, 21), ("leaves#3", 11, 20), ("leaves#4", 15, 19),
        ("dust", 6, 21), ("dust#2", 1, 17), ("dust#3", 28, 18), ("dust#4", 21, 21), ("dust#5", 9, 21),
    ]
    describe.update({"frames": "frames", "frames_b": "frames"})
    return dict(hall, title="The Exhibition Hall, closed", tmx="ValeHallClosed.tmx", pieces=pieces,
                describe=describe, picks=picks,
                notes=["The Exhibition Hall closed, until Lupini's 8-heart event: the open hall's plan, put away.",
                       "Every display under a dust sheet of its footprint; the paintings down, a paler square where each",
                       "hung, two lots leaning sheeted against the wall; the benches sheeted; the plaques, palms and the runner gone.",
                       "The faded mural stays. Cobwebs in the corners, leaves blown in, dust. The back rooms as they are."])


BUILDINGS["hall_closed"] = closed_hall(BUILDINGS["hall"])

# Studio II through the twins' hearts (docs/design/vale_newcomers.md, section 5; the brief's "The
# interior through their hearts"): stage -> (the condition, as a Content Patcher When: the mail
# flag the heart event that changes the room sets, tools/build_corrie_events.py; and the change
# to the base layout). Each is its own EditMap over the base map
# (tools/vale_studio2.py diffs a stage's build against the base), so they combine in any order.
STUDIO2_STAGES = {
    # their own glass lamps replace the city ones
    "opal6": ({"HasFlag": "blinxxy.MuseArt_Opal6Seen"}, {"swap": {"city_lamp": "glass_lamp"}, "describe": {"glass_lamp": "glass_lamp"}}),
    # the line gets a gap by the bench, where they've started sharing the middle
    "jet8": ({"HasFlag": "blinxxy.MuseArt_Jet8Seen"}, {"remove": ["split_line", "split_line#6", "split_line#7",
                                                                  "split_line_b", "split_line_b#6", "split_line_b#7"]}),
    # Mabel's bench in use; her finished window in the studio's window
    "jet10": ({"HasFlag": "blinxxy.MuseArt_Jet10Seen"}, {"swap": {"mabel_bench": "mabel_bench_used", "studio_window": "mabel_window"},
                                          "describe": {"mabel_bench_used": "bench_used", "mabel_window": "mabel_window"}}),
}


def studio2_stage(spec, stages):
    """Studio II's spec with the given heart stages applied."""
    import copy
    out = copy.deepcopy(spec)
    out["texts_of"] = "studio2"         # (a stage's right-click texts are Studio II's)
    for st in stages:
        change = STUDIO2_STAGES[st][1]
        pieces = []
        for p in out["pieces"]:
            if p[0] in change.get("remove", []):
                continue
            key = p[0].split("#")[0]
            if key in change.get("swap", {}):
                tag = p[0][len(key):]
                p = (change["swap"][key] + tag, *p[1:])
            pieces.append(p)
        out["pieces"] = pieces
        described = {}
        for k, v in out["describe"].items():
            key = k.split("#")[0]
            to = change.get("swap", {}).get(key, key)
            described[to + k[len(key):]] = change.get("describe", {}).get(to, v)
        out["describe"] = described
    return out


# every stage at once, for the plan
BUILDINGS["studio2_hearts"] = studio2_stage(BUILDINGS["studio2"], list(STUDIO2_STAGES))

# the game's floorings (walls_and_floors) for floors that aren't planks or Sam's tiles
FLOORINGS = {"flagstones": 10, "terracotta": 21, "parquet": 16, "checker": 22, "stone tiles": 9}
FLOOR_COLOURS = {"planks": (196, 140, 82), "flagstones": (150, 146, 138), "terracotta": (186, 98, 60), "parquet": (150, 90, 50), "checker": (200, 200, 210), "stone tiles": (220, 200, 160), "kitchen tiles": (200, 214, 226),
                 "carpet": (150, 70, 70), "straw": (214, 190, 110), "glass": (170, 210, 190)}


def fronts(spec):
    """The front doors: [(room, x, (outside door x, y))]. A building may have more
    than one (Fern's cottage and her seedhouse)."""
    fr = spec["front"] if isinstance(spec["front"], list) else [spec["front"]]
    out = spec["outside"] if isinstance(spec["outside"], list) else [spec["outside"]]
    return [(room, x, o) for (room, x), o in zip(fr, out)]


def paper_at(spec, rn, x):
    """The wallpaper of room rn's back wall at column x (wall_zones: (room, x0, x1, paper))."""
    for zr, x0, x1, pp in spec.get("wall_zones", []):
        if zr == rn and x0 <= x <= x1:
            return pp
    return spec.get("walls", {}).get(rn, spec.get("wall", 0))


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
        return (*GAME_PAINTINGS.get(LUPINI[key], (2, 2)), 0)
    if key in GAME_FURN:
        return GAME_FURN[key][1:]
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
    for room, d, _ in fronts(spec):
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
    measure = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    notes = []
    for ln in spec["notes"]:                        # wrapped to the plan's width
        words = ln.split()
        notes.append(words.pop(0))
        for wd in words:
            if measure.textlength(notes[-1] + " " + wd, font=small) < W * T - 20:
                notes[-1] += " " + wd
            else:
                notes.append(wd)
    notes_h = 22 * len(notes) + 16
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
    for room, dx, _ in fronts(spec):
        f = spec["rooms"][room]["f"]
        box(dx, f, 1, 2, fill=(236, 200, 120))
        d.text((dx * T + T // 2, (f + 2) * T + 4), "FRONT DOOR", fill=(255, 255, 255), font=bold, anchor="mt")
    for zr, x0, x1, pp in spec.get("wall_zones", []):
        r = spec["rooms"][zr]
        box(x0, r["t"] + 1, x1 - x0 + 1, 2, fill=spec.get("zone_colours", {}).get(pp, (190, 225, 230)))   # a glass wall
    if "zone_colours" in spec and spec.get("wall") in spec["zone_colours"]:      # (the rest of the wall)
        for r in spec["rooms"].values():
            zoned = {x for zr, x0, x1, _ in spec.get("wall_zones", []) for x in range(x0, x1 + 1)}
            for x in range(r["a"], r["b"] + 1):
                if x not in zoned:
                    box(x, r["t"] + 1, 1, 2, fill=spec["zone_colours"][spec["wall"]])

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
    for p in sorted(spec["pieces"], key=lambda p: p[0].split("#")[0] not in RUGS):
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
    room, dx, _ = fronts(spec)[0]
    f = spec["rooms"][room]["f"]
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
    for i, ln in enumerate(notes):
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


# Studio II's PixelLab and Higgsfield pieces outline in pure black; the Vale's furniture (and the
# game's) in a dark warm brown. Near-black turns that brown as the sheet is packed (the user,
# 2026-11-09: "this giant black outline").
WARM_OUTLINES = "art/gilded_vale/interiors/studio2/"


def warm_outline(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and r + g + b < 60:
                px[x, y] = (54, 24, 20, a)
    return im


def pack(picks):
    from vanillafy import selout
    images, slots, x, y, row_h = {}, {}, 0, 0, 0
    for name, path in picks.items():
        im = Image.open(path + ".png").convert("RGBA")
        bb = im.getbbox()
        # (pieces are stood on the bottom of their cells; a few hang from the top instead)
        im = im if name in TOP_ALIGNED or name in AS_DRAWN else im.crop(bb)   # (a beam keeps its exact place in its tile)
        w, h = ART[name][:2]
        cols, rows = max(w, -(-im.width // 16)), max(h, -(-im.height // 16))
        if (cols, rows) != (w, h):
            print(f"  {name}: art is {cols}x{rows} tiles, planned {w}x{h}")
        tile = Image.new("RGBA", (cols * 16, rows * 16))
        if name in TOP_ALIGNED:
            im = im.crop((0, 0, im.width, rows * 16 - BEAM_DROP))
            tile.alpha_composite(im, ((cols * 16 - im.width) // 2, BEAM_DROP))
        elif name in AS_DRAWN:
            tile.alpha_composite(im, (0, 0))
        else:
            tile.alpha_composite(im, ((cols * 16 - im.width) // 2, rows * 16 - im.height))
        if path.startswith(WARM_OUTLINES):
            tile = warm_outline(tile)
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


def message_key(building, key):
    # (a key naming another building, "hall.poster", shares that building's text)
    return f"blinxxy.MuseArt_Vale.{key if '.' in key else building + '.' + key}"


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
             if k in spec["picks"] and spec.get("all_picks")
             or any(p[0].split("#")[0] == k for p in spec["pieces"])}   # (all_picks: a stage's pieces too)
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
        for k in range(3):
            for x in range(a, b + 1):
                g["Back"][t + 1 + k][x] = wallpaper(paper_at(spec, rn, x))[k]
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
        paper = wallpaper(paper_at(spec, lower, d))
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
    warps = []
    for room, d, (ox, oy) in fronts(spec):
        f = rooms[room]["f"]
        g["Front"][f][d - 1], g["Front"][f][d], g["Front"][f][d + 1] = 163, 0, 164
        g["Front"][f + 1][d - 1], g["Front"][f + 1][d], g["Front"][f + 1][d + 1] = 97, 197, 131
        g["Back"][f + 1][d], g["Buildings"][f + 1][d] = 23, 0
        g["Back"][f + 2][d] = g["Buildings"][f + 2][d] = 0
        warps.append(f"{d} {f + 2} {VALE} {ox} {oy + 1}")

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
            pw, ph = GAME_PAINTINGS.get(n, (2, 2))
            for j in range(ph):
                for i in range(pw):
                    g["Front2" if g["Front"][py + j][px + i] else "Front"][py + j][px + i] = FURN + n + 32 * j + i
        elif key in GAME_FURN:
            n, pw, ph, sol = GAME_FURN[key]
            for j in range(ph):
                for i in range(pw):
                    x, y, gid = px + i, py + j, FURN + n + 32 * j + i
                    if j >= ph - sol:
                        g["Buildings"][y][x] = gid
                    else:
                        g["Front2" if g["Front"][y][x] else "Front"][y][x] = gid
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
                        if g["Back2"][y][x]:
                            # (a decal and a rug share Back2: the decal would cut a hole in the rug)
                            raise SystemExit(f"{p[0]} at {x},{y} lands on a rug or mat")
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
                        g["Front2"][py + 1 + j][px + i] = FURN + EASEL_PAINTING + 32 * j + i

    tile_props = [("Buildings", d_, rooms[lower]["t"] + 2, "Action", "Door") for _, lower, d_ in spec["doors"]]
    # right-click a piece (or its plaque) to read about it: Action Message, its text in
    # Strings/StringsFromMaps (content.json, from i18n vale.<building>.<key>)
    for p in spec["pieces"]:
        key = spec.get("describe", {}).get(p[0])
        if not key:
            continue
        x0, y0 = p[1], p[2]
        w, h, sol = footprint(p)
        if sol:
            cells = [(x0 + i, y0 + h - 1 - j) for i in range(w) for j in range(sol)]
        else:                                   # on the wall: its wainscot below, in reach
            r = next(r for r in rooms.values() if r["a"] <= x0 <= r["b"] and r["t"] < y0 <= r["t"] + 3)
            cells = [(x0 + i, y) for i in range(w) for y in (r["t"] + 2, r["t"] + 3)]
        for x, y in cells:
            if g["Buildings"][y][x]:
                tile_props.append(("Buildings", x, y, "Action", f"Message {message_key(spec.get('texts_of', name), key)}"))
    props = {"Warp": " ".join(warps),
             "Light": " ".join(f"{x} {y} {bmr.LIGHT_TEXTURE}" for x, y in spec["lights"])}
    if spec["doors"]:
        # the game makes a tile a door only if this lists it (x y, sheet 1, tile 120), as Haley's map does
        props["Doors"] = " ".join(f"{d_} {rooms[lower]['t'] + 2} 1 120" for _, lower, d_ in spec["doors"])
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("indoor2", T2, "townInterior_2", 512, 656),
                ("zz_wallsfloors", WF, "walls_and_floors", 256, 688),
                ("z_blinxxy.MuseArt_" + sheet_name[:-4], furn_first, sheet_name, sheet.width, sheet.height)]
    if spec.get("all_picks") or any(p[0].split("#")[0] in LUPINI or p[0].split("#")[0] in GAME_FURN
                                    or p[0] == "display_easel" for p in spec["pieces"]):
        tilesets.append(("zz_furniture", FURN, "../TileSheets/furniture", 512, 1488))
    write_raw(os.path.join(assets, spec["tmx"]), W, H, g, props, tilesets, tile_props, bmr.LAYERS)
    print("wrote", spec["tmx"], f"{W}x{H}")
    # the walkways, on the built map: from the front door's mat to every room (a door's
    # foot, 121, opens as you walk up)
    walk = {(x, y) for y in range(H) for x in range(W) if g["Buildings"][y][x] in (0, 121) and g["Back"][y][x] != 1}
    room, d, _ = fronts(spec)[0]
    f = rooms[room]["f"]
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

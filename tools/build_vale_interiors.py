"""Builds the Gilded Vale's interiors (1.4, in development; the dev pack only).

Each room is a decoratable shell cut from the game's own Shed, widened by
repeating its middle columns and rows (as tools/build_margot_room.py does for
Margot's rooms), so its walls are straight, three tiles tall, and the game
papers and floors them itself (WallID / FloorID; Data/Locations makes each a
DecoratableLocation). Furniture is PixelLab art made for each room
(art/gilded_vale/interiors/<room>/), outlines recoloured, packed on one sheet.

  ValeHouse.tmx         Bea's inn, downstairs: the tea room. Along the back
                        wall the stairs up, the bookcase of her great-aunt's
                        guest books (her 4-heart event), sketches the
                        artists gave, her tea counter, the teapot shelf, the
                        china dresser, the fireplace with two armchairs (the
                        residents' evenings); tea tables, and a piano for
                        Emmett. The front door out to the Vale.
  ValeHouseUpstairs.tmx the guest room, up the stairs: the bed you can sleep in
                        (Bed and TouchAction Sleep; the map lets you wake up
                        there). Bea's rooms and the artists' room to come.
  ValeHouseInterior.png the furniture, on one sheet.
  ValeWallpapers.png    the rooms' wallpapers (Data/AdditionalWallpaperFlooring).

Usage: build_vale_interiors.py <exports dir> "[CP] Gilded Vale (dev)/assets"
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from build_shop_interior import read
from build_spouse_areas import to_sheet
from vanillafy import selout
import build_margot_room as bmr

ART = "art/gilded_vale/interiors/vale_house/vale_house_v1"
SHEET = "ValeHouseInterior.png"
SHEET_COLS = 16
# name: the picked candidate (2026-11-07)
PICKS = {"tea_counter": "tea_counter_1_1", "teapot_shelf": "teapot_shelf_1_1", "china_dresser": "china_dresser_1_0",
         "armchair": "armchair_1_0", "tea_table": "tea_table_2_4",
         "guestbook_case": "guestbook_case_1_0", "sketch_wall": "sketch_wall_1_2",
         "piano": "piano_1_1",
         # the guest room (2026-11-07: "very boring" with the tea room's chair and table)
         "wardrobe": "../vale_house_v2/wardrobe_2_0", "washstand": "../vale_house_v2/washstand_2_1",
         "rug": "../vale_house_v2/rug_2_1", "nightstand": "../vale_house_v2/nightstand_1_0",
         "writing_desk": "../vale_house_v2/writing_desk_2_1", "easel": "../vale_house_v2/easel_2_2"}
WALLPAPERS = ["wallpaper_1_2"]
VALE = "blinxxy.MuseArt_GildedVale"
INN_DOOR = (28, 19)                  # the inn's door on the Vale map (build_vale.py vale_house)
# The Vale House is one map, as the game's houses are (Pierre's, Haley's: rooms on
# one floor joined through the walls; the game never draws a staircase): the
# tea room, and the guest room behind it, through the Shed's own door frame in
# the guest room's bottom wall and an opening cut in the tea room's back wall
# (the user, 2026-11-07: stairs into a solid wall "doesn't make sense").
# Each room: the Shed's columns and rows it keeps (repeats widen it), and its
# pieces: (x, y of the top-left tile in the room, solid bottom rows; 0 on the
# wall, drawn over it). "vanilla:" pieces are the game's own tiles.
TEA = dict(
    # widened by the Shed's plain columns (2-3, 8-9): the others carry the door's frame
    cols=[0, 1, 2, 3, 2, 3, 2, 3, 4, 5, 6, 7, 8, 9, 8, 9, 8, 9, 10, 11, 12],
    rows=[0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 13],
    pieces={"guestbook_case": ((2, 2), 1), "sketch_wall": ((10, 1), 0), "teapot_shelf": ((12, 1), 0),
            "china_dresser": ((15, 2), 1), "tea_counter": ((10, 5), 2),        # out from the wall, Bea behind it
            "armchair": ((14, 7), 1), "armchair#2": ((18, 7), 1),
            "tea_table": ((3, 7), 1), "tea_table#2": ((7, 9), 1), "tea_table#3": ((12, 9), 1),
            "piano": ((1, 9), 1)},
    fireplace=(17, 1),                   # the game's brick fireplace (Haley's, the Saloon's): x, top row
    lights=[(4, 5), (10, 4), (16, 5), (10, 9), (17, 6)])
GUEST = dict(
    cols=list(range(13)), rows=[0, 1, 2, 3, 4, 5, 6, 7, 12, 13],
    # an artist's room: the bed out from the wall with a nightstand beside it, a
    # wardrobe and washstand, a writing desk and an easel by the light, a rug
    pieces={"wardrobe": ((1, 2), 1), "washstand": ((4, 2), 1), "sketch_wall": ((6, 1), 0),
            "nightstand": ((11, 5), 1), "rug": ((5, 5), -2), "writing_desk": ((1, 6), 1), "easel": ((4, 6), 1)},
    bed=(9, 4), lights=[(3, 4), (9, 5), (6, 7)])
GUEST_AT = (1, 0)                        # the guest room's top-left in the house map
TEA_AT = (0, 10)                         # the tea room's, below it
EXIT = (10, 22)                          # the front door (the tea room's own, in the house map)
FIREPLACE = {"Front": [(667, 668), (699, 700), (731, 732)], "Buildings": [(763, 764), (795, 796)]}


def pack(art):
    images, slots, x, y, row_h = {}, {}, 0, 0, 0
    for name, pick in PICKS.items():
        im = Image.open(os.path.join(art, pick + ".png")).convert("RGBA")
        im = im.crop(im.getbbox())
        cols, rows = -(-im.width // 16), -(-im.height // 16)
        tile = Image.new("RGBA", (cols * 16, rows * 16))
        tile.alpha_composite(im, ((cols * 16 - im.width) // 2, rows * 16 - im.height))
        images[name] = selout(tile)
        if x + cols > SHEET_COLS:
            x, y, row_h = 0, y + row_h, 0
        slots[name] = (x, y, cols, rows)
        x += cols
        row_h = max(row_h, rows)
    sheet = Image.new("RGBA", (SHEET_COLS * 16, (y + row_h) * 16))
    for name, (sx, sy, _, _) in slots.items():
        sheet.alpha_composite(images[name], (sx * 16, sy * 16))
    return sheet, {n: (sy * SHEET_COLS + sx, c, r) for n, (sx, sy, c, r) in slots.items()}


def furnish(grid, slots, room, saloon):
    for key, ((px, py), solid) in room["pieces"].items():
        first, cols, rows = slots[key.split("#")[0]]
        for j in range(rows):
            for i in range(cols):
                if solid == -2:
                    layer = "Back2"                     # (a rug: on the floor, walked over)
                elif solid < 0:
                    # underfoot (the stairs): the rows over the wall draw above its trim,
                    # the floor row is walked on (its warp takes you up)
                    layer = "Front2" if py + j <= 3 else "Back2"
                elif j >= rows - solid:
                    layer = "Buildings"
                else:
                    layer = "Front" if not grid["Front"][py + j][px + i] else "Front2"
                grid[layer][py + j][px + i] = (SHEET, first + j * SHEET_COLS + i)
    if "bed" in room:                   # the Saloon's spare bed, as in Margot's room
        sets, layers = saloon[0], saloon[1]
        for layer in ("Buildings", "Front"):
            lw, data = layers[layer]
            for j in range(4):
                for i in range(2):
                    gid = data[(bmr.BED_FROM[1] + j) * lw + bmr.BED_FROM[0] + i]
                    if gid:
                        grid[layer][room["bed"][1] + j][room["bed"][0] + i] = to_sheet(gid, sets)


def carve_passage(grid, x, y_top, y_bottom, sets):
    """An opening through the tea room's back wall at column x, from the guest
    room's door down to the floor: floor on Back, nothing on Buildings or Front,
    and the wall's trim down each side of it."""
    for y in range(y_top, y_bottom + 1):
        grid["Back"][y][x] = to_sheet(4689 if (x + y) % 2 == 0 else 4690, sets)      # the Shed's floor
        for l in ("Buildings", "Front", "Front2"):
            grid[l][y][x] = None
        grid["Front"][y][x - 1] = to_sheet(2241, sets)      # the Shed's right-hand wall trim, on the wall to its left
        grid["Front"][y][x + 1] = to_sheet(2245, sets)      # and its left-hand trim, on the wall to its right


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    shed = read(os.path.join(exports, "Shed.tmx"))
    saloon = read(os.path.join(exports, "Saloon.tmx"))
    sheet, slots = pack(ART)
    sheet.save(os.path.join(assets, SHEET))
    papers = Image.new("RGBA", (256, 48))
    for i, name in enumerate(WALLPAPERS):
        papers.alpha_composite(Image.open(os.path.join(ART, name + ".png")).convert("RGBA").crop((0, 0, 16, 48)), (i * 16, 0))
    papers.save(os.path.join(assets, "ValeWallpapers.png"))
    tilesets = [("indoor", 1, "townInterior", 512, 1088),
                ("walls_and_floors", 2177, "walls_and_floors", 256, 688),
                ("z_blinxxy.MuseArt_ValeHouseInterior", 2865, SHEET, sheet.width, sheet.height)]
    tea, guest = bmr.shell(shed, TEA), bmr.shell(shed, GUEST)
    furnish(tea, slots, TEA, saloon)
    fx, fy = TEA["fireplace"]
    for layer, rows in FIREPLACE.items():
        for j, pair in enumerate(rows):
            y = fy + j + (len(FIREPLACE["Front"]) if layer == "Buildings" else 0)
            for i, gid in enumerate(pair):
                tea[layer][y][fx + i] = ("townInterior", gid - 1)
    furnish(guest, slots, GUEST, saloon)
    tw, th = len(TEA["cols"]), len(TEA["rows"])
    gw, gh = len(GUEST["cols"]), len(GUEST["rows"])
    w, h = tw, TEA_AT[1] + th
    grid = {l: [[None] * w for _ in range(h)] for l in bmr.LAYERS}
    for room, (ox, oy) in ((guest, GUEST_AT), (tea, TEA_AT)):
        rw, rh = len(room["Back"][0]), len(room["Back"])
        for l in bmr.LAYERS:
            for y in range(rh):
                for x in range(rw):
                    if room[l][y][x] is not None:
                        grid[l][oy + y][ox + x] = room[l][y][x]
    door_x = GUEST_AT[0] + 6                              # the Shed's door column
    carve_passage(grid, door_x, TEA_AT[1], TEA_AT[1] + 3, shed[0])
    tile_props = []
    for (ox, oy), rw, rh in ((GUEST_AT, gw, gh), (TEA_AT, tw, th)):
        tile_props += [(ox + x, oy + bmr.WALL_ROW, "WallID", "Wall") for x in range(1, rw - 1) if ox + x != door_x or oy != TEA_AT[1]]
        tile_props += [(ox + x, oy + y, "FloorID", "Floor") for y in range(3, rh - 1) for x in range(1, rw - 1)]
    tile_props += [(door_x, y, "FloorID", "Floor") for y in range(TEA_AT[1], TEA_AT[1] + 4)]
    bx, by = GUEST_AT[0] + GUEST["bed"][0], GUEST_AT[1] + GUEST["bed"][1]
    # A real night's stay, as on Ginger Island: the bed's tiles put you to bed, and the
    # map lets you wake up here (paying Bea is the C# mod's, later).
    tile_props += [(bx + i, by + j, "TouchAction", "Sleep") for i in range(2) for j in (2, 3)]
    lights = [(GUEST_AT[0] + x, GUEST_AT[1] + y) for x, y in GUEST["lights"]] + [(TEA_AT[0] + x, TEA_AT[1] + y) for x, y in TEA["lights"]]
    props = {"WallIDs": "Wall blinxxy.MuseArt_ValeWallpapers:0", "FloorIDs": "Floor 0",
             "Warp": f"{EXIT[0]} {EXIT[1]} {VALE} {INN_DOOR[0]} {INN_DOOR[1] + 1}",
             "Light": " ".join(f"{x} {y} {bmr.LIGHT_TEXTURE}" for x, y in lights), "AllowWakeUpWithoutBed": "T"}
    bmr.write(os.path.join(assets, "ValeHouse.tmx"), grid, w, h, props, tilesets, tile_props)
    old = os.path.join(assets, "ValeHouseUpstairs.tmx")
    if os.path.exists(old):
        os.remove(old)
    print("wrote ValeHouse.tmx", f"{w}x{h}")


if __name__ == "__main__":
    main()

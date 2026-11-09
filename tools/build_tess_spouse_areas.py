"""Builds Tess Brand's spouse room (6x9) and spouse patio (4x4) maps (1.4, the dev pack), the way
Margot's are built (build_margot_spouse_areas.py), from the approved draft (docs/tess_marriage.md):

  the room   Leah's spouse room shell (walls, wainscot, floor, window) with her things taken out;
             Tess's workbench against the back wall (where she stands), her timber stacked by kind
             leaning on the wall (the game's Wood and Log Panels), Bram's letters in the box she made,
             the Little Gilded Tree standing on it (her small carving of the Vale), the woodcut rug
  the patio  a carving bench (the game's Oak Bench) with a half-finished piece on it that changes with
             the seasons: her rabbit in spring, duck in summer, sleeping fox in fall, owl in winter
             (TessSpousePatio_<season>.tmx; data/tess_marriage.json loads the season's)

Her own pieces are packed onto TessSpouse.png; the game's furniture is referenced from its own sheet
(../TileSheets/furniture), not copied.

Usage: build_tess_spouse_areas.py <exports dir (spouseRooms.tmx)> "[CP] Gilded Vale (dev)/assets"
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from build_shop_interior import read
from build_spouse_areas import LAYERS, SPOT, to_sheet, write

SHEET = "TessSpouse.png"
COLS = 8
# piece: (source image, crop box or None for its bounding box, tiles wide, tall)
PIECES = {
    "workbench": ("art/gilded_vale/interiors/studio3/workbench.png", None, 3, 2),
    # Bram's letters in the box she made, the Little Gilded Tree standing on its lid (drawn into one
    # piece so the tree's foot sits on the lid, not on the tile above it)
    "chest":     ("art/gilded_vale/interiors/pottery/pottery_v1/chest_1_9.png", None, 2, 3),
    "rabbit":    ("art/tess/counter/carvings.png", (0, 0, 16, 16), 1, 1),
    "duck":      ("art/tess/counter/carvings.png", (16, 0, 32, 16), 1, 1),
    "fox":       ("art/tess/counter/carvings.png", (32, 0, 48, 16), 1, 1),
    "owl":       ("art/tess/counter/carvings.png", (48, 0, 64, 16), 1, 1),
}
SEASON_PIECE = {"spring": "rabbit", "summer": "duck", "fall": "fox", "winter": "owl"}
FURN = {"wood_panel": 2633, "log_panel": 2635, "rug": 1777, "bench": 192}      # the game's sprite indexes


def pack():
    from vanillafy import selout
    slots, x, y, row_h = {}, 0, 0, 0
    tiles = []
    for name, (path, box, w, h) in PIECES.items():
        im = Image.open(path).convert("RGBA")
        im = im.crop(box) if box else im.crop(im.getbbox())
        cell = Image.new("RGBA", (w * 16, h * 16))
        cell.alpha_composite(im, ((w * 16 - im.width) // 2, h * 16 - im.height))
        if x + w > COLS:
            x, y, row_h = 0, y + row_h, 0
        slots[name] = (y * COLS + x, w, h)
        cell = selout(cell) if box is None else cell
        if name == "chest":
            tree = Image.open("art/tess/counter/carvings.png").convert("RGBA").crop((112, 0, 128, 32))
            lid = h * 16 - im.height + 7                 # the tree's foot, partway onto the lid
            cell.alpha_composite(tree, ((w * 16 - im.width) // 2 + 3, lid - 32))
        tiles.append((cell, x, y))
        x += w
        row_h = max(row_h, h)
    sheet = Image.new("RGBA", (COLS * 16, (y + row_h) * 16))
    for im, tx, ty in tiles:
        sheet.alpha_composite(im, (tx * 16, ty * 16))
    return sheet, slots


def put(grid, slots, piece, x, y, solid=0, layer=None):
    first, w, h = slots[piece]
    for j in range(h):
        for i in range(w):
            target = layer or ("Buildings" if j >= h - solid else "Front")
            grid[target][y + j][x + i] = ("tess", first + j * COLS + i)


def put_furn(grid, piece, x, y, w, h, solid=0, layer=None):
    first = FURN[piece]
    for j in range(h):
        for i in range(w):
            target = layer or ("Buildings" if j >= h - solid else "Front")
            grid[target][y + j][x + i] = ("furn", first + 32 * j + i)


def tilesets(sheet, *extra):
    sets, first = [], 1
    for name, key, image, w, h in extra + (("z_blinxxy.MuseArt_TessSpouse", "tess", SHEET, sheet.width, sheet.height),
                                           ("zz_furniture", "furn", "../TileSheets/furniture", 512, 1488)):
        sets.append((name, first, image, w, h, key))
        first += (w // 16) * (h // 16)
    firsts = {key: f for _, f, _, _, _, key in sets}
    return [s[:5] for s in sets], lambda c: 0 if c is None else firsts[c[0]] + c[1]


def room(exports, sheet, slots, out):
    W, H, X0 = 6, 9, 12                          # Leah's room is the third in spouseRooms
    sets, layers, _ = read(os.path.join(exports, "spouseRooms.tmx"))
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    for l in LAYERS:
        if l not in layers:
            continue
        lw, data = layers[l]
        for y in range(H):
            for x in range(W):
                gid = data[y * lw + X0 + x]
                grid[l][y][x] = to_sheet(gid, sets) if gid else None
    # Leah's rug, palm, sculpture and wall pieces out (as for Margot's and Sable's rooms); her window stays
    for y in range(4, H):
        for x in range(1, W - 1):
            grid["Back"][y][x] = grid["Back"][y][0]
    for x in range(W):
        for y in range(3, H):
            grid["Buildings"][y][x] = None
        grid["Front"][2][x] = None if x >= 3 else grid["Front"][2][x]
    for y in (1, 2):
        for x in range(3):
            grid["Front"][y][x] = None
    for x in range(W):
        if x != 3:
            grid["Front"][1][x] = None
    # The wall is rows 0-2, the floor from row 3: pieces stand with their top on the wainscot.
    put(grid, slots, "workbench", 0, 2, solid=1)              # her bench, against the back wall (on the floor)
    put_furn(grid, "wood_panel", 4, 1, 1, 3, solid=1)         # her timber, leaning on the wall
    put_furn(grid, "log_panel", 5, 1, 1, 3, solid=1)
    # her firewood and offcuts, the game's log piles (Robin's: townInterior_2 312/344 and 313/345),
    # stacked two by two down the right-hand side under the timber (the user, 2026-11-09: more log
    # piles). Not on the room's last row: in the house that's behind the bottom wall.
    for (x, y), first in (((4, 4), 313), ((5, 4), 312), ((4, 6), 312), ((5, 6), 313)):
        grid["Front"][y][x] = ("townInterior_2", first)
        grid["Buildings"][y + 1][x] = ("townInterior_2", first + 32)
    put_furn(grid, "rug", 2, 5, 2, 2, layer="Back2")
    put(grid, slots, "chest", 0, 5, solid=1)                  # Bram's letters, in the box she made,
                                                              # the Little Gilded Tree standing on it
    for y in range(H):
        for x in range(W):
            grid["Paths"][y][x] = None
    grid["Paths"][4][1] = ("paths", SPOT)                    # at her bench
    sets_, gid = tilesets(sheet, ("indoor", "townInterior", "townInterior", 512, 1088), ("p", "paths", "paths", 64, 256),
                          ("v15_TownInterior2", "townInterior_2", "townInterior_2", 512, 656))
    write(out, W, H, sets_, grid, gid)
    print("wrote", out)


def patio(sheet, slots, season, out):
    W = H = 4
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    put_furn(grid, "bench", 1, 1, 2, 2, solid=1)              # the carving bench
    put(grid, slots, SEASON_PIECE[season], 2, 1, layer="AlwaysFront")   # the season's piece, half done, on it
    grid["Paths"][3][1] = ("paths", SPOT)                    # in front of it
    sets_, gid = tilesets(sheet, ("p", "paths", "paths", 64, 256))
    write(out, W, H, sets_, grid, gid)
    print("wrote", out)


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    sheet, slots = pack()
    sheet.save(os.path.join(assets, SHEET))
    room(exports, sheet, slots, os.path.join(assets, "TessSpouseRoom.tmx"))
    for season in SEASON_PIECE:
        patio(sheet, slots, season, os.path.join(assets, f"TessSpousePatio_{season}.tmx"))


if __name__ == "__main__":
    main()

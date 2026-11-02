"""Builds Margot's spouse room (6x9) and spouse patio (4x4) maps (1.3), as
Sable's are built (build_spouse_areas.py).

The room starts from Leah's spouse room shell (walls, wainscot, floor,
window) with her rug, palm and sculpture taken out, papered in Margot's
sage damask (Wallpapers.png, pattern 0). It's her conservation corner
(docs/margot_events.md, "Married life"), with her own pieces from her rooms
(MargotRoom.png, build_margot_room.py): the restoration table with its
magnifying lamp and a damaged painting under repair, where she stands
(Paths tile 7); her plan chest under the map of the valley; and below, a
rug, an easel with a painting half cleaned, her travelling trunk and a
museum crate with a wrapped painting.
The patio: the yard easel she paints at (from behind, frames 32-34, as
Sable does), and the drying rack of the small canvases she's started
painting herself.

Reads maps written by the museart_export_map debug command.
Usage: build_margot_spouse_areas.py <exports dir> <content pack assets dir> art/margot/room/picks
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_interior import read
from build_spouse_areas import LAYERS, SPOT, to_sheet, write
from build_margot_room import ROOM_SHEET, SHEET_COLS, pack

PAPER = "Wallpapers.png"              # the damask: pattern 0, its three rows at tiles 0, 16, 32


def place(grid, slots, piece, x, y, solid=None, layer=None):
    """A piece from MargotRoom.png with its top-left tile at (x, y): its last
    `solid` rows on Buildings, the rest on Front; or all of it on `layer`."""
    first, cols, rows = slots[piece]
    for j in range(rows):
        for i in range(cols):
            target = layer or ("Buildings" if j >= rows - solid else "Front")
            grid[target][y + j][x + i] = ("room", first + j * SHEET_COLS + i)


def tilesets(sheet, *extra):
    sets, first = [], 1
    for name, key, image, w, h in extra + (("z_blinxxy.MuseArt_MargotRoom", "room", ROOM_SHEET, sheet.width, sheet.height),):
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

    # Take out Leah's rug, palm, sculpture, paintings and the things along
    # the right wall (as for Sable's room).
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

    # Her damask on the walls, top row included (drawn on Buildings there,
    # being solid).
    damask = lambda y: ("paper", y * 16)
    for y in range(3):
        for x in range(W):
            if grid["Back"][y][x] is not None:
                grid["Back"][y][x] = damask(y)
    red = grid["Buildings"][0][0]
    for x in range(W):
        if grid["Buildings"][0][x] == red:
            grid["Buildings"][0][x] = damask(0)

    # The wall is rows 0-2 (the paper, then the wainscot), the floor from
    # row 3: pieces stand with their top on the wainscot, so nothing can be
    # walked behind.
    place(grid, slots, "table", 0, 1, solid=1)       # the restoration table, against the back wall
    place(grid, slots, "chest", 3, 2, solid=1)       # the plan chest
    place(grid, slots, "map", 4, 0, layer="Front")   # the valley map on the wall above it, clear of the window
    # The lower half: a rug, the easel with a painting half cleaned, her
    # travelling trunk and a museum crate with a wrapped painting.
    place(grid, slots, "rug", 1, 5, layer="Back2")
    place(grid, slots, "easel", 4, 4, solid=1)
    place(grid, slots, "trunk", 0, 7, solid=1)
    place(grid, slots, "crates", 4, 7, solid=1)
    for y in range(H):
        for x in range(W):
            grid["Paths"][y][x] = None
    grid["Paths"][4][1] = ("paths", SPOT)            # at her table

    sets_, gid = tilesets(sheet, ("indoor", "townInterior", "townInterior", 512, 1088), ("p", "paths", "paths", 64, 256),
                          ("v15_TownInterior2", "townInterior_2", "townInterior_2", 512, 656),
                          ("z_blinxxy.MuseArt_Wallpapers", "paper", PAPER, 256, 48))
    write(out, W, H, sets_, grid, gid)
    print("wrote", out)


def patio(sheet, slots, out):
    W = H = 4
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    # The yard easel (ArtShopYard.png: tiles 0-1, top then base), where she
    # paints, and her drying rack beside it.
    grid["Front"][1][2] = ("yard", 0)
    grid["Buildings"][2][2] = ("yard", 1)
    grid["Paths"][3][2] = ("paths", SPOT)
    place(grid, slots, "drying", 0, 2, solid=1)       # (below the back fence)
    sets_, gid = tilesets(sheet, ("p", "paths", "paths", 64, 256), ("z_blinxxy.MuseArt_ArtShopYard", "yard", "ArtShopYard.png", 16, 96))
    write(out, W, H, sets_, grid, gid)
    print("wrote", out)


def main():
    exports, assets, picks = sys.argv[1], sys.argv[2], sys.argv[3]
    sheet, slots = pack(picks)
    room(exports, sheet, slots, os.path.join(assets, "MargotSpouseRoom.tmx"))
    patio(sheet, slots, os.path.join(assets, "MargotSpousePatio.tmx"))


if __name__ == "__main__":
    main()

"""Builds Sable's spouse room (6x9) and spouse patio (4x4) maps.

The room starts from Leah's spouse room shell (walls, wainscot, floor,
window) with her rug, palm and sculpture taken out and the shop's teal
wallpaper. It's furnished as a cluttered studio from other vanilla spouse
rooms: an easel against the back wall (where Sable stands, marked by Paths
tile 7) with Leah's stool beside it, Penny's bookshelf and plant, Leah's
pottery bowls as paint pots, Maru's rug, Emily's worktable with fabric and
scissors, and a second easel of canvases in the corner.
The patio is our yard easel with Sable standing at it (Paths 7), and a
flower pot. Tiles reference the game's own tilesheets plus our yard easel.

Reads maps written by the museart_export_map debug command.
Usage: build_spouse_areas.py <exports dir> <content pack assets dir>
"""
import json, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_interior import read

LAYERS = ["Back", "Back2", "Buildings", "Paths", "Front", "AlwaysFront"]
STUDIO = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "art", "sable_room", "SableStudio.json")))
SPOT = 7                         # Paths tile marking where the spouse stands


def to_sheet(gid, sets):
    """(sheet name, index) for a gid in a map's tileset order."""
    first, sheet = max((s for s in sets if s[0] <= gid), key=lambda s: s[0])
    return sheet, gid - first


def write(path, w, h, tilesets, grid, gid_of):
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{w}" height="{h}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(LAYERS) + 1}" nextobjectid="1">']
    for name, first, image, iw, ih in tilesets:
        lines.append(f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(iw // 16) * (ih // 16)}" columns="{iw // 16}">')
        lines.append(f'    <image source="{image}" width="{iw}" height="{ih}" />')
        lines.append('  </tileset>')
    for i, layer in enumerate(LAYERS):
        lines.append(f'  <layer id="{i + 1}" name="{layer}" width="{w}" height="{h}">')
        lines.append('    <data encoding="csv">')
        rows = [",".join(str(gid_of(grid[layer][y][x])) for x in range(w)) for y in range(h)]
        lines.append(",\n".join(rows))
        lines.append('    </data>')
        lines.append('  </layer>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


def place(grid, piece, x, y, solid=None, layer=None, top=None):
    """A piece from SableStudio.png with its top-left tile at (x, y). Its last
    `solid` rows go on Buildings (you can't walk through them), the rest on
    Front (drawn over anyone behind), its first row on `top` if given (to sit
    in front of wall decor); or all of it on `layer`."""
    p = STUDIO["pieces"][piece]
    for j in range(p["rows"]):
        for i in range(p["cols"]):
            target = layer or ("Buildings" if j >= p["rows"] - solid else top if j == 0 and top else "Front")
            grid[target][y + j][x + i] = ("studio", (p["y"] + j) * STUDIO["tiles_wide"] + p["x"] + i)


def room(exports, out):
    W, H, X0 = 6, 9, 12                          # Leah's room is the third in spouseRooms
    sets, layers, _ = read(os.path.join(exports, "spouseRooms.tmx"))
    leah_sets, leah, _ = read(os.path.join(exports, "LeahHouse.tmx"))
    # Cells hold (sheet, index) so tiles from different maps mix safely.
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    for l in LAYERS:
        if l not in layers:
            continue
        lw, data = layers[l]
        for y in range(H):
            for x in range(W):
                gid = data[y * lw + X0 + x]
                grid[l][y][x] = to_sheet(gid, sets) if gid else None

    def leah_tile(layer, x, y):
        lw, data = leah[layer]
        gid = data[y * lw + x]
        return to_sheet(gid, leah_sets) if gid else None

    # Take out her rug (plain planks, as in the room's own edge column), the
    # palm, the sculpture and the things along the right wall.
    for y in range(4, H):
        for x in range(1, W - 1):
            grid["Back"][y][x] = grid["Back"][y][0]
    for x in range(W):
        for y in range(3, H):
            grid["Buildings"][y][x] = None
        grid["Front"][2][x] = None if x >= 3 else grid["Front"][2][x]

    # Teal wallpaper from the shop, top row included (it's drawn on
    # Buildings there too, being solid).
    teal = lambda y: ("townInterior_2", 297 if y % 2 == 0 else 329)
    for y in range(3):
        for x in range(W):
            if grid["Back"][y][x] is not None:
                grid["Back"][y][x] = teal(y)
    red = grid["Buildings"][0][0]
    for x in range(W):
        if grid["Buildings"][0][x] == red:
            grid["Buildings"][0][x] = teal(0)
    for y in (1, 2):                           # her palm's leaves, on the wall rows
        for x in range(3):
            grid["Front"][y][x] = None

    # Sable's own furniture (tools/art/build_studio_tiles.py), against the back
    # and right walls; the left side stays open to the rest of the house.
    for y in range(H):                           # (Leah's paintings come down too)
        for x in range(W):
            if y == 1 and x != 3:
                grid["Front"][y][x] = None
    place(grid, "board", 1, 0, solid=0)          # sketches and swatches, on the wall by the window
    place(grid, "desk", 0, 1, solid=1, top="AlwaysFront")   # the restoration desk against the wall; its lamp in front of the board
    place(grid, "cabinet", 4, 1, solid=1)        # ground pigments, amethyst, ruby, mortar
    place(grid, "rug", 1, 4, layer="Back2")      # the drop cloth, in front of the desk
    place(grid, "easel", 4, 5, solid=1)          # the canvas they keep turned to the wall
    place(grid, "rack", 0, 7, solid=1)           # small canvases drying
    place(grid, "table", 2, 7, solid=1)          # coffee and sweet peas
    for y in range(H):
        for x in range(W):
            grid["Paths"][y][x] = None
    grid["Paths"][4][3] = ("paths", SPOT)

    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("p", 2177, "paths", 64, 256),
                ("v15_TownInterior2", 2241, "townInterior_2", 512, 656),
                ("z_blinxxy.MuseArt_SableStudio", 3553, "SableStudio.png", STUDIO["tiles_wide"] * 16, STUDIO["tiles_high"] * 16)]
    firsts = {"townInterior": 1, "paths": 2177, "townInterior_2": 2241, "studio": 3553}
    write(out, W, H, tilesets, grid, lambda c: 0 if c is None else firsts[c[0]] + c[1])
    print("wrote", out)


def patio(out):
    W = H = 4
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    # Our yard easel (ArtShopYard.png: tiles 0-1 are the easel, top then base).
    grid["Front"][1][2] = ("yard", 0)
    grid["Buildings"][2][2] = ("yard", 1)
    grid["Paths"][3][2] = ("paths", SPOT)
    place(grid, "table", 0, 2, solid=1)          # coffee and sweet peas beside the easel
    tilesets = [("p", 1, "paths", 64, 256), ("z_blinxxy.MuseArt_ArtShopYard", 65, "ArtShopYard.png", 16, 96),
                ("z_blinxxy.MuseArt_SableStudio", 71, "SableStudio.png", STUDIO["tiles_wide"] * 16, STUDIO["tiles_high"] * 16)]
    firsts = {"paths": 1, "yard": 65, "studio": 71}
    write(out, W, H, tilesets, grid, lambda c: 0 if c is None else firsts[c[0]] + c[1])
    print("wrote", out)


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    room(exports, os.path.join(assets, "SableSpouseRoom.tmx"))
    patio(os.path.join(assets, "SableSpousePatio.tmx"))


if __name__ == "__main__":
    main()

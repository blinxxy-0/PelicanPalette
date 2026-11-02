"""Builds the museum's painting wing (1.3, docs/design/museum_wing.md):
PaintingWing.tmx, a gallery 29 wide and 30 deep made from the game's own
Shed shell (its borders, wallpapered walls, skirting and floor), so it's a
decoratable room papered by the game like Margot's (build_margot_room.py).

  The back wall (rows 1-3) holds the Old Valley's five; three freestanding
  gallery walls across the room (a top edge, two rows of paper and the
  skirting, rows 7-10, 14-17 and 21-24) hold the Wreck's five, the Miners'
  five, and Calico's four with the Dwarves' two. Each gallery wall stops two
  tiles short of the side walls, so there's an aisle at both ends. You come
  in from the museum's east doorway on the west side (rows 26-27).

The paintings, their cards, the benches and the paint-tube Junimo are
placed by the game (ArtStudio/Social/PaintingWing.cs), as the paintings are
donated. Reads maps written by the museart_export_map debug command.

Usage: build_museum_wing.py <exports dir> <content pack assets dir>
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_interior import read
from build_spouse_areas import to_sheet
from build_margot_room import LAYERS, LIGHT_TEXTURE, write

W, H = 29, 30
BOTTOM = 28                           # the bottom border's row
GALLERY_WALLS = [7, 14, 21]           # each gallery wall's top edge row
WALL_SPAN = (3, 25)                   # their columns
DOOR_ROWS = (26, 27)                  # the way in from the museum, on the west side
MUSEUM_DOOR = (48, 15)                # where you come out in the museum
LIGHTS = [(7, 5), (21, 5), (7, 12), (21, 12), (7, 19), (21, 19), (7, 26), (21, 26)]


def shed_tiles(exports):
    sets, layers, _ = read(os.path.join(exports, "Shed.tmx"))
    def at(layer, x, y):
        lw, data = layers[layer]
        g = data[y * lw + x]
        return to_sheet(g, sets) if g else None
    return {
        "corner_tl": at("Buildings", 0, 0), "top": at("Buildings", 1, 0), "corner_tr": at("Buildings", 12, 0),
        "left": at("Buildings", 0, 1), "right": at("Buildings", 12, 1),
        "paper": [at("Back", 1, 1), at("Back", 1, 2)], "skirting": at("Buildings", 1, 3),
        "floor": {(x % 2, y % 2): at("Back", x, 3 + y) for x in (0, 1) for y in (0, 1)},
        "bottom_l": at("Buildings", 0, 12), "bottom_r": at("Buildings", 12, 12),
        "bottom_edge_l": at("Front", 1, 12), "bottom_edge": at("Front", 2, 12), "bottom_edge_r": at("Front", 11, 12),
        "void": at("Buildings", 0, 13),
    }


def build(t):
    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    def both(x, y, tile):
        grid["Buildings"][y][x] = tile
        grid["Front"][y][x] = tile
    floor = lambda x, y: t["floor"][(x % 2, (y - 3) % 2)]
    for y in range(1, BOTTOM + 1):
        for x in range(1, W - 1):
            grid["Back"][y][x] = floor(x, y)
    # The room's border: top, sides and bottom, as the Shed's.
    both(0, 0, t["corner_tl"]); both(W - 1, 0, t["corner_tr"])
    for x in range(1, W - 1):
        both(x, 0, t["top"])
    for y in range(1, BOTTOM):
        both(0, y, t["left"]); both(W - 1, y, t["right"])
    both(0, BOTTOM, t["bottom_l"]); both(W - 1, BOTTOM, t["bottom_r"])
    grid["Front"][BOTTOM][1] = t["bottom_edge_l"]; grid["Front"][BOTTOM][W - 2] = t["bottom_edge_r"]
    for x in range(2, W - 2):
        grid["Front"][BOTTOM][x] = t["bottom_edge"]
    for x in range(W):
        grid["Buildings"][BOTTOM + 1][x] = t["void"]
    # The back wall: paper, then the skirting.
    for x in range(1, W - 1):
        grid["Back"][1][x], grid["Back"][2][x] = t["paper"]
        grid["Buildings"][3][x] = t["skirting"]
    # The gallery walls: a top edge, paper, skirting.
    x0, x1 = WALL_SPAN
    for top in GALLERY_WALLS:
        both(x0, top, t["corner_tl"]); both(x1, top, t["corner_tr"])
        for x in range(x0 + 1, x1):
            both(x, top, t["top"])
        for x in range(x0, x1 + 1):
            grid["Back"][top + 1][x], grid["Back"][top + 2][x] = t["paper"]
            grid["Buildings"][top + 3][x] = t["skirting"]
    # The way in from the museum: the west border opened.
    for y in DOOR_ROWS:
        grid["Buildings"][y][0] = grid["Front"][y][0] = None
        grid["Back"][y][0] = floor(0, y)
    # Wall areas (each column's top paper tile) and the floor, for the game's
    # wallpaper and flooring.
    walls = [(x, 1) for x in range(1, W - 1)] + [(x, top + 1) for top in GALLERY_WALLS for x in range(x0, x1 + 1)]
    wall_rows = {1, 2, 3} | {r for top in GALLERY_WALLS for r in (top, top + 1, top + 2, top + 3)}
    floors = [(x, y) for y in range(4, BOTTOM) for x in range(1, W - 1)
              if y not in wall_rows or not (x0 <= x <= x1)]
    # The paper itself can't be walked on (the gallery walls can be walked
    # up to from their ends): it stays on Back, where the game papers it,
    # marked impassable.
    solid = [(x, y + i) for x, y in walls for i in (0, 1)]
    tile_props = ([(x, y, "WallID", "Wall") for x, y in walls] + [(x, y, "FloorID", "Floor") for x, y in floors]
                  + [(x, y, "Passable", "F") for x, y in solid])
    return grid, tile_props


def main():
    exports, assets = sys.argv[1], sys.argv[2]
    grid, tile_props = build(shed_tiles(exports))
    props = {
        "WallIDs": "Wall blinxxy.MuseArt_Wallpapers:3",
        "FloorIDs": "Floor 0",
        "Warp": " ".join(f"0 {y} ArchaeologyHouse {MUSEUM_DOOR[0]} {MUSEUM_DOOR[1] + i}" for i, y in enumerate(DOOR_ROWS)),
        "Light": " ".join(f"{x} {y} {LIGHT_TEXTURE}" for x, y in LIGHTS),
    }
    tilesets = [("indoor", 1, "townInterior", 512, 1088), ("walls_and_floors", 2177, "walls_and_floors", 256, 688)]
    out = os.path.join(assets, "PaintingWing.tmx")
    write(out, grid, W, H, props, tilesets, tile_props)
    print("wrote", out)


if __name__ == "__main__":
    main()

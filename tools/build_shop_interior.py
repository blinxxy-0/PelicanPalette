"""Builds the art shop's interior maps: the shop itself, and the same room
dressed as a gallery for the 14-heart exhibition.

The room starts from the shape of Leah's main room (the frame, wallpaper,
wainscot and wood floor), with her furniture taken out and the kitchen
extension closed off. It's then stretched to 20x15: two columns either side
of the door (which ends up in the middle) and four floor rows, so there's
room for aisles like Pierre's shop.

Furnishing (2026-10-24, after the user asked for a stocked local craft
store rather than a counter and a few paintings):
  - Pierre's counter on the back wall, where Sable serves, and the
    commission board (our own art, ArtShopBoard.png).
  - Tall shelves of paint bottles, cubbies and a canvas rack along the
    back wall; low display shelves on the floor either side of the aisle
    from the door; crates, barrels, bins and baskets along the side walls.
  - Sable's easels on a rug in the corner, where they paint after closing.
  - Sable's own paintings on the walls, every one different.
The pieces are PixelLab picks packed by tools/art/build_shop_furniture.py
into ArtShopFurniture.png. A piece's bottom row(s) go on Buildings (they
block, and the farmer stands in front of them); the rows above go on Front,
so the farmer walks behind them.

The gallery keeps the room (counter, board, wall shelves, side stock and
easels) and swaps the floor shelves for white gallery panels, plinths,
spotlights, a rope line, a bench and a wine table, with abstracts on the
walls. The 14-heart event switches to it with changeToTemporaryMap.

Every other tile references the game's own tilesheets (townInterior,
townInterior_2), so no vanilla art is copied.

Reads maps written by the museart_export_map debug command.
Usage: build_shop_interior.py <exports dir> <furniture.json> <out dir> <door x> <door y>
  (furniture.json: where build_shop_furniture.py packed each piece; the
   sheet itself, ArtShopFurniture.png, is read from <out dir>, beside the maps;
   door x/y: the Forest tile you arrive on when leaving the shop)
"""
import json, sys, xml.etree.ElementTree as ET
from xml.sax.saxutils import quoteattr

LAYERS = ["Back", "Back2", "Buildings", "Front", "Front2", "AlwaysFront"]
W0, H0 = 16, 11             # Leah's room
W, H = 20, 15               # ours, stretched
DOOR_X = 9                  # the doorway column, after stretching
BOARD_X = 6                 # the commission board's left column (3 wide)
FIRE_X = 14                 # the fireplace's left column (4 wide, against the right-hand corner)
ARRIVAL = (DOOR_X, H - 2)   # where you appear coming in
FURNITURE_COLS = 16

# Our map uses Leah's tileset order, so her gids copy over unchanged.
INTERIOR, INTERIOR2, BOARD, FURNITURE = 65, 2241, 3553, 3559


def tilesets(furniture_rows):
    return [("p", 1, "paths", 64, 256, 4), ("untitled tile sheet", INTERIOR, "townInterior", 512, 1088, 32),
            ("v15_TownInterior2", INTERIOR2, "townInterior_2", 512, 656, 32),
            ("z_blinxxy.MuseArt_ArtShopBoard", BOARD, "ArtShopBoard.png", 48, 32, 3),
            ("z_blinxxy.MuseArt_ArtShopFurniture", FURNITURE, "ArtShopFurniture.png", FURNITURE_COLS * 16, furniture_rows * 16, FURNITURE_COLS)]


def read(path):
    root = ET.parse(path).getroot()
    w = int(root.get("width"))
    sets = sorted((int(t.get("firstgid")), t.find("image").get("source").replace("\\", "/").split("/")[-1]) for t in root.findall("tileset"))
    layers = {}
    for l in root.findall("layer"):
        data = [int(v) for v in l.find("data").text.replace("\n", "").split(",") if v.strip()]
        layers[l.get("name")] = (int(l.get("width") or w), data)   # layers can be a different size from the map
    props = {p.get("name"): p.get("value") for p in root.find("properties").findall("property")} if root.find("properties") is not None else {}
    return sets, layers, props


def to_ours(gid, sets):
    """Convert a gid from another map's tileset order to ours."""
    if gid == 0:
        return 0
    first, sheet = max((s for s in sets if s[0] <= gid), key=lambda s: s[0])
    base = {"paths": 1, "townInterior": INTERIOR, "townInterior_2": INTERIOR2}[sheet]
    return base + (gid - first)


def shell(leah):
    """Leah's main room with her things taken out and the kitchen closed off,
    in her 16x11 coordinates."""
    grid = {name: [[0] * W0 for _ in range(H0)] for name in LAYERS}
    for name in LAYERS:
        if name not in leah:
            continue
        w, data = leah[name]
        for y in range(H0):
            for x in range(W0):
                grid[name][y][x] = data[y * w + x]

    def put(layer, x, y, gid):
        grid[layer][y][x] = gid

    def clear(layers, x0, y0, x1, y1):
        for layer in layers:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    grid[layer][y][x] = 0

    # Every wall decoration goes (our paintings replace them), and the
    # chimney and fireplace on the right become plain wall.
    clear(["Front"], 2, 1, 13, 3)
    for x in (11, 12):                      # chimney cap in the top frame
        put("Front", x, 0, grid["Front"][0][9])
        put("Buildings", x, 0, 0)
    for x in range(2, 14):                  # plain wallpaper and wainscot
        put("Back", x, 0, grid["Back"][0][9])
        put("Back", x, 1, grid["Back"][1][9])
        put("Back", x, 2, grid["Back"][2][9])
        put("Buildings", x, 2, grid["Buildings"][2][10])
        put("Buildings", x, 3, grid["Buildings"][3][10])
        put("Buildings", x, 4, 0)
    put("Buildings", 14, 4, grid["Buildings"][4][14])
    # Her furniture.
    clear(["Buildings", "Front", "Front2"], 2, 4, 13, 7)
    clear(["Buildings"], 1, 5, 14, 8)
    clear(["Front", "Front2"], 1, 5, 14, 7)
    clear(["Back2"], 0, 0, W0 - 1, H0 - 1)
    # Floor: even planks wherever her furniture left marks.
    for y in range(4, 9):
        for x in range(1, 15):
            put("Back", x, y, grid["Back"][5 + (y % 2)][2 + (x % 2)] if y > 4 else grid["Back"][4][2 + (x % 2)])

    # Close off the kitchen extension: frame the bottom right.
    bottom = grid["Front"][8][2]
    lw, lfront = leah["Front"]
    for x in range(9, 14):
        put("Front", x, 8, bottom)
        put("Front2", x, 8, 0)
    put("Front", 14, 8, lfront[14 * lw + 14])   # bottom-right corner pieces
    put("Front", 15, 8, lfront[14 * lw + 15])
    for y in (9, 10):
        for x in range(9, 16):
            for layer in LAYERS:
                put(layer, x, y, 0)
            put("Buildings", x, y, 65)
    put("Front", 8, 9, lfront[9 * lw + 8])      # the doorway's right-hand frame, as in Leah's
    put("Front", 8, 10, 195)
    put("Front", 9, 8, bottom)
    for x in (4, 5):                            # where her plant stood by the door
        put("Front", x, 8, bottom)
    return grid


def stretch(grid):
    """16x11 -> 20x15: copies of plain columns 5 and 11 (two each, so the
    floor's plank pattern keeps its rhythm), and of plain floor rows."""
    for after in (11, 5):
        for rows in grid.values():
            for r in rows:
                r[after + 1:after + 1] = [r[after]] * 2
    for rows in grid.values():
        rows[8:8] = [list(rows[i]) for i in (5, 7, 5, 7)]
    return grid


class Room:
    def __init__(self, grid, pieces):
        self.grid, self.pieces = grid, pieces
        self.actions = []

    def put(self, layer, x, y, gid):
        self.grid[layer][y][x] = gid

    def place(self, name, x, y, solid=1):
        """Puts a packed piece with its top-left tile at (x, y). Its bottom
        `solid` rows block (Buildings); the rows above draw over the farmer
        (Front, or Front2 where Front is taken). Wall art takes solid=0: it
        hangs over the wall, whose wainscot rail is on Buildings and must
        stay (a painting's lower tiles there replaced the rail)."""
        p = self.pieces[name]
        for j in range(p["h"]):
            for i in range(p["w"]):
                gid = FURNITURE + (p["y"] + j) * FURNITURE_COLS + p["x"] + i
                if gid in self.empty:
                    continue
                if j >= p["h"] - solid:
                    self.put("Buildings", x + i, y + j, gid)
                else:
                    self.put("Front2" if self.grid["Front"][y + j][x + i] else "Front", x + i, y + j, gid)
        return p

    def copy(self, src, layer, sx, sy, dx, dy, w=1, h=1):
        sets, layers = src
        pw, data = layers[layer]
        for j in range(h):
            for i in range(w):
                self.put(layer, dx + i, dy + j, to_ours(data[(sy + j) * pw + sx + i], sets))


def furnish(room, leah, pierre, gallery):
    lw, lfront = leah[1]["Front"]
    bw, lbuild = leah[1]["Buildings"]
    l2w, lback2 = leah[1]["Back2"]

    # --- The back wall -----------------------------------------------------
    # Pierre's counter, right up against the wainscot as in his shop: the
    # keeper stands on the row its top covers (row 4) and walks in at the
    # open right end; the left end meets the wall. Register, glass case of
    # pigments, bottle cabinet.
    room.copy(pierre, "Front", 3, 17, 2, 4, w=4)
    room.copy(pierre, "Buildings", 3, 18, 2, 5, w=4)
    room.actions += [("Buildings", x, 5, "blinxxy.MuseArt_Counter") for x in range(2, 6)]
    # The commission board beside the counter.
    for j in range(2):
        for i in range(3):
            room.put("Front", BOARD_X + i, 1 + j, BOARD + j * 3 + i)
    room.actions += [("Buildings", x, 3, "blinxxy.MuseArt_Board") for x in range(BOARD_X, BOARD_X + 3)]
    # Paintings over the counter; the cubbies, and another painting before
    # the fireplace.
    walls = ("painting_blocks", "painting_mosaic") if gallery else ("painting_fruit_jug", "painting_cabin")
    room.place(walls[0], 2, 1, solid=0)
    room.place(walls[1], 4, 1, solid=0)
    room.place("rack_cubbies", 9, 2)
    room.place("painting_stripes" if gallery else "painting_river", 12, 1, solid=0)
    if not gallery:
        room.place("stack_canvases", 12, 3)         # blank canvases leaning on the wall
    # Leah's fireplace (her tiles x 10-13: log piles either side of the
    # hearth, the chimney cap in the frame) in the right-hand corner, under
    # the exterior's chimney.
    lw_, lback = leah[1]["Back"]
    for i in range(4):
        sx, dx = 10 + i, FIRE_X + i
        room.put("Back", dx, 4, lback[4 * lw_ + sx])
        for y in (2, 3, 4):
            room.put("Buildings", dx, y, lbuild[y * bw + sx])
        for y in ((1, 2, 3) if i in (1, 2) else (3,)):     # not her painting and plant either side
            room.put("Front", dx, y, lfront[y * lw + sx])
        if i in (1, 2):                     # the stack through the top frame, with nothing behind it (as hers)
            room.put("Front", dx, 0, lfront[sx])
            room.put("Back", dx, 0, lback[sx])

    # --- Sable's corner: easels on a rug, bottom right ---------------------
    rug = [[lback2[y * l2w + x] for x in range(10, 14)] for y in range(5, 8)]
    for j, row in enumerate(rug):
        for i, gid in enumerate(row):
            room.put("Back2", 14 + i, 10 + j, gid)
    easels = (("easel_lighthouse", 14), ("easel_sunset", 15)) + ((("easel_tree", 17),) if not gallery else ())
    for name, ex in easels:
        room.place(name, ex, 10)

    # --- Along the side walls ----------------------------------------------
    # Open bins of supplies down the left-hand wall, one tile wide like
    # Pierre's seed bins, with his kind of barrel below them; a palm in the
    # far corner.
    room.place("bins_column", 1, 5, solid=6)          # against the wall: all of it blocks
    room.copy(pierre, "Front", 1, 23, 1, 11)
    room.copy(pierre, "Buildings", 1, 24, 1, 12)
    room.copy(pierre, "Front", 1, 17, 18, 11)
    room.copy(pierre, "Buildings", 1, 18, 18, 12)

    if not gallery:
        # --- The shop floor: two rows of low shelves either side of the
        # aisle (the row in front of the counter stays clear for customers),
        # the canvas rack and stock by the right-hand wall.
        room.place("crate_supplies", 2, 8)          # stock beside the bins, as Pierre's crates sit by his
        room.place("low_paints", 5, 7)
        room.place("low_tubes", 5, 10)
        room.place("low_craft", 11, 7)
        room.place("low_craft_b", 11, 10)
        room.place("rack_canvases", 16, 6)
        return

    # --- The exhibition ----------------------------------------------------
    # The floor shelves are cleared away for the evening, and the rest is
    # kept sparse: a room full of guests needs the floor (the user found the
    # first dressing far too crowded). Two white panels well apart either
    # side of the aisle from the door (Evelyn's landscape on the left,
    # portraits on the right), wine and cheese and a bust by the right-hand
    # wall.
    room.place("gallery_panel_landscape", 5, 7)
    room.place("gallery_panel_portraits", 12, 7)
    room.place("gallery_wine", 15, 6)
    room.place("gallery_bust", 17, 6)


def main():
    exports, furniture_json, out_dir, door_x, door_y = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
    leah = read(f"{exports}/LeahHouse.tmx")
    pierre = read(f"{exports}/SeedShop.tmx")
    pieces = json.load(open(furniture_json))
    from PIL import Image
    sheet = Image.open(f"{out_dir}/ArtShopFurniture.png").convert("RGBA")
    rows = sheet.height // 16
    empty = {FURNITURE + j * FURNITURE_COLS + i for j in range(rows) for i in range(FURNITURE_COLS)
             if sheet.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)).getbbox() is None}

    # Not Leah's DayTiles/NightTiles: they swap her window and the patch of
    # sunlight on her floor, and our wall has paintings where her window was
    # (at night the swap drew wallpaper over one of them).
    props = {"Warp": f"{DOOR_X} {H - 1} Forest {door_x} {door_y}"}
    for gallery, name in ((False, "ArtShop.tmx"), (True, "ArtShopGallery.tmx")):
        room = Room(stretch(shell(leah[1])), pieces)
        room.empty = empty
        furnish(room, (leah[0], leah[1]), (pierre[0], pierre[1]), gallery)
        write(f"{out_dir}/{name}", room.grid, props, room.actions, rows)
        print(f"wrote {out_dir}/{name}")


def write(path, grid, props, actions, furniture_rows):
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(LAYERS) * 2 + 1}" nextobjectid="{len(actions) + 1}">',
             '  <properties>']
    for k, v in props.items():
        lines.append(f'    <property name="{k}" value={quoteattr(v)} />')
    lines.append('  </properties>')
    for name, first, image, w, h, cols in tilesets(furniture_rows):
        lines.append(f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(w // 16) * (h // 16)}" columns="{cols}">')
        lines.append(f'    <image source="{image}" width="{w}" height="{h}" />')
        lines.append('  </tileset>')
    for i, name in enumerate(LAYERS):
        lines.append(f'  <layer id="{i + 1}" name="{name}" width="{W}" height="{H}">')
        lines.append('    <data encoding="csv">')
        rows = [",".join(str(v) for v in row) for row in grid[name]]
        lines.append(",\n".join(rows))
        lines.append('    </data>')
        lines.append('  </layer>')
    oid = 1
    for i, name in enumerate(LAYERS):
        mine = [a for a in actions if a[0] == name]
        lines.append(f'  <objectgroup id="{len(LAYERS) + i + 1}" name="{name}">')
        for _, x, y, action in mine:
            lines.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            lines.append(f'      <properties><property name="Action" value={quoteattr(action)} /></properties>')
            lines.append('    </object>')
            oid += 1
        lines.append('  </objectgroup>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()

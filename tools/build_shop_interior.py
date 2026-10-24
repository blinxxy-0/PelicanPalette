"""Builds the art shop's interior map from vanilla interior tiles.

The room starts from the shape of Leah's main room (the frame, wallpaper,
wainscot and wood floor), with her furniture taken out and the kitchen
extension closed off. Then it's furnished as a shop: Pierre's counter
against the back wall, a framed corkboard for commissions, easels on a rug,
her paintings, and her bookshelf in the corner. Every tile references the
game's own tilesheets (townInterior, townInterior_2), so no vanilla art is
copied; the board is our own (ArtShopBoard.png, from tools/art/commission_board.py).

Reads maps written by the museart_export_map debug command.
Usage: build_shop_interior.py <exports dir> <out.tmx> <door x> <door y>
  (door x/y: the Forest tile you arrive on when leaving the shop)
"""
import sys, xml.etree.ElementTree as ET
from xml.sax.saxutils import quoteattr

W, H = 16, 11
LAYERS = ["Back", "Back2", "Buildings", "Front", "Front2", "AlwaysFront"]

# Our map uses Leah's tileset order, so her gids copy over unchanged.
TILESETS = [("p", 1, "paths", 64, 256, 4), ("untitled tile sheet", 65, "townInterior", 512, 1088, 32),
            ("v15_TownInterior2", 2241, "townInterior_2", 512, 656, 32),
            ("z_blinxxy.MuseArt_ArtShopBoard", 3553, "ArtShopBoard.png", 48, 32, 3)]
INTERIOR, INTERIOR2, BOARD = 65, 2241, 3553


def read(path):
    root = ET.parse(path).getroot()
    w = int(root.get("width"))
    sets = sorted((int(t.get("firstgid")), t.find("image").get("source").replace("\\", "/").split("/")[-1]) for t in root.findall("tileset"))
    layers = {}
    for l in root.findall("layer"):
        data = [int(v) for v in l.find("data").text.replace("\n", "").split(",") if v.strip()]
        layers[l.get("name")] = (w, data)
    props = {p.get("name"): p.get("value") for p in root.find("properties").findall("property")} if root.find("properties") is not None else {}
    return sets, layers, props


def to_ours(gid, sets):
    """Convert a gid from another map's tileset order to ours."""
    if gid == 0:
        return 0
    first, sheet = max((s for s in sets if s[0] <= gid), key=lambda s: s[0])
    base = {"paths": 1, "townInterior": INTERIOR, "townInterior_2": INTERIOR2}[sheet]
    return base + (gid - first)


def main():
    exports, out, door_x, door_y = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    leah_sets, leah, leah_props = read(f"{exports}/LeahHouse.tmx")
    pierre_sets, pierre, _ = read(f"{exports}/SeedShop.tmx")

    grid = {name: [[0] * W for _ in range(H)] for name in LAYERS}
    for name in LAYERS:
        if name not in leah:
            continue
        w, data = leah[name]
        for y in range(H):
            for x in range(W):
                grid[name][y][x] = data[y * w + x]

    def put(layer, x, y, gid):
        grid[layer][y][x] = gid

    def clear(layers, x0, y0, x1, y1):
        for layer in layers:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    grid[layer][y][x] = 0

    def copy_from_pierre(layer, sx, sy, dx, dy, w=1, h=1):
        pw, data = pierre[layer]
        for j in range(h):
            for i in range(w):
                put(layer, dx + i, dy + j, to_ours(data[(sy + j) * pw + sx + i], pierre_sets))

    # --- Take out Leah's things -------------------------------------------
    # Wall decor rows: keep her paintings and the window on the left; the
    # chimney, fireplace and everything on the right wall go.
    clear(["Front"], 10, 1, 13, 3)
    for x in (11, 12):                      # chimney cap in the top frame
        put("Front", x, 0, grid["Front"][0][9])
        put("Buildings", x, 0, 0)
    for x in range(10, 14):                 # plain wallpaper and wainscot there
        put("Back", x, 0, grid["Back"][0][9])
        put("Back", x, 1, grid["Back"][1][9])
        put("Back", x, 2, grid["Back"][2][9])
        put("Buildings", x, 2, grid["Buildings"][2][10])
        put("Buildings", x, 3, grid["Buildings"][3][10])
        put("Buildings", x, 4, 0)
    put("Buildings", 14, 4, grid["Buildings"][4][14])
    # The bed, nightstands, logs, chair and phone; the easel (re-added below).
    clear(["Buildings", "Front", "Front2"], 2, 1, 4, 4)
    clear(["Buildings", "Front", "Front2"], 1, 5, 14, 7)
    clear(["Buildings"], 1, 8, 14, 8)
    clear(["Buildings", "Front"], 6, 3, 7, 4)
    clear(["Front"], 14, 3, 14, 4)
    for x in range(2, 5):                   # wainscot where the bed's head was
        put("Buildings", x, 2, grid["Buildings"][2][5])
        put("Buildings", x, 3, grid["Buildings"][3][5])
    put("Buildings", 1, 5, 0)
    lw, lfront = leah["Front"]                # her paintings on that wall stay
    for x in range(2, 5):
        put("Front", x, 1, lfront[lw + x])
    # Floor: even planks wherever her furniture left marks.
    for y in range(4, 9):
        for x in range(1, 15):
            put("Back", x, y, grid["Back"][5 + (y % 2)][2 + (x % 2)] if y > 4 else grid["Back"][4][2 + (x % 2)])
    for x in range(1, 15):
        put("Back", x, 8, grid["Back"][8][2 + (x % 2)])

    # --- Close off the kitchen extension: frame the bottom right ----------
    bottom = grid["Front"][8][2]
    for x in range(9, 14):
        put("Front", x, 8, bottom)
        put("Front2", x, 8, 0)
    put("Front", 14, 8, leah["Front"][1][14 * leah["Front"][0] + 14])   # bottom-right corner pieces
    put("Front", 15, 8, leah["Front"][1][14 * leah["Front"][0] + 15])
    for y in (9, 10):
        for x in range(9, 16):
            for layer in LAYERS:
                put(layer, x, y, 0)
            put("Buildings", x, y, 65)
    for x in range(9, 15):
        put("Buildings", x, 9, 65)
    put("Front", 8, 9, leah["Front"][1][9 * leah["Front"][0] + 8])        # the doorway's right-hand frame, as in Leah's
    put("Front", 8, 10, grid["Front"][10][6] if False else 195)
    put("Front", 9, 8, bottom)
    # A plant by the door instead of Leah's at the frame.
    clear(["Front", "Front2"], 4, 7, 5, 8)
    put("Front", 4, 8, bottom)
    put("Front", 5, 8, bottom)

    # --- Furnish the shop -------------------------------------------------
    # Leah's bookshelf moves from the middle of the back wall into the
    # right-hand corner, and plain wainscot goes back where it stood.
    shelf = {(layer, dx, dy): grid[layer][2 + dy][8 + dx] for layer in ("Buildings", "Front") for dx in (0, 1) for dy in (0, 1, 2)}
    for x in (8, 9):
        put("Buildings", x, 2, grid["Buildings"][2][10])
        put("Buildings", x, 3, grid["Buildings"][3][10])
        put("Buildings", x, 4, 0)
        put("Front", x, 2, 0)
    for x in (6, 7):                        # where Leah's chair stood: solid wall again
        put("Buildings", x, 3, grid["Buildings"][3][10])
    for (layer, dx, dy), gid in shelf.items():
        put(layer, 12 + dx, 2 + dy, gid)

    # Pierre's counter, right up against the wainscot as in his shop: the
    # keeper stands on the row its top covers (row 4) and walks in at the
    # open right end. The left end meets the wall. Just the register, the
    # glass case (of pigments) and the bottle cabinet; no "for sale" end.
    copy_from_pierre("Front", 3, 17, 2, 4, w=4)
    copy_from_pierre("Buildings", 3, 18, 2, 5, w=4)
    clear(["Back2"], 6, 5, 7, 5)
    # Pierre's palm, in the bare corner left of the door.
    copy_from_pierre("Front", 1, 17, 1, 7)
    copy_from_pierre("Buildings", 1, 18, 1, 8)

    # The commission board: a framed corkboard (our own art) on the back wall.
    for j in range(2):
        for i in range(3):
            put("Front", 8 + i, 1 + j, BOARD + j * 3 + i)

    # The easels and their rug, one row further from the wall so the floor in
    # front of the board and the bookshelf stays open.
    rug = [[grid["Back2"][y][x] for x in range(10, 14)] for y in range(5, 8)]
    clear(["Back2"], 10, 5, 13, 7)
    for j, row in enumerate(rug):
        for i, gid in enumerate(row):
            put("Back2", 10 + i, 6 + j, gid)
    easel_top = (leah["Front"][1][3 * leah["Front"][0] + 6], leah["Front"][1][3 * leah["Front"][0] + 7])
    easel_base = (leah["Buildings"][1][4 * leah["Buildings"][0] + 6], leah["Buildings"][1][4 * leah["Buildings"][0] + 7])
    for ex in (10, 12):
        put("Front", ex, 6, easel_top[0]); put("Front", ex + 1, 6, easel_top[1])
        put("Buildings", ex, 7, easel_base[0]); put("Buildings", ex + 1, 7, easel_base[1])

    # --- Tile actions -----------------------------------------------------
    actions = [("Buildings", x, 5, "blinxxy.MuseArt_Counter") for x in range(2, 6)]
    actions += [("Buildings", x, 3, "blinxxy.MuseArt_Board") for x in range(8, 11)]

    props = {k: v for k, v in leah_props.items() if k in ("DayTiles", "NightTiles")}
    props["Warp"] = f"7 10 Forest {door_x} {door_y}"
    write(out, grid, props, actions)
    print(f"wrote {out}")


def write(path, grid, props, actions):
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(LAYERS) * 2 + 1}" nextobjectid="{len(actions) + 1}">',
             '  <properties>']
    for k, v in props.items():
        lines.append(f'    <property name="{k}" value={quoteattr(v)} />')
    lines.append('  </properties>')
    for name, first, image, w, h, cols in TILESETS:
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

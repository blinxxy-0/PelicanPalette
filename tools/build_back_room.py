"""Builds the art shop's back room (1.2): ArtShopBackRoom.tmx, through the
door in the shop's back wall.

It's the same building as the shop, so it's cut from the shop's own empty
shell (Leah's room, stretched; build_shop_interior.shell/stretch): the
middle columns, with the doorway at the bottom, and a shorter floor. In it
the portfolio book on its stand, the plan chest, and the shop's stock: the
blank canvases that used to lean on the shop's back wall, frames and
supplies, and the river painting that hung where the door is now.

Reads maps written by the museart_export_map debug command.
Usage: build_back_room.py <exports dir> <furniture.json> <out dir>
"""
import json, sys
from xml.sax.saxutils import quoteattr
from PIL import Image

import build_shop_interior as shop

# The shell's columns and rows we keep: its side walls and the middle around the doorway
# (shop.DOOR_X), the back wall and four rows of floor, then the bottom wall.
COLS = [0, 1] + list(range(6, 13)) + [18, 19]
ROWS = [0, 1, 2, 3, 4, 5, 6, 7, 12, 13, 14]
W, H = len(COLS), len(ROWS)
DOOR_X = COLS.index(shop.DOOR_X)
ARRIVAL = (DOOR_X, H - 2)          # just inside, coming from the shop
LAMPS = [(2, 4), (7, 4), (DOOR_X, 7), (2, 7), (8, 7)]

# The trapdoor down to Sable's room (SableTrapdoor.png, tools/art/trapdoor.py),
# in the corner past the frames, where nobody walks. Its tile is a
# LockedDoorWarp, so Sable walks through it at night like any door; the
# player's way down is checked by ArtShop.TrapdoorPrefix (2 hearts, or the
# key to the town), not by the warp's own friendship rule.
TRAPDOOR = (9, 7)
TRAPDOOR_ARRIVAL = (8, 7)          # where the stairs up bring you, beside it
TRAPDOOR_ACTION = "LockedDoorWarp 1 2 blinxxy.MuseArt_SableBasement 600 2600 Sable 500"

# The book's stand and the plan chest (art/back_room/; tools/art/back_room.py).
LECTERN = (5, 3)                   # 2 wide, 2 high: ahead as you come in
PLAN_CHEST = (2, 2)                # 3 wide, 3 high: up against the wall, standing on row 4 (clear of the corner's slant)


def cut(grid):
    return {layer: [[rows[y][x] for x in COLS] for y in ROWS] for layer, rows in grid.items()}


# A rug before the book's stand: the red one from Haley and Emily's living
# room (HaleyHouse Back2, x 3-7, y 20-23), three rows of it (its top border,
# a middle row and its bottom border), so it ends above the bottom wall.
RUG = (3, 5)
RUG_SOURCE = (3, (20, 21, 23), 5)      # x, rows, width


def furnish(room, haley):
    sx, rows, w = RUG_SOURCE
    for j, sy in enumerate(rows):
        room.copy(haley, "Back2", sx, sy, RUG[0], RUG[1] + j, w=w)
    # Stock along the walls, clear of the doorway and the book.
    room.place("painting_river", 3, 1, solid=0)      # the painting from the shop's back wall
    room.place("plan_chest", *PLAN_CHEST)
    room.place("lectern", *LECTERN)
    room.place("stack_canvases", 7, 3)               # and its leaning canvases
    room.place("crate_frames", 8, 5)
    room.place("crate_supplies", 1, 7)
    # The book's stand and the plan chest: clicked on their bottom rows.
    room.actions += [("Buildings", LECTERN[0] + i, LECTERN[1] + 1, "blinxxy.MuseArt_Portfolio") for i in range(2)]
    room.actions += [("Buildings", PLAN_CHEST[0] + i, PLAN_CHEST[1] + 2, "blinxxy.MuseArt_PlanChest") for i in range(3)]
    room.put("Buildings", *TRAPDOOR, room.trapdoor)
    room.actions.append(("Buildings", *TRAPDOOR, TRAPDOOR_ACTION))


def write(path, grid, props, actions, furniture_rows):
    lines = ['<?xml version="1.0" encoding="utf-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(shop.LAYERS) * 2 + 1}" nextobjectid="{len(actions) + 1}">',
             '  <properties>']
    for k, v in props.items():
        lines.append(f'    <property name="{k}" value={quoteattr(v)} />')
    lines.append('  </properties>')
    for name, first, image, w, h, cols in shop.tilesets(furniture_rows):
        lines.append(f'  <tileset firstgid="{first}" name="{name}" tilewidth="16" tileheight="16" tilecount="{(w // 16) * (h // 16)}" columns="{cols}">')
        lines.append(f'    <image source="{image}" width="{w}" height="{h}" />')
        lines.append('  </tileset>')
    for i, name in enumerate(shop.LAYERS):
        lines.append(f'  <layer id="{i + 1}" name="{name}" width="{W}" height="{H}">')
        lines.append('    <data encoding="csv">')
        lines.append(",\n".join(",".join(str(v) for v in row) for row in grid[name]))
        lines.append('    </data>')
        lines.append('  </layer>')
    # Tile actions, as an object layer per map layer (as the shop writes them).
    for i, layer in enumerate(sorted({a[0] for a in actions})):
        lines.append(f'  <objectgroup id="{len(shop.LAYERS) + i + 1}" name="{layer}">')
        for n, (lay, x, y, action) in enumerate(a for a in actions if a[0] == layer):
            lines.append(f'    <object id="{n + 1}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            lines.append('      <properties>')
            lines.append(f'        <property name="Action" value={quoteattr(action)} />')
            lines.append('      </properties>')
            lines.append('    </object>')
        lines.append('  </objectgroup>')
    lines.append('</map>')
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


def main():
    exports, furniture_json, out_dir = sys.argv[1], sys.argv[2], sys.argv[3]
    leah = shop.read(f"{exports}/LeahHouse.tmx")
    haley = shop.read(f"{exports}/HaleyHouse.tmx")
    pieces = json.load(open(furniture_json))
    sheet = Image.open(f"{out_dir}/ArtShopFurniture.png").convert("RGBA")
    rows = sheet.height // 16
    empty = {shop.FURNITURE + j * shop.FURNITURE_COLS + i for j in range(rows) for i in range(shop.FURNITURE_COLS)
             if sheet.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)).getbbox() is None}

    room = shop.Room(cut(shop.stretch(shop.shell(leah[1]))), pieces)
    room.empty = empty
    room.trapdoor = shop.FURNITURE + rows * shop.FURNITURE_COLS
    furnish(room, (haley[0], haley[1]))
    props = {
        "Warp": f"{DOOR_X} {H - 1} blinxxy.MuseArt_ArtShop {shop.BACK_DOOR_X} 4",
        "Light": " ".join(f"{x} {y} {shop.LIGHT_TEXTURE}" for x, y in LAMPS),
    }
    write(f"{out_dir}/ArtShopBackRoom.tmx", room.grid, props, room.actions, rows)
    print(f"wrote {out_dir}/ArtShopBackRoom.tmx ({W}x{H}, arrive at {ARRIVAL})")


if __name__ == "__main__":
    main()

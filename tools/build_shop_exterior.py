"""Builds the map patch that puts the art shop and its yard into Cindersap
Forest, in the open grass west of the path down from Marnie's ranch.

The 7x7-tile building (ArtShopExterior.png) is its own tilesheet: its wall
rows go on Buildings (blocking movement), the roof on Front (the farmer
walks behind it), and its door tile gets a LockedDoorWarp into the shop.
It was 6x6 until the deeper roof (2026-10-24), which grew it a row upward;
the yard stayed where it was in the Forest.
Around it, vanilla Forest tiles make a yard like the other houses have:
a wooden fence on the west and south (Marnie's fence pieces) with a gate
onto the riverside path, stepping stones from the door to the gate and
east to the path from Marnie's, barrels, a display easel and a signpost
with the shop's hours. The ground is left as the Forest has it (the dark
grass the house stands on, like Leah's yard); the stones are our own
transparent tiles on Back2, so they sit cleanly on light or dark grass. Content Patcher
overlays it onto Maps/Forest at PATCH_X, PATCH_Y.

Usage: build_shop_exterior.py <ArtShopExterior.png> <bigcraftables.png> <out dir>
  writes ArtShopExteriorPatch.tmx and ArtShopYard.png into <out dir>
"""
import os, sys
from PIL import Image
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from stepping_stones import tiles as stone_tiles
from xml.sax.saxutils import quoteattr

PATCH_X, PATCH_Y, W, H = 76, 27, 13, 12     # Forest tiles 76-88, 27-38
HOUSE_X, HOUSE_Y = 2, 0                       # building's top-left within the patch
HOUSE = 7
WALL_ROWS = (4, 5, 6)                         # rows of the building sheet that are walls
DOOR_COLS = (5,)                              # columns of its door (bottom row)
DOOR_ACTION = "LockedDoorWarp 9 13 blinxxy.MuseArt_ArtShop 900 2000"
YARD_Y = 1                                    # the yard's rows, below the patch's top row (the roof's)
SIGN_ACTION = "blinxxy.MuseArt_Sign"

# Vanilla Forest tiles (spring_outdoorsTileSheet, which the game swaps per season).
# Fence rail pieces: top half on Front, bottom half (+25) on Buildings.
# 359 starts a run on a post, 360 continues it, 361 ends it on a post.
RAIL_START, RAIL_MID, RAIL_END = 359, 360, 361
POST_CAP, POST, CORNER_UPPER, CORNER = 362, 387, 412, 437     # Marnie's fence corner
BARREL_FRONT, BARREL_BASE = 1498, 1523                        # one of Marnie's barrels
SIGN_FRONT, SIGN_BASE = 411, 436

OUTDOORS_FIRST = 1
HOUSE_FIRST = 2000          # after the outdoors sheet's 1975 tiles
YARD_FIRST = HOUSE_FIRST + HOUSE * HOUSE
YARD_TILES = 6              # easel (2 tiles) + 4 stepping stones


def main():
    house_png, bigcraftables_png, out_dir = sys.argv[1:4]
    house = Image.open(house_png).convert("RGBA")
    assert house.size == (HOUSE * 16, HOUSE * 16)

    # Our own yard pieces on a small sheet: the easel (from the item art),
    # then the stepping stones.
    stones = stone_tiles()
    yard = Image.new("RGBA", (16, 16 * (2 + len(stones))), (0, 0, 0, 0))
    yard.alpha_composite(Image.open(bigcraftables_png).convert("RGBA").crop((0, 0, 16, 32)))
    for i, stone in enumerate(stones):
        yard.alpha_composite(stone, (0, 32 + i * 16))
    yard.save(os.path.join(out_dir, "ArtShopYard.png"))
    assert yard.height // 16 == YARD_TILES
    stone_gids = [YARD_FIRST + 2 + i for i in range(len(stones))]

    layers = {name: [[0] * W for _ in range(H)] for name in ("Back2", "Buildings", "Front")}
    actions = []

    def put(layer, x, y, gid):
        if 0 <= x < W and 0 <= y < H:
            layers[layer][y][x] = gid

    # The house.
    for y in range(HOUSE):
        for x in range(HOUSE):
            if house.crop((x * 16, y * 16, x * 16 + 16, y * 16 + 16)).getbbox() is None:
                continue
            put("Buildings" if y in WALL_ROWS else "Front", HOUSE_X + x, HOUSE_Y + y, HOUSE_FIRST + y * HOUSE + x)
    door_y = HOUSE_Y + HOUSE - 1
    for c in DOOR_COLS:
        actions.append(("Buildings", HOUSE_X + c, door_y, DOOR_ACTION))

    # Fence: down the west side from beside the house, then along the south.
    fence_x, fence_y = 0, 9 + YARD_Y
    put("Front", fence_x, 3 + YARD_Y, POST_CAP)
    for y in range(4 + YARD_Y, fence_y - 1):
        put("Buildings", fence_x, y, POST)
    put("Buildings", fence_x, fence_y - 1, CORNER_UPPER)
    put("Buildings", fence_x, fence_y, CORNER)
    door_x = HOUSE_X + DOOR_COLS[-1]
    gate_x, fence_end = door_x, 10
    # Two runs either side of the gate, each finishing cleanly on a post
    # (the left run joins the corner post, so it needs no start piece).
    for x in range(1, fence_end + 1):
        if x == gate_x:
            continue                            # the gate onto the riverside path
        rail = RAIL_END if x in (gate_x - 1, fence_end) else RAIL_START if x == gate_x + 1 else RAIL_MID
        put("Front", x, fence_y - 1, rail)
        put("Buildings", x, fence_y, rail + 25)

    # Stepping stones: from the door down to the gate, and a branch east to
    # the path that comes down from Marnie's.
    path = [(door_x, y) for y in range(door_y + 1, fence_y + 1)]
    path += [(x, door_y + 2) for x in range(door_x + 1, W - 1)]
    for i, (x, y) in enumerate(path):
        put("Back2", x, y, stone_gids[(x * 3 + y) % len(stone_gids)])

    # Barrels by the west fence, a display easel in the yard, a signpost by the path.
    for x in (1, 2):
        put("Front", x, 6 + YARD_Y, BARREL_FRONT)
        put("Buildings", x, 7 + YARD_Y, BARREL_BASE)
    put("Front", 4, 6 + YARD_Y, YARD_FIRST)
    put("Buildings", 4, 7 + YARD_Y, YARD_FIRST + 1)
    put("Front", 11, 4 + YARD_Y, SIGN_FRONT)
    put("Buildings", 11, 5 + YARD_Y, SIGN_BASE)
    actions.append(("Buildings", 11, 5 + YARD_Y, SIGN_ACTION))

    write(os.path.join(out_dir, "ArtShopExteriorPatch.tmx"), layers, actions)
    print("wrote patch; door at Forest", PATCH_X + door_x - 1, PATCH_Y + door_y, "-", PATCH_X + door_x, PATCH_Y + door_y,
          "; exit tile", PATCH_X + door_x, PATCH_Y + door_y + 1)


def write(path, layers, actions):
    def layer(i, name):
        data = ",\n".join(",".join(str(v) for v in row) for row in layers[name])
        return (f'  <layer id="{i}" name="{name}" width="{W}" height="{H}">\n'
                f'    <data encoding="csv">\n{data}\n    </data>\n  </layer>')

    groups = []
    oid = 1
    for i, name in enumerate(("Back2", "Buildings", "Front")):
        objs = []
        for lname, x, y, action in actions:
            if lname != name:
                continue
            objs.append(f'    <object id="{oid}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">\n'
                        f'      <properties><property name="Action" value={quoteattr(action)} /></properties>\n    </object>')
            oid += 1
        groups.append(f'  <objectgroup id="{4 + i}" name="{name}">\n' + "\n".join(objs) + ('\n' if objs else '') + '  </objectgroup>')

    xml = f'''<?xml version="1.0" encoding="utf-8"?>
<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="7" nextobjectid="{oid}">
  <tileset firstgid="{OUTDOORS_FIRST}" name="outdoors" tilewidth="16" tileheight="16" tilecount="1975" columns="25">
    <image source="spring_outdoorsTileSheet" width="400" height="1264" />
  </tileset>
  <tileset firstgid="{HOUSE_FIRST}" name="z_blinxxy.MuseArt_ArtShopExterior" tilewidth="16" tileheight="16" tilecount="{HOUSE * HOUSE}" columns="{HOUSE}">
    <image source="ArtShopExterior.png" width="{HOUSE * 16}" height="{HOUSE * 16}" />
  </tileset>
  <tileset firstgid="{YARD_FIRST}" name="z_blinxxy.MuseArt_ArtShopYard" tilewidth="16" tileheight="16" tilecount="{YARD_TILES}" columns="1">
    <image source="ArtShopYard.png" width="16" height="{YARD_TILES * 16}" />
  </tileset>
{layer(1, "Back2")}
{layer(2, "Buildings")}
{layer(3, "Front")}
{chr(10).join(groups)}
</map>
'''
    open(path, "w", encoding="utf-8").write(xml)


if __name__ == "__main__":
    main()

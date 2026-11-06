"""The cobbled path from the town road to Margot's cottage door (1.3, from
playtesting: the cottage stood on the grass with no way to it).

It's the game's own Cobblestone Path flooring, baked into the town map so
it's there for good once the cottage is built (data/margot.json), like the
road: no tool lifts it. The game builds a floor tile from several pieces by
its neighbours, so rather than work those out, the path was laid as real
flooring in a dev game and the game's own drawing of it saved, at 1x on a
transparent image, in spring and in winter (`museart_pathfloor 411`, then
`museart_exportpath`): art/margot/path/path_spring.png and path_winter.png,
Town x 21-23, y 92-96. Its edges show the grass under them, so the ground
is the map's own in every season.

The path: down Town column 23 from the road (rows 92-96), then left along
row 96 to the door step (columns 21-22).

Writes spring_, summer_, fall_ and winter_MargotPath.png (the seven tiles
in a row; summer and fall are spring's, as the game's flooring is) and
MargotPathPatch.tmx (their Back2 layer over Town x 21-23, y 92-96).

Usage: margot_path.py <repo root>
"""
import os, sys
from PIL import Image

ORIGIN, W, H = (21, 92), 3, 5
CELLS = [(23, y) for y in range(92, 97)] + [(21, 96), (22, 96)]


def straight(im):
    """The export's colours are premultiplied by alpha (as the game draws
    them); PNG wants them straight."""
    px = im.load()
    for x in range(im.width):
        for y in range(im.height):
            r, g, b, a = px[x, y]
            if 0 < a < 255:
                px[x, y] = (min(255, r * 255 // a), min(255, g * 255 // a), min(255, b * 255 // a), a)
    return im


def sheet_from(drawn):
    sheet = Image.new("RGBA", (8 * 16, 16), (0, 0, 0, 0))
    for n, (x, y) in enumerate(CELLS):
        X, Y = (x - ORIGIN[0]) * 16, (y - ORIGIN[1]) * 16
        sheet.paste(drawn.crop((X, Y, X + 16, Y + 16)), (n * 16, 0))
    return sheet


def main():
    root = sys.argv[1]
    art = os.path.join(root, "art", "margot", "path")
    assets = os.path.join(root, "[CP] Pelican Palette", "assets")
    spring = sheet_from(straight(Image.open(os.path.join(art, "path_spring.png")).convert("RGBA")))
    winter = sheet_from(straight(Image.open(os.path.join(art, "path_winter.png")).convert("RGBA")))
    for season, im in (("spring", spring), ("summer", spring), ("fall", spring), ("winter", winter)):
        im.save(os.path.join(assets, f"{season}_MargotPath.png"))
    grid = [[0] * W for _ in range(H)]
    for n, (x, y) in enumerate(CELLS):
        grid[y - ORIGIN[1]][x - ORIGIN[0]] = n + 1
    csv = ",\n".join(",".join(str(v) for v in row) for row in grid)
    tmx = f"""<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.10.2" orientation="orthogonal" renderorder="right-down" width="{W}" height="{H}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="2" nextobjectid="1">
  <tileset firstgid="1" name="z_blinxxy.MuseArt_MargotPath" tilewidth="16" tileheight="16" tilecount="8" columns="8">
    <image source="spring_MargotPath.png" width="128" height="16" />
  </tileset>
  <layer id="1" name="Back2" width="{W}" height="{H}">
    <data encoding="csv">
{csv}
    </data>
  </layer>
</map>
"""
    with open(os.path.join(assets, "MargotPathPatch.tmx"), "w", encoding="utf-8") as f:
        f.write(tmx)
    print("wrote the MargotPath sheets and MargotPathPatch.tmx")


if __name__ == "__main__":
    main()

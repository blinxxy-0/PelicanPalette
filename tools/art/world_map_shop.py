"""The art shop as the world map draws its buildings: a few pixels with a
dark outline, like Leah's cottage beside it (added 2026-10-25 for 1.1).

Writes WorldMapShop.png and WorldMapShop_winter.png (the game swaps in the
_winter one itself; spring, summer and fall share the first). The content
pack draws it over the Forest on the map (Data/WorldMap, Forest, Textures),
which works the same on Stardew Valley Expanded's redrawn map.

Usage: world_map_shop.py <out dir>
"""
import os
import sys
from PIL import Image

# Where it sits on LooseSprites/map: the shop's Forest tiles (76-89, 27-39)
# scaled into the Forest's map area (47,96 76x65 for 120x120 tiles), nudged
# left so it doesn't cover the path down from Marnie's.
MAP_X, MAP_Y = 92, 109

ROWS = [
    "..ooooc..",
    ".oLLRRRo.",
    "oLLRRRRRo",
    "ooooooooo",
    ".wYwwDwY.",
    ".wwwwDws.",
    ".ggggggg.",
]

COLOURS = {
    "o": (27, 52, 58),      # outline
    "c": (140, 76, 78),     # chimney, as the map's brick
    "L": (88, 168, 160),    # roof, lit side
    "R": (47, 111, 115),    # roof, shade side
    "w": (233, 217, 166),   # plaster
    "s": (184, 154, 106),   # plaster in shade
    "Y": (250, 190, 27),    # lit window, the map's yellow
    "D": (63, 122, 58),     # the green door
}
# The shadow under it is a translucent dark rather than a darker grass, so it
# shades whatever ground is drawn there: map recolours (Earthy, Starblue,
# Vibrant Pastoral, which Remapping supports), SVE's map, or snow.
SHADOW = (24, 40, 16, 96)
SNOW = {"L": (240, 246, 252), "R": (190, 206, 222)}


def draw(colours):
    im = Image.new("RGBA", (len(ROWS[0]), len(ROWS)), (0, 0, 0, 0))
    for y, row in enumerate(ROWS):
        for x, key in enumerate(row):
            if key == "g":
                im.putpixel((x, y), SHADOW)
            elif key != ".":
                im.putpixel((x, y), colours[key] + (255,))
    return im


def main():
    out = sys.argv[1]
    draw(COLOURS).save(os.path.join(out, "WorldMapShop.png"))
    draw({**COLOURS, **SNOW}).save(os.path.join(out, "WorldMapShop_winter.png"))
    print("map sprite", len(ROWS[0]), "x", len(ROWS), "at", MAP_X, MAP_Y)


if __name__ == "__main__":
    main()

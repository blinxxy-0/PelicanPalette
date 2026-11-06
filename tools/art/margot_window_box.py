"""The window box on Margot's cottage, in the three plantings you choose
from in her 4-heart event (1.3, from playtesting: the scene asked where the
cottage should go, and it always went in the same place).

The cottage's sheet (spring_MargotCottage.png, built by build_margot_cottage.py)
has the window box's flowers in its tiles 33 and 34, pink roses. This adds
two recoloured copies to the sheet's free space, which data/margot.json
swaps onto Town (19, 95) and (20, 95) by the flag the scene sets:

  tiles 21-22: lavender  (blinxxy.MuseArt_MargotBoxLavender)
  tiles 29-30: marigolds (blinxxy.MuseArt_MargotBoxMarigold)

Run after build_margot_cottage.py (it rebuilds the sheet without them).

Usage: margot_window_box.py <spring_MargotCottage.png>  (then seasonal_buildings.py)
"""
import sys
from PIL import Image

SHEET_COLS = 8
ROSES = (33, 34)
VARIANTS = {(21, 22): "lavender", (29, 30): "marigold"}
# The roses' pinks, and what each becomes.
RAMPS = {
    "lavender": {(131, 37, 88): (86, 62, 138), (195, 76, 127): (146, 116, 204), (170, 17, 101): (112, 80, 172),
                 (235, 149, 224): (204, 186, 242), (122, 62, 103): (88, 70, 118)},
    "marigold": {(131, 37, 88): (164, 80, 20), (195, 76, 127): (232, 142, 32), (170, 17, 101): (204, 108, 24),
                 (235, 149, 224): (250, 212, 84), (122, 62, 103): (148, 88, 40)},
}


def box(index):
    return (index % SHEET_COLS * 16, index // SHEET_COLS * 16, index % SHEET_COLS * 16 + 16, index // SHEET_COLS * 16 + 16)


def main():
    path = sys.argv[1]
    sheet = Image.open(path).convert("RGBA")
    for targets, name in VARIANTS.items():
        ramp = RAMPS[name]
        for src, dst in zip(ROSES, targets):
            tile = sheet.crop(box(src))
            assert not any(sheet.getpixel((x, y))[3] for x in range(box(dst)[0], box(dst)[2]) for y in range(box(dst)[1], box(dst)[3])), f"tile {dst} isn't free"
            px = tile.load()
            for x in range(16):
                for y in range(16):
                    r, g, b, a = px[x, y]
                    if a and (r, g, b) in ramp:
                        px[x, y] = ramp[(r, g, b)] + (a,)
            sheet.paste(tile, box(dst)[:2])
    sheet.save(path)
    print("added the lavender and marigold window boxes to", path)


if __name__ == "__main__":
    main()

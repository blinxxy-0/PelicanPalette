"""Installs the art shop's exterior from its PixelLab render.

Takes gable_b_1 (the user's pick, 2026-10-24, from the deeper-roof batch
art/shop_exterior/roof_batch.json: a big teal roof with a front gable over
the display window and a chimney on the right) and sets it on a 7x7-tile
sheet so it can be used as a map tilesheet: its base on the bottom row,
its right edge on the sheet's (which puts the door inside one tile
column). The puff of smoke drawn above the chimney is cleared: it's a loose
cluster of pixels apart from the building, and a painted puff never moves.

The first exterior (shop_b_1, 2026-10-23, 6x6) had a short, flat roof; the
user found it "almost 2d" next to the game's buildings.

Usage: install_shop_exterior.py <render.png> <out tilesheet.png> [mockup args...]
  mockup: --mock <exports dir> <map x> <map y> <out.png>
    pastes the building into a render of the vanilla Forest at that tile,
    with the area around it, for review at 3x.
"""
import os, subprocess, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_shop_furniture import drop_specks

TILES = 7


def install(src, out):
    im = Image.open(src).convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    building = drop_specks(im)
    building = building.crop(building.getbbox())
    size = TILES * 16
    assert building.width <= size and building.height <= size
    sheet = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    sheet.alpha_composite(building, (size - building.width, size - building.height))
    sheet.save(out)
    return sheet


def mock(sheet, exports, mx, my, out):
    here = os.path.dirname(os.path.abspath(__file__))
    region = (mx - 5, my - 4, 16, 14)
    tmp = out + ".base.png"
    subprocess.run([sys.executable, os.path.join(os.path.dirname(here), "render_tmx.py"),
                    os.path.join(exports, "Forest.tmx"), tmp, "--region", *map(str, region), "--scale", "1"], check=True)
    base = Image.open(tmp).convert("RGBA")
    base.alpha_composite(sheet, ((mx - region[0]) * 16, (my - region[1]) * 16))
    base.resize((base.width * 3, base.height * 3), Image.NEAREST).save(out)
    os.remove(tmp)


if __name__ == "__main__":
    sheet = install(sys.argv[1], sys.argv[2])
    if len(sys.argv) > 3 and sys.argv[3] == "--mock":
        mock(sheet, sys.argv[4], int(sys.argv[5]), int(sys.argv[6]), sys.argv[7])
    print("installed", sys.argv[2])

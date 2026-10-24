"""Installs the art shop's exterior from its PixelLab render.

Takes shop_b_1 (the user's pick, 2026-10-23: a classic storefront with a
big roof seen from above, like Leah's cottage), and crops it to exactly
6x6 tiles, so it can be used as a map tilesheet. Renders with chimney
smoke baked in (shop_a_1 had some) get rows 0..SMOKE_BOTTOM cleared first;
the game draws its own smoke.

Usage: install_shop_exterior.py <render.png> <out tilesheet.png> [mockup args...]
  mockup: --mock <exports dir> <map x> <map y> <out.png>
    pastes the building into a render of the vanilla Forest at that tile,
    with the area around it, for review at 3x.
"""
import os, subprocess, sys
from PIL import Image

SMOKE_BOTTOM = -1          # last row of baked-in smoke to clear (-1: none in shop_b_1)
CROP = (8, 8, 104, 104)    # 96x96: the building, base on the bottom row


def install(src, out):
    im = Image.open(src).convert("RGBA")
    px = im.load()
    for y in range(SMOKE_BOTTOM + 1):
        for x in range(im.width):
            px[x, y] = (0, 0, 0, 0)
    sheet = im.crop(CROP)
    assert sheet.getbbox()[3] == sheet.height, "building base must sit on the bottom row"
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

"""The Framer's Bench (1.3): its sprite, 16x32, at index 4 of
ArtStudio/assets/bigcraftables.png (widened to 80x32; the easel, mill,
cabinet and Master's Easel keep 0 to 3). PixelLab, picked 2026-11-02
(art/frames/framers_bench_pick.png, unwrapped: the generator returned it
split across its left and right edges); outline recoloured
(vanillafy.selout), never black.

Usage: framers_bench.py <ArtStudio/assets dir>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PICK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "art", "frames", "framers_bench_pick.png")


def main():
    path = os.path.join(sys.argv[1], "bigcraftables.png")
    sheet = Image.open(path).convert("RGBA")
    if sheet.width < 80:
        wider = Image.new("RGBA", (80, sheet.height), (0, 0, 0, 0))
        wider.alpha_composite(sheet, (0, 0))
        sheet = wider
    sheet.paste((0, 0, 0, 0), (64, 0, 80, 32))
    sheet.alpha_composite(selout(Image.open(PICK).convert("RGBA")), (64, 0))
    sheet.save(path)
    print("wrote", path)


if __name__ == "__main__":
    main()

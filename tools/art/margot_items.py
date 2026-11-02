"""Margot's 10-heart paintings (1.3), wall decor, to
[CP] Pelican Palette/assets/margot/items.png (64x32):
  - (0, 0) 32x32: "Margot, by You", her portrait as you paint it,
    loose and a little lopsided (PixelLab, picked 2026-11-01)
  - (32, 0) 32x32: "North Window, by Margot and You", her cottage's north
    window at night, one half neat and one half loose
(The third, "You, by Margot", is painted from the farmer's own sprite in
the game: ArtStudio/Painting/FarmerPortrait.cs.) Outlines recoloured
(vanillafy.selout): a darker shade of what they border, never black.

Usage: margot_items.py art/margot/rewards/picks <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PIECES = ["margot_by_you_20", "north_window_33"]


def main():
    picks, out = sys.argv[1], sys.argv[2]
    sheet = Image.new("RGBA", (32 * len(PIECES), 32), (0, 0, 0, 0))
    for i, name in enumerate(PIECES):
        sheet.alpha_composite(selout(Image.open(os.path.join(picks, name + ".png")).convert("RGBA")), (i * 32, 0))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()

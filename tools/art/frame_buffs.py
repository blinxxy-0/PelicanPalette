"""Builds the frame bonus icons shown on the easel's setup screen (1.3):
ArtStudio/assets/frame_buffs.png, 16x16 icons in a row in FrameTrait's
order (light, sparks, thrift, learning, value, night), like the game's own
buff icons.

From PixelLab generations (2026-11-03, art/frames/buffs/*_batch.json, styled
on the vanilla TileSheets/BuffsIcons), the picks in art/frames/buffs/picks; outlines recoloured
from black and selout'd, as all generated art is.

Usage: frame_buffs.py <art/frames/buffs dir> <ArtStudio/assets dir>
"""
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import recolour_black, selout

# In FrameTrait's order (Painting/Frames.cs), which picks each one's place.
# (Picked from the candidates: light 6 of the second light batch; sparks 2,
# thrift 26, learning 33, value 13, night 48 of the first.)
PICKS = ["light", "sparks", "thrift", "learning", "value", "night"]


def main():
    art, assets = sys.argv[1], sys.argv[2]
    sheet = Image.new("RGBA", (16 * len(PICKS), 16), (0, 0, 0, 0))
    for i, job in enumerate(PICKS):
        icon = Image.open(os.path.join(art, "picks", job + ".png")).convert("RGBA")
        assert icon.size == (16, 16), (job, icon.size)
        sheet.alpha_composite(selout(recolour_black(icon)), (16 * i, 0))
    out = os.path.join(assets, "frame_buffs.png")
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()

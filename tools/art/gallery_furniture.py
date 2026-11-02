"""The museum collection's furniture rewards (1.3, restoration_mechanics.md
section 5), to [CP] Pelican Palette/assets/collection/gallery.png (96x32,
furniture sprite indexes on its 16-pixel grid, six to a row):
  - index 0, 32x32: the velvet rope, two brass posts and a red rope
  - index 2, 32x32: the gallery bench, green leather with brass studs
  - index 4, 32x16: the picture light, a brass lamp to hang over a painting
The Old Valley set's reward (all three). PixelLab, picked 2026-11-01
(art/collection); outlines recoloured (vanillafy.selout), never black.

Usage: gallery_furniture.py art/collection/picks <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PIECES = [("velvet_rope_1", 0), ("gallery_bench_4", 32), ("picture_light_33", 64)]


def main():
    picks, out = sys.argv[1], sys.argv[2]
    sheet = Image.new("RGBA", (96, 32), (0, 0, 0, 0))
    for name, x in PIECES:
        sheet.alpha_composite(selout(Image.open(os.path.join(picks, name + ".png")).convert("RGBA")), (x, 0))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()

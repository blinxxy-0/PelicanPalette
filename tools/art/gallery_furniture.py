"""The museum collection's furniture rewards (1.3, restoration_mechanics.md
section 5), to [CP] Pelican Palette/assets/collection/gallery.png (96x80,
furniture sprite indexes on its 16-pixel grid, six to a row):
  - index 0, 32x32: the velvet rope, two brass posts and a red rope
  - index 2, 32x32: the gallery bench, green leather with brass studs
  - index 4, 32x16: the picture light, a brass lamp to hang over a painting
  - index 10, 16x16: a gallery card, for a painting's place in the museum's
    painting wing while it's still missing (drawn here: cream card, ink lines)
  - index 11, 16x32: the wing's doorway roped off: one brass post (cut from
    the velvet rope) with its rope looped up to the wall
  - index 12, 48x48: the paint-tube Junimo on its marble plinth, the
    centrepiece of Sable's gallery (art/shop_interior/gallery_junimo.png,
    the same art), for the whole collection donated
The first three are the Old Valley set's reward. PixelLab, picked
2026-11-01 (art/collection); outlines recoloured (vanillafy.selout), never
black.

Usage: gallery_furniture.py art/collection/picks <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import selout

PIECES = [("velvet_rope_1", (0, 0)), ("gallery_bench_4", (32, 0)), ("picture_light_33", (64, 0))]
JUNIMO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "art", "shop_interior", "gallery_junimo.png")


CREAM, CREAM_SHADE, CARD_EDGE, INK = (236, 224, 196, 255), (210, 194, 160, 255), (120, 86, 56, 255), (92, 70, 58, 255)


def card():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    for x in range(3, 13):
        for y in range(5, 12):
            px[x, y] = CARD_EDGE if x in (3, 12) or y in (5, 11) else CREAM
    for x in range(4, 12):
        px[x, 10] = CREAM_SHADE
    for x in range(5, 11):
        px[x, 7] = INK
    for x in range(5, 9):
        px[x, 9] = INK
    return im


def doorway_rope(rope):
    """The left post of the velvet rope, standing at the foot of the
    doorway, its rope looped up to the wall above."""
    post = rope.crop((0, 0, 10, 32))
    im = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    im.alpha_composite(post, (0, 0))
    red = [c for c in rope.getdata() if c[3] and c[0] > 120 and c[1] < 70 and c[2] < 80]
    deep = min(red, key=sum) if red else (120, 30, 40, 255)
    bright = max(red, key=sum) if red else (170, 50, 60, 255)
    px = im.load()
    # From the post's top (about 7, 5) up and right to the wall (15, 0).
    for i, (x, y) in enumerate([(8, 6), (9, 6), (10, 5), (11, 4), (12, 3), (13, 2), (14, 1), (15, 0)]):
        px[x, y] = bright
        if y + 1 < 32:
            px[x, y + 1] = deep
    return im


def main():
    picks, out = sys.argv[1], sys.argv[2]
    sheet = Image.new("RGBA", (96, 80), (0, 0, 0, 0))
    for name, at in PIECES:
        sheet.alpha_composite(selout(Image.open(os.path.join(picks, name + ".png")).convert("RGBA")), at)
    sheet.alpha_composite(Image.open(JUNIMO).convert("RGBA"), (0, 32))
    sheet.alpha_composite(card(), (64, 16))
    rope = selout(Image.open(os.path.join(picks, "velvet_rope_1.png")).convert("RGBA"))
    sheet.alpha_composite(doorway_rope(rope), (80, 16))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()

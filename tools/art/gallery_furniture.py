"""The museum collection's furniture rewards (1.3, restoration_mechanics.md
section 5), to [CP] Pelican Palette/assets/collection/gallery.png (96x128,
furniture sprite indexes on its 16-pixel grid, six to a row):
  - index 0, 32x32: the velvet rope, two brass posts and a red rope
  - index 2, 32x32: the gallery bench, green leather with brass studs
  - index 4, 32x16: the picture light (no longer used: replaced by the plinth)
  - index 10, 16x16: a gallery card, for a painting's place in the museum's
    painting wing while it's still missing (drawn here: cream card, ink lines)
  - index 15, 48x64: the wing's white marble display, a board three tiles
    wide on a pillar (PixelLab, picked 2026-11-02; the generator draws it no
    wider than 32 pixels, so its board is widened here: the border and
    corners kept, the plain middle extended, the pillar a little wider)
  - index 30, 16x48: the wing's doorway roped off, seen end-on: two short
    brass posts (cut from the velvet rope's), one behind the other, the
    velvet sagging between them
  - index 12, 48x48: the paint-tube Junimo on its marble plinth, the
    centrepiece of Sable's gallery (art/shop_interior/gallery_junimo.png,
    the same art), for the whole collection donated
  - index 39, 16x32: the marble plinth, white marble with a brass plaque
    (PixelLab with a straight-on guide, art/collection/plinth, 2026-11-05),
    the Old Valley set's third piece in place of the picture light
Since 2026-11-05 the sheet is edited in place after this script (the
doorway rope flipped, the bench dropped 5 pixels so its feet meet the floor
and you sit on it, the plinth added): don't rerun it over those.
The rope, bench and plinth are the Old Valley set's reward. PixelLab, picked
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


def short_post(rope):
    """The velvet rope's left post, without its rope, shortened (its round
    top, a little shaft, its base) so two can stand one behind the other."""
    post = rope.crop((1, 3, 9, 31))
    px = post.load()
    for x in range(post.width):
        for y in range(post.height):
            r, g, b, a = px[x, y]
            if a and r > 100 and g < 80 and b < 90:
                px[x, y] = (0, 0, 0, 0)
    h = post.height
    short = Image.new("RGBA", (post.width, 16))
    short.alpha_composite(post.crop((0, 0, post.width, 6)), (0, 0))
    short.alpha_composite(post.crop((0, 10, post.width, 14)), (0, 6))
    short.alpha_composite(post.crop((0, h - 6, post.width, h)), (0, 10))
    return short


ROPE_LIGHT, ROPE_DARK = (151, 34, 49, 255), (114, 16, 37, 255)
ROPE_CURVE = [(8, 17), (9, 17), (10, 18), (11, 19), (12, 21), (12, 22), (13, 23), (13, 24), (13, 25), (13, 26),
              (13, 27), (12, 28), (12, 29), (11, 31), (10, 32), (9, 33), (8, 33)]


def doorway_rope(rope):
    """Across a doorway in a side wall, seen end-on: the far post, the
    velvet sagging out from its top to the near post's, the near post in
    front. Their bases stand on the doorway's two rows."""
    post = short_post(rope)
    im = Image.new("RGBA", (16, 48), (0, 0, 0, 0))
    im.alpha_composite(post, (3, 15))
    px = im.load()
    for x, y in ROPE_CURVE:
        px[x, y] = ROPE_LIGHT
        px[min(15, x + 1), y] = ROPE_DARK
    im.alpha_composite(post, (3, 31))
    return im


def stretch(im, axis, keep, to):
    """Keeps `keep` pixels at each end along an axis and repeats the line in
    the middle until it's `to` long."""
    w, h = im.size
    out = Image.new("RGBA", (to, h) if axis == 0 else (w, to))
    if axis == 0:
        out.alpha_composite(im.crop((0, 0, keep, h)), (0, 0))
        for x in range(keep, to - keep):
            out.alpha_composite(im.crop((w // 2, 0, w // 2 + 1, h)), (x, 0))
        out.alpha_composite(im.crop((w - keep, 0, w, h)), (to - keep, 0))
    else:
        out.alpha_composite(im.crop((0, 0, w, keep)), (0, 0))
        for y in range(keep, to - keep):
            out.alpha_composite(im.crop((0, h // 2, w, h // 2 + 1)), (0, y))
        out.alpha_composite(im.crop((0, h - keep, w, h)), (0, to - keep))
    return out


def display(pick):
    """The marble display at three tiles wide: its 32x33 board widened to
    48 square, its pillar to 40."""
    src = pick.crop((8, 12, 40, 60))
    board = stretch(stretch(src.crop((0, 0, 32, 33)), 0, 9, 48), 1, 9, 48)
    pillar = stretch(src.crop((0, 33, 32, 48)), 0, 16, 40)
    out = Image.new("RGBA", (48, 64), (0, 0, 0, 0))
    out.alpha_composite(board, (0, 1))
    out.alpha_composite(pillar, (4, 49))
    return out


def main():
    picks, out = sys.argv[1], sys.argv[2]
    sheet = Image.new("RGBA", (96, 128), (0, 0, 0, 0))
    for name, at in PIECES:
        sheet.alpha_composite(selout(Image.open(os.path.join(picks, name + ".png")).convert("RGBA")), at)
    sheet.alpha_composite(Image.open(JUNIMO).convert("RGBA"), (0, 32))
    sheet.alpha_composite(card(), (64, 16))
    rope = Image.open(os.path.join(picks, "velvet_rope_1.png")).convert("RGBA")
    sheet.alpha_composite(selout(doorway_rope(rope)), (0, 80))
    sheet.alpha_composite(selout(display(Image.open(os.path.join(picks, "marble_display_1.png")).convert("RGBA"))), (48, 32))
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()

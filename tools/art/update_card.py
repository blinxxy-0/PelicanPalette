"""The 1.2 update card for the Nexus page: what's new, on one image, in the
style of the game's own update announcements (a night sky, a parchment card
with the title across its top, a list with icons, and two characters at the
bottom).

The parchment, the lines (the game's dialogue font) and most icons are
rendered in game by `museart_updatecard` into media/update_card.png, with
media/update_card_slots.txt listing the icons it leaves to this script (the
lectern, the plan chest and Sable's skiff, which are map art). This adds
them, then the sky, the gold title (page_art's lettering, from the
"Pelican Palette" sign text) and Sable and Julian's portraits.

Usage: update_card.py <dev media dir> <docs/media dir>
  writes <docs/media>/page/update_1_2.png
"""
import os
import random
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..", "..")
CP = os.path.join(ROOT, "[CP] Pelican Palette", "assets")
sys.path.insert(0, HERE)
from page_art import outlined

PIECES = {"lectern": (4, 3, 2, 2), "planchest": (1, 3, 3, 3)}     # tile x, y, w, h in ArtShopFurniture.png


def scaled(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def map_icons(card, slots):
    furniture = Image.open(os.path.join(CP, "ArtShopFurniture.png")).convert("RGBA")
    for line in slots:
        name, x, y = line.split()
        x, y = int(x), int(y)
        if name in PIECES:
            tx, ty, tw, th = PIECES[name]
            piece = furniture.crop((tx * 16, ty * 16, (tx + tw) * 16, (ty + th) * 16))
            piece = scaled(piece.crop(piece.getbbox()), 2)
        elif name == "skiff":
            piece = Image.open(os.path.join(CP, "NightMarketStall.png")).convert("RGBA")
            piece = piece.crop(piece.getbbox())
        else:
            continue
        # Centred on the line, in a 64-pixel slot (bigger ones spread a little).
        card.alpha_composite(piece, (x + (64 - piece.width) // 2 if piece.width <= 64 else x, y + 32 - piece.height // 2))


def sky(w, h, seed=12):
    """A night sky: deep blue at the top, lighter towards a bank of clouds."""
    top, bottom = (14, 20, 64), (64, 104, 178)
    im = Image.new("RGBA", (w, h))
    px = im.load()
    for y in range(h):
        t = y / (h - 1)
        c = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,)
        for x in range(w):
            px[x, y] = c
    rnd = random.Random(seed)
    # Stars: single bright pixels, and a few four-pointed twinkles, in 4x4 blocks.
    for _ in range(140):
        x, y = rnd.randrange(0, w // 4) * 4, rnd.randrange(0, int(h * 0.7) // 4) * 4
        b = rnd.choice((150, 190, 230))
        for dx in range(4):
            for dy in range(4):
                px[x + dx, y + dy] = (b, b, min(255, b + 20), 255)
    for _ in range(14):
        cx, cy = rnd.randrange(8, w // 4 - 8) * 4, rnd.randrange(4, int(h * 0.6) // 4) * 4
        for (dx, dy, c) in ((0, 0, 255), (-4, 0, 200), (4, 0, 200), (0, -4, 200), (0, 4, 200), (-8, 0, 140), (8, 0, 140), (0, -8, 140), (0, 8, 140)):
            for i in range(4):
                for j in range(4):
                    if 0 <= cx + dx + i < w and 0 <= cy + dy + j < h:
                        px[cx + dx + i, cy + dy + j] = (c, c, 255, 255)
    # Clouds along the bottom: overlapping round puffs, in 8x8 blocks, shaded underneath.
    puffs = [(rnd.randrange(-40, w + 40), h - rnd.randrange(40, 200), rnd.randrange(60, 130)) for _ in range(26)]
    for by in range(0, h, 8):
        for bx in range(0, w, 8):
            inside = [(cx, cy, r) for cx, cy, r in puffs if (bx - cx) ** 2 + (by - cy) ** 2 <= r * r]
            if not inside:
                continue
            cx, cy, r = min(inside, key=lambda p: p[1])
            shade = (by - (cy - r)) / (2 * r)
            c = (236, 242, 250) if shade < 0.55 else (196, 212, 236) if shade < 0.8 else (160, 182, 220)
            for i in range(8):
                for j in range(8):
                    if bx + i < w and by + j < h:
                        px[bx + i, by + j] = c + (255,)
    return im


ASSETS = os.path.join(ROOT, "ArtStudio", "assets")


def item_sprite(index):
    sheet = Image.open(os.path.join(ASSETS, "items.png")).convert("RGBA")
    cols = sheet.width // 16
    return sheet.crop((index % cols * 16, index // cols * 16, index % cols * 16 + 16, index // cols * 16 + 16))


def prop(sprite, k, degrees):
    """A sprite made big (nearest, a whole number of times) and tilted a
    little, with a soft dark shadow under it, the way the game's update
    cards scatter their foliage round the edges."""
    # Tilted at twice its size, then made big, so it stays clean blocky
    # pixels (tilting the big sprite leaves ragged stair-steps).
    small = scaled(sprite.crop(sprite.getbbox()), 2).rotate(degrees, resample=Image.NEAREST, expand=True)
    big = scaled(small, max(1, k // 2))
    shadow = Image.new("RGBA", big.size, (10, 14, 40, 0))
    shadow.putalpha(big.getchannel("A").point(lambda a: 110 if a else 0))
    out = Image.new("RGBA", (big.width + 14, big.height + 14), (0, 0, 0, 0))
    out.alpha_composite(shadow, (12, 12))
    out.alpha_composite(big, (0, 0))
    return out


def corners(out):
    """The mod's own things, big, in the bottom corners: an easel with its
    painting on the left; the Iridium Palette and Brush on the right,
    and a tube of paint by the easel."""
    w, h = out.size
    easel = Image.open(os.path.join(ASSETS, "bigcraftables.png")).convert("RGBA").crop((0, 0, 16, 32))
    e = prop(easel, 11, 7)
    out.alpha_composite(e, (-30, h - e.height + 60))
    tube = prop(item_sprite(48 + 6), 8, 24)            # a tube of red
    out.alpha_composite(tube, (150, h - tube.height + 10))

    palette = prop(item_sprite(14), 13, -10)            # the Iridium Palette
    out.alpha_composite(palette, (w - palette.width + 50, h - palette.height + 40))
    brush = prop(item_sprite(7), 10, 20)                # the Iridium Brush
    out.alpha_composite(brush, (w - 290, h - 300))


def portrait(sheet, frame, k=4):
    im = Image.open(sheet).convert("RGBA")
    cols = im.width // 64
    p = im.crop((frame % cols * 64, frame // cols * 64, frame % cols * 64 + 64, frame // cols * 64 + 64))
    return scaled(p, k)


def main():
    dev_media, media = sys.argv[1:3]
    card = Image.open(os.path.join(dev_media, "update_card.png")).convert("RGBA")
    slots = open(os.path.join(dev_media, "update_card_slots.txt"), encoding="utf-8").read().split("\n")
    map_icons(card, [s for s in slots if s.strip()])

    margin, over = 80, 110                   # the sky around the card, and the title over its top
    w, h = card.width + 2 * margin, card.height + over + 40
    out = sky(w, h)
    out.alpha_composite(card, (margin, over))

    title = outlined(Image.open(os.path.join(media, "signtext", "sign_pelican_palette.png")).convert("RGBA"), 3,
                     (255, 246, 214), (246, 190, 82), width=2)
    out.alpha_composite(title, ((w - title.width) // 2, over - title.height // 2 + 20))

    corners(out)

    # Sable and Julian at the bottom, cut off by the edge, as the game's cards do.
    sable = portrait(os.path.join(CP, "sable", "portraits_winter.png"), 1)
    julian = portrait(os.path.join(CP, "julian", "portraits.png"), 0)
    out.alpha_composite(sable, (w // 2 - sable.width - 40, h - sable.height + 24))
    out.alpha_composite(julian, (w // 2 + 40, h - julian.height + 24))

    os.makedirs(os.path.join(media, "page"), exist_ok=True)
    path = os.path.join(media, "page", "update_1_2.png")
    out.convert("RGB").save(path)
    print("wrote", path, out.size)


if __name__ == "__main__":
    main()

"""The four brush items (items.png sprites 4-7), drawn pixel by pixel.

The user found the first brushes crude (2026-10-25) and pointed at the
fishing rods: one silhouette, and the tiers told apart by material, from
bamboo to iridium. These do the same. Each brush lies on the diagonal like
a rod (handle bottom-left, bristles top-right) with a dark outline, a lit
upper edge and a shaded lower one. The handles follow the rods (bamboo,
training green, fiberglass white, iridium) and the ferrules the tool
metals:

  Flat Wash    golden cane, tin ferrule, a wide flat tuft of pale bristle
  Sable Round  green lacquer, copper ferrule, a full pointed russet tuft
  Rigger       pearl white, steel ferrule, a long fine dark tuft
  Iridium      iridium purple, gold ferrule, a lilac tuft with a sparkle

Geometry: along the brush, u = x - y (-13 at the handle's end, 13 at the
tip); across it, v = x + y - 15 (0 is the upper, lit edge).

Usage: brushes.py <items.png>   (rewrites sprites 4-7 in place)
"""
import sys
from PIL import Image

OUTLINE = (52, 28, 38)

# Light, mid, dark for each material.
CANE = [(240, 196, 92), (204, 150, 52), (150, 100, 36)]
GREEN = [(150, 196, 84), (92, 146, 52), (52, 96, 42)]
PEARL = [(252, 250, 246), (218, 212, 208), (164, 156, 154)]
RED = (176, 52, 48)
EBONY = [(110, 104, 120), (56, 50, 64), (30, 26, 36)]
IRIDIUM = [(176, 140, 240), (120, 82, 196), (74, 46, 138)]
TIN = [(214, 214, 220), (160, 160, 172), (108, 108, 122)]
COPPER = [(246, 166, 110), (204, 110, 62), (142, 64, 40)]
STEEL = [(240, 244, 250), (186, 194, 210), (120, 128, 150)]
GOLD = [(255, 236, 120), (238, 186, 52), (180, 120, 24)]
PALE = [(250, 238, 206), (226, 204, 160), (180, 150, 108)]
RUSSET = [(214, 140, 86), (164, 92, 52), (110, 58, 36)]
SOOT = [(124, 98, 84), (84, 62, 52), (54, 38, 34)]
LILAC = [(250, 236, 255), (214, 176, 250), (160, 120, 220)]
SPARKLE = (255, 255, 255)


def band(lo, hi):
    return range(lo, hi + 1)


# Cross-section (v range) for each u along the tuft, per brush shape.
TUFTS = {
    "flat":  {4: band(-1, 2), 5: band(-1, 2), 6: band(-1, 2), 7: band(-1, 2), 8: band(-1, 2), 9: band(-1, 2), 10: band(-1, 2)},
    "round": {4: band(-1, 2), 5: band(-1, 2), 6: band(-1, 2), 7: band(-1, 2), 8: band(0, 1), 9: band(0, 1), 10: band(0, 0), 11: band(1, 1)},
    "fine":  {4: band(0, 1), 5: band(0, 1), 6: band(0, 1), 7: band(0, 1), 8: band(0, 1), 9: band(0, 1), 10: band(0, 1), 11: band(0, 0), 12: band(1, 1)},
}

# In the sheet's order, which is Brushes.All's after the hog bristle:
# sable (sprite 4), flat (5), rigger (6), iridium (7). The tiers go
# flat, sable, rigger, iridium (level 2, 3, 6, 9).
BRUSHES = [
    # (handle, ferrule, bristles, tuft shape, butt colour, sparkle)
    (GREEN, COPPER, RUSSET, "round", EBONY[1], False),     # Sable Round, tier 2
    (CANE, TIN, PALE, "flat", None, False),                # Flat Wash, tier 1
    (PEARL, STEEL, SOOT, "fine", RED, False),              # Rigger, tier 3
    (IRIDIUM, GOLD, LILAC, "round", GOLD[1], True),        # Iridium, tier 4
]


def draw(handle, ferrule, bristles, tuft, butt, sparkle):
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    shape = TUFTS[tuft]

    def part(u, v):
        """The material at (u, v), and the v-range of that cross-section."""
        if -13 <= u <= -2:
            rng = band(0, 1)
            return ((butt if butt and u <= -11 else None) or handle, rng) if v in rng else (None, rng)
        if -1 <= u <= 3:
            rng = band(-1, 2) if u >= 1 else band(0, 1)       # the ferrule swells where it crimps the tuft
            return (ferrule, rng) if v in rng else (None, rng)
        rng = shape.get(u)
        if rng and v in rng:
            return bristles, rng
        return None, rng or range(0)

    fill = {}
    for y in range(16):
        for x in range(16):
            u, v = x - y, x + y - 15
            mat, rng = part(u, v)
            if mat is None:
                continue
            if isinstance(mat, tuple):          # a single colour (the butt), shaded by edge
                mat = [mat, mat, mat]
            shade = 0 if v == min(rng) else 2 if v == max(rng) else 1
            fill[(x, y)] = mat[shade]
    for (x, y), c in fill.items():
        px[x, y] = c + (255,)
    for y in range(16):
        for x in range(16):
            if (x, y) in fill:
                continue
            if any((x + dx, y + dy) in fill for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = OUTLINE + (255,)
    if sparkle:
        for x, y in ((14, 4), (11, 1)):
            px[x, y] = SPARKLE + (255,)
    return im


def main():
    path = sys.argv[1]
    sheet = Image.open(path).convert("RGBA")
    for i, spec in enumerate(BRUSHES):
        x0 = (4 + i) * 16
        sheet.paste(Image.new("RGBA", (16, 16), (0, 0, 0, 0)), (x0, 0))
        sheet.alpha_composite(draw(*spec), (x0, 0))
    sheet.save(path)
    print("drew brushes into", path)


if __name__ == "__main__":
    main()

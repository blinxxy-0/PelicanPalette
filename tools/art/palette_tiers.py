"""The better Palettes (items slots 13 and 14), recoloured from the wooden
Palette (slot 9) so all three share one shape: the wood's three tones and
outline swapped for glazed enamel (white with a navy rim) and for iridium
(the purple of the game's iridium tools). The paint dabs stay as they are.

Usage: palette_tiers.py <items.png>   (edited in place)
"""
import sys
from PIL import Image

WOOD = [(232, 186, 120), (200, 146, 84), (150, 100, 56), (52, 28, 38)]      # light, mid, dark, outline
TIERS = {
    13: [(250, 248, 240), (226, 222, 214), (170, 178, 198), (44, 52, 92)],    # enamel
    14: [(214, 180, 250), (172, 124, 226), (118, 76, 170), (48, 24, 70)],     # iridium
}
WOODEN = 9


def slot(i):
    return (i % 16 * 16, i // 16 * 16)


def main():
    path = sys.argv[1]
    items = Image.open(path).convert("RGBA")
    x, y = slot(WOODEN)
    wooden = items.crop((x, y, x + 16, y + 16))
    for index, colours in TIERS.items():
        swap = {w + (255,): c + (255,) for w, c in zip(WOOD, colours)}
        tier = Image.new("RGBA", (16, 16))
        for py in range(16):
            for px in range(16):
                p = wooden.getpixel((px, py))
                tier.putpixel((px, py), swap.get(p, p))
        items.paste(tier, slot(index))
    items.save(path)


if __name__ == "__main__":
    main()

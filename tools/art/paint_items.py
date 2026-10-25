"""The Palette (items.png sprite 9) and a paint tube for every pigment
(sprites 48 onward, rows 3-4, in Palette.All order), drawn pixel by pixel.

Paint became a resource (docs/painting_depth.md, 2026-10-25): you carry a
Palette holding paint for each colour, and refill it from tubes.

  Palette  a wooden kidney-shaped palette, thumb hole, five dabs of paint
  Tube     lying on the diagonal like the brushes (crimped end bottom-left,
           cap top-right): a silver body with a label band in the pigment's
           own colour, shaded light to dark, and a dark cap

Usage: paint_items.py <items.png>   (grows the sheet to 5 rows if needed)
"""
import colorsys, math, os, sys
from PIL import Image

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "ArtStudio.Sim"))

OUTLINE = (52, 28, 38)
SILVER = [(240, 242, 248), (192, 196, 210), (130, 134, 152)]
CAP = [(96, 90, 104), (60, 54, 68), (36, 32, 44)]
WOOD = [(232, 186, 120), (200, 146, 84), (150, 100, 56)]
PALETTE_SPRITE = 9
TUBE_SPRITE = 48

# Palette.All's colours, in order (Core/Palette.cs; append-only there too).
PIGMENTS = [0x2E2430, 0x6E4A2F, 0xA85D33, 0xDCA03E, 0xF3E5C2, 0x8F8A86, 0xC23C3C, 0xE8899E, 0xF4CB3A,
            0x4E7A31, 0x8DC044, 0x4A6894, 0x5FB0E3, 0x7A3E77, 0xF8FBFF, 0x8E5DDB, 0x3CC7B4, 0xE3204F, 0xF2B48C]


def ramp(rgb):
    """Light, mid, dark of a colour (dark shifted cooler, as the game shades)."""
    r, g, b = ((rgb >> 16) & 255) / 255, ((rgb >> 8) & 255) / 255, (rgb & 255) / 255
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    def at(dl, dh=0.0):
        rr, gg, bb = colorsys.hls_to_rgb((h + dh) % 1, max(0.06, min(0.94, l + dl)), s)
        return (round(rr * 255), round(gg * 255), round(bb * 255))
    return [at(0.16), at(0.0), at(-0.18, 0.02)]


def outline(px, fill, size=16):
    for y in range(size):
        for x in range(size):
            if (x, y) not in fill and any((x + dx, y + dy) in fill for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = OUTLINE + (255,)


def tube(rgb):
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    label = ramp(rgb)
    fill = {}
    for y in range(16):
        for x in range(16):
            u, v = x - y, x + y - 15        # along the tube, across it
            if -11 <= u <= -9:
                rng, mat = range(-3, 5), SILVER          # the crimped, flattened end
            elif -8 <= u <= 4:
                rng, mat = range(-2, 4), label if -5 <= u <= 3 else SILVER
            elif u == 5:
                rng, mat = range(-1, 3), SILVER          # the shoulder
            elif 6 <= u <= 9:
                rng, mat = range(-1, 3), CAP
            else:
                continue
            if v not in rng:
                continue
            shade = 0 if v == min(rng) else 2 if v == max(rng) else 1
            fill[(x, y)] = mat[shade]
    for (x, y), c in fill.items():
        px[x, y] = c + (255,)
    outline(px, fill)
    return im


def palette():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    fill = {}
    cx, cy, rx, ry = 7.5, 8.0, 7.0, 5.6
    for y in range(16):
        for x in range(16):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d > 1.0:
                continue
            if (x - 4.5) ** 2 + (y - 9.5) ** 2 <= 1.6:   # thumb hole
                continue
            # Lit from the top left.
            lit = (x - cx) / rx + (y - cy) / ry
            fill[(x, y)] = WOOD[0] if lit < -0.7 else WOOD[2] if lit > 0.75 else WOOD[1]
    dabs = [((9, 5), 0xC23C3C), ((12, 7), 0xF4CB3A), ((11, 10), 0x5FB0E3), ((7, 11), 0x4E7A31), ((5, 6), 0xF3E5C2)]
    for (x, y), rgb in dabs:
        r = ramp(rgb)
        for (dx, dy), shade in (((0, 0), 1), ((1, 0), 1), ((0, 1), 2), ((1, 1), 2)):
            if (x + dx, y + dy) in fill:
                fill[(x + dx, y + dy)] = r[shade]
        fill[(x, y)] = r[0]
    for (x, y), c in fill.items():
        px[x, y] = c + (255,)
    outline(px, fill)
    return im


def main():
    path = sys.argv[1]
    sheet = Image.open(path).convert("RGBA")
    rows = max(sheet.height // 16, math.ceil((TUBE_SPRITE + len(PIGMENTS)) / 16))
    if sheet.height < rows * 16:
        grown = Image.new("RGBA", (sheet.width, rows * 16), (0, 0, 0, 0))
        grown.alpha_composite(sheet)
        sheet = grown

    def put(index, im):
        x, y = (index % 16) * 16, (index // 16) * 16
        sheet.paste(Image.new("RGBA", (16, 16), (0, 0, 0, 0)), (x, y))
        sheet.alpha_composite(im, (x, y))

    put(PALETTE_SPRITE, palette())
    for i, rgb in enumerate(PIGMENTS):
        put(TUBE_SPRITE + i, tube(rgb))
    sheet.save(path)
    print(f"drew the palette and {len(PIGMENTS)} tubes into {path} ({sheet.width}x{sheet.height})")


if __name__ == "__main__":
    main()

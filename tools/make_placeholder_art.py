"""Placeholder sprites for every Art Studio item, until the real art pass.

Writes into ArtStudio/assets/: items.png (16x16 cells, 16 per row),
bigcraftables.png (the easel, drawn by art/easel.py), furniture.png (2x2 and 3x3 picture
frames), skill_icon.png (16x16), skill_page_icon.png (10x10) and
professions/0-5.png (16x16).

Pigment colors are read from ArtStudio/Core/Palette.cs so the jars always
match the palette. Usage: python tools/make_placeholder_art.py
"""
import os, re, sys
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(__file__), "..", "ArtStudio")
ASSETS = os.path.join(ROOT, "assets")

OUT = (59, 36, 20, 255)
WOOD = (138, 90, 50, 255)
WOOD_LIGHT = (176, 122, 69, 255)
WOOD_DARK = (101, 63, 33, 255)
CREAM = (243, 236, 220, 255)
CREAM_DARK = (214, 204, 182, 255)
MAT = (236, 226, 204, 255)
GOLD = (222, 170, 60, 255)
GOLD_LIGHT = (250, 214, 110, 255)
GOLD_DARK = (160, 110, 30, 255)
SILVER = (190, 196, 205, 255)
SILVER_DARK = (120, 126, 140, 255)
GLASS = (210, 230, 235, 255)
CORK = (170, 120, 70, 255)
CLEAR = (0, 0, 0, 0)


def pigments():
    src = open(os.path.join(ROOT, "Core", "Palette.cs"), encoding="utf-8").read()
    return [tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
            for h in re.findall(r'new\("\w+", "[^"]+", 0x([0-9A-Fa-f]{6})', src)]


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)


def frame(d, x0, y0, size, border, outer=OUT, wood=WOOD, light=WOOD_LIGHT, dark=WOOD_DARK, inner=MAT):
    """A square picture frame with a bevelled moulding."""
    x1, y1 = x0 + size - 1, y0 + size - 1
    d.rectangle([x0, y0, x1, y1], fill=outer)
    d.rectangle([x0 + 1, y0 + 1, x1 - 1, y1 - 1], fill=wood)
    d.line([x0 + 1, y0 + 1, x1 - 1, y0 + 1], fill=light)
    d.line([x0 + 1, y0 + 1, x0 + 1, y1 - 1], fill=light)
    d.line([x0 + 1, y1 - 1, x1 - 1, y1 - 1], fill=dark)
    d.line([x1 - 1, y0 + 1, x1 - 1, y1 - 1], fill=dark)
    d.rectangle([x0 + border - 1, y0 + border - 1, x1 - border + 1, y1 - border + 1], fill=dark)
    if inner is not None:
        d.rectangle([x0 + border, y0 + border, x1 - border, y1 - border], fill=inner)
    else:
        d.rectangle([x0 + border, y0 + border, x1 - border, y1 - border], fill=CLEAR)


def canvas(d, ox, w, h):
    """A stretched canvas, slightly turned so its side shows."""
    x0, y0 = ox + (16 - w) // 2, (16 - h) // 2
    d.rectangle([x0, y0, x0 + w, y0 + h], fill=OUT)
    d.rectangle([x0 + 1, y0 + 1, x0 + w - 2, y0 + h - 1], fill=CREAM)
    d.line([x0 + w - 1, y0 + 1, x0 + w - 1, y0 + h - 1], fill=CREAM_DARK)
    d.line([x0 + 1, y0 + h - 1, x0 + w - 2, y0 + h - 1], fill=CREAM_DARK)


def brush(d, ox, handle, bristles, ferrule=SILVER, length=11, width=1, tip=3):
    """A brush lying on a diagonal, handle bottom-left, bristles top-right."""
    for i in range(length):
        x, y = ox + 2 + i, 13 - i
        d.rectangle([x, y, x + width, y + width], fill=handle)
    fx, fy = ox + 2 + length, 13 - length
    d.rectangle([fx, fy, fx + width, fy + width], fill=ferrule)
    for j in range(1, tip + 1):
        d.rectangle([fx + j, fy - j, fx + j + width, fy - j + width], fill=bristles if j < tip else shade(bristles, 0.7))
    # Outline pass: darken any transparent neighbour of a drawn pixel.
    return


def outline(img, box):
    px = img.load()
    x0, y0, x1, y1 = box
    marks = []
    for x in range(x0, x1):
        for y in range(y0, y1):
            if px[x, y][3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if x0 <= nx < x1 and y0 <= ny < y1 and px[nx, ny][3] != 0 and px[nx, ny] != OUT:
                    marks.append((x, y))
                    break
    for m in marks:
        px[m] = OUT


def jar(d, ox, oy, color):
    """A small glass jar of pigment with a cork."""
    d.rectangle([ox + 4, oy + 5, ox + 11, oy + 14], fill=OUT)
    d.rectangle([ox + 5, oy + 6, ox + 10, oy + 13], fill=GLASS)
    d.rectangle([ox + 5, oy + 9, ox + 10, oy + 13], fill=color)
    d.line([ox + 5, oy + 9, ox + 10, oy + 9], fill=shade(color, 1.15))
    d.line([ox + 6, oy + 6, ox + 6, oy + 8], fill=(255, 255, 255, 255))
    d.rectangle([ox + 5, oy + 2, ox + 10, oy + 5], fill=OUT)
    d.rectangle([ox + 6, oy + 3, ox + 9, oy + 4], fill=CORK)


def items():
    img = Image.new("RGBA", (256, 48), CLEAR)
    d = ImageDraw.Draw(img)
    # 0: the painting's frame (art is drawn into its 12x12 middle at runtime).
    frame(d, 0, 0, 16, 2, inner=CREAM)
    # 1-3: canvases, small to large.
    canvas(d, 16, 8, 10)
    canvas(d, 32, 10, 12)
    canvas(d, 48, 12, 14)
    # 4-7: sable, flat wash, rigger, iridium brushes (Brushes.All order after hog bristle).
    brush(d, 64, WOOD_DARK, (80, 50, 30, 255))
    brush(d, 80, WOOD, (200, 170, 120, 255), width=2, length=9, tip=3)
    brush(d, 96, (40, 40, 40, 255), (120, 80, 50, 255), length=9, tip=5)
    brush(d, 112, (120, 70, 190, 255), (200, 160, 255, 255), ferrule=GOLD, length=10, tip=3)
    for ox in (64, 80, 96, 112):
        outline(img, (ox, 0, ox + 16, 16))
    # 8: an empty gilt picture frame.
    frame(d, 128 + 1, 1, 14, 3, outer=OUT, wood=GOLD, light=GOLD_LIGHT, dark=GOLD_DARK, inner=None)
    # 16+: pigment jars, one per palette entry.
    for i, color in enumerate(pigments()):
        cell = 16 + i
        jar(d, (cell % 16) * 16, (cell // 16) * 16, color)
    img.save(os.path.join(ASSETS, "items.png"))


def furniture():
    img = Image.new("RGBA", (80, 48), CLEAR)
    d = ImageDraw.Draw(img)
    frame(d, 0, 0, 32, 3)
    frame(d, 32, 0, 48, 3)
    img.save(os.path.join(ASSETS, "furniture.png"))


def palette_icon(size):
    img = Image.new("RGBA", (size, size), CLEAR)
    d = ImageDraw.Draw(img)
    d.ellipse([0, 1, size - 1, size - 2], fill=OUT)
    d.ellipse([1, 2, size - 2, size - 3], fill=(214, 168, 112, 255))
    hole = size // 4
    d.ellipse([size - hole - 3, size // 2, size - 3, size // 2 + hole - 1], fill=CLEAR)
    dabs = [(194, 60, 60, 255), (220, 160, 62, 255), (78, 122, 49, 255), (74, 104, 148, 255)]
    spots = [(0.25, 0.35), (0.5, 0.25), (0.72, 0.35), (0.3, 0.62)]
    for c, (fx, fy) in zip(dabs, spots):
        x, y = int(fx * size), int(fy * size)
        r = max(1, size // 8)
        d.rectangle([x - r + 1, y - r + 1, x + r - 1, y + r - 1], fill=c)
    return img


def professions():
    os.makedirs(os.path.join(ASSETS, "professions"), exist_ok=True)
    def blank():
        img = Image.new("RGBA", (16, 16), CLEAR)
        return img, ImageDraw.Draw(img)

    # 0 Plein Air: a sun over a hill.
    img, d = blank()
    d.ellipse([4, 1, 11, 8], fill=GOLD_LIGHT, outline=GOLD_DARK)
    d.rectangle([0, 11, 15, 15], fill=(78, 122, 49, 255))
    d.ellipse([-4, 8, 10, 18], fill=(110, 160, 60, 255))
    yield img
    # 1 Gallerist: a gold coin in a frame.
    img, d = blank()
    frame(d, 0, 0, 16, 2, inner=MAT)
    d.ellipse([4, 4, 11, 11], fill=GOLD, outline=GOLD_DARK)
    d.line([7, 6, 7, 9], fill=GOLD_LIGHT)
    yield img
    # 2 Steady Hand: a perfectly straight stroke.
    img, d = blank()
    d.rectangle([1, 1, 14, 14], fill=CREAM, outline=OUT)
    d.line([3, 12, 12, 3], fill=(74, 104, 148, 255), width=2)
    yield img
    # 3 Visionary: a star burst.
    img, d = blank()
    d.polygon([(8, 0), (10, 6), (16, 6), (11, 10), (13, 16), (8, 12), (3, 16), (5, 10), (0, 6), (6, 6)], fill=GOLD_LIGHT, outline=GOLD_DARK)
    yield img
    # 4 Patron's Favorite: a heart.
    img, d = blank()
    d.ellipse([1, 2, 8, 9], fill=(214, 70, 90, 255))
    d.ellipse([7, 2, 14, 9], fill=(214, 70, 90, 255))
    d.polygon([(1, 6), (14, 6), (8, 14)], fill=(214, 70, 90, 255))
    outline(img, (0, 0, 16, 16))
    yield img
    # 5 Prolific: a stack of canvases.
    img, d = blank()
    for i, y in enumerate((9, 5, 1)):
        d.rectangle([2 + i, y, 13 + i - 2, y + 5], fill=CREAM, outline=OUT)
    yield img


def main():
    items()
    # The easel has real art now (tools/art/easel.py), not a placeholder.
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
    import easel as easel_art
    easel_art.image().save(os.path.join(ASSETS, "bigcraftables.png"))
    furniture()
    palette_icon(16).save(os.path.join(ASSETS, "skill_icon.png"))
    palette_icon(10).save(os.path.join(ASSETS, "skill_page_icon.png"))
    for i, img in enumerate(professions()):
        img.save(os.path.join(ASSETS, "professions", f"{i}.png"))
    print("placeholder art written to", os.path.abspath(ASSETS))


if __name__ == "__main__":
    main()

"""The Varnishing Cabinet (big craftable, sheet slot 2), the Varnish tin
(items slot 10) and the linen canvases (items slots 11 and 12), drawn
pixel by pixel. The cabinet uses the keg's and bee house's wood palette
(sampled from the game's Craftables sheet) so it sits beside them. A
canvas waits on a rack behind its door: the mod draws the painting being
varnished over that canvas.

Usage: varnish_items.py <bigcraftables.png> <items.png>   (both edited in place)
"""
import sys
from PIL import Image

WOOD = {
    "o": (75, 43, 24), "D": (119, 59, 31), "R": (160, 81, 41), "M": (206, 120, 41),
    "L": (240, 167, 64), "H": (255, 201, 102),
    "i": (44, 26, 16), "s": (196, 214, 222),             # inside the cabinet, a glint on the glass
    "c": (244, 236, 214), "k": (214, 200, 170),          # the canvas on the rack
    "t": (43, 43, 53), "S": (216, 216, 224), "G": (154, 154, 168), "g": (106, 106, 120),   # tin
    "A": (224, 149, 42), "a": (168, 96, 26), "y": (255, 210, 122),                       # amber varnish
}

def cabinet_rows():
    """32 rows: a crown moulding, a frieze, a glazed door (the canvas on its
    rack behind it, x 4-11 and y 13-22), a drawer, and turned feet. The
    sides carry the wood grain: lit on the left, shaded on the right."""
    rows = ["." * 16] * 5 + [
        ".oooooooooooooo.",
        "oHHHHHHHHHHHHHHo",
        "oLLLLLLLLLLLLLLo",
        "oMMMMMMMMMMMMMMo",
        "oooooooooooooooo",
        "oLMHMMMHMMMHMMRo",
        "oMooooooooooooRo",
    ]
    inside = ["siiiiiiiii"] + ["iccccccckH"] * 10 + ["ikkkkkkkkH", "LLLLLLLLLL"]
    for n, row in enumerate(inside):
        left, right = ("L", "R") if n % 3 else ("M", "D")
        rows.append("o" + left + "o" + row + "o" + right + "o")
    rows += [
        "oMooooooooooooRo",
        "oLMMMMoHHoMMMMRo",
        "oMRRRRRRRRRRRRDo",
        "oooooooooooooooo",
        ".oDDo......oDDo.",
        ".oRDo......oRDo.",
        "..oo........oo..",
    ]
    return rows


CABINET = cabinet_rows()
# The tin of varnish standing on top, at the back right.
TIN_ON_CABINET = [
    ".ttt.",
    "tSSSt",
    "tAyAt",
    "tAAat",
    "taaat",
    ".ttt.",
]

VARNISH = [
    "................",
    "................",
    "................",
    "....tttttttt....",
    "...tSSSSSSSSt...",
    "...tgGAGGGGgt...",
    "...tAyAAAAAat...",
    "...tAyAttAAat...",
    "...tAyAAAAAat...",
    "...tAAAAAAAat...",
    "...tgGGGGGGgt...",
    "...tgGGGGGGgt...",
    "....tttttttt....",
    "................",
    "................",
    "................",
]

# Linen: the canvas sprites, rewoven in a warmer, greyer cloth with a
# faint crosshatch, so they read as the better canvas at a glance.
LINEN, LINEN_DARK = (226, 214, 186), (212, 199, 170)


def draw(rows, palette):
    im = Image.new("RGBA", (len(rows[0]), len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        assert len(row) == im.width, (y, row)
        for x, ch in enumerate(row):
            if ch != ".":
                im.putpixel((x, y), palette[ch] + (255,))
    return im


def linen(canvas):
    out = canvas.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            # Only the pale cloth, not its frame or shading.
            if a and r > 200 and g > 190 and b > 160:
                px[x, y] = (LINEN_DARK if x % 2 == 0 and y % 2 == 0 else LINEN) + (255,)
    return out


def main():
    big_path, items_path = sys.argv[1], sys.argv[2]
    big = Image.open(big_path).convert("RGBA")
    if big.width < 48:
        grown = Image.new("RGBA", (48, big.height), (0, 0, 0, 0))
        grown.paste(big, (0, 0))
        big = grown
    cabinet = draw(CABINET, WOOD)
    cabinet.alpha_composite(draw(TIN_ON_CABINET, WOOD), (10, 2))
    big.paste(Image.new("RGBA", (16, 32)), (32, 0))
    big.alpha_composite(cabinet, (32, 0))
    big.save(big_path)

    items = Image.open(items_path).convert("RGBA")
    def slot(i):
        return (i % 16 * 16, i // 16 * 16)
    def put(i, im):
        x, y = slot(i)
        items.paste(Image.new("RGBA", (16, 16)), (x, y))
        items.alpha_composite(im, (x, y))
    put(10, draw(VARNISH, WOOD))
    for i, canvas in ((11, 2), (12, 3)):           # medium and large canvases
        x, y = slot(canvas)
        put(i, linen(items.crop((x, y, x + 16, y + 16))))
    items.save(items_path)


if __name__ == "__main__":
    main()

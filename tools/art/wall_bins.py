"""Open bins of art supplies down the shop's left-hand wall, and a double
crate beside them, drawn pixel by pixel at the game's scale.

The user's notes (2026-10-24): the generated bins sat at an angle and
their goods were huge (paint tubes half a person's height). Pierre's seed
bins are the model: a column one tile wide, each bin a straight-on tray
16 pixels wide and 9 tall (a dark frame, a lit rim, a 12x5 well of goods,
a front lip), the next bin directly below; every item a pixel or two.
These follow that layout in a similar wood palette, with our own goods.

Used by build_shop_furniture.py.
"""
from PIL import Image

WOOD = {
    "G": (104, 43, 27),     # frame outline
    "H": (137, 66, 5),      # rim, shaded
    "I": (183, 104, 5),     # rim corners
    "J": (201, 130, 33),    # rim, lit
    "R": (218, 147, 33),    # inner sides, lip
    "K": (120, 62, 16),     # the well's shadowed back
}

# One row pattern per well row (5 rows, 12 wide); letters are this bin's
# own colours, "." shows the well's back.
BINS = {
    "tubes_red": ({"a": (196, 40, 52), "b": (140, 24, 40), "c": (200, 200, 210)},
                  ["abbcabbcabbc", "bcabbcabbcab", "abbcabbcabbc", "cabbcabbcabb", "abbcabbcabbc"]),
    "tubes_yellow": ({"a": (240, 196, 48), "b": (196, 140, 24), "c": (200, 200, 210)},
                     ["abbcabbcabbc", "bcabbcabbcab", "abbcabbcabbc", "cabbcabbcabb", "abbcabbcabbc"]),
    "tubes_blue": ({"a": (64, 120, 200), "b": (40, 76, 150), "c": (200, 200, 210)},
                   ["abbcabbcabbc", "bcabbcabbcab", "abbcabbcabbc", "cabbcabbcabb", "abbcabbcabbc"]),
    "tubes_green": ({"a": (76, 168, 64), "b": (44, 112, 48), "c": (200, 200, 210)},
                    ["abbcabbcabbc", "bcabbcabbcab", "abbcabbcabbc", "cabbcabbcabb", "abbcabbcabbc"]),
    "brushes": ({"h": (150, 92, 44), "d": (96, 56, 30), "f": (210, 210, 220), "t": (60, 40, 30)},
                ["t.t.t.t.t.t.", "fhfhfhfhfhfh", "hdhdhdhdhdhd", "dhdhdhdhdhdh", "hdhdhdhdhdhd"]),
    "pencils": ({"r": (210, 52, 52), "o": (236, 140, 40), "y": (236, 210, 64), "g": (72, 160, 72), "b": (64, 110, 200), "p": (140, 72, 180), "w": (232, 200, 150)},
                ["wwwwwwwwwwww", "roygbproygbp", "roygbproygbp", "roygbproygbp", "oygbproygbpr"]),
    "yarn": ({"a": (196, 64, 96), "b": (140, 40, 72), "c": (72, 140, 200), "d": (44, 96, 150), "e": (236, 196, 80), "f": (190, 140, 40)},
             ["abacdcefeaba", "babdcdfefbab", "cdcefeabacdc", "dcdfefbabdcd", "efeabacdcefe"]),
    "clay": ({"a": (196, 104, 72), "b": (150, 72, 48), "c": (150, 150, 160), "d": (104, 104, 116)},
             ["aaab.cccd.aa", "aaab.cccd.aa", "bbbb.dddd.bb", "cccd.aaab.cc", "dddd.bbbb.dd"]),
    "paper": ({"a": (240, 232, 210), "b": (200, 186, 156), "c": (236, 200, 168)},
              ["abaabaabaaba", "babbabbabbab", "abaabaabaaba", "cbccbccbccbc", "abaabaabaaba"]),
    "charcoal": ({"a": (52, 48, 56), "b": (84, 80, 92), "c": (30, 28, 34)},
                 ["abacabacabac", "cabacabacaba", "abacabacabac", "bacabacabaca", "abacabacabac"]),
}
COLUMN = ["tubes_red", "tubes_yellow", "tubes_green", "tubes_blue", "brushes", "pencils", "yarn", "clay", "paper", "charcoal"]


def tray(width, goods, colours):
    """One bin, `width` pixels wide and 8 tall (the 9th row is the next
    bin's top edge, or the column's base)."""
    inner = width - 4
    rows = ["GGG" + "H" * (width - 5) + "GG",
            "GI" + "J" * (width - 4) + "IG"]
    for i, line in enumerate(goods):
        side = "R" if 1 <= i <= 3 else "J"
        rows.append("G" + side + (line * 4)[:inner] + side + "G")
    rows.append("GIJ" + "R" * (width - 6) + "JIG")
    im = Image.new("RGBA", (width, len(rows)), (0, 0, 0, 0))
    px = im.load()
    for y, line in enumerate(rows):
        for x, ch in enumerate(line):
            px[x, y] = (colours.get(ch) or WOOD.get(ch, WOOD["K"])) + (255,)
    return im


def column():
    """The bins stacked down the wall, one tile wide, closed at the base."""
    bins = [tray(16, BINS[name][1], BINS[name][0]) for name in COLUMN]
    pitch = bins[0].height
    im = Image.new("RGBA", (16, pitch * len(bins) + 1), (0, 0, 0, 0))
    for i, b in enumerate(bins):
        im.alpha_composite(b, (0, i * pitch))
    base = tray(16, ["." * 12] * 5, {})
    im.paste(base.crop((0, 0, 16, 1)), (0, pitch * len(bins)))
    return im


def double_crate(left, right):
    """Two bins side by side on a low wooden stand (2 tiles wide), its
    front planks below: Pierre's produce crates, with our goods."""
    a = tray(16, BINS[left][1], BINS[left][0])
    b = tray(16, BINS[right][1], BINS[right][0])
    im = Image.new("RGBA", (32, 24), (0, 0, 0, 0))
    im.alpha_composite(a, (0, 0))
    im.alpha_composite(b, (16, 0))
    px = im.load()
    front = [WOOD["G"]] + [WOOD["J"], WOOD["R"], WOOD["R"], WOOD["H"]] * 3 + [WOOD["H"], WOOD["G"]]
    for y in range(8, 24):
        for x in range(32):
            edge = x in (0, 31) or y == 23
            px[x, y] = (WOOD["G"] if edge else front[(y - 8) % len(front)] if x not in (15, 16) else WOOD["G"]) + (255,)
    return im

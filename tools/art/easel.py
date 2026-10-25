"""The easel sprite (16x32): the Easel item, and the display easel in the
art shop's yard. Drawn pixel by pixel in the vanilla fence's wood palette
(sampled from the Forest fence and barrels) so it sits beside them: a
shaded wooden A-frame with a clamp and tray, a small landscape on the
canvas, and dabs of paint on the tray.

Usage: easel.py <bigcraftables.png out>
"""
import sys
from PIL import Image

PALETTE = {
    "O": (62, 39, 36), "D": (90, 51, 34), "M": (125, 98, 58), "L": (162, 133, 59), "H": (190, 159, 52),  # wood
    "c": (244, 236, 214), "k": (214, 200, 170),                                                            # canvas
    "S": (125, 186, 230), "s": (163, 212, 240), "u": (248, 214, 96),                                       # sky, sun
    "g": (88, 158, 58), "G": (56, 118, 48), "r": (205, 66, 60), "b": (72, 108, 176), "y": (236, 196, 64),
}

TOP = [
    "......OHMO......",
    ".....OOLMDOO....",
    ".....OHLMMDO....",
    ".OOOOOOOOOOOOOO.",
]
CANVAS = [            # 12 wide: cream margin left, shaded margin right and bottom
    "ccccccccccck",
    "cSSSSSSSSuuk",
    "cSssSSSSuuuk",
    "cSSssSSSSuuk",
    "cSSSSSSSSSSk",
    "cSSSSSggSSSk",
    "cSggSggggSSk",
    "cggggggGgggk",
    "cgGgggGGGgGk",
    "cGGrGGGGGGGk",
    "cGGGGGGGrGGk",
    "cGGGGGGGGGGk",
    "kkkkkkkkkkkk",
]
TRAY = [
    ".OOOOOOOOOOOOOO.",
    "OHLLrLbLyLLLLLMO",
    "ODDDDDDDDDDDDDDO",
]
LEG_ROWS = 12
LEFT = [2, 2, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0]    # x of each row's front-left leg ("OLDO")
RIGHT = [10, 10, 10, 10, 11, 11, 11, 11, 12, 12, 12, 12]
BACK_ROWS = 9                                    # the back leg is shorter: it stands further away


def rows():
    out = list(TOP)
    out += ["." + "O" + line + "O" + "." for line in CANVAS]
    out += TRAY
    for i in range(LEG_ROWS):
        row = ["."] * 16
        if i < BACK_ROWS:
            row[6:10] = list("ODDO")
        row[LEFT[i]:LEFT[i] + 4] = list("OLDO")
        row[RIGHT[i]:RIGHT[i] + 4] = list("OMDO")
        out.append("".join(row))
    assert len(out) == 32 and all(len(r) == 16 for r in out), [len(r) for r in out]
    return out


def image():
    im = Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    for y, row in enumerate(rows()):
        for x, ch in enumerate(row):
            if ch != ".":
                im.putpixel((x, y), PALETTE[ch] + (255,))
    return im


if __name__ == "__main__":
    # The easel is sprite 0; the sheet's other sprites (the Pigment Mill, 1)
    # are kept.
    import os
    path = sys.argv[1]
    sheet = Image.open(path).convert("RGBA") if os.path.exists(path) else Image.new("RGBA", (16, 32), (0, 0, 0, 0))
    sheet.paste(image(), (0, 0))
    sheet.save(path)
    print("wrote", path)

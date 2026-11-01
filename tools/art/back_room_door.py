"""Draws the back room door's two additions (1.2, from the user: the back
room needed to read as a room artists are welcome in, not a storage
closet):
  - door_sign.png (48x16): "ARTISTS / ONLY!" on a cream board with a rust
    border, the shop's colours, hung on two nails, for the wall over the door
  - door_window.png (16x16): a lit pane with a cross of glazing bars, drawn
    over the door's upper panel (the door itself is the game's own tile)
Outlines are dark shades of their own colours, not black.

Usage: back_room_door.py <out dir>
"""
import os, sys
from PIL import Image

CREAM, CREAM_SHADE = (236, 220, 184), (206, 184, 146)
RUST, RUST_DARK = (168, 84, 50), (104, 46, 30)
INK = (70, 38, 34)
NAIL = (120, 110, 104)
GLASS_HI, GLASS, GLASS_LO = (255, 238, 176), (246, 206, 120), (214, 160, 84)
FRAME = (92, 52, 36)

# A 3x5 pixel font, just the letters needed.
GLYPHS = {
    "A": ["010", "101", "111", "101", "101"],
    "R": ["110", "101", "110", "101", "101"],
    "T": ["111", "010", "010", "010", "010"],
    "I": ["111", "010", "010", "010", "111"],
    "S": ["011", "100", "010", "001", "110"],
    "O": ["010", "101", "101", "101", "010"],
    "N": ["101", "111", "111", "101", "101"],
    "L": ["100", "100", "100", "100", "111"],
    "Y": ["101", "101", "010", "010", "010"],
    "!": ["010", "010", "010", "000", "010"],
}


def text(px, word, x, y, colour):
    for ch in word:
        for j, row in enumerate(GLYPHS[ch]):
            for i, bit in enumerate(row):
                if bit == "1":
                    px[x + i, y + j] = colour + (255,)
        x += 4


def sign():
    im = Image.new("RGBA", (48, 16), (0, 0, 0, 0))
    px = im.load()
    x0, y0, x1, y1 = 3, 1, 44, 15          # the board
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            edge = x in (x0, x1) or y in (y0, y1)
            inner = x in (x0 + 1, x1 - 1) or y in (y0 + 1, y1 - 1)
            px[x, y] = (RUST_DARK if edge else RUST if inner else CREAM_SHADE if y == y1 - 2 else CREAM) + (255,)
    for nx in (9, 38):                     # two nails it hangs from
        px[nx, y0 - 1] = NAIL + (255,)
        px[nx, y0] = RUST_DARK + (255,)
    text(px, "ARTISTS", 24 - 27 // 2, y0 + 2, INK)
    text(px, "ONLY!", 24 - 19 // 2, y0 + 8, INK)
    return im


def window():
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    x0, y0, x1, y1 = 4, 5, 11, 13          # inside the door's upper panel
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if x in (x0, x1) or y in (y0, y1) or x == 7 or y == 9:
                px[x, y] = FRAME + (255,)
            else:
                # Light from the room: brightest at the top left of each pane.
                corner = (x - x0) + (y - y0) if x < 7 else (x - 8) + (y - y0)
                px[x, y] = (GLASS_HI if corner <= 1 else GLASS_LO if (x in (6, x1 - 1) or y in (8, y1 - 1)) else GLASS) + (255,)
    return im


def main():
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    sign().save(os.path.join(out, "door_sign.png"))
    window().save(os.path.join(out, "door_window.png"))
    print("wrote", out)


if __name__ == "__main__":
    main()

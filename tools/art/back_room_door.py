"""Draws the back room door's two additions (1.2, from the user: the back
room needed to read as a room artists are welcome in, not a storage
closet):
  - door_sign.png (48x16): "ARTISTS / ONLY!" on a paint-splashed cream
    board with a palette on its corner, for the wall over the door
  - door_window.png (16x16): a lit pane with a cross of glazing bars, drawn
    over the door's upper panel (the door itself is the game's own tile)
Outlines are dark shades of their own colours, not black.

Usage: back_room_door.py <out dir>
"""
import os, sys
from PIL import Image

BOARD, BOARD_SHADE = (237, 221, 190), (220, 196, 158)
FRAME, FRAME_LIGHT, FRAME_DARK = (176, 67, 42), (198, 108, 71), (106, 31, 23)
INK = (75, 49, 29)
PAINT_RED, PAINT_RED_DARK = (155, 38, 27), (116, 35, 22)
PAINT_GREEN, PAINT_GREEN_DARK = (130, 164, 30), (99, 132, 0)
PAINT_BLUE, PAINT_BLUE_DARK = (93, 99, 162), (62, 66, 120)
PALETTE, PALETTE_DARK = (220, 168, 101), (150, 98, 52)
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
    # The user's pick of the PixelLab boards (sign_painted_13: a cream board
    # in a red-brown frame, splashed with paint, a palette on its corner),
    # redrawn small enough to fit between the ceiling trim and the door,
    # clear of the window: 33x15, centred over the door.
    im = Image.new("RGBA", (48, 16), (0, 0, 0, 0))
    px = im.load()
    x0, y0, x1, y1 = 8, 0, 40, 14          # the board
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            edge = x in (x0, x1) or y in (y0, y1)
            # (A single row of frame top and bottom, so the letters breathe.)
            inner = x in (x0 + 1, x1 - 1)
            colour = FRAME_DARK if edge else FRAME if inner else BOARD_SHADE if y == y1 - 1 else BOARD
            px[x, y] = colour + (255,)
    # Paint: a red smear at the top left, a green drip at the top right, a
    # blue blob at the bottom left.
    for x, y in ((9, 1), (10, 1), (11, 1), (9, 2), (10, 2), (9, 3)):
        px[x, y] = (PAINT_RED if (x + y) % 3 else PAINT_RED_DARK) + (255,)
    for x, y in ((35, 0), (36, 0), (37, 0), (38, 0), (36, 1), (37, 1), (38, 1), (38, 2), (38, 3), (38, 4)):
        px[x, y] = (PAINT_GREEN if y < 2 else PAINT_GREEN_DARK) + (255,)
    for x, y in ((9, 12), (9, 13), (10, 13), (11, 13), (10, 14)):
        px[x, y] = (PAINT_BLUE if y == 13 else PAINT_BLUE_DARK) + (255,)
    # A little palette hung on the bottom right corner, over the frame.
    for y in range(11, 16):
        for x in range(37, 43):
            dx, dy = (x - 39.5) / 3.0, (y - 13) / 2.5
            if dx * dx + dy * dy <= 1.0:
                outline = (x - 39.5) ** 2 / 9 + (y - 13) ** 2 / 6.25 > 0.55
                px[x, y] = (PALETTE_DARK if outline else PALETTE) + (255,)
    for x, y, c in ((38, 12, PAINT_RED), (40, 12, PAINT_BLUE), (41, 13, PAINT_GREEN)):
        px[x, y] = c + (255,)
    px[39, 14] = PALETTE_DARK + (255,)     # the thumb hole
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

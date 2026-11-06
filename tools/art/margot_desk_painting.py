"""The damaged painting on Margot's desk in her 2-heart event (1.3, from
playtesting: the scene says one's there, and nothing was).

The Old Valley's farm (A Farm at Evening) as it's found, from the game's own
damaged picture (`museart_exportold valley_farm`, a pixel a cell, 28x28),
halved to 14x14 in a 1-pixel frame and stood up on the desk. The sprite is
24x32, placed at the desk's tile (17, 14) in the museum: the painting sits
at x 2-17, its foot on the desk's top at y 23.

Usage: margot_desk_painting.py <old_valley_farm_damaged.png> <out.png>
"""
import sys
from PIL import Image

FRAME, FRAME_DARK = (122, 78, 44, 255), (86, 52, 30, 255)


def main():
    damaged = Image.open(sys.argv[1]).convert("RGBA")
    assert damaged.size == (28, 28), damaged.size
    picture = damaged.resize((14, 14), Image.NEAREST)
    out = Image.new("RGBA", (24, 32), (0, 0, 0, 0))
    x0, y0 = 2, 8
    for x in range(16):
        for y in range(16):
            edge = x in (0, 15) or y in (0, 15)
            if edge:
                out.putpixel((x0 + x, y0 + y), FRAME_DARK if y == 15 or x == 15 else FRAME)
    out.paste(picture, (x0 + 1, y0 + 1))
    out.save(sys.argv[2])
    print("wrote", sys.argv[2])


if __name__ == "__main__":
    main()

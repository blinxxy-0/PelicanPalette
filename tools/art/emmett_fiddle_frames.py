"""Emmett's fiddle frames for his heart events (docs/vale_events.md; built by
tools/build_emmett_events.py). PLACEHOLDERS, drawn by script: a small fiddle and bow added to
his own approved frames, to be redrawn properly later. Nothing of his walk or sitting frames
(0 to 17) changes; this writes frames 18 to 23 and so grows the sheet to six rows (64 x 192).

  18, 19  standing, facing up (to the amphitheatre's seats), playing: the bow in two places
  20, 21  sitting facing left (his frame 16), playing; mirrored in the events for facing right
  22      sitting facing left, the fiddle across his knees
  23      sitting facing left, his notebook in his lap and a pencil in his mouth

Outlines are a darker shade of the colour they border, never black (the user's rule).

Usage: python tools/art/emmett_fiddle_frames.py          (from the repository root; rerunnable)
"""
import os
from PIL import Image

SHEET = os.path.join("[CP] Gilded Vale (dev)", "assets", "emmett", "sprites.png")

WOOD = (178, 92, 40)          # the fiddle's body
WOOD_LIT = (214, 134, 64)
NECK = (92, 50, 30)           # neck and scroll, ebony-ish brown
STICK = (98, 60, 36)          # the bow's stick
HAIR = (232, 220, 188)        # the bow's hair
SKIN = (232, 160, 88)         # his hand (the sheet's own skin tone)
PAGE = (236, 226, 198)
COVER = (124, 78, 46)         # the notebook's cover (the sheet's coat brown)
PENCIL = (232, 190, 70)


def shade(c):
    """An outline: the neighbouring colour, darker, in its own hue (tools/art/vanillafy.py)."""
    return (int(c[0] * 0.52), int(c[1] * 0.42), int(c[2] * 0.46))


def frame(sheet, n):
    return sheet.crop(((n % 4) * 16, (n // 4) * 32, (n % 4) * 16 + 16, (n // 4) * 32 + 32))


def put(im, pts, colour):
    px = im.load()
    for x, y in pts:
        if 0 <= x < 16 and 0 <= y < 32:
            px[x, y] = colour + (255,)


def line(x0, y0, x1, y1):
    """Bresenham's line, as pixels."""
    pts, dx, dy = [], abs(x1 - x0), -abs(y1 - y0)
    sx, sy, err = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1), abs(x1 - x0) - abs(y1 - y0)
    while True:
        pts.append((x0, y0))
        if (x0, y0) == (x1, y1):
            return pts
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def fiddle_body(im, x, y):
    """A 6 x 4 fiddle body, its top-left at (x, y), lit on top, outlined in its own darker wood."""
    rows = [".oooo.", "oooooo", "ooo.oo", ".oooo."]
    pts = [(x + i, y + j) for j, r in enumerate(rows) for i, ch in enumerate(r) if ch != "."]
    put(im, pts, WOOD)
    put(im, [(x + 1, y), (x + 2, y), (x + 1, y + 1), (x + 2, y + 1)], WOOD_LIT)
    put(im, [(x + 3, y + 2)], NECK)                                           # the f-holes, one dark pixel
    put(im, [(x, y + 2), (x + 5, y + 2), (x + 1, y + 3), (x + 4, y + 3)], shade(WOOD))


def bow(im, x0, y0, x1, y1):
    """The bow: hair with the stick a pixel above it, and his hand holding its frog end (x0, y0)."""
    put(im, [(x, y - 1) for x, y in line(x0, y0, x1, y1)], STICK)
    put(im, line(x0, y0, x1, y1), HAIR)
    put(im, [(x0, y0), (x0 + 1, y0)], SKIN)
    put(im, [(x0, y0 + 1)], shade(SKIN))


def playing_up(sheet, shift):
    """From behind (his frame 8): the fiddle's neck and scroll past his left ear, his bowing
    elbow out on the right, and the bow's tip showing past his left shoulder."""
    im = frame(sheet, 8)
    put(im, [(1, 9), (1, 10), (2, 11), (2, 12)], NECK)                         # neck and scroll
    put(im, [(0, 9)], shade(NECK))
    put(im, [(13, 14), (14, 14), (14, 15), (15, 15), (14, 16), (15, 16)], (86, 50, 32))   # the bowing elbow, out
    put(im, [(15, 14), (15, 17), (14, 17)], shade((86, 50, 32)))
    tip = line(3 - shift, 14 + shift // 2, 0, 13 + shift // 2)                # the bow's tip
    put(im, [(x, y - 1) for x, y in tip], STICK)
    put(im, tip, HAIR)
    return im


def sitting_playing(sheet, shift):
    """Side on, facing left (his frame 16): the fiddle under his chin, the bow across it."""
    im = frame(sheet, 16)
    fiddle_body(im, 3, 13)
    put(im, [(2, 13), (1, 12), (0, 12)], NECK)
    put(im, [(0, 11)], shade(NECK))
    bow(im, 12 + shift, 19 + shift // 2, 1 + shift, 12 + shift // 2)
    return im


def across_knees(sheet):
    im = frame(sheet, 16)
    fiddle_body(im, 8, 19)
    put(im, [(7, 21), (6, 21), (5, 21)], NECK)
    put(im, [(4, 20), (4, 21)], shade(NECK))
    return im


def notebook(sheet):
    im = frame(sheet, 16)
    put(im, [(x, y) for x in range(8, 13) for y in range(18, 21)], PAGE)
    put(im, [(x, 21) for x in range(8, 13)] + [(7, 18), (7, 19), (7, 20)], COVER)
    put(im, [(13, 18), (13, 19), (13, 20), (13, 21)], shade(COVER))
    put(im, [(9, 19), (10, 19), (11, 19)], shade(PAGE))                        # a scrawled line
    put(im, [(2, 11), (3, 11), (4, 11)], PENCIL)                               # the pencil in his mouth
    put(im, [(1, 11)], shade(PENCIL))
    return im


def main():
    sheet = Image.open(SHEET).convert("RGBA")
    assert sheet.size[0] == 64 and sheet.size[1] >= 160, sheet.size
    out = Image.new("RGBA", (64, 192))
    out.paste(sheet.crop((0, 0, 64, 160)), (0, 0))          # frames 0 to 17 kept as they are (and 18, 19 cleared)
    out.paste(Image.new("RGBA", (32, 32)), (32, 128))
    frames = {18: playing_up(sheet, 0), 19: playing_up(sheet, 2),
              20: sitting_playing(sheet, 0), 21: sitting_playing(sheet, 2),
              22: across_knees(sheet), 23: notebook(sheet)}
    for n, im in frames.items():
        out.paste(im, ((n % 4) * 16, (n // 4) * 32))
    out.save(SHEET)
    print("wrote frames 18 to 23 into", SHEET)


if __name__ == "__main__":
    main()

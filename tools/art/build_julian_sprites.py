"""Builds Julian Vane's walking sprite sheet (Characters/<id>: 64x128,
16x32 frames; rows walk down, right, up, left, 4 frames each, as vanilla)
from the PixelLab candidate the user picked (walk_b_1, 2026-10-31, drawn
with Elliott's sheet as the style reference).

Vanilla clean-up as Sable's (build_sable_sprites.py): hard alpha, no
pure-black outlines, and one shared palette (vanilla NPC frames measure
10-30 colours). The coat came out nearly black, a silhouette beside
vanilla NPCs, whose dark clothes are mid-tones inside a darker outline: its
inner greys are lifted to charcoal, the outer edge kept dark.

The side views' glasses came out as a smear of dark pixels across the face
(the user: they "don't really look placed on his face"). The right-facing
head is redrawn on its eye rows: one lens (dark rims either side of a
light glint) at the front of the face; that head goes on all
four right-facing frames, following the walk's bob, and the left-facing row
is the right-facing one mirrored, as many vanilla sheets are.

Usage: build_julian_sprites.py <PixelLab generations dir> <out.png>
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vanillafy import quantize, recolour_black

PICK = ("stardew-julian-walk", "walk_b", 1)
COLOURS = 24


def lift_charcoal(im, threshold=70, grey=24):
    """Lifts dark, colourless pixels inside the figure (not on its edge)."""
    px = im.load()
    w, h = im.size
    changes = {}
    for y in range(h):
        for x in range(w):
            p = px[x, y]
            if not p[3] or max(p[:3]) > threshold or max(p[:3]) - min(p[:3]) > grey:
                continue
            edge = any(not (0 <= x + dx < w and 0 <= y + dy < h) or px[x + dx, y + dy][3] == 0
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if not edge:
                r, g, b = p[:3]
                changes[(x, y)] = (min(255, int(r * 1.3) + 8), min(255, int(g * 1.3) + 9), min(255, int(b * 1.3) + 14), 255)
    for k, v in changes.items():
        px[k] = v
    return im


RIGHT_ROW, LEFT_ROW = 1, 3
SKIN, SKIN_LIGHT, SKIN_SHADE = (218, 151, 124), (249, 192, 153), (163, 97, 83)
RIM, GLINT, HAIR = (49, 23, 49), (196, 212, 226), (70, 74, 84)
# Frame 0's eye rows (y 8 and 9 of the frame), x 5 to 12: hair, ear, cheek,
# then the lens (rim, glint, rim) at the front of the face. (A long dark arm
# back to the ear read as a band across the cheek at 1x.)
EYE_ROWS = {
    8: [HAIR, SKIN_SHADE, SKIN, SKIN, RIM, GLINT, RIM, SKIN],
    9: [HAIR, SKIN, SKIN_LIGHT, SKIN_LIGHT, SKIN, SKIN_SHADE, SKIN_LIGHT, SKIN],
}
HEAD_ROWS = range(3, 11)      # frame 0's head, copied to the other frames


def fix_side_heads(sheet):
    px = sheet.load()
    y0 = RIGHT_ROW * 32
    for y, colours in EYE_ROWS.items():
        for i, c in enumerate(colours):
            px[5 + i, y0 + y] = c + (255,)

    def top(f):
        return min(y for y in range(y0, y0 + 32) for x in range(f * 16, f * 16 + 16) if px[x, y][3]) - y0

    head = {(x, y): px[x, y0 + y] for y in HEAD_ROWS for x in range(16)}
    base = top(0)
    for f in range(1, 4):
        bob = top(f) - base
        for y in range(HEAD_ROWS.start, HEAD_ROWS.start + bob):
            for x in range(16):
                px[f * 16 + x, y0 + y] = (0, 0, 0, 0)
        for (x, y), c in head.items():
            px[f * 16 + x, y0 + y + bob] = c
    right = sheet.crop((0, y0, 64, y0 + 32))
    for f in range(4):
        frame = right.crop((f * 16, 0, f * 16 + 16, 32)).transpose(Image.FLIP_LEFT_RIGHT)
        sheet.paste(frame, (f * 16, LEFT_ROW * 32))
    return sheet


def main():
    gen, out = sys.argv[1], sys.argv[2]
    batch, job, n = PICK
    sheet = Image.open(os.path.join(gen, batch, f"{job}_{n}.png")).convert("RGBA")
    if sheet.size != (64, 128):
        raise SystemExit(f"expected a 64x128 sheet, got {sheet.size}")
    sheet.putalpha(sheet.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    sheet = fix_side_heads(quantize(recolour_black(lift_charcoal(sheet)), COLOURS))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()

"""Bram's stone after Tess's 8-heart event (1.4; docs/tess_events.md, approved by the user 2026-11-09):
the graveyard's plain stone (branch claude/vale-guide-grove, art/gilded_vale/props/vale_graveyard_v1/
stone_bram.png and _winter), its name no longer scratched but cut: each pale scratch of B R / A M becomes a
dark groove with a light edge under it, the same letters where Lupini put them; and one small leaf cut above
them, off the Gilded Tree. He asked for a plain stone (his echo in the Grove): this is still plain.

The image model garbled 3-pixel letters, so the cutting is done here, on the stone's own pixels.

Usage: bram_carved_stone.py <stone_bram.png> <stone_bram_winter.png> <out dir>
   writes stone_bram_carved.png and stone_bram_carved_winter.png
"""
import os, sys
from PIL import Image

SCRATCH = (214, 212, 204)          # the scratched letters' colour
GROOVE = (71, 73, 69)              # the stone's own darkest grey
EDGE = (196, 196, 194)             # its own highlight
STONE = {(151, 151, 151), (133, 133, 133), (170, 170, 170), (196, 196, 194), (99, 98, 98)}
LEAF = [(10, 12), (9, 13), (10, 13), (8, 14), (9, 14), (7, 15)]   # a slanted leaf, its stem down-left


def carve(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    letters = [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][:3] == SCRATCH]
    for x, y in letters + [p for p in LEAF if px[p][:3] in STONE]:
        px[x, y] = GROOVE + (255,)
    for x, y in letters + LEAF:
        if y + 1 < im.height and px[x, y + 1][:3] in STONE:
            px[x, y + 1] = EDGE + (255,)
    return im


def main():
    plain, winter, out = sys.argv[1], sys.argv[2], sys.argv[3]
    carve(plain).save(os.path.join(out, "stone_bram_carved.png"))
    carve(winter).save(os.path.join(out, "stone_bram_carved_winter.png"))
    print("wrote", out)


if __name__ == "__main__":
    main()

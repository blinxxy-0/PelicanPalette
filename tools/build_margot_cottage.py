"""Builds Margot's cottage on the town map (1.3): the patch laid over Town
once Robin has built it (data/margot.json: from her 4-heart event).

  spring_MargotCottage.png the cottage and her bench, outlines recoloured
                          (summer, fall and winter copies: seasonal_buildings.py)
                          (vanillafy.selout), on one sheet.
  MargotCottagePatch.tmx  the patch, laid over Town at ORIGIN in Overlay
                          mode: only its own tiles change, so the grass,
                          the road and the riverbank are the map's own, in
                          vanilla and in Stardew Valley Expanded alike.

Where (decided 2026-11-01): between the main road and the river, south of
Haley and Emily's house. The face we see is the back of the house, its big
window to the water; the door is at the right-hand end, reached down the
grass between the house and the cherry tree. The cottage is c_3's gabled
shape drawn at the town's own scale (its door is as big as Haley and
Emily's, and lined up on one tile), PixelLab pick v4c_3. It stands on
rows 95-96 at x 18-22, clear of the road
(the gable's peak reaches the kerb) and of the riverbank. A bench by the
river below the door, and a base-game flowerpot beside it. (A yard for her
frame work, to the left, was tried and dropped, 2026-11-01: the strip by the
river is too narrow for it; its pieces are in her workshop indoors.)

Usage: build_margot_cottage.py art/margot/cottage/picks <content pack assets dir>
  (the picks dir holds the chosen generations, by batch, as PixelLab named them)
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "art"))
from vanillafy import selout
from build_margot_room import write

import build_margot_room
from build_margot_room import LAYERS
# (The cottage also draws on Back3: its corners' slivers of wall, over the
# path. The shared writer writes the layers it's given here.)
LAYERS = LAYERS[:2] + ["Back3"] + LAYERS[2:]
build_margot_room.LAYERS = LAYERS
ORIGIN = (18, 91)                     # the patch's top-left on Town
W, H = 6, 9                           # Town x 18-23, y 91-99
SHEET = "spring_MargotCottage.png"   # (the seasons: tools/art/seasonal_buildings.py)
SHEET_COLS = 8
# (pick, crop box in the pick or None, Town x, y of its top-left, solid bottom rows)
PIECES = [
    # Cropped from x 10 so the arched door (pixels 58-73) is one tile.
    # Solid from the eaves down (2026-11-05, from playtesting: with only the
    # walls solid you could walk in under the roof, and your head showed
    # through it).
    (("stardew-margot-cottage4", "cottage_v4c_3"), (10, 0, 90, 96), (18, 91), 4),
    # Down at the river's edge, turned to face the water (west), a tile
    # right of the door and a row below it (2026-11-05, from playtesting: it
    # faced the viewer; PixelLab, side_bench_candidates). Solid on both its
    # tiles, so it stands one row lower than at first: at (22, 97) it shut
    # off the tile in front of the door (2026-11-06, from playtesting).
    (("stardew-margot-yard", "water_bench_side"), None, (22, 98), 2),
]
TOWN = "spring_town"
# (A base-game flowerpot stood by the door until 2026-11-05: its plant
# overlapped the cottage's corner, and it was taken out, from playtesting.)
POT = []
# A tile of a piece's solid rows is solid only if the art spans at least
# half its width and a quarter of its height: the cottage's edge columns are
# a few pixels of ivy or wall, and its bottom row is the wall's last half-tile.
SOLID_WIDTH, SOLID_HEIGHT = 0.5, 0.25
DOOR = (21, 96)                       # the arched door's tile (Town)


def main():
    picks, assets = sys.argv[1], sys.argv[2]
    images = []
    for (batch, job), box, at, solid in PIECES:
        im = Image.open(os.path.join(picks, batch, job + ".png")).convert("RGBA")
        if box:
            im = im.crop(box)
        images.append((selout(im), at, solid))
    # Pack on the sheet, left to right, each on the 16-pixel grid.
    slots, x, y, row_h = [], 0, 0, 0
    for im, _, _ in images:
        cols, rows = im.width // 16, im.height // 16
        if x + cols > SHEET_COLS:
            x, y, row_h = 0, y + row_h, 0
        slots.append((x, y, cols, rows))
        x += cols
        row_h = max(row_h, rows)
    sheet = Image.new("RGBA", (SHEET_COLS * 16, (y + row_h) * 16), (0, 0, 0, 0))
    for (im, _, _), (sx, sy, _, _) in zip(images, slots):
        sheet.alpha_composite(im, (sx * 16, sy * 16))
    sheet.save(os.path.join(assets, SHEET))

    grid = {l: [[None] * W for _ in range(H)] for l in LAYERS}
    for (im, (tx, ty), solid), (sx, sy, cols, rows) in zip(images, slots):
        for j in range(rows):
            for i in range(cols):
                gx, gy = tx + i - ORIGIN[0], ty + j - ORIGIN[1]
                if not (0 <= gx < W and 0 <= gy < H):
                    continue
                if im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16)).getbbox() is None:
                    continue
                tile = im.crop((i * 16, j * 16, i * 16 + 16, j * 16 + 16))
                tp = tile.load()
                wide = sum(1 for a in range(16) if any(tp[a, b][3] >= 128 for b in range(16))) / 16
                tall = sum(1 for b in range(16) if any(tp[a, b][3] >= 128 for a in range(16))) / 16
                # Solid rows are solid; but a tile of a building's last two
                # rows with only a sliver of wall in it (the cottage's
                # corners) goes on Back3 (over the path's Back2), walkable and
                # drawn under you, so
                # you pass in front of the corner instead of under it.
                thick = wide >= SOLID_WIDTH and tall >= SOLID_HEIGHT
                if j < rows - solid:
                    layer = "Front"
                elif thick or rows <= 2 or j < rows - 2:
                    layer = "Buildings"
                else:
                    layer = "Back3"
                grid[layer][gy][gx] = (SHEET, (sy + j) * SHEET_COLS + sx + i)
    for layer, x, y, t in POT:
        grid[layer][y - ORIGIN[1]][x - ORIGIN[0]] = (TOWN, t)
    tilesets = [("Town", 1, TOWN, 512, 1152),
                ("z_blinxxy.MuseArt_MargotCottage", 1 + 32 * 72, SHEET, sheet.width, sheet.height)]
    out = os.path.join(assets, "MargotCottagePatch.tmx")
    write(out, grid, W, H, {}, tilesets)
    print("wrote", out, SHEET)


if __name__ == "__main__":
    main()

"""The hidden sculpture: a stone gardener swallowed by a wild hedge, in the parterre's
south-west corner pocket (x 6-7, y 44-45). Fern's teacher kept every hedge square for
seventy years; since he died she lets the garden grow, and this hedge has grown right
over the figure the old artists carved for him. Only its head shows.
(docs/superpowers/specs/2026-11-08-vale-falls-overlook-sculpture-design.md, section 3)

The art is the garden's own pixels, read from the committed sheets with the layers of
each tile stacked (as vale_feature_fixes._patch reads them):
- the head: the top 16 px of the west statue, lifted off the gravel and grass round it;
- the hedge: the leaf pixels of the lower west bed's bushes (no path, soil or shadow),
  each bush's capped top lobe and its foot (BUSHES), behind the head and over its
  shoulders, filling the pocket with a rounded top and lumpy free side.
Spring's art goes in the spring, summer and fall sheets; winter's is the same cut from
the winter sheet, so the snow lies on it as on the bed beside it. Only the feet (row 45)
are solid; the head and the hedge's top (row 44) are on Front, and you stand behind
them, at (7,44), to read the plinth. The grass under it all stays on Back2.

Run as: python tools/vale_sculpture.py <GildedVale.tmx> <exports>   (build_vale.VALE_PIECES)
Re-runnable: the pocket's own slots, once added at the sheets' end, are painted over.
"""
import os, re, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from vale_patch import load, save
from vale_feature_fixes import FEATURE_FIRST, GROUND, SEASONS, lift, snow

STATUE = (6, 41, 7, 43)         # the west statue on its plinth
HEDGE = (8, 43, 11, 45)         # the lower west hedged bed, with its bushes' tops in row 43
POCKET = (6, 44, 7, 45)         # the corner pocket: the head's row, then the feet's (in BOXES["sculpture"])
PARTERRE_GROUND = GROUND["parterre"]        # what's ground round the statue: gravel, grass
ACTION = "Message blinxxy.MuseArt_Vale.garden.hidden_statue"
STACK = ("Back2", "Buildings", "Front")     # the feature's layers, bottom up
# The bed's bushes are 42 px tall (rows 3-44 of HEDGE's art): a lobe with a rounded,
# snow-capped top (rows 3-17), a second capped lobe below it, then the leafy foot. The
# pocket has 32. Cut at row 44 (as first done) the hedge's top was the upper lobe sliced
# through: a flat dark line in winter. So each bush keeps its whole top lobe and drops its
# middle (the second cap): top lobe, then the foot. And the bed's bushes are trimmed, with
# straight sides: the east bush's side ran straight down the pocket's east edge for 15 px.
# So the east foot is the west bush's foot mirrored (its rounded corner to the east) and
# stepped in under the lobe: the wild hedge's free side goes in lumps. Cuts, (x0, y0, x1,
# y1) of HEDGE's art, where their top-left goes in the pocket's 32 x 32 px, and whether
# mirrored, back to front:
BUSHES = [((3, 32, 24, 45), (9, 19), True),     # east: the west bush's foot, mirrored, stepped in
          ((40, 0, 60, 18), (13, 2), False),    # the east bush's top lobe, behind the head's right
          ((3, 0, 24, 18), (-3, 0), False),     # west: the west bush's top lobe, against the wall
          ((3, 32, 24, 45), (-3, 18), False)]   # its foot
HEAD_AT = (9, 1)    # the head, between the bushes, over x 7 (as in the approved mockup)
SHOULDERS = 10      # px of the head's cut from which the front leaves cover it


def own(v):
    return FEATURE_FIRST <= v < FEATURE_FIRST + 10000


def leaf(c):
    """A bush's leaves in the parterre's art: dark greens and teals. Not the gravel
    (orange), the soil and shadow under the bushes (brown, olive), nor the flowers."""
    r, g, b, a = c
    return a and r < 70 and g > r + 10


class Sheets:
    def __init__(self, assets):
        self.assets = assets
        self.im = {se: Image.open(self.path(se)).convert("RGBA") for se in SEASONS}
        self.cols = self.im["spring"].width // 16
        self.end = self.im["spring"].height // 16 * self.cols      # the first free slot

    def path(self, se):
        return os.path.join(self.assets, se + "_GildedValeBuildings.png")

    def box(self, v):
        i = v - FEATURE_FIRST
        x, y = i % self.cols * 16, i // self.cols * 16
        return x, y, x + 16, y + 16

    def region(self, m, se, x0, y0, x1, y1):
        """Map tiles' feature art (px), each tile's layers stacked."""
        im = Image.new("RGBA", ((x1 - x0 + 1) * 16, (y1 - y0 + 1) * 16))
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                for k in STACK:
                    if own(m.g[k][y][x]):
                        im.alpha_composite(self.im[se].crop(self.box(m.g[k][y][x])), ((x - x0) * 16, (y - y0) * 16))
        return im

    def grow(self, slots):
        """Room on every sheet for slots up to `slots`."""
        h = (slots + self.cols - 1) // self.cols * 16
        for se in SEASONS:
            if self.im[se].height < h:
                big = Image.new("RGBA", (self.im[se].width, h))
                big.paste(self.im[se], (0, 0))
                self.im[se] = big


def leaves(spring, art, season):
    """The bed's leaf pixels (from the spring art's colours), in a season's art. In
    winter a pixel beside them counts too if it isn't snow: the snow caps' outline sits a
    pixel off the spring bush (as in vale_feature_fixes.front_mask)."""
    W, H = spring.size
    sp, ar = spring.load(), art.load()
    keep = {(x, y) for y in range(H) for x in range(W) if leaf(sp[x, y])}
    if season == "winter":
        near = {(x + dx, y + dy) for x, y in keep for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
        keep |= {(x, y) for x, y in near if 0 <= x < W and 0 <= y < H and ar[x, y][3] and not snow(ar[x, y])}
    out = Image.new("RGBA", art.size)
    op = out.load()
    for x, y in keep:
        op[x, y] = ar[x, y]
    return out


def compose(m, sheets, se):
    """The pocket's art (32 x 32 px) for a season: the bushes, the head in front of them,
    and the bushes' leaves again over the head's shoulders."""
    src = "winter" if se == "winter" else "spring"
    statue_spring = sheets.region(m, "spring", *STATUE)
    top = lift(statue_spring, statue_spring, "spring", PARTERRE_GROUND)[0].getbbox()[1]
    statue = lift(statue_spring, sheets.region(m, src, *STATUE), src, PARTERRE_GROUND)[0]
    head = statue.crop((0, top, statue.width, top + 16))
    bed = leaves(sheets.region(m, "spring", *HEDGE), sheets.region(m, src, *HEDGE), src)
    hedge = Image.new("RGBA", (32, 32))
    for crop, at, mirror in BUSHES:
        cut = bed.crop(crop)
        over(hedge, cut.transpose(Image.FLIP_LEFT_RIGHT) if mirror else cut, at)
    art = hedge.copy()
    over(art, head, HEAD_AT)
    cut = HEAD_AT[1] + SHOULDERS
    over(art, hedge.crop((0, cut, 32, 32)), (0, cut))
    return art


def over(dst, im, at):
    """im laid over dst at (x, y), which may hang off its edges."""
    layer = Image.new("RGBA", dst.size)
    layer.paste(im, at)
    dst.alpha_composite(layer)


def patch_file(tmx_path, exports):
    assets = os.path.dirname(os.path.abspath(tmx_path))
    m = load(tmx_path)
    sheets = Sheets(assets)
    x0, y0, x1, y1 = POCKET
    arts = {se: compose(m, sheets, se) for se in SEASONS}

    # the head's row on Front, the feet's on Buildings: a slot already there is reused only
    # if its pixels are what this would paint in every season (so a slot another piece, or
    # an older version of this one, put there is never painted over); else a new slot at
    # the sheets' end
    nxt = sheets.end
    cells, claimed = [], set()
    for y, layer in ((y0, "Front"), (y1, "Buildings")):
        for x in range(x0, x1 + 1):
            v = m.g[layer][y][x]
            px = ((x - x0) * 16, (y - y0) * 16)
            want = {se: arts[se].crop(px + (px[0] + 16, px[1] + 16)).tobytes() for se in SEASONS}
            mine = own(v) and v not in claimed and v - FEATURE_FIRST < sheets.end and                 all(sheets.im[se].crop(sheets.box(v)).tobytes() == want[se] for se in SEASONS)
            if not mine:
                assert v == 0 or own(v), (layer, x, y, v)
                m.g[layer][y][x] = FEATURE_FIRST + nxt
                nxt += 1
            claimed.add(m.g[layer][y][x])
            cells.append((x, y, layer))
    sheets.grow(nxt)
    for x, y, layer in cells:
        px = ((x - x0) * 16, (y - y0) * 16)
        for se in SEASONS:
            sheets.im[se].paste(arts[se].crop(px + (px[0] + 16, px[1] + 16)), sheets.box(m.g[layer][y][x])[:2])
    for x in range(x0, x1 + 1):
        m.props[("Buildings", x, y1)] = {"Action": ACTION}

    for se in SEASONS:
        sheets.im[se].save(sheets.path(se))
    h = sheets.im["spring"].height
    m.text = re_tileset(m.text, h // 16 * sheets.cols, h)
    save(m, tmx_path)
    print("hidden sculpture: wrote", tmx_path)


def re_tileset(text, tilecount, height):
    """The feature tileset's tilecount and image height, for the sheets as saved."""
    text = re.sub(r'(<tileset firstgid="20000" [^>]*tilecount=")\d+(")', lambda mm: mm.group(1) + str(tilecount) + mm.group(2), text)
    return re.sub(r'(<image source="spring_GildedValeBuildings.png" width="\d+" height=")\d+(")', lambda mm: mm.group(1) + str(height) + mm.group(2), text)


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2])

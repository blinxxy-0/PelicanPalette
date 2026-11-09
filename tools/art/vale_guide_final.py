"""The painters' guide's chosen art, made final (the user's picks, 2026-11-09, from
art/gilded_vale/props/vale_guide_v1/candidates.md): A1, B1, C1, U1, W2, E1/E3/E5/E7, F2, H1, R2,
V2, G2, P1 (FF2, the Founders' Frame, is drawn by tools/art/frame_styles.py into the main mod's sheets).

    python tools/art/vale_guide_final.py

Reads the picked candidates (vale_guide_v1/, made by vale_guide_candidates.py) and writes:
  - art/gilded_vale/props/vale_graveyard_v1/: the graveyard's outdoor pieces, each <name>.png and its
    <name>_winter.png (snow lying on it, made here), for build_vale.py's PICKS and SEASONAL (Task 6
    places them): headstone_<agnes|hollis|mabel> (1x2), stone_bram (1x2), marker_<ada|hester|amos>
    (1x2, Tobin's T on Hester's and Amos's foot), urn_<who> (1x1, seven glazes), wall_left / wall_mid /
    wall_right (1x1, an east-west run), wall_side (1x1, a north-south run), gatepost (1x2) and gate
    (2x1); and preview.png (every piece, spring over winter, at 3x).
  - art/gilded_vale/props/vale_guide_final/: the Grove's pieces (tools/build_vale_grove.py lays them):
    echo_<id>_0.png and echo_<id>_1.png (32x48, the idle's two frames: the tool moving a little),
    hidden_easel, founders_easel (empty), founders_canvas (the same easel holding the finished golden
    canvas: only its top tile differs), return_frame (16x32), panorama (22x7 tiles); and the icons
    guide_icon and painted_fish (16x16).
  - The icons into the dev pack: "[CP] Gilded Vale (dev)/assets/PaintersGuide.png" and PaintedFish.png.
"""
import os
import shutil
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
PROPS = os.path.join(ROOT, "art", "gilded_vale", "props")
PICKS = os.path.join(PROPS, "vale_guide_v1")
GRAVES = os.path.join(PROPS, "vale_graveyard_v1")
FINAL = os.path.join(PROPS, "vale_guide_final")
PACK_ASSETS = os.path.join(ROOT, "[CP] Gilded Vale (dev)", "assets")

URNS = ["agnes", "hollis", "mabel", "bram", "ada", "hester", "amos"]


def pick(name):
    return Image.open(os.path.join(PICKS, name + ".png")).convert("RGBA")


# ---- snow ------------------------------------------------------------------------------------
# The game's own snow on its graveyard's stones (winter_town, the tombstones): a white cap with a
# sky-blue rim, and a deep blue line of shadow on the stone under it.

WHITE, BLUE, DEEP = (249, 251, 251, 255), (81, 170, 255, 255), (49, 90, 179, 255)
PALE = (200, 226, 250, 255)


def lum(c):
    return 0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2]


def green(c):
    return c[3] and c[1] > c[0] + 8 and c[1] > c[2] + 8


def no_green(im, foot_rows):
    """Winter has no grass: tufts and moss at the foot become a little drifted snow. Greens higher up
    (Amos's hedge-green glaze, Ada's celadon) are the piece's own colours and stay."""
    px = im.load()
    W, H = im.size
    box = im.getbbox()
    if not box:
        return im
    bottom = box[3]
    for y in range(H):
        for x in range(W):
            c = px[x, y]
            if not green(c):
                continue
            if y >= bottom - foot_rows:
                above = px[x, y - 1] if y else (0, 0, 0, 0)
                px[x, y] = WHITE if not above[3] or green(above) else PALE
    return im


def snow_cap(im, depth=4, x_range=None):
    """Snow on a piece's top: each column's top `depth` pixels (only through the solid shape), a blue
    rim on top and at the sides, white inside, a blue lower edge, and the deep blue shadow under it."""
    src = im.copy()
    s = src.load()
    px = im.load()
    W, H = im.size
    solid = lambda x, y: 0 <= x < W and 0 <= y < H and s[x, y][3] == 255
    cap = {}
    for x in (x_range or range(W)):
        ys = [y for y in range(H) if solid(x, y)]
        if not ys:
            continue
        t = ys[0]
        for k in range(depth):
            if not solid(x, t + k):
                break
            cap[(x, t + k)] = k
    for (x, y), k in cap.items():
        side = not solid(x - 1, y) or not solid(x + 1, y)
        if k == 0:
            px[x, y] = BLUE
        elif side:
            px[x, y] = DEEP if k >= 2 else BLUE
        elif k == depth - 1 or (x, y + 1) not in cap:
            px[x, y] = BLUE
        else:
            px[x, y] = WHITE
    for (x, y), k in cap.items():
        if (x, y + 1) not in cap and solid(x, y + 1) and k == depth - 1:
            px[x, y + 1] = DEEP
    return im


def snow_ledges(im, max_y):
    """A line of snow resting on every ledge above max_y: each solid pixel open above gets a white
    pixel over it (thin bars and rails, as on the iron gate)."""
    s = im.copy().load()
    px = im.load()
    W, H = im.size
    for y in range(1, min(max_y, H)):
        for x in range(W):
            if s[x, y][3] == 255 and not s[x, y - 1][3]:
                px[x, y - 1] = WHITE
    return im


def snow_top_face(im, rows=None, cols=None):
    """Snow over a wall's top face (the capstones seen from above): its colours mapped by lightness
    onto the snow's, so the joints between the stones still show as faint blue lines."""
    px = im.load()
    W, H = im.size
    for y in (rows or range(H)):
        for x in (cols or range(W)):
            c = px[x, y]
            if c[3] != 255:
                continue
            if c[0] > c[2] + 60 and c[1] < 140 and lum(c) < 110:       # the end posts' dead vines stay
                continue
            l = lum(c)
            px[x, y] = WHITE if l >= 130 else (PALE if l >= 100 else BLUE)
    return im


def winter(name, im):
    w = im.copy()
    if name.startswith(("headstone", "stone")):
        no_green(w, 5)
        snow_cap(w, 4)
    elif name.startswith("marker"):
        no_green(w, 3)
        snow_cap(w, 4)
    elif name.startswith("urn"):
        snow_cap(w, 2)
    elif name == "gatepost":
        snow_cap(w, 3)
    elif name == "gate":
        snow_ledges(w, 8)
    elif name == "wall_side":
        snow_top_face(w, cols=range(4, 12))
    elif name.startswith("wall"):
        no_green(w, 0)
        snow_top_face(w, rows=range(0, 6))        # (row 6, the capstones' lit front lip, stays)
    return w


def to_ground(im):
    """Moved down so its base sits on the tile's bottom row, like the other stones (the PixelLab
    headstones stood three pixels up)."""
    box = im.getbbox()
    out = Image.new("RGBA", im.size)
    out.paste(im, (0, im.height - box[3]))
    return out


def graveyard():
    os.makedirs(GRAVES, exist_ok=True)
    pieces = {}
    for who in ("agnes", "hollis", "mabel"):
        pieces[f"headstone_{who}"] = to_ground(pick(f"A_A1_{who}"))
    pieces["stone_bram"] = pick("B1_bram")
    for who in ("ada", "hester", "amos"):
        pieces[f"marker_{who}"] = pick(f"C1_{who}")
    for who in URNS:
        pieces[f"urn_{who}"] = pick(f"U1_{who}")
    for part in ("wall_left", "wall_mid", "wall_right", "wall_side", "gatepost", "gate"):
        pieces[part] = pick(f"W2_{part}")
    both = []
    for name, im in pieces.items():
        w = winter(name, im)
        im.save(os.path.join(GRAVES, name + ".png"))
        w.save(os.path.join(GRAVES, name + "_winter.png"))
        both.append((im, w))
    preview(both, os.path.join(GRAVES, "preview.png"))
    return pieces


def preview(pairs, path, scale=3):
    """Every piece, spring above winter, on the game's spring grass and its winter snow."""
    gap = 4
    width = sum(a.width + gap for a, _ in pairs) + gap
    h = max(a.height for a, _ in pairs)
    out = Image.new("RGBA", (width, 2 * h + 3 * gap), (104, 148, 72, 255))
    out.paste((232, 240, 248, 255), (0, h + gap + gap // 2, width, out.height))
    x = gap
    for a, b in pairs:
        out.alpha_composite(a, (x, gap + h - a.height))
        out.alpha_composite(b, (x, 2 * gap + 2 * h - b.height))
        x += a.width + gap
    out.resize((out.width * scale, out.height * scale), Image.NEAREST).save(path)


# ---- the echoes' idle: the tool moving a little --------------------------------------------------
# Each echo's working hand and tool, as a box in its 32x48 picture, and which way it moves for the
# second frame (one pixel). The box's far edge stays where it was, so the arm stretches rather than
# coming apart from the body.

IDLE = {
    "agnes": ("E1_echo_agnes_ink", (15, 14, 19, 19), (0, -1)),   # the brush, up the canvas
    "hollis": ("E3_echo_hollis_ink", (18, 20, 21, 23), (1, 0)),  # the gouge, pushed along the block
    "mabel": ("E5_echo_mabel_ink", (18, 6, 22, 13), (0, -1)),    # the blue glass, lifted to the light
    "bram": ("E7_echo_bram_ink", (4, 6, 10, 14), (0, -1)),       # the mallet, raised for the next blow
}


def idle_frame(im, box, move):
    x0, y0, x1, y1 = box
    dx, dy = move
    src = im.load()
    out = im.copy()
    px = out.load()
    if dy:
        # rows shift up (dy -1): each row takes the one below it; the box's last row stays
        for x in range(x0, x1 + 1):
            for y in range(y0 - 1, y1):
                px[x, y] = src[x, y + 1]
    else:
        # columns shift right (dx 1): each column takes the one to its left; the first stays
        for y in range(y0, y1 + 1):
            for x in range(x1 + 1, x0, -1):
                px[x, y] = src[x - 1, y]
    return out


def grove():
    os.makedirs(FINAL, exist_ok=True)
    for who, (src, box, move) in IDLE.items():
        im = pick(src)
        im.save(os.path.join(FINAL, f"echo_{who}_0.png"))
        idle_frame(im, box, move).save(os.path.join(FINAL, f"echo_{who}_1.png"))
    for name, src in (("hidden_easel", "H1_hidden_easel"), ("founders_easel", "F2_founders_empty"),
                      ("founders_canvas", "F2_founders_painting"), ("return_frame", "R2_return_frame"),
                      ("panorama", "V2_panorama"), ("guide_icon", "G2_guide_icon"),
                      ("painted_fish", "P1_painted_fish")):
        pick(src).save(os.path.join(FINAL, name + ".png"))
    shutil.copyfile(os.path.join(FINAL, "guide_icon.png"), os.path.join(PACK_ASSETS, "PaintersGuide.png"))
    shutil.copyfile(os.path.join(FINAL, "painted_fish.png"), os.path.join(PACK_ASSETS, "PaintedFish.png"))


def main():
    graveyard()
    grove()
    print("wrote", GRAVES, "and", FINAL)


if __name__ == "__main__":
    main()

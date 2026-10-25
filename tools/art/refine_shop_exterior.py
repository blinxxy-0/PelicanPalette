"""Refines the art shop's exterior towards the game's own buildings
(2026-10-25, after the user compared it with the farmhouse, shed and coop):

  - Shingles: each roof face is redrawn in rows of rounded scallop
    shingles at the game's proportions (the shed's are 10-11 px on 7 px
    rows, and ours match): a U-shaped dark arc
    under each, darker where the row above overlaps it, lighter towards its
    lower left, offset row to row. Each face keeps its own light: the
    gable's left slope lit, its right slope in shade, the wing between.
  - Eaves: a lit rim over a dark underside along the bottom of each face,
    and the roof's outer corners rounded off rather than sharp.
  - Outlines: the render's near-black lines become a darker shade of the
    colour they border, as the game's art does (a red roof edged in deep
    red, planks in dark brown), so the building stops looking cut out.
  - Walls: a shadow band under the eaves and a faint plaster grain.
  - Door and windows, redrawn in detail: a bevelled frame, a four-pane
    door light with glints, two recessed panels, a brass knob; glass in the
    gable window; reflections, a shadowed head and a lit sill on the display
    window.

It starts from the PixelLab render each time (through install_shop_exterior),
so it can be re-run; positions below are on the installed 112x112 sheet.

Usage: refine_shop_exterior.py <render.png> <out tilesheet.png>
"""
import colorsys
import os
import sys
import tempfile
from collections import Counter, deque
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from install_shop_exterior import install

# The roof's teal, highlight to deepest; a face's shingle body is one step.
TEAL = [(132, 206, 186), (96, 178, 164), (66, 148, 146), (46, 120, 128), (32, 94, 110), (22, 70, 90), (14, 50, 70), (8, 32, 48)]
FACE_BASE = {"lit": 1, "wing": 2, "shade": 4}
DEEPEST = len(TEAL) - 1
SHINGLE_W, SHINGLE_ROW = 11, 7
# The roof's faces as the user coloured them (2026-10-25), mapped onto the sheet.
FACE_MASK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "art", "shop_exterior", "roof_faces.png")
MASK_KINDS = {(176, 226, 32): "lit", (160, 80, 160): "shade", (250, 130, 30): "wing", (240, 30, 30): "wing"}
PLASTER_LIGHT, PLASTER, PLASTER_SHADE = (234, 224, 192), (226, 214, 180), (196, 180, 146)

# --- The door, drawn by hand (x 78-94, y 77-101 on the sheet) -----------
DOOR_AT = (78, 77)
DOOR_KEYS = {
    "o": (70, 34, 28), "a": (178, 126, 84), "b": (146, 100, 64), "c": (106, 68, 46),
    "G": (26, 56, 38), "S": (44, 88, 52), "M": (64, 118, 62), "L": (92, 150, 76),
    "Q": (236, 248, 242), "W": (156, 210, 216), "X": (102, 164, 180), "Y": (64, 112, 134),
    "K": (255, 216, 112), "k": (188, 132, 48),
}
DOOR = [
    "ooooooooooooooooo",
    "oaaaaaaaaaaaaaaco",
    "oabbbbbbbbbbbbbco",
    "oabGGGGGGGGGGGcco",
    "oabGSSSSSSSSSGcco",
    "oabGLGGGGGGGSGcco",
    "oabGLGQWGQWGSGcco",
    "oabGLGWXGWXGSGcco",
    "oabGLGXYGXYGSGcco",
    "oabGLGGGGGGGSGcco",
    "oabGLGQWGQWGSGcco",
    "oabGLGXYGXYGSGcco",
    "oabGLGGGGGGGSGcco",
    "oabGLLLLLLLMSGcco",
    "oabGLMMMMMMKSGcco",
    "oabGLMMMMMMkSGcco",
    "oabGLSSSMSSSSGcco",
    "oabGLSMLMSMLSGcco",
    "oabGLSMLMSMLSGcco",
    "oabGLSMLMSMLSGcco",
    "oabGLSMLMSMLSGcco",
    "oabGLLLLMLLLSGcco",
    "oabGLMMMMMMMSGcco",
    "oabGSSSSSSSSSGcco",
    "oabGGGGGGGGGGGcco",
]

# --- The gable window's glass (x 45-51, y 57-64) -------------------------
ATTIC_AT = (45, 57)
ATTIC_KEYS = {"d": (40, 26, 22), "Q": (232, 244, 238), "W": (140, 196, 206), "X": (92, 150, 170), "Y": (58, 100, 124), "J": (135, 95, 66), "I": (120, 84, 56)}
ATTIC = [
    "ddddddd",
    "QWXJQWX",
    "WXXJWXX",
    "XYYJXYY",
    "IIIJIII",
    "QWXIQWX",
    "WXYIWXY",
    "XYYIXYY",
]

# The display window: its glass (the render's navy) and its sill row.
DISPLAY = (31, 79, 69, 97)          # x0, y0, x1, y1
NAVY = (38, 56, 84)
SILL_Y = 99


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def is_teal(c):
    h, s, v = colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
    return 0.42 < h < 0.56 and s > 0.35 and v > 0.18


def is_plaster(c):
    return c[0] > 170 and c[1] > 155 and c[2] > 120 and c[0] >= c[2]


def darker(c, k=0.5, purple=10):
    """A deeper shade: darker, a touch more saturated, nudged towards purple."""
    h, s, v = colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)
    r, g, b = colorsys.hsv_to_rgb(h, min(1, s * 1.15 + 0.05), v * k)
    return (min(255, int(r * 255) + purple // 2), int(g * 255), min(255, int(b * 255) + purple))


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def neighbours(x, y, w, h, ring=1):
    for dy in range(-ring, ring + 1):
        for dx in range(-ring, ring + 1):
            if (dx or dy) and 0 <= x + dx < w and 0 <= y + dy < h:
                yield x + dx, y + dy


def components(mask):
    seen, comps = set(), []
    for start in mask:
        if start in seen:
            continue
        comp, queue = [], deque([start])
        seen.add(start)
        while queue:
            x, y = queue.popleft()
            comp.append((x, y))
            for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if n in mask and n not in seen:
                    seen.add(n)
                    queue.append(n)
        comps.append(comp)
    return comps


def shingle_step(base, x, y, top):
    """Which ramp step a roof pixel takes, for scallop shingles in rows."""
    row, v = divmod(y - top, SHINGLE_ROW)
    u = (x + (row % 2) * (SHINGLE_W // 2)) % SHINGLE_W
    c = (SHINGLE_W - 1) / 2
    t = abs(u - c) / c
    # Its rounded bottom: nearly a half circle, deepest in the middle and
    # rising almost to the row's top at the sides, so each shingle stands
    # apart and the row below shows between them.
    arc = SHINGLE_ROW - 1 - round((SHINGLE_ROW - 2) * t ** 2.2)
    n = speck(x, y)
    if v > arc:
        # The row below, showing between two shingles: dark, flecked.
        return base + 2 + (1 if n < 0.18 else 0)
    if v == arc:
        return base + 3                            # the dark arc itself
    if v == 0:
        return base + 1                            # in the shadow of the row above
    if v == 1 and (x + y) % 2 == 0:
        return base + 1                            # that shadow dithering into the shingle
    if v == arc - 1 and u <= c:
        return base - 1 if n > 0.12 else base     # lit lower left, worn here and there
    if v == arc - 2 and u == 2 and (x * 3 + row * 5) % 4 == 0:
        return base - 2                            # an odd glint
    # The body's grain, as on the game's roofs: dark flecks mostly in its
    # upper half, light ones lower down, never two side by side.
    upper = v <= arc / 2
    if n < (0.22 if upper else 0.1) and speck(x - 1, y) >= 0.22:
        return base + 1
    if n > (0.93 if upper else 0.82) and speck(x - 1, y) <= 0.82:
        return base - 1
    return base


def speck(x, y):
    """A fixed pseudo-random number in [0, 1) for a pixel, so the grain is
    the same every time the art is rebuilt."""
    h = (x * 374761393 + y * 668265263) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return (h ^ (h >> 16)) / 2 ** 32


def stamp(px, at, rows, keys):
    x0, y0 = at
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            px[x0 + i, y0 + j] = keys[ch] + (255,)


def refine(im):
    w, h = im.size
    px = im.load()
    src = im.copy().load()
    opaque = {(x, y) for x in range(w) for y in range(h) if src[x, y][3]}

    # --- Door and windows first, so nothing below repaints them ----------
    stamp(px, DOOR_AT, DOOR, DOOR_KEYS)
    stamp(px, ATTIC_AT, ATTIC, ATTIC_KEYS)
    drawn = {(DOOR_AT[0] + i, DOOR_AT[1] + j) for j in range(len(DOOR)) for i in range(len(DOOR[0]))}
    drawn |= {(ATTIC_AT[0] + i, ATTIC_AT[1] + j) for j in range(len(ATTIC)) for i in range(len(ATTIC[0]))}
    x0, y0, x1, y1 = DISPLAY
    glass = {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1) if src[x, y][:3] == NAVY}
    for x, y in glass:
        streak = (x - y) % 13
        if streak in (0, 1):
            colour = mix(NAVY, (196, 220, 236), 0.45 if streak == 0 else 0.3)       # reflections
        elif (x, y - 1) not in glass and y - 1 >= y0 - 1:
            colour = darker(NAVY, 0.7, 0)                                          # under the window head
        else:
            colour = mix(NAVY, (70, 96, 130), (y1 - y) / max(1, y1 - y0) * 0.5)   # lighter towards the top
        px[x, y] = colour + (255,)
    for x in range(x0 - 3, x1 + 4):
        if src[x, SILL_Y][3] and lum(src[x, SILL_Y]) > 60:
            px[x, SILL_Y] = (186, 136, 92, 255)         # the sill's lit top
    drawn |= glass

    # --- Roof faces, from the user's colouring (art/shop_exterior/roof_faces.png):
    # the high gable's lit left slope and shaded right slope, and the wing's
    # front slope, which runs into the middle of the shaded slope (its ridge
    # level along the top, a matching slope out of sight behind).
    roof = {p for p in opaque if is_teal(src[p])} - drawn
    mask = Image.open(FACE_MASK).convert("RGBA").load()
    faces_by_kind = {kind: set() for kind in FACE_BASE}
    loose = set()
    for p in roof:
        kind = MASK_KINDS.get(mask[p][:3]) if mask[p][3] else None
        (faces_by_kind[kind] if kind else loose).add(p)
    for p in sorted(loose):            # a pixel the colouring missed joins its neighbours' face
        near = Counter(k for k, f in faces_by_kind.items() for n in neighbours(*p, w, h, 2) if n in f)
        if near:
            faces_by_kind[near.most_common(1)[0][0]].add(p)
    faces = [(k, f) for k, f in faces_by_kind.items() if f]
    roof = set().union(*(f for _, f in faces))

    for kind, face in faces:
        base = FACE_BASE[kind]
        top = min(y for _, y in face)
        for x, y in face:
            px[x, y] = TEAL[max(0, min(DEEPEST, shingle_step(base, x, y, top)))] + (255,)
        # The eave: the bottom row of shingles hangs over the wall as
        # scallops, so between them the wall (in the roof's shadow) or the
        # sky shows; along the scallops' edge, the dark arc.
        lowest = {}
        for x, y in face:
            lowest[x] = max(lowest.get(x, y), y)
        for x, yb in lowest.items():
            if (x, yb + 1) in roof:
                continue                              # this column's roof carries on below
            under = src[x, yb + 1] if yb + 1 < h else (0, 0, 0, 0)
            for y in range(yb - 2, yb + 1):
                if (x, y) not in face:
                    continue
                row, v = divmod(y - top, SHINGLE_ROW)
                u = (x + (row % 2) * (SHINGLE_W // 2)) % SHINGLE_W
                c = (SHINGLE_W - 1) / 2
                arc = SHINGLE_ROW - 1 - round((SHINGLE_ROW - 2) * (abs(u - c) / c) ** 2.2)
                gap = v > arc and y > yb - 2
                if gap:
                    px[x, y] = (darker(under[:3], 0.62, 0) + (255,)) if under[3] else (0, 0, 0, 0)
                elif y == yb:
                    px[x, y] = TEAL[DEEPEST] + (255,)
                elif y == yb - 1 and (x, y) in face:
                    px[x, y] = TEAL[max(0, base - 2)] + (255,)      # the eave's lit rim

    # --- The roof's shape, at the joins between faces:
    #   the high ridge (lit slope | shaded slope) is convex: a lit line on
    #     the lit side, a dark one on the shaded side;
    #   where the wing meets the shaded slope is a valley, a crease going
    #     in: dark on both sides, the wing's edge casting a little shadow;
    #   the wing's own ridge, level along its top, catches the light.
    face_of = {p: k for k, face in faces for p in face}
    for (x, y), kind in face_of.items():
        others = {face_of[n] for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)) if n in face_of and face_of[n] != kind}
        if not others:
            continue
        if {kind} | others == {"lit", "shade"}:
            px[x, y] = (TEAL[0] if kind == "lit" else TEAL[DEEPEST]) + (255,)
        else:
            px[x, y] = TEAL[DEEPEST if kind == "wing" else DEEPEST - 1] + (255,)
    wing = dict(faces).get("wing", set())
    ridge_y = min((y for _, y in wing), default=None)
    for x, y in wing:
        if y == ridge_y or (y == ridge_y + 1 and (x, y - 1) not in wing):
            if (x, y - 1) not in roof and px[x, y][:3] != TEAL[DEEPEST]:
                px[x, y] = TEAL[0] + (255,)                 # the ridge cap
                if (x, y + 1) in wing:
                    px[x, y + 1] = TEAL[1] + (255,)

    # --- Outlines: a darker shade of what they border --------------------
    dark = {p for p in opaque if lum(src[p]) < 34 and p not in roof and p not in drawn}
    for x, y in dark:
        near = [px[n][:3] for n in neighbours(x, y, w, h) if n in opaque and n not in dark]
        if not near:
            near = [px[n][:3] for n in neighbours(x, y, w, h, 2) if n in opaque and n not in dark]
        if not near:
            continue
        colour = Counter(near).most_common(1)[0][0]
        edge = any(n not in opaque for n in neighbours(x, y, w, h))
        px[x, y] = darker(colour, 0.38 if edge else 0.5) + (255,)
    for p in roof:
        if any(n not in opaque for n in neighbours(*p, w, h)):
            px[p] = TEAL[DEEPEST] + (255,)

    # --- Round the roof's corners (twice: a two-pixel curve) --------------
    for _ in range(2):
        for x, y in sorted(roof):
            if px[x, y][3] and sum(1 for n in neighbours(x, y, w, h) if not px[n][3]) >= 5:
                px[x, y] = (0, 0, 0, 0)

    # --- Walls: shadow under the eaves, plaster grain ---------------------
    plaster = {p for p in opaque if is_plaster(src[p]) and p not in roof and p not in drawn}
    for x, y in plaster:
        depth = next((d for d in (1, 2, 3) if (x, y - d) in roof or ((x, y - d) in opaque and lum(src[x, y - d]) < 60)), None)
        if depth == 1:
            px[x, y] = darker(PLASTER, 0.72, 0) + (255,)
        elif depth in (2, 3):
            px[x, y] = PLASTER_SHADE + (255,)
        elif (x * 7 + y * 3) % 13 == 0:
            px[x, y] = PLASTER_LIGHT + (255,)
        elif (x * 5 + y * 11) % 17 == 0:
            px[x, y] = PLASTER_SHADE + (255,)
    return im


def main():
    render, out = sys.argv[1], sys.argv[2]
    with tempfile.TemporaryDirectory() as tmp:
        sheet = os.path.join(tmp, "sheet.png")
        install(render, sheet)
        refine(Image.open(sheet).convert("RGBA")).save(out)


if __name__ == "__main__":
    main()

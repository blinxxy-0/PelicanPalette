"""The Gilded Vale's dressing (the user's decor plan, approved 2026-11-06: the map
"looks really empty"). Zones are the plan's letters (art/gilded_vale/decor_plan.png).

Two kinds of piece:
- The game's own props, copied tile for tile from Town (Town numbering, which the
  Vale's map shares): lamp posts, benches, the park fountain, the picnic table,
  planted beds, crates.
- Paths-layer markers, which the game turns into the real thing when the map loads
  (GameLocation.loadPathsLayerObjectsInArea, read 2026-11-06): 9 oak, 10 maple,
  11 pine, 22 tall grass, 24/25/26 large/medium/small bush, and 34 with a SpawnTree
  tile property ("fruit <id>") for the orchard's fruit trees.

Every piece checks its footprint first: markers go only on plain grass with nothing
on the tile; props only where no building, lane or other prop already is. Anything
skipped is printed, so a move in the layout shows up here instead of overlapping.
"""
import random

# Ground the decor pass will use, kept free by props placed before it (build_vale.py
# reachable_spot): fenced yards and the orchard's trees, (x0, y0, x1, y1) inclusive.
# The last two: the atelier's bench, and the overlook (vale_patch.OVERLOOK_BOX: the bluff
# tools/vale_overlook.py raises there, with the lake's painting spot on it).
RESERVED = [(82, 13, 90, 22), (9, 55, 14, 60), (87, 2, 94, 10), (84, 25, 88, 26), (80, 53, 95, 75)]

# Tile property objects for the writer: (layer, x, y) -> {name: value}
TILE_PROPS = {}

# name: [(dx, dy, layer, gid)], anchored at the piece's top-left tile.
PROPS = {
    # a lamp post: three tiles of post on Front, its base solid (Town, by the square)
    "lamp": [(0, 0, "Front", 2947), (0, 1, "Front", 2979), (0, 2, "Front", 3011), (0, 3, "Buildings", 3043)],
    # the park's bench facing the viewer: its back on Front, its seat solid
    "bench": [(0, 0, "Front", 2330), (1, 0, "Front", 2335), (2, 0, "Front", 2331),
              (0, 1, "Buildings", 2362), (1, 1, "Buildings", 2367), (2, 1, "Buildings", 2363)],
    "bench_long": [(0, 0, "Front", 2330), (1, 0, "Front", 2335), (2, 0, "Front", 2335), (3, 0, "Front", 2331),
                   (0, 1, "Buildings", 2362), (1, 1, "Buildings", 2367), (2, 1, "Buildings", 2367), (3, 1, "Buildings", 2363)],
    # side benches: facing right (the west side of something) and facing left
    "bench_e": [(0, 0, "Front", 2048), (0, 1, "Buildings", 2080), (0, 2, "Buildings", 2112)],
    "bench_w": [(0, 0, "Front", 2062), (0, 1, "Buildings", 2094), (0, 2, "Buildings", 2126)],
    # the park fountain (Town x 24-28, y 23-28), 5 x 6
    "fountain": [(2, 0, "Front", 3111)] + [(i, 1, "Front", g) for i, g in enumerate((3080, 3077, 3241, 3110, 3084))]
                + [(i, 2 + j, "Buildings", g) for j, row in enumerate(((3112, 3305, 3273, 3305, 3116), (3144, 3145, 3305, 3147, 3148),
                                                                       (3176, 3177, 3178, 3179, 3180), (3208, 3209, 3210, 3211, 3212)))
                   for i, g in enumerate(row)]
                # the basin's floor, on Back under the water (Town has it; without it the
                # paving showed through the basin in winter, the user, 2026-11-07)
                + [(1, 2, "Back", 3113), (2, 2, "Back", 252), (3, 2, "Back", 3115), (2, 3, "Back", 3146)],
    # the picnic table by the park (Town x 32-34, y 36-38)
    "picnic": [(0, 0, "Front", 3091), (1, 0, "Front", 3092), (2, 0, "Front", 3093), (1, 1, "Front", 1124),
               (0, 1, "Buildings", 3123), (1, 1, "Buildings", 3124), (2, 1, "Buildings", 3125),
               (0, 2, "Buildings", 3155), (1, 2, "Buildings", 3156), (2, 2, "Buildings", 3157)],
}

# The game's fence sprites (LooseSprites/Fence1-5: 3 x 11 pieces of 16 x 32), as map
# tilesheets (build_vale.py write()); piece numbers from the modding wiki's tutorial.
FENCE_FIRST = {"wood": 31000, "stone": 31100}
FENCE_SHEETS = {"wood": "Fence1", "stone": "Fence2"}
UL, UR, V_MID, V_TOP_END, BL, H_MID, BR, H_RIGHT_END, H_LEFT_END = 0, 2, 3, 5, 6, 7, 8, 9, 10

OAK, MAPLE, PINE, GRASS, BUSH_L, BUSH_M, BUSH_S, SPAWN_TREE = 9, 10, 11, 22, 24, 25, 26, 34
BUSH_W = {BUSH_L: 3, BUSH_M: 2, BUSH_S: 1}
# Fruit trees by Data/FruitTrees id: cherry, apricot, peach, apple, pomegranate, orange
CHERRY, APRICOT, PEACH, APPLE, POMEGRANATE, ORANGE = "628", "629", "631", "633", "632", "630"


def decor(g, ground, paths_first, W, H):
    skipped = []

    def plain(x, y):
        return (0 <= x < W and 0 <= y < H and set(ground.get((g["Back"][y][x], g["Buildings"][y][x]), "?")) <= {"G", "D"}
                and not any(g[l][y][x] for l in ("Buildings", "Buildings2", "Back2", "Front", "Front2", "AlwaysFront", "Paths")))

    def marker(x, y, idx, w=1, why=""):
        if all(plain(x + i, y) for i in range(w)):
            g["Paths"][y][x] = paths_first + idx
            return True
        skipped.append((why or idx, x, y))
        return False

    def fruit(x, y, tree_id):
        if marker(x, y, SPAWN_TREE, why="fruit " + tree_id):
            TILE_PROPS[("Paths", x, y)] = {"SpawnTree": "fruit " + tree_id}

    def prop(name, x, y):
        cells = PROPS[name]
        clash = [(x + dx, y + dy) for dx, dy, layer, _ in cells
                 if not (0 <= x + dx < W and 0 <= y + dy < H) or (layer != "Back" and g[layer][y + dy][x + dx])
                 or (layer == "Buildings" and (g["Front2"][y + dy][x + dx] or g["Paths"][y + dy][x + dx]))]
        if clash:
            skipped.append((name, x, y, clash[:3]))
            return False
        for dx, dy, layer, gid in cells:
            g[layer][y + dy][x + dx] = gid
        return True

    rnd = random.Random(5)

    def fence(x0, y0, x1, y1, sides="tblr", gates=(), side_gates=(), kind="wood"):
        """The game's own placeable fence, laid as map tiles (LooseSprites/Fence1 wood,
        Fence2 stone; the user, 2026-11-06, after the modding wiki's "How Fences Work"):
        a piece for every place in a fence, upper and lower corners, run ends, the
        vertical post and its top end, so corners join as a farmer's fences do. Each
        piece is 16 x 32: its lower half on Buildings (solid), its upper half on Front a
        row up, so the piece below overlaps the one above, as the game draws them.
        gates: gaps in the top or bottom run (x); side_gates: gaps in the sides (y)."""
        first = FENCE_FIRST[kind]
        cells = {}
        horiz = [y for y, side in ((y0, "t"), (y1, "b")) if side in sides]
        for y in horiz:
            for x in range(x0, x1 + 1):
                if x in gates and y == y1:          # (gates are in the bottom run)
                    continue
                corner_l, corner_r = x == x0 and "l" in sides, x == x1 and "r" in sides
                if corner_l:
                    cells[(x, y)] = UL if y == y0 else BL
                elif corner_r:
                    cells[(x, y)] = UR if y == y0 else BR
                elif x == x0 or (y == y1 and (x - 1) in gates):
                    cells[(x, y)] = H_LEFT_END
                elif x == x1 or (y == y1 and (x + 1) in gates):
                    cells[(x, y)] = H_RIGHT_END
                else:
                    cells[(x, y)] = H_MID
        for x, side in ((x0, "l"), (x1, "r")):
            if side not in sides:
                continue
            for y in range(y0, y1 + 1):
                if (x, y) in cells or y in side_gates:
                    continue
                top_end = (y == y0) or (y - 1) in side_gates
                cells[(x, y)] = V_TOP_END if top_end else V_MID
        for (x, y), piece in cells.items():
            col, row = piece % 3, piece // 3
            top, bottom = first + (row * 2) * 3 + col, first + (row * 2 + 1) * 3 + col
            if not (0 < y < H and 0 <= x < W) or g["Buildings"][y][x] or g["Front2"][y][x] or g["Front"][y - 1][x]:
                skipped.append(("fence", x, y))
                continue
            g["Buildings"][y][x] = bottom
            g["Front"][y - 1][x] = top

    def flowers(x0, y0, x1, y1, share, ground=0.0):
        """Wildflowers in the grass: the game's flower tufts (pink 258, blue 308) on Back2,
        under the farmer's feet (on Front they drew over him: the user, 2026-11-08, "I can
        walk under instead of over"), and some of the grass itself its flowery tiles (152, 153)."""
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if not plain(x, y):
                    continue
                r = rnd.random()
                if r < share:
                    g["Back2"][y][x] = rnd.choice((258, 308))
                elif r < share + ground and g["Back"][y][x] == 176:
                    g["Back"][y][x] = rnd.choice((152, 153))

    def rock(x, y):
        if plain(x, y):
            g["Buildings"][y][x] = rnd.choice((233, 283))

    def clump(x, y, tree=OAK):
        """A stand as the game grows them: a tree with bushes and flowers at its feet."""
        marker(x, y, tree, why="clump tree")
        for dx, dy, k in ((-2, 1, BUSH_M), (2, 1, BUSH_S), (-1, 2, BUSH_S)):
            marker(x + dx, y + dy, k, BUSH_W[k], "clump bush")
        flowers(x - 3, y - 1, x + 3, y + 3, 0.12)

    # Q: lamps along the high street (its north curb) and the plaza
    for x in (24, 33, 41, 50):
        prop("lamp", x, 2)
    for x, y in ((37, 36), (52, 36), (45, 43), (21, 58), (66, 21)):
        prop("lamp", x, y)

    # D: a hedgerow under the woods behind the high street, gaps at the lamps
    for x in (26, 29, 35, 38, 43, 46, 52):
        if not all(plain(x + i, 3) for i in range(3)):
            marker(x, 2, BUSH_L, 3, "hedge")        # a step back behind whatever stands there (Lupini's street spot)
        else:
            marker(x, 3, BUSH_L, 3, "hedge")

    # O: bushes and flowers along the upper cliff's top
    for x, kind in ((3, BUSH_M), (7, BUSH_S), (11, BUSH_M), (16, BUSH_L), (22, BUSH_S), (26, BUSH_M), (36, BUSH_M), (40, BUSH_S), (47, BUSH_L)):
        marker(x, 26, kind, BUSH_W[kind], "cliff-top bush")

    # C: the Painter's clearing, ringed by trees at the trail's end
    for x, y, t in ((2, 13, MAPLE), (11, 13, OAK), (2, 22, OAK), (11, 21, MAPLE), (12, 17, PINE), (3, 18, PINE)):
        marker(x, y, t, why="clearing tree")
    for x, y, k in ((8, 12, BUSH_M), (4, 15, BUSH_S), (9, 22, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "clearing bush")
    for x, y in ((5, 16), (8, 17), (5, 19), (8, 20), (6, 22)):
        marker(x, y, GRASS, why="clearing grass")

    # B: the inn's garden, between it and the pottery (tea tables come with the props)
    # (Bea's tea garden is painted into the map: build_vale.py FEATURES)

    # E: an old orchard across the river, north-east
    for (x, y), t in zip(((88, 3), (92, 3), (90, 6), (88, 9), (93, 9)), (CHERRY, APPLE, PEACH, APRICOT, APPLE)):
        fruit(x, y, t)

    # F: Lupini's wild garden round the atelier
    for x, y, k in ((80, 16, BUSH_M), (85, 18, BUSH_L), (89, 15, BUSH_S), (82, 21, BUSH_S), (88, 21, BUSH_M), (91, 18, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "wild garden bush")
    for x, y in ((83, 15), (84, 16), (86, 15), (87, 16), (83, 19), (90, 20), (86, 22), (81, 18), (92, 22)):
        marker(x, y, GRASS, why="wild garden grass")

    # P: the atelier's bench at the upper cliff's edge, east of Lupini's stairs (the
    # overlook's own bench is on its bluff: tools/vale_overlook.py)
    prop("bench", 85, 25)
    marker(84, 26, BUSH_S, why="atelier bench bush")
    marker(89, 26, BUSH_S, why="atelier bench bush")

    # G: the pottery's yard (clay pit and firewood come with the props)
    # (Town's "crates" at x 75, y 93 were a fragment of something else: the user, 2026-11-06)

    # H: the plaza: the park's fountain east of the Hall's door, benches round it
    prop("fountain", 46, 40)
    prop("bench_w", 52, 41)
    prop("bench_long", 41, 43)

    # I: the Lost Parterre is painted into the map (build_vale.py FEATURES)

    # K: Fern's wildflower meadow, long grass in drifts, bushes at its edges
    for y in range(64, 75):
        for x in range(3, 31):
            if (x * 7 + y * 13) % 5 < 2 and ((x - 16) / 13.0) ** 2 + ((y - 69) / 5.0) ** 2 < 1:
                marker(x, y, GRASS, why="meadow")
    for x, y, k in ((2, 63, BUSH_M), (29, 64, BUSH_S), (31, 70, BUSH_M)):
        marker(x, y, k, BUSH_W[k], "meadow edge")

    # The lower terrace's open middle: a stand of trees between the meadow and the lake
    for x, y, t in ((5, 57, MAPLE), (27, 60, OAK), (31, 72, PINE)):        # (the Gilded Tree's lawn is x 33-44)
        marker(x, y, t, why="lower trees")

    # N: a picnic spot at the end of the east trail. (The overlook's bluff stands here now:
    # tools/vale_overlook.py clears its box and lays the picnic table at its foot, PICNIC.
    # These stay as they were, so the generator the decor shares draws the same for
    # everything after them, outside the box.)
    prop("picnic", 89, 64)
    for x, y, k in ((88, 60, BUSH_M), (92, 69, BUSH_S), (93, 60, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "picnic bush")
    for x, y, t in ((94, 66, MAPLE), (90, 72, OAK)):
        marker(x, y, t, why="picnic tree")

    # The cohesion pass (the user, 2026-11-06, after Timberline Falls: "we're missing
    # things like nature, fences, bushes"): every home gets a yard, the grass gets its
    # flowers and stones, and trees grow in stands with bushes at their feet.

    # Lupini's wild garden, fenced, a gate onto his lane (x 79-81)
    fence(82, 13, 90, 22, sides="tblr", gates=(85, 86))
    flowers(83, 14, 89, 21, 0.18, 0.2)
    # Emmett's side yard, against his cottage's west wall
    fence(9, 55, 14, 60, sides="tlb", gates=(11, 12))
    flowers(10, 57, 13, 59, 0.15)
    # the Old Studios gone to seed: long grass at their sides and backs
    for x, y in ((66, 34), (67, 36), (66, 37), (79, 31), (94, 33), (94, 36), (81, 35), (88, 30)):
        marker(x, y, GRASS, why="studio grass")
    # stands of trees in the open ground
    clump(14, 21, MAPLE)
    clump(5, 31, OAK)
    clump(73, 45, MAPLE)
    clump(85, 45, OAK)
    clump(91, 52, PINE)
    clump(26, 70, OAK)
    # stones here and there, as the game's grass has
    # ((92, 62) is on the overlook now: vale_overlook clears it; it stays here so the
    # shared generator draws the same for everything after it)
    for x, y in ((16, 12), (6, 25), (39, 24), (58, 30), (70, 44), (90, 43), (4, 50), (24, 57), (29, 66), (8, 72), (92, 62), (47, 66)):
        rock(x, y)
    # flowers through every open stretch (the meadow thickest)
    flowers(0, 10, 20, 27, 0.03, 0.04)          # the upper west
    flowers(34, 20, 55, 27, 0.03, 0.03)         # under the inn and the pottery
    flowers(64, 0, 95, 12, 0.04, 0.05)          # the orchard
    flowers(0, 28, 30, 38, 0.03, 0.03)          # the middle west (above the parterre)
    flowers(64, 42, 95, 47, 0.05, 0.06)         # below the Old Studios
    flowers(2, 62, 31, 75, 0.12, 0.25)          # Fern's meadow
    flowers(0, 54, 50, 75, 0.03, 0.03)          # the rest of the lower terrace
    flowers(84, 54, 95, 75, 0.04, 0.04)         # the overlook's side (vale_overlook clears its box; the call stays for the generator)
    flowers(0, 26, 56, 27, 0.06)                # along the upper cliff's top
    flowers(0, 46, 95, 47, 0.05)                # and the lower one's

    for s in skipped:
        print("decor skipped", s)

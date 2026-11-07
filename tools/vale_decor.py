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
                   for i, g in enumerate(row)],
    # the picnic table by the park (Town x 32-34, y 36-38)
    "picnic": [(0, 0, "Front", 3091), (1, 0, "Front", 3092), (2, 0, "Front", 3093), (1, 1, "Front", 1124),
               (0, 1, "Buildings", 3123), (1, 1, "Buildings", 3124), (2, 1, "Buildings", 3125),
               (0, 2, "Buildings", 3155), (1, 2, "Buildings", 3156), (2, 2, "Buildings", 3157)],
}

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
                 if not (0 <= x + dx < W and 0 <= y + dy < H) or g[layer][y + dy][x + dx]
                 or (layer == "Buildings" and (g["Front2"][y + dy][x + dx] or g["Paths"][y + dy][x + dx]))]
        if clash:
            skipped.append((name, x, y, clash[:3]))
            return False
        for dx, dy, layer, gid in cells:
            g[layer][y + dy][x + dx] = gid
        return True

    # Q: lamps along the high street (its north curb) and the plaza
    for x in (24, 33, 41, 50):
        prop("lamp", x, 2)
    for x, y in ((37, 36), (52, 36), (45, 43), (21, 58), (66, 21)):
        prop("lamp", x, y)

    # D: a hedgerow under the woods behind the high street, gaps at the lamps
    for x in (26, 29, 35, 38, 43, 46, 52):
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
    for x, y, k in ((35, 17, BUSH_M), (38, 17, BUSH_S)):     # (the game has no free-standing bed: Town's is against Emily's wall)
        marker(x, y, k, BUSH_W[k], "inn garden bush")

    # E: an old orchard across the river, north-east
    for (x, y), t in zip(((88, 3), (92, 3), (90, 6), (88, 9), (93, 9)), (CHERRY, APPLE, PEACH, APRICOT, APPLE)):
        fruit(x, y, t)

    # F: Lupini's wild garden round the atelier
    for x, y, k in ((80, 16, BUSH_M), (85, 18, BUSH_L), (89, 15, BUSH_S), (82, 21, BUSH_S), (88, 21, BUSH_M), (91, 18, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "wild garden bush")
    for x, y in ((83, 15), (84, 16), (86, 15), (87, 16), (83, 19), (90, 20), (86, 22), (81, 18), (92, 22)):
        marker(x, y, GRASS, why="wild garden grass")

    # P: the overlook bench at the upper cliff's edge, east of Lupini's stairs
    prop("bench", 85, 25)
    marker(84, 26, BUSH_S, why="overlook bush")
    marker(89, 26, BUSH_S, why="overlook bush")

    # G: the pottery's yard (clay pit and firewood come with the props)
    # (Town's "crates" at x 75, y 93 were a fragment of something else: the user, 2026-11-06)

    # H: the plaza: the park's fountain east of the Hall's door, benches round it
    prop("fountain", 46, 40)
    prop("bench_w", 52, 41)
    prop("bench_long", 41, 43)

    # I: the sculpture garden: beds and hedges (the statues come with the props)
    for x, y, k in ((3, 44, BUSH_M), (26, 44, BUSH_M), (6, 46, BUSH_S), (23, 46, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "garden bush")
    for x, k in ((10, BUSH_M), (15, BUSH_S), (19, BUSH_M)):
        marker(x, 46, k, BUSH_W[k], "garden hedge")

    # K: Fern's wildflower meadow, long grass in drifts, bushes at its edges
    for y in range(64, 75):
        for x in range(3, 31):
            if (x * 7 + y * 13) % 5 < 2 and ((x - 16) / 13.0) ** 2 + ((y - 69) / 5.0) ** 2 < 1:
                marker(x, y, GRASS, why="meadow")
    for x, y, k in ((2, 63, BUSH_M), (29, 64, BUSH_S), (31, 70, BUSH_M)):
        marker(x, y, k, BUSH_W[k], "meadow edge")

    # The lower terrace's open middle: a stand of trees between the meadow and the lake
    for x, y, t in ((33, 63, OAK), (37, 67, MAPLE), (41, 63, OAK), (35, 72, PINE), (43, 70, MAPLE), (39, 74, OAK), (5, 57, MAPLE), (27, 60, OAK)):
        marker(x, y, t, why="lower trees")

    # N: a picnic spot at the end of the east trail
    prop("picnic", 89, 64)
    for x, y, k in ((88, 60, BUSH_M), (92, 69, BUSH_S), (93, 60, BUSH_S)):
        marker(x, y, k, BUSH_W[k], "picnic bush")
    for x, y, t in ((94, 66, MAPLE), (90, 72, OAK)):
        marker(x, y, t, why="picnic tree")

    for s in skipped:
        print("decor skipped", s)

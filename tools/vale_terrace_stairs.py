"""The terraces' four flights (build_vale.STAIR_SPOTS), snowed on in winter, as the
overlook's flight is (docs/terrain_painting.md, "A raised bluff").

In winter the terraces' treads stayed the stairs art's khaki stone, a bare flight pasted
on the snow, while the overlook's flight has snow on every tread. All five flights are
the same art, so the terraces take the overlook's own copies of it (vale_overlook
flight_pieces: the art unchanged in spring, summer and fall, snow on the treads in
winter, slots in the Vale's seasonal sheets), tile for tile: treads on Back2, side walls
on Buildings2.

The cliff's shading needs nothing here. Each terrace cliff runs right across the map
(build_vale.cliff), so its band already runs on beside each flight and behind its side
walls, whose open outer halves show it; under the treads it is hidden, since every tread
row of the art but the top one is solid. (The overlook's band was one that stopped beside
its flight; these never did.)

Only the flights' own tiles change (vale_patch.BOXES, "stairs_*"). Re-runnable: the
copies are found again by their pixels, and the flights' gids map to themselves.

Run as: python tools/vale_terrace_stairs.py <GildedVale.tmx> <exports>   (build_vale.VALE_PIECES)
"""
import os, sys, types

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_vale import STAIR_SPOTS
from vale_patch import STAIR_BOXES, load, save

ROWS = 6        # the art's rows, from the cliff's top row (lay_stairs; the rows above and below hold none)


def cells(sx, top):
    """A flight's tiles: (x, y, layer), treads on Back2, side walls on Buildings2."""
    return [(sx + i, top + j, "Back2" if i in (1, 2) else "Buildings2") for j in range(ROWS) for i in range(4)]


def patch_file(tmx_path, exports):
    from vale_overlook import flight_pieces
    m = load(tmx_path)
    # (flight_pieces and its sheet slots need only the assets folder, not the ground table)
    ctx = types.SimpleNamespace(assets=os.path.dirname(os.path.abspath(tmx_path)), exports=exports)
    own = flight_pieces(m, ctx)
    for (sx, top), box in zip(STAIR_SPOTS, STAIR_BOXES.values()):
        for x, y, layer in cells(sx, top):
            assert box[0] <= x <= box[2] and box[1] <= y <= box[3], (x, y, box)
            m.g[layer][y][x] = own.get(m.g[layer][y][x], m.g[layer][y][x])
    save(m, tmx_path)


if __name__ == "__main__":
    patch_file(sys.argv[1], sys.argv[2])

"""Renders a TMX map (or part of it) to PNG, for planning map edits.

Tilesheet images are looked up by file name next to the TMX (as written by
the museart_export_map command) or in --sheets. Pass --grid to label tile
coordinates every 5 tiles, --passable to tint tiles with Buildings-layer
content red.

Usage: render_tmx.py map.tmx out.png [--region x y w h] [--grid] [--sheets dir] [--scale n]
"""
import argparse, base64, gzip, os, struct, zlib, xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

DRAW_ORDER = ["Back", "Back2", "Back3", "Buildings", "Buildings2", "Front", "Front2", "AlwaysFront", "AlwaysFront2"]


def load(tmx_path, sheets_dir):
    root = ET.parse(tmx_path).getroot()
    tilesets = []
    for ts in root.findall("tileset"):
        img = ts.find("image")
        name = os.path.basename(img.get("source").replace("\\", "/"))
        path = os.path.join(sheets_dir, name + ".png")
        if not os.path.exists(path):
            path = os.path.join(sheets_dir, name)
        tilesets.append((int(ts.get("firstgid")), int(ts.get("columns")), Image.open(path).convert("RGBA") if os.path.exists(path) else None))
    tilesets.sort(key=lambda t: t[0])
    layers = {}
    for layer in root.findall("layer"):
        w, h = int(layer.get("width")), int(layer.get("height"))
        data = layer_data(layer.find("data"))
        layers[layer.get("name")] = (w, h, data)
    return root, tilesets, layers


def layer_data(node):
    """A layer's gids, from CSV or from base64 (optionally zlib/gzip
    compressed), the way Tiled saves maps like SVE's."""
    if node.get("encoding") == "base64":
        raw = base64.b64decode(node.text.strip())
        if node.get("compression") == "zlib":
            raw = zlib.decompress(raw)
        elif node.get("compression") == "gzip":
            raw = gzip.decompress(raw)
        return [v & 0x1FFFFFFF for v in struct.unpack(f"<{len(raw) // 4}I", raw)]   # (high bits are flip flags)
    return [int(v) for v in node.text.replace("\n", "").split(",") if v.strip()]


def tile_image(tilesets, gid, cache):
    gid &= 0x1FFFFFFF  # strip flip flags
    if gid == 0:
        return None
    if gid in cache:
        return cache[gid]
    for first, cols, img in reversed(tilesets):
        if gid >= first:
            if img is None:
                cache[gid] = None
                return None
            i = gid - first
            x, y = (i % cols) * 16, (i // cols) * 16
            tile = img.crop((x, y, x + 16, y + 16))
            cache[gid] = tile
            return tile
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("tmx")
    ap.add_argument("out")
    ap.add_argument("--region", nargs=4, type=int)
    ap.add_argument("--grid", action="store_true")
    ap.add_argument("--passable", action="store_true")
    ap.add_argument("--sheets")
    ap.add_argument("--scale", type=int, default=2)
    ap.add_argument("--layers", nargs="+", help="only draw these layers")
    a = ap.parse_args()

    root, tilesets, layers = load(a.tmx, a.sheets or os.path.dirname(os.path.abspath(a.tmx)))
    mw, mh = int(root.get("width")), int(root.get("height"))
    rx, ry, rw, rh = a.region or (0, 0, mw, mh)
    canvas = Image.new("RGBA", (rw * 16, rh * 16), (20, 20, 20, 255))
    cache = {}
    for name in DRAW_ORDER:
        if name not in layers or (a.layers and name not in a.layers):
            continue
        w, h, data = layers[name]
        for y in range(ry, min(ry + rh, h)):
            for x in range(rx, min(rx + rw, w)):
                tile = tile_image(tilesets, data[y * w + x], cache)
                if tile is not None:
                    canvas.alpha_composite(tile, ((x - rx) * 16, (y - ry) * 16))
        if a.passable and name == "Buildings":
            tint = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
            d = ImageDraw.Draw(tint)
            for y in range(ry, min(ry + rh, h)):
                for x in range(rx, min(rx + rw, w)):
                    if data[y * w + x]:
                        d.rectangle([(x - rx) * 16, (y - ry) * 16, (x - rx) * 16 + 15, (y - ry) * 16 + 15], fill=(255, 0, 0, 70))
            canvas.alpha_composite(tint)

    canvas = canvas.resize((canvas.width * a.scale, canvas.height * a.scale), Image.NEAREST)
    if a.grid:
        d = ImageDraw.Draw(canvas)
        s = 16 * a.scale
        for x in range(rx, rx + rw):
            if x % 5 == 0:
                px = (x - rx) * s
                d.line([px, 0, px, canvas.height], fill=(255, 255, 255, 90))
                d.text((px + 2, 2), str(x), fill=(255, 255, 0, 255))
        for y in range(ry, ry + rh):
            if y % 5 == 0:
                py = (y - ry) * s
                d.line([0, py, canvas.width, py], fill=(255, 255, 255, 90))
                d.text((2, py + 2), str(y), fill=(255, 255, 0, 255))
    canvas.save(a.out)
    print(f"{a.out}: tiles {rx},{ry} {rw}x{rh}")


if __name__ == "__main__":
    main()

"""Renders a festival map around where the villagers stand, marking each
villager's tile (Set-Up layer: red, MainEvent layer: blue), to choose free
spots for a new NPC. Uses render_tmx.py for the map itself.

Usage: festival_spots.py <exports dir> <map name> <layer> <out.png> [scale]
"""
import os, subprocess, sys, xml.etree.ElementTree as ET
from PIL import Image, ImageDraw


def layer_tiles(path, name):
    root = ET.parse(path).getroot()
    w = int(root.get("width"))
    for l in root.findall("layer"):
        if l.get("name") == name:
            data = [int(v) for v in l.find("data").text.replace("\n", "").split(",") if v.strip()]
            return [(i % w, i // w) for i, v in enumerate(data) if v]
    return []


def main():
    exports, name, layer, out = sys.argv[1:5]
    scale = int(sys.argv[5]) if len(sys.argv) > 5 else 2
    tmx = os.path.join(exports, name + ".tmx")
    spots = layer_tiles(tmx, layer)
    xs, ys = [x for x, _ in spots], [y for _, y in spots]
    x0, y0 = max(0, min(xs) - 6), max(0, min(ys) - 6)
    w, h = max(xs) - x0 + 7, max(ys) - y0 + 7
    here = os.path.dirname(os.path.abspath(__file__))
    subprocess.run([sys.executable, os.path.join(here, "render_tmx.py"), tmx, out, "--grid", "--passable",
                    "--region", str(x0), str(y0), str(w), str(h), "--scale", str(scale), "--sheets", exports], check=True)
    im = Image.open(out).convert("RGBA")
    d = ImageDraw.Draw(im)
    t = 16 * scale
    for x, y in spots:
        d.rectangle([(x - x0) * t, (y - y0) * t, (x - x0 + 1) * t - 1, (y - y0 + 1) * t - 1], outline=(255, 0, 0) if layer == "Set-Up" else (0, 80, 255), width=2)
    im.save(out)
    print(f"{name} {layer}: region {x0},{y0} {w}x{h}, {len(spots)} villagers")


if __name__ == "__main__":
    main()

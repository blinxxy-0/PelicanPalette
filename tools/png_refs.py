"""Exports pixel-art PNGs as raw RGBA rows for ArtStudio.Sim, which quantizes
them with the mod's own palette code. Usage: png_refs.py out.json a.png b.png ..."""
import json, os, sys
from PIL import Image

out = []
for path in sys.argv[2:]:
    img = Image.open(path).convert("RGBA")
    w, h = img.size
    px = img.load()
    rows = ["".join("%02x%02x%02x%02x" % px[x, y] for x in range(w)) for y in range(h)]
    out.append({"name": os.path.splitext(os.path.basename(path))[0], "w": w, "h": h, "rgba": rows})
json.dump(out, open(sys.argv[1], "w"))
print(f"{len(out)} images -> {sys.argv[1]}")

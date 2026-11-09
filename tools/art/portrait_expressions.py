"""Builds a Vale resident's portrait sheet from a finished neutral portrait and Higgsfield's
face-only edits of it, Bea's way (art/bea/higgsfield/README.md): each edit is read back onto
its 64x64 grid (tools/art/pixel_readback.py), finished as the neutral was
(tools/art/portrait_finish.py), and only the face box is taken from it, so the hair and
clothes stay pixel-identical across the expressions.

Slots, 2 across: 0 neutral, then the edits in the order given (1 happy, 2 sad, 3 unique,
4 love, 5 angry, as the residents' dialogue tags them).

Usage: portrait_expressions.py <neutral 64x64.png> <out sheet.png> <x0,y0,x1,y1> <edit.png>..."""
import os
import subprocess
import sys
import tempfile
import numpy as np
from PIL import Image

here = os.path.dirname(os.path.abspath(__file__))
base_path, out, box = sys.argv[1], sys.argv[2], [int(v) for v in sys.argv[3].split(",")]
edits = sys.argv[4:]
x0, y0, x1, y1 = box
base = np.asarray(Image.open(base_path).convert("RGBA"))
faces = [base]
tmp = tempfile.mkdtemp()
for i, e in enumerate(edits):
    rb, fin = os.path.join(tmp, f"rb{i}.png"), os.path.join(tmp, f"fin{i}.png")
    subprocess.run([sys.executable, os.path.join(here, "pixel_readback.py"), e, rb], check=True)
    subprocess.run([sys.executable, os.path.join(here, "portrait_finish.py"), rb, fin], check=True)
    f = np.asarray(Image.open(fin).convert("RGBA"))
    o = base.copy()
    o[y0:y1, x0:x1] = f[y0:y1, x0:x1]
    faces.append(o)
rows = (len(faces) + 1) // 2
sheet = np.zeros((rows * 64, 128, 4), np.uint8)
for i, f in enumerate(faces):
    sheet[(i // 2) * 64:(i // 2) * 64 + 64, (i % 2) * 64:(i % 2) * 64 + 64] = f
Image.fromarray(sheet, "RGBA").save(out)
print("wrote", out)

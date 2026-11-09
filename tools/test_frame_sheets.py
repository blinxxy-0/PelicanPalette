"""Guards the released frame sheets against the Debug-only Founders' Frame (the Vale painters' guide
design; tools/art/frame_styles.py draws it at the end of the sheets so no released sprite moves).

    python tools/test_frame_sheets.py [<base commit>]

Compares ArtStudio/assets/furniture.png and items.png in the working tree with the same sheets at the
base commit (default c154397, the last before the Founders' Frame existed). Every pixel outside the
founders' slots must be identical; where the sheet has grown since, every pixel outside them must be
fully transparent. The slots come from ArtStudio/Painting/ArtItems.cs's own constants: the item at
MoreFrameSprite + its place in MoreFrameIds (the released ids, then DebugOnlyFrameIds), and the
framed paintings at MoreFrameFurnitureIndex (2x2 at the index, 3x3 at index + 2, 1x1 at index +
SmallFrameRow). Exits non-zero on a failure.
"""
import io
import os
import re
import subprocess
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ART_ITEMS = os.path.join(ROOT, "ArtStudio", "Painting", "ArtItems.cs")
SHEETS = ("ArtStudio/assets/furniture.png", "ArtStudio/assets/items.png")
BASE = sys.argv[1] if len(sys.argv) > 1 else "c154397"


def const(text, name):
    m = re.search(r"\b" + name + r"\s*=\s*(\d+)", text)
    assert m, f"{name} not found in ArtItems.cs"
    return int(m.group(1))


def founders_slots():
    """{sheet path: [(x0, y0, x1, y1)]} for the Founders' Frame's sprites, in pixels."""
    text = open(ART_ITEMS, encoding="utf-8").read()
    # MoreFrameIds: the released frames, then DebugOnlyFrameIds (the #if DEBUG lines hold no quotes).
    ids_block = re.search(r"MoreFrameIds\s*=(.*?);", text, re.S).group(1)
    ids = re.findall(r'"([a-z-]+)"', ids_block)
    if "DebugOnlyFrameIds" in ids_block:
        ids += re.findall(r'"([a-z-]+)"', re.search(r"DebugOnlyFrameIds\s*=\s*\{(.*?)\};", text, re.S).group(1))
    n = ids.index("founders")
    # frames drawn on a sheet of their own (OwnSheetFrames: Tess's Carved Ash Frame) take an item
    # sprite but no place on furniture.png; their item sprites are new too, so they're slots as well
    own = re.findall(r'"([a-z-]+)"', re.search(r"OwnSheetFrames\s*=\s*\{(.*?)\};", text, re.S).group(1))
    on_sheet = n - sum(1 for i in ids[:n] if i in own)
    first_band, second_band = const(text, "FirstBandFrames"), const(text, "SecondBandFurniture")
    small = const(text, "MoreFrameFurniture") + 5 * on_sheet if on_sheet < first_band else second_band + 5 * (on_sheet - first_band)
    item = const(text, "MoreFrameSprite") + n
    own_items = [const(text, "MoreFrameSprite") + ids.index(i) for i in own]
    row = const(text, "SmallFrameRow")

    def tiles(sheet, index, size):
        per_row = Image.open(os.path.join(ROOT, sheet)).width // 16
        x, y = index % per_row * 16, index // per_row * 16
        return (x, y, x + size * 16, y + size * 16)
    furniture, items = SHEETS
    return {
        furniture: [tiles(furniture, small, 2), tiles(furniture, small + 2, 3), tiles(furniture, small + row, 1)],
        items: [tiles(items, item, 1)] + [tiles(items, i, 1) for i in own_items],
    }


def at_base(path):
    data = subprocess.run(["git", "show", f"{BASE}:{path}"], cwd=ROOT, capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(data)).convert("RGBA")


def test_frame_sheets_untouched():
    failures = []
    for path, slots in founders_slots().items():
        base, now = at_base(path), Image.open(os.path.join(ROOT, path)).convert("RGBA")
        assert now.width >= base.width and now.height >= base.height, (path, base.size, now.size)
        b, p = base.load(), now.load()
        inside = lambda x, y: any(x0 <= x < x1 and y0 <= y < y1 for x0, y0, x1, y1 in slots)
        changed = grown = founders = 0
        for y in range(now.height):
            for x in range(now.width):
                if inside(x, y):
                    founders += 1
                elif x < base.width and y < base.height:
                    changed += p[x, y] != b[x, y]
                else:
                    grown += p[x, y][3] != 0
        print(f"{path}: {base.size} at {BASE} -> {now.size}; founders slots {slots} ({founders} px); "
              f"changed outside them: {changed}; opaque in the grown area outside them: {grown}")
        if changed or grown:
            failures.append(path)
    assert not failures, failures


if __name__ == "__main__":
    try:
        test_frame_sheets_untouched()
    except AssertionError as e:
        print("FAIL test_frame_sheets_untouched", e)
        sys.exit(1)
    print("ok   test_frame_sheets_untouched")

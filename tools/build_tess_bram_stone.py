"""Bram's stone after Tess's 8-heart event (1.4; docs/tess_events.md, "leave it plain", approved by the
user 2026-11-09): she cuts his name properly where Lupini scratched it, and one leaf. The graveyard
(tools/vale_graveyard.py) lays his plain stone at (12, 17)-(12, 18) of the Vale; this lays the carved
one over it once you've seen the event, from Tess's side, without touching the graveyard's own files:

  - assets/tess/bram_stone.png: the carved stone's top and foot, then the same in winter (from
    art/tess/stone/stone_bram_carved(_winter).png, tools/art/bram_carved_stone.py)
  - assets/TessBramStone.tmx and TessBramStone_winter.tmx: 1 x 2 map patches, the top on Front and the
    foot on Buildings, with the grave's own action on the foot (blinxxy.MuseArt_Grave bram), so reading
    the stone still works
  - data/tess_bram_stone.json: the patches (Overlay, from the event on, the winter one in winter) and his
    stone's new words in Mods/blinxxy.MuseArt/ValeStrings (vale.grave.bram, i18n tess.grave.bram)

Usage: python tools/build_tess_bram_stone.py
"""
import json, os, re
from PIL import Image

PACK = "[CP] Gilded Vale (dev)"
ART = os.path.join("art", "tess", "stone")
TOP = (12, 17)                         # tools/vale_graveyard.py GRAVES["bram"]: the foot at (12, 18)
SHEET = "tess/bram_stone.png"
EVENT = "blinxxy.MuseArt_TessHeart8"
WHEN = {"ValeResidents": "true", "HasFile:assets/tess/sprites.png": "true", "HasFile:assets/tess/portraits.png": "true",
        "HasSeenEvent": EVENT}
# the stone's line about the marker, then its inscription (vale.grave.bram: "A plain stone, never carved.
# Scratched into it, not very neatly:^^BRAM FAIRLIE"); the wording the handover proposed
WORDS = "A plain stone. His name is cut into it now, deep and even, and under it a single leaf.^^BRAM FAIRLIE"


def sheet():
    out = Image.new("RGBA", (16, 64))
    for n, name in enumerate(("stone_bram_carved.png", "stone_bram_carved_winter.png")):
        im = Image.open(os.path.join(ART, name)).convert("RGBA")
        assert im.size == (16, 32), (name, im.size)
        out.alpha_composite(im, (0, 32 * n))
    out.save(os.path.join(PACK, "assets", SHEET))


def patch(path, first):
    """A 1 x 2 map: the stone's top (tile first) on Front over its foot (first + 1) on Buildings."""
    def layer(lid, name, top, foot):
        return (f'  <layer id="{lid}" name="{name}" width="1" height="2">\n'
                f'    <data encoding="csv">\n{top},\n{foot}\n</data>\n  </layer>')
    lines = ['<?xml version="1.0" encoding="UTF-8"?>',
             '<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" '
             'width="1" height="2" tilewidth="16" tileheight="16" infinite="0" nextlayerid="5" nextobjectid="2">',
             '  <tileset firstgid="1" name="z_blinxxy.MuseArt_TessBramStone" tilewidth="16" tileheight="16" '
             'tilecount="4" columns="1">',
             f'    <image source="{SHEET}" width="16" height="64"/>',
             '  </tileset>',
             layer(1, "Back", 0, 0),
             layer(2, "Buildings", 0, first + 2),
             layer(3, "Front", first + 1, 0),
             '  <objectgroup id="4" name="Buildings">',
             '    <object id="1" name="TileData" x="0" y="16" width="16" height="16">',
             '      <properties>',
             '        <property name="Action" type="string" value="blinxxy.MuseArt_Grave bram"/>',
             '      </properties>',
             '    </object>',
             '  </objectgroup>',
             '</map>']
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


def main():
    sheet()
    patch(os.path.join(PACK, "assets", "TessBramStone.tmx"), 0)
    patch(os.path.join(PACK, "assets", "TessBramStone_winter.tmx"), 2)
    area = {"X": TOP[0], "Y": TOP[1], "Width": 1, "Height": 2}
    changes = [
        {"LogName": "Bram's stone, carved (after Tess's 8-heart event)", "Action": "EditMap",
         "Target": "Maps/blinxxy.MuseArt_GildedVale", "FromFile": "assets/TessBramStone.tmx",
         "ToArea": area, "PatchMode": "Overlay", "Priority": "Late", "Update": "OnLocationChange, OnTimeChange",
         "When": {**WHEN, "Season": "spring, summer, fall"}},
        {"LogName": "Bram's stone, carved, in winter", "Action": "EditMap",
         "Target": "Maps/blinxxy.MuseArt_GildedVale", "FromFile": "assets/TessBramStone_winter.tmx",
         "ToArea": area, "PatchMode": "Overlay", "Priority": "Late", "Update": "OnLocationChange, OnTimeChange",
         "When": {**WHEN, "Season": "winter"}},
        {"LogName": "Bram's stone's words, carved", "Action": "EditData", "Target": "Mods/blinxxy.MuseArt/ValeStrings",
         "Priority": "Late", "Update": "OnLocationChange, OnTimeChange", "When": WHEN,
         "Entries": {"vale.grave.bram": "{{i18n:tess.grave.bram}}"}},
    ]
    out = os.path.join(PACK, "data", "tess_bram_stone.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write("// Bram's stone, carved, after Tess's 8-heart event. Generated by tools/build_tess_bram_stone.py:\n"
                "// edit that, not this file. (The graveyard itself is tools/vale_graveyard.py's.)\n")
        json.dump({"Changes": changes}, f, indent=2, ensure_ascii=False)
        f.write("\n")
    path = os.path.join(PACK, "i18n", "default.json")
    s = open(path, encoding="utf-8").read()
    s = re.sub(r'  "tess\.grave\.bram": "(?:[^"\\]|\\.)*",\n', "", s)
    anchor = '  "tess.name": "Tess",\n'
    assert anchor in s
    open(path, "w", encoding="utf-8").write(s.replace(anchor, anchor + f'  "tess.grave.bram": {json.dumps(WORDS)},\n', 1))
    print("wrote", out)


if __name__ == "__main__":
    main()

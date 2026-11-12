"""The art shop's moved site, for players with Aymeric de Borel's alternate house installed.

Aymeric's alternate (Eugene-compatible) house is built in SVE's Forest at 71-88, 26-39, on the art
shop's own ground (76-88, 27-38). With it installed (ArtStudio/Shop/ShopSite.cs detects it and sets
the Content Patcher token {{blinxxy.MuseArt/ShopMoved}} to "true"), the shop moves by MOVED_BY
(19 west, 3 south) onto the open woods below SVE's crop field, at 57-69, 30-41. (A player's report,
2026-11-12; the user chose to move the shop only when both are installed.)

This writes, from the usual patches so the two can't drift apart:
- [CP] Pelican Palette/data/shop_moved.json: the shop and its yard (ReplaceByLayer: SVE has a stump
  and a bush drawn on that ground), its no-spawn tiles, the shop's and gallery's exit warps, the
  schedule entries that stand by the shop (Sable's mornings and Friday easel, Julian's 24th,
  Margot's married Sundays), and the two Forest events staged at the shop (Sable's 4 hearts,
  Margot's 8), every position shifted. Where an event starts the farmer on ground that's now
  Aymeric's yard, the farmer starts at the end of their first walk instead.
- [CP] Gilded Vale (dev)/data/shop_moved.json: Lupini's 2-heart event's Forest version.
- When {"blinxxy.MuseArt/ShopMoved": "false"} on the usual shop patches in content.json.

Run as: python tools/shop_moved_site.py   (re-runnable)
"""
import json, os, re

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
PP = os.path.join(ROOT, "[CP] Pelican Palette")
VALE = os.path.join(ROOT, "[CP] Gilded Vale (dev)")
DX, DY = -19, 3
TOKEN = "blinxxy.MuseArt/ShopMoved"
AYMERIC = (71, 26, 88, 39)              # his alternate house's patch (inclusive)
SHOP = (76, 27, 13, 12)                 # the usual site's patch


def shifted(x, y):
    return x + DX, y + DY


def in_aymeric(x, y):
    return AYMERIC[0] <= x <= AYMERIC[2] and AYMERIC[1] <= y <= AYMERIC[3]


def entries(path, prefix, must=""):
    """{key: value} for the one-line JSON entries in path whose key starts with prefix."""
    out = {}
    for line in open(path, encoding="utf-8-sig"):
        s = line.strip().rstrip(",")
        if s.startswith('"' + prefix) and must in s:
            out.update(json.loads("{" + s + "}"))
    return out


def shift_event(script):
    """An event script with its absolute positions moved: the view, the actors, big props and
    temporary sprites (moves are relative and stay). A farmer starting on Aymeric's ground starts
    where their first walk ends."""
    parts = script.split("/")
    vx, vy = map(int, parts[1].split())
    parts[1] = "%d %d" % shifted(vx, vy)
    actors = parts[2].split(" ")
    for i in range(0, len(actors) - 3, 4):
        x, y = shifted(int(actors[i + 1]), int(actors[i + 2]))
        actors[i + 1], actors[i + 2] = str(x), str(y)
    out = []
    for p in parts[3:]:
        w = p.split(" ")
        if w[0] == "addBigProp":
            w[1], w[2] = map(str, shifted(int(w[1]), int(w[2])))
        elif w[0] == "temporaryAnimatedSprite":
            w[9], w[10] = map(str, shifted(int(w[9]), int(w[10])))
        out.append(" ".join(w))
    # the farmer's start, if it's now in Aymeric's yard: fold in their first straight walk
    fi = actors.index("farmer")
    fx, fy = int(actors[fi + 1]), int(actors[fi + 2])
    if in_aymeric(fx, fy):
        k = next(i for i, p in enumerate(out) if p.startswith("move farmer "))
        _, _, mx, my, *_ = out[k].split(" ")
        fx, fy = fx + int(mx), fy + int(my)
        actors[fi + 1], actors[fi + 2] = str(fx), str(fy)
        del out[k]
    assert not in_aymeric(fx, fy), (fx, fy)
    parts[2] = " ".join(actors)
    return "/".join(parts[:3] + out)


def shift_schedule(value):
    return re.sub(r"Forest (\d+) (\d+)", lambda m: "Forest %d %d" % shifted(int(m.group(1)), int(m.group(2))), value)


def leave_sooner(value, from_farm=False, minutes=20):
    """A schedule with each walk into the shop from far off (the farm when married, or a location
    other than the Forest and the shop's own rooms) starting `minutes` sooner."""
    steps = value.split("/")
    out = []
    for i, step in enumerate(steps):
        words = step.split(" ")
        came_from = steps[i - 1].split(" ")[1] if i > 0 and len(steps[i - 1].split(" ")) > 1 else ""
        far = (i == 0 and from_farm) or (i > 0 and came_from not in ("", "Forest", "blinxxy.MuseArt_SableBasement")
                                         and not came_from.startswith("blinxxy.MuseArt_ArtShop"))
        if len(words) > 1 and words[0].isdigit() and words[1] == "blinxxy.MuseArt_ArtShop" and far:
            t = int(words[0])
            mins = (t // 100) * 60 + t % 100 - minutes
            words[0] = str((mins // 60) * 100 + mins % 60)
        out.append(" ".join(words))
    return "/".join(out)


def when(moved):
    return {TOKEN: "true" if moved else "false"}


def main():
    content = open(os.path.join(PP, "content.json"), encoding="utf-8").read()
    # the usual no-spawn tiles, moved
    nospawn = [(int(x), int(y)) for x, y in re.findall(r'"Position": \{ "X": (\d+), "Y": (\d+) \}, "Layer": "Back", "SetProperties": \{ "NoSpawn": "All"', content)]
    assert len(nospawn) == 156, len(nospawn)
    x0, y0 = shifted(SHOP[0], SHOP[1])
    patches = [
        {"LogName": "Art shop, moved for Aymeric's alternate house", "Action": "EditMap", "Target": "Maps/Forest",
         "FromFile": "assets/ArtShopExteriorPatch.tmx", "ToArea": {"X": x0, "Y": y0, "Width": SHOP[2], "Height": SHOP[3]},
         "PatchMode": "ReplaceByLayer", "Priority": "Late", "When": when(True)},
        {"LogName": "Art shop yard no-spawn, moved", "Action": "EditMap", "Target": "Maps/Forest",
         "MapTiles": [{"Position": {"X": sx, "Y": sy}, "Layer": "Back", "SetProperties": {"NoSpawn": "All", "Spawnable": None}}
                      for sx, sy in (shifted(x, y) for x, y in nospawn)], "When": when(True)},
    ]
    for room in ("ArtShop", "ArtShopGallery"):
        tmx = open(os.path.join(PP, "assets", room + ".tmx"), encoding="utf-8").read()
        warp = re.search(r'name="Warp" value="([^"]+)"', tmx).group(1)
        patches.append({"LogName": f"{room} exit, to the moved shop", "Action": "EditMap", "Target": f"Maps/blinxxy.MuseArt_{room}",
                        "MapProperties": {"Warp": shift_schedule(warp)}, "When": when(True)})
    for npc, path in (("Sable", "data/sable_schedule.json"), ("JulianVane", "data/julian_schedule.json"), ("MargotPell", "data/margot_schedule.json")):
        sched = json.loads(re.sub(r"^\s*//.*$", "", open(os.path.join(PP, path), encoding="utf-8-sig").read(), flags=re.M))
        moved = {k: shift_schedule(v) for k, v in sched.items() if re.search(r"Forest \d+ \d+", v)}
        if npc == "Sable":
            # the moved shop is about 20 tiles further from the farm and town: Sable sets off for it
            # 20 minutes sooner wherever she walks there from elsewhere, so it opens on time
            for k, v in sched.items():
                earlier = leave_sooner(moved.get(k, v), from_farm=k.startswith("marriage"))
                if earlier != moved.get(k, v):
                    moved[k] = earlier
        patches.append({"LogName": f"{npc}'s schedule by the moved shop", "Action": "EditData", "Target": f"Characters/schedules/{npc}",
                        "Entries": moved, "When": when(True)})
    events = {}
    events.update(entries(os.path.join(PP, "data/sable_events.json"), "blinxxy.MuseArt_Sable4h/"))
    events.update(entries(os.path.join(PP, "data/margot_events.json"), "blinxxy.MuseArt_Margot8h/"))
    assert len(events) == 2, list(events)
    patches.append({"LogName": "Sable's and Margot's events by the moved shop", "Action": "EditData", "Target": "Data/Events/Forest",
                    "Entries": {k: shift_event(v) for k, v in events.items()}, "When": when(True)})
    write(os.path.join(PP, "data", "shop_moved.json"), patches)

    lupini = entries(os.path.join(VALE, "data/lupini_events.json"), "blinxxy.MuseArt_Lupini2h/", "DayOfMonth")
    assert len(lupini) == 1
    write(os.path.join(VALE, "data", "shop_moved.json"), [
        {"LogName": "Lupini's 2-heart event (the Forest's) by the moved shop", "Action": "EditData", "Target": "Data/Events/Forest",
         "Entries": {k: shift_event(v) for k, v in lupini.items()}, "When": when(True)}])

    # the usual patches step aside when the shop has moved
    for marker in ('"FromFile": "assets/ArtShopExteriorPatch.tmx",', '"MapTiles": [\n        { "Position": { "X": 76, "Y": 27 }, "Layer": "Back", "SetProperties": { "NoSpawn"'):
        i = content.index(marker)
        start = content.rindex("{\n      \"Action\": \"EditMap\"", 0, i)
        head = content[start:i]
        if TOKEN not in head:
            content = content[:start] + content[start:].replace('"Target": "Maps/Forest",', '"Target": "Maps/Forest",\n      "When": { "%s": "false" },' % TOKEN, 1)
    # CJB Cheats Menu's warp to the shop's gate stays on the usual site; the moved one goes with it
    cjb = re.search(r'"Location": "Forest", "Tile": "(\d+), (\d+)"', content)
    if cjb:
        i = content.index('"Target": "Mods/CJBok.CheatsMenu/Warps"')
        start = content.rindex("{", 0, i)
        if TOKEN not in content[start:i + 200]:
            content = content[:i] + content[i:].replace('"When": { "HasMod": "CJBok.CheatsMenu" },',
                                                        '"When": { "HasMod": "CJBok.CheatsMenu", "%s": "false" },' % TOKEN, 1)
        gx, gy = shifted(int(cjb.group(1)), int(cjb.group(2)))
        moved_patches = json.loads(re.sub(r"^//.*$", "", open(os.path.join(PP, "data", "shop_moved.json"), encoding="utf-8").read(), flags=re.M))["Changes"]
        moved_patches.append({"LogName": "CJB Cheats Menu's warp to the moved shop", "Action": "EditData", "Target": "Mods/CJBok.CheatsMenu/Warps",
                              "Entries": {"{{ModId}}_ArtShop": {"Id": "{{ModId}}_ArtShop", "SectionId": "forest", "DisplayName": "{{i18n:location.art-shop}}",
                                                               "Location": "Forest", "Tile": f"{gx}, {gy}"}},
                              "When": {"HasMod": "CJBok.CheatsMenu", TOKEN: "true"}})
        write(os.path.join(PP, "data", "shop_moved.json"), moved_patches)
    # last of each pack's patches, so the moved events and schedules win over the usual ones
    content = _include_last(content, "    // The shop moved, when Aymeric's alternate house is on its ground (tools/shop_moved_site.py; last, so it wins)\n"
                                     '    { "Action": "Include", "FromFile": "data/shop_moved.json" }')
    open(os.path.join(PP, "content.json"), "w", encoding="utf-8").write(content)
    vale = open(os.path.join(VALE, "content.json"), encoding="utf-8").read()
    vale = _include_last(vale, "    // Lupini by the art shop, if it moved (tools/shop_moved_site.py; last, so it wins)\n"
                               '    { "Action": "Include", "FromFile": "data/shop_moved.json" }')
    open(os.path.join(VALE, "content.json"), "w", encoding="utf-8").write(vale)
    manifest = os.path.join(VALE, "manifest.json")
    m = open(manifest, encoding="utf-8-sig").read()
    if '"blinxxy.MuseArt"' not in m:                # its token comes from the C# mod
        m = m.replace('{ "UniqueID": "blinxxy.MuseArt.CP" }', '{ "UniqueID": "blinxxy.MuseArt.CP" },\n    { "UniqueID": "blinxxy.MuseArt" }', 1)
        assert '"blinxxy.MuseArt"' in m
        open(manifest, "w", encoding="utf-8").write(m)
    print("shop moved site: written")


def _include_last(text, block):
    """text with block as the last of its Changes, taken out of wherever an earlier run put it."""
    lines = text.split("\n")
    lines = [l for l in lines if "data/shop_moved.json" not in l and "tools/shop_moved_site.py" not in l]
    text = "\n".join(lines)
    end = text.rindex("]")                       # the end of the Changes list
    body = text[:end].rstrip()
    if not body.endswith(","):
        body += ","
    return body + "\n" + block + "\n  " + text[end:].lstrip()


def write(path, patches):
    head = ("// The art shop moved, for players with Aymeric de Borel's alternate house installed: generated by\n"
            "// tools/shop_moved_site.py (edit that, not this). Each patch only applies when\n"
            "// {{blinxxy.MuseArt/ShopMoved}} is true (ArtStudio/Shop/ShopSite.cs).\n")
    with open(path, "w", encoding="utf-8") as f:
        f.write(head + json.dumps({"Changes": patches}, indent=2, ensure_ascii=False) + "\n")


if __name__ == "__main__":
    main()

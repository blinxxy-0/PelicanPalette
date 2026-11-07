"""Learns how the game recolours its ground through the seasons, for painted art that
must change with it (the user, 2026-11-06: the clay bank's sand stayed spring yellow in
fall). The seasonal tilesheets share their layout, so each pixel of the spring sheet
and the same pixel of the fall sheet are one colour's two seasons: for every spring
colour, the commonest colour it becomes, per season.

Usage: python tools/art/vale_season_colours.py <spring dir> <summer dir> <fall dir> <winter dir> <out.json>
  each dir holds that season's sheets named spring_*.png, as exported.
"""
import collections, json, os, sys
from PIL import Image

SHEETS = ("spring_outdoorsTileSheet", "spring_outdoorsTileSheet2", "spring_town")


def learn(spring_dir, season_dir):
    votes = collections.defaultdict(collections.Counter)
    for name in SHEETS:
        a = Image.open(os.path.join(spring_dir, name + ".png")).convert("RGBA")
        b = Image.open(os.path.join(season_dir, name + ".png")).convert("RGBA")
        for ca, cb in zip(a.get_flattened_data(), b.get_flattened_data()):
            if ca[3] == 255 and cb[3] == 255:
                votes[ca[:3]][cb[:3]] += 1
    out = {}
    for c, v in votes.items():
        total = sum(v.values())
        best, n = v.most_common(1)[0]
        if total >= 12 and best != c and n >= total * 0.5:   # a colour the season reliably changes
            out["%02x%02x%02x" % c] = "%02x%02x%02x" % best
    return out


if __name__ == "__main__":
    spring, *seasons, path = sys.argv[1:]
    data = {name: learn(spring, d) for name, d in zip(("summer", "fall", "winter"), seasons)}
    json.dump(data, open(path, "w"), indent=0)
    print({k: len(v) for k, v in data.items()})

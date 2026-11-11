"""Writes Lupini's heart events ([CP] Gilded Vale (dev)/data/lupini_events.json) from the approved draft
in docs/vale_events.md (the "Lupini" sections: 2 and 4 hearts, then 6, 8 and 10 after "# 6 to 10
hearts"; approved by the user to be built as written). Built like Tess's and the Corries'
(build_tess_events.py, build_corrie_events.py): readable pieces, the text lifted into the dev pack's
i18n as lupini-event.*, one block after Lupini's own lines ("lupini.dialogue.bram").

Event IDs: blinxxy.MuseArt_Lupini2h ... Lupini10h, each needing the one before. Each plays where the
draft puts it, in the Gilded Vale (data/lupini_schedule.json has him out at a painting spot on fine
days, at his atelier at 4, the Vale House in the evening; the draft's places win where they differ).
The game walks every scene; the player only answers.

Remembered choices, as mail flags:
  4 hearts, where you stopped:    blinxxy.MuseArt_LupiniSpot_washing / _bus / _claypit
  6 hearts, the unfinished one:   blinxxy.MuseArt_LupiniStudio_restore / _finish / _leave

What the events change (this file's map and data patches; the art is tools/art/lupini_event_art.py,
all of it placeholder):
  - 2 hearts: the puddle sketch, a framed painting (furniture).
  - 4 hearts: the spot you chose gets a stool and a rock like his others, copied from the
    rooftops' (GildedValeBuildings 1100-1103 over 1132-1135), its stool's feet carrying
    blinxxy.MuseArt_PaintSpot <washing|bus|claypit> (ArtStudio/Vale/PaintersGuide.cs ChosenSpots:
    not one of the guide's eight). Bea's washing line stands west of the Vale House for the walk.
  - 6 hearts: the big Old Studio is boarded up (its door planked over, a line instead of the warp)
    until this event's been seen; the event takes the planks off on screen.
  - 8 hearts: sets blinxxy.MuseArt_ValeHallOpen (content.json swaps the Hall's map on it). The
    scene plays in the open hall as a temporary map (Maps/blinxxy.MuseArt_LupiniHallEvent, the
    same ValeHall.tmx), with the two pictures on its end wall by your answers at 4 and 6. The
    Vale's sign is repainted from then on.
  - 10 hearts: his travelling easel (furniture only: Painting Mastery's master's easel stays the
    only easel with a bonus) and the Vale at dawn (a painting, furniture).
  - 2 hearts needs "a fine day after a rainy one": G blinxxy.MuseArt_RAINED_YESTERDAY
    (ArtStudio/Vale/LupiniEvents.cs). With the town trips on, a second version plays by the river
    in the Forest on his Saturdays there (data/vale_town_trips.json), once he's arrived.

Usage: python tools/build_lupini_events.py       (from the repository root; run
       tools/art/lupini_event_art.py first for the art)
"""
import json, os, re, sys
import xml.etree.ElementTree as ET

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_sable_events import q, speak, script, quick

PACK = "[CP] Gilded Vale (dev)"
L, BEA, TOBIN, FERN, EMMETT, SABLE = "Lupini", "BeaThistle", "TobinClay", "FernAster", "EmmettWren", "Sable"
VALE, STUDIO = "blinxxy.MuseArt_GildedVale", "blinxxy.MuseArt_ValeStudio"
HALL_EVENT = "blinxxy.MuseArt_LupiniHallEvent"          # the open Hall, for the 8-heart scene
EV = {h: f"blinxxy.MuseArt_Lupini{h}h" for h in (2, 4, 6, 8, 10)}
AGAIN = EV[2] + "_again"                                # the 2-heart question, second time round
SPOT = {k: f"blinxxy.MuseArt_LupiniSpot_{k}" for k in ("washing", "bus", "claypit")}
STUDIO_CHOICE = {k: f"blinxxy.MuseArt_LupiniStudio_{k}" for k in ("restore", "finish", "leave")}
HALL_OPEN = "blinxxy.MuseArt_ValeHallOpen"
RAINED = "blinxxy.MuseArt_RAINED_YESTERDAY"
SABLE8 = "blinxxy.MuseArt_Sable8h"
ITEM = {k: f"blinxxy.MuseArt_Lupini{k}" for k in ("PuddleSketch", "Easel", "Dawn")}
PROPS, ITEMS = "Mods\\blinxxy.MuseArt\\LupiniEventProps", "Mods\\blinxxy.MuseArt\\LupiniItems"


def has(slug):
    return {f"HasFile:assets/{slug}/sprites.png": "true", f"HasFile:assets/{slug}/portraits.png": "true"}


WHEN = {"ValeResidents": "true", **has("lupini")}
# the 8-heart scene has the Vale's other residents in it
WHEN8 = {**WHEN, **has("bea"), **has("tobin"), **has("fern"), **has("emmett")}


def F(hearts):
    return f"Friendship {L} {hearts * 250}"


def say(line):
    return "message " + q(line)


def sprite(texture, x, y, w, h, tx, ty, sort):
    """A still prop for the scene (as the Corries' and Margot's)."""
    return f"temporaryAnimatedSprite {texture} {x} {y} {w} {h} 9999 1 9999 {tx} {ty} false false {sort} 0 1 0 0 0"


def easel(tx, ty):
    """His travelling easel (items.png), standing with its feet on row ty + 1."""
    return sprite(ITEMS, 16, 0, 16, 32, tx, ty, ty + 1)


def skip(*actions):
    """What a skip still does (the game's setSkipActions: trigger actions, '#'-separated)."""
    return "setSkipActions " + "#".join(actions)


# ================================================================ 2 hearts

def tail2(lupini_faces):
    return [
        f"faceDirection {L} {lupini_faces}",
        say("He turns the sketchbook round. It's mostly white: a few lines of the road's edge, and the light lying flat across the water, the one bright stripe on it."),
        speak(L, "The light. The way it lies down on the water like it's tired. It does that for about ten minutes after rain, and then it gets up again.$0"),
        speak(L, "I used to draw like this all day, when I was twenty. Before anyone was looking.$s"),
        say("He tears the page out and gives it to you."),
        speak(L, "There. A Lupini. Don't tell anyone, they'll want to know what it's worth.$u"),
        f"addItem (F){ITEM['PuddleSketch']} 1",
        "pause 300", "end",
    ]


CLOUDS = ("The clouds in it.", [speak(L, "Closer. Still wrong.$u")])
FROG = ("A frog?", [say("He looks into the puddle for a long moment."),
                    speak(L, "...There's no frog. Why would you say frog?$a")])


def heart2(at):
    """2 hearts: the artist's eye (the Vale's sign, a morning after rain; or the Forest, by the river).
    at: (viewport, farmer start, steps, Lupini, his facing, puddle tile, his facing to you)."""
    view, farmer, steps, lupini, faces, pool, to_you = at
    return script(
        "continue", view, f"farmer {farmer} 1 {L} {lupini} {faces}", "skippable",
        skip(f"AddItem (F){ITEM['PuddleSketch']} 1"),
        sprite(PROPS, 0, 0, 32, 16, *pool, 0),
        "pause 600", f"move farmer {steps} 0 1", "pause 300",
        say("Lupini is crouched over a puddle in the road, sketchbook on his knee, glasses pushed down his nose. He doesn't look up."),
        speak(L, "Don't step in it. You'll ruin it.$u"),
        speak(L, "Go on. What am I drawing?"),
        # "The puddle?": the choice comes round once more, without this answer (AGAIN)
        quick([("The puddle?", [speak(L, "Everyone says that. Wrong. Try again.$h"), f"switchEvent {AGAIN}"]),
               CLOUDS, FROG]),
        *tail2(to_you),
    ), script(quick([CLOUDS, FROG]), *tail2(to_you))


VALE2 = ("19 7", "15 7", 3, "20 6", 2, (19, 7), 3)       # the cobbles under the sign, the puddle at 19-20, 7
FOREST2 = ("82 40", "78 40", 3, "84 40", 3, (82, 40), 3)  # his Saturday spot by the river (vale_town_trips.json)


# ================================================================ 4 hearts

# where you stop: the farmer's and his tiles (he sits where the stool will be), facings, the viewport
STOPS = {
    "washing": dict(view="16 16", lupini="16 17", lf=0, farmer="17 17", ff=0,
                    then=[say("Sheets on the line, the light through them.")]),
    "bus": dict(view="13 5", lupini="13 4", lf=3, farmer="14 4", ff=3,
                then=[say("The empty road, the bus shelter, the long shadows.")]),
    "claypit": dict(view="51 24", lupini="50 25", lf=1, farmer="50 26", ff=1,
                    then=[speak(L, "He'll be unbearable.$u")]),
}


def walk_to(view, lupini, lf, farmer, ff):
    return ["globalFade", "viewport -1000 -1000", f"warp {L} {lupini}", f"faceDirection {L} {lf}",
            f"warp farmer {farmer}", f"faceDirection farmer {ff}", f"viewport {view}", "globalFadeToClear", "pause 400"]


def heart4():
    """4 hearts: a place worth painting (outside his atelier, a late afternoon; then a walk)."""
    answers = []
    for spot, text in (("washing", "Here, by Bea's washing line."), ("bus", "Here, the turning circle by the bus."),
                       ("claypit", "Here, Tobin's clay pit.")):
        s = STOPS[spot]
        answers.append((text, [f"addMailReceived {SPOT[spot]}", skip(f"AddMail Current {SPOT[spot]} Received"),
                               *walk_to(s["view"], s["lupini"], s["lf"], s["farmer"], s["ff"]), *s["then"]]))
    return script(
        "continue", "73 25", f"farmer 73 28 0 {L} 73 24 2", "skippable",
        "pause 500", "move farmer 0 -2 0", "pause 300",
        speak(L, "My friend! Walk with me. I need somewhere to paint, and I've painted everywhere.$h"),
        # the overlook
        *walk_to("90 60", "89 61", 3, "90 61", 3),
        say("At the overlook he stops, frames it with his hands, and walks on."),
        speak(L, "Too famous. It's on every postcard, and half of them are mine.$u"),
        # the lake
        *walk_to("47 63", "47 64", 1, "46 64", 1),
        speak(L, "Too blue. Nobody believes it."),
        # the old tree
        *walk_to("41 64", "42 64", 3, "43 64", 3),
        speak(L, "Too gold. It's showing off, and it knows it."),
        say("He gets quieter as you go."),
        speak(L, "Do you know, I can't remember the last time I painted something because I wanted to look at it.$s"),
        quick(answers),
        say("He sits down where he is and starts sketching, and doesn't say anything for a long time."),
        "pause 1500",
        speak(L, "I don't know why this one. I don't know why at all.$l"),
        speak(L, "That's the first good sign in twenty years.$h"),
        "pause 300", "end",
    )


# ================================================================ 6 hearts

DOOR = (73, 37)                 # the big Old Studio's door (73, 37-38), its warp on the lower tile


def heart6(margot):
    """6 hearts: the unfinished painting (outside the Old Studios, an afternoon; then inside)."""
    choices = []
    for k, text, then in (
            ("restore", "Restore it.", [speak(L, "Keep it as it was. Yes. That's a kind of honesty.$0")]),
            ("finish", "Finish it.", [say("A long stillness."),
                                      speak(L, "Finish someone else's painting. I've done that for twenty years, with my own.$0")]),
            ("leave", "Leave it as it is.", [speak(L, "As it is. Unfinished, and nobody's.$l")])):
        choices.append((text, [f"addMailReceived {STUDIO_CHOICE[k]}", skip(f"AddMail Current {STUDIO_CHOICE[k]} Received"), *then]))
    conservator = [
        speak(L, "That conservator of yours, at the museum. She could make this as good as the day it was left.$0"),
        say("He doesn't sound sure he wants that."),
    ] if margot else []
    return script(
        "continue", "73 40", f"farmer 73 43 0 {L} 73 39 2", "skippable",
        # the planks are off the door (map_tiles.png: the plain door's tiles), on the ground beside it
        f"changeMapTile Buildings {DOOR[0]} {DOOR[1]} 5", f"changeMapTile Buildings {DOOR[0]} {DOOR[1] + 1} 15",
        sprite(PROPS, 32, 0, 32, 16, 74, 39, 0),
        "pause 500", "move farmer 0 -3 0", "pause 300",
        say("He's prised the boards off the big studio's door."),
        speak(L, "I've walked past this door for thirty years. Today it was unlocked. Doors don't do that, my friend. Come in before it changes its mind.$u"),
        f"faceDirection {L} 0", "pause 400",
        # inside (the Old Studio as built: tools/build_vale_buildings.py "studio")
        "globalFade", "viewport -1000 -1000", f"changeLocation {STUDIO}",
        "warp farmer 8 15", "faceDirection farmer 3", f"warp {L} 6 15", f"faceDirection {L} 3",
        "viewport 8 14", "globalFadeToClear", "pause 600",
        say("Dust, a cot, a washstand. On the easel, a landscape of the Vale, three-quarters finished. The sky's done; the valley floor is bare canvas."),
        f"move {L} 0 -1 0", f"move {L} 3 0 1", f"faceDirection {L} 0", "faceDirection farmer 0", "pause 400",
        say("The note, in pencil: 'Back after lunch.' No name, no date."),
        speak(L, "Whoever it was, they never came back from lunch.$s"),
        f"move {L} 4 0 1", f"move {L} 0 -1 0", f"faceDirection {L} 0",
        "move farmer 3 0 1", "move farmer 0 -1 0", "faceDirection farmer 0", "pause 400",
        say("He turns the backward painting round: the same view, finished, and worse."),
        speak(L, "They tried it twice. The unfinished one's better. It usually is.$u"),
        *conservator,
        f"faceDirection {L} 3", "faceDirection farmer 1",
        quick(choices),
        say("He doesn't decide."),
        speak(L, "Let's leave the door open. It's been shut long enough.$s"),
        "pause 300", "end",
    )


# ================================================================ 8 hearts

# the open Hall (ValeHall.tmx): the end wall's two pictures, where its patches put them (HALL_WALL)
HALL_WALL = {"painting": (18, 11), "sketch": (21, 11)}


def heart8():
    """8 hearts: the exhibition (the plaza, an evening; the Exhibition Hall, open)."""
    off = "-100 -100 2"
    wall_x, wall_y = HALL_WALL["painting"]
    return script(
        "continue", "44 42", f"farmer 44 45 0 {L} {off} {BEA} {off} {TOBIN} {off} {FERN} {off} {EMMETT} {off}", "skippable",
        # the dust sheets are off: the Hall's open for good (content.json's map swap)
        f"addMailReceived {HALL_OPEN}", skip(f"AddMail Current {HALL_OPEN} Received"),
        "pause 500", "move farmer 0 -3 0", "pause 400",
        say("The Exhibition Hall's doors are open, and lit."),
        "globalFade", f"changeToTemporaryMap {HALL_EVENT} false",
        "warp farmer 11 21", "faceDirection farmer 0",
        f"warp {L} 12 21", f"faceDirection {L} 0",
        f"warp {TOBIN} 16 17", f"faceDirection {TOBIN} 1",
        f"warp {BEA} 16 18", f"faceDirection {BEA} 0",
        f"warp {FERN} 22 17", f"faceDirection {FERN} 3",
        f"warp {EMMETT} 27 21", f"faceDirection {EMMETT} 3",
        "viewport 16 17", "globalFadeToClear", "pause 800",
        say("The dust sheets are off. The residents are there with cups of Bea's tea: Tobin by a plinth of his bowls, Fern beside a case of her pressed flowers, Emmett tuning in a corner."),
        say("No judges, no prices. On the end wall, two pictures and nothing else of Lupini's: the unfinished painting from the Old Studio, and his sketch from your walk."),
        "pause 600", f"faceDirection {L} 3", "faceDirection farmer 1",
        say("He's standing at the back, as far from his own wall as the room allows."),
        speak(L, "I'm terrified. Isn't that ridiculous? I've had shows in three cities.$s"),
        speak(L, "None of them were for anyone. They were for buyers."),
        f"faceDirection {L} 1", "pause 300",
        say("He watches Tobin explaining a bowl to Bea, who's heard it before."),
        speak(L, "This one's for everyone.$l"),
        f"faceDirection {L} 3",
        quick([
            ("Go and stand by your wall.", [
                speak(L, "You're very bossy for a farmer.$u"),
                f"move {L} 0 -3 0", f"move {L} {wall_x + 1 - 12} 0 1", f"move {L} 0 -4 0", f"faceDirection {L} 2",
                say("He goes. Someone asks him about the sketch, and he talks for twenty minutes.")]),
            ("Stay back here with me.", [
                speak(L, "Yes. The view's better from the back. It always is.$h")]),
        ]),
        "pause 400",
        say("Emmett plays. Bea makes a speech nobody can hear. Lupini laughs at something Tobin says, loudly."),
        "pause 600", "end",
    )


# ================================================================ 10 hearts

def heart10(sable):
    """10 hearts: something nobody will see (the overlook, dawn). Their easels at its north edge,
    looking out over the Vale towards the falls."""
    off = "-100 -100 2"
    actors = f"farmer 92 62 0 {L} 90 57 0" + (f" {SABLE} {off}" if sable else "")
    with_sable = [
        say("Footsteps on the path. Sable, with a sketchbook, sees the two of you and stops."),
        f"warp {SABLE} 92 69", f"faceDirection {SABLE} 0", f"move {SABLE} 0 -4 0", "pause 800",
        say("Lupini doesn't turn round."),
        speak(L, "There's a third easel in the shed, if you're staying.$u"),
        "pause 1500",
        f"move {SABLE} 2 0 1", f"faceDirection {SABLE} 0", "pause 600",
        say("A long pause; Sable fetches it and sets it up."),
        "globalFade", f"warp {SABLE} 94 57", f"faceDirection {SABLE} 0", easel(94, 55), "globalFadeToClear", "pause 800",
        say("Nobody says anything for the rest of the scene, and it's the most the two of them have ever said to each other."),
        "pause 1500",
    ] if sable else []
    return script(
        "continue", "92 58", actors, "skippable",
        skip(f"AddItem (F){ITEM['Easel']} 1", f"AddItem (F){ITEM['Dawn']} 1"),
        easel(90, 55), easel(92, 55),
        "pause 600", "move farmer 0 -5 0", "pause 300",
        say("Two easels stand at the edge, facing the Vale. He's at one, painting."),
        speak(L, "There you are. That one's yours. Don't argue, it's too early.$0"),
        speak(L, "I'm painting something I'll never show. Nobody's going to buy it, or review it, or hang it. When it's done I'm going to put it in a cupboard."),
        speak(L, "Paint with me. Don't show me yours either."),
        say("You paint together in silence as the sun comes up over the falls."),
        # (the scene fades through the sunrise; no minigame)
        "globalFade 0.005", "pause 1500", "globalFadeToClear 0.005", "pause 1200",
        *with_sable,
        f"faceDirection {L} 1", "faceDirection farmer 3",
        speak(L, "Do you know, I haven't enjoyed anything this much since I was twenty. I was very bad at painting at twenty.$l"),
        say("He gives you his old travelling easel."),
        speak(L, "It's carried every Lupini I ever liked. It won't make you better. It'll make you go outside.$h"),
        f"addItem (F){ITEM['Easel']} 1", f"addItem (F){ITEM['Dawn']} 1",
        "pause 300", "end",
    )


# ================================================================ text into i18n

class Lines:
    """The lines lifted out of the scripts (as cp_i18n.Strings.script does), one key per distinct
    line: a line that two versions of an event share keeps the first version's key."""

    def __init__(self):
        self.data, self.by_text = {}, {}

    def add(self, key, text):
        if text not in self.by_text:
            assert key not in self.data, key
            self.data[key] = text
            self.by_text[text] = key
        return "{{i18n:" + self.by_text[text] + "}}"

    def script(self, prefix, text):
        n = 0

        def key():
            nonlocal n
            n += 1
            return f"{prefix}.{n:02}"

        def lift(m):
            if m.group(1) is not None:
                return '"' + (self.add(key(), m.group(1)) if m.group(1).strip() else m.group(1)) + '"'
            parts = m.group(2).split("#")
            return "quickQuestion " + "#".join(self.add(key(), p) if p.strip() else p for p in parts) + "(break)"

        return re.sub(r'"([^"]*)"|quickQuestion (.*?)\(break\)', lift, text)


# ================================================================ map patches

TILE_SHEET_IDS = {"map_tiles.png": "z_blinxxy.MuseArt_LupiniMap", "hall_wall.png": "z_blinxxy.MuseArt_LupiniHallWall"}


def patch_tmx(path, image, w, h, layers, props=None):
    """A small map patch (as build_tess_bram_stone.py's): layers are {name: [tile index or None, ...]}
    row by row, indices into the image (16x16 tiles); props are {(x, y): {name: value}} on Buildings."""
    from PIL import Image
    im = Image.open(os.path.join(os.path.dirname(path), image))
    cols, count = im.width // 16, (im.width // 16) * (im.height // 16)
    out = ['<?xml version="1.0" encoding="UTF-8"?>',
           f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{w}" height="{h}" '
           f'tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(layers) + 3}" nextobjectid="{len(props or {}) + 1}">',
           f' <tileset firstgid="1" name="{TILE_SHEET_IDS[image]}" tilewidth="16" tileheight="16" tilecount="{count}" columns="{cols}">',
           f'  <image source="{image}" width="{im.width}" height="{im.height}"/>', ' </tileset>']
    for n, name in enumerate(("Back", *layers), 1):
        cells = layers.get(name, [None] * (w * h))
        rows = [",".join(str(0 if c is None else c + 1) for c in cells[r * w:(r + 1) * w]) for r in range(h)]
        out += [f' <layer id="{n}" name="{name}" width="{w}" height="{h}">', '  <data encoding="csv">', ",\n".join(rows), '</data>', ' </layer>']
    if props:
        out.append(f' <objectgroup id="{len(layers) + 2}" name="Buildings">')
        for i, ((x, y), values) in enumerate(props.items(), 1):
            out.append(f'  <object id="{i}" name="TileData" x="{x * 16}" y="{y * 16}" width="16" height="16">')
            out.append('   <properties>')
            out += [f'    <property name="{k}" type="string" value="{v}"/>' for k, v in values.items()]
            out += ['   </properties>', '  </object>']
        out.append(' </objectgroup>')
    out.append('</map>')
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out) + "\n")


def map_tiles(col, row, w, h, cols=10):
    return [(row + r) * cols + col + c for r in range(h) for c in range(w)]


# where the chosen spot's stool and rock stand: the left foot tile of each (2 x 2, top on Front2,
# foot on Buildings), on open grass, nothing straddling a path or a wall
CHOSEN = {
    "washing": ((15, 18), (17, 20)),   # west of the Vale House, below Bea's washing line (15-18, 14-15)
    "bus": ((12, 3), (15, 3)),         # north of the bus's turning circle, looking down the road at the shelter
    "claypit": ((49, 24), (46, 24)),   # on the grass west of Tobin's clay pit, looking at it
}
WASHING_LINE = (15, 14)                # its top-left tile: 4 x 2, the posts' feet and hems on row 15
STOOL = ((1100, 1101), (1132, 1133))   # the rooftops' stool and rock (tools/vale_spots.py's template)
ROCK = ((1102, 1103), (1134, 1135))
VALE_SHEET = "z_blinxxy.MuseArt_GildedValeBuildings"


def check_clear(tmx, tiles):
    """Every tile a patch lays on the Vale must be empty on Buildings, Front and Front2 now."""
    root = ET.parse(tmx).getroot()
    for layer in root.findall("layer"):
        if layer.get("name") not in ("Buildings", "Front", "Front2"):
            continue
        w = int(layer.get("width"))
        data = [int(v) for v in layer.find("data").text.replace("\n", "").split(",")]
        for x, y in tiles:
            assert data[y * w + x] == 0, f"({x}, {y}) isn't clear on {layer.get('name')}"


def spot_tiles(spot):
    (sx, sy), (rx, ry) = CHOSEN[spot]
    action = {"Action": f"blinxxy.MuseArt_PaintSpot {spot}"}
    tiles = []
    for (x, y), (top, foot), props in (((sx, sy), STOOL, action), ((rx, ry), ROCK, None)):
        for i in (0, 1):
            tiles.append({"Position": {"X": x + i, "Y": y - 1}, "Layer": "Front2", "SetTilesheet": VALE_SHEET, "SetIndex": top[i]})
            t = {"Position": {"X": x + i, "Y": y}, "Layer": "Buildings", "SetTilesheet": VALE_SHEET, "SetIndex": foot[i]}
            if props:
                t["SetProperties"] = props
            tiles.append(t)
    return tiles


def map_patches():
    assets = os.path.join(PACK, "assets", "lupini")
    vale_tmx = os.path.join(PACK, "assets", "GildedVale.tmx")
    # Bea's washing line (map_tiles.png 6-9 over 16-19)
    patch_tmx(os.path.join(assets, "WashingLine.tmx"), "map_tiles.png", 4, 2,
              {"Buildings": [None] * 4 + map_tiles(6, 1, 4, 1), "Front2": map_tiles(6, 0, 4, 1) + [None] * 4})
    # the Old Studio's door, boarded (map_tiles.png 4 over 14): a line, not the warp
    patch_tmx(os.path.join(assets, "OldStudioBoarded.tmx"), "map_tiles.png", 1, 2,
              {"Buildings": map_tiles(4, 0, 1, 2)}, {(0, 1): {"Action": "Message blinxxy.MuseArt_Vale.studio.boarded"}})
    # the Vale's sign, repainted (map_tiles.png 0-3, 10-13 over 20-23)
    patch_tmx(os.path.join(assets, "ValeSignRepainted.tmx"), "map_tiles.png", 4, 3,
              {"Buildings": [None] * 8 + map_tiles(0, 2, 4, 1), "Front2": map_tiles(0, 0, 4, 2) + [None] * 4})
    # the Hall's end wall, for the 8-heart scene (hall_wall.png, 9 columns)
    for n, state in enumerate(("restore", "finish", "leave")):
        patch_tmx(os.path.join(assets, f"HallPainting_{state}.tmx"), "hall_wall.png", 2, 2, {"Front": map_tiles(n * 2, 0, 2, 2, cols=9)})
    for n, spot in enumerate(("washing", "bus", "claypit")):
        patch_tmx(os.path.join(assets, f"HallSketch_{spot}.tmx"), "hall_wall.png", 1, 2, {"Front": map_tiles(6 + n, 0, 1, 2, cols=9)})

    wx, wy = WASHING_LINE
    check_clear(vale_tmx, [(wx + i, wy + j) for i in range(4) for j in range(2)])
    for spot, feet in CHOSEN.items():
        check_clear(vale_tmx, [(x + i, y - j) for x, y in feet for i in (0, 1) for j in (0, 1)])

    late = {"PatchMode": "Overlay", "Priority": "Late", "Update": "OnLocationChange"}
    patches = [
        {"LogName": "Lupini 4: Bea's washing line", "Action": "EditMap", "Target": f"Maps/{VALE}",
         "FromFile": "assets/lupini/WashingLine.tmx", "ToArea": {"X": wx, "Y": wy, "Width": 4, "Height": 2}, **late, "When": WHEN},
        *[{"LogName": f"Lupini 4: his spot by the {spot}", "Action": "EditMap", "Target": f"Maps/{VALE}",
           "Priority": "Late", "Update": "OnLocationChange", "When": {**WHEN, "HasFlag": SPOT[spot]},
           "MapTiles": spot_tiles(spot)} for spot in CHOSEN],
        {"LogName": "Lupini 6: the Old Studio boarded up, until his 6-heart event", "Action": "EditMap", "Target": f"Maps/{VALE}",
         "FromFile": "assets/lupini/OldStudioBoarded.tmx", "ToArea": {"X": DOOR[0], "Y": DOOR[1], "Width": 1, "Height": 2}, **late,
         "When": {**WHEN, f"HasSeenEvent |contains={EV[6]}": "false"}},
        {"LogName": "Lupini 8: the Vale's sign, repainted", "Action": "EditMap", "Target": f"Maps/{VALE}",
         "FromFile": "assets/lupini/ValeSignRepainted.tmx", "ToArea": {"X": 19, "Y": 2, "Width": 4, "Height": 3}, **late,
         "When": {**WHEN, "HasFlag": HALL_OPEN}},
        # the 8-heart scene's hall: the open Hall, whatever the real one's showing that evening
        {"LogName": "Lupini 8: the open Hall, for the scene", "Action": "Load", "Target": f"Maps/{HALL_EVENT}",
         "FromFile": "assets/ValeHall.tmx"},
    ]
    px, py = HALL_WALL["painting"]
    sx, sy = HALL_WALL["sketch"]
    for state in ("restore", "finish", "leave"):
        when = {**WHEN, "HasFlag": STUDIO_CHOICE[state]}
        if state == "leave":            # (skipped before the question: as it was)
            when = {**WHEN, "HasFlag |contains=" + ",".join(STUDIO_CHOICE[k] for k in ("restore", "finish")): "false"}
        patches.append({"LogName": f"Lupini 8: the unfinished painting, {state}", "Action": "EditMap", "Target": f"Maps/{HALL_EVENT}",
                        "FromFile": f"assets/lupini/HallPainting_{state}.tmx", "ToArea": {"X": px, "Y": py, "Width": 2, "Height": 2},
                        "PatchMode": "Overlay", "When": when})
    for spot in ("washing", "bus", "claypit"):
        patches.append({"LogName": f"Lupini 8: his sketch of the {spot}", "Action": "EditMap", "Target": f"Maps/{HALL_EVENT}",
                        "FromFile": f"assets/lupini/HallSketch_{spot}.tmx", "ToArea": {"X": sx, "Y": sy, "Width": 1, "Height": 2},
                        "PatchMode": "Overlay", "When": {**WHEN, "HasFlag": SPOT[spot]}})
    return patches


# ================================================================ the file

def main():
    lines = Lines()
    vale, forest = {}, {}

    s2, again = heart2(VALE2)
    vale[f"{EV[2]}/{F(2)}/Time 900 1200/Weather sunny/G {RAINED}"] = lines.script("lupini-event.2h", s2)
    vale[AGAIN] = lines.script("lupini-event.2h", again)
    f2, fagain = heart2(FOREST2)
    forest[f"{EV[2]}/{F(2)}/DayOfWeek Sat/DayOfMonth 6 20/!Season winter/Time 1100 1400/Weather sunny/G {RAINED}"] = lines.script("lupini-event.2h", f2)
    forest[AGAIN] = lines.script("lupini-event.2h", fagain)

    vale[f"{EV[4]}/{F(4)}/Time 1600 1800/Weather sunny/SawEvent {EV[2]}"] = lines.script("lupini-event.4h", heart4())

    met = "G PLAYER_HAS_MET Current MargotPell"
    vale[f"{EV[6]}/{F(6)}/Time 1300 1600/SawEvent {EV[4]}/{met}"] = lines.script("lupini-event.6h", heart6(True))
    vale[f"{EV[6]}/{F(6)}/Time 1300 1600/SawEvent {EV[4]}/G !PLAYER_HAS_MET Current MargotPell"] = lines.script("lupini-event.6h", heart6(False))

    vale8 = {f"{EV[8]}/{F(8)}/Time 1800 2100/Weather sunny/SawEvent {EV[6]}": lines.script("lupini-event.8h", heart8())}

    vale[f"{EV[10]}/{F(10)}/Time 600 700/SawEvent {EV[8]}/SawEvent {SABLE8}"] = lines.script("lupini-event.10h", heart10(True))
    vale[f"{EV[10]}/{F(10)}/Time 600 700/SawEvent {EV[8]}/!SawEvent {SABLE8}"] = lines.script("lupini-event.10h", heart10(False))

    # (a line can't hold the script's own separators once its token's replaced)
    for text in lines.data.values():
        assert "/" not in text and '"' not in text, text

    items = {
        ITEM["PuddleSketch"]: f"{ITEM['PuddleSketch']}/painting/1 2/1 2/1/1200/-1/{lines.add('lupini-event.item.puddle_sketch', 'Lupini\'s Puddle Sketch')}/0/Mods\\blinxxy.MuseArt\\LupiniItems/true",
        ITEM["Easel"]: f"{ITEM['Easel']}/decor/1 2/1 1/1/800/-1/{lines.add('lupini-event.item.easel', 'Lupini\'s Travelling Easel')}/1/Mods\\blinxxy.MuseArt\\LupiniItems/true",
        ITEM["Dawn"]: f"{ITEM['Dawn']}/painting/2 2/2 2/1/1000/-1/{lines.add('lupini-event.item.dawn', 'The Vale at Dawn')}/2/Mods\\blinxxy.MuseArt\\LupiniItems/true",
    }
    spot_lines = {
        f"vale.lupini_spot.{k}": lines.add(f"lupini-event.spot.{k}", v) for k, v in (
            ("washing", "Lupini's stool by Bea's washing line, where you stopped on his walk. Sheets on the line, the light through them."),
            ("bus", "Lupini's stool by the turning circle, where you stopped on his walk. The empty road, the bus shelter, the long shadows."),
            ("claypit", "Lupini's stool by Tobin's clay pit, where you stopped on his walk. Tobin pretends he hasn't noticed it."))}
    boarded = lines.add("lupini-event.studio_boarded", "The door's boarded shut. Through a gap in the planks: dust, and the corner of an easel.")

    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/LupiniEventProps", "FromFile": "assets/lupini/event_props.png"},
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/LupiniItems", "FromFile": "assets/lupini/items.png"},
        # (the Vale's event list starts empty: ours. Low, so another resident's events can load it too)
        {"Action": "Load", "Target": f"Data/Events/{VALE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"LogName": "Lupini's heart events, in the Vale", "Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        {"LogName": "Lupini's 8-heart event, with the Vale's residents", "Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN8, "Entries": vale8},
        {"LogName": "Lupini's 2-heart event, on his Saturday in the Forest", "Action": "EditData", "Target": "Data/Events/Forest",
         "When": {**WHEN, "ValeResidentsTownTrips": "true"}, "Entries": forest},
        {"LogName": "Lupini's keepsakes", "Action": "EditData", "Target": "Data/Furniture", "When": WHEN, "Entries": items},
        {"LogName": "Lupini's spot, what it says", "Action": "EditData", "Target": "Mods/blinxxy.MuseArt/ValeStrings", "When": WHEN, "Entries": spot_lines},
        {"LogName": "The Old Studio's boarded door", "Action": "EditData", "Target": "Strings/StringsFromMaps", "When": WHEN,
         "Entries": {"blinxxy.MuseArt_Vale.studio.boarded": boarded}},
        *map_patches(),
    ]
    out = os.path.join(PACK, "data", "lupini_events.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("// Lupini's heart events, and what they change on the map. Generated by tools/build_lupini_events.py\n"
                "// from the approved draft in docs/vale_events.md: edit that script, not this file.\n")
        f.write(json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")

    # the text into i18n, right after Lupini's own lines (this script's old keys out first)
    p = os.path.join(PACK, "i18n", "default.json")
    s = open(p, encoding="utf-8").read()
    s = re.sub(r'  "lupini-event\.[^"]+": .*\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in lines.data.items())
    anchor = re.search(r'  "lupini\.dialogue\.bram": .*\n', s)
    s = s[:anchor.end()] + block + s[anchor.end():]
    open(p, "w", encoding="utf-8", newline="\n").write(s)
    print("wrote", out, f"({len(vale) + len(vale8) + len(forest)} entries,", len(lines.data), "lines)")


if __name__ == "__main__":
    main()

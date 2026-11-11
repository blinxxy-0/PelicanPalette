"""Writes Fern Aster's heart events ([CP] Gilded Vale (dev)/data/fern_events.json) from the approved
draft in docs/vale_events.md (Fern's 2, 4, 6, 8 and 10 hearts; approved by the user 2026-11-11).
Built like the Corries' and Tess's (build_corrie_events.py, build_tess_events.py): readable pieces,
the text lifted into the dev pack's i18n (fern.event.*, fern.letter.*), spliced in as one block
right after Fern's own keys (that file has comments, so it isn't rewritten).

Each event plays where Fern is at that time on her schedule (data/fern_schedule.json):
  2 hearts  the meadow (Gilded Vale), Wed/Sun mornings, fine, spring or summer
  4 hearts  the sculpture garden (Gilded Vale), afternoons in winter or on a Saturday, not raining
  6 hearts  your farm, 7am to 11am, fine (four versions, one per season: the season's wild seeds)
  8 hearts  her seedhouse (ValeFern), 8pm to midnight, a fine summer night
  10 hearts the sculpture garden, Saturday (or Monday, when her town trips are off) mornings, not
            winter or rain, dating her; two versions, the second if you said "some" at 4 hearts
Each needs the one before. The 4-heart answer is remembered as a mail flag (FernGrantRestore /
FernGrantWild / FernGrantBoth).

What they leave behind (map patches by flag, Overlay, like Bram's stone):
  - her ring of stakes round the bees' nest in the meadow (assets/FernStakes.tmx) from the 2-heart
    event to the end of that season (mail flag blinxxy.MuseArt_FernStakes, taken away on day 1);
  - the night vine in flower in the seedhouse on summer nights from 8pm, after the 8-heart event
    (assets/FernNightVine.tmx);
  - every path in the sculpture garden flagstoned (assets/FernPath_<season>.tmx; the user, 2026-11-11:
    all of them, after her 10 hearts): laid overnight
    once the 10-heart event can happen (blinxxy.MuseArt_FernPathCleared, set by a morning trigger),
    so it's there when she shows it you, and for good after.
And mail: the honey (three mornings after the 2-heart event), "Not yet" (at 8 hearts outside
summer), the vine cutting (three mornings after the 8-heart event).

The art is tools/art/fern_event_art.py's (placeholders). Usage: python tools/build_fern_events.py
"""
import json, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cp_i18n import Strings
from build_sable_events import q, speak, script, quick

PACK = "[CP] Gilded Vale (dev)"
FERN = "FernAster"
VALE, COTTAGE = "blinxxy.MuseArt_GildedVale", "blinxxy.MuseArt_ValeFern"
EV = {h: f"blinxxy.MuseArt_Fern{h}h" for h in (2, 4, 6, 8, 10)}
WHEN = {"ValeResidents": "true", "HasFile:assets/fern/sprites.png": "true", "HasFile:assets/fern/portraits.png": "true"}
PROPS = "Mods\\blinxxy.MuseArt\\FernEventProps"
CUTTING = "(F)blinxxy.MuseArt_FernVineCutting"
SEEDS = {"spring": "(O)495", "summer": "(O)496", "fall": "(O)497", "winter": "(O)498"}
SEED_COUNT = 5

# where things are
STAKES_AT = (31, 63)           # the ring's top-left tile (2x2): the meadow's east edge, clear of its grass and its trees' crowns
MOWER_AT = (25, 61)            # the Heritage Society's mower, at the meadow's edge by the terrace
VINE_AT = (24, 12)             # the night vine's top-left tile in the seedhouse (2x3)
PATH_AREA = (6, 38, 22, 9)     # tools/art/fern_event_art.py PATH_AREA: the whole garden


def F(hearts):
    return f"Friendship {FERN} {hearts * 250}"


def mail(flag):
    return f"G PLAYER_HAS_MAIL Current blinxxy.MuseArt_{flag} Any"


def say(line):
    return "message " + q(line)


def sprite(x, y, w, h, tx, ty, sort, frames=1, interval=9999):
    """A prop from FernEventProps, standing for the scene (as Margot's desk painting)."""
    return f"temporaryAnimatedSprite {PROPS} {x} {y} {w} {h} {interval} {frames} 9999 {tx} {ty} false false {sort} 0 1 0 0 0"


# ---------------------------------------------------------------- 2 hearts: the bees

def heart2():
    sx, sy = STAKES_AT
    key = f"{EV[2]}/{F(2)}/Time 800 1200/!Season fall winter/Weather sunny/DayOfWeek Wed Sun"
    return key, script(
        "continue", f"{sx - 2} {sy + 1}", f"farmer {sx - 1} {sy - 3} 2 {FERN} {sx + 2} {sy + 1} 3", "skippable",
        sprite(0, 0, 32, 32, *MOWER_AT, MOWER_AT[1] + 1),
        sprite(32, 0, 32, 32, sx, sy, sy + 1, frames=2, interval=250),          # the nest, its bees busy
        "pause 600",
        say("A mower is parked at the meadow's edge, shiny and red, with a label: 'On loan from the Heritage Society. For the meadow.'"),
        say("Fern is on her knees in the long grass a long way from it, very still."),
        "move farmer 0 2 2", "pause 300",
        speak(FERN, "Don't move. Well, move, but slowly. Come and look.$u"),
        "speed farmer 1", "move farmer 0 2 2", "faceDirection farmer 1", "pause 600",
        say("A bumblebee nest in a hollow in the grass, the bees going in and out."),
        speak(FERN, "They send me a mower and I find this. Every year they send me a mower, and every year I find something.$h"),
        speak(FERN, "I'm not using it. I've never used it. I've told them. They keep sending it, like a very persistent present.$a"),
        say("She has a handful of garden stakes and string."),
        speak(FERN, "Help me mark round it, so nobody walks through.$0"),
        quick([
            ("Can't we move the nest?", [speak(FERN, "You don't move a nest. You move yourself.$s"),
                                         say("She hands you a stake."), speak(FERN, "Like this.$0")]),
            ("We could mow round it.", [speak(FERN, "We could. We won't.$a"),
                                        speak(FERN, "...All right, a path round it. A small one. With scissors.$u")]),
            ("Let's leave the whole meadow.", [speak(FERN, "That's what I keep telling them!$h")]),
        ]),
        "pause 300", "globalFade",
        sprite(96, 0, 32, 32, sx, sy, sy + 1),                                  # the ring, staked out
        "globalFadeToClear", "pause 400",
        say("You stake out a ring round the nest together. She talks to the bees the whole time, and once, by accident, to you."),
        "addMailReceived blinxxy.MuseArt_FernStakes", "pause 300", "end",
    )


# ---------------------------------------------------------------- 4 hearts: the grant

def heart4():
    key = (f"{EV[4]}/{F(4)}/Time 1300 1700/!Weather rainy/SawEvent {EV[2]}/"
           'G ANY "SEASON winter" "DAY_OF_WEEK Saturday"')
    return key, script(
        "continue", "22 42", f"farmer 18 42 1 {FERN} 24 42 2", "skippable",
        "pause 600", "move farmer 5 0 1", "pause 300",
        say("Fern sits on the plinth of the oldest statue with a letter in her lap."),
        f"faceDirection {FERN} 3",
        speak(FERN, "The Heritage Society. They'll give the Vale a grant to restore the garden. Hedges, paths, everything back how it was a hundred years ago.$s"),
        speak(FERN, "There's a condition. They want the statues cleaned. All of them.$s"),
        f"faceDirection {FERN} 1", "pause 600",
        say("She stands and shows you the oldest one: a woman with a book, green-gold with a moss that grows in her folds and nowhere else in the Vale."),
        speak(FERN, "This moss doesn't grow anywhere else. I've looked. It took a hundred years to get here.$0"),
        f"faceDirection {FERN} 3",
        speak(FERN, "If I say no, the paths stay broken and the roof on the seedhouse leaks another winter. If I say yes, this goes in a bucket.$u"),
        speak(FERN, "What would you do? Honestly. Don't be nice.$0"),
        quick([
            ("Take the grant. Clean them.", ["pause 1500", speak(FERN, "...That's honest. Thank you.$s"),
                                             "addMailReceived blinxxy.MuseArt_FernGrantRestore"]),
            ("Keep the moss. Lose the grant.", [speak(FERN, "That's what I want to hear. It's not what I need to hear.$h"),
                                                "addMailReceived blinxxy.MuseArt_FernGrantWild"]),
            ("Clean some. Leave this one.", [say("She goes very still."), speak(FERN, "Some.$0"),
                                             say("She says it again."), speak(FERN, "Some. I hadn't thought of some.$0"),
                                             "addMailReceived blinxxy.MuseArt_FernGrantBoth"]),
        ]),
        "pause 400",
        say("She folds the letter and doesn't decide."),
        speak(FERN, "I'll write to them tomorrow. Or next week. Moss is patient. So am I.$u"),
        "pause 300", "end",
    )


# ---------------------------------------------------------------- 6 hearts: rows

def heart6(season):
    """On your farm. Farm events are placed from the farmhouse's front door: (64, 15) is the tile in
    front of it wherever the house stands (Farm.ResetForEvent offsets warps and props by it), so this
    works on every farm map and wherever Robin moved the house. Nobody walks far: the farm's own
    crops and buildings are wherever you put them, and nothing on the farm is changed."""
    key = f"{EV[6]}/{F(6)}/Time 700 1100/Weather sunny/Season {season}/SawEvent {EV[4]}"
    return key, script(
        # (on the farm the game stands everyone at the door and centres the camera there; warps
        # are then moved with the house, so she's warped to her spot by the fence)
        "continue", "64 15", f"farmer 64 15 2 {FERN} 64 16 0", "skippable",
        f"warp {FERN} 66 17", f"faceDirection {FERN} 3",
        "pause 600",
        say("Fern is at your fence: her first visit to the farm, on the early bus."),
        speak(FERN, "I was passing. Nobody passes your farm. I came on purpose.$u"),
        f"move {FERN} 0 2 2", "pause 400",
        say("She walks your fields with her hands in her dungaree pockets, quiet for a long time. She crouches at a straight row and touches a leaf."),
        "pause 800", f"move {FERN} 0 -2 0", f"move {FERN} -1 0 3",
        "move farmer 0 2 2", "faceDirection farmer 1", f"faceDirection {FERN} 3",
        speak(FERN, "It's lovely. It's all so... tidy. Everything where you put it.$0"),
        speak(FERN, "My teacher used to say a good garden needs one thing nobody planned. Something that turns up and asks to stay.$0"),
        say("She takes a seed packet from her front pocket: the season's wild seeds, in a packet she folded herself."),
        speak(FERN, "Plant these somewhere. Anywhere. Don't decide where. Throw them, if you like.$l"),
        f"addItem {SEEDS[season]} {SEED_COUNT}",
        quick([
            ("By the house, where I'll see them.", [speak(FERN, "Good. You'll have to look at them every morning and not tidy them.$h")]),
            ("In the middle of a row.", [say("She laughs, loudest at herself."), speak(FERN, "Now you're learning.$h")]),
            ("Somewhere I'll forget.", [speak(FERN, "That's the best place. That's where all the good things grow.$l")]),
        ]),
        "pause 300", "end",
    )


# ---------------------------------------------------------------- 8 hearts: the night vine

def heart8():
    vx, vy = VINE_AT
    # the vine's own tiles come off for the scene, and it's drawn as a prop instead, closed, half
    # open and open: map tiles on Front would cover a prop (they draw over everything standing)
    off = [f"removeTile {vx + dx} {vy + dy} Front" for dy in (0, 1) for dx in (0, 1)] + \
          [f"removeTile {vx + dx} {vy + 2} Buildings" for dx in (0, 1)]
    key = f"{EV[8]}/{F(8)}/Time 2000 2400/Season summer/Weather sunny/SawEvent {EV[6]}"
    return key, script(
        "continue", "18 15", f"farmer 21 19 0 {FERN} {vx - 1} {vy + 2} 1", "skippable",
        *off, sprite(0, 32, 32, 48, vx, vy, vy + 2),
        "pause 600",
        say("The seedhouse is dark but for a lantern by the trellis. Fern sits on an upturned pot in front of the night-flowering vine, a blanket round her shoulders."),
        "move farmer 0 -4 0", "faceDirection farmer 1",
        f"faceDirection {FERN} 2",
        speak(FERN, "Shh. Sit. It's going to happen. I can tell. I've been wrong three years running, but I can tell.$l"),
        "move farmer 1 0 1", "faceDirection farmer 1", f"faceDirection {FERN} 1", "pause 1200",
        say("You wait. She talks quietly, to the vine and then to you."),
        "pause 600",
        speak(FERN, "Everyone thinks letting things grow is a philosophy. Sometimes it is. Sometimes I just can't keep up. A whole Vale of gardens, and one of me.$s"),
        f"faceDirection {FERN} 2",
        speak(FERN, "My teacher kept every hedge square for seventy years. I couldn't. So I told everyone the wild was better. What if I only said it because I can't do the other thing?$s"),
        quick([
            ("Does it matter, if the garden's happy?", [say("She thinks about it for a long time."), "pause 800",
                                                       speak(FERN, "...It might not. Huh.$0")]),
            ("You chose. You could have kept them square.", [speak(FERN, "Could I?$l"), "pause 800",
                                                             speak(FERN, "...I could. I didn't want to. Oh.$l")]),
            ("(Say nothing; take her hand.)", [say("She lets you.")]),
        ]),
        f"faceDirection {FERN} 1", "faceDirection farmer 1", "pause 1500",
        "removeTemporarySprites", sprite(32, 32, 32, 48, vx, vy, vy + 2), "pause 900",
        "removeTemporarySprites", sprite(64, 32, 32, 48, vx, vy, vy + 2), "pause 600",
        say("The vine opens: pale flowers, one after another, glowing faintly in the lantern light."),
        f"emote {FERN} 32",
        say("She laughs out loud, claps a hand over her mouth, and laughs again."),
        "pause 300", "end",
    )


# ---------------------------------------------------------------- 10 hearts: one path

def heart10(some, day):
    """Two versions: "You said some" only if that was your answer at 4 hearts."""
    told = mail("FernGrantBoth") if some else f"G !PLAYER_HAS_MAIL Current blinxxy.MuseArt_FernGrantBoth Any"
    key = (f"{EV[10]}/{F(10)}/Dating {FERN}/Time 800 1200/DayOfWeek {day}/!Season winter/!Weather rainy/"
           f"SawEvent {EV[8]}/{mail('FernPathCleared')}/{told}")
    lines = [
        "continue", "18 42", f"farmer 28 42 3 {FERN} 10 42 1", "skippable",
        "pause 600",
        say("One path through the garden has been cleared: swept flagstones, the hedges either side trimmed back just enough. The rest is as wild as ever. Fern stands at the end of it with shears, sweaty and pleased."),
        "move farmer -10 0 3", "pause 300",
        speak(FERN, "One path. Just one. Look.$h"),
        say("Along it, statues you couldn't see before: a woman with a book (the oldest, her moss still on), a boy with a kite, a pair of dancers."),
        f"move {FERN} 7 0 1",
        speak(FERN, "I wrote to the Heritage Society. I said they could have one path, cleaned and swept, for visitors. The rest stays wild. They said yes.$0"),
        say("She sounds amazed."),
        speak(FERN, "They said yes!$h"),
    ]
    if some:
        lines.append(speak(FERN, "You said some. I couldn't stop thinking about some.$l"))
    lines += [
        speak(FERN, "It's not neither. It's both, chosen carefully. Like your farm: a bit you shape, and a wild bit you leave.$l"),
        speak(FERN, "Walk it with me? You're the first.$l"),
        quick([
            ("Lead the way.", [say("She takes your hand and talks to every statue you pass, introducing you."),
                               f"move {FERN} 0 1 2", "faceDirection farmer 1",
                               f"move {FERN} 6 0 1 true", "move farmer 6 0 1", f"faceDirection {FERN} 0", "faceDirection farmer 0"]),
            ("Which statue's your favourite?", [speak(FERN, "The one with the moss. Obviously. She's a hundred years old and still growing things.$u")]),
        ]),
        "pause 600", "end",
    ]
    return key, script(*lines)


# ---------------------------------------------------------------- the map patches

def tmx(path, w, h, sheet, sheet_w, sheet_h, layers):
    """A small map patch: its own tilesheet (the sheet's path relative to assets/), and layers as
    {name: {(x, y): local tile index}}."""
    cols = sheet_w // 16

    def data(cells):
        return "\n".join(",".join(str(cells[(x, y)] + 1 if (x, y) in cells else 0) for x in range(w)) + ("," if y < h - 1 else "")
                         for y in range(h))
    out = ['<?xml version="1.0" encoding="UTF-8"?>',
           f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="{w}" height="{h}" '
           f'tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(layers) + 1}" nextobjectid="1">',
           f'  <tileset firstgid="1" name="z_blinxxy.MuseArt_{os.path.splitext(os.path.basename(sheet))[0]}" tilewidth="16" tileheight="16" '
           f'tilecount="{cols * (sheet_h // 16)}" columns="{cols}">',
           f'    <image source="{sheet}" width="{sheet_w}" height="{sheet_h}"/>',
           '  </tileset>']
    for n, (name, cells) in enumerate(layers.items(), 1):
        out.append(f'  <layer id="{n}" name="{name}" width="{w}" height="{h}">\n    <data encoding="csv">\n{data(cells)}\n</data>\n  </layer>')
    out.append('</map>')
    with open(os.path.join(PACK, "assets", path), "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out) + "\n")


def map_patches():
    # the ring of stakes: 2x2 on Buildings (solid: nobody walks through)
    tmx("FernStakes.tmx", 2, 2, "fern/stakes.png", 32, 32,
        {"Buildings": {(x, y): y * 2 + x for y in range(2) for x in range(2)}})
    # the night vine in flower: its top two rows on Front, its foot on Buildings, as the room has it
    tmx("FernNightVine.tmx", 2, 3, "fern/night_vine.png", 32, 48,
        {"Buildings": {(x, 2): 4 + x for x in range(2)}, "Front": {(x, y): y * 2 + x for y in range(2) for x in range(2)}})
    # the garden's paths: its own Back2 and Buildings tiles, re-laid (the art script's list;
    # a Buildings tile stays solid, as the birdbath's and the statue's are)
    tiles = json.load(open(os.path.join(PACK, "assets", "fern", "path_tiles.json")))
    ax, ay, aw, ah = PATH_AREA
    rows = (len(tiles) + 7) // 8
    by_layer = {}
    for n, (layer, x, y) in enumerate(tiles):
        by_layer.setdefault(layer, {})[(x - ax, y - ay)] = n
    for season in ("spring", "summer", "fall", "winter"):
        tmx(f"FernPath_{season}.tmx", aw, ah, f"fern/path_{season}.png", 128, rows * 16, by_layer)

    def edit(log, target, path, area, when, update="OnLocationChange, OnTimeChange"):
        return {"LogName": log, "Action": "EditMap", "Target": target, "FromFile": f"assets/{path}",
                "ToArea": {"X": area[0], "Y": area[1], "Width": area[2], "Height": area[3]},
                "PatchMode": "Overlay", "Priority": "Late", "Update": update, "When": {**WHEN, **when}}
    return [
        edit("Fern's ring of stakes round the bees' nest (her 2 hearts, to the season's end)", f"Maps/{VALE}", "FernStakes.tmx",
             (*STAKES_AT, 2, 2), {"HasFlag |contains=blinxxy.MuseArt_FernStakes": "true"}),
        edit("The night vine in flower (after Fern's 8 hearts: summer nights)", f"Maps/{COTTAGE}", "FernNightVine.tmx",
             (*VINE_AT, 2, 3), {"HasSeenEvent |contains=" + EV[8]: "true", "Season": "summer", "Query: {{Time}} >= 2000": "true"}),
        *[edit(f"Fern's flagstoned paths in the sculpture garden ({season})", f"Maps/{VALE}", f"FernPath_{season}.tmx", PATH_AREA,
               {"HasFlag |contains=blinxxy.MuseArt_FernPathCleared": "true", "Season": season})
          for season in ("spring", "summer", "fall", "winter")],
    ]


# ---------------------------------------------------------------- mail and triggers

def letters(strings):
    bg = "[letterbg Mods/blinxxy.MuseArt/ValeLetter 0]"
    honey = strings.add("fern.letter.honey", "From the bees. They asked me to write it.^^   -F")
    honey_t = strings.add("fern.letter.honey.title", "Honey")
    waiting = strings.add("fern.letter.notyet", "Not yet. It's waiting for summer. So am I.^^   -F")
    waiting_t = strings.add("fern.letter.notyet.title", "The vine")
    cutting = strings.add("fern.letter.cutting", "From the vine. It took three years. Be patient with it.^^   -F")
    cutting_t = strings.add("fern.letter.cutting.title", "A cutting")
    mail_entries = {
        "blinxxy.MuseArt_FernHoney": f"{bg}{honey}%item id (O)340 1 %%[#]{honey_t}",
        "blinxxy.MuseArt_FernNotYet": f"{bg}{waiting}[#]{waiting_t}",
        "blinxxy.MuseArt_FernCutting": f"{bg}{cutting}%item id {CUTTING} 1 %%[#]{cutting_t}",
    }

    def t(id_, cond, actions, once=True):
        e = {"Id": f"blinxxy.MuseArt_{id_}", "Trigger": "DayStarted", "Condition": cond, "Actions": actions}
        if not once:
            e["MarkActionApplied"] = False
        return e
    seen = "PLAYER_HAS_SEEN_EVENT Current "
    triggers = [
        # "a few days later": a count of mornings since the event, the letter on the third
        t("FernHoneyDays", f"{seen}{EV[2]}, !PLAYER_HAS_MAIL Current blinxxy.MuseArt_FernHoney Any",
          ["IncrementStat blinxxy.museart_fernhoneydays 1"], once=False),
        t("FernHoney", "PLAYER_STAT Current blinxxy.museart_fernhoneydays 3", ["AddMail Current blinxxy.MuseArt_FernHoney now"]),
        # the ring of stakes stays to the end of its season
        t("FernStakesGone", "DAY_OF_MONTH 1, PLAYER_HAS_MAIL Current blinxxy.MuseArt_FernStakes Received",
          ["RemoveMail Current blinxxy.MuseArt_FernStakes"]),
        # 8 hearts outside summer: the event waits, and she writes
        t("FernNotYet", f"PLAYER_FRIENDSHIP_POINTS Current {FERN} 2000, {seen}{EV[6]}, !SEASON summer, !{seen}{EV[8]}",
          ["AddMail Current blinxxy.MuseArt_FernNotYet now"]),
        t("FernCuttingDays", f"{seen}{EV[8]}, !PLAYER_HAS_MAIL Current blinxxy.MuseArt_FernCutting Any",
          ["IncrementStat blinxxy.museart_ferncuttingdays 1"], once=False),
        t("FernCutting", "PLAYER_STAT Current blinxxy.museart_ferncuttingdays 3", ["AddMail Current blinxxy.MuseArt_FernCutting now"]),
        # the path, cleared overnight once her 10-heart event can happen
        t("FernPathCleared", f"PLAYER_FRIENDSHIP_POINTS Current {FERN} 2500, PLAYER_NPC_RELATIONSHIP Current {FERN} Dating, {seen}{EV[8]}",
          ["AddMail Current blinxxy.MuseArt_FernPathCleared received"]),
    ]
    return mail_entries, {e["Id"]: e for e in triggers}


def main():
    strings = Strings(os.path.join(PACK, "_none_"), [])          # (no file read: the dev i18n has comments)
    vale, cottage, farm = {}, {}, {}

    def put(where, name, kv):
        key, value = kv
        where[key] = strings.script(f"fern.event.{name}", value)
    put(vale, "heart2", heart2())
    put(vale, "heart4", heart4())
    for season in SEEDS:
        put(farm, "heart6", heart6(season))          # (the same lines, the same keys, each season)
    put(cottage, "heart8", heart8())
    put(vale, "heart10_some", heart10(True, "Sat"))
    put(vale, "heart10", heart10(False, "Sat"))
    trips = {}
    put(trips, "heart10_some", heart10(True, "Mon"))
    put(trips, "heart10", heart10(False, "Mon"))
    mail_entries, triggers = letters(strings)
    cutting_name = strings.add("fern.furniture.vine_cutting", "Night Vine Cutting")
    dialogue_gate = {"Tue8": EV[4], "Sat8": EV[6]}
    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/FernEventProps", "FromFile": "assets/fern/event_props.png"},
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/FernFurniture", "FromFile": "assets/fern/vine_cutting.png"},
        # (our maps' event lists start empty; Low, so another resident's own Load of one wins)
        *[{"Action": "Load", "Target": f"Data/Events/{loc}", "FromFile": "data/blank.json", "Priority": "Low"} for loc in (VALE, COTTAGE)],
        {"Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        {"LogName": "Fern's 10 hearts on a Monday, when she isn't in town", "Action": "EditData", "Target": f"Data/Events/{VALE}",
         "When": {**WHEN, "ValeResidentsTownTrips": "false"}, "Entries": trips},
        {"Action": "EditData", "Target": f"Data/Events/{COTTAGE}", "When": WHEN, "Entries": cottage},
        {"LogName": "Fern's 6-heart event, on your farm", "Action": "EditData", "Target": "Data/Events/Farm", "When": WHEN, "Entries": farm},
        {"Action": "EditData", "Target": "Data/mail", "When": WHEN, "Entries": mail_entries},
        {"Action": "EditData", "Target": "Data/TriggerActions", "When": WHEN, "Entries": triggers},
        # the vine cutting: a pot plant (decor, 1x2), not sold anywhere
        {"Action": "EditData", "Target": "Data/Furniture", "When": WHEN, "Entries": {
            "blinxxy.MuseArt_FernVineCutting": f"blinxxy.MuseArt_FernVineCutting/decor/1 2/1 1/1/500/-1/{cutting_name}/0/Mods\\blinxxy.MuseArt\\FernFurniture/true"}},
        # her Tue8 line (one path) leans on the 4-heart event, her Sat8 line (the rows) waits for the
        # 6: before then, those days fall back to her 6-heart lines
        *[{"LogName": f"Fern's {k} line waits for {ev}", "Action": "EditData", "Target": "Characters/Dialogue/FernAster",
           "Priority": "Late", "When": {**WHEN, "HasSeenEvent |contains=" + ev: "false"}, "Entries": {k: None}}
          for k, ev in dialogue_gate.items()],
        *map_patches(),
    ]
    header = ("// Fern Aster's heart events. Generated by tools/build_fern_events.py from the approved draft in\n"
              "// docs/vale_events.md: edit that script, not this file. Art: tools/art/fern_event_art.py (placeholders).\n")
    out = os.path.join(PACK, "data", "fern_events.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    # the text into i18n, as one block after Fern's own keys (this script's old keys out first)
    p = os.path.join(PACK, "i18n", "default.json")
    s = open(p, encoding="utf-8").read()
    s = re.sub(r'  "fern\.(event|letter\.(honey|notyet|cutting)|furniture)[.a-z0-9_]*": .*\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in strings.data.items())
    anchor = re.search(r'  "fern\.dialogue\.dating_FernAster": .*\n', s)
    s = s[:anchor.end()] + block + s[anchor.end():]
    open(p, "w", encoding="utf-8", newline="\n").write(s)
    n = sum(len(x) for x in (vale, trips, cottage, farm))
    print("wrote", out, f"({n} event entries,", len(strings.data), "lines)")


if __name__ == "__main__":
    main()

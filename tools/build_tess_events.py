"""Writes Tess Brand's heart events ([CP] Gilded Vale (dev)/data/tess_events.json) from the
approved draft in docs/tess_events.md (2-, 4- and 6-heart events approved by the user
2026-11-09). Built like Margot's and Sable's (build_margot_events.py, build_sable_events.py):
readable pieces, the text lifted into the pack's i18n (tess.event.*).

The dev pack's i18n has comments, so its lines are spliced in after "tess.name" rather than
the file being rewritten (as tools/tess_dialogue.py does).

Event IDs are fixed (the studio's stages and Bram's carving follow them):
blinxxy.MuseArt_TessHeart<N>.

Usage: python tools/build_tess_events.py
"""
import json, os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_sable_events import q, speak, script, quick

PACK = "[CP] Gilded Vale (dev)"
T = "TessBrand"
L = "Lupini"
B = "BeaThistle"
O, J = "OpalCorrie", "JetCorrie"
F = f"Friendship {T}"
VALE, STUDIO, HOUSE = "blinxxy.MuseArt_GildedVale", "blinxxy.MuseArt_ValeStudio3", "blinxxy.MuseArt_ValeHouse"
EV = {h: f"blinxxy.MuseArt_TessHeart{h}" for h in (2, 4, 6, 8, 10, 14)}
WHEN = {"ValeResidents": "true", "HasFile:assets/tess/sprites.png": "true", "HasFile:assets/tess/portraits.png": "true"}
TWINS = {"HasFile:assets/opal/sprites.png": "true", "HasFile:assets/jet/sprites.png": "true"}
OFF = "-100 -100"


def heart2():
    """2 hearts: the numeral, again (outside Studio III, a morning)."""
    # From Winter 7, Year 3 the carved III is up (data/vale_studios.json); she can't be at 2
    # hearts before Winter 1, Year 3, so only that winter's first week needs ruling out.
    key = (f"{EV[2]}/{F} 500/Time 800 1100/Weather sunny/"
           'G ANY "YEAR 4" "!SEASON winter" "DAY_OF_MONTH 7 28"')
    return key, script(
        "continue", "91 39", f"farmer 91 44 0 {T} 91 38 0", "skippable",
        # the stepladder against the studio's front (the Exhibition Hall's spare ladder art)
        "temporaryAnimatedSprite Mods\\blinxxy.MuseArt\\TessEventProps 0 0 16 48 9999 1 9999 91 36 false false 38 0 1 0 0 0",
        f"positionOffset {T} 0 -40",
        "pause 600", "move farmer 0 -4 0", "pause 300",
        "message " + q("Tess is up a stepladder against Studio III, a mallet in one hand and a gouge in the other, re-cutting the III over the door."),
        speak(T, "Don't stand under me. Chips.$0#$b#It's not right. The middle stroke's thinner than the other two. Nobody'd notice. I notice.$0"),
        f"shake {T} 500", "message " + q("The ladder wobbles. She grabs the frame."),
        speak(T, "I'm fine.$a"),
        quick([
            ("Let me hold the ladder.", [f"friendship {T} 30", speak(T, "I said I'm fine.$a"), f"shake {T} 400",
                                         "pause 400", speak(T, "...Hold the ladder.$u")]),
            ("It looks right to me.", [f"friendship {T} 15", speak(T, "That's because you're not the one who carved it.$u"),
                                       f"shake {T} 400", speak(T, "Hold the ladder. Please.$0")]),
            ("(Hold the ladder without asking.)", [f"friendship {T} 30",
                                                   "message " + q("She opens her mouth to argue. Shuts it. Keeps carving.")]),
        ]),
        "move farmer 0 -1 0", "pause 500",
        "message " + q("A few more strokes. She climbs down and stands back beside you, looking up at it for a long time."),
        f"positionOffset {T} 0 40", f"warp {T} 92 40", f"faceDirection {T} 0", "faceDirection farmer 0", "pause 1200",
        speak(T, "There. Now it's mine.$h"),
        f"faceDirection {T} 3",
        speak(T, "...Thanks. For the ladder. Don't tell anyone I needed it.$0"),
        "pause 300", "end",
    )


def heart4():
    """4 hearts: the way he holds it (Studio III, a morning)."""
    key = f"{EV[4]}/{F} 1000/Time 800 1100/!DayOfWeek Sun/SawEvent {EV[2]}"
    return key, script(
        "continue", "8 8", f"farmer 9 11 0 {T} 7 5 0 {L} 9 13 0", "skippable",
        "pause 400", "move farmer 0 -3 0", "move farmer -1 0 3", "faceDirection farmer 0",
        f"move {L} 0 -4 0", f"move {L} 1 0 1", f"move {L} 0 -1 0", f"faceDirection {L} 3",
        speak(L, "Forgive me, forgive me. The farmer said the new carver doesn't bite. I have been meaning to come and admire.$u"),
        speak(T, "The farmer says a lot of things. Mind the shavings.$0"),
        f"faceDirection {L} 0",
        "message " + q("Lupini watches her work. She takes long curls off a block of ash with a gouge, her thumb laid along the blade. He stops talking, which he never does."),
        "pause 1500",
        speak(L, "You hold it as if you were apologising to the wood.$s#$b#I knew someone who held a chisel like that. A long time ago.$s"),
        "pause 600",
        speak(L, "Good morning, Miss Brand. It is very good work.$0"),
        f"move {L} 0 3 2", f"move {L} -1 0 3", f"move {L} 0 2 2", f"warp {L} {OFF}",
        "pause 600",
        "message " + q("Tess has stopped carving."),
        f"faceDirection {T} 2",
        speak(T, "What does he know?$0#$b#He looked at my hands like he'd seen them before.$0"),
        quick([
            ("He's been in the Vale thirty years. He knew the old carvers.", [f"friendship {T} 30",
                speak(T, "...Right. Of course he did.$s"), "message " + q("She turns the gouge over in her fingers.")]),
            ("Ask him yourself.", [f"friendship {T} 15",
                speak(T, "I'm not asking Lupini anything. He'd make a speech.$a#$b#A long one.$u")]),
            ("Maybe all carvers hold a chisel that way.", [f"friendship {T} 30",
                speak(T, "Maybe.$u"), "message " + q("She doesn't believe it. Neither do you.")]),
        ]),
        f"faceDirection {T} 0",
        speak(T, "Stay if you like. Don't talk. Talking's for people who've finished.$0"),
        "pause 300", "end",
    )


def heart6(twins):
    """6 hearts: B.F. (Studio III, an evening; then the Vale House). With the Corrie twins once
    their art is in the pack (their branch merged); without them, Lupini takes Jet's line."""
    key = f"{EV[6]}/{F} 1500/Time 1900 2200/SawEvent {EV[4]}"
    # (offstage until the second scene: a starting position needs a facing, a warp doesn't)
    others = f" {B} {OFF} 2 {L} {OFF} 2" + (f" {J} {OFF} 2 {O} {OFF} 2" if twins else "")
    first = [
        "continue", "5 7", f"farmer 9 11 0 {T} 3 5 2{others}", "skippable",
        "pause 400", "move farmer 0 -4 0", "move farmer -6 0 3", "faceDirection farmer 0",
        "message " + q("The middle drawer of the old dresser is open. Tess is sitting on the floor in front of it, a roll of chisels in her lap and a bundle of letters tied with string."),
        speak(T, "Sit down. I need to show you something, and if I stand up I won't.$0"),
        "message " + q("She hands you a chisel, handle first, and turns your hand so the lamplight catches the steel. Stamped into it, small and worn: B.F."),
        speak(T, "Bram Fairlie. Studio III, thirty-one summers. It's on the sign by the lane.$s#$b#He was my grandfather. My mother's father. He died when I was a baby. I never met him.$s"),
        speak(T, "Mum never carved. She hardly ever talked about the Vale. When she cleared out the attic she gave me a box and said I was the only one who'd want it. His chisels, and his letters to her.$0#$b#Years of letters, about this room. The light. The other artists. A tree he never finished.$0"),
        f"faceDirection {T} 1", "pause 1200", f"faceDirection {T} 2",
        speak(T, "When the studio came up for sale I spent everything I had. Everything. And then I didn't tell anyone.$0"),
        quick([
            ("Why didn't you tell anyone?", [f"friendship {T} 30",
                speak(T, "Because then I'd be Bram's granddaughter. Everybody would be kind to me because of him.$s#$b#I wanted to know if I'd be any good here as just me.$0")]),
            ("Why are you telling me?", [f"friendship {T} 30",
                speak(T, "...Because you held the ladder.$l#$b#Don't make it a thing.$u")]),
            ("(Say nothing.)", [f"friendship {T} 30", "message " + q("She waits. Then she laughs, once."),
                speak(T, "That's the right answer. That's annoyingly the right answer.$u")]),
        ]),
        "message " + q("She takes a photograph in a plain frame out of the drawer: a big man in a leather apron on the step of Studio III, laughing at something out of frame. The apron is the one she's wearing."),
        "message " + q("She hangs it on the wall by the dresser, next to the others she's kept hidden."),
        speak(T, "There. He can have his wall back.$0"),
        "pause 800", "globalFade",
        # ---- the Vale House, later ----
        "viewport -1000 -1000", f"changeLocation {HOUSE}",
        "warp farmer 15 28", f"warp {T} 15 29", f"warp {B} 21 21", f"warp {L} 4 22",
        f"faceDirection {B} 2", f"faceDirection {L} 1", "faceDirection farmer 0", f"faceDirection {T} 0",
    ]
    if twins:
        first += [f"warp {J} 11 22", f"warp {O} 13 23", f"faceDirection {J} 1", f"faceDirection {O} 3"]
    first += [
        "viewport 14 24", "globalFadeToClear", "pause 400",
        "message " + q("The Vale House, after dark."),
        "move farmer 0 -3 0", f"move {T} 0 -3 0", "pause 300",
        speak(T, "I've got something to say. It won't take long.$0"),
    ]
    if twins:
        first += [
            speak(J, "Bram's chisels. I knew in your first week.$0#$b#I grew up next door to him. He used to let me sweep.$0"),
            f"faceDirection {O} 3",
            speak(O, "You KNEW? Jet! You let me make her soup for a whole winter without telling me she was family?$a"),
            speak(J, "You'd have made more soup.$0"),
        ]
    else:
        first += [
            speak(L, "Bram's chisels. I knew the morning I watched you work.$0#$b#I said nothing. It was not mine to say.$s"),
        ]
    first += [
        "message " + q("Tess's mouth does the thing it does when it surprises everyone."),
        speak(T, "...$h"),
        speak(L, "To Bram Fairlie, who once carved a whole chair to win an argument about chairs, and lost it.$u"),
        speak(B, "And to his granddaughter, who still owes me for eleven breakfasts.$h"),
        speak(T, "Twelve. I've been counting.$u"),
        "pause 300", "end",
    ]
    return key, script(*first)


def heart8():
    """8 hearts: leave it plain (the graveyard, an evening). The graveyard is the approved build on
    branch claude/vale-guide-grove (tools/vale_graveyard.py): the gate at (9-10, 13), the back row's
    stones with their feet on row 18 (Bram's the last, at x 12) and urns on row 19; the column x 11
    between his and Mabel's is open ground. She honours his wish (his echo: "leave it plain"): his name
    cut properly and one leaf, not a carved headstone. Bram's stone changes for good once this event's
    been seen (the graveyard's swap: tools/art/bram_carved_stone.py)."""
    key = f"{EV[8]}/{F} 2000/Time 1600 1800/Weather sunny/SawEvent {EV[6]}"
    return key, script(
        "continue", "10 15", f"farmer 10 9 2 {T} 8 11 2", "skippable",
        "pause 600", "move farmer 0 2 2", "faceDirection farmer 3", "pause 400",
        "message " + q("Tess is standing at the top of the trail above the graveyard, where she stops every evening. She doesn't turn round."),
        speak(T, "I come up here every night. I've never gone in.$0#$b#...Come with me?$0"),
        f"move {T} 1 0 1", f"move {T} 0 3 2", f"move {T} 2 0 1", f"move {T} 0 4 2", f"faceDirection {T} 1",
        "move farmer 0 3 2", "move farmer 1 0 1", "move farmer 0 3 2", "faceDirection farmer 1",
        "pause 600",
        "message " + q("Past Agnes's carved stone, and Hollis's, and Mabel's, to the plain one at the end of the row."),
        speak(T, "BRAM FAIRLIE. Lupini did that. With a palette knife. He told me he'd hated every letter.$s#$b#He was right. The R's backwards.$u"),
        "message " + q("She takes a letter out of her apron, unfolds it, and reads it to the stone."),
        speak(T, "'The top of the tree's the hard part, love. I keep leaving it. Every stone I carve, they want a line on it, and I pick each word with them and cut it deep. When it's my turn, leave mine plain. Scratch the name on and let the stone do the talking.'$0"),
        "pause 800",
        speak(T, "So he wanted it plain. And it is. And I hate it.$s"),
        quick([
            ("Then carve it. You're the only one who can.", [f"friendship {T} 30",
                speak(T, "That's what I keep thinking. And then I think, he asked.$s")]),
            ("Ask Lupini. He scratched the name.", [f"friendship {T} 30",
                speak(T, "I did. He said, 'He'd have hated my lettering more than any carving.' That's not an answer. It's Lupini.$u")]),
            ("He said plain. He didn't say badly.", [f"friendship {T} 30",
                "message " + q("A short laugh."), speak(T, "...No. He didn't, did he.$h")]),
        ]),
        "message " + q("She kneels in front of it and lays her hand flat over the scratched name, measuring."),
        speak(T, "Plain, then. His name, cut properly, deep enough to last. And one leaf, small, under it. Off the tree he never finished.$0#$b#That's not breaking his rule. That's just finishing his sentence.$l"),
        "pause 800", "globalFade",
        "message " + q("Every evening for a week, Tess goes back up the trail with her chisels."),
        "pause 300", "end",
    )


def heart10():
    """10 hearts: her chair (Studio III, an evening; you're dating). Her chair is the game's Birch
    Chair (TileSheets/furniture sprite 6), the one that stands at Bram's bench from the room's last
    stage. After the fade the carving's tiles are swapped for its finished state (the indices are
    tools/build_vale_buildings.py studio3's: bram_carving_5 on the shared sheet), so it's finished
    before the event ends; the room's patches keep it so afterwards."""
    key = f"{EV[10]}/{F} 2500/Time 1900 2200/Dating {T}/SawEvent {EV[8]}"
    return key, script(
        "continue", "8 7", f"farmer 9 11 0 {T} 7 7 1", "skippable",
        "temporaryAnimatedSprite TileSheets\\furniture 96 0 16 32 9999 1 9999 8 6 false false 8 0 1 0 0 0",
        "pause 400", "move farmer 0 -3 0", "faceDirection farmer 0",
        "message " + q("In the middle of the floor stands a new chair. Tess is beside it with her arms folded."),
        speak(T, "Don't say anything until you've looked at it. Then don't say anything until you've sat in it.$0"),
        "message " + q("A chair in pale birch, light and plain, its back very slightly curved."),
        speak(T, "I tried to make it his. Oak, heavy, the arms carved. It kept going wrong. Every time I stopped thinking about him it got better.$s#$b#So it's not his. It's the first thing I've made in here that isn't.$0"),
        speak(T, "Sit in it. You're the first. That's the point.$l"),
        "message " + q("You sit. It holds. It's comfortable, which surprises you, and her."),
        quick([
            ("It's perfect.", [f"friendship {T} 30", speak(T, "It's not. The left back leg's a hair short. But I'll take it.$u")]),
            ("It's very you.", [f"friendship {T} 30", speak(T, "Plain, stubborn and holds up under pressure? ...Yes. All right.$h")]),
            ("Can I keep sitting in it?", [f"friendship {T} 30", speak(T, "You can keep sitting in it forever. That came out wrong. Or right. Stop looking at me.$l")]),
        ]),
        f"move {T} 0 -2 0", f"move {T} 6 0 1", f"faceDirection {T} 0", "pause 400",
        speak(T, "And then there's this.$0"),
        "message " + q("She picks up a gouge, climbs onto the stool, and cuts the last leaves at the top of the tree while you watch. It takes the rest of the evening."),
        "globalFade", "pause 600",
        # the carving finished: bram_carving_5's tiles
        "changeMapTile Front 13 2 46", "changeMapTile Front 14 2 47", "changeMapTile Front 13 3 62",
        "changeMapTile Front 14 3 63", "changeMapTile Buildings 13 4 78", "changeMapTile Buildings 14 4 79",
        "globalFadeToClear", "pause 600", f"faceDirection {T} 2",
        speak(T, "There. Thirty years late.$s#$b#He'd have said I rushed the top.$h"),
        speak(T, "I came to the Vale for him. I'm staying for the light, and the soup, and the stupid tree, and... you, a bit. Mostly you.$l"),
        "pause 300", "end",
    )


def heart14(upgraded):
    """14 hearts: both names (your farmhouse, an evening; married). The farmhouse's maps differ by
    upgrade: the small house's main room (FarmHouse1: the door at 9,11) is the bigger house's
    (FarmHouse2, from the second upgrade on: the door at 27,30) moved 18 right and 19 down, so the
    event is written once for the small house and shifted for the bigger one, each behind the
    game's own upgrade check (both under the one event ID: seeing either is seeing it).
    The sign is her name sign's own sprite (TessCarvings 8, data/tess_shop.json), and you're
    given one: (F)blinxxy.MuseArt_TessNameSign."""
    dx, dy = (18, 19) if upgraded else (0, 0)

    def at(x, y):
        return f"{x + dx} {y + dy}"
    house = "PLAYER_FARMHOUSE_UPGRADE Current 2" if upgraded else "PLAYER_FARMHOUSE_UPGRADE Current 1 1"
    key = (f"{EV[14]}/G PLAYER_NPC_RELATIONSHIP Current {T} Married, {house}/{F} 3500/Time 1800 2200/"
           f"SawEvent {EV[10]}")
    return key, script(
        "continue", at(10, 8), f"farmer {at(12, 8)} 3 {T} {at(9, 10)} 0", "skippable",
        "pause 600", "playSound doorClose", "pause 300", f"move {T} 0 -2 1", "pause 300",
        "message " + q("Tess comes in with something long and flat wrapped in sacking, sets it down and stands back from it."),
        speak(T, "I made something for the farm. Don't laugh.$0#$b#Robin says I'm not allowed to call it furniture.$0"),
        "pause 300",
        # the sign, unwrapped, leaning between you (her name sign's own sprite)
        f"temporaryAnimatedSprite Mods\\blinxxy.MuseArt\\TessCarvings 0 16 32 16 9999 1 9999 {at(10, 8)} false false {8 + dy} 0 1 0 0 0",
        "message " + q("She pulls the sacking off: a slab of walnut, both your names carved into it in her letters, plain and deep, with a border of small leaves running round it. Not Bram's style. Hers."),
        speak(T, "I did yours first. Don't read into that.$u#$b#...Read into it.$l"),
        quick([
            ("Over the door.", [speak(T, "Where everyone can see it. Show-off. ...Good.$h")]),
            ("In here, where it's ours.", [speak(T, "Where only we see it. ...Yes. That's better.$l")]),
            ("Wherever you think.", [speak(T, "I think you're avoiding a decision. Over the door. No, in here. I'll decide in the morning.$u")]),
        ]),
        "pause 400",
        speak(T, "Every stone in that graveyard has a name cut into it by somebody who loved them.$0#$b#I wanted one with two names on it that nobody's finished yet.$l"),
        "pause 300", "addItem (F)blinxxy.MuseArt_TessNameSign 1", "pause 300", "end",
    )


def splice(i18n):
    path = os.path.join(PACK, "i18n", "default.json")
    s = open(path, encoding="utf-8").read()
    s = re.sub(r'  "tess\.event\.[^"]*": "(?:[^"\\]|\\.)*",\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in i18n.items())
    anchor = '  "tess.name": "Tess",\n'
    assert anchor in s
    open(path, "w", encoding="utf-8").write(s.replace(anchor, anchor + block, 1))


def lift(prefix, text, store):
    """Every quoted line and each quickQuestion's question and answers into i18n keys
    <prefix>.01, .02... (cp_i18n.Strings.script, without its whole-file rewrite)."""
    n = 0

    def key():
        nonlocal n
        n += 1
        return f"{prefix}.{n:02}"

    def add(t):
        k = key()
        store[k] = t
        return "{{i18n:" + k + "}}"

    def sub(m):
        if m.group(1) is not None:
            return '"' + (add(m.group(1)) if m.group(1).strip() else m.group(1)) + '"'
        parts = m.group(2).split("#")
        return "quickQuestion " + "#".join(add(p) if p.strip() else p for p in parts) + "(break)"

    return re.sub(r'"([^"]*)"|quickQuestion (.*?)\(break\)', sub, text)


def main():
    i18n = {}
    k2, s2 = heart2()
    k4, s4 = heart4()
    k6, s6 = heart6(True)
    k6b, s6b = heart6(False)
    k8, s8 = heart8()
    k10, s10 = heart10()
    vale = {k2: lift("tess.event.heart2", s2, i18n), k8: lift("tess.event.heart8", s8, i18n)}
    studio = {k4: lift("tess.event.heart4", s4, i18n), k10: lift("tess.event.heart10", s10, i18n)}
    with_twins = {k6: lift("tess.event.heart6", s6, i18n)}
    without = {k6b: lift("tess.event.heart6_alone", s6b, i18n)}
    k14, s14 = heart14(False)
    k14b, s14b = heart14(True)
    farmhouse = {k14: lift("tess.event.heart14", s14, i18n)}
    farmhouse[k14b] = lift("tess.event.heart14", s14b, {})       # the same lines, the same keys
    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/TessEventProps", "FromFile": "assets/tess/event_props.png"},
        # (the Vale's and Studio III's event lists start empty: ours. Low, so another Vale
        # resident's events can load the Vale's list too)
        {"Action": "Load", "Target": f"Data/Events/{VALE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"Action": "Load", "Target": f"Data/Events/{STUDIO}", "FromFile": "data/blank.json"},
        {"Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        {"Action": "EditData", "Target": f"Data/Events/{STUDIO}", "When": WHEN, "Entries": studio},
        {"LogName": "Tess's 14-heart event, in your farmhouse", "Action": "EditData", "Target": "Data/Events/FarmHouse",
         "When": WHEN, "Entries": farmhouse},
        {"LogName": "Tess's 6-heart event, with the Corrie twins", "Action": "EditData", "Target": f"Data/Events/{STUDIO}",
         "When": {**WHEN, **TWINS}, "Entries": with_twins},
        {"LogName": "Tess's 6-heart event, before the twins are in the pack", "Action": "EditData", "Target": f"Data/Events/{STUDIO}",
         "When": {**WHEN, "HasFile:assets/opal/sprites.png": "false"}, "Entries": without},
    ]
    out = os.path.join(PACK, "data", "tess_events.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write("// Tess Brand's heart events. Generated by tools/build_tess_events.py from the approved\n"
                "// draft in docs/tess_events.md: edit that script, not this file.\n")
        json.dump({"Changes": changes}, f, indent=2, ensure_ascii=False)
        f.write("\n")
    splice(i18n)
    print("wrote", out, "with", len(i18n), "lines")


if __name__ == "__main__":
    main()

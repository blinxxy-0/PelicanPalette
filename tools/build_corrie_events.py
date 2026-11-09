"""Writes Opal and Jet Corrie's heart events ([CP] Gilded Vale (dev)/data/corrie_events.json).

Built from readable pieces like Sable's and Margot's (build_sable_events.py). The story is the
approved draft in docs/corrie_twins_events.md (approved 2026-11-09). The game walks every
scene; the player only answers. Their text goes into the dev pack's i18n as corrie-event.*
(lifted by cp_i18n.Strings, then written in as text: that file has comments).

Each event plays where that twin is on their schedule (data/opal_schedule.json,
jet_schedule.json). The events that change Studio II set a mail flag the room's heart stages
follow (tools/build_vale_buildings.py STUDIO2_STAGES): Opal's 6 (blinxxy.MuseArt_Opal6Seen),
Jet's 8 and 10 (blinxxy.MuseArt_Jet8Seen, _Jet10Seen). Jet's 8 and 10 change the room on screen
too (removeTile, changeMapTile; the tiles come from data/vale_studio2.json).

Opal's 6 and 10 have two versions each: the Exhibition Hall reopened (Lupini's 8 hearts, the mail
flag blinxxy.MuseArt_ValeHallOpen) or still shut (docs/design/vale_newcomers.md, section 3).
Whichever plays first sets blinxxy.MuseArt_Opal6Seen / _Opal10Seen, and the other then never plays.

The keepsakes are furniture (tools/corrie_furniture.py): Jet's blue offcut (his 2), Opal's spring
window (her 10), the window Jet drew for your farmhouse (his 10). Opal's cracked sunflower hangs in
Studio II after her 8 (the stage "opal8").

Usage: build_corrie_events.py        (from the repository root)
"""
import json, os, re, sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cp_i18n import Strings
from build_sable_events import q, speak, script, quick

PACK = "[CP] Gilded Vale (dev)"
O, J = "OpalCorrie", "JetCorrie"
S2, VALE, HALL, HOUSE = ("blinxxy.MuseArt_ValeStudio2", "blinxxy.MuseArt_GildedVale",
                         "blinxxy.MuseArt_ValeHall", "blinxxy.MuseArt_ValeHouse")
HALL_OPEN = "G PLAYER_HAS_MAIL Current blinxxy.MuseArt_ValeHallOpen Any"
HALL_SHUT = "G !PLAYER_HAS_MAIL Current blinxxy.MuseArt_ValeHallOpen Any"
PROPS = "Mods\\blinxxy.MuseArt\\CorrieEventProps"         # 32x32 each: 0 spring light, 1 Mabel's dawn light
FURN = "TileSheets\\furniture"
GIFT = {k: f"(F)blinxxy.MuseArt_Corrie{k}" for k in ("BlueOffcut", "SpringWindow", "FarmWindow")}
LAMPS = {"modern": (112, 720), "ornate": (480, 864)}       # Data/Furniture 1447, 1758: the city's chrome and red


def F(npc, hearts):
    return f"Friendship {npc} {hearts * 250}"


def mail(flag):
    return f"G PLAYER_HAS_MAIL Current blinxxy.MuseArt_{flag} Any"


def no_mail(flag):
    return f"G !PLAYER_HAS_MAIL Current blinxxy.MuseArt_{flag} Any"


def sprite(texture, x, y, w, h, tx, ty, sort):
    """A still prop for the scene (as Margot's desk painting)."""
    return f"temporaryAnimatedSprite {texture} {x} {y} {w} {h} 9999 1 9999 {tx} {ty} false false {sort} 0 1 0 0 0"


def light(n, tx, ty):
    """Coloured light lying on the floor: below everything standing on it."""
    return sprite(PROPS, n * 32, 0, 32, 32, tx, ty, 0)


def say(line):
    return "message " + q(line)


def stage_tiles(stage):
    """The tiles a Studio II heart stage changes (data/vale_studio2.json), as event commands."""
    text = open(os.path.join(PACK, "data", "vale_studio2.json"), encoding="utf-8").read()
    data = json.loads(re.sub(r"^\s*//.*$", "", text, flags=re.M))
    edit = next(c for c in data["Changes"] if c.get("LogName") == f"Studio II: {stage}")
    cmds = []
    for t in edit["MapTiles"]:
        x, y = t["Position"]["X"], t["Position"]["Y"]
        if t.get("Remove"):
            cmds.append(f"removeTile {x} {y} {t['Layer']}")
        elif "SetIndex" in t:
            assert t["SetTilesheet"] == "z_blinxxy.MuseArt_ValeStudio2Interior", t
            cmds.append(f"changeMapTile {t['Layer']} {x} {y} {t['SetIndex']}")
    return cmds


def events():
    studio, vale, hall, house = {}, {}, {}, {}

    # ================================================================ Opal

    # ---- 2 hearts: the city lamps (Studio II, a morning) ------------------------------
    studio[f"blinxxy.MuseArt_Opal2h/{F(O, 2)}/Time 800 1300"] = script(
        "continue", "10 8", f"farmer 7 12 0 {O} 7 6 2 {J} 14 10 0", "skippable",
        # (the game's own lamps out of the crate, standing in the straw: its Modern and Ornate Lamps,
        # the fanciest it has; the Box and Classic Lamps read as a cupboard and a green shade, the user)
        sprite(FURN, *LAMPS["modern"], 16, 48, 5, 5, 7), sprite(FURN, *LAMPS["ornate"], 16, 48, 9, 5, 7),
        "pause 600", "move farmer 0 -3 0", "pause 300",
        say("A packing crate in the middle of Opal's half, straw everywhere. Opal is on her knees in it, lifting out lamps: chrome, brass, a glass globe the size of a melon. Jet is at his bench, not looking."),
        speak(O, "The last of the flat in Zuzu City. Jet said leave them. I said, Jet, they're Italian.$u"),
        say("She stands up with the biggest one: a chandelier dripping crystal."),
        speak(O, "You! You have a house. Take one. Take this one.$h"),
        quick([
            ("Where would I even put it?", [
                say("She looks at the chandelier, at you, at the chandelier, and laughs until she has to sit down on the crate."),
                speak(O, "Over the cows! They'd look wonderful.$u")]),
            ("It's a bit much for a farmhouse.", [
                speak(O, "It is, isn't it. Everything we owned was a bit much. That was rather the point.$s"),
                say("Then she laughs at herself.")]),
            ("I'll take it!", [
                speak(O, "Darling!$h"),
                f"faceDirection {J} 0", speak(J, "It won't go through a farmhouse door.$0"),
                speak(O, "...It didn't go through ours either. They brought it in through the window.$u")]),
        ]),
        "pause 400",
        say("She puts the chandelier back in the straw and digs out the smallest thing in the crate."),
        speak(O, "Here. The little one. It's the only one that was ever any use.$l"),
        "addItem (F)1751",
        "pause 300", "end",
    )

    # ---- 4 hearts: a letter (Studio II, a morning) -------------------------------------
    studio[f"blinxxy.MuseArt_Opal4h/{F(O, 4)}/Time 800 1300/SawEvent blinxxy.MuseArt_Opal2h"] = script(
        "continue", "8 9", f"farmer 7 12 0 {O} 4 9 2 {J} 14 10 0", "skippable",
        "pause 600", "move farmer 0 -1 0", "move farmer -2 0 3", "faceDirection farmer 0", "pause 300",
        say("Opal is behind the counter with a letter in one hand and a glossy catalogue in the other. Jet is cutting. She reads aloud, in a voice:"),
        speak(O, "'Corrie Classic. The authentic stained-glass look. Self-adhesive.' Self-adhesive!$a"),
        speak(O, "The firm that bought us. They've put our name on a sticker. Mabel's name. On a sticker you put on a bathroom window.$a"),
        f"move {O} -2 0 3", f"move {O} 2 0 1", f"faceDirection {O} 2",
        quick([
            ("You could sue them.", [
                speak(O, "With what? We sold them the name. Signed it away with a pen that cost more than my first kiln.$u")]),
            ("Does it matter what they call it?", [
                speak(O, "It's our mother's name!$a"), "pause 600",
                speak(O, "...No. You're right. It doesn't. That's the awful part.$s")]),
            ("Make something better.", [
                speak(O, "That's what Jet said. In fewer words.$l")]),
        ]),
        "pause 400", speak(J, "Opal.$0"),
        "pause 800", say("She stops. She looks at the catalogue for a long moment."),
        speak(O, "It's not ours any more. That was the point.$s"),
        say("She drops it in the bin."),
        speak(O, "Right. Tea. You'll have tea.$h"),
        "pause 300", "end",
    )

    # ---- 6 hearts: the Hall's windows (the plaza; or the Hall, once it's open) ---------
    six = ["addMailReceived blinxxy.MuseArt_Opal6Seen"]
    vale[f"blinxxy.MuseArt_Opal6h/{F(O, 6)}/Time 1300 1530/!Weather rainy/SawEvent blinxxy.MuseArt_Opal4h/{HALL_SHUT}/{no_mail('Opal6Seen')}"] = script(
        "continue", "37 40", f"farmer 38 44 0 {O} 36 41 0", "skippable",
        "pause 600", "move farmer 0 -2 0", "pause 300", f"faceDirection {O} 1",
        say("Opal is standing in front of the Exhibition Hall with a sketchbook held up against the light, squinting from it to the dusty windows and back."),
        speak(O, "There you are! Stand here. No, here. Now look up.$h"),
        "move farmer -1 0 3", "faceDirection farmer 0", f"faceDirection {O} 0", "pause 600",
        speak(O, "Those windows went in the year we were born. Plain glass, cheap, because the Vale couldn't afford anything else. Mabel said she'd do them properly one day. She never got round to it.$0"),
        f"faceDirection {O} 1",
        say("She turns the sketchbook round: the Hall's windows redrawn, each one a season of the Vale, in coloured pencil."),
        speak(O, "Spring, summer, fall, winter. Four windows. I've been drawing them for forty years. On napkins, in meetings. On a contract once.$l"),
        quick([
            ("They're beautiful.", [speak(O, "They are, aren't they! I'm not even going to pretend to be modest.$h")]),
            ("Who would you have to ask?", [speak(O, "Lupini, I suppose. He'd say yes and then forget. And somebody would have to open the place first.$u")]),
            ("Make them anyway.", [speak(O, "...For a building nobody goes in. Yes. Why not.$l")]),
        ]),
        "pause 400", say("She closes the sketchbook."),
        speak(O, "Twenty years ago I'd have had a team of forty on this by Friday. Now it's me, and Jet, and our hands. If they still work.$s"),
        *six, "pause 300", "end",
    )
    # (the Hall open: inside, on one of their Hall days, with Lupini across the reception desk)
    hall[f"blinxxy.MuseArt_Opal6hHall/{F(O, 6)}/DayOfWeek Tue Thu/Time 900 1700/SawEvent blinxxy.MuseArt_Opal4h/{HALL_OPEN}/{no_mail('Opal6Seen')}"] = script(
        "continue", "14 17", f"farmer 14 21 0 {O} 14 17 0 Lupini 14 15 2", "skippable",
        "pause 600", "move farmer 0 -2 0", "move farmer -2 0 3", "faceDirection farmer 1", "pause 300",
        say("Opal has her sketchbook open on the reception desk: the Hall's windows redrawn, each one a season of the Vale, in coloured pencil. Lupini is pretending not to be impressed."),
        speak(O, "Spring, summer, fall, winter. Four windows. I've been drawing them for forty years. On napkins, in meetings. On a contract once.$l"),
        speak("Lupini", "And what would four windows cost a poor gallery?$u"),
        speak(O, "For you? Far too much. And you'd be getting a bargain, and you'd know it, and you'd...$h"),
        speak("Lupini", "Yes.$h"),
        speak(O, "I haven't finished.$a"),
        speak("Lupini", "Yes. Do it. All four.$h"),
        f"faceDirection {O} 3",
        speak(O, "He had one coat and no money when we met him. Now look at him, running a gallery.$u"),
        "faceDirection Lupini 3",
        speak("Lupini", "She hasn't changed. Not one bit. Thank goodness.$h"),
        *six, "pause 300", "end",
    )

    # ---- 8 hearts: her first panel (Studio II, an evening) -----------------------------
    studio[f"blinxxy.MuseArt_Opal8h/{F(O, 8)}/Time 1900 2200/{mail('Opal6Seen')}"] = script(
        "continue", "13 8", f"farmer 7 12 0 {O} 16 6 2 {J} -100 -100 2", "skippable",
        "pause 600", "move farmer 0 -5 0", "move farmer 7 0 1", "pause 300",
        say("The kiln is open, still ticking as it cools. Opal is standing in front of it holding a small panel in both hands: a sunflower, every piece cut and leaded by her, and a crack straight across the middle."),
        f"faceDirection {O} 3",
        speak(O, "My first one. All mine, start to finish. The first in twenty years. And it cracked in the kiln.$s"),
        speak(O, "Do you know what I'm frightened of? That my hands have forgotten. That we didn't come home to make things. We came home because they sold us, and we had nowhere else to go, and I'm just very good at talking about it.$s"),
        quick([
            ("One crack doesn't mean your hands forgot.", [speak(O, "No. It means I rushed the cooling. I know that. I knew it this morning.$l")]),
            ("Show me how you made it.", [
                say("She does: the cartoon, the cutting, the lead. Halfway through she stops."),
                speak(O, "...I do remember. Don't I.$l")]),
            ("You came home because you wanted to.", [speak(O, "Did I? I'd like to believe that.$s")]),
        ]),
        "pause 600", f"warp {J} 7 12", f"faceDirection {J} 0", "pause 800",
        say("In the doorway behind her is Jet, back from somewhere, holding his coat. He heard. He doesn't say anything."),
        f"move {J} 0 -6 0", f"move {J} 8 0 1", f"faceDirection {J} 1", f"faceDirection {O} 3",
        say("He takes the cracked panel from her gently and holds it up to the lamp. The crack throws a thin bright line across the floor."),
        speak(J, "Mabel's first one cracked too. She kept it.$l"),
        say("Opal laughs, crying a bit."),
        speak(O, "She did! It's in the loft somewhere. Oh, Jet.$u"),
        "addMailReceived blinxxy.MuseArt_Opal8Seen", "pause 300", "end",
    )

    # ---- 10 hearts: morning light (the Hall at sunrise; or Bea's tea room) -------------
    ten = [say("On the way out, Opal presses a small parcel into your hands: a window of your own. Spring, the first of the four."),
           f"addItem {GIFT['SpringWindow']}", "addMailReceived blinxxy.MuseArt_Opal10Seen"]
    hall[f"blinxxy.MuseArt_Opal10hHall/{F(O, 10)}/Time 600 800/{mail('OpalLetterHall')}/{no_mail('Opal10Seen')}"] = script(
        "continue", "14 18", f"farmer 14 21 0 {O} 13 18 0 {J} 15 18 0", "skippable",
        "pause 600", "move farmer 0 -2 0", "pause 300",
        say("The gallery is empty and dim. Opal and Jet are standing in the middle of the floor, not talking, which you've never seen."),
        "pause 1200",
        say("The sun comes up. Through the first of the new windows, above the door behind you, colour comes across the floor: a pool of spring green and gold that slides slowly over the tiles as the sun climbs."),
        light(0, 13, 19), "pause 1000",
        speak(O, "Spring. The first of the four.$l"),
        "pause 1200",
        speak(J, "It's good, Opal.$l"),
        f"faceDirection {O} 2", f"faceDirection {J} 2",
        speak(O, "It's ours.$h"),
        say("She takes your arm on one side and Jet's on the other."),
        speak(O, "All three of ours. You held the ladder.$h"),
        quick([
            ("It's the most beautiful thing in the Vale.", [speak(O, "Don't tell Lupini.$u")]),
            ("Mabel would be proud.", [speak(O, "...She'd say the lead's too thick. And then she'd cry.$l")]),
            ("(Watch the light.)", [say("Nobody says anything until it's reached the far wall.")]),
        ]),
        *ten, "pause 300", "end",
    )
    house[f"blinxxy.MuseArt_Opal10h/{F(O, 10)}/Time 600 800/{mail('OpalLetterHouse')}/{no_mail('Opal10Seen')}"] = script(
        "continue", "16 24", f"farmer 15 28 0 {O} 16 23 0 {J} 17 23 0 BeaThistle -100 -100 2", "skippable",
        "pause 600", "move farmer 0 -4 0", "pause 300",
        say("The tea room is empty and dim. Opal and Jet are standing by the tea tables, not talking, which you've never seen. In the wall above them is a new window."),
        "pause 1200",
        say("The sun comes up. Colour comes through the new window and across the tea tables: spring green and gold, sliding slowly as the sun climbs."),
        light(0, 15, 25), "pause 1000",
        speak(O, "Spring. The first of the four.$l"),
        "pause 1200",
        speak(J, "It's good, Opal.$l"),
        f"faceDirection {O} 2", f"faceDirection {J} 2",
        speak(O, "It's ours.$h"),
        say("She takes your arm on one side and Jet's on the other."),
        speak(O, "All three of ours. You held the ladder.$h"),
        "warp BeaThistle 18 24", "faceDirection BeaThistle 3", "pause 400",
        say("Bea brings three cups of tea and a fourth for herself, and sits down, which she never does."),
        speak("BeaThistle", "My aunt always wanted a window like that.$0"),
        speak(O, "I know. She told me. I was nine.$l"),
        *ten, "pause 300", "end",
    )

    # ================================================================ Jet

    # ---- 2 hearts: the seedhouse (the Vale, at dawn) -----------------------------------
    vale[f"blinxxy.MuseArt_Jet2h/{F(J, 2)}/Time 600 800/!Weather rainy"] = script(
        "continue", "18 38", f"farmer 17 41 0 {J} 19 37 3 FernAster -100 -100 2", "skippable",
        "pause 600", "move farmer 0 -2 0", "move farmer 1 0 1", "faceDirection farmer 0", "pause 300",
        say("Mist on the gardens. Jet is at the side of Fern's seedhouse with one pane out of its frame, a putty knife in his hand. He doesn't turn round."),
        speak(J, "Hm. Early.$0"),
        "pause 800", say("He fits a new pane, runs his thumb along the putty, and steps back to look. A long quiet."),
        quick([
            ("Does Fern know you do this?", [speak(J, "She thinks the wind's very careful.$u")]),
            ("Can I help?", [speak(J, "Hold this. Don't press.$0"), say("You hold the pane while he putties it in."), speak(J, "Good.$h")]),
            ("(Watch him work.)", ["pause 1200", speak(J, "Old glass. Somebody cared about this house once.$l")]),
        ]),
        f"faceDirection {J} 2", "pause 400",
        say("He takes an offcut of blue glass from his apron and holds it out to you."),
        speak(J, "Keep it in a window.$l"),
        f"addItem {GIFT['BlueOffcut']}",
        "globalFade 0.02", f"warp {J} -100 -100", "warp FernAster 17 39", "faceDirection FernAster 0",
        "globalFadeToClear 0.02", "pause 400",
        say("The seedhouse door opens behind you and Fern looks out, yawning, at a pane that's whole. By then he's gone."),
        "pause 300", "end",
    )

    # ---- 4 hearts: Mabel's bench (Studio II, an afternoon) -----------------------------
    studio[f"blinxxy.MuseArt_Jet4h/{F(J, 4)}/Time 1300 1700/SawEvent blinxxy.MuseArt_Jet2h"] = script(
        "continue", "10 7", f"farmer 7 12 0 {J} 10 5 0", "skippable",
        "pause 600", "move farmer 0 -5 0", "move farmer 1 0 1", "faceDirection farmer 0", "pause 300",
        say("Jet is at Mabel's bench with a cloth, wiping it down, though it's clean. Her pliers are laid out as they always are."),
        speak(J, "Her bench.$0"), "pause 800",
        speak(J, "I learned under it. Picking up offcuts. Then on her knee. Then beside her.$0"),
        speak(J, "She'd put my hand on the cutter and press. 'Like you mean to break it, and you won't.'$l"),
        quick([
            ("Sit down. Try it.", [say("He puts a hand on the stool, then takes it away."), speak(J, "Not yet.$s")]),
            ("She sounds like a good teacher.", [speak(J, "She was. Opal got the talking. I got the hands.$l")]),
            ("(Say nothing.)", ["pause 1500", speak(J, "You're the only one who doesn't ask.$h")]),
        ]),
        "pause 400", say("He folds the cloth, squares her pliers by a fraction, and goes back to the light table."),
        f"move {J} 3 0 1", f"faceDirection {J} 0",
        "pause 300", "end",
    )

    # ---- 6 hearts: the loft (Studio II, an afternoon) -----------------------------------
    studio[f"blinxxy.MuseArt_Jet6h/{F(J, 6)}/Time 1300 1700/SawEvent blinxxy.MuseArt_Jet4h"] = script(
        "continue", "13 7", f"farmer 7 12 0 {J} 15 5 2", "skippable",
        "pause 600", "move farmer 0 -5 0", "move farmer 7 0 1", "faceDirection farmer 0", "pause 300",
        say("Jet is coming down the ladder from the loft with a box on his shoulder, coughing in the dust. More boxes are stacked at its foot."),
        speak(J, "Opal's lamps need the room.$u"),
        speak(J, "Sort. Keep, or bin.$0"),
        say("He hands you a box. It's full of things the twins left behind twenty years ago: a cracked mug, their school reports, a pair of tiny safety glasses."),
        "pause 600",
        say("At the bottom: a book with a cloth cover, its corners worn round. Jet goes still."),
        speak(J, "Her pattern book.$l"),
        say("He turns the pages: windows, years of them, each drawn and coloured and signed M.C. The last page is a window of the Vale at dawn, the sun coming up over gold trees, the river running down the middle. It's half coloured in. The other half is pencil."),
        speak(J, "She never finished it. She was ill by then. We left it in the loft when we boarded up.$s"),
        quick([
            ("You could finish it.", [say("He looks at you a long time."), speak(J, "...Not yet.$s")]),
            ("Will you show Opal?", [speak(J, "She'll cry. Then she'll want to change it.$u")]),
            ("(Say nothing.)", [say("He closes the book very carefully.")]),
        ]),
        "pause 400",
        f"move {J} -4 0 3", f"faceDirection {J} 0", "pause 600",
        say("He puts the book on Mabel's bench, square to its edge, and goes back up the ladder."),
        f"move {J} 4 0 1", f"faceDirection {J} 0", "pause 300", f"warp {J} -100 -100",
        "addMailReceived blinxxy.MuseArt_JetPatternBook", "pause 300", "end",
    )

    # ---- 8 hearts: the line (Studio II, an evening) -------------------------------------
    studio[f"blinxxy.MuseArt_Jet8h/{F(J, 8)}/Time 1900 2100/SawEvent blinxxy.MuseArt_Jet6h"] = script(
        "continue", "10 7", f"farmer 7 12 0 {O} 8 7 1 {J} 13 7 3", "skippable",
        "pause 400",
        say("Raised voices before the door's even shut. Opal on her side of the line, Jet on his, for real this time: a crate of her sketches has slid over the line into his offcuts."),
        "move farmer 0 -2 0", "faceDirection farmer 0",
        speak(O, "It's three inches, Jet! Three inches of floor!$a"),
        speak(J, "It's my floor.$a"),
        speak(O, "It's Mabel's floor! You drew that line in the first week of the company and you've been standing behind it ever since!$a"),
        "pause 1200", f"faceDirection {J} 2", "pause 400",
        quick([
            ("Work at Mabel's bench. Both of you.", [speak(O, "We can't. It's hers.$s"), speak(J, "It's in the middle.$0")]),
            ("Rub out a bit of the line.", [say("Opal laughs despite herself."), speak(O, "With what? It's paint. Jet painted it. Of course he did.$u")]),
            ("Why did you draw the line?", [speak(J, "So I'd have somewhere that was mine.$s")]),
        ]),
        "pause 600",
        say("Jet gets a scraper from his bench and, without a word, scrapes a gap in the line by Mabel's bench."),
        f"move {J} -2 0 3", f"move {J} 0 -1 0", f"faceDirection {J} 2", "pause 400",
        *stage_tiles("jet8"), "pause 800",
        speak(J, "I never wanted to leave.$s"),
        speak(O, "...I know. I've always known. I went anyway, and you came, and I never once asked you.$s"),
        speak(J, "You didn't have to ask.$l"),
        say("Opal steps through the gap into the middle. After a moment, so does he."),
        f"move {O} 1 0 1", f"move {O} 0 -1 0", f"move {J} -1 0 3", f"faceDirection {O} 1", f"faceDirection {J} 3",
        "addMailReceived blinxxy.MuseArt_Jet8Seen", "pause 600", "end",
    )

    # ---- 10 hearts: the window (Studio II, a morning) -----------------------------------
    studio[f"blinxxy.MuseArt_Jet10h/{F(J, 10)}/Time 600 900/{mail('JetLetter')}"] = script(
        "continue", "10 7", f"farmer 7 12 0 {J} 10 5 0 {O} 4 6 2", "skippable",
        "pause 600", "move farmer 0 -4 0", "move farmer 2 0 1", "faceDirection farmer 0", "pause 300",
        say("Jet is sitting at Mabel's bench. You've never seen anyone sit there. Her pattern book is open at the last page; beside it, the window, finished: the Vale at dawn, the sun over the gold trees, the river."),
        f"faceDirection {J} 2",
        speak(J, "Finished it last night. Her lines. My cuts.$l"),
        f"move {O} 4 0 1", f"faceDirection {O} 1", "pause 600",
        say("Opal comes out in her dressing gown, sees him at the bench, and stops. She doesn't say anything at all, which is how you know."),
        say("Jet stands, lifts the panel, and the three of you carry it to the big window. He fits it into the frame himself."),
        "globalFade 0.015", *stage_tiles("jet10"), "globalFadeToClear 0.015",
        f"faceDirection {J} 0", f"faceDirection {O} 0", "pause 800",
        say("The sun comes through it. The Vale at dawn lands on the floor across the middle of the studio, right where the line used to be."),
        light(1, 9, 6), "pause 1000",
        speak(O, "Oh, Mum.$l"),
        f"faceDirection {J} 2",
        speak(J, "Thank you for holding the ladder.$h"),
        quick([
            ("She'd be proud of you both.", [speak(O, "She'd say we took our time.$s"), speak(J, "We did.$u")]),
            ("Now you can make your own.", [speak(J, "Already started.$h"), say("He shows you a drawing: a window for your farmhouse.")]),
            ("(Look at the light.)", [say("Nobody says anything for a long time.")]),
        ]),
        say("Before you go, Jet hands you a small window wrapped in newspaper: the one he drew for your farmhouse."),
        f"addItem {GIFT['FarmWindow']}",
        "addMailReceived blinxxy.MuseArt_Jet10Seen", "pause 300", "end",
    )
    return studio, vale, hall, house


def props(path):
    """Two pools of coloured light for the 10-heart events: spring (Opal's first Hall window)
    and Mabel's Vale at dawn. Soft, so the floor shows through."""
    im = Image.new("RGBA", (64, 32))
    d = ImageDraw.Draw(im, "RGBA")
    for n, colours in enumerate(([(126, 206, 116), (250, 206, 96), (160, 220, 120), (240, 220, 140)],
                                 [(246, 168, 120), (250, 206, 96), (104, 148, 236), (126, 206, 116)])):
        for k, c in enumerate(colours):
            x = n * 32 + 2 + k * 7
            d.polygon([(x, 2), (x + 6, 2), (x + 9, 29), (x + 3, 29)], fill=c + (100,))
            d.polygon([(x + 1, 6), (x + 5, 6), (x + 7, 25), (x + 3, 25)], fill=c + (90,))
    im.save(path)


LETTERS = {
    # (from the approved draft; the tea room's when the Hall's still shut)
    "OpalLetterHall": ("Come to the Hall at six. Don't be late, the sun won't wait.^^   -Opal", "From Opal",
                       f"PLAYER_FRIENDSHIP_POINTS Current {O} 2500, PLAYER_HAS_MAIL Current blinxxy.MuseArt_Opal8Seen Any, PLAYER_HAS_MAIL Current blinxxy.MuseArt_ValeHallOpen Any"),
    "OpalLetterHouse": ("Come to Bea's at six. Don't be late, the sun won't wait.^^   -Opal", "From Opal",
                        f"PLAYER_FRIENDSHIP_POINTS Current {O} 2500, PLAYER_HAS_MAIL Current blinxxy.MuseArt_Opal8Seen Any, !PLAYER_HAS_MAIL Current blinxxy.MuseArt_ValeHallOpen Any"),
    "JetLetter": ("Come tomorrow. Early.^^   -J", "From Jet",
                  f"PLAYER_FRIENDSHIP_POINTS Current {J} 2500, PLAYER_HAS_MAIL Current blinxxy.MuseArt_Jet8Seen Any"),
}


def main():
    studio, vale, hall, house = events()
    strings = Strings(os.path.join(PACK, "_none_"), [])          # (no file read: the dev i18n has comments)
    for scripts in (studio, vale, hall, house):
        for key, value in scripts.items():
            name = key.split("/")[0].split("_")[-1].lower()
            scripts[key] = strings.script("corrie-event." + name, value)
    mail_entries, triggers = {}, {}
    for flag, (text, title, condition) in LETTERS.items():
        mail_entries[f"blinxxy.MuseArt_{flag}"] = (f"[letterbg Mods/blinxxy.MuseArt/ValeLetter 0]{strings.add('corrie-letter.' + flag.lower(), text)}"
                                                   f"[#]{strings.add('corrie-letter.' + flag.lower() + '.title', title)}")
        triggers[f"blinxxy.MuseArt_{flag}"] = {"Id": f"blinxxy.MuseArt_{flag}", "Trigger": "DayStarted", "Condition": condition,
                                               "Actions": [f"AddMail Current blinxxy.MuseArt_{flag} now"]}
    bench_book = strings.add("corrie-event.bench_book", "Mabel's bench, worn pale where her arms rested. Her pattern book lies on it now, closed, square to the edge.")
    when = {"ValeResidents": "true", "HasFile:assets/opal/sprites.png": "true", "HasFile:assets/opal/portraits.png": "true",
            "HasFile:assets/jet/sprites.png": "true", "HasFile:assets/jet/portraits.png": "true"}
    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/CorrieEventProps", "FromFile": "assets/corrie_event_props.png"},
        # (our maps' event lists start empty; Low, so another pack's own Load of one wins)
        *[{"Action": "Load", "Target": f"Data/Events/{loc}", "FromFile": "data/blank.json", "Priority": "Low"} for loc in (S2, VALE, HALL)],
        {"Action": "EditData", "Target": f"Data/Events/{S2}", "When": when, "Entries": studio},
        {"Action": "EditData", "Target": f"Data/Events/{VALE}", "When": when, "Entries": vale},
        {"Action": "EditData", "Target": f"Data/Events/{HALL}", "When": when, "Entries": hall},
        {"Action": "Load", "Target": f"Data/Events/{HOUSE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"Action": "EditData", "Target": f"Data/Events/{HOUSE}", "When": when, "Entries": house},
        {"Action": "EditData", "Target": "Data/mail", "When": when, "Entries": mail_entries},
        {"Action": "EditData", "Target": "Data/TriggerActions", "When": when, "Entries": triggers},
        # Jet's 6: the pattern book lies on Mabel's bench from then on (until his 10 puts it to use)
        {"Action": "EditData", "Target": "Strings/StringsFromMaps", "When": {**when, "HasFlag": "blinxxy.MuseArt_JetPatternBook"},
         "Entries": {"blinxxy.MuseArt_Vale.studio2.bench": bench_book}},
    ]
    header = ("// Opal and Jet Corrie's heart events. Generated by tools/build_corrie_events.py from the\n"
              "// approved draft in docs/corrie_twins_events.md: edit that script, not this file.\n")
    out = os.path.join(PACK, "data", "corrie_events.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    props(os.path.join(PACK, "assets", "corrie_event_props.png"))
    # the text into i18n, after the twins' dialogue (this script's old keys out first)
    p = os.path.join(PACK, "i18n", "default.json")
    s = open(p, encoding="utf-8").read()
    s = re.sub(r'  "corrie-(event|letter)\.[^"]+": .*\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in strings.data.items())
    anchor = re.search(r'  "jet\.gift\.neutral": .*\n', s)
    s = s[:anchor.end()] + block + s[anchor.end():]
    open(p, "w", encoding="utf-8", newline="\n").write(s)
    n = sum(len(x) for x in (studio, vale, hall, house))
    print("wrote", out, f"({n} events,", len(strings.data), "lines)")


if __name__ == "__main__":
    main()

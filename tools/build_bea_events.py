"""Writes Bea Thistle's heart events ([CP] Gilded Vale (dev)/data/bea_events.json) from the approved
draft in docs/vale_events.md (the "Bea Thistle" sections: 2 and 4 hearts, then 6, 8 and 10 after
"# 6 to 10 hearts"). Built like Tess's and the Corries' (build_tess_events.py,
build_corrie_events.py): readable pieces, the text lifted into the dev pack's i18n as bea.event.*,
spliced in after Bea's own keys (that file has comments, so it isn't rewritten).

Each event plays where Bea is on her schedule (data/bea_schedule.json): the tea counter in the
Vale House from 8:30 to 1pm and again from 4:30pm (all day in winter and rain), the tea garden in
the Vale on fine afternoons, and on Thursdays (with the town-trips switch) Pierre's until about 2.
Event IDs: blinxxy.MuseArt_Bea2h, Bea4h, Bea6h, Bea8h, Bea10h, each needing the one before.

What they leave behind (all here, from Bea's side):
  - 4 hearts: where the guest books go, as a mail flag (blinxxy.MuseArt_BeaBooksShelf, _BeaBooksAttic,
    _BeaBooksRead); the folded sketch, a furniture keepsake ((F)blinxxy.MuseArt_BeaAtticSketch); her
    Thu4 line about the attic becomes one about the guest books.
  - 6 hearts: what you told her (blinxxy.MuseArt_BeaOfferSell, _BeaOfferOwn, _BeaOfferWant); a
    week of short lines (the conversation topic blinxxy.MuseArt_BeaOffer, 7 days); the hotel
    group's folder on the end of her counter until her 8 hearts.
  - 8 hearts: the folder gone; her Mon8 line about dinner becomes one about the stew. The morning
    after you reach 10 hearts with her, the new sign goes up outside (blinxxy.MuseArt_BeaNewSign),
    and the day after that (blinxxy.MuseArt_BeaNewSignDayAfter) her 10-heart event can happen.
  - 10 hearts: the artists' room's key (the mail flag blinxxy.MuseArt_ValeArtistsRoom, which
    ArtStudio/Vale/ValeResidents.cs honours for sleeping there, and the game's own
    doorUnlockblinxxy.MuseArt_ArtistsRoom, which opens its door); its bed made one you can sleep in;
    the guest books where you said at 4 hearts, for good.

Props: placeholder art (tools/art/bea_event_props.py), the game's own Pink Cake, and the game's
visitors as cameos (HOTEL_MAN, ARTISTS).

Usage: python tools/build_bea_events.py      (from the repository root)
"""
import json, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cp_i18n import Strings
from build_sable_events import q, speak, script, quick

PACK = "[CP] Gilded Vale (dev)"
B = "BeaThistle"
HOUSE, VALE = "blinxxy.MuseArt_ValeHouse", "blinxxy.MuseArt_GildedVale"
EV = {h: f"blinxxy.MuseArt_Bea{h}h" for h in (2, 4, 6, 8, 10)}
WHEN = {"ValeResidents": "true", "HasFile:assets/bea/sprites.png": "true", "HasFile:assets/bea/portraits.png": "true"}
PROPS = "Mods\\blinxxy.MuseArt\\BeaEventProps"           # tools/art/bea_event_props.py: 16 px tiles, ten across
SKETCH = "(F)blinxxy.MuseArt_BeaAtticSketch"
ARTISTS_ROOM = "blinxxy.MuseArt_ValeArtistsRoom"          # ValeResidents.ArtistsRoomMail
DOOR_UNLOCK = "doorUnlockblinxxy.MuseArt_ArtistsRoom"     # the game's own: its Door action opens for this
TOPIC = "blinxxy.MuseArt_BeaOffer"
SIGN, SIGN_NEXT = "blinxxy.MuseArt_BeaNewSign", "blinxxy.MuseArt_BeaNewSignDayAfter"
BOOKS = {k: f"blinxxy.MuseArt_BeaBooks{k.title()}" for k in ("shelf", "attic", "read")}
OFFER = {k: f"blinxxy.MuseArt_BeaOffer{k.title()}" for k in ("sell", "own", "want")}
# The game's own visitors, as cameos (their own sheets, read from the game: nothing copied here).
# Leah's city exes have only front-facing frames, so they face the room.
HOTEL_MAN = "Morris"
ARTISTS = ("LeahExFemale", "LeahExMale")
OFF = "-100 -100"


def F(hearts):
    return f"Friendship {B} {hearts * 250}"


def mail(flag):
    return f"G PLAYER_HAS_MAIL Current {flag} Any"


def say(line):
    return "message " + q(line)


def prop(index, tx, ty, w=16, h=16, sort=None):
    """A still prop from the placeholder sheet, its top-left on tile (tx, ty)."""
    x, y = (index % 10) * 16, (index // 10) * 16
    return (f"temporaryAnimatedSprite {PROPS} {x} {y} {w} {h} 9999 1 9999 {tx} {ty} false false "
            f"{sort if sort is not None else ty + h // 16 - 1} 0 1 0 0 0")


def tall(index, tx, ty):
    """A 16 x 32 prop standing with its foot on tile (tx, ty)."""
    return prop(index, tx, ty - 1, 16, 32, ty)


# ================================================================ 2 hearts

def heart2():
    """Food for twelve (the tea room, an afternoon). She's at her counter until 1pm on fine days
    and all afternoon in winter (data/bea_schedule.json): noon to 1, or noon to 4 in winter."""
    body = script(
        "continue", "16 24", f"farmer 15 29 0 {B} 23 22 2", "skippable",
        "pause 500", "move farmer 0 -3 0", "pause 300",
        say("Every tea table is laid: cups, plates, a cake stand on each, twelve places. Nobody's sitting at any of them. "
            "Bea is at the end of her counter with a tea towel over her shoulder, looking at the door."),
        speak(B, "A coach party from the city. Twelve of them, booked a month ago. They telephoned an hour ago. They've gone to the seaside instead.$0"),
        f"faceDirection {B} 3", "pause 800", say("She looks at the cake stands."), f"faceDirection {B} 2",
        speak(B, "So. Sit anywhere. You've got the best table in the house, and the other eleven.$u"),
        # you sit at the table on the right-hand rug; she brings the tea round to the other side
        "move farmer 2 0 1", "move farmer 0 -1 1",
        f"move {B} 0 1 2", f"move {B} -3 0 3", f"move {B} 0 2 3",
        "pause 400",
        say("You sit. She brings tea in Tobin's heavy cups and sits across from you, which she never does."),
        speak(B, "My great-aunt ran this place for fifty years. When she left it to me I came down from the city to sell it. "
                 "I was a hotel manager. Four hundred rooms, and I couldn't have told you a single guest's name.$0"),
        speak(B, "I put the sign up the first week. Then the roof leaked, and I took it down to fix it. That was eight years ago.$0"
                 "#$b#The sign's still in the shed. Very good sign.$u"),
        quick([
            ("You should sell it.", [speak(B, "Should I?$6"), "pause 800",
                                     speak(B, "Everyone says so. You're the first one who said it to my face. Have another scone.$u")]),
            ("I'm glad you stayed.", [speak(B, "...Well. The scones are glad too.$l"), f"faceDirection {B} 2",
                                      say("She looks away, at the empty tables.")]),
            ("(Eat another scone.)", [speak(B, "Now that's the right answer.$h")]),
        ]),
        "pause 400",
        # to the door together (the camera down with you, clear of the text box)
        "viewport move 0 3 1000", "move farmer 0 1 2", "move farmer -2 0 3", "move farmer 0 2 2", "faceDirection farmer 0",
        f"move {B} 0 2 2", f"move {B} -5 0 3", f"faceDirection {B} 2",
        say("At the door, she presses a box into your hands: what's left of the cake, for the farm."),
        "addItem (O)221 1",
        "pause 300", "end",
    )
    not_days = "!DayOfWeek Sun Thu"        # Sunday the tea room's shut; Thursday she's at Pierre's
    return {
        f"{EV[2]}/{F(2)}/Time 1200 1300/{not_days}/!Weather rainy": body,
        f"{EV[2]}/{F(2)}/Time 1300 1600/Season winter/{not_days}/!Weather rainy": body,
    }


# ================================================================ 4 hearts

ATTIC = [
    # The attic's things, brought down into the upstairs hall (its west end, by the guest room), all
    # on the hall's middle row: its back row is the furniture's and its front row half hidden by the wall
    prop(11, 2, 14), prop(10, 3, 14),                  # two trunks, the second open
    tall(5, 9, 14),                                    # the dressmaker's dummy, in front of the piano
    tall(6, 11, 14),                                   # the broken easel
    tall(9, 13, 14),                                   # an old desk, its drawer half out
    prop(4, 5, 15), prop(4, 8, 14), prop(4, 10, 15),   # dust
]


def heart4():
    """The guest books (the attic; built as the upstairs hall, with the attic's things brought down
    into it). Wednesday mornings, her quiet day: at the counter from 8:30."""
    key = f"{EV[4]}/{F(4)}/DayOfWeek Wed/Time 900 1200/SawEvent {EV[2]}"
    return {key: script(
        "continue", "15 23", f"farmer 15 29 0 {B} 15 21 2", "skippable",
        "pause 500", "move farmer 0 -6 0", "pause 300",
        say("Bea meets you at the foot of the stairs with a lamp and a broom."),
        speak(B, "You've got a strong back and no sense of self-preservation. I need both. The attic.$u"),
        f"move {B} 0 -4 0", "move farmer 0 -2 0",
        "globalFade", "viewport -1000 -1000",
        *ATTIC,
        f"warp {B} 4 14", f"faceDirection {B} 3", "warp farmer 6 14", "faceDirection farmer 3",
        "viewport 8 14", "globalFadeToClear", "pause 600",
        say("Up a ladder from the hall, under the eaves: the attic. Most of it ends up in the hall between you. "
            "Trunks, a dressmaker's dummy, a broken easel, dust."),
        prop(2, 5, 15),
        say("She pulls a stack of old books out of a trunk. Guest books, fifty years of them. She opens one. Every page has a signature, "
            "and half of them have a little sketch: a tree, a teacup, the lake, a cat on a windowsill."),
        f"faceDirection {B} 1",
        speak(B, "Oh. Oh, look at this. This one's the old tree, the year it was struck by lightning. And this...$6"),
        "pause 600", say("She turns a page."),
        speak(B, "Somebody drew my aunt. She'd have hated it. It's perfect.$6"),
        "pause 600",
        speak(B, "I came up here to throw all this out. I've been meaning to for eight years.$s"),
        # to the old desk (along the front row, round the dummy and the easel)
        "move farmer 0 1 2", "move farmer 6 0 1", "move farmer 0 -1 1", "pause 600",
        say("You find a folded sketch in a desk drawer: the Vale House, drawn from the plaza, signed only with an initial."),
        "move farmer 0 1 2", "move farmer -6 0 3", "move farmer 0 -1 3", f"faceDirection {B} 1", "pause 300",
        quick([
            ("Put them on a shelf downstairs, for guests to read.", [
                speak(B, "In the tea room? People would get jam on them.$l"), "pause 800",
                speak(B, "...People should get jam on them.$u"), f"addMailReceived {BOOKS['shelf']}"]),
            ("Keep them up here, safe.", [
                speak(B, "Safe. Yes. That's what she did with them too.$0"),
                f"faceDirection {B} 3", say("She closes the trunk gently."), f"addMailReceived {BOOKS['attic']}"]),
            ("Read me another one.", [
                f"emote {B} 32", say("She sits on the trunk and reads to you until the lamp runs low."),
                f"addMailReceived {BOOKS['read']}"]),
        ]),
        "pause 400",
        f"addItem {SKETCH} 1",
        "pause 300", "end position 6 14",
    )}


# ================================================================ 6 hearts

def heart6():
    """The offer (the plaza, an afternoon). Her tea-garden hours: fine afternoons, not in winter,
    not on Sundays (the lake) or Thursdays (back from Pierre's to the counter)."""
    key = f"{EV[6]}/{F(6)}/Time 1300 1600/Weather sunny/!Season winter/!DayOfWeek Sun Thu/SawEvent {EV[4]}"
    man = "BeaHotelMan"
    return {key: script(
        "continue", "48 46", f"farmer 52 50 0 {B} 48 46 1", "skippable",
        f"addTemporaryActor {HOTEL_MAN} 16 32 49 46 3 false Character {man}",
        "pause 500", "move farmer 0 -3 0", "faceDirection farmer 3", "pause 300",
        say("A man in a city suit stands by the fountain with a leather folder. Bea is with him, arms folded, tea towel over her shoulder."),
        # (his line as narration: he has no portrait, and a speech bubble this long fades before it's read)
        say("He shakes her hand. 'Think it over, Ms. Thistle. It's a very generous number.'"),
        "pause 400",
        # off towards the bus (the Vale's top-left corner), past the fountain's west side
        f"move {man} 0 1 2", f"move {man} -10 0 3", f"warp {man} {OFF}",
        "pause 400", f"faceDirection {B} 2",
        say("She sits on the edge of the fountain."),
        "move farmer -3 0 3", "move farmer 0 -1 3", f"faceDirection {B} 1", "pause 300",
        speak(B, "A hotel group. They want the Vale House. They'd put in a spa.$0"),
        "pause 800",
        speak(B, "In the kitchen, as far as I can tell.$u"),
        speak(B, "It's more than the inn's worth. It's more than the inn's ever made. I could go back to the city and never fix another pipe.$0"),
        f"faceDirection {B} 2", "pause 600",
        speak(B, "Everyone here would tell me to keep it. Everyone here has a tab.$s"),
        f"faceDirection {B} 1", "pause 400", say("She looks at you."),
        speak(B, "What would you do? Not for them. For me.$s"),
        quick([
            ("Sell it. You've earned a rest.", [
                speak(B, "I have, haven't I.$s"), say("She doesn't sound relieved."), f"addMailReceived {OFFER['sell']}"]),
            ("Keep it, but run it your way.", [
                speak(B, "My way. I don't think I've got a way. I've got hers.$6"), f"addMailReceived {OFFER['own']}"]),
            ("What do you want?", [
                "pause 2000", speak(B, "...Nobody's asked me that in eight years. I don't know. That's the trouble.$s"),
                f"addMailReceived {OFFER['want']}"]),
        ]),
        "pause 400",
        say("She stands and brushes off her apron."),
        speak(B, "Well. The scones won't bake themselves. I've checked.$u"),
        f"addConversationTopic {TOPIC} 7",
        f"move {B} -2 0 3", f"move {B} 0 1 2", f"move {B} -6 0 3",
        "pause 300", "end",
    )}


# ================================================================ 8 hearts

def heart8():
    """Dinner for one (the Vale House, closed, an evening): Wednesdays, at the counter till 11:30."""
    key = f"{EV[8]}/{F(8)}/DayOfWeek Wed/Time 1900 2200/SawEvent {EV[6]}"
    return {key: script(
        "continue", "21 24", f"farmer 15 29 0 {B} 26 21 0", "skippable",
        "ambientLight 140 140 90",
        "pause 600",
        say("A card on the door says Closed tonight. It opens anyway, for you."),
        "move farmer 0 -3 0", "pause 300",
        say("The tea room's dark but for one lamp. Bea is at the stove in the kitchen corner, cooking, humming. One place is laid at the dining table."),
        f"faceDirection {B} 3", "pause 800",
        say("When she sees you she lays another."),
        f"move {B} 0 1 2", f"move {B} -1 0 3", f"faceDirection {B} 2", "pause 800",
        speak(B, "Don't make a fuss. I'm cooking for myself. First time since the city. Nobody's coach, nobody's booking. Just mine.$l"),
        # to the table, side by side behind it at its two places (the table hides your knees)
        "move farmer 13 0 1", "move farmer 0 -4 0", "move farmer -1 0 3", "move farmer 0 1 2",
        f"move {B} 0 1 2",
        "pause 600",
        say("It's a plain stew from the hotel's staff kitchen, which she ate at midnight for ten years."),
        speak(B, "This is the best thing I've ever made, and nobody's paying for it.$h"),
        speak(B, "I wrote to the hotel people this morning.$0"),
        say("She puts down her fork."),
        speak(B, "I said no. Then I said no again, in case the first one was too polite.$0"),
        "pause 600",
        speak(B, "I thought I was keeping it because I owed her. I'm not. I'm keeping it because when I'm not looking after anybody, "
                 "I don't know who I am. I'd like to find out here.$s"),
        quick([
            ("Then stay and find out.", [speak(B, "I'm going to. Eat your stew.$l")]),
            ("Your aunt would be proud.", [speak(B, "She'd be furious I didn't put a spa in.$s#$b#...She'd be proud.$h")]),
        ]),
        "pause 400",
        # the washing up: her at the sink, you beside her with the cloth
        f"move {B} 0 -1 0", f"move {B} 2 0 1", f"move {B} 0 -1 0",
        "move farmer 1 0 1", "move farmer 0 -2 0",
        say("You wash up together. She lets you dry, which she has never let anyone do."),
        "pause 1000", "end",
    )}


# ================================================================ 10 hearts

def heart10():
    """The artists' room (the Vale House, reopened, a morning). At the counter from 8:30, except
    Thursdays (Pierre's). The guest books are where you said at 4 hearts: three versions under the
    one ID (seeing one is seeing it), the shelf one also for a save without any of the three flags."""
    a, b = "BeaArtistA", "BeaArtistB"

    def version(books):
        books_in_tea_room = {
            "shelf": [say("The tea room's changed. The guest books from the attic are on a shelf by the window, with a card: "
                          "'Read us. Jam permitted.'")],
            "attic": [say("The tea room's changed.")],
            "read": [prop(1, 19, 21),
                     say("The tea room's changed. A guest book lies open on the counter, at her aunt's page.")],
        }[books]
        hall = [tall(7, 20, 13)] if books == "attic" else []
        on_the_way = ([say("On a shelf in the hall upstairs: the guest books from the attic, with a card. 'Read us. Jam permitted.'")]
                      if books == "attic" else [])
        return script(
            "continue", "16 24", f"farmer 15 29 0 {B} 21 21 2", "skippable",
            f"addTemporaryActor {ARTISTS[0]} 16 32 10 23 2 false Character {a}",
            f"addTemporaryActor {ARTISTS[1]} 16 32 13 23 2 false Character {b}",
            *hall,
            "pause 500", "move farmer 0 -3 0", "pause 300",
            *books_in_tea_room,
            say("Two young artists off the bus are sketching at a tea table."),
            f"faceDirection {B} 3", "pause 600", f"faceDirection {B} 2",
            speak(B, "They can't pay. I've told them that's the price.$h"),
            # upstairs together: round the counter's kitchen end and across to the stairs
            f"move {B} 2 0 1", f"move {B} 0 2 2", f"move {B} -8 0 3",
            "move farmer 0 -2 0", f"faceDirection {B} 0",
            say("She takes you upstairs to the room beside hers, the one with the canvases and easels, and unlocks it."),
            "globalFade", "viewport -1000 -1000",
            f"warp {B} 15 13", f"faceDirection {B} 0", "warp farmer 15 14", "faceDirection farmer 0",
            "viewport 15 12", "globalFadeToClear", "pause 400",
            *on_the_way,
            "doAction 15 11", "pause 800",
            # in, the camera following up into the room
            "viewport move 0 -4 1250", f"move {B} 0 -7 0", f"move {B} -1 0 1",
            "move farmer 0 -8 3", f"faceDirection {B} 1",
            "pause 600",
            speak(B, "This was the artists' room when the Vale was full. Anyone who came to paint and couldn't pay slept here. "
                     "My aunt stopped it when the money ran out.$0"),
            speak(B, "I've started it again. And this is yours.$l"),
            say("She gives you a key on a ribbon."),
            speak(B, "Any night you like. Free. Don't tell the others, they'll all want one.$l"),
            quick([
                ("Thank you, Bea.", [speak(B, "Don't. I'll cry, and then I'll have to make more tea.$l")]),
                ("What do you want for dinner?", [say("She laughs, properly."), speak(B, "Stew. Obviously.$h")]),
            ]),
            f"addMailReceived {ARTISTS_ROOM}", f"addMailReceived {DOOR_UNLOCK}",
            "pause 600", "end position 15 7",
        )

    base = f"{EV[10]}/{F(10)}/Time 900 1200/!DayOfWeek Thu/SawEvent {EV[8]}/{mail(SIGN_NEXT)}"
    no = "G !PLAYER_HAS_MAIL Current"
    return {
        f"{base}/{no} {BOOKS['attic']} Any/{no} {BOOKS['read']} Any": version("shelf"),
        f"{base}/{mail(BOOKS['attic'])}": version("attic"),
        f"{base}/{mail(BOOKS['read'])}": version("read"),
    }


# ================================================================ what they leave behind

# Placeholder map pieces (assets/Bea*.tmx), each laid over the map from the moment it's due.
PIECES = {
    # name: (map, x, y, [(layer, dy, tile index)], (dy of an action tile, its text key) or None, when)
    "BeaCounterFolder": (HOUSE, 19, 21, [("Front", 0, 0)], None,
                         {"HasSeenEvent": EV[6], "HasSeenEvent |contains=" + EV[8]: "false"}),
    "BeaCounterBook": (HOUSE, 19, 21, [("Front", 0, 1)], None,
                       {"HasSeenEvent": EV[10], "HasFlag": BOOKS["read"]}),
    "BeaHallShelf": (HOUSE, 20, 12, [("Front", 0, 7), ("Buildings", 1, 17)], (1, "books_hall"),
                     {"HasSeenEvent": EV[10], "HasFlag": BOOKS["attic"]}),
    "BeaSign": (VALE, 29, 23, [("Front", 0, 8), ("Buildings", 1, 18)], (1, "sign"),
                {"HasFlag": SIGN}),
}


def tmx(name, cells, action):
    """A one-tile-wide map patch with the props sheet as its own tilesheet."""
    h = max(dy for _, dy, _ in cells) + 1
    layers = []
    for lid, layer in enumerate(("Back", "Buildings", "Front"), 1):
        col = ["0"] * h
        for lay, dy, index in cells:
            if lay == layer:
                col[dy] = str(index + 1)
        layers.append(f'  <layer id="{lid}" name="{layer}" width="1" height="{h}">\n'
                      f'    <data encoding="csv">\n' + ",\n".join(col) + '\n</data>\n  </layer>')
    obj = ""
    if action:
        dy, text = action
        obj = ('\n  <objectgroup id="4" name="Buildings">\n'
               f'    <object id="1" name="TileData" x="0" y="{dy * 16}" width="16" height="16">\n'
               '      <properties>\n'
               f'        <property name="Action" type="string" value="Message blinxxy.MuseArt_Vale.bea.{text}"/>\n'
               '      </properties>\n    </object>\n  </objectgroup>')
    text = ('<?xml version="1.0" encoding="UTF-8"?>\n'
            f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" width="1" height="{h}" '
            'tilewidth="16" tileheight="16" infinite="0" nextlayerid="5" nextobjectid="2">\n'
            '  <tileset firstgid="1" name="z_blinxxy.MuseArt_BeaEventProps" tilewidth="16" tileheight="16" tilecount="20" columns="10">\n'
            '    <image source="bea/event_props.png" width="160" height="32"/>\n  </tileset>\n'
            + "\n".join(layers) + obj + "\n</map>\n")
    with open(os.path.join(PACK, "assets", name + ".tmx"), "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


# Her lines afterwards (new text: the draft says what each is about, not the words)
AFTER_LINES = {
    "thu4": "I keep going back up to the attic and reading the guest books. I was meant to be throwing them out. I've thrown out one spoon.$u",
    "mon8": "Do you know what nobody asks an innkeeper? What she'd like for dinner.$s#$b#Stew, since you're wondering. The staff-kitchen kind. I've made it twice this week, and I didn't charge myself either time.$h",
}
# The week after the offer: shorter lines (draft: "for a week her lines are shorter")
SHORT = {
    "Mon": "Tea's on. Help yourself.$0",
    "Tue": "Busy. Sorry. The pot's fresh.$0",
    "Wed": "Spoons. Thirty-one. Same as last week.$0",
    "Thu": "Pierre had flour. Small mercies.$0",
    "Fri": "Emmett's playing tonight. I might even listen.$s",
    "Sat": "Full garden. Lovely. Mind the wasp.$0",
    "Sun": "Lake, then laundry. Same as ever.$0",
    "rainy": "Rain. Good. Quiet.$0",
}
STRINGS = {
    # the counter while the hotel group's folder sits on it (from her 6 hearts to her 8)
    "counter_folder": "The counter: a cake under glass, the till, Lupini's tab, and a leather folder from the city, not yet opened.",
    # after her 10 hearts, where the guest books went
    "guestbook_shelf": "Fifty years of the Vale House's guest books, signatures and sketches on every page. A card in Bea's hand: 'Read us. Jam permitted.'",
    "counter_read": "The counter: a cake under glass, the till, Lupini's tab, and a guest book lying open at her great-aunt's page.",
    "books_hall": "The guest books from the attic, fifty years of them, on a shelf of their own. A card: 'Read us. Jam permitted.'",
    "sign": "The Vale House. Under new management (the same management).",
    "sketch": "Vale House Sketch",
}


def lifter(strings):
    """Lifts an event's text into i18n (as cp_i18n.Strings.script), but a line already lifted under
    the same prefix keeps its key: the 2-heart event's winter copy and the 10-heart event's three
    versions share their lines, and only what differs gets keys of its own."""
    seen = {}

    def lift(prefix, text):
        known = seen.setdefault(prefix, {})

        def add(t):
            if t not in known:
                known[t] = strings.add(f"{prefix}.{len(known) + 1:02}", t)
            return known[t]

        def sub(m):
            if m.group(1) is not None:
                return '"' + (add(m.group(1)) if m.group(1).strip() else m.group(1)) + '"'
            parts = m.group(2).split("#")
            return "quickQuestion " + "#".join(add(p) if p.strip() else p for p in parts) + "(break)"

        return re.sub(r'"([^"]*)"|quickQuestion (.*?)\(break\)', sub, text)
    return lift


def dialogue_keys():
    """Her day-of-the-week lines (any season, any hearts) in data/bea_dialogue.json."""
    text = open(os.path.join(PACK, "data", "bea_dialogue.json"), encoding="utf-8").read()
    days = ("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    keys = re.findall(r'"((?:spring_|summer_|fall_|winter_)?(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)\d*)":', text)
    return {k: next(d for d in days if d in k) for k in keys}


def main():
    strings = Strings(os.path.join(PACK, "_none_"), [])          # (no file read: the dev i18n has comments)
    lift = lifter(strings)
    house, vale = {}, {}
    for n, events in ((2, heart2()), (4, heart4()), (8, heart8()), (10, heart10())):
        for k, v in events.items():
            house[k] = lift(f"bea.event.heart{n}", v)
    for k, v in heart6().items():
        vale[k] = lift("bea.event.heart6", v)

    def s(key, text):
        return strings.add(f"bea.event.{key}", text)

    after = {k: s("after." + k, v) for k, v in AFTER_LINES.items()}
    short = {k: s("short." + k.lower(), v) for k, v in SHORT.items()}
    texts = {k: s("text." + k, v) for k, v in STRINGS.items()}
    for name, (_, _, _, cells, action, _) in PIECES.items():
        tmx(name, cells, action)

    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/BeaEventProps", "FromFile": "assets/bea/event_props.png"},
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/BeaFurniture", "FromFile": "assets/bea/attic_sketch.png"},
        # (our maps' event lists start empty; Low, so the other residents' events can load them too)
        *[{"Action": "Load", "Target": f"Data/Events/{loc}", "FromFile": "data/blank.json", "Priority": "Low"} for loc in (HOUSE, VALE)],
        {"LogName": "Bea's heart events (2, 4, 8, 10)", "Action": "EditData", "Target": f"Data/Events/{HOUSE}", "When": WHEN, "Entries": house},
        {"LogName": "Bea's 6-heart event", "Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        # the keepsake from the attic (4 hearts): a small framed picture
        {"Action": "EditData", "Target": "Data/Furniture", "When": WHEN, "Entries": {
            SKETCH[3:]: f"{SKETCH[3:]}/painting/2 2/2 2/1/500/-1/{texts['sketch']}/0/Mods\\blinxxy.MuseArt\\BeaFurniture/true"}},
        # her lines after the events
        {"LogName": "Bea's Thu4 after the attic", "Action": "EditData", "Target": "Characters/Dialogue/BeaThistle", "Priority": "Late",
         "When": {**WHEN, "HasSeenEvent": EV[4]}, "Entries": {"Thu4": after["thu4"]}},
        {"LogName": "Bea's Mon8 after the stew", "Action": "EditData", "Target": "Characters/Dialogue/BeaThistle", "Priority": "Late",
         "When": {**WHEN, "HasSeenEvent": EV[8]}, "Entries": {"Mon8": after["mon8"]}},
        {"LogName": "Bea's short week after the offer", "Action": "EditData", "Target": "Characters/Dialogue/BeaThistle", "Priority": "Late",
         "When": {**WHEN, "HasConversationTopic": TOPIC}, "Entries": {k: short[d] for k, d in dialogue_keys().items()}},
        {"LogName": "Bea's short week after the offer (rain)", "Action": "EditData", "Target": "Characters/Dialogue/rainy", "Priority": "Late",
         "When": {**WHEN, "HasConversationTopic": TOPIC}, "Entries": {B: short["rainy"]}},
        # what the counter and the shelves say
        {"Action": "EditData", "Target": "Strings/StringsFromMaps", "Priority": "Late", "When": WHEN,
         "Entries": {"blinxxy.MuseArt_Vale.bea.books_hall": texts["books_hall"], "blinxxy.MuseArt_Vale.bea.sign": texts["sign"]}},
        {"LogName": "The hotel folder on Bea's counter", "Action": "EditData", "Target": "Strings/StringsFromMaps", "Priority": "Late",
         "When": {**WHEN, **PIECES["BeaCounterFolder"][5]}, "Entries": {"blinxxy.MuseArt_Vale.house.counter": texts["counter_folder"]}},
        {"LogName": "The guest books in the tea room", "Action": "EditData", "Target": "Strings/StringsFromMaps", "Priority": "Late",
         "When": {**WHEN, "HasSeenEvent": EV[10], "HasFlag |contains=" + BOOKS["attic"] + ", " + BOOKS["read"]: "false"},
         "Entries": {"blinxxy.MuseArt_Vale.house.guestbook": texts["guestbook_shelf"]}},
        {"LogName": "The guest book open on Bea's counter", "Action": "EditData", "Target": "Strings/StringsFromMaps", "Priority": "Late",
         "When": {**WHEN, **PIECES["BeaCounterBook"][5]}, "Entries": {"blinxxy.MuseArt_Vale.house.counter": texts["counter_read"]}},
        # the map pieces
        *[{"LogName": f"Bea's events: {name}", "Action": "EditMap", "Target": f"Maps/{loc}", "FromFile": f"assets/{name}.tmx",
           "ToArea": {"X": x, "Y": y, "Width": 1, "Height": max(dy for _, dy, _ in cells) + 1}, "PatchMode": "Overlay",
           "Priority": "Late", "Update": "OnLocationChange", "When": {**WHEN, **when}}
          for name, (loc, x, y, cells, _, when) in PIECES.items()],
        # the artists' room's bed, yours after her 10 hearts (as the guest bed: tools/build_vale_house.py)
        {"LogName": "The artists' room's bed, yours", "Action": "EditMap", "Target": f"Maps/{HOUSE}", "Update": "OnLocationChange",
         "When": {**WHEN, "HasFlag": ARTISTS_ROOM},
         "MapTiles": [{"Position": {"X": x, "Y": y}, "Layer": "Back", "SetProperties": props}
                      for x in (17, 18) for y in (5, 6, 7)
                      for props in [{"Bed": "T", **({"TouchAction": "Sleep"} if y > 5 else {})}]]},
        # the new sign: up the morning after you reach 10 hearts (her 8 seen); the event the day after
        {"Action": "EditData", "Target": "Data/TriggerActions", "When": WHEN, "Entries": {
            SIGN_NEXT: {"Id": SIGN_NEXT, "Trigger": "DayStarted", "Condition": f"PLAYER_HAS_MAIL Current {SIGN} Any",
                        "Actions": [f"AddMail Current {SIGN_NEXT} received"]},
            SIGN: {"Id": SIGN, "Trigger": "DayStarted",
                   "Condition": f"PLAYER_FRIENDSHIP_POINTS Current {B} 2500, PLAYER_HAS_SEEN_EVENT Current {EV[8]}",
                   "Actions": [f"AddMail Current {SIGN} received"]}},
         # (the day-after check first: on the morning the sign goes up it hasn't yet)
         "MoveEntries": [{"ID": SIGN_NEXT, "BeforeID": SIGN}]},
    ]
    header = ("// Bea Thistle's heart events. Generated by tools/build_bea_events.py from the approved draft in\n"
              "// docs/vale_events.md: edit that script, not this file.\n")
    out = os.path.join(PACK, "data", "bea_events.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    # the text into i18n, right after Bea's own keys (this script's old keys out first)
    p = os.path.join(PACK, "i18n", "default.json")
    s_ = open(p, encoding="utf-8").read()
    s_ = re.sub(r'  "bea\.event\.[^"]+": .*\n', "", s_)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in strings.data.items())
    anchor = re.search(r'  "bea\.dialogue\.AcceptBirthdayGift_Negative": .*\n', s_)
    s_ = s_[:anchor.end()] + block + s_[anchor.end():]
    open(p, "w", encoding="utf-8", newline="\n").write(s_)
    print("wrote", out, f"({len(house) + len(vale)} event entries,", len(strings.data), "lines)")


if __name__ == "__main__":
    main()

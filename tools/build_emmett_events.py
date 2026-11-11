"""Writes Emmett Wren's heart events ([CP] Gilded Vale (dev)/data/emmett_events.json) from the
approved draft in docs/vale_events.md ("Emmett Wren", 2 and 4 hearts, and 6 to 10 hearts; the user
approved building them as drafted, 2026-11-11). Built like Tess's and the Corries'
(build_tess_events.py, build_corrie_events.py): readable pieces, the text lifted into the dev
pack's i18n as emmett.event.* (spliced in after Emmett's dialogue: that file has comments).

Event IDs: blinxxy.MuseArt_Emmett2h, ...4h, 6h, 8h, 10h; each needs the one before. The 4-heart
choice is remembered as a mail flag (blinxxy.MuseArt_Emmett4hWidow / 4hYours / 4hSame).

Where and when: where his schedule (data/emmett_schedule.json) has him at that time, so nobody is
moved for them:
  2   the amphitheatre's stage, 9am to noon (Sundays from 10, when he gets there), not raining
  4   the lakeside bench, Tuesdays and Thursdays 1pm to 5pm, not raining
  6   the amphitheatre, 3pm to 5pm, not raining: Fridays and Sundays, the days he's on the stage all
      afternoon; Sundays only with the town trips on (data/vale_town_trips.json: after his 4-heart
      event his Fridays are the Saloon's)
  8   the Vale House, 10pm to 11:30pm, when he goes home (not Fridays with the town trips on: he's
      on the late bus from the Saloon)
  10  the overlook bench, spring Saturday afternoons (his Saturdays end on that bench), dating him

What they change:
  - the amphitheatre is overgrown (brambles on its seats) until his 6-heart event clears it: a map
    patch of the seats' tiles with brambles drawn on, per season, until the mail flag
    blinxxy.MuseArt_AmphitheatreCleared (set by the event, which clears them on screen too)
  - his travelling pack hangs on the wall of his cottage, empty, from his 10-heart event on
  - his map of the Republic, given in his 10-heart event: furniture, (F)blinxxy.MuseArt_EmmettRepublicMap

PLACEHOLDER ART, drawn here (and his fiddle frames by tools/art/emmett_fiddle_frames.py): the
brambles, the pack (full, empty, on its hook), the things he unpacks, Tobin's wheelbarrow, Bea's
tea tray, the framed map. Outlines are a darker shade of the colour they border, never black.

Usage: python tools/build_emmett_events.py        (from the repository root)
"""
import json, os, random, re, sys
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_sable_events import q, speak, script, quick
from render_tmx import load as load_tmx

PACK = "[CP] Gilded Vale (dev)"
E, B, T, FN, L = "EmmettWren", "BeaThistle", "TobinClay", "FernAster", "Lupini"
VALE, HOUSE, COTTAGE = "blinxxy.MuseArt_GildedVale", "blinxxy.MuseArt_ValeHouse", "blinxxy.MuseArt_ValeEmmett"
EV = {h: f"blinxxy.MuseArt_Emmett{h}h" for h in (2, 4, 6, 8, 10)}
CLEARED = "blinxxy.MuseArt_AmphitheatreCleared"
MAP_ITEM = "blinxxy.MuseArt_EmmettRepublicMap"
PROPS = "Mods\\blinxxy.MuseArt\\EmmettEventProps"            # 16 x 32 cells, see props()
SEASONS = ("spring", "summer", "fall", "winter")
WHEN = {"ValeResidents": "true", "HasFile:assets/emmett/sprites.png": "true", "HasFile:assets/emmett/portraits.png": "true"}
OFF = "-100 -100"

# His sheet (tools/art/emmett_fiddle_frames.py): 8 standing facing up, 16 and 17 sitting facing left
# and right, 18-19 playing facing up, 20-21 playing sitting (facing left; flipped for right),
# 22 the fiddle across his knees, 23 his notebook.
UP, SIT_L, PLAY_UP, PLAY_SIT, KNEES, NOTEBOOK = 8, 16, (18, 19), (20, 21), 22, 23
# Sitting on the overlook bench, as his schedule does (data/emmett.json emmett_sit_left)
SIT_OVERLOOK = (-16, 24)
# ... and the lakeside bench (its back on the left: the same pose mirrored), and Bea's footstool
SIT_LAKE = (16, 24)
SIT_STOOL = (0, 16)

# The amphitheatre's art (tools/build_vale.py AMPHITHEATRE: the Buildings2 layer, x 31-43, y 46-55)
AMPH = (31, 46, 13, 10)
# Bramble clumps on its seats: (centre x, foot y) in tiles. Clear of the stage, and of the seats
# the events use (his 2-heart event's seat for you, 34 51).
CLUMPS = [(33.1, 49.0), (35.3, 48.0), (32.5, 51.9), (36.0, 49.8), (37.6, 47.7),
          (39.8, 48.0), (41.3, 49.7), (40.1, 51.3), (42.1, 52.4), (38.7, 50.5)]


def F(hearts):
    return f"Friendship {E} {hearts * 250}"


def say(line):
    return "message " + q(line)


def sprite(cell, tx, ty, sort, w=16, h=32, flip=False):
    """A still prop from the event props sheet (16 x 32 cells), its top-left on tile (tx, ty)."""
    return (f"temporaryAnimatedSprite {PROPS} {cell * 16} 0 {w} {h} 9999 1 9999 {tx} {ty} false "
            f"{'true' if flip else 'false'} {sort} 0 1 0 0 0")


def play(frames, ms, flip=False):
    """Emmett playing (an animation loop), with the fiddle heard."""
    return [f"animate {E} {'true' if flip else 'false'} true {ms} {frames[0]} {frames[1]}", "playSound gusviolin"]


def stop_playing(frame, flip=False):
    """He stops: the fiddle stops, and he stands facing up (UP), or holds still on a sitting frame
    (an animation of one frame, which faceDirection can't undo: only used while he sits)."""
    if frame == UP:
        return ["stopSound gusviolin", f"stopAnimation {E}", f"faceDirection {E} 0"]
    return ["stopSound gusviolin", f"stopAnimation {E}", f"animate {E} {'true' if flip else 'false'} true 1000 {frame}"]


def bow():
    """A bow: a little dip and back."""
    return [f"positionOffset {E} 0 6", "pause 250", f"positionOffset {E} 0 -6", "pause 250"]


# ---------------------------------------------------------------------------------------- events

def heart2():
    """2 hearts: an empty house (the amphitheatre, a morning)."""
    key = (f"{EV[2]}/{F(2)}/Time 900 1200/!Weather rainy/"
           'G ANY "!DAY_OF_WEEK Sunday" "TIME 1000 1200"')
    return key, script(
        "continue", "37 52", f"farmer 37 60 0 {E} 37 54 0", "skippable",
        *play(PLAY_UP, 110), "pause 600",
        "move farmer 0 -3 0", "pause 400",
        say("Emmett is alone on the stage, playing to the empty stone seats: a fast reel, all of it, with the bows at the end. Nobody claps."),
        *stop_playing(UP), "pause 300", *bow(), *bow(), "pause 600",
        f"faceDirection {E} 2", f"emote {E} 16",
        speak(E, "A crowd! Finally. Sit anywhere, they're all the best seats.$u"),
        # (up the forecourt's west side and onto the third row)
        "move farmer -3 0 3", "move farmer 0 -6 0", "faceDirection farmer 1",
        f"faceDirection {E} 0", "pause 400",
        *play(PLAY_UP, 300), "pause 2600", *stop_playing(UP),
        say("You sit on the stone tiers. He plays a slower one, and stops halfway."),
        "pause 500",
        speak(E, "Five springs ago I walked in here at sunset. I'd played a wedding in the next valley and slept in a hedge. The seats were all brambles then. I sat on that one, the third row, and played to nobody.$0"),
        speak(E, "I meant to stay a week.$0#$b#The week's going well.$u"),
        quick([
            ("Play me the rest of that one.", [
                *play(PLAY_UP, 300), "pause 3000", *stop_playing(UP), f"emote {E} 32",
                say("He does. It's better than the reel.")]),
            ("Why did you stay?", [
                say("He opens his mouth, closes it, and plays the first bars of a song about a turnip instead."),
                *play(PLAY_UP, 160), "pause 1400", *stop_playing(UP),
                speak(E, "...Long story. Very boring. Mostly turnips.$l")]),
            ("Where are you from?", [
                speak(E, "I've played in a hundred towns. I couldn't tell you which one I'm from.$s")]),
        ]),
        "pause 400", *bow(), "pause 300", *bow(),
        say("He bows to the empty seats, and to you."),
        speak(E, "Same time tomorrow. Bring a friend. Bring a hedge.$u"),
        "pause 300", "end",
    )


def heart4():
    """4 hearts: whose song (the lakeside bench, an afternoon). He sits on the bench facing the lake
    (east): his sitting frames mirrored."""
    key = f"{EV[4]}/{F(4)}/DayOfWeek Tue Thu/Time 1300 1700/!Weather rainy/SawEvent {EV[2]}"
    sit_flip = "true"
    return key, script(
        "continue", "47 62", f"farmer 45 57 2 {E} 47 62 1", "skippable",
        f"positionOffset {E} {SIT_LAKE[0]} {SIT_LAKE[1]}", f"animate {E} {sit_flip} true 1000 {NOTEBOOK}",
        "pause 500",
        # (down the stepping stones and round the bench's foot onto the sand in front of it)
        "move farmer 0 7 2", "move farmer 4 0 1", "move farmer 0 -2 0", "faceDirection farmer 3", "pause 300",
        say("Emmett sits on the bench with his notebook, a pencil behind his ear and another in his mouth."),
        speak(E, "You've caught me working. Don't tell anyone. It'll ruin my reputation.$u"),
        speak(E, "I'm writing a song. Here, listen."),
        *play(PLAY_SIT, 220, flip=True), "pause 2600",
        say("He plays a few lines about the lake. It's good, and it stops where he hasn't finished it."),
        *stop_playing(KNEES, flip=True), "pause 400",
        speak(E, "Now this one."),
        *play(PLAY_SIT, 480, flip=True), "pause 2800",
        say("A plain, slow tune."),
        *stop_playing(KNEES, flip=True), "pause 300",
        speak(E, "A farmer's widow taught me that, way up north, the winter I mended her fence. She'd made it up for her husband. She sang it at the kitchen sink every night for forty years, and never once to anybody.$0"),
        speak(E, "Half my favourite songs came like that. Ordinary folk, singing to the washing-up. Nobody writes them down. So I keep them.$s"),
        quick([
            ("The widow's is better.", [
                speak(E, "It is, isn't it. I've been trying to write one like it for twenty years.$h"),
                "addMailReceived blinxxy.MuseArt_Emmett4hWidow"]),
            ("Yours is better.", [
                speak(E, "Liar. Thank you.$l"),
                "addMailReceived blinxxy.MuseArt_Emmett4hYours"]),
            ("They sound like the same song.", [
                "pause 1500", say("A long pause."),
                f"animate {E} {sit_flip} true 1000 {NOTEBOOK}",
                speak(E, "...They do. Hers is about somewhere she belonged.$s"),
                f"animate {E} {sit_flip} true 1000 17", say("He closes the notebook."),
                "addMailReceived blinxxy.MuseArt_Emmett4hSame"]),
        ]),
        "pause 400",
        # up off the bench, round its foot and away up the path towards the plaza, humming
        f"stopAnimation {E}", f"positionOffset {E} {-SIT_LAKE[0]} {-SIT_LAKE[1]}", f"warp {E} 47 64",
        f"faceDirection {E} 3", "pause 200",
        f"move {E} -2 0 3", f"emote {E} 56 true", f"move {E} 0 -6 0 true",
        say("He hums the widow's tune all the way back up to the plaza."),
        "pause 600", "end",
    )


def heart6(trips, tiles):
    """6 hearts: a musical evening (the amphitheatre, a late afternoon). `tiles` is the brambles' map
    tiles: (x, y, index of the cleared tile on the patch's sheet), cut in the order given."""
    days = "Sun" if trips else "Fri Sun"
    key = f"{EV[6]}/{F(6)}/DayOfWeek {days}/Time 1500 1700/!Weather rainy/SawEvent {EV[4]}"

    def cut(group, who):
        out = []
        for x, y, clean in group:
            swing = f"shake {who} 300" if who != "farmer" else "pause 300"         # (shake is for villagers)
            out += [swing, "playSound weed_cut", f"changeMapTile Buildings2 {x} {y} {clean}", "pause 350"]
        return out
    left = [t for t in tiles if t[0] <= 36]
    right = [t for t in tiles if t[0] > 36]
    return key, script(
        "continue", "37 51",
        f"farmer 37 60 0 {E} 34 50 0 {T} {OFF} 2 {FN} {OFF} 2 {B} {OFF} 2 {L} {OFF} 2", "skippable",
        "pause 400", f"shake {E} 400", "playSound weed_cut", "pause 400",
        "move farmer 0 -4 0", f"shake {E} 400", "playSound weed_cut", "pause 300",
        say("Emmett is hacking at the brambles on the stone seats with a borrowed sickle (Fern's)."),
        f"faceDirection {E} 2",
        speak(E, "Ah. Perfect timing. Grab some gloves. You're a farmer, this is basically farming.$u"),
        # you clear the tiers together: he works the west side, you the east
        "move farmer 2 0 1", "move farmer 0 -4 0", "faceDirection farmer 0", f"faceDirection {E} 0",
        *cut(left[:3], E), *cut(right[:3], "farmer"),
        say("You clear the tiers together."),
        "globalFade", "viewport -1000 -1000",
        *[f"changeMapTile Buildings2 {x} {y} {c}" for x, y, c in left[3:6] + right[3:6]],
        # Tobin, with a wheelbarrow
        f"warp {T} 41 56", f"faceDirection {T} 0", sprite(6, 42, 55, 57),
        f"warp {E} 36 52", f"faceDirection {E} 2", "warp farmer 39 52", "faceDirection farmer 2",
        "viewport 38 52", "globalFadeToClear", "pause 400",
        f"emote {T} 12", f"shake {T} 400",
        say("Tobin turns up with a wheelbarrow, complaining."),
        *cut(left[6:8], E), *cut(right[6:8], "farmer"),
        # Fern, checking nobody's killing anything rare
        f"warp {FN} 31 56", f"move {FN} 2 0 1", f"move {FN} 0 -3 0", f"faceDirection {FN} 0", f"emote {FN} 8",
        say("Fern arrives to make sure nobody's killing anything rare."),
        f"faceDirection {FN} 1", "pause 600", f"faceDirection {FN} 0",
        # Bea, along the forecourt from the plaza's stairs with the tea
        f"warp {B} 47 56", f"move {B} -4 0 3", f"faceDirection {B} 0",
        sprite(7, 42, 55, 57),
        say("Bea brings tea, and stays."),
        "pause 400", "globalFade", "viewport -1000 -1000",
        *[f"changeMapTile Buildings2 {x} {y} {c}" for x, y, c in tiles],
        "removeTemporarySprites",
        # evening: the seats clear, everyone in their places
        f"warp {E} 37 54", f"faceDirection {E} 0", "warp farmer 38 56", "faceDirection farmer 0",
        f"warp {B} 35 52", f"faceDirection {B} 1", f"warp {T} 36 52", f"faceDirection {T} 3",
        f"warp {FN} 42 51", f"faceDirection {FN} 3",
        "viewport 37 51", "globalFadeToClear", "pause 600",
        say("By evening the seats are clear for the first time in decades."),
        f"faceDirection {E} 2",
        speak(E, "When I walked in here, it was all brambles. I thought, nobody's sat here for forty years. Nobody will.$0"),
        "move farmer 2 0 1", "move farmer 0 -5 0", "faceDirection farmer 2",
        f"emote {B} 12", f"shake {T} 300",
        f"warp {L} 37 45", f"move {L} 0 3 2", f"faceDirection {L} 2",
        say("The residents take their seats: Bea and Tobin in the front row, arguing; Fern on the steps; Lupini, late, at the very back."),
        f"faceDirection {B} 2", f"faceDirection {T} 2", f"faceDirection {E} 0", "pause 400",
        *play(PLAY_UP, 160), "pause 3000",
        say("Emmett plays. The amphitheatre does what it was built for, and the sound carries all the way to the lake."),
        *stop_playing(UP), "pause 300",
        quick([
            ("Play another!", [
                f"emote {E} 32", *play(PLAY_UP, 140), "pause 2200",
                say("He does. Then another. Bea shouts for a slow one."),
                *play(PLAY_UP, 420), "pause 2400", *stop_playing(UP)]),
            ("(Sit with the others and listen.)", [
                *play(PLAY_UP, 320), "pause 3200", *stop_playing(UP),
                say("After the last tune, he looks for you in the seats first.")]),
        ]),
        "pause 400", "globalFade", "viewport -1000 -1000",
        f"warp {B} {OFF}", f"warp {T} {OFF}", f"warp {FN} {OFF}", f"warp {L} {OFF}",
        f"stopAnimation {E}", f"warp {E} 38 54", f"faceDirection {E} 3", "warp farmer 36 54", "faceDirection farmer 1",
        "viewport 37 53", "globalFadeToClear", "pause 600",
        say("Afterwards, alone with you on the stage:"),
        speak(E, "Funny. I've played to thousands. I've never been nervous. Tonight my hands were shaking.$l"),
        f"addMailReceived {CLEARED}",
        "pause 300", "end",
    )


def heart8(trips):
    """8 hearts: the song (the Vale House, late). He sits on the footstool by the fire, facing it;
    you sit behind him, where he can't see your face."""
    key = f"{EV[8]}/{F(8)}/Time 2200 2330/SawEvent {EV[6]}" + ("/!DayOfWeek Fri" if trips else "")
    return key, script(
        "none", "6 22", f"farmer 10 24 3 {E} 4 22 3", "skippable",
        f"positionOffset {E} {SIT_STOOL[0]} {SIT_STOOL[1]}", f"animate {E} false true 1000 {KNEES}",
        "playSound cracklingFire",
        "pause 500",
        say("The tea room's empty but for him; Bea's gone up to bed, and the fire's low."),
        # past the armchair and the tea table to the hearth
        "move farmer -3 0 3", "move farmer 0 1 2", "move farmer -4 0 3", "move farmer 0 -1 0", "faceDirection farmer 0", "pause 300",
        say("Emmett is by the fire with the fiddle across his knees, not playing."),
        speak(E, "Couldn't sleep. Bea said I could stay if I didn't play anything with a chorus.$s"),
        speak(E, "I've been writing something. The first song I've ever written about a place I live."),
        speak(E, "I haven't played it for anyone. Sit there.$l"),
        "faceDirection farmer 1", "pause 300",
        speak(E, "No, there, where I can't see your face.$l"),
        "move farmer 0 -3 0", "move farmer 2 0 1", "faceDirection farmer 3", "pause 600",
        *play(PLAY_SIT, 520), "pause 2400",
        say("He plays it: a slow tune, the Vale in verses."),
        say("The inn and its two good roofs."),
        say("A potter who says a bowl holds soup."),
        say("A gardener talking to the hedges."),
        say("A painter standing at his easel, not painting."),
        "pause 1600",
        say("Then a verse he clearly didn't mean to sing: about someone who comes off the bus with soil on their boots and makes the Vale feel like somewhere you could stay."),
        *stop_playing(KNEES), "pause 900",
        speak(E, "...That verse isn't finished. That verse isn't anything. Forget that verse.$l"),
        quick([
            ("Play it again.", [
                "pause 1500", *play(PLAY_SIT, 700), "pause 1500",
                say("A long pause. He plays it again, slower."),
                "pause 2500", *stop_playing(KNEES)]),
            ("It's the best one.", [
                speak(E, "...It is, isn't it. Blast.$h")]),
            ("Who's it about?", [
                speak(E, "A ferryman. From the north. Very famous ferryman.$u"),
                say("He doesn't meet your eye.")]),
        ]),
        "stopSound cracklingFire", "pause 400", "end",
    )


def heart10():
    """10 hearts: the bag (the overlook bench, a spring afternoon; dating). He sits on the bench as
    his Saturdays do; his pack stands at the bench's foot (not by his feet: they're over the bluff's
    rim). He gets up and lays its things out on the seat from behind the bench."""
    key = (f"{EV[10]}/{F(10)}/Dating {E}/Season spring/DayOfWeek Sat/Time 1400 1800/!Weather rainy/"
           f"SawEvent {EV[8]}")
    pack, emptied, things = 0, 1, (2, 3, 4, 5)        # props(): the full pack, the empty one, shirt cup book map
    laid = [sprite(n, 89, 58, 61) for n in things]
    return key, script(
        "continue", "89 59", f"farmer 92 63 0 {E} 89 58 3", "skippable",
        f"positionOffset {E} {SIT_OVERLOOK[0]} {SIT_OVERLOOK[1]}", f"animate {E} false true 1000 {SIT_L}",
        sprite(pack, 89, 59, 60), "pause 500",
        "move farmer 0 -1 0", "move farmer -3 0 3", "faceDirection farmer 0", "pause 300",
        say("Emmett sits on the bench with his old travelling pack at his feet, stuffed full, the fiddle case strapped on top, his long coat on."),
        speak(E, "Every spring. Same thing. I pack the bag, I walk up here, and I look at the road out of the Vale.$0"),
        speak(E, "Five years, I've never gone. But I've never unpacked either. It's sat by my bed, packed, for five years. In case."),
        speak(E, "I've lived out of that bag since I was eighteen. Every town, every hedge. It's the only home I've ever had, and it's a bag.$s"),
        "pause 1800",
        say("He looks at you for a long time."),
        # up, round behind the bench, and the pack's things out onto the seat one at a time
        f"stopAnimation {E}", f"positionOffset {E} {-SIT_OVERLOOK[0]} {-SIT_OVERLOOK[1]}", f"warp {E} 90 58",
        f"faceDirection {E} 3", "pause 500",
        *[c for item in laid for c in ("playSound leafrustle", item, "pause 700")],
        "removeTemporarySprites", sprite(emptied, 89, 59, 60), *laid,
        say("Then he opens the pack and lays things out on the bench, one at a time: a spare shirt, a tin cup, the songbook, a map of the Republic with every town crossed through."),
        f"faceDirection {E} 2",
        speak(E, "I'm unpacking it. In front of you. So I can't pretend I didn't.$l"),
        quick([
            ("Welcome home.", [
                say("He laughs, and it cracks halfway."),
                speak(E, "...Say that again. I want to write it down.$l")]),
            ("Want help carrying it back?", [
                speak(E, "The bag? No. It's empty. It's very light now.$h")]),
        ]),
        "pause 400", f"faceDirection {E} 3", "pause 300",
        "removeTemporarySprites", sprite(emptied, 89, 59, 60), *laid[:3],
        f"faceDirection {E} 2",
        say("He folds the map with every town crossed through and gives it to you."),
        speak(E, "Here. I won't be needing it. Every town in Ferngill, and I've finally found the one I'm from.$u"),
        f"addItem (F){MAP_ITEM} 1",
        "pause 300", "end",
    )


# ------------------------------------------------------------------------------------------- art

def shade(c, k=1.0):
    """An outline: the neighbouring colour, darker, in its own hue (tools/art/vanillafy.py outline_shade)."""
    return (int(c[0] * 0.52 * k), int(c[1] * 0.42 * k), int(c[2] * 0.46 * k), 255)


def outline(im):
    """Every transparent pixel touching the drawing becomes a darker shade of the colour it touches."""
    px = im.load()
    w, h = im.size
    add = {}
    for y in range(h):
        for x in range(w):
            if px[x, y][3]:
                continue
            near = [px[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 255
                    and px[x + dx, y + dy][:3] != (0, 0, 0)]
            if near:
                base = max(set(n[:3] for n in near), key=[n[:3] for n in near].count)
                add[(x, y)] = shade(base)
    for k, v in add.items():
        px[k] = v
    return im


def rect(d, x0, y0, x1, y1, c):
    d.rectangle([x0, y0, x1, y1], fill=c + (255,) if len(c) == 3 else c)


def draw_pack(full):
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    canvas, lit, dark, strap = (150, 128, 84), (184, 162, 112), (118, 98, 62), (112, 68, 38)
    if full:
        rect(d, 3, 15, 12, 30, canvas)                       # the bag, bulging
        rect(d, 2, 18, 13, 28, canvas)
        rect(d, 3, 16, 6, 27, lit)
        rect(d, 10, 18, 13, 29, dark)
        rect(d, 3, 15, 12, 19, dark)                         # the flap
        rect(d, 4, 15, 11, 18, (134, 112, 72))
        rect(d, 5, 19, 6, 29, strap)                         # straps
        rect(d, 9, 19, 10, 29, strap)
        rect(d, 7, 22, 8, 23, (206, 178, 92))                # buckle
        rect(d, 1, 11, 14, 14, (84, 48, 30))                 # the fiddle case, strapped on top
        rect(d, 1, 10, 8, 14, (84, 48, 30))
        rect(d, 2, 11, 7, 11, (120, 74, 44))
        rect(d, 9, 12, 13, 12, (120, 74, 44))
        rect(d, 5, 10, 6, 15, strap)
        rect(d, 11, 11, 12, 15, strap)
    else:
        rect(d, 3, 24, 12, 30, canvas)                       # slumped, empty
        rect(d, 2, 26, 13, 30, canvas)
        rect(d, 3, 24, 7, 27, lit)
        rect(d, 10, 26, 13, 30, dark)
        rect(d, 3, 23, 11, 25, (134, 112, 72))
        rect(d, 5, 25, 6, 30, strap)
        rect(d, 9, 25, 10, 30, strap)
    return outline(im)


def draw_on_bench(what):
    """One of the pack's things lying on the overlook bench's seat (this cell's tiles 89, 58-59)."""
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    if what == "shirt":
        rect(d, 2, 2, 8, 6, (214, 200, 168))
        rect(d, 2, 2, 8, 2, (232, 222, 196))
        rect(d, 2, 5, 8, 6, (190, 174, 140))
        rect(d, 4, 3, 6, 3, (196, 70, 56))                   # a red thread: his neckerchief tucked in it
    elif what == "cup":
        rect(d, 3, 8, 6, 11, (170, 176, 182))
        rect(d, 3, 8, 6, 8, (206, 212, 216))
        rect(d, 7, 9, 7, 10, (140, 146, 152))                # the handle
    elif what == "book":
        rect(d, 2, 13, 8, 17, (110, 64, 36))
        rect(d, 3, 14, 7, 16, (142, 88, 50))
        rect(d, 3, 12, 5, 12, (236, 226, 198))               # scraps of paper sticking out
        rect(d, 6, 13, 7, 13, (236, 226, 198))
    elif what == "map":
        rect(d, 2, 19, 8, 23, (226, 206, 160))
        rect(d, 2, 19, 8, 19, (240, 226, 188))
        for x, y in ((3, 20), (5, 21), (7, 20), (4, 22), (7, 22)):
            rect(d, x, y, x, y, (156, 60, 44))                # towns, crossed through
        rect(d, 5, 19, 5, 23, (196, 176, 130))               # the fold
    return outline(im)


def draw_wheelbarrow():
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    rect(d, 4, 22, 14, 26, (128, 96, 60))                    # the tray, side on
    rect(d, 5, 21, 13, 21, (156, 120, 78))
    rect(d, 6, 24, 12, 24, (110, 80, 48))
    rect(d, 13, 24, 15, 25, (110, 80, 48))                    # handles
    rect(d, 9, 27, 9, 30, (96, 70, 44))                       # the leg
    d.ellipse([1, 25, 6, 30], fill=(80, 74, 70, 255))         # the wheel
    rect(d, 3, 27, 4, 28, (130, 122, 116))
    rect(d, 6, 20, 9, 21, (70, 110, 46))                      # cut brambles heaped in it
    rect(d, 9, 19, 11, 21, (92, 52, 60))
    return outline(im)


def draw_tea():
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    rect(d, 1, 27, 14, 29, (148, 98, 58))                    # the tray
    rect(d, 1, 27, 14, 27, (178, 126, 80))
    rect(d, 4, 21, 9, 26, (236, 238, 240))                    # the teapot (Bea's white china, a blue band)
    rect(d, 5, 20, 8, 20, (236, 238, 240))
    rect(d, 6, 19, 7, 19, (110, 140, 196))
    rect(d, 4, 23, 9, 23, (110, 140, 196))
    rect(d, 10, 22, 11, 23, (236, 238, 240))                 # the spout
    rect(d, 2, 22, 3, 24, (220, 222, 226))                   # the handle
    rect(d, 11, 25, 13, 26, (236, 238, 240))                 # a cup
    return outline(im)


def props(path):
    """The event props: 16 x 32 cells. 0 the pack, full; 1 empty; 2-5 the shirt, cup, songbook and
    map on the bench; 6 Tobin's wheelbarrow; 7 Bea's tea tray."""
    cells = [draw_pack(True), draw_pack(False), *[draw_on_bench(w) for w in ("shirt", "cup", "book", "map")],
             draw_wheelbarrow(), draw_tea()]
    out = Image.new("RGBA", (16 * len(cells), 32))
    for n, im in enumerate(cells):
        out.paste(im, (n * 16, 0))
    out.save(path)


def republic_map(path):
    """His map of the Republic, framed (painting furniture, 2 x 2): every town crossed through."""
    im = Image.new("RGBA", (32, 32))
    d = ImageDraw.Draw(im)
    rect(d, 1, 2, 30, 29, (120, 76, 44))                     # the frame
    rect(d, 1, 2, 30, 2, (156, 104, 62))
    rect(d, 4, 5, 27, 26, (226, 206, 160))                   # the map
    rect(d, 4, 5, 27, 5, (238, 224, 186))
    coast = [(6, 9), (9, 7), (13, 8), (17, 6), (21, 8), (24, 11), (25, 15), (23, 19), (24, 23), (19, 24),
             (14, 23), (10, 24), (7, 21), (6, 16), (6, 9)]
    d.line(coast, fill=(150, 120, 80, 255))
    d.line([(9, 12), (14, 15), (19, 13), (22, 18)], fill=(110, 150, 190, 255))     # a river
    for x, y in ((9, 10), (15, 10), (20, 11), (11, 16), (17, 17), (21, 21), (13, 21), (8, 18), (18, 8)):
        d.line([(x - 1, y - 1), (x + 1, y + 1)], fill=(156, 60, 44, 255))
        d.line([(x - 1, y + 1), (x + 1, y - 1)], fill=(156, 60, 44, 255))
    out = outline(im)
    out.save(path)


def pack_on_hook(path):
    """His pack, empty, on a peg on the cottage wall (1 x 2 tiles: the wall and the wainscot)."""
    im = Image.new("RGBA", (16, 32))
    d = ImageDraw.Draw(im)
    rect(d, 7, 4, 8, 6, (96, 70, 44))                        # the peg
    rect(d, 6, 6, 9, 7, (112, 68, 38))                       # the loop over it
    rect(d, 4, 8, 11, 24, (150, 128, 84))                    # the empty pack, hanging flat
    rect(d, 4, 8, 7, 22, (184, 162, 112))
    rect(d, 10, 9, 11, 24, (118, 98, 62))
    rect(d, 4, 8, 11, 12, (134, 112, 72))                    # the flap
    rect(d, 5, 12, 5, 24, (112, 68, 38))                     # straps
    rect(d, 10, 12, 10, 24, (112, 68, 38))
    rect(d, 7, 15, 8, 16, (206, 178, 92))                    # buckle
    outline(im).save(path)


def brambles(season, rng):
    """A bramble layer for the amphitheatre's area (AMPH, in pixels): arching canes with thorns, and
    leaves in the season's colours (bare in winter), on a shadow."""
    x0, y0, w, h = AMPH
    im = Image.new("RGBA", (w * 16, h * 16))
    d = ImageDraw.Draw(im)
    cane = (96, 46, 58) if season != "winter" else (110, 74, 66)
    cane_lit = (138, 74, 80) if season != "winter" else (150, 112, 96)
    leaves = {"spring": [(58, 104, 44), (82, 138, 56), (116, 170, 72)],
              "summer": [(52, 96, 40), (74, 128, 50), (104, 158, 64)],
              "fall": [(140, 70, 34), (186, 108, 42), (214, 150, 60)],
              "winter": []}[season]
    berry = (70, 34, 70) if season == "summer" else None
    for cx, cy in CLUMPS:
        px, py = int((cx - x0) * 16), int((cy - y0) * 16)
        d.ellipse([px - 11, py - 3, px + 11, py + 2], fill=(40, 34, 30, 90))          # the shadow under it
        canes = []
        for k in range(9):
            dx = rng.randint(-11, 11)
            top = rng.randint(8, 14)
            pts = [(px + dx // 3, py), (px + dx // 2 + rng.randint(-1, 1), py - top // 2), (px + dx, py - top),
                   (px + dx + (2 if dx >= 0 else -2), py - top + 3)]
            d.line(pts, fill=cane + (255,))
            d.point(pts[2], fill=cane_lit + (255,))
            canes.append(pts)
            for p in pts[1:3]:
                d.point((p[0] + 1, p[1]), fill=cane_lit + (255,))                   # thorns
        if leaves:
            for _ in range(26):
                pts = rng.choice(canes)
                a, b = rng.choice(list(zip(pts, pts[1:])))
                t = rng.random()
                lx, ly = int(a[0] + (b[0] - a[0]) * t), int(a[1] + (b[1] - a[1]) * t)
                c = rng.choice(leaves)
                d.rectangle([lx - 1, ly - 1, lx, ly], fill=c + (255,))
                d.point((lx - 1, ly - 1), fill=leaves[-1] + (255,))
            if berry:
                for _ in range(3):
                    pts = rng.choice(canes)
                    d.point(pts[2], fill=berry + (255,))
    return outline_leaves(im)


def outline_leaves(im):
    """Outlines on the brambles' solid pixels only (the shadow stays soft)."""
    px = im.load()
    w, h = im.size
    add = {}
    for y in range(h):
        for x in range(w):
            if px[x, y][3] == 255:
                continue
            near = [px[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if 0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] == 255]
            if near and rng_free_edge(px, x, y, w, h):
                base = max(set(n[:3] for n in near), key=[n[:3] for n in near].count)
                add[(x, y)] = shade(base)
    for k, v in add.items():
        px[k] = v
    return im


def rng_free_edge(px, x, y, w, h):
    """Only outline below and beside a leaf or cane, not above it (light from above, as vanilla)."""
    return not (y + 1 < h and px[x, y + 1][3] == 255 and not (y - 1 >= 0 and px[x, y - 1][3] == 255))


def amphitheatre_patches():
    """The brambles drawn onto the amphitheatre's own seat tiles, per season: a sheet each, the same
    layout every season (the bramble tiles, then the same tiles clean, for the event to swap), and a
    map patch each (assets/EmmettAmph_<season>.tmx) for the Buildings2 layer. Returns the tiles, in
    the order the event cuts them: (x, y, the clean tile's index)."""
    root, tilesets, layers = load_tmx(os.path.join(PACK, "assets", "GildedVale.tmx"), os.path.join(PACK, "assets"))
    first = next(int(t.get("firstgid")) for t in root.findall("tileset") if t.find("image").get("source").endswith("GildedValeBuildings.png"))
    cols = next(int(t.get("columns")) for t in root.findall("tileset") if t.find("image").get("source").endswith("GildedValeBuildings.png"))
    mw, _, data = layers["Buildings2"]
    x0, y0, w, h = AMPH
    changed = None
    sheets = {}
    for season in SEASONS:
        src = Image.open(os.path.join(PACK, "assets", f"{season}_GildedValeBuildings.png")).convert("RGBA")
        clean = Image.new("RGBA", (w * 16, h * 16))
        for ty in range(h):
            for tx in range(w):
                g = data[(y0 + ty) * mw + x0 + tx]
                if g:
                    i = g - first
                    clean.paste(src.crop(((i % cols) * 16, (i // cols) * 16, (i % cols) * 16 + 16, (i // cols) * 16 + 16)), (tx * 16, ty * 16))
        mask = brambles(season, random.Random(9))
        grown = clean.copy()
        # brambles only where the seats are (not over the grass and cliff round the art's edge)
        seat = clean.getchannel("A").point(lambda a: 255 if a else 0)
        cut = Image.new("RGBA", mask.size)
        cut.paste(mask, (0, 0), seat)
        grown.alpha_composite(cut)
        tiles = [(tx, ty) for ty in range(h) for tx in range(w)
                 if grown.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16)).tobytes() != clean.crop((tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16)).tobytes()]
        changed = sorted(set(tiles) | set(changed or []))
        sheets[season] = (clean, grown)
    n = len(changed)
    out = []
    for season in SEASONS:
        clean, grown = sheets[season]
        sheet = Image.new("RGBA", (16 * 8, 16 * ((2 * n + 7) // 8)))
        for k, (tx, ty) in enumerate(changed):
            box = (tx * 16, ty * 16, tx * 16 + 16, ty * 16 + 16)
            sheet.paste(grown.crop(box), ((k % 8) * 16, (k // 8) * 16))
            sheet.paste(clean.crop(box), (((n + k) % 8) * 16, ((n + k) // 8) * 16))
        name = f"emmett/amph_brambles_{season}.png"
        sheet.save(os.path.join(PACK, "assets", name))
        grid = [[0] * w for _ in range(h)]
        for k, (tx, ty) in enumerate(changed):
            grid[ty][tx] = k + 1
        write_tmx(os.path.join(PACK, "assets", f"EmmettAmph_{season}.tmx"), w, h, name, sheet.size,
                  f"z_blinxxy.MuseArt_EmmettAmph_{season}", {"Buildings2": grid})
    # cut west to east, top row first, each clump's tiles together as near as that goes
    order = sorted(changed, key=lambda t: (t[0] + x0 > 36, t[1], t[0]))
    return [(tx + x0, ty + y0, n + changed.index((tx, ty))) for tx, ty in order]


def write_tmx(path, w, h, image, size, tileset, layer_grids):
    """A small map patch: one tilesheet, the layers given (grids of 1-based tile numbers), an empty Back."""
    def layer(lid, name, grid):
        rows = ",\n".join(",".join(str(v) for v in row) for row in grid)
        return (f'  <layer id="{lid}" name="{name}" width="{w}" height="{h}">\n'
                f'    <data encoding="csv">\n{rows}\n</data>\n  </layer>')
    count = (size[0] // 16) * (size[1] // 16)
    parts = ['<?xml version="1.0" encoding="UTF-8"?>',
             f'<map version="1.4" tiledversion="1.4.2" orientation="orthogonal" renderorder="right-down" '
             f'width="{w}" height="{h}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="{len(layer_grids) + 2}" nextobjectid="1">',
             f'  <tileset firstgid="1" name="{tileset}" tilewidth="16" tileheight="16" tilecount="{count}" columns="{size[0] // 16}">',
             f'    <image source="{image}" width="{size[0]}" height="{size[1]}"/>',
             '  </tileset>',
             layer(1, "Back", [[0] * w for _ in range(h)])]
    parts += [layer(n + 2, name, grid) for n, (name, grid) in enumerate(layer_grids.items())]
    parts.append('</map>')
    open(path, "w", encoding="utf-8", newline="\n").write("\n".join(parts) + "\n")


# ----------------------------------------------------------------------------------------- build

def lift(prefix, text, store):
    """Every quoted line and each quickQuestion's question and answers into i18n keys <prefix>.01,
    .02... (as build_tess_events.py)."""
    n = 0

    def add(t):
        nonlocal n
        n += 1
        k = f"{prefix}.{n:02}"
        assert store.get(k, t) == t, k
        store[k] = t
        return "{{i18n:" + k + "}}"

    def sub(m):
        if m.group(1) is not None:
            return '"' + (add(m.group(1)) if m.group(1).strip() else m.group(1)) + '"'
        parts = m.group(2).split("#")
        return "quickQuestion " + "#".join(add(p) if p.strip() else p for p in parts) + "(break)"

    return re.sub(r'"([^"]*)"|quickQuestion (.*?)\(break\)', sub, text)


def splice(i18n):
    """The events' text into the dev pack's i18n, one block straight after Emmett's own lines."""
    path = os.path.join(PACK, "i18n", "default.json")
    s = open(path, encoding="utf-8").read()
    s = re.sub(r'  "emmett\.(event|furniture)\.[^"]*": "(?:[^"\\]|\\.)*",\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in i18n.items())
    anchor = re.search(r'  "emmett\.dialogue\.dating_EmmettWren": .*\n', s)
    assert anchor
    s = s[:anchor.end()] + block + s[anchor.end():]
    open(path, "w", encoding="utf-8", newline="\n").write(s)


def main():
    os.makedirs(os.path.join(PACK, "assets", "emmett"), exist_ok=True)
    props(os.path.join(PACK, "assets", "emmett", "event_props.png"))
    republic_map(os.path.join(PACK, "assets", "emmett", "republic_map.png"))
    pack_on_hook(os.path.join(PACK, "assets", "emmett", "pack_hook.png"))
    write_tmx(os.path.join(PACK, "assets", "EmmettPackHook.tmx"), 1, 2, "emmett/pack_hook.png", (16, 32),
              "z_blinxxy.MuseArt_EmmettPackHook", {"Front": [[1], [2]]})
    tiles = amphitheatre_patches()

    i18n = {}
    k2, s2 = heart2()
    k4, s4 = heart4()
    k6, s6 = heart6(False, tiles)
    k6t, s6t = heart6(True, tiles)
    k8, s8 = heart8(False)
    k8t, s8t = heart8(True)
    k10, s10 = heart10()
    vale = {k2: lift("emmett.event.heart2", s2, i18n), k4: lift("emmett.event.heart4", s4, i18n),
            k10: lift("emmett.event.heart10", s10, i18n)}
    vale_no_trips = {k6: lift("emmett.event.heart6", s6, i18n)}
    vale_trips = {k6t: lift("emmett.event.heart6", s6t, i18n)}           # the same lines, the same keys
    house_no_trips = {k8: lift("emmett.event.heart8", s8, i18n)}
    house_trips = {k8t: lift("emmett.event.heart8", s8t, i18n)}
    i18n["emmett.furniture.republic_map"] = "Emmett's Map of the Republic"

    trips_on, trips_off = {**WHEN, "ValeResidentsTownTrips": "true"}, {**WHEN, "ValeResidentsTownTrips": "false"}
    hook = {"X": 4, "Y": 11, "Width": 1, "Height": 2}
    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/EmmettEventProps", "FromFile": "assets/emmett/event_props.png"},
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/EmmettFurniture", "FromFile": "assets/emmett/republic_map.png"},
        {"Action": "EditData", "Target": "Data/Furniture", "Entries": {
            MAP_ITEM: f"{MAP_ITEM}/painting/2 2/2 2/1/800/-1/{{{{i18n:emmett.furniture.republic_map}}}}/0/Mods\\blinxxy.MuseArt\\EmmettFurniture/true"}},
        # (our maps' event lists start empty; Low, so another pack's own Load of one wins)
        {"Action": "Load", "Target": f"Data/Events/{VALE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"Action": "Load", "Target": f"Data/Events/{HOUSE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        {"LogName": "Emmett's 6-heart event (Fridays and Sundays)", "Action": "EditData", "Target": f"Data/Events/{VALE}",
         "When": trips_off, "Entries": vale_no_trips},
        {"LogName": "Emmett's 6-heart event (Sundays: his Fridays are the Saloon's)", "Action": "EditData",
         "Target": f"Data/Events/{VALE}", "When": trips_on, "Entries": vale_trips},
        {"LogName": "Emmett's 8-heart event", "Action": "EditData", "Target": f"Data/Events/{HOUSE}",
         "When": trips_off, "Entries": house_no_trips},
        {"LogName": "Emmett's 8-heart event (not Fridays: he's at the Saloon)", "Action": "EditData",
         "Target": f"Data/Events/{HOUSE}", "When": trips_on, "Entries": house_trips},
        # the amphitheatre, overgrown until his 6-heart event clears it
        *[{"LogName": f"The amphitheatre's brambles ({season})", "Action": "EditMap", "Target": f"Maps/{VALE}",
           "FromFile": f"assets/EmmettAmph_{season}.tmx",
           "ToArea": {"X": AMPH[0], "Y": AMPH[1], "Width": AMPH[2], "Height": AMPH[3]},
           "PatchMode": "Overlay", "Priority": "Late", "Update": "OnLocationChange",
           "When": {**WHEN, "Season": season, f"HasFlag |contains={CLEARED}": "false"}} for season in SEASONS],
        # his pack, empty, on a peg in his cottage after his 10-heart event
        {"LogName": "Emmett's pack on its peg", "Action": "EditMap", "Target": f"Maps/{COTTAGE}",
         "FromFile": "assets/EmmettPackHook.tmx", "ToArea": hook, "PatchMode": "Overlay", "Update": "OnLocationChange",
         "When": {**WHEN, "HasSeenEvent": EV[10]}},
    ]
    out = os.path.join(PACK, "data", "emmett_events.json")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("// Emmett Wren's heart events, the amphitheatre's brambles, his pack on its peg and his map of\n"
                "// the Republic. Generated by tools/build_emmett_events.py from the approved draft in\n"
                "// docs/vale_events.md: edit that script, not this file.\n")
        json.dump({"Changes": changes}, f, indent=2, ensure_ascii=False)
        f.write("\n")
    splice(i18n)
    print("wrote", out, "with", len(i18n), "lines;", len(tiles), "bramble tiles")


if __name__ == "__main__":
    main()

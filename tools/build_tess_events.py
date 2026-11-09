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
    vale = {k2: lift("tess.event.heart2", s2, i18n)}
    studio = {k4: lift("tess.event.heart4", s4, i18n)}
    with_twins = {k6: lift("tess.event.heart6", s6, i18n)}
    without = {k6b: lift("tess.event.heart6_alone", s6b, i18n)}
    changes = [
        {"Action": "Load", "Target": "Mods/blinxxy.MuseArt/TessEventProps", "FromFile": "assets/tess/event_props.png"},
        # (the Vale's and Studio III's event lists start empty: ours. Low, so another Vale
        # resident's events can load the Vale's list too)
        {"Action": "Load", "Target": f"Data/Events/{VALE}", "FromFile": "data/blank.json", "Priority": "Low"},
        {"Action": "Load", "Target": f"Data/Events/{STUDIO}", "FromFile": "data/blank.json"},
        {"Action": "EditData", "Target": f"Data/Events/{VALE}", "When": WHEN, "Entries": vale},
        {"Action": "EditData", "Target": f"Data/Events/{STUDIO}", "When": WHEN, "Entries": studio},
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

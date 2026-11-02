"""Writes Margot Pell's heart events ([CP] Pelican Palette/data/margot_events.json).

Built from readable pieces like Sable's and Julian's (build_sable_events.py).
The story is the approved draft in docs/margot_events.md (approved
2026-11-01). The game walks every scene; the player only answers.

Usage: build_margot_events.py <content pack dir>
"""
import json, os, sys
from cp_i18n import Strings
from build_sable_events import q, speak, script, quick

M = "MargotPell"
J = "JulianVane"
F = f"Friendship {M}"


def events():
    museum, robin, cottage, forest, saloon = {}, {}, {}, {}, {}

    # ---- 2 hearts: what's missing (the museum, a weekday afternoon) -------
    museum[f"blinxxy.MuseArt_Margot2h/{F} 500/DayOfWeek Mon Tue Wed Thu Fri Sat/Time 1300 1600"] = script(
        # (museart_route: up the aisle beside the reading desks.)
        "continue", "18 12", f"farmer 20 16 0 {M} 18 13 2", "skippable",
        "pause 600", "move farmer 0 -3 0", "move farmer -1 0 3", "pause 400",
        "message " + q("A small damaged painting lies on the desk under her magnifying lamp: a farmhouse, a third of it flaked away."),
        speak(M, "Come here. Quick, before the light moves.$u"),
        speak(M, "Most people think restoring is painting over the damage. It isn't. You look at what's left until it tells you what's missing.$u"),
        "pause 400",
        speak(M, "The fence runs this way. The shadow's long, so it's evening. So this gap...$7"),
        f"faceDirection {M} 1",
        quick([
            ("A gate.", [f"friendship {M} 30", speak(M, "...Yes. A gate. How did you —$6#$b#You looked. That's all it is.$h")]),
            ("Another fence post.", [f"friendship {M} 15", speak(M, "That's what I thought too. Then I saw the path.$7#$b#A gate.$u")]),
            ("I'd have to see the rest.", [f"friendship {M} 30", speak(M, "Good answer. Annoying, but good.$u")]),
        ]),
        f"faceDirection {M} 2", "pause 800",
        "message " + q("She dabs one tiny stroke into the gap, then sits back."),
        f"faceDirection {M} 1",
        speak(M, "I've touched a thousand paintings. I've never made one. Isn't that strange?$s"),
        "pause 300", "end",
    )

    # ---- 4 hearts: a place of her own (Robin's shop, a weekday morning) ---
    robin[f"blinxxy.MuseArt_Margot4h/{F} 1000/DayOfWeek Mon Tue Wed Thu Fri Sat/Time 900 1200/SawEvent blinxxy.MuseArt_Margot2h"] = script(
        "continue", "8 20", f"farmer 6 23 0 Robin 8 18 2 {M} 8 20 0", "skippable",
        "pause 600",
        speak("Robin", "...so a big north window, a workroom, a little kitchen. That's a cottage, not a shed."),
        speak(M, "North light doesn't change all day. It's the only light you can trust with paint.$u"),
        "move farmer 0 -2 0", "pause 300", f"faceDirection {M} 3", "faceDirection farmer 1",
        speak(M, "Oh! You weren't supposed to see this yet.$l#$b#I mean — it's not a secret. It's just... a lot.$l"),
        "pause 400",
        speak(M, "The museum sent me for a season. It's been more than that. Gus has started charging me for jam.$7#$b#I don't want to go back to a city where nothing's old enough to need me.$s"),
        f"faceDirection {M} 0", "pause 400",
        "message " + q("Robin unrolls the drawing: a small cottage with a big window."),
        speak("Robin", "Three days, if the weather holds. Where are we putting it?"),
        f"faceDirection {M} 3",
        # (The answer changes her line, not where it's built: by the river.)
        quick([
            ("Near the museum.", ["addMailReceived blinxxy.MuseArt_MargotByMuseum", speak(M, "Close to work. Sensible. Gunther will never let me leave.$h")]),
            ("Near the river.", ["addMailReceived blinxxy.MuseArt_MargotByRiver", speak(M, "Where the old painters sat. ...I like that.$7")]),
            ("Somewhere you can see the whole town.", ["addMailReceived blinxxy.MuseArt_MargotByTown", speak(M, "Then I'll know what needs fixing first.$u")]),
        ]),
        f"faceDirection {M} 0",
        speak("Robin", "Three days, then."),
        "pause 600", f"faceDirection {M} 3",
        speak(M, "I'm staying. I said it out loud. Now I have to.$l"),
        "blinxxy.MuseArt_MargotCommission",
        "pause 300", "end",
    )

    # ---- 6 hearts: the errand (her cottage, evening) ----------------------
    cottage[f"blinxxy.MuseArt_Margot6h/{F} 1500/Time 1800 2100/SawEvent blinxxy.MuseArt_Margot4h/SawEvent blinxxy.MuseArt_Sable6h"] = script(
        "continue", "4 5", f"farmer 6 8 0 {M} 2 5 0", "skippable",
        "pause 600", "move farmer 0 -3 0", "move farmer -3 0 3", "pause 300",
        "message " + q("Margot sits at her table with an unopened envelope from the city museum. She hasn't lit the lamp."),
        f"faceDirection {M} 1",
        speak(M, "I have to tell someone, and you're the only one who won't make it worse.$s"),
        speak(M, "Years ago I signed for a painting. A forest landscape signed 'E.' I sent it to Sable to be restored. Then Sable left the museum, and the painting left with them.$7"),
        speak(M, "The museum didn't send me here for the old paintings. Well — not only. They asked me to bring it back. Quietly.$s#$b#'Quietly.' As if I could do anything quietly.$a"),
        speak(M, "Sable's been restoring it for a year. I've seen it through the shop window. It's nearly finished. It's beautiful.$s"),
        quick([
            ("Report it. It's your job.", ["addMailReceived blinxxy.MuseArt_MargotReport", speak(M, "It is. ...It is my job.$7")]),
            ("Talk to Sable first.", ["addMailReceived blinxxy.MuseArt_MargotTalk", speak(M, "Talk to them. Like a person.$6#$b#That's either very wise or very obvious.$h")]),
            ("It's not my business.", ["addMailReceived blinxxy.MuseArt_MargotNotMine", speak(M, "No. But you listened anyway. That's the nicest kind of not-your-business.$u")]),
        ]),
        f"faceDirection {M} 0", "pause 600",
        "message " + q("She props the envelope against the lamp, still sealed."),
        "pause 300", "end",
    )

    # ---- 8 hearts: the river bend (the Forest river, a sunny morning) -----
    forest[f"blinxxy.MuseArt_Margot8h/{F} 2000/Time 800 1100/Weather sunny/SawEvent blinxxy.MuseArt_Margot6h"] = script(
        "continue", "88 38", f"farmer 92 35 2 {M} 87 38 2", "skippable",
        "pause 600", "move farmer 0 3 2", "move farmer -3 0 3", "pause 300",
        f"faceDirection {M} 1",
        speak(M, "Every painting was painted from somewhere. I've been looking for this one's spot for a week.$u"),
        "message " + q("She holds up a small restored painting of a river bend, lowers it, and walks along the bank. You walk beside her."),
        f"move {M} 0 2 2", "move farmer 0 2 2", f"move {M} 0 0 1", "pause 400",
        "message " + q("She stops and holds it up. The bend, the willow, the light: it matches."),
        speak(M, "...Here. They stood exactly here.$6"),
        speak(M, "A hundred years ago someone stood where we're standing and thought this was worth keeping.$l"),
        "blinxxy.MuseArt_MargotErrand",
        speak(M, "They'll stop sending me letters now. I'm just... a conservator who lives in a valley.$s#$b#I think that's the best thing I've ever been.$h"),
        "pause 300", "end",
    )

    # ---- 10 hearts: under the paint (her cottage, after hours; dating) ----
    cottage[f"blinxxy.MuseArt_Margot10h/Dating {M}/{F} 2500/Time 2000 2400/SawEvent blinxxy.MuseArt_Margot8h"] = script(
        "continue", "4 5", f"farmer 6 8 0 {M} 2 5 0", "skippable",
        "pause 600", "move farmer 0 -3 0", "move farmer -3 0 3", "pause 300",
        f"faceDirection {M} 1",
        speak(M, "Look. Hold it up to the light.$u"),
        "message " + q("Faint lines show through the paint of an old painting: a drawing underneath, a figure the painter started and painted over."),
        speak(M, "They changed their mind. Right there, in the middle. Painters do. It's my favourite thing to find.$u"),
        speak(M, "Everything I've ever fixed was someone else's mind, changing.$7#$b#I want to make something that's mine. I'm terrified it'll be awful.$s"),
        "message " + q("She sets a small blank canvas on the table, then a second one."),
        speak(M, "Will you paint with me? So if mine's awful, at least we'll both have one.$l"),
        quick([
            ("Let's paint each other.", ["addMailReceived blinxxy.MuseArt_MargotEachOther", speak(M, "Hold still. No — don't hold that still. You look like a statue.$h")]),
            ("Let's paint the window.", ["addMailReceived blinxxy.MuseArt_MargotWindow", speak(M, "The north window. At night. Nobody paints windows at night. Good.$7")]),
        ]),
        "globalFade 0.007", "pause 1500", "globalFadeToClear 0.007", "pause 600",
        "message " + q("The two canvases dry on the windowsill."),
        # (What you painted, by your answer: MargotEvents.Paintings.)
        "blinxxy.MuseArt_MargotPaintings",
        "pause 300", "end",
    )

    # ---- 14 hearts: both names (married; the museum, evening) -------------
    museum[f"blinxxy.MuseArt_Margot14h/G PLAYER_NPC_RELATIONSHIP Current {M} Married/{F} 3500/Time 1700 1900/SawEvent blinxxy.MuseArt_Margot10h"] = script(
        "continue", "18 12", f"farmer 20 16 0 {M} 18 12 0 Gunther 6 8 2", "skippable",
        "pause 600", "move farmer 0 -3 0", "move farmer -1 0 3", "pause 300", f"faceDirection {M} 1",
        speak(M, "Read the card.$u"),
        "blinxxy.MuseArt_MargotCard",
        speak(M, "I've signed a thousand condition reports. I've never had my name on a card.$l#$b#Ours. Our names.$l"),
        speak("Gunther", "It's the most popular painting in the museum."),
        speak(M, "It's the only painting in the museum, Gunther.$a"),
        speak("Gunther", "And the most popular."),
        "pause 600",
        speak(M, "You don't make it new. You make it able to keep being old.$h#$b#Let's keep being old.$h"),
        "pause 300", "end",
    )
    # ---- Julian and Margot: "Between" (the Saloon, a visit evening) ------
    # Once you've met them both, on one of his visit days, in the hour
    # they're both there. Their lines on his later visits are
    # MargotEvents.BetweenLines.
    saloon[f"blinxxy.MuseArt_MargotJulian/DayOfMonth 22 23 24/Time 1700 1800/G PLAYER_HAS_MET Current {J}, PLAYER_HAS_MET Current {M}"] = script(
        "continue", "17 21", f"farmer 20 21 3 {J} 16 22 1 {M} 21 23 0", "skippable",
        "pause 400", f"move {M} 0 -1 0", f"move {M} -4 0 3", "pause 300",
        speak(M, "You're the critic.$7"),
        speak(J, "And you must be the restorer. Gus warned me.$u"),
        speak(M, "Conservator.$a"),
        speak(J, "Of course. The people who paint over other people's paintings.$u"),
        speak(M, "We don't paint over. We paint between. It's a hundred years of training, and you could fit what you know about it on a napkin.$a"),
        "pause 400",
        speak(J, "Then tell me, conservator. When you fill a gap in a dead man's painting, whose brushstroke is it?$7"),
        speak(M, "His. If I've done it right, nobody can tell it was ever missing.$7"),
        speak(J, "So the best work you'll ever do, no one will ever see.$0"),
        "pause 800",
        speak(M, "...Yes.$s"),
        f"faceDirection {M} 1", f"faceDirection {J} 1", "pause 300",
        speak(J, "Settle it, farmer. Is a restoration art?$u"),
        quick([
            ("It's art.", [f"friendship {M} 30", speak(M, "Thank you.$h"), speak(J, "Two against one. I'll be writing about this.$u")]),
            ("It's care. That's different, and just as good.", [f"friendship {M} 30", f"friendship {J} 30", "pause 600",
                speak(J, "...That's annoyingly well put.$7"), speak(M, "Write that down, Julian.$l")]),
            ("Ask me again after a drink.", [f"friendship {J} 30",
                "message " + q("Gus sets down two glasses without being asked. They clink them, grudgingly.")]),
        ]),
        f"faceDirection {M} 3", "pause 300",
        speak(M, "I have notes on your review.$u"),
        speak(J, "I have notes on your 'between'.$u"),
        "message " + q("They both look almost pleased."),
        "pause 300", "end",
    )
    return museum, robin, cottage, forest, saloon


def main():
    pack = sys.argv[1]
    museum, robin, cottage, forest, saloon = events()
    strings = Strings(pack, ["margot-event."])
    for scripts in (museum, robin, cottage, forest, saloon):
        for key, value in scripts.items():
            scripts[key] = strings.script("margot-event." + key.split("/")[0].split("_")[-1].lower(), value)
    changes = [
        {"Action": "EditData", "Target": "Data/Events/ArchaeologyHouse", "Entries": museum},
        {"Action": "EditData", "Target": "Data/Events/ScienceHouse", "Entries": robin},
        # Her cottage is ours, so its event list starts empty.
        {"Action": "Load", "Target": "Data/Events/blinxxy.MuseArt_MargotCottage", "FromFile": "data/blank.json"},
        {"Action": "EditData", "Target": "Data/Events/blinxxy.MuseArt_MargotCottage", "Entries": cottage},
        {"Action": "EditData", "Target": "Data/Events/Forest", "Entries": forest},
        {"Action": "EditData", "Target": "Data/Events/Saloon", "Entries": saloon},
    ]
    header = ("// Margot Pell's heart events. Generated by tools/build_margot_events.py from\n"
              "// the approved draft in docs/margot_events.md: edit that script, not this file.\n")
    out = os.path.join(pack, "data", "margot_events.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    strings.save()
    print("wrote", out, "and", strings.path)


if __name__ == "__main__":
    main()

"""Writes Sable's heart events ([CP] Art Studio/data/sable_events.json).

Event scripts are long '/'-separated strings with nested quoting, so they're
built here from readable pieces instead of being edited by hand. The story
is the approved draft in docs/phase6_romance.md.

Usage: build_sable_events.py <content pack dir>
"""
import json, os, sys


def q(s):
    return '"' + s + '"'


def speak(who, line):
    return f"speak {who} {q(line)}"


def script(*cmds):
    return "/".join(cmds)


def quick(answers, question=" "):
    """A quickQuestion: answers are (text, [commands]); an answer's commands
    are separated by backslashes."""
    head = "quickQuestion " + question + "#" + "#".join(a for a, _ in answers)
    return head + "".join("(break)" + "\\".join(cmds) for _, cmds in answers)


def events():
    shop, forest, josh = {}, {}, {}

    # ---- 2 hearts: "Don't laugh" (the shop, after closing) ----------------
    shop["blinxxy.MuseArt_Sable2h/Friendship Sable 500/Time 1800 2000/!DayOfWeek Wed"] = script(
        "continue", "10 6", "farmer 7 9 0 Sable 12 8 0", "skippable",
        "pause 800", "move farmer 0 -1 0", "move farmer 4 0 1", "pause 600",
        "emote Sable 16", "faceDirection Sable 3", "pause 300",
        speak("Sable", "Oh. It's after hours.$6#$b#...You can stay. Don't touch anything."),
        "faceDirection Sable 0", "pause 500",
        speak("Sable", "It's a landscape. Cindersap Forest, about forty years ago. The museum sent it to me to restore. I was meant to send it back."),
        "pause 300",
        speak("Sable", "Look at the light on the water. Whoever painted this stood right where my shop is now. Same river, same bend.$s"),
        speak("Sable", "It's signed 'E.' Just the initial. No records. Nobody knows who painted it."),
        "faceDirection Sable 3", "pause 400",
        speak("Sable", "I kept it. Then I quit my job and moved here. It's why I'm here.#$b#Don't laugh.$u"),
        quick([
            ("That's not funny. That's brave.", ["friendship Sable 30", speak("Sable", "...Brave is a strong word. Stubborn, maybe.$l")]),
            ("So you stole it?", [speak("Sable", "Borrowed. Indefinitely. ...With paperwork. Mostly.$a")]),
            ("Who do you think E. was?", ["friendship Sable 30", speak("Sable", "Somebody who loved this valley. You can see it in how they did the water.$h")]),
        ]),
        "pause 400", "faceDirection Sable 0", "pause 400",
        speak("Sable", "Go home, farmer. The light's gone."),
        "pause 300", "end",
    )

    # ---- 4 hearts: side by side (the riverbank, sunny morning) ------------
    forest["blinxxy.MuseArt_Sable4h/Friendship Sable 1000/Time 800 1000/Weather sunny/DayOfWeek Mon Tue Thu Sat/SawEvent blinxxy.MuseArt_Sable2h"] = script(
        "continue", "89 39", "farmer 93 35 2 Sable 88 40 0", "skippable",
        "addBigProp 88 39 blinxxy.MuseArt_Easel", "addBigProp 90 39 blinxxy.MuseArt_Easel",
        "animate Sable false true 300 32 32 33 33 32 34",
        "pause 600", "move farmer 0 4 2", "move farmer -2 0 3", "move farmer 0 1 2", "move farmer -1 0 3", "pause 300",
        "stopAnimation Sable", "faceDirection Sable 1",
        speak("Sable", "You're late. I set up a canvas for you an hour ago.$u"),
        "faceDirection Sable 0", "faceDirection farmer 0", "animate Sable false true 300 32 32 33 33 32 34", "pause 600",
        "globalFade 0.007", "pause 1500", "globalFadeToClear 0.007", "pause 600",
        "stopAnimation Sable", "faceDirection Sable 1", "pause 300",
        speak("Sable", "Let me see.#$b#Your water is all one blue. Water's never one blue."),
        speak("Sable", "When you mix, don't chase the color. Let it come to you. Hold steady, and only move when it moves."),
        "pause 300",
        speak("Sable", "Here. It's the first good brush I ever owned. It's been in a drawer for ten years.#$b#It should be painting.$l"),
        "addItem (O)blinxxy.MuseArt_Brush_sable 1", "pause 300",
        "faceDirection farmer 3", "faceDirection Sable 0", "pause 600",
        "message " + q("Sable's canvas is a careful pencil sketch. There's no paint on it."),
        speak("Sable", "Just a study.$u"),
        quick([
            ("You never finish them, do you?", ["friendship Sable 30", "faceDirection Sable 3", speak("Sable", "...No. Not anymore.$s")]),
            ("It's beautiful already.", ["friendship Sable 30", "faceDirection Sable 3", speak("Sable", "It's a sketch. But... thank you.$l")]),
        ]),
        "pause 400", "end",
    )

    # ---- 6 hearts: the letter (the shop, evening) --------------------------
    # Your answer is remembered (mail flags) for the 10-heart event, and the
    # next day Sable says what they did with the letter.
    shop["blinxxy.MuseArt_Sable6h/Friendship Sable 1500/Time 1800 2000/SawEvent blinxxy.MuseArt_Sable4h"] = script(
        "continue", "5 5", "farmer 7 9 0 Sable 3 4 2", "skippable",
        "pause 600", "move farmer 0 -3 0", "move farmer -4 0 3", "faceDirection farmer 0", "pause 600",
        speak("Sable", "The Zuzu City Museum wants me back. Head of restoration. Double the pay, my old studio. A window.$s"),
        "pause 400",
        speak("Sable", "I told myself I came here for the painting. But I've been restoring it for a year. I could finish it tomorrow."),
        "pause 400",
        speak("Sable", "I haven't painted anything of my own in ten years.#$b#A critic called my work 'technically flawless and completely empty'. I believed them.#$b#It was easier to fix other people's work. You can't fail at someone else's painting.$s"),
        quick([
            ("Stay.", ["friendship Sable 40", "addMailReceived blinxxy.MuseArt_LetterStay", "addConversationTopic blinxxy.MuseArt_LetterStay 3",
                       speak("Sable", "...You'd say that to anyone who sells you canvases.#$b#No. You wouldn't.$l")]),
            ("It's your call. I'll miss you either way.", ["friendship Sable 20", "addMailReceived blinxxy.MuseArt_LetterYourCall", "addConversationTopic blinxxy.MuseArt_LetterYourCall 3",
                       speak("Sable", "That's the worst possible answer. It's also the kind one.$s")]),
            ("Paint something first. Then decide.", ["friendship Sable 40", "addMailReceived blinxxy.MuseArt_LetterPaintFirst", "addConversationTopic blinxxy.MuseArt_LetterPaintFirst 3",
                       speak("Sable", "Something of mine?$6#$b#...Fine. One thing. Then I'll decide.")]),
        ]),
        "pause 500", "end",
    )

    # ---- 8 hearts: "E." (Evelyn's house, Wednesday afternoon) --------------
    josh["blinxxy.MuseArt_Sable8h/Friendship Sable 2000/DayOfWeek Wed/Time 1430 1700/SawEvent blinxxy.MuseArt_Sable6h"] = script(
        "continue", "5 20", "farmer 9 23 0 Sable 5 20 3 Evelyn 1 20 1 George 16 21 2", "skippable",
        "pause 600", "move farmer 0 -4 0", "move farmer -3 0 3", "faceDirection farmer 3", "pause 400",
        speak("Sable", "I wanted you to see this, Evelyn. You know the forest better than anyone."),
        "pause 300",
        "message " + q("Sable unwraps a small landscape from brown paper: the river bend in Cindersap Forest, signed 'E.'"),
        "pause 800", "emote Evelyn 16", "pause 400",
        speak("Evelyn", "Oh my. Oh, my goodness."),
        "pause 500",
        speak("Evelyn", "I painted this. The summer before I married George. I sat on the riverbank every morning for a month."),
        "emote Sable 16",
        speak("Sable", "You're E.$6"),
        speak("Evelyn", "Evelyn. I always signed with just the E. I thought it looked very mysterious.$h#$b#I gave it to a traveling dealer for a song. We needed the money for the farm."),
        "pause 300",
        speak("Evelyn", "I stopped painting after that. There was always something more useful to do. You know how it is."),
        "pause 600", "faceDirection George 3",
        speak("George", "She was good, too. Better than good."),
        speak("Evelyn", "Oh, hush.#$b#...Sable, dear. Would you paint with me one morning? I'm slow now. My hands shake."),
        "pause 300",
        speak("Sable", "I'd like that more than anything.$l"),
        "pause 500", "emote Evelyn 20", "pause 300",
        "globalFade 0.005", "pause 1200",
        "message " + q("Sable and Evelyn talk about the river bend and old brushes until the tea goes cold."),
        "friendship Evelyn 50", "pause 300", "end",
    )
    # ---- 10 hearts: the portrait (the shop, after hours; dating) ----------
    # The first line answers your 6-heart choice, and the portrait is made
    # from your farmer as they look today (both by the C# mod's event commands).
    shop["blinxxy.MuseArt_Sable10h/Dating Sable/Friendship Sable 2500/Time 1800 2000/SawEvent blinxxy.MuseArt_Sable8h"] = script(
        "continue", "7 6", "farmer 7 9 0 Sable 8 7 3", "skippable",
        "addBigProp 7 6 blinxxy.MuseArt_Easel",
        "pause 700", "move farmer 0 -1 0", "pause 500",
        "blinxxy.MuseArt_LetterAnswer",
        speak("Sable", "It's the first original I've finished in ten years.#$b#Don't say anything until you've looked at it properly."),
        "pause 400", "faceDirection Sable 0", "pause 300",
        "message " + q("Sable pulls the sheet away. It's a portrait of you."),
        "pause 800", "faceDirection Sable 3",
        speak("Sable", "Technically flawless. Completely full.$l#$b#...That's what the critic should have said about this one."),
        quick([
            ("It's the best thing I've ever seen.", ["friendship Sable 50", speak("Sable", "You haven't seen many paintings.#$b#...I'll take it.$l")]),
            ("(Hug them.)", ["friendship Sable 50", "faceDirection farmer 1", "emote Sable 20", speak("Sable", "Careful. It's still wet.$l")]),
        ]),
        "pause 400",
        speak("Sable", "It's yours. Hang it somewhere I can see it when I visit.$l"),
        "blinxxy.MuseArt_GiveFarmerPortrait",
        "pause 600", "end",
    )

    # ---- 14 hearts: the exhibition (married; the shop, a sunny evening) ----
    shop["blinxxy.MuseArt_Sable14h/Spouse Sable/Friendship Sable 3500/Time 1800 2000/Weather sunny/SawEvent blinxxy.MuseArt_Sable10h"] = script(
        "continue", "8 6",
        "farmer 7 9 0 Sable 12 8 0 Evelyn 8 4 0 George 7 4 0 Leah 11 5 0 Elliott 6 6 1 Emily 6 4 0 Haley 10 5 3 Robin 2 6 0 Lewis 5 8 1",
        "skippable",
        "pause 800", "move farmer 0 -1 0", "pause 500",
        "message " + q("Sable's paintings hang on every wall. Evelyn's landscape is in the middle, and your portrait is beside it."),
        "pause 300", "faceDirection Lewis 0",
        speak("Lewis", "A real gallery, in our little valley! This is going in the newsletter."),
        "faceDirection Leah 2",
        speak("Leah", "This one has... sculptural color. Don't tell Sable I said that.$h"),
        "faceDirection Elliott 2",
        speak("Elliott", "And so the evening unfolded, every canvas a window into a quieter heart..."),
        "faceDirection Haley 3",
        speak("Haley", "Okay, fine. Painting is kind of cool. Does Sable teach lessons?"),
        speak("Robin", "I built every one of those frames, you know. Just saying."),
        speak("Evelyn", "Forty years, and it's back on a wall in the forest. Isn't that something, George?"),
        speak("George", "Hmph. Best painting in the room.#$b#...After the farmer's."),
        "pause 500", "move Sable -4 0 3", "faceDirection farmer 1", "pause 300",
        speak("Sable", "Ten years of nothing. Then one farmer walks in with a lot of brown on them.$l"),
        "pause 300",
        speak("Sable", "Thank you for ruining my quiet, empty life.$h"),
        "emote Sable 20", "friendship Sable 50", "pause 800", "end",
    )
    return shop, forest, josh


def main():
    pack = sys.argv[1]
    shop, forest, josh = events()
    changes = [
        {"Action": "EditData", "Target": "Data/Events/blinxxy.MuseArt_ArtShop", "Entries": shop},
        {"Action": "EditData", "Target": "Data/Events/Forest", "Entries": forest},
        {"Action": "EditData", "Target": "Data/Events/JoshHouse", "Entries": josh},
        # The day after the letter: what Sable did with it, by your answer.
        {"Action": "EditData", "Target": "Characters/Dialogue/Sable", "Entries": {
            "blinxxy.MuseArt_LetterStay": "I tore the letter up this morning.$l#$b#Don't look so pleased with yourself.$u",
            "blinxxy.MuseArt_LetterYourCall": "I made my call. The letter's in the stove.$s#$b#...I'm staying. I'm not sure I'd have decided that if you'd made it for me.",
            "blinxxy.MuseArt_LetterPaintFirst": "I wrote back and said I needed a season to think.$u#$b#Then I bought myself a canvas. From my own shop. Retail price.$h",
        }},
        # Evelyn, once she's remembered.
        {"Action": "EditData", "Target": "Characters/Dialogue/Evelyn",
         "When": {"HasSeenEvent": "blinxxy.MuseArt_Sable8h"},
         "Entries": {
             "Wed": "Sable and I painted the river bend this morning. My hands remembered more than I thought they would.$h",
             "Sat": "I found my old brushes in the attic. George says they smell like the summer before we married.$h",
         }},
    ]
    header = ("// Sable's heart events. Generated by tools/build_sable_events.py from the\n"
              "// approved draft in docs/phase6_romance.md: edit that script, not this file.\n")
    out = os.path.join(pack, "data", "sable_events.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    print("wrote", out)


if __name__ == "__main__":
    main()

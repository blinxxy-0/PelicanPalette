"""Writes Julian Vane's heart events ([CP] Pelican Palette/data/julian_events.json).

Built from readable pieces like Sable's (build_sable_events.py). The story
is the approved draft in docs/julian_events.md (approved 2026-10-31, with
the 10-heart line reworded by the user). He's only in the valley the 22nd to
the 24th, so every event waits for one of those days, where he is then.

Usage: build_julian_events.py <content pack dir>
"""
import json, os, sys
from cp_i18n import Strings
from build_sable_events import q, speak, script, quick

J = "JulianVane"


def events():
    back, museum, bus, saloon, shop = {}, {}, {}, {}, {}

    # ---- 2 hearts: the third one (the back room, a visit morning) ---------
    back[f"blinxxy.MuseArt_Julian2h/Friendship {J} 500/DayOfMonth 22 24/Time 1000 1300/G blinxxy.MuseArt_PORTFOLIO_KEPT 3"] = script(
        "continue", "5 6", f"farmer 5 9 0 {J} 5 5 0 Sable -50 -50 2", "skippable",
        "pause 600", "move farmer 0 -2 0", "move farmer 1 0 1", "faceDirection farmer 0", "pause 400",
        speak(J, "Don't hover. Come and look with me.$7"),
        "move farmer 0 -1 0", "pause 600",
        speak(J, "This page. The first one is careful. The second is clever. The third...$7"),
        "pause 800", f"faceDirection {J} 1", "faceDirection farmer 3",
        speak(J, "Who painted the third one?$0"),
        quick([
            ("I did.", [f"friendship {J} 30", speak(J, "Did you.$6#$b#Hm.$0")]),
            ("Sable taught me.", [f"friendship {J} 30", "pause 400", speak(J, "...Did they.$s#$b#They were always a good teacher. Even when nobody asked.$s")]),
        ]),
        "warp Sable 5 8", "faceDirection Sable 0", "pause 400",
        f"faceDirection {J} 2", "faceDirection farmer 2", "pause 1200",
        "message " + q("Neither of them speaks. Sable takes a box of frames from beside the door and leaves."),
        "move Sable 0 1 2", "warp Sable -50 -50", "pause 800",
        f"faceDirection {J} 1", "faceDirection farmer 3",
        speak(J, "Well. That was the warmest welcome I've had in years.$u"),
        "pause 300", "end",
    )

    # ---- 4 hearts: an honest review (the museum, a visit afternoon) -------
    museum[f"blinxxy.MuseArt_Julian4h/Friendship {J} 1000/DayOfMonth 22 23/Time 1300 1600/SawEvent blinxxy.MuseArt_Julian2h"] = script(
        # (The farmer starts inside the display room: walking in from the
        # library crossed the wall between them.)
        # (Gunther, offscreen behind his counter, on a tile that's free in
        # Stardew Valley Expanded's museum too.)
        "continue", "27 11", f"farmer 26 13 1 {J} 28 12 0 Gunther 6 8 2", "skippable",
        "pause 500", "move farmer 1 0 1", "faceDirection farmer 0", "pause 400",
        speak(J, "Your museum has room for exactly one painting. I've been deciding which of yours.$u"),
        f"faceDirection {J} 3", "faceDirection farmer 1", "pause 300",
        "blinxxy.MuseArt_JulianReview",
        speak(J, "But that's what I think. What were you trying to do?$7"),
        quick([
            ("Get it exactly right.", ["addMailReceived blinxxy.MuseArt_JulianExact", speak(J, "Then you did. Exactness is underrated. People only call it cold when they're afraid of it.$0")]),
            ("Make it feel like the place.", ["addMailReceived blinxxy.MuseArt_JulianFeeling", speak(J, "Then you did that too. That's the harder one.$h")]),
            ("I just like painting.", ["addMailReceived blinxxy.MuseArt_JulianJoy", "pause 800", speak(J, "...That's the best answer anyone's given me in twenty years.$l")]),
        ]),
        "pause 400",
        speak("Gunther", "We don't actually have a painting wall."),
        f"faceDirection {J} 0",
        speak(J, "You do now.$a"),
        "pause 300", "end",
    )

    # ---- 6 hearts: the evening bus (the bus stop, the 24th) ---------------
    bus[f"blinxxy.MuseArt_Julian6h/Friendship {J} 1500/DayOfMonth 24/Time 1730 1900/SawEvent blinxxy.MuseArt_Julian4h/SawEvent blinxxy.MuseArt_Sable6h"] = script(
        "continue", "21 11", f"farmer 16 12 1 {J} 22 11 0", "skippable",
        "pause 500",
        "message " + q("The evening bus pulls away without him."),
        "move farmer 5 0 1", "pause 300", f"faceDirection {J} 3",
        speak(J, "I missed it. On purpose.$u#$b#Don't tell anyone. It would ruin me.$u"),
        "pause 400", f"faceDirection {J} 0",
        speak(J, "Ten years ago I reviewed a young painter's first show. I wrote one sentence about it that everyone still quotes.$0#$b#'Technically flawless and completely empty.'$0"),
        speak(J, "I wrote it because it sounded good. I was right about the first half.$s"),
        f"faceDirection {J} 3", "pause 400",
        "message " + q("He takes an old exhibition catalogue from inside his coat. In the margin, next to the third painting, in pencil: 'the third one is not empty.'"),
        speak(J, "I wrote that the same night. I never published it. It didn't sound as good.$s"),
        quick([
            ("You have to tell Sable.", [speak(J, "I know. I've known for ten years. Knowing is the easy part.$s")]),
            ("Why tell me?", [speak(J, "Because you're the only person here who looks at both of us like we're worth looking at.$7")]),
            ("(Say nothing.)", [speak(J, "A critic's dream. An audience that doesn't argue.$h")]),
        ]),
        "pause 400",
        speak(J, "Lewis has offered to drive me back to the city tonight. I've told him what I think of his truck. He's driving me anyway.$u"),
        "pause 300", "end",
    )

    # ---- 8 hearts: the catalogue (the Saloon, a visit evening) ------------
    saloon[f"blinxxy.MuseArt_Julian8h/Friendship {J} 2000/DayOfMonth 22 23 24/Time 1600 1800/SawEvent blinxxy.MuseArt_Julian6h"] = script(
        "continue", "17 21", f"farmer 19 21 3 {J} 16 22 1", "skippable",
        "pause 600",
        "message " + q("Julian sits alone with a glass of wine he hasn't touched. The catalogue lies on the table in front of him."),
        speak(J, "I've tried to walk into that shop four times. I get as far as the yard.$s"),
        "pause 500",
        speak(J, "Would you give them this? Not from me. Just... give it to them.$s"),
        quick([
            ("I'll do it.", [f"friendship {J} 30", speak(J, "Thank you.$l#$b#That's two people in this valley I've said that to. Don't let it get around.$u")]),
            ("You should give it to them yourself.", [speak(J, "I know what I should do. I'm asking what you'll do.$a#$b#...Please.$s")]),
        ]),
        "addItem (O)blinxxy.MuseArt_OldCatalogue 1",
        "pause 300", "end",
    )

    # ---- 10 hearts: the second review (the shop, after hours) -------------
    shop[f"blinxxy.MuseArt_Julian10h/Friendship {J} 2500/DayOfMonth 22 23 24/Time 1800 2000/SawEvent blinxxy.MuseArt_Julian8h/SawEvent blinxxy.MuseArt_Sable8h/G PLAYER_HAS_MAIL Current blinxxy.MuseArt_CatalogueGiven"] = script(
        "continue", "11 10", f"farmer 9 13 0 Sable 14 12 0 {J} 9 15 0", "skippable",
        "pause 600", "move farmer 0 -2 0", "faceDirection farmer 1", "pause 400",
        "message " + q("Sable stands at an easel with a painting of their own: the river bend, the light flat on the water. Julian is in the doorway."),
        f"move {J} 0 -3 0", f"move {J} 3 0 1", "pause 600",          # (museart_route: clear of the shelves)
        speak("Sable", "You can say it. You've had ten years to work on it.$a"),
        "pause 600",
        "message " + q("Julian takes out his notebook, and reads."),
        speak(J, "'The river bend, at the hour the light lies flat on the water. Ten years ago I called this painter's work technically flawless and completely empty.'$7#$b#'I was wrong. This painting might not be technically flawless, but it is perfect in its own right.'$l"),
        "pause 1200", "faceDirection Sable 3", "pause 400",
        speak("Sable", "'Perfect in its own right.' You'd never have written that ten years ago.$u#$b#...Thank you, Julian.$l"),
        "pause 400",
        speak(J, "I'll be back next season. To review the next one.$h"),
        "blinxxy.MuseArt_SableDinner",
        "addItem (F)blinxxy.MuseArt_JulianReview 1",
        "pause 300", "end",
    )
    return back, museum, bus, saloon, shop


def main():
    pack = sys.argv[1]
    back, museum, bus, saloon, shop = events()
    strings = Strings(pack, ["julian-event.", "julian-after."])
    for scripts in (back, museum, bus, saloon, shop):
        for key, value in scripts.items():
            scripts[key] = strings.script("julian-event." + key.split("/")[0].split("_")[-1].lower(), value)
    changes = [
        # The back room is ours, so its event list starts empty.
        {"Action": "Load", "Target": "Data/Events/blinxxy.MuseArt_ArtShopBackRoom", "FromFile": "data/blank.json"},
        {"Action": "EditData", "Target": "Data/Events/blinxxy.MuseArt_ArtShopBackRoom", "Entries": back},
        {"Action": "EditData", "Target": "Data/Events/ArchaeologyHouse", "Entries": museum},
        {"Action": "EditData", "Target": "Data/Events/BusStop", "Entries": bus},
        {"Action": "EditData", "Target": "Data/Events/Saloon", "Entries": saloon},
        {"Action": "EditData", "Target": "Data/Events/blinxxy.MuseArt_ArtShop", "Entries": shop},
        # After his last event, Sable on his visit days.
        {"Action": "EditData", "Target": "Characters/Dialogue/Sable",
         "When": {"HasSeenEvent": "blinxxy.MuseArt_Julian10h"},
         "Entries": {f"{season}_{day}": strings.add("julian-after.sable", "Julian's in town. He's buying the coffee this time.$u")
                     for season in ("spring", "summer", "fall", "winter") for day in (22, 23, 24)}},
    ]
    header = ("// Julian Vane's heart events. Generated by tools/build_julian_events.py from\n"
              "// the approved draft in docs/julian_events.md: edit that script, not this file.\n")
    out = os.path.join(pack, "data", "julian_events.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write(header + json.dumps({"Changes": changes}, indent=2, ensure_ascii=False) + "\n")
    strings.save()
    print("wrote", out, "and", strings.path)


if __name__ == "__main__":
    main()

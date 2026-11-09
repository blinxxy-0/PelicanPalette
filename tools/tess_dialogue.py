"""Tess Brand's everyday words (1.4), approved by the user 2026-11-09 (docs/tess_writing_batch1.md).
Writes them into the dev pack: the i18n entries (tess.dialogue.*, tess.gift.*) and
data/tess_dialogue.json, which maps the game's dialogue keys to them.

Lines that know who she is wait for her events, not her hearts: the 6-heart lines (and Pine
Tar's second line) need her 6-heart reveal seen, the 8-heart lines her 8-heart event.

Usage: python tools/tess_dialogue.py
"""
import json, os, re

PACK = "[CP] Gilded Vale (dev)"
REVEAL, GRAVE = "blinxxy.MuseArt_TessHeart6", "blinxxy.MuseArt_TessHeart8"
WHEN = {"ValeResidents": "true", "HasFile:assets/tess/sprites.png": "true", "HasFile:assets/tess/portraits.png": "true"}

GIFTS = {
    "love": "...This is good. Really good. I'm going to stand here holding it for a second.$h",
    "like": "Thanks. That'll get used. Everything gets used.$0",
    "dislike": "It's not a present, it's a mess. ...Sorry. Thank you.$s",
    "hate": "No. Take it back outside. It doesn't even burn properly.$a",
    "neutral": "Oh. Thanks.$0",
}

# key -> line, always there
LINES = {
    "Introduction": "Mind the shavings. I'm not sweeping them. They're insulation.$0#$b#Tess. I carve. I've got the studio. ...That's all there is, really.$0",
    "Mon": "Mondays I sharpen everything. It's that or think.$0",
    "Tue": "Off to Robin's. She sells me the offcuts nobody wants. I want them.$0",
    "Wed": "You don't carve a shape into wood. You find out which shape it'll let you have.$0",
    "Thu": "Bea's put me on credit at the inn. I didn't ask her to. I'm paying it back in carved birds. She's got a lot of birds.$u",
    "Fri": "Somebody pencilled where every tool goes on that board. I keep putting them back. Feels rude not to.$0",
    "Sat": "People buy the rabbits. Nobody buys the owls. I keep carving owls.$u",
    "Sun": "Sunday. I don't carve on Sundays. I just think about carving, which is worse.$0",
    "Mon2": "Lupini walks past my door every morning and nods. Just nods. Is he always like that?$0",
    "Wed2": "Whoever was here before me knew what they were doing. Every tool's where your hand goes.$0",
    "Fri2": "Opal keeps leaving soup on my step. I keep eating it. We haven't discussed it.$u",
    "Sun2": "Tobin gave me a mug for a handle I carved him. He didn't ask why the handle. That's why I like him.$h",
    "Mon4": "I spent everything I had on this place. Everything. Don't tell Bea, she'll feed me out of pity.$s",
    "Tue4": "Robin's showing me joints. Mortise, tenon. I've been carving things that don't have to hold anything up. Turns out that's the easy part.$0",
    "Wed4": "Fern brought me another plant. It died. She brought another. I think it's a contest.$u",
    "Thu4": "Jet Corrie looked at my chisels the other day like he'd seen a ghost. Then he went and made tea.$0",
    "Sat4": "Cherry smells like burnt sugar when you cut it. Walnut smells like rain on a road. Ash doesn't smell of anything. I like ash best.$h",
    "Sun4": "I keep finding little things carved into this place. A face in a doorframe. A mouse on a beam. Somebody had fun here.$u",
    "winter_Mon": "Cold gets into the joints. The wood's, I mean. Mine too.$0",
    "winter_Thu": "The stove takes offcuts. I feed it the mistakes. It's never hungry.$u",
    "spring_Tue": "Spring. The sap's up. Everything I cut is wet and complaining.$0",
    "spring_Fri": "The Vale's first spring I've seen. Nobody told me about the blossom.$h",
    "summer_Wed": "Too hot to carve after noon. The wood sweats. So do I.$0",
    "summer_Sat": "I carved on the jetty this morning. Dropped a gouge in the lake. Don't tell anyone.$u",
    "fall_Mon": "Fall. Good wood comes down in the wind. I'm out every morning with a barrow.$h",
    "fall_Thu": "The Gilded Tree's dropping its leaves. I've been drawing them before they go.$0",
    "winter_1": "First snow. I've got a stove, a bedroll and a studio. That's more than last winter.$0",
    "spring_1": "Spring. I made it through a winter here. Nobody's more surprised than me.$u",
    "summer_1": "Summer. The studio smells like hot pine all day. I could live on it.$h",
    "fall_1": "Fall. When the gold comes off the trees, you can see what they're made of.$0",
    "fall_25": "It's my birthday. I don't usually say. ...I'm saying.$l",
    "AcceptGift_(O)709": "Hardwood. Smell that. Like a cellar with the sun on it.$h",
    "AcceptGift_(O)726": "Pine tar. Bram used to-- ...Somebody I knew used to swear by this. Thank you.$s",
    "AcceptGift_(O)236": "Soup. Hot soup. You've no idea. ...Fine, you've some idea. I'm going to eat it right now.$h",
    "AcceptBirthdayGift_Positive": "You remembered? I didn't tell anyone. ...Did I? Thank you.$l",
    "AcceptBirthdayGift_Negative": "It's the thought. I'm told it's the thought.$u",
    "AcceptBouquet": "Flowers. For me. ...Give me a minute, I'm not used to having a face that does this. Yes. Yes, all right.$l",
    "RejectBouquet_VeryLowHearts": "I don't know you well enough to take that. Sorry.$s",
    "RejectBouquet_LowHearts": "Not yet. Ask me again when I've stopped being suspicious of you.$u",
    "RejectBouquet": "...I can't. Not now. It's not you.$s",
    "RejectMermaidPendant": "That's a lot of question. Ask me something smaller first.$s",
    "RejectMermaidPendant_NeedHouseUpgrade": "Where would I put the workbench? Your house is a shoebox. A nice shoebox.$u",
    "dating_TessBrand": "Don't look at me like that in front of the counter. People are buying owls.$l",
}
# once she's told you who she is (her 6-heart event)
AFTER_REVEAL = {
    "Mon6": "Everyone keeps telling me stories about him. I like the ones where he was wrong.$u",
    "Wed6": "Lupini says my grandfather once carved a whole chair to settle an argument about chairs. I believe it.$h",
    "Fri6": "Emmett wants to write a song about Bram. I said no. ...I gave him one of the letters. I don't know why.$s",
    "Sun6": "I was afraid I was only any good because of his tools. Some days I still am. Less, though.$0",
    "AcceptGift_(O)726": "Pine tar. He swore by it. Now I can find out if he was right.$u",
}
# once she's been to his grave with you (her 8-heart event)
AFTER_GRAVE = {
    "Mon8": "I've started on furniture. Proper furniture, that holds weight. It's terrifying. Sit on this. No, carefully.$u",
    "Wed8": "I went to his grave. With you. I keep thinking I should feel finished. I don't. That's all right.$0",
    "Fri8": "His carving's nearly done. I'm saving the top for when I'm ready. He'd have said that's an excuse. He'd have been right.$u",
    "Sun8": "I used to come to the Vale for him. Now I think I'd stay if he'd never been here. Don't make a thing of it.$l",
}
RAINY = "Rain on the skylight. Best sound there is. I get more done in the rain than all week.$h"


def slug(key):
    return re.sub(r"[^A-Za-z0-9_]", "", key.replace("(O)", ""))


def main():
    i18n = {f"tess.gift.{k}": v for k, v in GIFTS.items()}
    def entries(lines, suffix=""):
        out = {}
        for k, v in lines.items():
            name = f"tess.dialogue.{slug(k)}{suffix}"
            i18n[name] = v
            out[k] = "{{i18n:" + name + "}}"
        return out
    always, reveal, grave = entries(LINES), entries(AFTER_REVEAL, "_reveal"), entries(AFTER_GRAVE)
    i18n["tess.dialogue.rainy"] = RAINY
    changes = [
        {"Action": "Load", "Target": "Characters/Dialogue/TessBrand", "FromFile": "data/blank.json", "When": WHEN},
        {"Action": "EditData", "Target": "Characters/Dialogue/TessBrand", "When": WHEN, "Entries": always},
        {"Action": "EditData", "Target": "Characters/Dialogue/TessBrand", "When": {**WHEN, "HasSeenEvent": REVEAL}, "Entries": reveal},
        {"Action": "EditData", "Target": "Characters/Dialogue/TessBrand", "When": {**WHEN, "HasSeenEvent": GRAVE}, "Entries": grave},
        {"Action": "EditData", "Target": "Characters/Dialogue/rainy", "When": WHEN, "Entries": {"TessBrand": "{{i18n:tess.dialogue.rainy}}"}},
    ]
    with open(os.path.join(PACK, "data", "tess_dialogue.json"), "w", encoding="utf-8") as f:
        f.write("// Tess's everyday words (docs/tess_writing_batch1.md, approved by the user 2026-11-09).\n"
                "// Written by tools/tess_dialogue.py: edit the lines there and run it again.\n"
                "// Portraits: $0 neutral, $h happy, $s sad, $u her dry half-smile, $l love, $a angry.\n"
                "// Lines that know who she is wait for her events: the 6-heart lines for her reveal\n"
                "// (blinxxy.MuseArt_TessHeart6), the 8-heart lines for her 8-heart event.\n")
        json.dump({"Changes": changes}, f, indent=2, ensure_ascii=False)
        f.write("\n")
    # the i18n entries, after "tess.name", replacing any earlier copies
    path = os.path.join(PACK, "i18n", "default.json")
    s = open(path, encoding="utf-8").read()
    s = re.sub(r'  "tess\.(dialogue|gift)\.[^"]*": "(?:[^"\\]|\\.)*",\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in i18n.items())
    anchor = '  "tess.name": "Tess",\n'
    assert anchor in s
    s = s.replace(anchor, anchor + block, 1)
    open(path, "w", encoding="utf-8").write(s)
    print("wrote", len(i18n), "lines")


if __name__ == "__main__":
    main()

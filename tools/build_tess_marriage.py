"""Writes Tess Brand's engagement, wedding and married life ([CP] Gilded Vale (dev)/data/tess_marriage.json)
from the approved draft in docs/tess_marriage.md (the user, 2026-11-09), as Margot's are built
(build_margot_marriage.py, build_margot_wedding.py):

  - her marriage fields: the kiss (frame 32, Emily's, facing right), the spouse room and the season's
    patio (tools/build_tess_spouse_areas.py), her sleeping frame (50)
  - the pendant's yes and the engaged line (Data/EngagementDialogue)
  - every "- Key: "line"" under "## Married lines" in the doc -> Characters/Dialogue/MarriageDialogueTessBrand
  - Tuesdays and Fridays at Studio III (marriage_Tue, marriage_Fri), behind the town-trips switch like
    everyone's buses (without it she stays home those days)
  - the wedding: the game's own, her apron off (Characters/TessBrand_Wedding), Lupini and Bea in the front
    row, Jet and Opal Corrie behind Bea once they're in the pack (Robin is among the game's own guests)

The dev pack's i18n has comments, so its lines are spliced in after "tess.name" (tess.marriage.*,
tess.engagement.*), as tools/tess_dialogue.py does.

Usage: build_tess_marriage.py <exports dir (Data_Weddings.json)>
"""
import json, os, re, sys

PACK = "[CP] Gilded Vale (dev)"
T = "TessBrand"
DOC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "docs", "tess_marriage.md")
WHEN = {"ValeResidents": "true", "HasFile:assets/tess/sprites.png": "true", "HasFile:assets/tess/portraits.png": "true"}
TRIPS = {**WHEN, "ValeResidentsTownTrips": "true"}
PENDANT = "...Is that what I think it is?$0#$b#Yes. Obviously yes. Give it here before I say something stupid.$l#$b#I'm making the ring box. Don't you dare buy one.$u"
ENGAGED = "I've measured your ring finger four times. In your sleep. Don't ask.$l"
AT_HER_WEDDING = f"PLAYER_NPC_RELATIONSHIP Current {T} Married"
ATTENDEES = {
    # the front row, at either end (the game's own guests leave 22 and 34 free)
    "Lupini": dict(Setup="Lupini 22 65 0", Celebration="faceDirection Lupini 1 true", Condition="PLAYER_HAS_MET Target Lupini"),
    "BeaThistle": dict(Setup="BeaThistle 34 65 0", Celebration="faceDirection BeaThistle 3 true", Condition="PLAYER_HAS_MET Target BeaThistle"),
    # behind Bea, once the twins are in the pack (meeting them needs them to exist)
    "JetCorrie": dict(Setup="JetCorrie 33 66 0", Celebration="faceDirection JetCorrie 3 true", Condition="PLAYER_HAS_MET Target JetCorrie"),
    "OpalCorrie": dict(Setup="OpalCorrie 33 67 0", Celebration="emote OpalCorrie 20 true", Condition="PLAYER_HAS_MET Target OpalCorrie"),
}


def married_lines():
    text = open(DOC, encoding="utf-8").read()
    married = text.split("## Married lines", 1)[1].split("## What building this needs", 1)[0]
    lines = re.findall(r'^- (\w+): "(.*)"\s*$', married, flags=re.M)
    if len(lines) < 50:
        sys.exit(f"only {len(lines)} lines found in {DOC}; has its layout changed?")
    return lines


def wedding(exports):
    script = json.load(open(os.path.join(exports, "Data_Weddings.json"), encoding="utf-8"))["EventScript"]["default"]
    start = "changeLocation Town/"
    if script.count(start) != 1 or script.count("/end wedding") != 1:
        sys.exit("couldn't find where the ceremony starts and ends; has the game's wedding changed?")
    # the apron off for the ceremony, back on as it ends (her portrait stays her own)
    script = script.replace(start, start + "changeSprite spouse Wedding/")
    script = script.replace("/end wedding", "/changeSprite spouse/end wedding")
    attendees = {f"blinxxy.MuseArt_TessWedding_{npc}": {
        "Id": npc, "Condition": AT_HER_WEDDING + ", " + a["Condition"], "Setup": a["Setup"], "Celebration": a["Celebration"]}
        for npc, a in ATTENDEES.items()}
    return script, attendees


def main():
    exports = sys.argv[1]
    i18n = {"tess.engagement.0": PENDANT, "tess.engagement.1": ENGAGED}
    married = {}
    for key, line in married_lines():
        i18n[f"tess.marriage.{key}"] = line
        married[key] = "{{i18n:tess.marriage." + key + "}}"
    script, attendees = wedding(exports)
    studio_day = ("700 blinxxy.MuseArt_ValeStudio3 7 5 0/1100 blinxxy.MuseArt_ValeStudio3 12 8 2/"
                  "1700 BusStop -1 23 3")
    changes = [
        {"Action": "EditData", "Target": "Data/Characters", "When": WHEN, "TargetField": [T], "Entries": {
            "KissSpriteIndex": 32, "KissSpriteFacingRight": True,
            "SpouseRoom": {"MapAsset": "blinxxy.MuseArt_TessSpouseRoom", "MapSourceRect": {"X": 0, "Y": 0, "Width": 6, "Height": 9}},
            "SpousePatio": {"MapAsset": "blinxxy.MuseArt_TessSpousePatio", "MapSourceRect": {"X": 0, "Y": 0, "Width": 4, "Height": 4},
                            "SpriteAnimationFrames": [[8, 2000], [8, 2000]]},
        }},
        {"Action": "Load", "Target": "Maps/blinxxy.MuseArt_TessSpouseRoom", "FromFile": "assets/TessSpouseRoom.tmx", "When": WHEN},
        {"Action": "Load", "Target": "Maps/blinxxy.MuseArt_TessSpousePatio", "FromFile": "assets/TessSpousePatio_{{season}}.tmx", "When": WHEN},
        {"Action": "Load", "Target": "Characters/TessBrand_Wedding", "FromFile": "assets/tess/sprites_wedding.png", "When": WHEN},
        {"Action": "EditData", "Target": "Data/AnimationDescriptions", "When": WHEN, "Entries": {"tessbrand_sleep": "50/50/50"}},
        {"Action": "EditData", "Target": "Data/EngagementDialogue", "When": WHEN, "Entries": {
            f"{T}0": "{{i18n:tess.engagement.0}}", f"{T}1": "{{i18n:tess.engagement.1}}"}},
        {"Action": "Load", "Target": f"Characters/Dialogue/MarriageDialogue{T}", "FromFile": "data/blank.json", "When": WHEN},
        {"Action": "EditData", "Target": f"Characters/Dialogue/MarriageDialogue{T}", "When": WHEN, "Entries": married},
        {"LogName": "Tess, married: Tuesdays and Fridays at Studio III", "Action": "EditData",
         "Target": f"Characters/schedules/{T}", "When": TRIPS,
         "Entries": {"marriage_Tue": studio_day, "marriage_Fri": studio_day}},
        {"Action": "EditData", "Target": "Data/Weddings", "When": WHEN, "TargetField": ["EventScript"], "Entries": {T: script}},
        {"Action": "EditData", "Target": "Data/Weddings", "When": WHEN, "TargetField": ["Attendees"], "Entries": attendees},
    ]
    out = os.path.join(PACK, "data", "tess_marriage.json")
    with open(out, "w", encoding="utf-8") as f:
        f.write("// Tess's engagement, wedding and married life. Generated by tools/build_tess_marriage.py from\n"
                "// docs/tess_marriage.md (approved by the user 2026-11-09): edit the doc, not this file.\n")
        json.dump({"Changes": changes}, f, indent=2, ensure_ascii=False)
        f.write("\n")
    path = os.path.join(PACK, "i18n", "default.json")
    s = open(path, encoding="utf-8").read()
    s = re.sub(r'  "tess\.(marriage|engagement)\.[^"]*": "(?:[^"\\]|\\.)*",\n', "", s)
    block = "".join(f"  {json.dumps(k)}: {json.dumps(v, ensure_ascii=False)},\n" for k, v in i18n.items())
    anchor = '  "tess.name": "Tess",\n'
    assert anchor in s
    open(path, "w", encoding="utf-8").write(s.replace(anchor, anchor + block, 1))
    print("wrote", out, f"({len(married)} married lines)")


if __name__ == "__main__":
    main()

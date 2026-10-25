# Phase 6: Sable's heart events and marriage (DRAFT, for approval)

This builds on the approved brief (`docs/npc_brief.md`). The arc in one line:
Sable rediscovers the painter "E.", then the painting, then themselves, and
it ends with them painting an original again: a portrait of you.

Portrait codes in the lines: `$h` happy, `$s` sad, `$u` wry, `$l` blushing,
`$a` annoyed, `$6` surprised, `$7` wink.

---

## 2 hearts: "Don't laugh" (the shop, evening)

**Trigger:** you walk into the shop between 6pm and 8pm with 2+ hearts,
on any day except Wednesday. Sable is at the easels after closing.

**Scene:**
- Sable sits at an easel with a damaged landscape propped on it: water stains
  and a torn corner. The painting is cracked down the middle. They're working
  with a tiny brush, close enough to touch it with their nose.
- They don't hear you come in. When you get close: `$6` "Oh. It's after
  hours. ...You can stay. Don't touch anything."
- "It's a landscape. Cindersap Forest, about forty years ago. The museum sent
  it to me to restore. I was meant to send it back."
- "Look at the light on the water. Whoever painted this stood right where my
  shop is now. Same river, same bend." `$s`
- "It's signed 'E.' Just the initial. No records. Nobody knows who painted it."
- "I kept it. Then I quit my job and moved here. It's why I'm here.#$b#Don't
  laugh." `$u`
- **Choice:**
  - "That's not funny. That's brave." → +friendship, `$l` "...Brave is a
    strong word. Stubborn, maybe."
  - "So you stole it?" → no change, `$a` "Borrowed. Indefinitely. ...With
    paperwork. Mostly."
  - "Who do you think E. was?" → +friendship, `$h` "Somebody who loved this
    valley. You can see it in how they did the water."
- They turn back to the painting. "Go home, farmer. The light's gone."

**After:** everyday dialogue starts mentioning the painting ("The crack's
nearly closed").

---

## 4 hearts: side by side (the riverbank, sunny morning)

**Trigger:** you enter the Forest between 8 and 10am on a sunny Monday,
Tuesday, Thursday or Saturday with 4+ hearts and the 2-heart event seen.
Sable is at their riverside spot.

**Scene:**
- Sable has a second stool and a small easel set up beside theirs. "You're
  late. I set up a canvas for you an hour ago." `$u`
- You paint side by side; the screen fades for a moment.
- They look at yours. "Your water is all one blue. Water's never one blue."
- **The mixing trick:** "When you mix, don't chase the color. Let it come to
  you. Hold steady, and only move when it moves." (In-game hint for the
  mixing bar.)
- They give you their old **Sable Round brush**: "It's the first good brush I
  ever owned. It's been in a drawer for ten years. It should be painting."
- You glance at their canvas: it's a careful pencil sketch, no paint on it.
  "Just a study." `$u`
- **Choice:**
  - "You never finish them, do you?" → `$s` "...No. Not anymore."
    (+friendship)
  - "It's beautiful already." → `$l` "It's a sketch. But... thank you."
    (+friendship)

**Reward:** a Sable Round brush (the item from the shop).

---

## 6 hearts: the letter (the shop, evening)

**Trigger:** you enter the shop between 6 and 8pm with 6+ hearts and the
4-heart event seen.

**Scene:**
- Sable stands at the counter holding a letter. The shop is half dark.
- "The Zuzu City Museum wants me back. Head of restoration. Double the pay,
  my old studio, a window." `$s`
- "I told myself I came here for the painting. But I've been restoring it
  for a year. I could finish it tomorrow."
- "I haven't painted anything of my own in ten years. A critic said my
  work was 'technically flawless and completely empty'. I believed them. It
  was easier to fix other people's work. You can't fail at someone else's
  painting."
- **Choice** (this shapes their later dialogue):
  - "Stay." → `$l` "...You'd say that to anyone who sells you canvases."
    beat. "No. You wouldn't." (Remembered as *stay*.)
  - "It's your call. I'll miss you either way." → `$s` "That's the worst
    possible answer. It's also the kind one." (Remembered as *your call*.)
  - "Paint something first. Then decide." → `$6` "Something of mine?" long
    pause. "...Fine. One thing. Then I'll decide." (Remembered as *paint
    first*.)
- They tear the letter in half the next day; there's a line about it either
  way.

**After:** dialogue varies by your answer; the one you chose comes back at
10 hearts.

---

## 8 hearts: "E." (Evelyn's house, Wednesday afternoon)

**Trigger:** you enter Evelyn and George's house (JoshHouse) on a Wednesday
between 2:30 and 5pm with 8+ hearts and the 6-heart event seen. That's when
Sable visits for tea.

**Scene:**
- Sable sits at the kitchen table with Evelyn. The restored landscape is
  wrapped in brown paper between them. George is in the next room.
- "I wanted you to see this, Evelyn. You know the forest better than
  anyone." Sable unwraps it.
- Evelyn goes very still. `(Evelyn)` "Oh my. Oh, my goodness."
- `(Evelyn)` "I painted this. The summer before I married George. I sat on
  the riverbank every morning for a month."
- Sable, `$6`: "You're E."
- `(Evelyn)` "Evelyn. I always signed with just the E. I thought it looked
  very mysterious." She laughs. "I gave it to a traveling dealer for a song.
  We needed the money for the farm."
- `(Evelyn)` "I stopped painting after that. There was always something more
  useful to do. You know how it is."
- George, from the next room: `(George)` "She was good, too. Better than
  good."
- `(Evelyn)` "Oh, hush." beat. "...Sable, dear. Would you paint with me one
  morning? I'm slow now. My hands shake."
- Sable, `$l`: "I'd like that more than anything."
- They talk shop: Evelyn's old brushes, the river bend. The scene fades on
  the two of them bent over the canvas.

**After:** on Wednesdays Evelyn and Sable paint by the river together for a
while. Evelyn gets a few new lines.

---

## 10 hearts: the portrait (the shop, after hours; you must be dating)

**Trigger:** you enter the shop between 6 and 8pm, dating Sable (you've
given the bouquet), with the 8-heart event seen.

**Scene:**
- The shop is lit by candles. One easel stands in the middle of the floor
  under a sheet.
- "I did what you said." (This line changes with your 6-heart answer:
  *paint first* gets "I painted something first."; *stay* gets "I stayed. And
  then I painted something."; *your call* gets "I made my call.")
- "It's the first original I've finished in ten years. Don't say anything
  until you've looked at it properly."
- They pull the sheet off. It's **a portrait of you**: your farmer as they
  look right now, painted in Sable's style.
- `$l` "Technically flawless. Completely full." beat. "...That's what the
  critic should have said about this one."
- **Choice:**
  - "It's the best thing I've ever seen." → `$l` "You haven't seen many
    paintings. ...I'll take it."
  - (Hug them.) → they freeze, then hug back. "Careful. It's still wet."
    (callback to the first line they ever said to you)
- "It's yours. Hang it somewhere I can see it when I visit."

**Reward:** the portrait, a framed painting you can hang. It's made in game
from your farmer's current look (hair, skin, clothes) and turned into a
painting with Sable's signature, using the same system as your own
paintings. Nobody else's copy looks the same.

---

## 14 hearts: the exhibition (married; the art shop, evening)

**Trigger:** you're married, with 14 hearts, and you walk into the shop
between 6 and 8pm on a sunny day.

**Scene:**
- The shop has been turned into a little gallery: Sable's originals hang on
  every wall. Evelyn's landscape hangs in the middle; your portrait is beside
  it. Townspeople mill about: Evelyn and George, Leah, Elliott, Emily,
  Haley, Robin and Lewis.
- Short lines from each: Leah grudgingly admits "this one has *sculptural*
  color"; Elliott narrates the evening; Haley asks Sable to teach her
  painting; Evelyn stands by her landscape with George's hand in hers.
- Sable finds you in the corner. `$l` "Ten years of nothing. Then one
  farmer walks in with half a field on their boots." (Changed
  2026-10-25 from "a lot of brown on them", which could read as being
  about the farmer's skin.)
- "Thank you for ruining my quiet, empty life." `$h`

**I'm suggesting the shop, not the farmhouse** as the brief said: every
farmhouse layout is different, so a crowd scene there tends to put people
inside furniture. The shop is a room we control.

---

## Bouquet, pendant and marriage

**Bouquet** (8+ hearts): `$l` "Is that... for me? I don't have anything
clever to say. That's never happened before."

**Mermaid's pendant** (10 hearts, dating): `$6` "...Yes. Obviously yes.
Give me a second, I'm mixing a lot of feelings at once." (Sable can marry a
farmer of any gender.)

**Wedding:** the 1.6 built-in wedding. This needs one new sprite: a **kiss
frame**.

**Married life:**
- **They keep the shop.** They leave the farm at 9 and walk to the forest,
  run the counter 10 to 5, and come home in the evening. Wednesdays they stay
  on the farm and paint at the patio easel.
- **Spouse room** (6x9, built from vanilla tiles): a studio corner with an
  easel and a half-finished canvas, a paint-splattered rug, stacked frames,
  shelves of jars and a window.
- **Spouse patio:** an outdoor easel facing your crops; Sable paints there
  (animated).
- **Marriage dialogue:** about 50 lines: mornings, rainy days, seasons, your
  cooking, the farm, sometimes a gift of a pigment jar or a canvas left on the
  table.
- **New animations:** painting at an easel (2 to 4 frames from the side),
  the kiss frame, and a sleeping frame for the spouse bed. They're drawn the
  same way as the dance frames: from Sable's own sprites, checked against
  vanilla.

---

## Build order once approved
1. The 2, 4, 6 and 8-heart events, each tested in my dev copy with the
   dialogue clicked through.
2. Bouquet and pendant lines, the kiss frame, then the wedding.
3. The 10-heart portrait (the farmer-to-painting renderer in C#).
4. Spouse room, patio, painting animation, marriage dialogue and schedule.
5. The 14-heart exhibition.

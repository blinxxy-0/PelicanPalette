# Opal and Jet Corrie's heart events (approved by the user 2026-11-09; built)

From the brief's outlines (`docs/corrie_twins_brief.md`, "Heart events"). The 2- and 4-heart events
and the 6- to 10-heart ones (further down, after Jet's 4) were all approved 2026-11-09, and built the
same day by `tools/build_corrie_events.py` (`data/corrie_events.json`). Each played through in the game.

Each twin has their own hearts and their own events, and both twins turn up in most of them: whose
hearts trigger it decides whose story it is. Opal is loud and long, Jet says one sentence where she
says ten. Nobody names Studio I's painter.

Portraits as their dialogue: `$0` neutral, `$h` happy, `$s` sad, `$u` unique (Opal laughing out loud;
Jet one brow up, dry), `$l` touched, `$a` angry.

**When they can happen:** where that twin is on their schedule (`data/opal_schedule.json`,
`data/jet_schedule.json`), so nobody's moved. Each 4-heart event needs the 2-heart one before it.
Event IDs: `blinxxy.MuseArt_Opal2h`, `blinxxy.MuseArt_Jet2h` and so on. From Spring 1, Year 3, like
the twins themselves.

---

## Opal

### 2 hearts: the city lamps (Studio II, a morning)

**Trigger:** you enter Studio II between 8am and 1pm (she's at the counter), with 2+ hearts with Opal.

**Scene:**
- A packing crate in the middle of Opal's half, straw everywhere. Opal is on her knees in it, lifting
  out lamps: chrome, brass, a glass globe the size of a melon. Jet is at his bench, not looking.
- `$u` "The last of the flat in Zuzu City. Jet said leave them. I said, Jet, they're Italian."
- She stands up with the biggest one: a chandelier dripping crystal. `$h` "You! You have a house. Take
  one. Take this one."
- **Choice:**
  - "Where would I even put it?" → She looks at the chandelier, at you, at the chandelier, and laughs
    until she has to sit down on the crate. `$u` "Over the cows! They'd look wonderful."
  - "It's a bit much for a farmhouse." → `$s` "It is, isn't it. Everything we owned was a bit much.
    That was rather the point." Then she laughs at herself. `$u`
  - "I'll take it!" → `$h` "Darling!" Jet, without looking up: "It won't go through a farmhouse door."
    Opal, slowly: "...It didn't go through ours either. They brought it in through the window." `$u`
- She puts the chandelier back in the straw and digs out the smallest thing in the crate.
  `$l` "Here. The little one. It's the only one that was ever any use."

**After:** you get a Candle Lamp (the game's own, `(F)1751`). Her city lamps are still up; at 6 hearts
their own glass lamps replace them (the studio's heart stage), and the city ones go up to the loft.

### 4 hearts: a letter (Studio II, a morning)

**Trigger:** you enter Studio II between 8am and 1pm, with 4+ hearts with Opal and her 2-heart event seen.

**Scene:**
- Opal is behind the counter with a letter in one hand and a glossy catalogue in the other. Jet is
  cutting. She reads aloud, in a voice:
  `$a` "'Corrie Classic. The authentic stained-glass look. Self-adhesive.' Self-adhesive!"
- `$a` "The firm that bought us. They've put our name on a sticker. Mabel's name. On a sticker you put
  on a bathroom window."
- She paces the length of the counter and back.
- **Choice:**
  - "You could sue them." → `$u` "With what? We sold them the name. Signed it away with a pen that cost
    more than my first kiln."
  - "Does it matter what they call it?" → `$a` "It's our mother's name!" A pause. `$s` "...No. You're
    right. It doesn't. That's the awful part."
  - "Make something better." → `$l` "That's what Jet said. In fewer words."
- From the bench, without looking up, Jet: "Opal."
- She stops. She looks at the catalogue for a long moment. `$s` "It's not ours any more. That was the
  point." She drops it in the bin.
- `$h` "Right. Tea. You'll have tea."

**After:** her Tue4 line (the company) stands on this. At 8 hearts her first panel by hand cracks in
the kiln.

---

## Jet

### 2 hearts: the seedhouse (the Vale, at dawn)

**Trigger:** you're near Fern's seedhouse on the Vale map between 6am and 8am (his dawn walk), not on
a rainy day, with 2+ hearts with Jet.

**Scene:**
- Mist on the gardens. Jet is at the side of Fern's seedhouse with one pane out of its frame, a putty
  knife in his hand. He doesn't turn round. `$0` "Hm. Early."
- He fits a new pane, runs his thumb along the putty, and steps back to look. A long quiet.
- **Choice:**
  - "Does Fern know you do this?" → `$u` "She thinks the wind's very careful."
  - "Can I help?" → `$0` "Hold this. Don't press." You hold the pane while he puttys it in. `$h` "Good."
  - (Watch him work.) → He works. After a while: `$l` "Old glass. Somebody cared about this house once."
- He takes an offcut of blue glass from his apron and holds it out to you. One sentence:
  `$l` "Keep it in a window."
- He picks up his tools and walks off down the path. The seedhouse door opens behind you and Fern
  looks out, yawning, at a pane that's whole. By then he's gone.

**After:** you get the Blue Offcut (furniture, a small keepsake to stand in a window). His Mon2 line ("for the air") stands on this.

### 4 hearts: Mabel's bench (Studio II, an afternoon)

**Trigger:** you enter Studio II between 1pm and 5pm (Opal's out; he's at the light table), with 4+
hearts with Jet and his 2-heart event seen.

**Scene:**
- Jet is at Mabel's bench with a cloth, wiping it down, though it's clean. Her pliers are laid out as
  they always are.
- `$0` "Her bench." A long pause. "I learned under it. Picking up offcuts. Then on her knee. Then beside her."
- `$l` "She'd put my hand on the cutter and press. 'Like you mean to break it, and you won't.'"
- **Choice:**
  - "Sit down. Try it." → He puts a hand on the stool, then takes it away. `$s` "Not yet."
  - "She sounds like a good teacher." → `$l` "She was. Opal got the talking. I got the hands."
  - (Say nothing.) → After a while: `$h` "You're the only one who doesn't ask."
- He folds the cloth, squares her pliers by a fraction, and goes back to the light table.

**After:** his Mon4 line (learning at the bench) stands on this. He sits at it for the first time at
10 hearts, to finish her design.

---

# 6 to 10 hearts

Two of these depend on the Exhibition Hall, which reopens at Lupini's 8 hearts (the mail flag
`blinxxy.MuseArt_ValeHallOpen`). Each has a version for a Hall that's still shut, so Opal's story can
be finished without Lupini (`docs/design/vale_newcomers.md`, section 3).

## Opal

### 6 hearts: the Hall's windows (the plaza, an afternoon)

**Trigger:** you're on the plaza between 1pm and 3:30pm (she's out looking up at the Hall), on a fine
day, with 6+ hearts with Opal and her 4-heart event seen.

**Scene (the Hall still shut):**
- Opal is standing in front of the Exhibition Hall with a sketchbook held up against the light,
  squinting from it to the dusty windows and back. `$h` "There you are! Stand here. No, here. Now
  look up."
- `$0` "Those windows went in the year we were born. Plain glass, cheap, because the Vale couldn't
  afford anything else. Mabel said she'd do them properly one day. She never got round to it."
- She turns the sketchbook round: the Hall's windows redrawn, each one a season of the Vale, in
  coloured pencil. `$l` "Spring, summer, fall, winter. Four windows. I've been drawing them for
  forty years. On napkins, in meetings. On a contract once."
- **Choice:**
  - "They're beautiful." → `$h` "They are, aren't they! I'm not even going to pretend to be modest."
  - "Who would you have to ask?" → `$u` "Lupini, I suppose. He'd say yes and then forget. And somebody
    would have to open the place first."
  - "Make them anyway." → `$l` "...For a building nobody goes in. Yes. Why not."
- She closes the sketchbook. `$s` "Twenty years ago I'd have had a team of forty on this by Friday.
  Now it's me, and Jet, and our hands. If they still work."

**Scene (the Hall open):** the same, but inside the gallery with Lupini, on one of their Hall days.
Opal pitches the four seasons to him across the reception desk; he pretends to haggle, then says yes
before she's finished. `$u` Opal: "He had one coat and no money when we met him. Now look at him,
running a gallery." Lupini, to you: "She hasn't changed. Not one bit. Thank goodness."

**After:** the studio's 6-heart stage: their own glass lamps replace the city ones (she gives the
city lamps to the loft, "where they can be expensive in private").

### 8 hearts: her first panel (Studio II, an evening)

**Trigger:** you enter Studio II between 7pm and 10pm (she's at her drawing board), with 8+ hearts
with Opal and her 6-heart event seen.

**Scene:**
- The kiln is open, still ticking as it cools. Opal is standing in front of it holding a small panel
  in both hands: a sunflower, every piece cut and leaded by her, and a crack straight across the
  middle.
- `$s` "My first one. All mine, start to finish. The first in twenty years. And it cracked in the kiln."
- `$s` "Do you know what I'm frightened of? That my hands have forgotten. That we didn't come home to
  make things. We came home because they sold us, and we had nowhere else to go, and I'm just very
  good at talking about it."
- **Choice:**
  - "One crack doesn't mean your hands forgot." → `$l` "No. It means I rushed the cooling. I know that.
    I knew it this morning."
  - "Show me how you made it." → She does: the cartoon, the cutting, the lead. Halfway through she
    stops. `$l` "...I do remember. Don't I."
  - "You came home because you wanted to." → `$s` "Did I? I'd like to believe that."
- In the doorway behind her is Jet, back from somewhere, holding his coat. He heard. He doesn't say
  anything. He crosses to the kiln, takes the cracked panel from her gently, and holds it up to the
  lamp. The crack throws a thin bright line across the floor.
- Jet: `$l` "Mabel's first one cracked too. She kept it."
- Opal laughs, crying a bit. `$u` "She did! It's in the loft somewhere. Oh, Jet."

**After:** her Mon8 line (her hands remembering) stands on this. The cracked sunflower hangs in her
half of the studio from now on, over her drawing board (the studio's stage "opal8").

### 10 hearts: morning light (the Exhibition Hall, at sunrise)

**Trigger:** you get a letter from Opal ("Come to the Hall at six. Don't be late, the sun won't
wait."), then go to the Exhibition Hall between 6am and 8am, with 10 hearts with Opal, her 8-heart
event seen and the Hall reopened.

**Scene (the Hall open):**
- The gallery is empty and dim. Opal and Jet are standing in the middle of the floor, not talking,
  which you've never seen.
- The sun comes up. Through the first of the new windows (behind us, above the door), colour comes
  across the floor: a pool of spring green and gold that slides slowly over the tiles as the sun
  climbs.
- Opal, very quietly for her: `$l` "Spring. The first of the four."
- They watch it for a long time. Jet: `$l` "It's good, Opal."
- `$h` "It's ours." She takes your arm on one side and Jet's on the other. "All three of ours. You held
  the ladder."
- **Choice:**
  - "It's the most beautiful thing in the Vale." → `$u` "Don't tell Lupini." 
  - "Mabel would be proud." → `$s` then `$l` "...She'd say the lead's too thick. And then she'd cry."
  - (Watch the light.) → Nobody says anything until it's reached the far wall.

**Scene (the Hall still shut):** the same, at Bea's: the Vale House tea room at dawn, a new window in
the tea room's wall, the colour across the tea tables. Bea brings three cups of tea and a fourth for
herself, and sits down, which she never does. Bea: "My aunt always wanted a window like that." Opal:
`$l` "I know. She told me. I was nine."

**After:** "On the way out, Opal presses a small parcel into your hands: a window of your own.
Spring, the first of the four." You get the Spring Window (furniture). The Hall's windows (or the tea room's) stay coloured for good.

## Jet

### 6 hearts: the loft (Studio II, an afternoon)

**Trigger:** you enter Studio II between 1pm and 5pm, with 6+ hearts with Jet and his 4-heart event seen.

**Scene:**
- Jet is coming down the ladder from the loft with a box on his shoulder, coughing in the dust. More
  boxes are stacked at its foot. `$u` "Opal's lamps need the room."
- He hands you a box. `$0` "Sort. Keep, or bin." It's full of things the twins left behind twenty
  years ago: a cracked mug, their school reports, a pair of tiny safety glasses.
- At the bottom: a book with a cloth cover, its corners worn round. Jet goes still.
- `$l` "Her pattern book."
- He turns the pages: windows, years of them, each drawn and coloured and signed M.C. The last page
  is a window of the Vale at dawn, the sun coming up over gold trees, the river running down the
  middle. It's half coloured in. The other half is pencil.
- `$s` "She never finished it. She was ill by then. We left it in the loft when we boarded up."
- **Choice:**
  - "You could finish it." → He looks at you a long time. `$s` "...Not yet."
  - "Will you show Opal?" → `$u` "She'll cry. Then she'll want to change it."
  - (Say nothing.) → He closes the book very carefully.
- He puts the book on Mabel's bench, square to its edge, and goes back up the ladder.

**After:** the pattern book lies on Mabel's bench (its right-click text changes to say so). His 10
hearts is this window.

### 8 hearts: the line (Studio II, an evening)

**Trigger:** you enter Studio II between 7pm and 9pm, with 8+ hearts with Jet and his 6-heart event seen.

**Scene:**
- Raised voices before the door's even shut. Opal on her side of the line, Jet on his, for real this
  time: a crate of her sketches has slid over the line into his offcuts.
- `$a` Opal: "It's three inches, Jet! Three inches of floor!"
- `$a` Jet: "It's my floor."
- `$a` Opal: "It's Mabel's floor! You drew that line in the first week of the company and you've been
  standing behind it ever since!"
- Silence. Jet looks at you.
- **Choice** (how to share the middle; each ends the same way):
  - "Work at Mabel's bench. Both of you." → Opal: `$s` "We can't. It's hers." Jet: "It's in the middle."
  - "Rub out a bit of the line." → Opal laughs despite herself. `$u` "With what? It's paint. Jet
    painted it. Of course he did."
  - "Why did you draw the line?" → Jet: `$s` "So I'd have somewhere that was mine."
- Jet gets a scraper from his bench and, without a word, scrapes a gap in the line by Mabel's bench,
  three tiles wide. Opal watches him.
- Jet, to the floor: `$s` "I never wanted to leave."
- Opal: `$s` "...I know. I've always known. I went anyway, and you came, and I never once asked you."
- Jet: `$l` "You didn't have to ask."
- Opal steps through the gap into the middle. After a moment, so does he.

**After:** the studio's 8-heart stage: the line has a gap by the bench. His Wed8 line (the offcut)
and her Wed8 line (his looks) stand on this.

### 10 hearts: the window (Studio II, a morning)

**Trigger:** you get a letter from Jet (four words: "Come tomorrow. Early. -J"), then enter Studio II
between 6am and 9am, with 10 hearts with Jet and his 8-heart event seen.

**Scene:**
- Jet is sitting at Mabel's bench. You've never seen anyone sit there. Her pattern book is open at the
  last page; beside it, the window, finished: the Vale at dawn, the sun over the gold trees, the river.
- `$l` "Finished it last night. Her lines. My cuts."
- Opal comes in in her dressing gown, sees him at the bench, and stops in the doorway. She doesn't say
  anything at all, which is how you know.
- Jet stands, lifts the panel, and the three of you carry it to the big window. He fits it into the
  frame himself.
- The sun comes through it. The Vale at dawn lands on the floor across the middle of the studio, right
  where the line used to be.
- Opal: `$l` "Oh, Mum."
- Jet, to you: `$h` "Thank you for holding the ladder."
- **Choice:**
  - "She'd be proud of you both." → Opal: `$s` "She'd say we took our time." Jet: `$u` "We did."
  - "Now you can make your own." → Jet: `$h` "Already started." He shows you a drawing: a window for
    your farmhouse.
  - (Look at the light.) → Nobody says anything for a long time.

**After:** the studio's 10-heart stage: Mabel's window in the studio's window, her bench in use (they
take turns at it). "Before you go, Jet hands you a small window wrapped in newspaper: the one he drew
for your farmhouse." You get the Farmhouse Window (furniture).

---

## How they're built

- `tools/build_corrie_events.py` writes `data/corrie_events.json`: the scripts (text in i18n as
  `corrie-event.*`), Opal's and Jet's 10-heart letters (trigger actions), and the bench's text once
  Jet's 6 has put the pattern book on it.
- The studio's heart stages follow the events: Opal's 6, Jet's 8 and 10 set mail flags
  (`blinxxy.MuseArt_Opal6Seen`, `_Jet8Seen`, `_Jet10Seen`) that `data/vale_studio2.json` waits for;
  Jet's 8 and 10 also change the room on screen (`removeTile`, `changeMapTile`).
- Props: the game's own Modern and Ornate Lamps in Opal's 2; the coloured light of the 10s.
- Rewards (Higgsfield art, 2026-11-09; tools/corrie_furniture.py): Jet's Blue Offcut (2), Opal's
  Spring Window (10), Jet's Farmhouse Window (10), all furniture; Opal's cracked sunflower hangs in
  Studio II after her 8. Opal's 2 gives the game's own Candle Lamp.
- No animations yet (kneeling, cutting, puttying): standing poses, the props doing the work.

---


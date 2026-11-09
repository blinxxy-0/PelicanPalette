# Opal and Jet Corrie's heart events (2 and 4 hearts approved by the user 2026-11-09; 6 to 10 a DRAFT)

From the brief's outlines (`docs/corrie_twins_brief.md`, "Heart events"). The 2- and 4-heart events
were approved 2026-11-09; the 6- to 10-heart ones are further down, after Jet's 4. Nothing here is
built yet.

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

**After:** you get the blue offcut (a small keepsake to stand in a window: furniture art, to make with
the rest after the PixelLab refill). His Mon2 line ("for the air") stands on this.

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

## What building these needs

- **Event scripts** in a `data/corrie_events.json` (or `tools/build_corrie_events.py`, as Sable's are
  built), with `Data/Events/blinxxy.MuseArt_ValeStudio2` and `Data/Events/blinxxy.MuseArt_GildedVale`.
- **Props:** a packing crate full of straw and lamps, the chandelier, a catalogue, a pane of glass:
  temporary sprites (`addTemporaryActor` / `temporarySprite`) drawn small, or the game's own crate.
- **Rewards:** the Candle Lamp is the game's own; Jet's blue offcut is a new furniture item that
  waits for its art.
- **Animations:** none yet (kneeling, cutting, puttying): the walk sheets have only walking frames,
  so these play as standing poses with the props doing the work, like the other residents' drafts.

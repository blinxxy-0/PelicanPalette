# The Vale residents' first heart events (DRAFT, for approval)

The 2- and 4-heart events for Bea, Lupini, Fern and Emmett (1.4), from their briefs (`docs/bea_brief.md`, `docs/lupini_brief.md`, `docs/fern_brief.md`, `docs/emmett_brief.md`). Tobin's events are 1.5's, through Pottery. Nothing here is built: the events need the residents' sprite sheets, and the scripts need your approval first.

Portrait codes are the ones their dialogue already uses: `$0` neutral, `$h` happy, `$s` sad, `$u` each one's own face (Bea's dry half-smile, Lupini's theatrical look, Fern's gap-toothed grin, Emmett's crooked grin), `$l` touched or caught out, `$a` cross. Bea's sheet also has `$6` surprised.

**When they can happen:** in the Vale, where that resident is at that time on their schedule (`[CP] Gilded Vale (dev)/data/<name>_schedule.json`), so nobody has to be moved for them. Each 4-heart event needs the 2-heart one before it. Event IDs follow the mod's: `blinxxy.MuseArt_Bea2h`, `blinxxy.MuseArt_Bea4h` and so on. A choice that's remembered is a mail flag, as Julian's and Margot's are.

---

## Bea Thistle

### 2 hearts: food for twelve (the tea room, an afternoon)

**Trigger:** you enter the Vale House between noon and 4pm, not on a Sunday (the tea room shuts at noon) or a rainy day, with 2+ hearts.

**Scene:**
- Every tea table is laid: cups, plates, a cake stand on each, twelve places. Nobody's sitting at any of them. Bea is at the end of her counter with a tea towel over her shoulder, looking at the door.
- `$0` "A coach party from the city. Twelve of them, booked a month ago. They telephoned an hour ago. They've gone to the seaside instead."
- She looks at the cake stands. `$u` "So. Sit anywhere. You've got the best table in the house, and the other eleven."
- You sit. She brings tea in Tobin's heavy cups and sits across from you, which she never does.
- `$0` "My great-aunt ran this place for fifty years. When she left it to me I came down from the city to sell it. I was a hotel manager. Four hundred rooms, and I couldn't have told you a single guest's name."
- "I put the sign up the first week. Then the roof leaked, and I took it down to fix it. That was eight years ago." `$u` "The sign's still in the shed. Very good sign."
- **Choice:**
  - "You should sell it." → `$6` "Should I?" A pause. `$u` "Everyone says so. You're the first one who said it to my face. Have another scone."
  - "I'm glad you stayed." → `$l` "...Well. The scones are glad too." She looks away, at the empty tables.
  - (Eat another scone.) → `$h` "Now that's the right answer."
- At the door, she presses a box into your hands: what's left of the cake, for the farm.

**After:** you get a Pink Cake. Her Tue4 and Mon4 lines (the hotel, the sign) now have this to stand on.

### 4 hearts: the guest books (the attic, a quiet morning)

**Trigger:** you enter the Vale House between 9am and noon on a Wednesday (her quiet day: she counts the spoons), with 4+ hearts and her 2-heart event seen.

**Scene:**
- Bea meets you at the foot of the stairs with a lamp and a broom. `$u` "You've got a strong back and no sense of self-preservation. I need both. The attic."
- The attic (a small temporary map under the eaves, or the upstairs hall dressed with crates: to decide when it's built): trunks, a dressmaker's dummy, a broken easel, dust.
- She pulls a stack of old books out of a trunk. Guest books, fifty years of them. She opens one. Every page has a signature, and half of them have a little sketch: a tree, a teacup, the lake, a cat on a windowsill.
- `$6` "Oh. Oh, look at this. This one's the old tree, the year it was struck by lightning. And this..." She turns a page. "Somebody drew my aunt. She'd have hated it. It's perfect."
- `$s` "I came up here to throw all this out. I've been meaning to for eight years."
- You find a folded sketch in a desk drawer: the Vale House, drawn from the plaza, signed only with an initial. (This is the map doc's discovery, "a sketch tucked in a desk".)
- **Choice** (remembered: her 10-heart event puts the guest books where you said):
  - "Put them on a shelf downstairs, for guests to read." → `$l` "In the tea room? People would get jam on them." A pause. `$u` "...People should get jam on them." (*shelf*)
  - "Keep them up here, safe." → `$0` "Safe. Yes. That's what she did with them too." She closes the trunk gently. (*attic*)
  - "Read me another one." → `$h` She sits on the trunk and reads to you until the lamp runs low. (*read*)

**After:** her Thu4 line ("an attic I haven't opened") changes to one about the guest books. The folded sketch stays with you as a keepsake (a furniture item, to draw).

---

## Lupini

### 2 hearts: the artist's eye (the Vale's sign, a morning after rain)

**Trigger:** you're on the high street by the Vale's sign between 9am and noon, on a fine day after a rainy one, with 2+ hearts. (With the town trips switched on, a second version can play by the river in the Forest on his Saturday there.)

**Scene:**
- Lupini is crouched over a puddle in the road, sketchbook on his knee, straw hat pushed back. He doesn't look up. `$u` "Don't step in it. You'll ruin it."
- "Go on. What am I drawing?"
- **Choice:**
  - "The puddle?" → `$h` "Everyone says that. Wrong. Try again." (the choice comes round once more, without this answer)
  - "The clouds in it." → `$u` "Closer. Still wrong."
  - "A frog?" → `$a` He looks into the puddle for a long moment. "...There's no frog. Why would you say frog?"
- Whatever you say: he turns the sketchbook round. It's mostly white: a few lines of the road's edge, and the light lying flat across the water, the one bright stripe on it.
- `$0` "The light. The way it lies down on the water like it's tired. It does that for about ten minutes after rain, and then it gets up again."
- `$s` "I used to draw like this all day, when I was twenty. Before anyone was looking."
- He tears the page out and gives it to you. `$u` "There. A Lupini. Don't tell anyone, they'll want to know what it's worth."

**After:** you get the sketch (a small framed furniture item, to draw). His Thu4 line (the puddle by the path) now remembers this.

### 4 hearts: a place worth painting (the Vale, a late afternoon walk)

**Trigger:** you meet Lupini outside his atelier between 4pm and 6pm on a fine day, with 4+ hearts and his 2-heart event seen.

**Scene:**
- `$h` "My friend! Walk with me. I need somewhere to paint, and I've painted everywhere."
- The walk (the event moves you both across the Vale): at the overlook he stops, frames it with his hands, and walks on. `$u` "Too famous. It's on every postcard, and half of them are mine."
- At the lake: "Too blue. Nobody believes it." At the old tree: "Too gold. It's showing off, and it knows it."
- He gets quieter as they go. `$s` "Do you know, I can't remember the last time I painted something because I wanted to look at it."
- **Choice:** where to stop. Each is an ordinary corner of the Vale, and the one you pick becomes a new painting spot (a stool and a rock like his others, added to the map by this flag):
  - "Here, by Bea's washing line." → sheets on the line, the light through them. (*washing*)
  - "Here, the turning circle by the bus." → the empty road, the bus shelter, the long shadows. (*bus*)
  - "Here, Tobin's clay pit." → `$u` "He'll be unbearable." (*claypit*)
- He sits down where he is and starts sketching, and doesn't say anything for a long time. Then: `$l` "I don't know why this one. I don't know why at all."
- `$h` "That's the first good sign in twenty years."

**After:** the spot you chose is a painting spot from now on (`blinxxy.MuseArt_PaintSpot`, as his others are), and his 8-heart exhibition hangs this sketch. His Sun4 line ("paint when it's fun") follows on from this.

---

## Fern Aster

### 2 hearts: the bees (the wildflower meadow, a morning)

**Trigger:** you're in the wildflower meadow between 8am and noon, on a fine day in spring or summer, with 2+ hearts.

**Scene:**
- A mower is parked at the meadow's edge, shiny and red, with a label: *On loan from the Heritage Society. For the meadow.* Fern is on her knees in the long grass a long way from it, very still.
- `$u` "Don't move. Well, move, but slowly. Come and look."
- A bumblebee nest in a hollow in the grass, the bees going in and out. `$h` "They send me a mower and I find this. Every year they send me a mower, and every year I find something."
- `$a` "I'm not using it. I've never used it. I've told them. They keep sending it, like a very persistent present."
- She has a handful of garden stakes and string. "Help me mark round it, so nobody walks through."
- **Choice:**
  - "Can't we move the nest?" → `$s` "You don't move a nest. You move yourself." She hands you a stake. "Like this."
  - "We could mow round it." → `$a` "We could. We won't." Then: `$u` "...All right, a path round it. A small one. With scissors."
  - "Let's leave the whole meadow." → `$h` "That's what I keep telling them!"
- You stake out a ring round the nest together. She talks to the bees the whole time, and once, by accident, to you.

**After:** a ring of stakes stands round the nest in the meadow for the rest of the season (a small map edit by this flag). A few days later, a jar of honey in the mail: "From the bees. They asked me to write it. -F"

### 4 hearts: the grant (the sculpture garden, an afternoon)

**Trigger:** you're in the sculpture garden between 1pm and 5pm (in winter, when she's brushing snow off the statues), with 4+ hearts and her 2-heart event seen.

**Scene:**
- Fern sits on the plinth of the oldest statue with a letter in her lap. `$s` "The Heritage Society. They'll give the Vale a grant to restore the garden. Hedges, paths, everything back how it was a hundred years ago."
- "There's a condition. They want the statues cleaned. All of them."
- She stands and shows you the oldest one: a woman with a book, green-gold with a moss that grows in her folds and nowhere else in the Vale. `$0` "This moss doesn't grow anywhere else. I've looked. It took a hundred years to get here."
- `$u` "If I say no, the paths stay broken and the roof on the seedhouse leaks another winter. If I say yes, this goes in a bucket."
- `$0` "What would you do? Honestly. Don't be nice."
- **Choice** (remembered: it shapes her 8- and 10-heart events):
  - "Take the grant. Clean them." → `$s` long pause. "...That's honest. Thank you." (*restore*)
  - "Keep the moss. Lose the grant." → `$h` "That's what I want to hear. It's not what I need to hear." (*wild*)
  - "Clean some. Leave this one." → `$6`-like stillness (she has no surprised portrait: `$0`). "Some." She says it again. "Some. I hadn't thought of some." (*both*)
- She folds the letter and doesn't decide. `$u` "I'll write to them tomorrow. Or next week. Moss is patient. So am I."

**After:** nothing changes in the garden yet. Her Tue8 line (clearing one path) leans on this.

---

## Emmett Wren

### 2 hearts: an empty house (the amphitheatre, a morning)

**Trigger:** you're at the amphitheatre between 9am and noon, with 2+ hearts.

**Scene:**
- Emmett is alone on the stage, playing to the empty stone seats: a fast reel, all of it, with the bows at the end. Nobody claps.
- He sees you. `$u` "A crowd! Finally. Sit anywhere, they're all the best seats."
- You sit on the stone tiers. He plays a slower one, and stops halfway.
- `$0` "Five springs ago I walked in here at sunset. I'd played a wedding in the next valley and slept in a hedge. The seats were all brambles then. I sat on that one, the third row, and played to nobody."
- "I meant to stay a week." `$u` "The week's going well."
- **Choice:**
  - "Play me the rest of that one." → `$h` He does. It's better than the reel.
  - "Why did you stay?" → `$l` He opens his mouth, closes it, and plays the first bars of a song about a turnip instead. "...Long story. Very boring. Mostly turnips."
  - "Where are you from?" → `$s` "I've played in a hundred towns. I couldn't tell you which one I'm from."
- He bows to the empty seats, and to you. `$u` "Same time tomorrow. Bring a friend. Bring a hedge."

**After:** his Wed line ("I came for a week") and spring_Fri line follow on from this.

### 4 hearts: whose song (the lakeside, an afternoon)

**Trigger:** you're at the lakeside bench between 1pm and 5pm on a Tuesday or Thursday (his afternoons by the water with his notebook), with 4+ hearts and his 2-heart event seen.

**Scene:**
- Emmett sits on the bench with his notebook, a pencil behind his ear and another in his mouth. `$u` "You've caught me working. Don't tell anyone. It'll ruin my reputation."
- "I'm writing a song. Here, listen." He plays a few lines about the lake. It's good, and it stops where he hasn't finished it.
- "Now this one." A plain, slow tune. `$0` "A farmer's widow taught me that, way up north, the winter I mended her fence. She'd made it up for her husband. She sang it at the kitchen sink every night for forty years, and never once to anybody."
- `$s` "Half my favourite songs came like that. Ordinary folk, singing to the washing-up. Nobody writes them down. So I keep them."
- **Choice:**
  - "The widow's is better." → `$h` "It is, isn't it. I've been trying to write one like it for twenty years." (*widow*)
  - "Yours is better." → `$l` "Liar. Thank you." (*yours*)
  - "They sound like the same song." → long pause. `$s` "...They do. Hers is about somewhere she belonged." He closes the notebook. (*same*)
- He hums the widow's tune all the way back up to the plaza.

**After:** from now on, some Fridays he takes the bus to play the Saloon (switch `data/vale_town_trips.json`'s Friday from its 4-hearts stand-in to `HasSeenEvent` on this event). His Tue4 line (songs from ordinary folk) was waiting for this.

---

## What building these needs

- **The residents' sprite sheets**, for the actors (and Bea's broom and lamp, Lupini's sketchbook, Emmett's fiddle as animation frames or props).
- **Small items to draw:** the attic sketch (Bea 4), Lupini's puddle sketch (Lupini 2): framed furniture, like Julian's review.
- **Map edits by flag:** Lupini's chosen spot (stool and rock, `blinxxy.MuseArt_PaintSpot`), Fern's ring of stakes. The attic for Bea's 4 hearts: a temporary map, or the upstairs hall dressed for the scene.
- **Mail:** Fern's honey letter; the remembered choices as mail flags.

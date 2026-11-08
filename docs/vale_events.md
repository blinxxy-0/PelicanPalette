# The Vale residents' heart events (DRAFT, for approval)

The 2- to 10-heart events for Bea, Lupini, Fern and Emmett (1.4; the 6- to 10-heart ones are further down, after "What building these needs"), from their briefs (`docs/bea_brief.md`, `docs/lupini_brief.md`, `docs/fern_brief.md`, `docs/emmett_brief.md`). Tobin's events are 1.5's, through Pottery. Nothing here is built: the events need the residents' sprite sheets, and the scripts need your approval first.

Portrait codes are the ones their dialogue already uses: `$0` neutral, `$h` happy, `$s` sad, `$u` each one's own face (Bea's dry half-smile, Lupini's theatrical look, Fern's cheeky closed-mouth grin, Emmett's crooked grin), `$l` touched or caught out, `$a` cross. Bea's sheet also has `$6` surprised.

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
- Lupini is crouched over a puddle in the road, sketchbook on his knee, glasses pushed down his nose. He doesn't look up. `$u` "Don't step in it. You'll ruin it."
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

---

# 6 to 10 hearts

The rest of each arc, from the same briefs. As above: where each resident already is, each needing the one before, choices remembered as mail flags. Fern's and Emmett's 10-heart events need you to be dating them (the bouquet), as vanilla romances' do; their 12- and 14-heart events and married life come with the marriage plan (spouse rooms, patios, the wedding), outlined at the end.

---

## Bea Thistle

### 6 hearts: the offer (the plaza, an afternoon)

**Trigger:** you cross the plaza between 1pm and 4pm on a fine day (her tea-garden hours), with 6+ hearts and her 4-heart event seen.

**Scene:**
- A man in a city suit stands by the fountain with a leather folder. Bea is with him, arms folded, tea towel over her shoulder. He shakes her hand, says "Think it over, Ms. Thistle. It's a very generous number," and walks off towards the bus.
- She sits on the edge of the fountain. `$0` "A hotel group. They want the Vale House. They'd put in a spa." A pause. `$u` "In the kitchen, as far as I can tell."
- "It's more than the inn's worth. It's more than the inn's ever made. I could go back to the city and never fix another pipe."
- `$s` "Everyone here would tell me to keep it. Everyone here has a tab." She looks at you. "What would you do? Not for them. For me."
- **Choice** (remembered; it doesn't decide anything, she's asking herself):
  - "Sell it. You've earned a rest." → `$s` "I have, haven't I." She doesn't sound relieved. (*sell*)
  - "Keep it, but run it your way." → `$6` "My way. I don't think I've got a way. I've got hers." (*own*)
  - "What do you want?" → long pause. `$s` "...Nobody's asked me that in eight years. I don't know. That's the trouble." (*want*)
- She stands and brushes off her apron. `$u` "Well. The scones won't bake themselves. I've checked."

**After:** for a week her lines are shorter and she's at the counter late. The folder sits on the end of the counter, unopened, every time you visit.

### 8 hearts: dinner for one (the Vale House, closed, an evening)

**Trigger:** you enter the Vale House between 7pm and 10pm on a Wednesday, with 8+ hearts and her 6-heart event seen. A card on the door says *Closed tonight*; it opens anyway, for you.

**Scene:**
- The tea room's dark but for one lamp. Bea is at the stove in the kitchen corner, cooking, humming. One place is laid at the dining table. When she sees you she lays another.
- `$l` "Don't make a fuss. I'm cooking for myself. First time since the city. Nobody's coach, nobody's booking. Just mine."
- It's a plain stew from the hotel's staff kitchen, which she ate at midnight for ten years. `$h` "This is the best thing I've ever made, and nobody's paying for it."
- "I wrote to the hotel people this morning." She puts down her fork. `$0` "I said no. Then I said no again, in case the first one was too polite."
- `$s` "I thought I was keeping it because I owed her. I'm not. I'm keeping it because when I'm not looking after anybody, I don't know who I am. I'd like to find out here."
- **Choice:**
  - "Then stay and find out." → `$l` "I'm going to. Eat your stew."
  - "Your aunt would be proud." → `$s` "She'd be furious I didn't put a spa in." Then, `$h`: "...She'd be proud."
- You wash up together. She lets you dry, which she has never let anyone do.

**After:** the hotel folder's gone from the counter. Her Mon8 line (what she'd like for dinner) changes to one about the stew.

### 10 hearts: the artists' room (the Vale House, reopened, a morning)

**Trigger:** you enter the Vale House between 9am and noon, with 10 hearts and her 8-heart event seen, the day after a new sign goes up outside: *The Vale House. Under new management (the same management).*

**Scene:**
- The tea room's changed. The guest books from the attic are on a shelf by the window with a card: *Read us. Jam permitted.* (If you told her at 4 hearts to keep them in the attic, they're on a shelf in the hall upstairs instead; if you asked her to read one, it lies open on the counter at her aunt's page.)
- Two young artists off the bus are sketching at a tea table. `$h` "They can't pay. I've told them that's the price."
- She takes you upstairs to the room beside hers, the one with the canvases and easels, and unlocks it. `$0` "This was the artists' room when the Vale was full. Anyone who came to paint and couldn't pay slept here. My aunt stopped it when the money ran out."
- `$l` "I've started it again. And this is yours." She gives you a key on a ribbon. "Any night you like. Free. Don't tell the others, they'll all want one."
- **Choice:**
  - "Thank you, Bea." → `$l` "Don't. I'll cry, and then I'll have to make more tea."
  - "What do you want for dinner?" → `$h` She laughs, properly. "Stew. Obviously."

**After:** the artists' room is yours: its bed works any night, free (the mail flag `blinxxy.MuseArt_ValeArtistsRoom`, which the room-booking code already honours), and its door opens for you. Now and then a visiting artist turns up at a tea table (the brief's small one-off scenes, to come).

---

## Lupini

### 6 hearts: the unfinished painting (the Old Studio, an afternoon)

**Trigger:** you meet Lupini outside the boarded Old Studios between 1pm and 4pm, with 6+ hearts and his 4-heart event seen.

**Scene:**
- He's prised the boards off the big studio's door. `$u` "I've walked past this door for thirty years. Today it was unlocked. Doors don't do that, my friend. Come in before it changes its mind."
- Inside (the Old Studio as built: the unfinished easel in the window's light, the note on its little table, the sketches pinned up, the painting hung backward): dust, a cot, a washstand. On the easel, a landscape of the Vale, three-quarters finished. The sky's done; the valley floor is bare canvas.
- The note, in pencil: *Back after lunch.* No name, no date. `$s` "Whoever it was, they never came back from lunch."
- He turns the backward painting round: the same view, finished, and worse. `$u` "They tried it twice. The unfinished one's better. It usually is."
- If you've met Margot: `$0` "That conservator of yours, at the museum. She could make this as good as the day it was left." He doesn't sound sure he wants that.
- **Choice** (remembered; it shapes his exhibition):
  - "Restore it." → `$0` "Keep it as it was. Yes. That's a kind of honesty." (*restore*)
  - "Finish it." → `$0` A long stillness. "Finish someone else's painting. I've done that for twenty years, with my own." (*finish*)
  - "Leave it as it is." → `$l` "As it is. Unfinished, and nobody's." (*leave*)
- He doesn't decide. `$s` "Let's leave the door open. It's been shut long enough."

**After:** the big Old Studio stays open from now on (the map doc's plan). His Thu6 line (the studios, full once) follows on from this.

### 8 hearts: the exhibition (the Exhibition Hall, an evening)

**Trigger:** you enter the plaza between 6pm and 9pm on a fine day, with 8+ hearts and his 6-heart event seen. The Hall's doors are open and lit.

**Scene:**
- The dust sheets are off: the event sets `blinxxy.MuseArt_ValeHallOpen`, and the closed hall gives way to the gallery for good. The residents are there with cups of Bea's tea: Tobin by a plinth of his bowls, Fern beside a case of her pressed flowers, Emmett tuning in a corner.
- No judges, no prices. On the end wall, two pictures and nothing else of Lupini's: the unfinished painting from the Old Studio (restored, finished or as it was, by your choice at 6 hearts) and his sketch from your walk at 4 hearts.
- `$s` He's standing at the back, as far from his own wall as the room allows. "I'm terrified. Isn't that ridiculous? I've had shows in three cities."
- "None of them were for anyone. They were for buyers." He watches Tobin explaining a bowl to Bea, who's heard it before. `$l` "This one's for everyone."
- **Choice:**
  - "Go and stand by your wall." → `$u` "You're very bossy for a farmer." He goes. Someone asks him about the sketch, and he talks for twenty minutes.
  - "Stay back here with me." → `$h` "Yes. The view's better from the back. It always is."
- Emmett plays. Bea makes a speech nobody can hear. Lupini laughs at something Tobin says, loudly.

**After:** the Hall is open for good, and the Vale's sign is repainted (the map doc). His paintings in the Hall change with the seasons from now on.

### 10 hearts: something nobody will see (the overlook, dawn)

**Trigger:** you're at the overlook bench between 6am and 7am, with 10 hearts and his 8-heart event seen.

**Scene:**
- Two easels stand at the edge, facing the Vale. He's at one, painting. `$0` "There you are. That one's yours. Don't argue, it's too early."
- "I'm painting something I'll never show. Nobody's going to buy it, or review it, or hang it. When it's done I'm going to put it in a cupboard."
- "Paint with me. Don't show me yours either."
- You paint together in silence as the sun comes up over the falls. (The scene fades through the sunrise; no minigame. The painting is yours: a furniture item, the Vale at dawn.)
- If you've seen Sable's 8-heart event: footsteps on the path. Sable, with a sketchbook, sees the two of you and stops. Lupini doesn't turn round. `$u` "There's a third easel in the shed, if you're staying." A long pause; Sable fetches it and sets it up. Nobody says anything for the rest of the scene, and it's the most the two of them have ever said to each other.
- `$l` At the end: "Do you know, I haven't enjoyed anything this much since I was twenty. I was very bad at painting at twenty."
- He gives you his old travelling easel. `$h` "It's carried every Lupini I ever liked. It won't make you better. It'll make you go outside."

**After:** you get his travelling easel (furniture only: Painting Mastery's master's easel stays the only easel with a bonus, the 1.4 plan's rule) and the dawn painting. He's seen painting for fun now: worse paintings, he says, laughing.

---

## Fern Aster

### 6 hearts: rows (your farm, a morning)

**Trigger:** you leave your farmhouse between 7am and 11am on a fine day, with 6+ hearts and her 4-heart event seen. Fern is at your fence: her first visit to the farm, on the early bus.

**Scene:**
- `$u` "I was passing. Nobody passes your farm. I came on purpose."
- She walks your fields with her hands in her dungaree pockets, quiet for a long time. She crouches at a straight row and touches a leaf.
- `$0` "It's lovely. It's all so... tidy. Everything where you put it."
- "My teacher used to say a good garden needs one thing nobody planned. Something that turns up and asks to stay." She takes a seed packet from her front pocket: the season's wild seeds, in a packet she folded herself.
- `$l` "Plant these somewhere. Anywhere. Don't decide where. Throw them, if you like."
- **Choice:**
  - "By the house, where I'll see them." → `$h` "Good. You'll have to look at them every morning and not tidy them."
  - "In the middle of a row." → `$h` She laughs, loudest at herself. "Now you're learning."
  - "Somewhere I'll forget." → `$l` "That's the best place. That's where all the good things grow."

**After:** you get a packet of the season's wild seeds (the game's own Spring, Summer, Fall or Winter Seeds; her wild corner of the farm was dropped, 2026-11-04). Her Sat8 line (forgiving the rows) waits for this.

### 8 hearts: the night vine (the seedhouse, at night)

**Trigger:** you enter Fern's cottage between 8pm and midnight on a fine summer night, with 8+ hearts and her 6-heart event seen. (Outside summer, a letter instead: "Not yet. It's waiting for summer. So am I. -F", and the event waits.)

**Scene:**
- The seedhouse is dark but for a lantern by the trellis. Fern sits on an upturned pot in front of the night-flowering vine, a blanket round her shoulders. `$l` "Shh. Sit. It's going to happen. I can tell. I've been wrong three years running, but I can tell."
- You wait. She talks quietly, to the vine and then to you.
- `$s` "Everyone thinks letting things grow is a philosophy. Sometimes it is. Sometimes I just can't keep up. A whole Vale of gardens, and one of me."
- "My teacher kept every hedge square for seventy years. I couldn't. So I told everyone the wild was better. What if I only said it because I can't do the other thing?"
- **Choice:**
  - "Does it matter, if the garden's happy?" → `$0` She thinks about it for a long time. "...It might not. Huh."
  - "You chose. You could have kept them square." → `$l` "Could I?" A pause. "...I could. I didn't want to. Oh."
  - (Say nothing; take her hand.) → `$l` She lets you.
- The vine opens: pale flowers, one after another, glowing faintly in the lantern light. `$h` She laughs out loud, claps a hand over her mouth, and laughs again.

**After:** from now on the vine flowers on summer nights (a seasonal prop in the seedhouse, to draw). A cutting comes in the mail a few days later: "From the vine. It took three years. Be patient with it. -F" (a decorative plant, to draw).

### 10 hearts: one path (the sculpture garden, dating, a morning)

**Trigger:** you enter the sculpture garden between 8am and noon, with 10 hearts, dating Fern, and her 8-heart event seen.

**Scene:**
- One path through the garden has been cleared: swept flagstones, the hedges either side trimmed back just enough. The rest is as wild as ever. Fern stands at the end of it with shears, sweaty and pleased.
- `$h` "One path. Just one. Look."
- Along it, statues you couldn't see before: a woman with a book (the oldest, her moss still on), a boy with a kite, a pair of dancers. `$0` "I wrote to the Heritage Society. I said they could have one path, cleaned and swept, for visitors. The rest stays wild. They said yes." She sounds amazed. "They said yes!"
- If you told her "some" at 4 hearts: `$l` "You said some. I couldn't stop thinking about some."
- `$l` "It's not neither. It's both, chosen carefully. Like your farm: a bit you shape, and a wild bit you leave."
- "Walk it with me? You're the first."
- **Choice:**
  - "Lead the way." → She takes your hand and talks to every statue you pass, introducing you.
  - "Which statue's your favourite?" → `$u` "The one with the moss. Obviously. She's a hundred years old and still growing things."

**After:** the path stays cleared for good (a map edit by this flag), its statues visible. In 1.5, sculpting begins beside them.

---

## Emmett Wren

### 6 hearts: a musical evening (the amphitheatre, a late afternoon)

**Trigger:** you're at the amphitheatre between 3pm and 6pm on a fine day, with 6+ hearts and his 4-heart event seen.

**Scene:**
- Emmett is hacking at the brambles on the stone seats with a borrowed sickle (Fern's). `$u` "Ah. Perfect timing. Grab some gloves. You're a farmer, this is basically farming."
- The event passes in a few quick scenes: you clear the tiers together; Tobin turns up with a wheelbarrow, complaining; Fern arrives to make sure nobody's killing anything rare; Bea brings tea, and stays.
- By evening the seats are clear for the first time in decades. `$0` "When I walked in here, it was all brambles. I thought, nobody's sat here for forty years. Nobody will."
- The residents take their seats: Bea and Tobin in the front row, arguing; Fern on the steps; Lupini, late, at the very back. Emmett plays. The amphitheatre does what it was built for, and the sound carries all the way to the lake.
- **Choice:**
  - "Play another!" → `$h` He does. Then another. Bea shouts for a slow one.
  - (Sit with the others and listen.) → `$l` After the last tune, he looks for you in the seats first.
- `$l` Afterwards, alone with you on the stage: "Funny. I've played to thousands. I've never been nervous. Tonight my hands were shaking."

**After:** the amphitheatre stays cleared (a map edit by this flag). Some evenings the residents come down to listen.

### 8 hearts: the song (the Vale House, late)

**Trigger:** you enter the Vale House between 10pm and midnight, with 8+ hearts and his 6-heart event seen. The tea room's empty but for him; Bea's gone up to bed, and the fire's low.

**Scene:**
- Emmett is by the fire with the fiddle across his knees, not playing. `$s` "Couldn't sleep. Bea said I could stay if I didn't play anything with a chorus."
- "I've been writing something. The first song I've ever written about a place I live." `$l` "I haven't played it for anyone. Sit there. No, there, where I can't see your face."
- He plays it: a slow tune, the Vale in verses. The inn and its two good roofs. A potter who says a bowl holds soup. A gardener talking to the hedges. A painter standing at his easel, not painting. (The lyrics appear a verse at a time.)
- Then a verse he clearly didn't mean to sing: about someone who comes off the bus with soil on their boots and makes the Vale feel like somewhere you could stay.
- He stops. `$l` "...That verse isn't finished. That verse isn't anything. Forget that verse."
- **Choice:**
  - "Play it again." → `$l` A long pause. He plays it again, slower.
  - "It's the best one." → `$h` "...It is, isn't it. Blast."
  - "Who's it about?" → `$u` "A ferryman. From the north. Very famous ferryman." He doesn't meet your eye.

**After:** the song is his to play at the Open House from now on (`docs/design/open_house.md`). His Fri8 line (the verse he hasn't played) was waiting for this.

### 10 hearts: the bag (the overlook, dating, a spring morning)

**Trigger:** you're at the overlook between 7am and 10am in spring, with 10 hearts, dating Emmett, and his 8-heart event seen. (Outside spring the event waits for it: every spring he packs his bag, his Tue6 line.)

**Scene:**
- Emmett sits on the bench with his old travelling pack at his feet, stuffed full, the fiddle case strapped on top, his long coat on. `$0` "Every spring. Same thing. I pack the bag, I walk up here, and I look at the road out of the Vale."
- "Five years, I've never gone. But I've never unpacked either. It's sat by my bed, packed, for five years. In case."
- `$s` "I've lived out of that bag since I was eighteen. Every town, every hedge. It's the only home I've ever had, and it's a bag."
- He looks at you for a long time. Then he opens the pack and lays things out on the bench, one at a time: a spare shirt, a tin cup, the songbook, a map of the Republic with every town crossed through.
- `$l` "I'm unpacking it. In front of you. So I can't pretend I didn't."
- **Choice:**
  - "Welcome home." → `$l` He laughs, and it cracks halfway. "...Say that again. I want to write it down."
  - "Want help carrying it back?" → `$h` "The bag? No. It's empty. It's very light now."
- He folds the map with every town crossed through and gives it to you. `$u` "Here. I won't be needing it. Every town in Ferngill, and I've finally found the one I'm from."

**After:** you get his map of the Republic (framed furniture, to draw). The pack hangs on a hook in his cottage, empty, and stays there.

---

## Fern and Emmett after 10 hearts (an outline only)

From the briefs, to plan with their marriages (spouse rooms, patios, the wedding, married lines), which need their own design doc and art:
- **Fern, 14 hearts (married):** a seed she planted on the farm the day she moved in grows into something nobody's seen before.
- **Emmett, 14 hearts (married):** he's written a song about the farm, and plays it to the animals first, because they're "a tough crowd".
- **12 hearts** for each: to write with the marriage plan.

## What building the 6- to 10-heart events needs

- **Items to draw:** Lupini's travelling easel (furniture only) and the dawn painting; Fern's vine cutting; Emmett's map of the Republic.
- **Map edits by flag:** the Old Studio open (Lupini 6), the Hall open (Lupini 8: the hall-closed work's `blinxxy.MuseArt_ValeHallOpen`), the repainted sign (Lupini 8), the cleared amphitheatre (Emmett 6), Fern's one path (Fern 10), the night vine in flower (Fern 8, summer nights).
- **Code:** the artists' room's door opening for you after Bea's 10 hearts (the room-booking code already honours `blinxxy.MuseArt_ValeArtistsRoom` for the bed).
- **Cameos:** the hotel group's man (Bea 6) and two visiting artists (Bea 10): extra sprites, or vanilla visitors reused.

# Margot Pell: engagement and married life (approved as written 2026-11-02; built)

The rest of her marriage is built (2026-11-01): the spouse room (her
restoration table, plan chest, the valley map, a museum crate), the patio
(an easel and the drying rack of her own small canvases), and the wedding
(the game's own, in her dress, with Gunther and Sable in the front row and
Julian at the back if you've met him). What's left is her words, below.

Keys are the game's (Sable's file and Leah's are the models). Portrait codes
as in her other lines: `$h` happy, `$u` her quick grin, `$l` love, `$s` sad,
`$a` cross, `$6`/`$7` her surprised and thinking faces. Items in [brackets]
are a little something she leaves for you that morning.

Built (2026-11-01) as drafted, ahead of approval: tools/build_margot_marriage.py
reads the lines below straight from this file, so edit them here and rerun it.

## Engagement

- **Bouquet** (8+ hearts; approved in docs/margot_events.md): `$l` "Flowers. For me. I'm going to press one and keep it forever and you're not allowed to watch me do it."
- **Mermaid's pendant** (10 hearts, dating; approved): `$6` "...Yes. Yes! Sorry. Yes." beat. `$h` "I'm going to need a new card for the museum."
  (The game answers the pendant from Data/EngagementDialogue: `MargotPell0` for her yes, `MargotPell1` while you're engaged.)
- **Engaged, until the wedding** (new): `$l` "I keep writing 'Margot Pell' on things and stopping halfway. Like I've forgotten how it ends."

## Her days, married

She keeps her museum hours (the draft: "she keeps her museum hours"):

- **Mon to Sat:** leaves the farm at 7:30 for the museum, home by 17:00. It's 202 tiles from the farm, so she's there at about 10:00: home for your morning chat, at work mid-morning.
- **Sun:** her day off. She walks to the river bend from her 8-heart event (Cindersap Forest) with a sketchbook, home by 15:00.

## Married lines

### Rainy days
- Rainy_Day_0: "Rain. The museum's damp alarm will be shrieking. Gunther can shriek back.$u"
- Rainy_Day_1: "I made coffee. It's strong enough to strip varnish, so sip.[(O)395]$h"
- Rainy_Day_2: "Grey light is honest light. Every crack in a painting shows on a day like this.$7"
- Rainy_Day_3: "Don't open the window near my table. Damp is the enemy. Well. Damp and you, when you're muddy.$u"
- Rainy_Day_4: "I'm going to sit by the stove and write condition reports. It's my idea of a holiday. Don't judge.$h"

### Rainy nights
- Rainy_Night_0: "You're soaked through. Sit. I'll get a towel. Not that one, that's for the paintings.$u"
- Rainy_Night_1: "The rain sounds like someone brushing a canvas very, very gently.$l"
- Rainy_Night_2: "I found a watermark on an old landscape today shaped exactly like a duck. I've told no one but you.$h"
- Rainy_Night_3: "Come and get warm. You're the only thing in this house I don't have to handle with gloves.$l"
- Rainy_Night_4: "Days like this, the old painters stayed in and changed their minds. You can see it under the paint.$7"

### Indoors, mornings
- Indoor_Day_0: "Good morning. There's a painting on the kitchen table. Don't put toast on it.$u"
- Indoor_Day_1: "I brought you something from the museum's spare drawer. It's allowed. Mostly.[(O)blinxxy.MuseArt_Pigment_ochre]$h"
- Indoor_Day_2: "I've been up since five cleaning a frame. It's the best part of the day. The frame agrees.$h"
- Indoor_Day_3: "You sleep like a statue. A nice statue. Restored.$l"
- Indoor_Day_4: "Coffee's on. I labelled the jars this time, so it's definitely coffee.$u"

### Indoors, evenings
- Indoor_Night_0: "Sit with me? I want to look at something that isn't a hundred years old.$l"
- Indoor_Night_1: "I used to go home to a room full of crates. Now I come home to you. The crates are jealous.$h"
- Indoor_Night_2: "Gunther says hello. He also says the museum is quieter without me, and he meant it as a compliment.$u"
- Indoor_Night_3: "I keep looking at the pendant. I've seen a thousand blues in a thousand paintings. None like this one.$l"
- Indoor_Night_4: "I'm painting something in my corner. Don't look. It's not finished. Nothing I make is ever finished.$u"

### Outdoors
- Outdoor_0: "The light on your fields this morning. Someone should paint it. ...Fine. Me.$h"
- Outdoor_1: "The fences want mending. I can see it from here. Occupational hazard.$u"
- Outdoor_2: "Every farm in every old painting looks like this one. Now I know why they painted it.$l"
- Outdoor_3: "Don't work too hard. You're the only original I've got.$l"
- Outdoor_4: "I found an old horseshoe by the fence. I've catalogued it. It's item one in our collection.$h"

### Good
- Good_0: "Good morning, my favourite thing in the valley.$l"
- Good_1: "I used to think my job was keeping things from changing. It's not. It's letting them keep going.$h"
- Good_2: "You've got a bit of paint on your ear. ...Leave it. It suits you.$u"
- Good_3: "I signed a museum card today. 'Restored by Margot Pell.' I looked at it for a full minute.$h"
- Good_4: "When I came to the valley, I thought I was here for the paintings. I wasn't.$l"
- Good_5: "Gunther asked if I'm happy. I said yes before he'd finished the question.$h"
- Good_6: "Be careful in the mines. You can't restore a person. I've checked.$s"
- Good_7: "Thank you for making me stay.$l"

### Neutral
- Neutral_0: "Morning. I'm thinking about glue. It's a whole field, you know.$7"
- Neutral_1: "The museum's quiet this week. Everyone's busy. Including you.$0"
- Neutral_2: "If you're passing the museum, Gunther has a crate with my name on it. Gently, please.$u"
- Neutral_3: "We're out of linseed oil. Also milk. Mostly linseed oil.$0"
- Neutral_4: "I've rearranged the shelves by period. You'll get used to it.$u"
- Neutral_5: "Do you think the cows would mind if I sketched them? Not for anything. Just practice.$7"

### Bad
- Bad_0: "You've stopped asking about my day.$s"
- Bad_1: "I'm working late at the museum a lot. It's easier than coming home to nobody.$s"
- Bad_2: "Paintings fade if you leave them in the dark too long. So do people.$s"
- Bad_3: "...Do you still want me here?$s"
- Bad_4: "I fixed a whole painting today and didn't tell anyone. Not even you. Especially not you.$s"

### Her places and her day
- spouseRoom_MargotPell: "North light at my own table, in my own house. I'm never moving again. Don't make me.$l"
- patio_MargotPell: "Don't look! It's the barn. It's my first barn. Barns are harder than they look.$u"
- jobLeave_MargotPell: "Off to the museum. If anything old turns up in the fields, don't clean it. Bring it to me.$u"
- jobReturn_MargotPell: "Three condition reports and a frame that fought back. I won. What's for dinner?$h"
- funLeave_MargotPell: "I'm going to the river bend with my sketchbook. Where they stood, remember?$l"
- funReturn_MargotPell: "I drew the willow nine times. The willow won. How was your day?$h"

### Children
- OneKid_0: "%kid1 put a sticker on the old painting in my corner. I've decided it's a restoration.$h"
- TwoKids_0: "Two of them. The house has never been less quiet or more lived in. I love it.$h"

### Days of the year
- spring_1: "A new year. A new catalogue. Item one: you.$l"
- spring_19: "You remembered! I'm going to put today in the record books. Literally. I keep a record book.$h"
- summer_1: "Summer. Everything in the museum sweats a little. Including Gunther.$u"
- fall_1: "Autumn. The light goes gold and every painting looks like it was meant to.$h"
- winter_1: "Snow. Come and look. Blue in the shadows, gold where the sun is. Like an old master's.$l"

# The Open House (DRAFT, for approval)

The Gilded Vale's festival, Fall 7 (decided 2026-11-03; 1.4 plan, section 2). The concept sheet's festival with no judging: the Vale shows its art for a day, and anyone can show theirs. Julian's painting contest at the Fair (Fall 16, nine days later) is the competitive one; this is its opposite. Fall 7 is a Sunday, so Margot is free (her Painting Wing afternoons are Tuesdays and Thursdays), and Julian comes as an extra visit off the morning bus.

## The one decision: an open day, not a closed festival

Stardew has two kinds of festival:
- **Active festivals** (the Egg Festival, the Fair): the day stops, you're taken to a festival map, the clock freezes, and you leave when it ends. Pelican Town's are all this kind.
- **Passive festivals** (the Night Market, the Trout Derby, 1.6's `Data/PassiveFestivals`): an ordinary day with something on. The map changes for the day, people's schedules change, you come and go as you like.

**Proposed: a passive festival.** The Vale is a bus ride away, so a festival that grabs your whole day would mean it starts the moment you arrive, which an active festival can't do in a location you reach by bus. An open day also suits the idea: no ceremony, no winner, people wandering between showcases. It lets the shops stay open and the clock run, so you can go home and water the crops.

## The day

- **When:** Fall 7, 9am to 6pm, every year. (With Stardew Valley Expanded: no SVE festival that day, its Community Day and Joja Day being the 22nd; Fall 7 is SVE's Morgan's birthday, which an open day doesn't get in the way of. Checked 2026-11-08.) On the calendar ("Open House", the Vale's gold tree as its icon: an icon to draw). A note in the mail on Fall 6, from Bea: the programme.
- **Where:** the plaza under the old tree, at its most golden; the Exhibition Hall (if Lupini's 8-heart event has opened it); the amphitheatre in the afternoon.
- **Map for the day:** a festival version of the plaza (`MapReplacements` in the passive festival's data, as the Night Market swaps the beach): bunting from the old tree, lanterns, trestle tables, a refreshments stall, the display easels. Vanilla props where the game has them (the Fair's and the Night Market's tables, lanterns and bunting), so little new art.
- **Rain:** it goes ahead. The refreshments move into the Vale House, and Emmett plays in the Hall (or the Vale House, if the Hall's still closed).

## Who's where (schedule keys `fall_7`)

| Who | Showcase |
|---|---|
| **Bea** | The refreshments table in the plaza (her brief): tea, cake, her soup. Her shop menu, opened from the stall instead of the counter. |
| **Tobin** | A table of his bowls and jugs by the fountain, "for using, not looking at". A small bowl to buy for the day (his ware: needs its art). |
| **Fern** | A flower stall: cut flowers she grumbles about, seed packets, a jar of the Vale's honey. Her seed shop, from the stall. |
| **Lupini** | In the Hall by the season's paintings if it's open; before then, at an easel under the old tree, painting the crowd (and telling everyone it's terrible). |
| **Emmett** | The amphitheatre, all afternoon (his brief); in the morning, wandering, playing to whoever stands still. After his 8-heart event, his song about the Vale is in the set. |
| **Julian** | Off the morning bus at 10, back on the 5 o'clock, like his other visits. Looks at everything, reviews nothing, out loud. When both are far enough in their stories, he and Lupini meet here: the review he doesn't remember writing (Lupini's brief; a scene to write). |
| **Margot** | The Hall's restored finds; worried about the humidity. |
| **Leah** | From 1.5, once she's begun sculpting with you. |

Getting Julian and Margot there needs the NPC route between the Bus Stop and the Vale (built: `data/vale_town_trips.json`), switched on for the day whether or not the town-trips switch is.

**More of the valley (to decide):** Pam could run an extra bus that day and a few townspeople could come: Gus (to Bea's stall), Elliott, Evelyn (to Fern's), Robin (to Tobin's planters). Each one is a schedule key and a line, so it's cheap; the question is how busy the Vale should feel.

## Showing a painting

The heart of it: a row of display easels under the old tree, one free for you.
- **Hang one:** talk to the free easel and choose a painting from your inventory, as you enter the Fair's contest (`PaintingContest.cs`: the entry sits in a shared inventory for the day, so it's safe in multiplayer, one easel per farmer). You get it back at the end of the day, or from the easel.
- **No judging.** Instead, people stop and look. Through the day the residents and visitors who pass your easel say one line about it: what it shows, how it makes them feel. The mod already knows a painting's subject and quality (the gallery evening and the contest read them), so the lines can be about *your* painting: Bea likes anything with food in it, Fern anything with plants, Lupini notices the light, Tobin asks what it's for.
- **A small reward:** friendship with everyone who stops (a little, once), and the first year, a keepsake: the Open House poster (furniture, to draw) with your painting's name on the list of exhibitors.
- **Past artists:** the other easels hold the Vale's own: Tobin's bowls on a plinth, Fern's pressed flowers, one of the Hall's restored finds, a resident's child's drawing (to decide).

## What's needed

- `Data/PassiveFestivals` entry (Fall 7, 9am-6pm, the plaza's festival map), calendar icon, Bea's programme letter.
- The festival plaza map patch (vanilla props mostly).
- `fall_7` schedules for the five, Julian and Margot; their festival lines (`fall_7` dialogue keys).
- C#: the display easel (from the contest's entry code), the passers-by's lines about your painting, the stalls opening the residents' shops. Debug-only like the rest of the Vale until release.
- Art: the residents' sprites (for everyone to be there), the poster, the calendar icon, Tobin's ware.

## Open questions

1. A passive festival (an open day), as proposed, or an active one like the Fair?
2. How many townspeople come, beyond Julian and Margot (and Leah from 1.5)?
3. Should showing a painting give anything beyond friendship and the first year's poster?

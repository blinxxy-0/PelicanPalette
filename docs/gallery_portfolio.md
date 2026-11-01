# The gallery evening: your portfolio (1.2, step 7) — draft for approval

Sable's 14-heart event, the exhibition, stays as it is. This adds one beat to
it: your portfolio book, brought out of the back room for the evening, and a
villager who finds one of your paintings in it.

## Where it goes

- **The book:** on the back room's lectern (the same sprite), carried out to
  the gallery floor for the evening, at (12, 10), between the portrait panel
  and the easels.
- **When:** after Haley and Robin have spoken, before Evelyn. That's the
  middle of the evening, while people are still wandering round.
- **No control:** the game walks the scene (your rule for cutscenes).

## The beat

> *The portfolio book has come out of the back room for the evening, open on
> its lectern. Someone's paging through it.*

Then one villager, chosen by what's in the book:

1. **A portrait of someone at the party.** If the book holds a portrait of
   Lewis, Leah, Elliott, Haley, Robin, Evelyn, George or Emily, that person
   finds it. If there are several, the first in that order.
2. **Otherwise your best painting.** Leah finds it, and names it.
3. **An empty book.** Sable has put out a pile of your sketches instead.

A small event command picks the case and fills in the line, the way
`blinxxy.MuseArt_GiveFarmerPortrait` works now.

### 1. Their own portrait

- **Lewis:** "Is this me? Hm. You've given me more hair than I've had in years. I approve."
- **Leah:** "That's me! You got the way I squint at things. Nobody ever notices that."
- **Elliott:** "My likeness, caught mid-reverie. I may weep. I may write about weeping."
- **Haley:** "Wait, that's me? ...Okay, the lighting's actually really flattering. I want a copy."
- **Robin:** "That's me in my work clothes! You even got the sawdust. Sort of."
- **Evelyn:** "Oh, my. You painted me with my roses. George, come and look!"
- **George:** "Hmph. That's me. ...Don't tell anyone I like it."
- **Emily:** "My portrait! Look at the colours you put round me. It's like you saw my aura."

### 2. Your best painting (Leah)

The best is the highest quality, then the most stars, as Julian's review picks it.

- **Iridium, or a Masterpiece:** "'{title}.' The farmer painted this? I need to sit down."
- **Gold:** "'{title}.' Look at that light. The farmer's going to put us all out of work."
- **Otherwise:** "'{title}.' It's honest. You can tell they painted what they saw, not what they thought they should."

### 3. An empty book

- **Leah:** "Sketches? These are the farmer's? Sable, you've been keeping secrets."
- **Sable:** "They're not ready for the book yet. Neither was I, once."

## After the event

The plan said the book would then stand in the shop where anyone can look
through it. Since step 2, it already lives on the back room's lectern, open
to anyone who walks through the "Artists only!" door. So I'd leave it there
rather than add a second stand.

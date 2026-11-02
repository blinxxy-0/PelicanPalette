# The museum's painting wing (plan, for approval)

The collection's last reward (restoration_mechanics.md section 5): once all
21 old paintings are donated, the museum opens a wing for them. Today the
donated paintings show only in Gunther's collection view; the wing is
where they actually hang.

## What it is

A new room, its own location, reached by a doorway in the museum. Built
like Margot's rooms: a decoratable shell with straight walls three tiles
tall and a wallpaper of its own (a deep museum red or green, to choose), and
furnished from the collection's own pieces:

- **The 21 paintings on the walls,** grouped by set (the Old Valley, the
  Wreck, the Miners, Calico, the Dwarves), each drawn from the restoration
  you donated, in its set's frame, with a small card under it. Click one
  for its card: title, set, who restored it, and the date.
- **The paint-tube Junimo** in the middle on its plinth, behind a velvet
  rope. **Gallery benches** facing the long wall, and **picture lights**
  over the best of them.
- **Margot** comes on some afternoons (her weekday schedule gains a stop
  there once it's open), and Gunther has a line or two for it.

## When it opens (decided 2026-11-02)

The morning after your **first** donation. Until then the doorway is there
but roped off, with a sign: "Gallery wing: opening soon". Every painting
has its own place on the walls from the start, like a gallery waiting for
its loans: an empty space with a small card ("The Wreck: still missing")
until you donate that one, then the painting itself in its set's frame.
The paint-tube Junimo arrives by letter at 21, as before; the wing has a
plinth waiting for it, which it fills once you've had the letter.

## Where the doorway goes (decided 2026-11-02: B)

`art/museum_wing/doorway_options.png` marks three:

- **A. The exhibit hall's back wall** (recommended): an archway between the
  last window and the palm. The wing sits "behind" the hall, which suits
  a museum. It changes a wall tile or two, and nothing anyone walks on.
- **B. The hall's east wall,** by the reading nook. Also works, but the
  doorway is tucked into a corner that's easy to miss.
- **C. The library's west wall,** by Gunther's counter. Close to Gunther,
  but it's beside his desk and the library's own window.

## Compatibility

Only the doorway's tiles change in the museum (an overlay, as for Margot's
storeroom steps); the wing is its own map. Checked with Stardew Valley
Expanded as for her steps.

## Layout (`art/museum_wing/wing_plan.png`)

Every old painting hangs in the mod's large frame, 3 tiles square, and a
Stardew room shows paintings only on walls that face you (side walls are a
thin border). Twenty-one in a row would need about 84 tiles of wall, so the
wing is a gallery with freestanding walls, as real galleries are:

- **29 tiles wide, 28 deep.** You come in from the museum at the bottom
  left (the hall's east-wall doorway leads into the wing's west side).
- **The back wall:** the Old Valley's five.
- **Three freestanding gallery walls** across the room, each with a
  two-tile aisle at both ends: the Wreck's five, the Miners' five, and
  Calico's four with the Dwarves' two.
- **Benches** facing each wall, and the **paint-tube Junimo** in the open
  space by the door.

The doorway in the museum: an opening in the hall's east wall, two tiles
tall, at the bottom of its east room (x 49, rows 15-16), with the wall's
edge capped above and below like the passage between the library and the
hall.

## Built (2026-11-02)

`tools/build_museum_wing.py` (the map, from the Shed's own tiles, papered
in the saloon red by default, its paper marked impassable so the gallery
walls can only be walked up to), `data/painting_wing.json` (the location,
the doorway's tiles and warps), `Social/PaintingWing.cs` (the rope, the
paintings, cards, benches and Junimo; nothing there can be picked up;
clicking a painting or card reads it), `museart_wingtest` (six checks, in
vanilla and with Stardew Valley Expanded). The doorway opens the morning
after the first donation; the rope is one brass post with its rope looped
to the wall, cut from the velvet rope's art.

Changed from the plan: no empty plinth before the Junimo comes (it simply
appears by the door once its letter has); Margot's afternoon visits and
Gunther's line about the wing are still to do.

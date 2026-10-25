# Art Studio

Paint your own pixel-art paintings at an easel, level up a new Painting skill, and sell or gift what you make. An art supply shop opens in Cindersap Forest, run by Sable, a former museum restorer who can become your partner.

## Requirements

- Stardew Valley 1.6.15 or later
- [SMAPI](https://smapi.io) 4.0 or later
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.9 or later
- [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)
- Optional: [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), to change the settings in game

## Installing

1. Install SMAPI, Content Patcher and SpaceCore.
2. Unzip the download into your `Mods` folder. You get one `ArtStudio` folder holding two mods: `ArtStudio` (the painting) and `[CP] Art Studio` (the shop and Sable). Keep both.
3. Run the game through SMAPI.

You can add it to an existing save. To remove it, sell or trash your paintings, easels and art supplies first, then delete the folder.

## Playing

**Getting started.** On your second day a letter arrives from the new art shop, with two canvases. You already know how to craft an **Easel** (30 wood, 10 fiber), **Small Canvases** (5 fiber, 2 wood) and a **Palette** (10 wood). Sable also gives you a Palette on your first visit to the shop. Place the easel anywhere and use it, with a Palette in your bag.

**Painting.** Pick a canvas and something from your inventory to paint, and its picture appears beside the canvas. Copy it cell by cell:
- **Mixing** works like fishing. Click a pigment and hold to keep your mix on the moving true color until the meter fills. A clean mix paints pure color; a bad one comes out muddy.
- **The light** runs down while you work. When it's gone, the painting is finished as it stands. Finish early and the leftover light becomes bonus experience.
- **Inspiration sparks** sometimes appear on the canvas. Paint that cell the right pure color before the spark fades for more light; catch three and the painting is *Inspired*.
- **Brushes** change how mixing feels and how far a mix goes. They wear out with use, like fishing tackle.

A painting takes part of your day: about 1 hour for a small canvas, 2 for medium and 3 for large. The clock moves on when you step away from the easel, and it won't start a painting that would run past 2am.

**Colours and paint.**
- **Colours are knowledge.** You start knowing eight. Learn more by buying their pigment from Sable, or from the first tube of a new colour you get. You never lose a colour you know.
- **Paint is what you spend.** Your **Palette** holds paint for each colour, up to 300 cells' worth, and every cell you paint uses one. A new Palette comes with the eight starter colours.
- **Refilling:** **paint tubes** (100 cells each) top the Palette up automatically when you sit down at an easel. The setup screen warns you if you're low on a colour the subject needs.
- **Where tubes come from:** Sable sells tubes of every colour you know. A **Pigment Mill** (unlocked at Painting level 1, or bought from Sable) grinds tubes overnight from clay, coal, stone, flowers, berries, minerals and gems. Milling something new can teach you a colour.
- **Relaxed difficulty** paints for free and doesn't need a Palette.

The painting's quality depends on how closely you copied the picture and on your Painting level.

**Portraits.** Once you have medium canvases (Painting level 4), friends at 4 or more hearts will sit for a portrait: pick their face under the items on the easel. A portrait takes an hour longer than a still life and sells for about 75% more. The sitter always loves being given theirs. Friends at 4+ hearts can also post portrait requests on the commission board.

**Plein air.** An easel placed outdoors offers **the view** as a subject: the scene around it (mostly what's in front of it) becomes the reference, across the whole canvas. A view takes an hour longer than a still life and is worth about as much as a portrait. It's worth more somewhere far from the farm (the Woods, the Summit, the Desert, Ginger Island), and more again in rain, in winter or after 7pm. The same place in another season counts as a new subject.

**Your painting record.** Every subject you paint is kept on a record page (ask at Sable's counter). The first painting of a new subject sells for 25% more. Painting the same thing over and over sells for less: up to 30% less after six. Milestones for painting many different subjects:
- 5 subjects: aquamarine paint
- 15: bigger Palette wells (450 cells)
- 30: paintings worth 10% more
- 50: a gift from Sable

**The Painting skill** goes from level 1 to 10. Each level gives you more light, and new levels bring new tools (broad brush, ruler, mirror, dropper), bigger canvases, new pigments and higher quality. At levels 5 and 10 you choose a profession, as with the other skills.

**Selling and gifting.**
- Sell paintings to Pierre, at the art shop (which pays 20% more), or through the shipping bin.
- Or **consign** up to three with Sable, at the counter. They go on the shop's easels and sell within one to three days (sooner the better they are) for half again what a shop pays. You can take one back before it sells.
- Everyone loves an iridium painting, and silver and gold ones are liked. Art lovers (Leah, Elliott, Emily, Haley, Abigail, Sebastian, Sam, Robin and Sable) like any painting. A painting of something a villager loves goes up a step. Nobody dislikes a painting.
- A **Picture Frame** turns a painting into furniture you can hang on a wall.

**The art shop** is in Cindersap Forest, west of the path down from Marnie's ranch. It's open 10am to 6pm (until 5pm on Fridays, from 1pm on Sundays) and closed on Wednesdays. It sells canvases, pigments, paint tubes, palettes, brushes, easels, frames and the Pigment Mill, with more stock as your skill grows. The **commission board** there has villagers asking for paintings of things they love. Take a request, paint it well enough, and hand it to them for gold and friendship.

**Sable** (they/them) runs the shop, and dresses for the weather like everyone else: a coat and scarf outdoors in winter, a linen shirt on Ginger Island. They have a full schedule, festival appearances, heart events at 2, 4, 6, 8, 10 and 14 hearts, and can be dated and married. Once you're married they still keep the shop. When they're home, it runs on trust.

## Settings

Change these with Generic Mod Config Menu, or in `ArtStudio/ArtStudio/config.json` after one run:

| Setting | Default | What it does |
|---|---|---|
| `Difficulty` | `Normal` | `Relaxed`: no light meter, and each color only needs mixing once. `Normal`: mixed colors dry after a while. `Hard`: paint runs out, the light never pauses and sparks fade fast, but paintings pay 20% more. |
| `LightTimeMultiplier` | `1.0` | Gives you more (or less) light per painting, on top of the difficulty (0.5x to 3x). |
| `ArtLoversLikeAnyPainting` | `true` | Whether art lovers like even a normal-quality painting. |

## Compatibility

- Works with **Stardew Valley Expanded**: the shop, Sable's schedules, events and festival spots all fit SVE's maps.
- Other mods that change the stretch of Cindersap Forest west of the path from Marnie's may overlap the shop.
- Not yet tested in multiplayer.

## Translating

All text is in two files: `ArtStudio/i18n/default.json` (the easel, items, skill and shop) and `[CP] Art Studio/i18n/default.json` (Sable's dialogue, events, mail and the shop's lines). Copy each one to `<language>.json` (for example `fr.json`), translate the values, and keep the keys and anything in `{{double braces}}` as they are. In dialogue, keep the codes too: `$h`, `#$b#`, `@` and similar.

## Credits

By blinxxy. Art made with PixelLab and edited by hand to match the game's style.

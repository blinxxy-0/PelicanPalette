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

**Getting started.** On your second day a letter arrives from the new art shop, with two canvases. You already know how to craft an **Easel** (30 wood, 10 fiber) and **Small Canvases** (5 fiber, 2 wood). Place the easel anywhere and use it.

**Painting.** Pick a canvas and something from your inventory to paint, and its picture appears beside the canvas. Copy it cell by cell:
- **Mixing** works like fishing. Click a pigment and hold to keep your mix on the moving true color until the meter fills. A clean mix paints pure color; a bad one comes out muddy.
- **The light** runs down while you work. When it's gone, the painting is finished as it stands. Finish early and the leftover light becomes bonus experience.
- **Inspiration sparks** sometimes appear on the canvas. Paint that cell the right pure color before the spark fades for more light; catch three and the painting is *Inspired*.
- **Brushes** change how mixing feels and how far a mix goes. They wear out with use, like fishing tackle.

The painting's quality depends on how closely you copied the picture and on your Painting level.

**The Painting skill** goes from level 1 to 10. Each level gives you more light, and new levels bring new tools (broad brush, ruler, mirror, dropper), bigger canvases, new pigments and higher quality. At levels 5 and 10 you choose a profession, as with the other skills.

**Selling and gifting.**
- Sell paintings to Pierre, at the art shop (which pays 20% more), or through the shipping bin.
- Everyone loves an iridium painting, and silver and gold ones are liked. Art lovers (Leah, Elliott, Emily, Haley, Abigail, Sebastian, Sam, Robin and Sable) like any painting. A painting of something a villager loves goes up a step. Nobody dislikes a painting.
- A **Picture Frame** turns a painting into furniture you can hang on a wall.

**The art shop** is in Cindersap Forest, west of the path down from Marnie's ranch. It's open 10am to 6pm (until 5pm on Fridays, from 1pm on Sundays) and closed on Wednesdays. It sells canvases, pigments, brushes, easels and frames, with more stock as your skill grows. The **commission board** there has villagers asking for paintings of things they love. Take a request, paint it well enough, and hand it to them for gold and friendship.

**Sable** (they/them) runs the shop. They have a full schedule, festival appearances, heart events at 2, 4, 6, 8, 10 and 14 hearts, and can be dated and married. Once you're married they still keep the shop. When they're home, it runs on trust.

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

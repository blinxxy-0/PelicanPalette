# Nexus Mods page: upload kit

Everything for the Pelican Palette page on Nexus Mods (Stardew Valley).

## Files

- `description.bbcode` is the page's Description. Paste it into the description editor's BBCode mode.
  - `{{name.png}}` marks an image. Upload each image (below) to the mod's Images tab first. Then replace each placeholder with that image's direct link: right-click the image, then "Copy image address".
  - `{{wiki}}` is the wiki's address.
- The release zip is `ArtStudio/bin/Release/net6.0/PelicanPalette 1.0.0.zip`. Build it with `dotnet build ArtStudio/ArtStudio.csproj -c Release -p:EnableModDeploy=false`.
- Images, all in `docs/media/`:
  - `page/`: `title_card.png` (the top of the page), `banner.png` (the same without the title) and the wooden headings (`header_*.png`), for the description.
  - `screens/`: in-game screenshots for the description and the Images tab.

## Page fields

- **Name:** Pelican Palette
- **Summary** (the short "About this mod" line):
  > Paint pixel art at an easel in a fishing-style minigame, level a Painting skill, and sell, gift, frame and varnish your work. With an art shop in Cindersap Forest and Sable, its romanceable owner.
- **Category:** Gameplay Mechanics
- **Version:** 1.0.0
- **Tags:** Gameplay, Crafting, New Characters, Romance, Skills, Content Patcher, SMAPI
- **Requirements:**
  - SMAPI (2400)
  - Content Patcher (1915)
  - SpaceCore (1348)
  - Generic Mod Config Menu (5098), optional
- **Permissions:** your call. A common choice is no reuploads, no asset reuse without asking, and translations allowed with credit (`README.md` explains how to translate).
- **Changelog:** paste `CHANGELOG.md`'s 1.0.0 section into the Changelogs tab.

## Images tab: suggested order and captions

1. `title_card.png`: *Welcome to Pelican Palette* (also the page's main image)
2. `shop_exterior.png`: *The art shop in Cindersap Forest, and Sable*
3. `shop_interior.png`: *Inside: canvases, pigments, paint and the commission board*
4. `mixing.png`: *Mixing a colour on the fishing-style bar*
5. `paint_glaze.png`: *Glazing on a medium canvas*
6. `paint_highlight.png`: *Highlights on a large canvas*
7. `results_masterpiece.png`: *A Masterpiece*
8. `setup_portrait.png`: *Painting a friend's portrait*
9. `setup_view.png`: *Plein air: painting the view*
10. `record_page.png`: *Your painting record and its milestones*
11. `consign_menu.png`: *Consigning paintings at Sable's gallery*
12. `board.png`: *The commission board*
13. `farmhouse_varnish.png`: *A Varnishing Cabinet, curing a painting*
14. `event_gallery_1.png`: *A gallery evening at the shop*

## After the first upload

Add the page's ID to both manifests' `UpdateKeys` (`"UpdateKeys": ["Nexus:<id>"]`), so SMAPI can tell players when there's an update.

# Food Truck Cafe: art style guide

What the game looks like and how new art is made. Written 24 November 2026 from the owner's feedback since October
(the detailed studies are in `Desktop/Sept/Store/brushes/STYLE.md`, outside git). The owner's word wins over this guide.

## 1. Rules that never change

- **The owner's own paintings stay.** AI may only add missing parts or repaint at a new angle with the art kept; paste
  the original pixels back where they exist. **Never regenerate a customer's face**: a face the owner says changed gets
  its original pixels back (`Store/characters`, `boy_headswap.py`).
- **Nothing goes into the game before the owner picks it** (a tick in the report or their words).
- **Three-way drafts for new art**: brief ChatGPT and Gemini, paint my own version, show all side by side in the report;
  the owner often mixes ("I like yours, but..."). Treat the owner's ideas as suggestions during brainstorming and add
  versions of my own.
- **One place per picture** in any sheet (table, kitchen, shop or UI); a shop icon beside kitchen items was once read
  as a kitchen object.
- **Copy the reference, measure it**: when the owner says "same as X", measure X's pixels (size, position, colours,
  curve) and copy the numbers instead of guessing.

## 2. The game's looks, by place

| Place | Look | Reference files |
| --- | --- | --- |
| Backgrounds (day scenes, shop pages, map) | Soft gouache/watercolour on paper, airy light palette, big simple masses framing the top corners, calm open centre and calm lower half | `Assets/Images/bg.png` (ch 1), `Assets/Resources/Chapter2/bg_day.png`, `bg_night.png`, `shop_bg.png`, `Assets/Images/truckbg.png` |
| Table items (food and stations on the cloth) | Textured gouache, thick soft outline in a darker shade of each fill, light from the upper right, **one shadow in the item's own shape, near-black (20,16,24) at 60 % (since 16 Nov 2026; the October navy 45,45,66 is retired)**, camera 40 degrees above the table: round rims are ovals 0.77 as tall as wide, boxes front-on | the owner's juice pitcher, apple basket, cookie plate |
| Kitchen items | Flat calm Procreate gouache, simple rounded shapes, outline 5 to 8 % of the object's size in a darker shade (charcoal only on dark appliances), heavier on the shadow side, light from the upper left, one broad soft shade patch, soft grey-violet contact shadow; front view | `Art/kitchen_layers` (21 layers), `Assets/Images/Kitchen/kitchen_bg_notree.png` |
| Cloths | Painted fabric with soft creases, seen from the 40-degree table camera, with a front drop that bends over the table edge (`Docs/tools/cloth_front.py`) | `Assets/Images/knockout 1.PNG` (ch 1 red gingham), `Resources/Chapter2/cloth.png` (blue stripes) |
| Buttons | **Cookies** (iced cookies, Gemini's look picked 17 Nov 2026) | `Store/cookies_2026-11-17`, start menu `Assets/Images/IMG_0452.png` |
| Panels | **Paper** (receipts, stickers, the finish card, day card, Daily Treats' mint checked card); the only other material is the wooden board | `Resources/DailyTreats/board_paper.png`, `Resources/Map/day_paper.png`, `Assets/Images/board.png` |
| Tap-me glow | Bright at the edge, fading outward, width 4 px + 15 % of the piece's shorter side, cool white; never a Gaussian blur | `Docs/tools/glow_brush.py` |
| Text | Delius (since 19 Nov 2026); black titles and money with a cream rim, brown hints | `Assets/TextMesh Pro/Fonts/Delius` |

What the owner has rejected: pencil-thin outlines (mine at 2 % read as pencil), glossy 3D shading, glowing light
effects, detailed "AI Christmas illustration" decoration, soft grey blob shadows on the table, wiggly edges.

## 3. Screen geometry (check every mock against it)

- Game scene on a phone (e.g. 1320 x 2868): the 1080 x 1920 background is scaled to the screen **height** and centre-
  cropped, so only the middle ~82 % of its width shows (x about 98..982); keep important content inside x 110..970.
- The table: camera half-height 5 world units; the cloth is 4.6 x 5 world from the screen's vertical middle down; its
  drop starts 0.456 world above the cloth's bottom (the cloth image's EDGE row, 1666 of 1834 at the chapter 2 size).
  Mocks place cloth and drop exactly so (`Store/cchapter_2026-11-24c/cloth/gingham_textures.py` `preview()`).
- Customers stand in the band just above the cloth; their size in play: see any real capture
  (`Store/table_bounds_2026-11-23/c2_capture_fixed.png`).
- Shop page: the same grid for every chapter (chapter 1 and 2 put the red truck at x ~512..1032 of 1080 with its tyres
  near y 1062, framing in the top corners, a calm lower half for the shop cards). Show a new page side by side with
  chapters 1 and 2 (`Store/cchapter_2026-11-24c/shop/side.py`).
- Every scene needs a **day and a night** version, pixel-aligned (DayNightTint fades `bg_day` into `bg_night`).

## 4. Working with ChatGPT and Gemini

- **Brief shape**: "Please create an image." Image 1 = the shape (the game's art or my draft), image 2 = the style or
  camera reference: the owner's own paintings (kitchen layers, the cookie plate, `Store/redesign/ai/ref_camera_plate.png`),
  never the polished AI items. Say what each image is, then "repaint exactly as it is ... in the game's style", the
  grid of states, light and shadow, "plain white background, wide white gaps, no words".
- **Style paragraphs** for table and kitchen items, and words that help or hurt: `Store/brushes/STYLE.md` section
  "What to tell ChatGPT". Hurt: "watercolour" alone, "realistic", "3D", "isometric", "cartoon", any text in an image.
- **ChatGPT** keeps a given shape; free plan about three pictures a day (it says when it resets). Typing right after an
  upload loses the text: wait, click the box, type. Pull a picture: `fetch(img.src, {credentials:'omit'})` → canvas →
  blob, a one-time click listener calls `navigator.clipboard.write`, one real click, then osascript
  `the clipboard as «class PNGf»` into a file.
- **Gemini** keeps a style but redraws shapes; paints any words it sees. Paste images with osascript
  `set the clipboard to (read POSIX file ... as «class PNGf»)` + cmd+v; pull with the picture's own "Copy image" button
  (hover the picture first) + osascript.
- For indoor/decor style the best reference is the kitchen background (Gemini restyled ChatGPT's cabin room toward it
  well, 24 Nov 2026).

## 5. My own painting

- Python: `Desktop/Sept/Store/artkit/venv/bin/python -I` (numpy, scipy, pillow, opencv, psd tools).
- Brush kit: `Store/artkit/brush` (26 brushes; `kitchen_style.Pic`: flat gouache fills with grain, a darker soft line,
  one soft shade; `ks2.py` painter). Recent scene painters: `Store/cchapter_2026-11-24b/claude/lib.py`,
  `Store/cchapter_2026-11-24c/claude/room.py`, `.../shop/shop*.py`, cloths `.../cloth/gingham_textures.py`.
- Sprites: hard clean cut-out edge, no halo; one object per PNG.

## 6. Hand-off to the owner

iCloud Drive `FoodTruckCafe art/`: ONE flat folder, one object per PNG, named `<group>_<object>.png`, table items at 40
degrees, no docs, PSDs or sheets. The owner's own `Updates/` subfolder is their drop place (check it at the start of an
art batch; never delete owner folders). Bug screenshots arrive in `FoodTruckCafe art/bugs`.

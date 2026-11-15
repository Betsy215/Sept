"""Draws the boxes of table_boxes.py on its screenshot. Usage: table_boxes_draw.py <tag> [<tag> ...] -> boxes_<tag>_drawn.jpg and boxes_sheet.jpg"""
import sys, json
from PIL import Image, ImageDraw, ImageFont
P = "/tmp/ftc/popframes/"; cells = []
try: font = ImageFont.truetype('/System/Library/Fonts/Supplemental/Arial Bold.ttf', 30)
except Exception: font = ImageFont.load_default()
for tag in sys.argv[1:]:
    d = json.load(open(P + "boxes_%s.json" % tag)); im = Image.open(P + "boxes_%s.png" % tag).convert("RGB"); W, H = im.size; orth, asp = d["cam"]; hw = orth * asp
    X = lambda x: (x + hw) / (2 * hw) * W; Y = lambda y: (orth - y) / (2 * orth) * H; dr = ImageDraw.Draw(im, "RGBA")
    a = d["area"]; dr.rectangle([X(a[0]), Y(a[3]), X(a[2]), Y(a[1])], outline=(255, 214, 0, 255), width=6)
    for name, x0, y0, x1, y1 in d["foods"]:
        col = (230, 30, 30, 255) if name == "IceCream" else (40, 110, 255, 255); dr.rectangle([X(x0), Y(y1), X(x1), Y(y0)], outline=col, width=5)
    for name, lo in d.get("fixed", []): dr.line([(0, Y(lo)), (W, Y(lo))], fill=(255, 255, 255, 230), width=3)
    cells.append(im.crop((0, int(H * 0.44), W, H)))
    im.save(P + "boxes_%s_drawn.jpg" % tag, quality=88)
sh = Image.new("RGB", (sum(c.width for c in cells) + 30 * (len(cells) - 1), cells[0].height), "white"); x = 0
for c in cells: sh.paste(c, (x, 0)); x += c.width + 30
sh = sh.resize((sh.width // 2, sh.height // 2), Image.LANCZOS); sh.save(P + "boxes_sheet.jpg", quality=88); print(sh.size)

"""Put a caption band on a raw capture and export at the App Store size.
Usage: python3 caption.py <raw.png> <out.png> "<caption>" <W> <H>
The raw capture is scaled to fill WxH (same aspect expected); the caption sits on a cream band at the bottom."""
import sys
from PIL import Image, ImageDraw, ImageFont
src, out, text, W, H = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
FONT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/TextMesh Pro/Fonts/beachday.otf"
im = Image.open(src).convert("RGB")
k = max(W / im.width, H / im.height)
im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
x0 = (im.width - W) // 2; y0 = (im.height - H) // 2
canvas = im.crop((x0, y0, x0 + W, y0 + H))
band_h = int(H * 0.125)
d = ImageDraw.Draw(canvas, "RGBA")
d.rectangle((0, H - band_h, W, H), fill=(255, 247, 225, 240))
d.rectangle((0, H - band_h - 10, W, H - band_h), fill=(120, 80, 45, 255))
size = int(band_h * 0.5); f = ImageFont.truetype(FONT, size)
while d.textlength(text, font=f) > W * 0.92 and size > 20:
    size -= 4; f = ImageFont.truetype(FONT, size)
tw = d.textlength(text, font=f); ty = H - band_h / 2 - size * 0.55
d.text(((W - tw) / 2 + 5, ty + 5), text, font=f, fill=(0, 0, 0, 60))
d.text(((W - tw) / 2, ty), text, font=f, fill=(80, 50, 25, 255))
canvas.save(out, "PNG")
print(out, canvas.size, "font", size)

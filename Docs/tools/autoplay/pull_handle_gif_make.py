"""Builds the two bounce films from pull_handle_gif.py's frames: <out>/handle_bounce_table.gif and handle_bounce_kitchen.gif,
plus a still sheet of both. Usage: pull_handle_gif_make.py <outdir>"""
import sys, glob
from PIL import Image
P = "/tmp/ftc/popframes/"; out = sys.argv[1].rstrip("/") + "/"; stills = []
for tag, x0, x1 in (("table", 0.0, 0.62), ("kitchen", 0.38, 1.0)):
    fs = sorted(glob.glob(P + "phg_%s_*.png" % tag)); frames = []
    for f in fs:
        im = Image.open(f).convert("RGB"); W, H = im.size
        c = im.crop((int(W * x0), int(H * 0.36), int(W * x1), int(H * 0.54))); frames.append(c.resize((560, round(560 * c.height / c.width)), Image.LANCZOS))
    seq = [frames[0]] * 1 + frames + [frames[-1]]
    dur = [900] + [33] * len(frames) + [700]
    pal = [f.quantize(colors=192, method=Image.MEDIANCUT, dither=Image.FLOYDSTEINBERG) for f in seq]
    pal[0].save(out + "handle_bounce_%s.gif" % tag, save_all=True, append_images=pal[1:], duration=dur, loop=0, optimize=False)
    stills.append((frames[0], frames[2]))
    print(tag, len(frames), "frames")
w, h = stills[0][0].size; sh = Image.new("RGB", (w * 2 + 16, h * 2 + 16), "white")
for j, (a, b) in enumerate(stills): sh.paste(a, (0, j * (h + 16))); sh.paste(b, (w + 16, j * (h + 16)))
sh.save(P + "phg_stills.jpg", quality=90)

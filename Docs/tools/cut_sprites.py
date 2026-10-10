"""Cut a ChatGPT two-pose sheet (figures on white, stacked or side by side) into neutral/happy sprites, without a white rim.
Usage (needs numpy, scipy, pillow): python3 Docs/tools/cut_sprites.py <sheet.png> <neutral_out.png> <happy_out.png> [match_neutral.png match_happy.png]
1. Background = white flood from the border, plus near-white pixels within RIM px of it (catches the
   white pockets between hair wisps the flood cannot reach; interior whites such as eyes are too far in).
2. Edge band: each pixel within RIM px of the background is un-mixed from white, alpha = how far its
   colour is from white relative to the nearest solid interior colour, and its colour set to that solid
   colour, so semi-transparent wisps carry hair colour, not white.
3. If match files are given, the result is scaled to their height and centred on their canvas size,
   so the sprite drops into Unity with the same size and pivot."""
import sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

import os
RIM = int(os.environ.get("RIM", 10))   # near-white pockets this close to the background are background;
# raise it (RIM=30) for characters with no white in their design, to clear white trapped between arm and body or deep in hair
BAND = 3   # edge band that gets un-mixed from white
src = Image.open(sys.argv[1]).convert("RGB")
W, H = src.size
flood = src.copy(); sent = (255, 0, 255)
for x in range(0, W, 8):
    for y in (0, H - 1):
        if flood.getpixel((x, y)) != sent: ImageDraw.floodfill(flood, (x, y), sent, thresh=40)
for y in range(0, H, 8):
    for x in (0, W - 1):
        if flood.getpixel((x, y)) != sent: ImageDraw.floodfill(flood, (x, y), sent, thresh=40)
rgb = np.asarray(src).astype(np.float32)
bg = np.all(np.asarray(flood) == sent, axis=2)
dist_bg = ndimage.distance_transform_edt(~bg)
lum_min = rgb.min(axis=2); sat = rgb.max(axis=2) - lum_min
WHITE = int(os.environ.get("WHITE", 222))  # how light a pixel must be to count as leftover background
nearwhite = (lum_min > WHITE) & (sat < 26)
bg |= nearwhite & (dist_bg <= RIM)
if os.environ.get("HOLES"):  # HOLES=1: enclosed pure-white pockets (e.g. between arm and body) are background too
    pure = (lum_min > 240) & (sat < 14) & ~bg
    lab, n = ndimage.label(pure)
    sizes = ndimage.sum(pure, lab, range(1, n + 1))
    bg |= np.isin(lab, [i + 1 for i, s in enumerate(sizes) if s >= int(os.environ.get("HOLE_MIN", 150))])
# Tiny enclosed specks (texture strokes, not real gaps) go back to the figure
lab, n = ndimage.label(bg)
sizes = ndimage.sum(bg, lab, range(1, n + 1))
border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
small = [i + 1 for i, s in enumerate(sizes) if s < int(os.environ.get("HOLE_MIN", 150)) and (i + 1) not in border]
bg &= ~np.isin(lab, small)
dist_bg = ndimage.distance_transform_edt(~bg)
solid = (~bg) & (dist_bg > BAND)
# local solid colour: normalised blur of the solid pixels (smooth, no streaks)
w = ndimage.gaussian_filter(solid.astype(np.float32), 6)
fg = np.dstack([ndimage.gaussian_filter(rgb[..., c] * solid, 6) for c in range(3)]) / np.maximum(w, 1e-3)[..., None]
band = (~bg) & (~solid)
num = (255.0 - rgb).sum(axis=2); den = np.maximum((255.0 - fg).sum(axis=2), 1.0)
a_band = np.clip(num / den, 0, 1)
alpha = np.where(bg, 0.0, np.where(band, a_band, 1.0))
# un-mix from white: keep the pixel's own texture, just remove the white it was blended with
a3 = np.maximum(alpha, 0.05)[..., None]
own = np.clip((rgb - (1 - a3) * 255.0) / a3, 0, 255)
mixw = np.clip(alpha * 2, 0, 1)[..., None]   # very faint pixels lean on the local solid colour
col = np.where(band[..., None], own * mixw + fg * (1 - mixw), rgb)
# tidy: drop isolated faint specks
alpha[alpha < 0.08] = 0
out = Image.fromarray(np.dstack([col, alpha * 255]).astype(np.uint8))

def split(im):
    """Two figures stacked or side by side: cut along the widest empty band, rows or columns."""
    a = np.asarray(im)[..., 3] > 0
    best = None
    for axis in (1, 0):  # axis 1: rows (stacked), axis 0: columns (side by side)
        idx = np.where(a.any(axis=axis))[0]
        g = np.diff(idx); k = g.argmax()
        if best is None or g[k] > best[0]: best = (g[k], axis, idx[k], idx[k + 1])
    _, axis, e0, e1 = best
    if axis == 1: first, second = im.crop((0, 0, W, e0 + 1)), im.crop((0, e1, W, H))
    else: first, second = im.crop((0, 0, e0 + 1, H)), im.crop((e1, 0, W, H))
    return first.crop(first.getbbox()), second.crop(second.getbbox())

figs = [out.crop(out.getbbox())] if os.environ.get("SINGLE") else split(out)  # SINGLE=1: one figure per image
outs = sys.argv[2:4]; matches = sys.argv[4:6]
for i, fig in enumerate(figs):
    if matches:
        ref = Image.open(matches[i]); rw, rh = ref.size
        k = (rh - 8) / fig.height
        f = fig.resize((round(fig.width * k), rh - 8), Image.LANCZOS)
        canvas = Image.new("RGBA", (rw, rh), (0, 0, 0, 0))
        canvas.alpha_composite(f, ((rw - f.width) // 2, 4))
    else:
        canvas = Image.new("RGBA", (fig.width + 8, fig.height + 8), (0, 0, 0, 0)); canvas.alpha_composite(fig, (4, 4))
    canvas.save(outs[i]); print(outs[i], canvas.size)

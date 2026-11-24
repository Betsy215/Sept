"""The tablecloth's front drop (owner, 23 November 2026: "some of the tab has a fall on the bottom, it shows another
surface of the table cloth, I want the bottom safe area to show that ... make sure the style matches the table cloths").
Built from each cloth's own pixels, so colour, paper texture and creases match:
  - the columns hang straight down from where they meet the table's front edge (the perspective slant is undone);
  - the rows are no longer foreshortened: a check on the front is square (the top shows it squashed by the 40-degree view);
  - a soft rounded highlight on the fold over the table edge, the face a little darker, darker still lower down, and a
    few soft drape folds that grow from the edge.
The art is as wide as its cloth and is drawn at the same scale in x and y; the game puts its top on the table's front
edge and lets it run past the bottom of the screen.   ../artkit/venv/bin/python -I front.py"""
import numpy as np
from PIL import Image
G = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/"
EDGE = 0.456 / 5.0          # the front edge on a Face ID iPhone: 34 pt home bar + 10 pt gap = 0.456 world of the cloth's 5
DEPTH = 1.3                  # world units of drop drawn (more than any screen shows)
def build(src, out, row_period, white_row_mode):
    a = np.asarray(Image.open(src).convert("RGB")).astype(np.float32); H, W, _ = a.shape
    ppw = W / 4.6                                                   # art pixels per world unit (phone width 4.6)
    phase = int(round(H * (1 - EDGE)))                              # the cloth row at the edge
    fh = int(round(DEPTH * ppw)); xs = np.arange(W, dtype=np.float32); cx = (W - 1) / 2
    # perspective: the pattern's width scale grows linearly down the cloth (measured: column period 167.6 at row H/2, 234.6 near the bottom)
    def scale(r): return 167.6 + (234.6 - 167.6) * (r - H * 0.5) / (H * 0.91 - H * 0.5)
    out_px = np.zeros((fh, W, 3), np.float32)
    col_period = scale(phase) * (W / 1173.0)                        # px; a square check on the front
    for y in range(fh):
        if white_row_mode:                                          # gingham: cycle one row period of the top, stretched to a square
            k = (y * row_period / col_period) % row_period
            ys = phase - row_period + 1 + k                         # rows just above the edge, continuing down from it
            ys = min(max(ys, 0), H - 1)
        else:                                                       # stripes: rows near the edge, slowly, mirrored so nothing repeats in a seam
            t = (y * 0.45) % (2 * (H - 1 - phase)); t = t if t <= H - 1 - phase else 2 * (H - 1 - phase) - t
            ys = phase + t
        r0 = int(ys); fr = ys - r0; r1 = min(r0 + 1, H - 1)
        sx = cx + (xs - cx) * scale(ys) / scale(phase)              # undo the slant: the column at the edge's x hangs straight
        sx = np.clip(sx, 0, W - 1); x0 = sx.astype(int); x1 = np.minimum(x0 + 1, W - 1); fx = (sx - x0)[:, None]
        row = lambda r: a[r, x0] * (1 - fx) + a[r, x1] * fx
        out_px[y] = row(r0) * (1 - fr) + row(r1) * fr
    yy = np.arange(fh, dtype=np.float32)[:, None]; X = xs[None, :]
    wy = yy / ppw                                                   # world units below the edge
    shade = 0.80 - 0.12 * np.clip(wy / 0.6, 0, 1) ** 0.8            # the face turned from the light, darker lower down
    grow = np.clip(wy / 0.22, 0, 1) ** 1.2                          # folds start a little below the edge
    rng = np.random.default_rng(7); fold = np.zeros_like(X)
    for lam, amp in ((0.55, 0.050), (0.83, 0.040), (1.37, 0.035)):
        lam_px = lam * ppw; ph = rng.uniform(0, 2 * np.pi); fold = fold + amp * np.sin(2 * np.pi * X / lam_px + ph + 0.35 * np.sin(2 * np.pi * X / (lam_px * 2.7)))
    shade = shade * (1 + fold * grow)
    img = out_px * shade[..., None]
    # the fold over the table edge: a soft rounded highlight, brightest just under the edge, and a hair of shade at the very top
    rim = 0.045 * ppw; peak = 0.30 * rim
    roll = 1 - 0.13 * np.exp(-((yy - rim * 1.5) / (rim * 0.9)) ** 2)   # the cloth turning down past the edge: a soft shade under the light
    img = img * roll[..., None]
    hl = np.exp(-((yy - peak) / (rim * 0.6)) ** 2) * 0.38
    img = img + (255 - img) * hl[..., None]
    top = np.exp(-(yy / (0.006 * ppw + 0.5)) ** 2) * 0.10; img = img * (1 - top[..., None])
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(out)
    print(out, W, "x", fh, "phase row", phase, "square period px", round(col_period, 1))
build(G + "Images/knockout 1.PNG", G + "Resources/Table/cloth_front.png", 102.0, True)
build(G + "Resources/Chapter2/cloth.png", G + "Resources/Chapter2/cloth_front.png", 0, False)

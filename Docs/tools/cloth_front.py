"""The tablecloth's front drop, v2 (owner, 23 November 2026: "the drop size looks good but the connection doesn't look
natural"). The cloth now bends over the table's edge instead of meeting a line:
  - the fabric is one surface: the drop starts with exactly the cloth row at the edge and goes on down the same cloth
    (repeating its last full check row), so no check or stripe is cut at the edge;
  - over a short rounded band the squashed rows of the top (seen at 40 degrees) open up to the square rows of the front;
  - the light changes gradually: the top's brightness, a broad soft shine on the bend, then the darker front, with soft
    drape folds lower down; no hard light or dark line;
  - columns and stripes hang straight down from where they meet the edge (the top's perspective slant undone).
The game puts the drop at the same height on every device (EDGE of the cloth), so the pattern always joins.
   ../artkit/venv/bin/python -I front.py"""
import numpy as np
from PIL import Image
G = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/"
EDGE = 0.456 / 5.0          # the front edge: 0.456 world of the cloth's 5 (a Face ID iPhone's home bar + gap); TableLayer.frontHeight
DEPTH = 1.3                  # world units of drop drawn (more than any screen shows)
ROLL = 0.105                 # world units of the bend over the edge (v4, between v2 'too obvious' and v3 'too weak')
def smooth(e0, e1, x): t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)
def build(src, out, p_mid, p_low, row_period):
    """p_mid, p_low: the cloth's column period at rows H/2 and 0.91 H (its perspective); row_period: its check row period
    near the bottom, 0 for stripes"""
    a = np.asarray(Image.open(src).convert("RGB")).astype(np.float32); H, W, _ = a.shape
    ppw = W / 4.6; phase = H * (1 - EDGE)
    def scale(r): return p_mid + (p_low - p_mid) * (r - H * 0.5) / (H * 0.91 - H * 0.5)
    sq = scale(phase)                                               # a check's width at the edge = its height on the front
    fh = int(round(DEPTH * ppw)); xs = np.arange(W, dtype=np.float32); cx = (W - 1) / 2
    # the top's squash as the game shows it: a cloth row of r image px is r * (5/H) world tall, a column of c px is c * (4.6/W) wide
    squash = (5.0 / H) / (4.6 / W)                                  # world height per image row / world width per image column
    roll_px = ROLL * ppw
    # fabric travelled per art row: on the top 1/squash image rows per art px, on the front row_period/sq (square checks)
    rate_top = 1.0 / squash
    rate_front = (row_period / sq) if row_period else 0.45          # stripes have no rows: a slow walk down for texture
    ys_list = []; s = phase
    for y in range(fh):
        k = smooth(0.0, 1.0, y / roll_px)
        ys_list.append(s); s += rate_top * (1 - k) + rate_front * k
    out_px = np.zeros((fh, W, 3), np.float32)
    span = row_period if row_period else (H - 1 - phase) * 2
    for y, ys in enumerate(ys_list):
        if ys > H - 1:                                              # past the cloth's last row: the last full row period again
            if row_period: ys = ys - row_period * np.ceil((ys - (H - 1)) / row_period)
            else:
                t = (ys - phase) % span; ys = phase + (t if t <= span / 2 else span - t)
        r0 = int(ys); fr = ys - r0; r1 = min(r0 + 1, H - 1)
        sx = np.clip(cx + (xs - cx) * scale(ys) / scale(phase), 0, W - 1)
        x0 = sx.astype(int); x1 = np.minimum(x0 + 1, W - 1); fx = (sx - x0)[:, None]
        row = lambda r: a[r, x0] * (1 - fx) + a[r, x1] * fx
        out_px[y] = row(r0) * (1 - fr) + row(r1) * fr
    yy = np.arange(fh, dtype=np.float32)[:, None]; X = xs[None, :]; u = yy / roll_px; wy = yy / ppw
    dark = 0.145 * smooth(0.3, 2.0, u) + 0.085 * np.clip((wy - ROLL) / 0.65, 0, 1) ** 0.8     # the front turned from the light
    rng = np.random.default_rng(7); fold = np.zeros_like(X)
    for lam, amp in ((0.55, 0.04), (0.83, 0.031), (1.37, 0.027)):
        lam_px = lam * ppw; ph = rng.uniform(0, 2 * np.pi); fold = fold + amp * np.sin(2 * np.pi * X / lam_px + ph + 0.35 * np.sin(2 * np.pi * X / (lam_px * 2.7)))
    m = (1 - dark) * (1 + fold * smooth(1.2, 3.5, u))
    img = out_px * m[..., None]
    shine = 0.10 * np.exp(-((u - 0.43) / 0.38) ** 2)                # the bend catching the light, soft
    img = img + (255 - img) * shine[..., None]
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).save(out)
    print(out, W, "x", fh, "edge row", round(phase, 1), "check", round(sq, 1), "roll px", round(roll_px, 1))
build(G + "Images/knockout 1.PNG", G + "Resources/Table/cloth_front.png", 167.6, 234.6, 102.0)
build(G + "Resources/Chapter2/cloth.png", G + "Resources/Chapter2/cloth_front.png", 469.2, 586.5, 0)

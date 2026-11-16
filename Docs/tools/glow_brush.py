"""The kitchen's 'tap me' glow as a reusable brush (owner, 15 November 2026: the glow on the coffee bags and on the
tree's fruit "look more like a glowing brush drawing outline of the object", mine on the blender "looks like it's
blurring out towards the edge"; "analyse the glow of coffee bags and fruits, and generate the brush so we can reuse").

What the two painted glows are (measured with glow_profile.py, picture pixels from the object's solid edge):
  coffee bags  Assets/Images/Kitchen/IMG_4475.png   alpha 222 at 1 px, 131 at 3, 57 at 10, 28 at 15, 11 at 20, 0 by 34
  tree apples  Assets/Images/Kitchen/IMG_4467.png   alpha 223 at 1 px, 105 at 3, 27 at 7, 12 at 9, 0 by 15
  colour: the same cool white (239, 247, 251) in both, from the first pixel out.
  - The band is brightest AT the edge and the same width all round the shape, like a soft brush run along the
    outline. A blur of the shape (what the blender had) is half strength at the edge and swells into corners.
  - The width follows the size of each piece: 4 px + 15.1 % of the piece's shorter side (bags, one piece 172 px
    tall: 30 px; each apple, 66 px: 14 px). Both curves have the same shape once divided by that width.
The blender's old glow was the opposite: almost nothing for 4 px, its peak (180) 6 px out, and a long tail
(24 at 35 px): a halo floating off the object.

  <python with numpy, scipy, PIL> glow_brush.py <item png> <out png> [--pad 40] [--width 1.0] [--strength 1.0] [--baked]
--width / --strength multiply the measured brush (1 = the bags and apples). The default output is the glow alone on
the item's canvas grown by --pad on every side (for a child sprite under the item, as the blender's "Glow");
--baked puts the item on top, the way the bags' and apples' pictures are painted.
The art venv has the libraries: /Users/betzthebest/Desktop/Sept/Store/artkit/venv/bin/python -I glow_brush.py ..."""
import argparse
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

COLOUR = (239, 247, 251)
WIDTH_BASE, WIDTH_PER_SIDE = 4.0, 0.151   # brush width = base + per_side * the piece's shorter side (fits bags and apples)
# the coffee bags' measured alpha, by distance / brush width (their width is 30 px); 0 = the edge itself
CURVE_T = np.array([0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 32, 34]) / 30.0
CURVE_A = np.array([215, 158, 131, 114, 102, 91, 82, 73, 64, 57, 50, 43, 37, 32, 28, 23, 20, 16, 13.5, 11, 9, 7.4, 6, 5, 3.9, 3.1, 2.5, 1.9, 1.5, 0.8, 0.4, 0])


def glow_alpha(mask, width_mul=1.0, strength=1.0, solid=None):
    """mask: True where the object is. Returns the glow's alpha (0..255 float) for every pixel outside it."""
    joined = ndi.binary_closing(mask, iterations=3)       # a stem or a gap of a few pixels stays one piece
    labels, n = ndi.label(joined)
    out = np.zeros(mask.shape, float)
    for i, sl in enumerate(ndi.find_objects(labels), 1):
        piece = labels == i
        h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
        if piece.sum() < 30: continue
        width = (WIDTH_BASE + WIDTH_PER_SIDE * min(h, w)) * width_mul
        d = ndi.distance_transform_edt(~(piece & mask))
        out = np.maximum(out, np.interp(d / width, CURVE_T, CURVE_A, right=0) * strength)
    out[mask] = 0                                          # nothing inside: a glass blender shows through
    return np.clip(out, 0, 255)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src"); ap.add_argument("out")
    ap.add_argument("--pad", type=int, default=40); ap.add_argument("--width", type=float, default=1.0)
    ap.add_argument("--strength", type=float, default=1.0); ap.add_argument("--baked", action="store_true")
    a = ap.parse_args()
    im = Image.open(a.src).convert("RGBA"); P = a.pad
    big = Image.new("RGBA", (im.width + 2 * P, im.height + 2 * P)); big.paste(im, (P, P))
    # the body is the near-solid part with its holes filled: the brush also runs under a soft, half-clear edge (the
    # blender's fades over 5 px; skipping it left a dim gap between the blender and its glow)
    mask = ndi.binary_fill_holes(np.asarray(big)[..., 3] >= 200)
    alpha = glow_alpha(mask, a.width, a.strength)
    g = np.zeros(alpha.shape + (4,), np.uint8); g[..., :3] = COLOUR; g[..., 3] = alpha.round().astype(np.uint8)
    out = Image.fromarray(g)
    if a.baked: out.alpha_composite(big)
    out.save(a.out)
    print("glow", out.size, "pad", P, "edge alpha", int(alpha.max()))


if __name__ == "__main__":
    main()

"""Food Truck Cafe preview cuts, v2: in-app footage only, with cute caption pills, push-in zooms,
cuts, crossfades, dip-to-cream, iris and slide transitions, music + in-game sound effects.
Usage: python3 cut2.py appstore | extended
Frames come from video/frames/<section>/fNNNNN.png (886x1920, 30 fps)."""
import os, sys, math, subprocess, functools
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = "/Users/betzthebest/Desktop/Sept/Store/video"
AUDIO = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Audio"
FONT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/TextMesh Pro/Fonts/beachday.otf"
W, H, FPS = 886, 1920, 30
CREAM = (255, 248, 230)

# ---------- caption pills ----------
PALETTE = [  # fill, border/text
    ((255, 250, 236), (124, 80, 46)),    # cream / brown
    ((255, 221, 229), (176, 64, 96)),    # pink / rose
    ((224, 244, 230), (46, 120, 92)),    # mint / green
    ((255, 240, 196), (160, 96, 24)),    # butter / amber
    ((226, 234, 255), (70, 84, 150)),    # sky / navy (night)
]

@functools.lru_cache(maxsize=64)
def pill(text, size, style, sub=None):
    fill, ink = PALETTE[style % len(PALETTE)]
    f = ImageFont.truetype(FONT, size)
    d0 = ImageDraw.Draw(Image.new("RGBA", (10, 10)))
    tw = d0.textlength(text, font=f); th = size * 1.05
    sw = 0
    if sub:
        fs = ImageFont.truetype(FONT, int(size * 0.55)); sw = d0.textlength(sub, font=fs); th += size * 0.7
    padx, pady = int(size * 0.75), int(size * 0.45)
    pw, ph = int(max(tw, sw) + padx * 2), int(th + pady * 2)
    im = Image.new("RGBA", (pw + 60, ph + 60), (0, 0, 0, 0))
    # shadow
    sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).rounded_rectangle([30, 40, 30 + pw, 40 + ph], radius=ph // 2, fill=(60, 30, 10, 90))
    sh = sh.filter(ImageFilter.GaussianBlur(10)); im.alpha_composite(sh)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([30, 30, 30 + pw, 30 + ph], radius=ph // 2, fill=fill + (255,), outline=ink + (255,), width=6)
    # tiny sparkles on both ends
    for sx in (30 + 14, 30 + pw - 14):
        for r, dy in ((9, -10), (5, 14)):
            cx, cy = sx, 30 + ph // 2 + dy
            d.polygon([(cx, cy - r), (cx + r * 0.35, cy - r * 0.35), (cx + r, cy), (cx + r * 0.35, cy + r * 0.35),
                       (cx, cy + r), (cx - r * 0.35, cy + r * 0.35), (cx - r, cy), (cx - r * 0.35, cy - r * 0.35)], fill=ink + (200,))
    y = 30 + pady - size * 0.08
    d.text((30 + (pw - tw) / 2, y), text, font=f, fill=ink + (255,))
    if sub:
        d.text((30 + (pw - sw) / 2, y + size * 1.05), sub, font=fs, fill=ink + (230,))
    return im

def ease_out_back(p, s=1.6):
    p -= 1; return 1 + p * p * ((s + 1) * p + s)

def draw_caption(frame, cap, t):
    """cap: dict(text, t0, t1, y=0.455, size=60, style=0, sub=None, tilt=-2). t in seconds inside the shot."""
    t0, t1 = cap["t0"], cap["t1"]
    if t < t0 or t > t1: return
    base = pill(cap["text"], cap.get("size", 60), cap.get("style", 0), cap.get("sub"))
    a = min(1.0, (t - t0) / 0.33)                     # pop in over 10 frames
    scale = 0.5 + 0.5 * ease_out_back(a) if a < 1 else 1.0
    alpha = min(1.0, (t1 - t) / 0.27)                 # fade out over 8 frames
    alpha = min(alpha, min(1.0, (t - t0) / 0.1))
    bob = math.sin((t - t0) * 2.2) * 5
    im = base.rotate(cap.get("tilt", -2), resample=Image.BICUBIC, expand=True)
    if scale != 1.0: im = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.BILINEAR)
    if alpha < 1:
        r, g, b, al = im.split(); al = al.point(lambda v: int(v * alpha)); im = Image.merge("RGBA", (r, g, b, al))
    cx = int(W * cap.get("x", 0.5)); cy = int(H * cap.get("y", 0.455) + bob)
    frame.alpha_composite(im, (cx - im.width // 2, cy - im.height // 2))

# ---------- shots ----------
@functools.lru_cache(maxsize=48)
def load(src, f):
    return Image.open(f"{ROOT}/frames/{src}/f{f:05d}.png").convert("RGB")

def ease(p): return p * p * (3 - 2 * p)

class Shot:
    def __init__(self, src, f0, f1, step=1, zoom=None, caps=(), trans="xfade", tdur=10, hold=0, name=""):
        self.src, self.f0, self.f1, self.step = src, f0, f1, step
        self.zoom, self.caps, self.trans, self.tdur, self.hold = zoom, list(caps), trans, (0 if trans == "cut" else tdur), hold
        self.n = (f1 - f0) // step + hold
        self.name = name or src
    def frame(self, i):
        src_i = self.f0 + min(i, self.n - self.hold - 1) * self.step
        im = load(self.src, src_i)
        if self.zoom:
            cx, cy, z0, z1 = self.zoom
            z = z0 + (z1 - z0) * ease(i / max(1, self.n - 1))
            cw, ch = W / z, H / z
            x0 = min(max(cx * W - cw * cx, 0), W - cw); y0 = min(max(cy * H - ch * cy, 0), H - ch)
            im = im.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize((W, H), Image.LANCZOS)
        im = im.convert("RGBA")
        for c in self.caps: draw_caption(im, c, i / FPS)
        return im

def blend(trans, a, b, p):
    """a = outgoing, b = incoming, p in 0..1"""
    if trans == "xfade": return Image.blend(a, b, ease(p))
    if trans == "dip":
        cream = Image.new("RGBA", (W, H), CREAM + (255,))
        return Image.blend(a, cream, ease(min(1, p * 2))) if p < 0.5 else Image.blend(cream, b, ease((p - 0.5) * 2))
    if trans == "iris":
        r = int(ease(p) * math.hypot(W, H) * 0.6)
        m = Image.new("L", (W, H), 0); ImageDraw.Draw(m).ellipse([W / 2 - r, H / 2 - r, W / 2 + r, H / 2 + r], fill=255)
        m = m.filter(ImageFilter.GaussianBlur(6))
        return Image.composite(b, a, m)
    if trans == "slide":
        out = a.copy(); out.alpha_composite(b, (0, int(H * (1 - ease(p))))); return out
    return b

def render(shots, out_path, sfx, fade_in=12, fade_out=18):
    starts, g = [], 0
    for i, s in enumerate(shots):
        if i: g -= s.tdur
        starts.append(g); g += s.n
    total = g
    print(f"{out_path}: {total} frames = {total / FPS:.1f} s")
    for s, st in zip(shots, starts): print(f"  {st / FPS:6.2f}s  {s.name:28s} {s.n / FPS:5.2f}s  in:{s.trans}")
    silent = out_path.replace(".mp4", "_silent.mp4")
    ff = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS),
                           "-i", "-", "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", "-movflags", "+faststart", silent], stdin=subprocess.PIPE)
    for k in range(total):
        active = [(s, k - st) for s, st in zip(shots, starts) if st <= k < st + s.n]
        if len(active) == 1:
            fr = active[0][0].frame(active[0][1])
        else:
            (sa, ia), (sb, ib) = active[0], active[1]
            fr = blend(sb.trans, sa.frame(ia), sb.frame(ib), (ib + 1) / sb.tdur)
        if k < fade_in: fr = Image.blend(Image.new("RGBA", (W, H), CREAM + (255,)), fr, (k + 1) / fade_in)
        if k >= total - fade_out: fr = Image.blend(fr, Image.new("RGBA", (W, H), (20, 14, 10, 255)), (k - (total - fade_out) + 1) / fade_out)
        ff.stdin.write(fr.convert("RGB").tobytes())
        if k % 150 == 0: print(f"  frame {k}/{total}", flush=True)
    ff.stdin.close(); ff.wait()
    # audio: music bed + sfx placed on the timeline (seconds)
    dur = total / FPS
    inputs = ["-i", silent, "-stream_loop", "-1", "-i", f"{AUDIO}/cute-music-26476.mp3"]
    chains = [f"[1:a]volume=0.55,afade=t=in:d=0.5,afade=t=out:st={dur - 2.5:.2f}:d=2.5,atrim=0:{dur:.2f}[m]"]
    labels = ["[m]"]
    for j, (t, f, gain) in enumerate(sfx):
        inputs += ["-i", f"{AUDIO}/{f}"]
        chains.append(f"[{j + 2}:a]aformat=channel_layouts=stereo,volume={gain},adelay={int(t * 1000)}|{int(t * 1000)}[s{j}]"); labels.append(f"[s{j}]")
    chains.append("".join(labels) + f"amix=inputs={len(labels)}:normalize=0:duration=first,aresample=48000[a]")
    cmd = ["ffmpeg", "-y", "-loglevel", "error"] + inputs + ["-filter_complex", ";".join(chains), "-map", "0:v", "-map", "[a]",
           "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-ac", "2", "-t", f"{dur:.2f}", out_path]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode: print(r.stderr[-2000:]); sys.exit(1)
    os.remove(silent)
    print("done", out_path, f"{dur:.1f}s")
    return starts

def C(text, t0, t1, **kw): return dict(text=text, t0=t0, t1=t1, **kw)

# arrangement recording: first drag frame, last frame to use, seconds into the shot when Ready is tapped
ARR_A, ARR_B, ARR_READY = 10, 440, 10.7

# ---------- edit lists ----------
def appstore():
    shots = [
        Shot("02_day_walkin", 40, 215, zoom=(0.62, 0.33, 1.0, 1.12), trans="cut", name="walk-in + serve",
             caps=[C("Serve hungry customers", 0.3, 2.8, style=0), C("Happy customers tip!", 5.4, 6.1, style=1, y=0.30, x=0.42)], hold=8),
        Shot("08_arrange", ARR_A, ARR_A + 66, zoom=(0.5, 0.72, 1.15, 1.25), trans="xfade", tdur=8, name="arrange table",
             caps=[C("Arrange your table", 0.2, 2.0, style=2, y=0.30, size=56, tilt=2)]),
        Shot("02_day_walkin", 100, 145, zoom=(0.82, 0.90, 1.3, 2.2), trans="xfade", tdur=8, name="zoom bread",
             caps=[C("Fresh bread", 0.1, 1.5, style=3, y=0.30, size=56)]),
        Shot("03_coffee_machine", 20, 65, zoom=(0.18, 0.88, 1.3, 2.2), trans="cut", name="zoom coffee",
             caps=[C("Hot coffee", 0.1, 1.5, style=0, y=0.30, size=56, tilt=2)]),
        Shot("02_day_walkin", 150, 192, zoom=(0.82, 0.73, 1.3, 2.1), trans="cut", name="zoom cake",
             caps=[C("Sweet cake", 0.1, 1.4, style=1, y=0.30, size=56)]),
        Shot("04_kitchen", 80, 170, zoom=(0.78, 0.63, 1.0, 1.15), trans="iris", tdur=12, name="kitchen bake",
             caps=[C("Bake in your own kitchen", 0.3, 2.4, style=3, size=54, y=0.455)]),
        Shot("04_kitchen", 200, 260, zoom=(0.76, 0.30, 1.6, 2.3), trans="xfade", tdur=8, name="zoom apple tree",
             caps=[C("Pick fresh apples", 0.1, 2.0, style=2, y=0.72, size=54)]),
        Shot("04_kitchen", 260, 320, zoom=(0.17, 0.80, 1.6, 2.3), trans="cut", name="zoom juice",
             caps=[C("Squeeze juice", 0.1, 2.0, style=3, y=0.30, size=54, tilt=2)]),
        Shot("05_shop", 65, 185, zoom=(0.5, 0.5, 1.0, 1.08), trans="dip", tdur=14, name="shop",
             caps=[C("Unlock new regulars", 0.3, 2.6, style=1, y=0.80), C("Meet the Chef!", 2.7, 4.0, style=3, y=0.80, sub="big orders, one extra item")]),
        Shot("06_dusk_night", 120, 310, step=2, zoom=(0.6, 0.35, 1.0, 1.1), trans="xfade", tdur=10, name="dusk to night",
             caps=[C("Serve till the moon comes out", 0.3, 3.0, style=4, size=50)]),
        Shot("06_dusk_night", 330, 380, zoom=(0.52, 0.27, 1.4, 2.0), trans="xfade", tdur=8, name="zoom moon",
             caps=[C("Cozy nights", 0.2, 1.6, style=4, y=0.70, size=56)]),
        Shot("07_chef_juice_window", 376, 436, zoom=(0.72, 0.30, 1.1, 1.35), trans="slide", tdur=12, name="kitchen at night",
             caps=[C("Moonlit kitchen", 0.3, 1.9, style=4, y=0.72, size=56, tilt=2)]),
        Shot("07_chef_juice_window", 452, 524, zoom=(0.6, 0.35, 1.0, 1.06), trans="xfade", tdur=10, name="chef at night / end",
             caps=[C("Food Truck Cafe", 0.3, 2.6, style=0, size=72, sub="bake, serve & grow your cafe", y=0.455)], hold=0),
    ]
    # timeline seconds are computed from the printed starts; sfx below use those (shot start + offset)
    return shots

def extended():
    shots = [
        Shot("02_day_walkin", 0, 358, zoom=(0.62, 0.33, 1.0, 1.10), trans="cut", name="walk-in + serve (full)",
             caps=[C("Serve hungry customers", 0.6, 3.4, style=0), C("Happy customers tip!", 6.7, 8.3, style=1, y=0.30, x=0.42),
                   C("Next customer!", 10.3, 11.9, style=2, y=0.30, x=0.42)]),
        Shot("08_arrange", ARR_A, ARR_B, zoom=(0.5, 0.6, 1.0, 1.08), trans="xfade", tdur=10, name="arrange table (full)",
             caps=[C("Arrange your table", 0.4, 3.0, style=2, y=0.30, size=56, tilt=2), C("Ready!", ARR_READY, ARR_READY + 1.2, style=3, y=0.30, size=56)]),
        Shot("03_coffee_machine", 0, 241, zoom=(0.5, 0.5, 1.0, 1.0), trans="xfade", tdur=10, name="coffee machine (full)",
             caps=[C("Tap the machine to brew", 0.4, 2.8, style=3, y=0.80)]),
        Shot("02_day_walkin", 100, 160, zoom=(0.82, 0.90, 1.3, 2.2), trans="xfade", tdur=8, name="zoom bread",
             caps=[C("Fresh bread", 0.1, 1.9, style=3, y=0.30, size=56)]),
        Shot("03_coffee_machine", 20, 80, zoom=(0.18, 0.88, 1.3, 2.2), trans="cut", name="zoom coffee",
             caps=[C("Hot coffee", 0.1, 1.9, style=0, y=0.30, size=56, tilt=2)]),
        Shot("02_day_walkin", 150, 210, zoom=(0.82, 0.73, 1.3, 2.1), trans="cut", name="zoom cake",
             caps=[C("Sweet cake", 0.1, 1.9, style=1, y=0.30, size=56)]),
        Shot("03_coffee_machine", 150, 210, zoom=(0.50, 0.65, 1.3, 2.1), trans="cut", name="zoom juice pitcher",
             caps=[C("Cold juice", 0.1, 1.9, style=2, y=0.30, size=56, tilt=2)]),
        Shot("04_kitchen", 0, 350, zoom=(0.78, 0.63, 1.0, 1.15), trans="iris", tdur=12, name="kitchen bake (full)",
             caps=[C("Bake in your own kitchen", 0.5, 3.0, style=3, size=54), C("Ding! Fresh bread", 10.6, 11.6, style=0, size=54)]),
        Shot("04_kitchen", 200, 290, zoom=(0.76, 0.30, 1.6, 2.3), trans="xfade", tdur=8, name="zoom apple tree",
             caps=[C("Pick fresh apples", 0.1, 2.6, style=2, y=0.72, size=54)]),
        Shot("04_kitchen", 260, 350, zoom=(0.17, 0.80, 1.6, 2.3), trans="cut", name="zoom juice bottle",
             caps=[C("Squeeze juice", 0.1, 2.6, style=3, y=0.30, size=54, tilt=2)]),
        Shot("04_kitchen", 120, 210, zoom=(0.50, 0.43, 1.6, 2.3), trans="cut", name="zoom eggs + cake",
             caps=[C("Whip up a cake", 0.1, 2.6, style=1, y=0.72, size=54)]),
        Shot("05_shop", 30, 255, zoom=(0.5, 0.5, 1.0, 1.08), trans="dip", tdur=14, name="shop (full)",
             caps=[C("Unlock new regulars", 1.2, 3.6, style=1, y=0.80), C("Meet the Chef!", 3.9, 5.6, style=3, y=0.80, sub="big orders, one extra item"),
                   C("Chef unlocked!", 5.8, 7.4, style=2, y=0.80)]),
        Shot("06_dusk_night", 0, 740, step=1, zoom=(0.6, 0.35, 1.0, 1.08), trans="xfade", tdur=10, name="dusk to night (full)",
             caps=[C("Serve till the moon comes out", 2.0, 5.0, style=4, size=50), C("Grandma tips double", 15.0, 17.5, style=4, size=50, y=0.80),
                   C("Day complete!", 21.5, 24.0, style=3, size=56, y=0.90)]),
        Shot("06_dusk_night", 330, 420, zoom=(0.52, 0.27, 1.4, 2.0), trans="xfade", tdur=8, name="zoom moon",
             caps=[C("Cozy nights", 0.2, 2.6, style=4, y=0.70, size=56)]),
        Shot("07_chef_juice_window", 0, 524, zoom=(0.6, 0.35, 1.0, 1.06), trans="slide", tdur=12, name="chef, juice, night kitchen (full)",
             caps=[C("The Chef orders big", 2.8, 5.2, style=0, size=54), C("Moonlit kitchen", 12.8, 14.8, style=4, y=0.72, size=56, tilt=2),
                   C("Food Truck Cafe", 15.3, 17.4, style=0, size=72, sub="bake, serve & grow your cafe")], hold=8),
    ]
    return shots

if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "appstore"
    shots = appstore() if which == "appstore" else extended()
    # sound effects: (shot index, seconds into shot, file, gain)
    if which == "appstore":
        ev = [(0, 5.4, "coin.mp3", 0.9), (5, 1.1, "ovensound_EDITED.wav", 0.7), (7, 0.3, "pour_EDITED.wav", 0.9), (8, 3.8, "cha-ching-7053.mp3", 0.6)]
    else:
        ev = [(0, 6.7, "coin.mp3", 0.9), (0, 11.4, "door_EDITED.wav", 0.8), (7, 10.6, "ovensound_EDITED.wav", 0.7), (9, 0.3, "pour_EDITED.wav", 0.9),
              (11, 5.9, "cha-ching-7053.mp3", 0.6), (12, 10.5, "coin.mp3", 0.9), (14, 5.5, "pour_EDITED.wav", 0.9), (14, 9.9, "coin.mp3", 0.9)]
    starts, g = [], 0
    for i, s in enumerate(shots):
        if i: g -= s.tdur
        starts.append(g); g += s.n
    sfx = [(starts[i] / FPS + t, f, gain) for i, t, f, gain in ev]
    out = f"{ROOT}/FoodTruckCafe_{'appstore_v2' if which == 'appstore' else 'extended_cut_v2'}.mp4"
    render(shots, out, sfx)

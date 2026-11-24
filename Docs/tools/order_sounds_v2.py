"""Order sounds v2 (owner, 24 Nov 2026: "more difference between them"): same sources, wider steps.
  OK      = the cash register a little lower, short and soft: just "done"
  Nice    = the register brighter with two bells rising (E6, G6)
  Perfect = today's cha-ching, a full rising bell sparkle (C6 E6 G6 C7) and a shimmer tail
Out next to this script: v2_order_ok|nice|perfect.mp3 (for the report; the game keeps v1 until picked)."""
import numpy as np, wave, subprocess
exec(open("make.py").read().split("cash = start(")[0].replace('OUT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Resources/Audio/Orders/"', 'OUT = "./v2_"'))
rng = np.random.default_rng(5)
def shimmer(sec, n, amp=0.08):
    out = np.zeros(int(SR * sec))
    for _ in range(n):
        st = rng.integers(0, len(out) - 4000); f = rng.uniform(3500, 7500); x = t(0.08); out[st:st + len(x)] += amp * np.sin(2 * np.pi * f * x) * np.exp(-x / 0.025)
    return out
cash = start(read("cash.wav")); ching = start(read("chaching2.wav")); pk = np.abs(cash).max(); pc = np.abs(ching).max()
save("order_ok", trim(pitch(cash, -2), 0.75, 0.3), 0.45)
save("order_nice", mix([(0, trim(pitch(cash, 2), 1.0) / pk), (0.05, 0.32 * bell(1318.5, 0.9, 0.25)), (0.16, 0.36 * bell(1568.0, 1.0, 0.3))], 1.25), 0.62)
C6, E6, G6, C7, E7 = 1046.5, 1318.5, 1568.0, 2093.0, 2637.0
sp = mix([(0.00, bell(C6, .9, .25)), (0.07, bell(E6, .9, .25)), (0.14, bell(G6, .9, .25)), (0.21, bell(C7, 1.2, .35)), (0.28, 0.7 * bell(E7, 1.2, .35))], 1.6)
save("order_perfect", mix([(0, trim(ching, 1.3, 0.3) / pc), (0.04, 0.42 * sp / np.abs(sp).max()), (0.25, shimmer(1.3, 26))], 1.7), 0.78)
for n in ("ok", "nice", "perfect"):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", f"v2_order_{n}.wav", "-b:a", "128k", f"v2_order_{n}.mp3"], check=True)

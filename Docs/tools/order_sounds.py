"""Order-complete sounds by speed (owner, 24 November 2026: "Use different order complete sound for different level of
speed, ok/nice/perfect. Just tune the current one"). From the game's own two: cash.mp3 (an order done) and
chaching2.mp3 (a perfect order).
  OK      = the cash register as now, a little shorter and softer
  Nice    = the same register, brighter (2 semitones up) with one small bell on top
  Perfect = the cha-ching of today's perfect order with a quick rising sparkle of bells
Out: FoodTruckCafe/Assets/Resources/Audio/Orders/order_ok|nice|perfect.wav   ../artkit/venv/bin/python -I make.py"""
import numpy as np, wave
SR = 44100; OUT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Resources/Audio/Orders/"
def read(p):
    with wave.open(p) as w: return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767
def save(name, s, peak):
    s = s / (np.abs(s).max() + 1e-9) * peak; f = int(0.04 * SR); s[-f:] *= np.linspace(1, 0, f)
    with wave.open(OUT + name + ".wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((s * 32767).astype(np.int16).tobytes())
    print(name, round(len(s) / SR, 2), "s")
def trim(s, sec, fade=0.35):
    s = s[:int(sec * SR)].copy(); f = int(fade * SR); s[-f:] *= np.linspace(1, 0, f) ** 1.5; return s
def start(s, thr=0.02):
    i = int(np.argmax(np.abs(s) > thr * np.abs(s).max())); return s[max(0, i - 200):]
def pitch(s, semis):
    k = 2 ** (semis / 12); idx = np.arange(0, len(s) - 1, k); i0 = idx.astype(int); fr = idx - i0
    return s[i0] * (1 - fr) + s[np.minimum(i0 + 1, len(s) - 1)] * fr
def t(sec): return np.arange(int(SR * sec)) / SR
def bell(f, sec=0.9, d=0.28, amp=1.0):
    x = t(sec); s = sum(w * np.sin(2 * np.pi * f * m * x) * np.exp(-x / (d / (1 + 0.6 * i))) for i, (m, w) in enumerate(((1, 1), (2.01, .4), (3.0, .18), (4.17, .08))))
    return amp * s * np.minimum(1, x / 0.003)
def mix(parts, sec):
    out = np.zeros(int(SR * sec))
    for at, s in parts:
        i = int(at * SR); s = s[:len(out) - i]; out[i:i + len(s)] += s
    return out
cash = start(read("cash.wav")); ching = start(read("chaching2.wav"))
pk = np.abs(cash).max(); pc = np.abs(ching).max()
ok = trim(cash, 1.25)
save("order_ok", ok, 0.55)
nice = trim(pitch(cash, 2), 1.2)
save("order_nice", mix([(0, nice / pk), (0.06, 0.22 * bell(1318.5, 0.9, 0.25))], 1.25), 0.62)
C6, E6, G6, C7 = 1046.5, 1318.5, 1568.0, 2093.0
sp = mix([(0.00, bell(C6, .8, .22)), (0.055, bell(E6, .8, .22)), (0.11, bell(G6, .8, .22)), (0.165, bell(C7, 1.0, .3))], 1.2)
perfect = mix([(0, trim(ching, 1.3, 0.3) / pc), (0.05, 0.26 * sp / np.abs(sp).max())], 1.35)
save("order_perfect", perfect, 0.72)

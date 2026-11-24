"""Bonus-day reveal recordings (bonus_capture.py) to mp4s with sound, plus the still before.   ../artkit/venv/bin/python -I videos.py"""
import json, glob, os, wave, subprocess, shutil
import numpy as np
from PIL import Image
SHOTS = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Screenshots"; SND = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Resources/Audio/Stars/"
REPORT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/report_page/day9/"; os.makedirs(REPORT, exist_ok=True); SR = 44100
clips = json.load(open("clips.json"))
def readwav(p):
    with wave.open(p) as w: return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767


for name, c in clips.items():
    fr = sorted(glob.glob(f"{SHOTS}/m9_{name}_*.png")); d = f"frames/{name}"; shutil.rmtree(d, ignore_errors=True); os.makedirs(d)
    for i, f in enumerate(fr): Image.open(f).convert("RGB").resize((440, 956), Image.LANCZOS).save(f"{d}/{i:03d}.png")
    dur = len(fr) / 25 + 0.8; audio = np.zeros(int(SR * dur), np.float32)
    for snd, at in c["sounds"]:
        s = readwav(SND + snd + ".wav"); i0 = int(max(0, at) * SR); s = s[:len(audio) - i0]; audio[i0:i0 + len(s)] += s
    with wave.open(f"{d}/audio.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((np.clip(audio, -1, 1) * 32767).astype(np.int16).tobytes())
    out = REPORT + f"{name}.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "25", "-i", f"{d}/%03d.png", "-i", f"{d}/audio.wav", "-vf", "tpad=stop_mode=clone:stop_duration=0.8",
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "26", "-preset", "slow", "-c:a", "aac", "-b:a", "96k", "-movflags", "+faststart", "-shortest", out], check=True)
    Image.open(f"{d}/{len(fr) - 1:03d}.png").save(REPORT + f"{name}.jpg", quality=80)
    print(name, len(fr), os.path.getsize(out) // 1024, "KB", c["sounds"])
for f in glob.glob(SHOTS + "/m9_*"): os.remove(f)

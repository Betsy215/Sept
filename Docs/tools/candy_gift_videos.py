"""Day 3 candy gift recordings (candy_capture.py) to mp4s with their sounds.   ../artkit/venv/bin/python -I videos.py"""
import json, glob, os, wave, subprocess, shutil
import numpy as np
from PIL import Image
SHOTS = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Screenshots"; RES = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Assets/Resources/"
REPORT = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/report_page/candy/"; os.makedirs(REPORT, exist_ok=True); SR = 44100
def load(path):
    for ext in (".wav", ".mp3"):
        f = RES + path + ext
        if os.path.exists(f):
            tmp = "/tmp/_snd.wav"; subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", f, "-ac", "1", "-ar", str(SR), tmp], check=True)
            with wave.open(tmp) as w: return np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32767
    return np.zeros(1, np.float32)
for name, c in json.load(open("clips.json")).items():
    st = name[1:]; fr = sorted(glob.glob(f"{SHOTS}/cg{st}_*.png")); d = f"frames/{name}"; shutil.rmtree(d, ignore_errors=True); os.makedirs(d)
    for i, f in enumerate(fr): Image.open(f).convert("RGB").crop((0, 0, 1320, 2400)).resize((440, 800), Image.LANCZOS).save(f"{d}/{i:03d}.png")
    audio = np.zeros(int(SR * (len(fr) / 25 + 0.6)), np.float32)
    for snd, at in c["sounds"]:
        s = load(snd) * 0.8; i0 = int(max(0, at) * SR); s = s[:len(audio) - i0]; audio[i0:i0 + len(s)] += s
    with wave.open(f"{d}/audio.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((np.clip(audio, -1, 1) * 32767).astype(np.int16).tobytes())
    out = REPORT + f"{name}.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", "25", "-i", f"{d}/%03d.png", "-i", f"{d}/audio.wav", "-vf", "tpad=stop_mode=clone:stop_duration=0.6",
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "26", "-preset", "slow", "-c:a", "aac", "-b:a", "96k", "-movflags", "+faststart", "-shortest", out], check=True)
    Image.open(f"{d}/{min(60, len(fr) - 1):03d}.png").save(REPORT + f"{name}.jpg", quality=80)
    print(name, len(fr), os.path.getsize(out) // 1024, "KB")
for f in glob.glob(SHOTS + "/cg*"): os.remove(f)

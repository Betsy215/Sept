"""Measure a painted glow (glow_brush.py): for every pixel outside the object's solid body, its distance to the body;
mean alpha and colour per distance. python3 glow_profile.py <picture> [<object picture, when the glow is its own sprite>]"""
import sys, math
from collections import deque
from PIL import Image
def profile(path, body_path=None, thr=250):
    im = Image.open(path).convert('RGBA'); W, H = im.size; px = im.load()
    if body_path:   # glow is a separate picture: the body comes from the object's own sprite, placed centred (as in the game)
        b = Image.open(body_path).convert('RGBA'); bx = (W - b.width)//2; by = (H - b.height)//2; bp = b.load()
        solid = [[False]*W for _ in range(H)]
        for y in range(b.height):
            for x in range(b.width):
                if 0 <= x+bx < W and 0 <= y+by < H and bp[x, y][3] >= thr: solid[y+by][x+bx] = True
    else:
        solid = [[px[x, y][3] >= thr for x in range(W)] for y in range(H)]
    INF = 10**9; d = [[INF]*W for _ in range(H)]; near = [[None]*W for _ in range(H)]; q = deque()
    for y in range(H):
        for x in range(W):
            if solid[y][x]: d[y][x] = 0; near[y][x] = (x, y); q.append((x, y))
    while q:   # nearest-seed propagation (close to the true Euclidean distance)
        x, y = q.popleft(); sx, sy = near[y][x]
        for dx, dy in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
            nx, ny = x+dx, y+dy
            if 0 <= nx < W and 0 <= ny < H:
                nd = math.hypot(nx-sx, ny-sy)
                if nd + 1e-6 < d[ny][nx]: d[ny][nx] = nd; near[ny][nx] = (sx, sy); q.append((nx, ny))
    acc = {}
    for y in range(H):
        for x in range(W):
            if not solid[y][x] and d[y][x] < INF:
                k = int(d[y][x] + 0.5); r, g, b_, a = px[x, y]
                s = acc.setdefault(k, [0, 0, 0, 0, 0, 0]); s[0] += a; s[1] += 1
                if a: s[2] += r*a; s[3] += g*a; s[4] += b_*a; s[5] += a
    return W, H, acc
if __name__ == '__main__':
    W, H, acc = profile(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
    print(sys.argv[1], W, H)
    for k in sorted(acc)[:60]:
        s = acc[k]; a = s[0]/s[1]
        col = (round(s[2]/s[5]), round(s[3]/s[5]), round(s[4]/s[5])) if s[5] else None
        if k <= 40 or a > 1: print(f'{k:3d}px alpha {a:6.1f} n{s[1]:5d} colour {col}')

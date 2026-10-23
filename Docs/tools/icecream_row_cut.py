#!/usr/bin/python3
"""Cuts the one-row ice cream station parts into the game and writes the station's layout into GameSceneOne (22 October 2026).

Input: a folder of PNGs that all share the box canvas (same size, transparent surroundings), each part drawn at its place:
  station_box.png              the box (and nothing else)
  sec<0..3>_l<1..3>.png        each well's fill at three levels, wells left to right vanilla, strawberry, chocolate, mint
  lid2.png, lid3.png           the lids over wells 2 and 3 (closed until the shop's "Two more flavours" upgrade)
  holder.png                   the cone holder
  upgrade_icon.png (optional)  the shop card picture, any size
Every part is cropped to its alpha bounds and saved under Assets/Images/icecream with its old name (the .meta and guid
stay), new names get a fresh .meta. The crop's bounds, as fractions of the box canvas from the bottom left, become the
station's sectionRects, lidRects, holderRect; buildPoint stays above the box's centre; the scene scale is set so the
box keeps about the old on-screen width.

  python3 Docs/tools/icecream_row_cut.py <parts folder> [--scale 0.28]
"""
import os, re, sys, uuid, json
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
IMG = os.path.join(ROOT, "Assets/Images/icecream")
SCENE = os.path.join(ROOT, "Assets/Scenes/GameSceneOne.unity")
STATION_SCRIPT_GUID = "dae2965ca6c97429e9f79bb7a326260b"
TRANSFORM_ID = "1663258850"                       # the IceCream object's Transform


def guid_of(png):
    meta = png + ".meta"
    if os.path.exists(meta):
        return re.search(r"^guid: ([0-9a-f]{32})", open(meta).read(), re.M).group(1)
    template = open(os.path.join(IMG, "station_box.png.meta")).read()
    g = uuid.uuid4().hex
    open(meta, "w").write(re.sub(r"^guid: [0-9a-f]{32}", "guid: " + g, template, count=1, flags=re.M))
    print("new meta", os.path.basename(meta), g)
    return g


def union_bbox(parts, names):
    """One crop box for a well's three levels, so each level's picture keeps its place in the well."""
    bbs = [Image.open(os.path.join(parts, n)).convert("RGBA").getbbox() for n in names]
    return (min(b[0] for b in bbs), min(b[1] for b in bbs), max(b[2] for b in bbs), max(b[3] for b in bbs))


def cut(parts, name, out_name=None, bb=None):
    im = Image.open(os.path.join(parts, name)).convert("RGBA")
    bb = bb or im.getbbox()
    if bb is None:
        raise SystemExit(name + " is empty")
    crop = im.crop(bb)
    out = os.path.join(IMG, out_name or name)
    crop.save(out)
    W, H = im.size
    x0, y0, x1, y1 = bb
    rect = (x0 / W, 1 - y1 / H, (x1 - x0) / W, (y1 - y0) / H)         # Unity: fractions from the bottom left
    print("%-14s %4dx%-4d rect x %.4f y %.4f w %.4f h %.4f" % (os.path.basename(out), crop.width, crop.height, *rect))
    return rect, (W, H)


def yaml_rect(r, indent):
    pad = " " * indent
    return "%s- serializedVersion: 2\n%s  x: %.4f\n%s  y: %.4f\n%s  width: %.4f\n%s  height: %.4f\n" % (pad, pad, r[0], pad, r[1], pad, r[2], pad, r[3])


def main():
    parts = sys.argv[1]
    scale = 0.28
    if "--scale" in sys.argv:
        scale = float(sys.argv[sys.argv.index("--scale") + 1])
    # the box keeps the whole canvas: the rects are fractions of the box sprite's bounds, so canvas and sprite must be one
    box = Image.open(os.path.join(parts, "station_box.png")).convert("RGBA"); size = box.size
    box.save(os.path.join(IMG, "station_box.png")); print("station_box.png %dx%d, the whole canvas" % size)
    sec_rects = []
    for i in range(4):
        names = ["sec%d_l%d.png" % (i, lv) for lv in (1, 2, 3)]
        bb = union_bbox(parts, names)
        for n in names:
            r = cut(parts, n, bb=bb)[0]
        sec_rects.append(r)
    lid = {2: cut(parts, "lid2.png", "lid.png")[0], 3: cut(parts, "lid3.png", "lid_unused.png")[0]}
    os.remove(os.path.join(IMG, "lid_unused.png"))                    # one lid sprite serves both wells; only the rect differs
    if "--holder-keep" in sys.argv:
        # the game's own holder.png (195 x 267, the cone-count states holder_0..4 in Resources/IceCream are cut from it) was
        # pasted on the canvas at this top-left; keep the file and place its full box
        k = sys.argv.index("--holder-keep"); hx, hy = int(sys.argv[k + 1]), int(sys.argv[k + 2])
        hw, hh = Image.open(os.path.join(IMG, "holder.png")).size
        holder_rect = (hx / size[0], 1 - (hy + hh) / size[1], hw / size[0], hh / size[1])
        print("holder.png kept   rect x %.4f y %.4f w %.4f h %.4f" % holder_rect)
    else:
        holder_rect = cut(parts, "holder.png")[0]
    if os.path.exists(os.path.join(parts, "upgrade_icon.png")):
        Image.open(os.path.join(parts, "upgrade_icon.png")).convert("RGBA").save(os.path.join(IMG, "upgrade_icon.png"))
        guid_of(os.path.join(IMG, "upgrade_icon.png"))
    lid_guid = guid_of(os.path.join(IMG, "lid.png"))

    # the scene: the IceCreamStation component block
    s = open(SCENE).read()
    start = s.index("m_Script: {fileID: 11500000, guid: %s, type: 3}" % STATION_SCRIPT_GUID)
    end = s.index("--- !u!", start)
    block = s[start:end]
    # sectionRects
    block = re.sub(r"  sectionRects:\n(?:  - serializedVersion: 2\n(?:    [xywidthheg]+: [-\d.]+\n){4}){4}",
                   "  sectionRects:\n" + "".join(yaml_rect(r, 2) for r in sec_rects), block)
    block = re.sub(r"  holderRect:\n    serializedVersion: 2\n(?:    [a-z]+: [-\d.]+\n){4}",
                   "  holderRect:\n    serializedVersion: 2\n    x: %.4f\n    y: %.4f\n    width: %.4f\n    height: %.4f\n" % holder_rect, block)
    lid_rects = [(0, 0, 0, 0), (0, 0, 0, 0), lid[2], lid[3]]
    block = re.sub(r"  lidRects:\n(?:  - serializedVersion: 2\n(?:    [a-z]+: [-\d.]+\n){4}){4}", "", block)
    block = block.replace("  buildPoint:", "  lidRects:\n" + "".join(yaml_rect(r, 2) for r in lid_rects) + "  buildPoint:")
    block = re.sub(r"  lidSprite: \{[^}]*\}\n", "", block)
    block = re.sub(r"(  holderSprite: \{[^}]*\}\n)", r"\1  lidSprite: {fileID: 21300000, guid: %s, type: 3}\n" % lid_guid, block)
    block = re.sub(r"  buildPoint: \{x: [-\d.]+, y: [-\d.]+\}", "  buildPoint: {x: 0.42, y: 1.22}", block)
    s = s[:start] + block + s[end:]
    # the drag collider (BoxCollider on the same object, local units at 100 px per unit): the drawn extent of the canvas
    s = re.sub(r"(m_GameObject: \{fileID: 1663258845\}\n(?:.*\n){0,14}?  m_Size: )\{x: [-\d.]+, y: [-\d.]+, z: 0.2\}\n  m_Center: \{x: [-\d.]+, y: [-\d.]+, z: 0\}",
               lambda m: m.group(1) + "{x: %g, y: %g, z: 0.2}\n  m_Center: {x: 0, y: %g, z: 0}" % (size[0] / 100, round(size[1] / 100 * 0.86, 2), -round(size[1] / 100 * 0.07, 2)), s, count=1)
    # the object's scale
    t = s.index("--- !u!4 &%s\n" % TRANSFORM_ID)
    te = s.index("--- !u!", t + 10)
    tb = re.sub(r"m_LocalScale: \{x: [-\d.]+, y: [-\d.]+, z: 1\}", "m_LocalScale: {x: %.2f, y: %.2f, z: 1}" % (scale, scale), s[t:te])
    s = s[:t] + tb + s[te:]
    open(SCENE, "w").write(s)
    json.dump({"canvas": size, "sectionRects": sec_rects, "lidRects": lid_rects, "holderRect": holder_rect, "scale": scale},
              open(os.path.join(parts, "layout_written.json"), "w"), indent=1)
    print("scene written; scale", scale)


if __name__ == "__main__":
    main()

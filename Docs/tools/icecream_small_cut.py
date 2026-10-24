#!/usr/bin/python3
"""Cuts the TWO-WELL level 1 station (owner, 23 October 2026: "a station with just two flavors, not with the other two
covered, half the size, everything else the same") into the game beside the long box. Same canvas as the long box
(1200 x 420), so the rects stay fractions of one bounds; the IceCreamStation picks this set while only two flavours are open.

Input folder: station_box.png (the short box on the whole canvas), sec0_l1..3.png, sec1_l1..3.png (the two wells' fills
at their place), the holder pasted at --holder-keep x y (the game's holder.png stays the holder picture).
Writes Assets/Images/icecream/station_box_small.png and sec0_l1_s.png .. sec1_l3_s.png (fresh .meta files), and the
smallBoxSprite, smallSectionLevels, smallSectionRects, smallHolderRect and smallBuildPoint fields of the IceCreamStation
block in GameSceneOne (added after buildPoint when absent).

  python3 Docs/tools/icecream_small_cut.py <parts folder> --holder-keep 551 149
"""
import os, re, sys
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import icecream_row_cut as C


def main():
    parts = sys.argv[1]
    box = Image.open(os.path.join(parts, "station_box.png")).convert("RGBA"); size = box.size
    box.save(os.path.join(C.IMG, "station_box_small.png")); box_guid = C.guid_of(os.path.join(C.IMG, "station_box_small.png"))
    print("station_box_small.png %dx%d, the whole canvas" % size)
    sec_rects = []; level_guids = []
    for i in range(2):
        names = ["sec%d_l%d.png" % (i, lv) for lv in (1, 2, 3)]
        bb = C.union_bbox(parts, names)
        for n in names:
            out_name = n.replace(".png", "_s.png")
            r = C.cut(parts, n, out_name, bb=bb)[0]
            level_guids.append(C.guid_of(os.path.join(C.IMG, out_name)))
        sec_rects.append(r)
    k = sys.argv.index("--holder-keep"); hx, hy = int(sys.argv[k + 1]), int(sys.argv[k + 2])
    hw, hh = Image.open(os.path.join(C.IMG, "holder.png")).size
    holder_rect = (hx / size[0], 1 - (hy + hh) / size[1], hw / size[0], hh / size[1])
    print("holder (the game's holder.png) at rect x %.4f y %.4f w %.4f h %.4f" % holder_rect)
    build_x = (sec_rects[0][0] + sec_rects[1][0] + sec_rects[1][2]) / 2.0          # above the middle of the two wells

    s = open(C.SCENE).read()
    start = s.index("m_Script: {fileID: 11500000, guid: %s, type: 3}" % C.STATION_SCRIPT_GUID)
    end = s.index("--- !u!", start)
    block = s[start:end]
    # drop earlier small fields, then add the set after buildPoint
    for key in ("smallBoxSprite", "smallSectionLevels", "smallSectionRects", "smallHolderRect", "smallBuildPoint"):
        block = re.sub(r"  %s:.*\n(?:  (?:- |  )[^\n]*\n)*" % key, "", block)
    small = "  smallBoxSprite: {fileID: 21300000, guid: %s, type: 3}\n" % box_guid
    small += "  smallSectionLevels:\n" + "".join("  - {fileID: 21300000, guid: %s, type: 3}\n" % g for g in level_guids)
    small += "  smallSectionRects:\n" + "".join(C.yaml_rect(r, 2) for r in sec_rects)
    small += "  smallHolderRect:\n    serializedVersion: 2\n    x: %.4f\n    y: %.4f\n    width: %.4f\n    height: %.4f\n" % holder_rect
    small += "  smallBuildPoint: {x: %.3f, y: 1.22}\n" % build_x
    m = re.search(r"  buildPoint: \{[^}]*\}\n", block)
    block = block[:m.end()] + small + block[m.end():]
    s = s[:start] + block + s[end:]
    open(C.SCENE, "w").write(s)
    print("scene written: small set, build point x %.3f" % build_x)


if __name__ == "__main__":
    main()

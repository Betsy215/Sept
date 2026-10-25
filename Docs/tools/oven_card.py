#!/usr/bin/python3
"""Adds the "OvenUpgrade" card to the Shop scene (25 October 2026: ovens are bought; the player starts with one, this card
sells the second and then the third): a copy of the IceCreamUpgrade card's objects with new fileIDs, as an Upgrade of
"Oven" (SessionManager.OvensOwned = the Oven upgrade level, 1 to 3). Idempotent: a second run finds the card by name
and does nothing. Price and texts are set here; the picture is Assets/Images/Kitchen/ovens/oven_card_icon.png.

  python3 Docs/tools/oven_card.py
"""
import os, re, random

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SCENE = os.path.join(ROOT, "Assets/Scenes/Shop.unity")
ICON_META = os.path.join(ROOT, "Assets/Images/Kitchen/ovens/oven_card_icon.png.meta")
SOURCE = "IceCreamUpgrade"
NAME = "OvenUpgrade"
PRICE = 500


def main():
    s = open(SCENE).read()
    if "m_Name: %s\n" % NAME in s:
        print("card already in the scene"); return
    icon_guid = re.search(r"^guid: ([0-9a-f]{32})", open(ICON_META).read(), re.M).group(1)
    blocks = re.split(r"(?m)^(?=--- !u!)", s)
    by_id = {}
    for b in blocks:
        m = re.match(r"--- !u!(\d+) &(\d+)\n", b)
        if m: by_id[m.group(2)] = (m.group(1), b)
    # the source card: its GameObject, then every component and every descendant (RectTransform children)
    go = next(i for i, (k, b) in by_id.items() if k == "1" and "m_Name: %s\n" % SOURCE in b)
    todo = [go]; ids = []
    while todo:
        g = todo.pop(0); ids.append(g)
        comps = re.findall(r"component: \{fileID: (\d+)\}", by_id[g][1])
        ids.extend(comps)
        for c in comps:
            if by_id[c][0] == "224":
                kids = re.findall(r"- \{fileID: (\d+)\}", by_id[c][1].split("m_Children:")[1].split("m_Father")[0]) if "m_Children:" in by_id[c][1] else []
                for k in kids: todo.append(re.search(r"m_GameObject: \{fileID: (\d+)\}", by_id[k][1]).group(1))
    src_rt = next(c for c in re.findall(r"component: \{fileID: (\d+)\}", by_id[go][1]) if by_id[c][0] == "224")
    used = set(by_id)
    rnd = random.Random(2510)
    def fresh():
        while True:
            n = str(rnd.randint(100000000, 1999999999))
            if n not in used:
                used.add(n); return n
    new_ids = {old: fresh() for old in ids}
    copies = []
    for old in ids:
        b = by_id[old][1]
        for o, n in new_ids.items():
            b = re.sub(r"(fileID: )%s\}" % o, r"\g<1>%s}" % n, b)
            b = re.sub(r"(&)%s\n" % o, r"\g<1>%s\n" % n, b)
        copies.append(b)
    new = "".join(copies)
    new = new.replace("  m_Name: %s\n" % SOURCE, "  m_Name: %s\n" % NAME, 1)
    new = new.replace("  itemName: %s\n" % SOURCE, "  itemName: %s\n" % NAME)
    new = re.sub(r"  price: \d+\n", "  price: %d\n" % PRICE, new)
    new = new.replace("  upgradeFoodType: IceCream\n", "  upgradeFoodType: Oven\n")
    new = re.sub(r"  popupInfoText: '[^']*'\n", "  popupInfoText: '$ %d\n\n    One more oven in the kitchen'\n" % PRICE, new)
    new = re.sub(r"  purchaseButtonText: .*\n", "  purchaseButtonText: $ %d\n" % PRICE, new)
    new = re.sub(r"  specialNote: .*\n", "  specialNote: Any oven bakes any recipe\n", new)
    new = re.sub(r"  m_text: .*\n", "  m_text: $ %d\n" % PRICE, new)
    new = re.sub(r"  m_Sprite: \{fileID: 21300000, guid: [0-9a-f]{32}, type: 3\}\n",
                 "  m_Sprite: {fileID: 21300000, guid: %s, type: 3}\n" % icon_guid, new, count=1)
    assert "itemType: 2" in new and "upgradeFoodType: Oven" in new and icon_guid in new and "$ %d" % PRICE in new
    # the card's RectTransform joins the container's children, right after the source card's
    s = s.replace("  - {fileID: %s}\n" % src_rt, "  - {fileID: %s}\n  - {fileID: %s}\n" % (src_rt, new_ids[src_rt]), 1)
    if not s.endswith("\n"):
        s += "\n"
    s += new
    open(SCENE, "w").write(s)
    print("card added: object", new_ids[go], "price", PRICE, "copied", len(ids), "objects")


if __name__ == "__main__":
    main()

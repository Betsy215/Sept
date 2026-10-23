#!/usr/bin/python3
"""Adds the "Two more flavours" card to the Shop scene (22 October 2026): a copy of the Coconut card's objects with
new fileIDs, as an Upgrade of "IceCream" (SessionManager level 2 opens wells 3 and 4 of the ice cream station).
Idempotent: a second run finds the card by name and does nothing. Price and texts are set here.

  python3 Docs/tools/icecream_upgrade_card.py
"""
import os, re, random

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SCENE = os.path.join(ROOT, "Assets/Scenes/Shop.unity")
ICON_META = os.path.join(ROOT, "Assets/Images/icecream/upgrade_icon.png.meta")
CONTAINER_RT = "367766245"
COCONUT = {"go": "281257709", "rt": "281257710", "ctl": "281257711", "cr": "281257712",
           "icon_go": "191538538", "icon_rt": "191538539", "icon_cr": "191538543", "icon_img": "191538542", "icon_btn": "191538541", "icon_shadow": "191538540",
           "price_go": "1077775573", "price_rt": "1077775574", "price_cr": "1077775577", "price_mr": "1077775576", "price_tmp": "1077775575"}
PRICE = 900
NAME = "IceCreamUpgrade"


def main():
    s = open(SCENE).read()
    if "m_Name: %s\n" % NAME in s:
        print("card already in the scene"); return
    icon_guid = re.search(r"^guid: ([0-9a-f]{32})", open(ICON_META).read(), re.M).group(1)
    used = set(re.findall(r"^--- !u!\d+ &(\d+)", s, re.M))
    rnd = random.Random(2210)
    def fresh():
        while True:
            n = str(rnd.randint(100000000, 1999999999))
            if n not in used:
                used.add(n); return n
    ids = {old: fresh() for old in COCONUT.values()}
    blocks = re.split(r"(?m)^(?=--- !u!)", s)
    copies = []
    for old in COCONUT.values():
        b = next(b for b in blocks if re.match(r"--- !u!\d+ &%s\n" % old, b))
        for o, n in ids.items():
            b = re.sub(r"(fileID: )%s\}" % o, r"\g<1>%s}" % n, b)
            b = re.sub(r"(&)%s\n" % o, r"\g<1>%s\n" % n, b)
        copies.append(b)
    new = "".join(copies)
    new = new.replace("  m_Name: Coconut\n", "  m_Name: %s\n" % NAME, 1)
    new = new.replace("  itemName: Coconut\n", "  itemName: %s\n" % NAME)
    new = new.replace("  price: 700\n", "  price: %d\n" % PRICE)
    new = new.replace("  itemType: 0\n", "  itemType: 2\n")
    new = new.replace("  upgradeFoodType: \n", "  upgradeFoodType: IceCream\n")
    new = re.sub(r"  popupInfoText: '\$ 700\n\n    Fresh young coconut'\n",
                 "  popupInfoText: '$ %d\n\n    Chocolate and mint at the ice cream station'\n" % PRICE, new)
    new = new.replace("  purchaseButtonText: $ 700\n", "  purchaseButtonText: $ %d\n" % PRICE)
    new = new.replace("  specialNote: \n", "  specialNote: Four flavours in a row\n")
    new = new.replace("  m_text: $ 700\n", "  m_text: $ %d\n" % PRICE)
    new = re.sub(r"  m_Sprite: \{fileID: 21300000, guid: [0-9a-f]{32}, type: 3\}\n",
                 "  m_Sprite: {fileID: 21300000, guid: %s, type: 3}\n" % icon_guid, new, count=1)
    assert "itemType: 2" in new and "upgradeFoodType: IceCream" in new and icon_guid in new and "$ %d" % PRICE in new
    # the card's RectTransform joins the ItemContainer's children, right after the Coconut card's
    s = s.replace("  - {fileID: %s}\n" % COCONUT["rt"], "  - {fileID: %s}\n  - {fileID: %s}\n" % (COCONUT["rt"], ids[COCONUT["rt"]]), 1)
    if not s.endswith("\n"):
        s += "\n"
    s += new
    open(SCENE, "w").write(s)
    print("card added: object", ids[COCONUT["go"]], "controller", ids[COCONUT["ctl"]], "price", PRICE)


if __name__ == "__main__":
    main()

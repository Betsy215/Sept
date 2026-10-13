"""Adds the Cookies food (a plate of three heart cookies) to the three scenes by YAML, 12 October 2026, following
Docs/systems notes and the Choux/apples objects as templates. Run once from the project root with the editor closed.
- GameSceneOne.unity: a Cookies table object (copy of Choux: ServeableItem, SpriteRenderer, BoxCollider, RefillableItem,
  Cookies), listed in LevelManager.serveableItems, OrderSystem.availableFoods and ScoreManager.itemPoints (30 points)
- Prefabs/Cookies.prefab: the order-bubble icon (copy of Choux.prefab with the single cookie sprite)
- KitchenScene.unity: a cookies plate in the empty cupboard slot cabright (copy of apples: tap to refill, 8 s respawn)
- Shop.unity: a Cookies card (copy of the Choux card) in a new row, $600
Script guids: Cookies.cs 619300d1bbb44910a6a053a5c4d9d2d4, CookiesInKitchen.cs 181b83381ab44205877c07f9120d4a87."""
import re, uuid, os
K={"k0":"1b29ca62d46747f599a707535114dbed","k1":"a101439079ba4050a3d831b09a031f1e","k2":"bd5918b883494ecfac5f3afe4568f454","k3":"bae750bfa85b4dcd8ea4964c7efa7fcb","icon":"f4ba16f083134c2fad7c924c3773d6cb"}
COOKIES_CS="619300d1bbb44910a6a053a5c4d9d2d4"; KITCHEN_CS="181b83381ab44205877c07f9120d4a87"
def docs(path):
    s=open(path).read(); parts=s.split("\n--- !u!"); head=parts[0]; out=[]
    for p in parts[1:]:
        m=re.match(r"(\d+) &(\d+)( stripped)?\n",p); out.append([int(m.group(1)),int(m.group(2)),bool(m.group(3)),p[m.end():]])
    return head,out
def dump(path,head,ds):
    open(path,"w").write(head+"".join(f"\n--- !u!{t} &{i}{' stripped' if st else ''}\n{b}" for t,i,st,b in ds))
def ids_used(ds): return {i for _,i,_,_ in ds}
def fresh(ds,start):
    used=ids_used(ds); i=start
    while True:
        if i not in used: used.add(i); yield i
        i+=1
def sub(b,old,new,count=1):
    assert old in b,(old[:60]); return b.replace(old,new,count)
def copy_block(ds,anchors,newids):
    """Copy the docs with these anchors, renumbering every reference among them."""
    sel=[d for d in ds if d[1] in anchors]; mp=dict(zip(anchors,newids)); out=[]
    for t,i,st,b in sel:
        for o,n in mp.items(): b=re.sub(rf"\b{o}\b",str(n),b)
        out.append([t,mp[i],st,b])
    return out
# ---------- GameSceneOne ----------
P="Assets/Scenes/GameSceneOne.unity"; head,ds=docs(P); gen=fresh(ds,1900000100)
old=[579974205,579974206,579974207,579974208,579974209,579974210,579974211]; new=[next(gen) for _ in old]
blk=copy_block(ds,old,new); go,serv,sr,tr,col,refill,sprite=blk
go[3]=sub(go[3],"m_Name: Choux","m_Name: Cookies")
serv[3]=sub(serv[3],"foodType: Choux","foodType: Cookies")
sr[3]=sub(sr[3],"guid: bb670d1bb92294ed7bf3f721bdf9d4e7","guid: "+K["k3"]); sr[3]=sub(sr[3],"m_Size: {x: 20.48, y: 20.48}","m_Size: {x: 6.8, y: 6.8}")
tr[3]=sub(tr[3],"m_LocalPosition: {x: -1.3, y: -0.9, z: -0.052374963}","m_LocalPosition: {x: -1.56, y: -2.32, z: -0.05}"); tr[3]=sub(tr[3],"m_LocalScale: {x: 0.1, y: 0.1, z: 1}","m_LocalScale: {x: 0.22, y: 0.22, z: 1}")
col[3]=sub(col[3],"m_Size: {x: 10, y: 8, z: 0}","m_Size: {x: 5.2, y: 3.6, z: 0}"); col[3]=sub(col[3],"m_Center: {x: 0, y: -1.3, z: 0}","m_Center: {x: 0, y: -0.3, z: 0}")
refill[3]=sub(refill[3],"customMaxCount: 4\n  customStartingCount: 4\n  customRefillTime: -1","customMaxCount: 3\n  customStartingCount: -1\n  customRefillTime: 0")
sprite[3]=sub(sprite[3],"guid: 22898871c47b42dfb4ddd16d343602c6","guid: "+COOKIES_CS)
sprite[3]=re.sub(r"chouxSprites:\n(  - \{fileID: 21300000, guid: [0-9a-f]+, type: 3\}\n)+","cookiesSprites:\n"+"".join(f"  - {{fileID: 21300000, guid: {K[k]}, type: 3}}\n" for k in ("k0","k1","k2","k3")),sprite[3])
at=[k for k,d in enumerate(ds) if d[1]==579974211][0]+1; ds[at:at]=blk
for d in ds:
    if "serveableItems:\n  - {fileID: 1420048312}" in d[3]:
        d[3]=sub(d[3],"  - {fileID: 676287864}\n  scoreManager:",f"  - {{fileID: 676287864}}\n  - {{fileID: {new[1]}}}\n  scoreManager:")
    if "availableFoods:\n  - foodType: Choux" in d[3]:
        d[3]=sub(d[3],"  itemSpacing: 1\n",f"  - foodType: Cookies\n    displayPrefab: {{fileID: 8878342301148696013, guid: {PREFAB_GUID}, type: 3}}\n  itemSpacing: 1\n") if False else d[3]
    if "  - itemType: Choux\n    points: 35\n" in d[3]:
        d[3]=sub(d[3],"  - itemType: Choux\n    points: 35\n","  - itemType: Choux\n    points: 35\n  - itemType: Cookies\n    points: 30\n")
# the bubble prefab
src=open("Assets/Prefabs/Choux.prefab").read()
src=sub(src,"m_Name: Choux","m_Name: Cookies"); src=sub(src,"guid: de236a7a017c44c879856fbb06568942","guid: "+K["icon"])
src=sub(src,"m_LocalScale: {x: 0.2, y: 0.2, z: 1}","m_LocalScale: {x: 0.45, y: 0.45, z: 1}"); src=sub(src,"m_Size: {x: 1.96, y: 1.18}","m_Size: {x: 2.2, y: 2.0}")
open("Assets/Prefabs/Cookies.prefab","w").write(src)
PREFAB_GUID=uuid.uuid4().hex
open("Assets/Prefabs/Cookies.prefab.meta","w").write(f"fileFormatVersion: 2\nguid: {PREFAB_GUID}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for d in ds:
    if "availableFoods:\n  - foodType: Choux" in d[3]:
        d[3]=sub(d[3],"  itemSpacing: 1\n",f"  - foodType: Cookies\n    displayPrefab: {{fileID: 8878342301148696013, guid: {PREFAB_GUID}, type: 3}}\n  itemSpacing: 1\n")
dump(P,head,ds); print("GameSceneOne: cookies object",new[0],"serveable",new[1],"prefab",PREFAB_GUID)
# ---------- KitchenScene ----------
P="Assets/Scenes/KitchenScene.unity"; head,ds=docs(P); gen=fresh(ds,1900000200)
old=[286221122,286221123,286221124,286221125,286221126,286221127]; new=[next(gen) for _ in old]
blk=copy_block(ds,old,new); go,rt,img,cr,script,gate=blk
go[3]=sub(go[3],"m_Name: apples","m_Name: cookies")
rt[3]=sub(rt[3],"m_Father: {fileID: 1306179005}","m_Father: {fileID: 65676405}"); rt[3]=sub(rt[3],"m_SizeDelta: {x: 200, y: 200}","m_SizeDelta: {x: 190, y: 190}")
img[3]=sub(img[3],"guid: 737498d42e2564250a5f0a5b6b763bd2","guid: "+K["k3"])
script[3]=sub(script[3],"guid: 6460af1444f147568b099ba461073dfc","guid: "+KITCHEN_CS)
gate[3]=sub(gate[3],"associatedFoodType: Apple","associatedFoodType: Cookies")
at=[k for k,d in enumerate(ds) if d[1]==286221127][0]+1; ds[at:at]=blk
for d in ds:
    if d[1]==65676405: d[3]=sub(d[3],"m_Children: []",f"m_Children:\n  - {{fileID: {new[1]}}}")
dump(P,head,ds); print("KitchenScene: cookies",new[0])
# ---------- Shop ----------
P="Assets/Scenes/Shop.unity"; head,ds=docs(P); gen=fresh(ds,1900000300)
old=[243475751,243475752,243475753,243475754]; new=[next(gen) for _ in old]
blk=copy_block(ds,old,new); pi=blk[0]
b=pi[3]
b=sub(b,"propertyPath: m_AnchoredPosition.y\n      value: -1488","propertyPath: m_AnchoredPosition.y\n      value: -2680")
b=sub(b,"propertyPath: m_text\n      value: $ 750","propertyPath: m_text\n      value: $ 600")
b=sub(b,"propertyPath: m_Name\n      value: Choux","propertyPath: m_Name\n      value: Cookies")
b=sub(b,"propertyPath: price\n      value: 750","propertyPath: price\n      value: 600")
b=sub(b,"propertyPath: itemName\n      value: Choux","propertyPath: itemName\n      value: Cookies")
b=re.sub(r'propertyPath: popupInfoText\n      value: "\$ 750 +\n        \\nChoux with fresh blueberries"','propertyPath: popupInfoText\n      value: "$ 600                                                                                     \n        \\nThree heart cookies on a plate"',b)
assert "Three heart cookies" in b
b=sub(b,"propertyPath: purchaseButtonText\n      value: $ 750","propertyPath: purchaseButtonText\n      value: $ 600")
b=sub(b,"objectReference: {fileID: 21300000, guid: de236a7a017c44c879856fbb06568942, type: 3}","objectReference: {fileID: 21300000, guid: "+K["icon"]+", type: 3}")
pi[3]=b
at=[k for k,d in enumerate(ds) if d[1]==243475754][0]+1; ds[at:at]=blk
for d in ds:
    if d[1]==367766245: d[3]=sub(d[3],"  - {fileID: 2077912419}\n",f"  - {{fileID: 2077912419}}\n  - {{fileID: {new[1]}}}\n")
dump(P,head,ds); print("Shop: card",new[0])

"""Adds the two patience decorations to Shop.unity as Upgrade cards (12 October 2026): Decor_Vase $400 and
Decor_Bouquet $900, copies of the Choux card (PrefabInstance 243475751) with itemType Upgrade and upgradeFoodType set.
Run once from the project root with the editor closed, after add_cookies.py (the Cookies card sits at y -2680)."""
import re
P="Assets/Scenes/Shop.unity"
ITEMS=[("Decor_Vase","Vase of flowers",400,"f7ebe6a174a34775a134b1f81daf5793",49,-2700,"Vase of flowers: customers wait 1 second longer"),
       ("Decor_Bouquet","Bouquet",900,"c73a07731dfc4713b6bda45f3c94327b",-400,-3280,"Tulip bouquet: customers wait 1 second longer")]
s=open(P).read(); parts=s.split("\n--- !u!"); head=parts[0]; ds=[]
for p in parts[1:]:
    m=re.match(r"(\d+) &(\d+)( stripped)?\n",p); ds.append([int(m.group(1)),int(m.group(2)),bool(m.group(3)),p[m.end():]])
used={d[1] for d in ds}; nxt=1900000400
def fresh():
    global nxt
    while nxt in used: nxt+=1
    used.add(nxt); nxt+=1; return nxt-1
old=[243475751,243475752,243475753,243475754]
def sub(b,o,n): assert o in b,o[:50]; return b.replace(o,n,1)
for key,name,price,icon,x,y,info in ITEMS:
    new=[fresh() for _ in old]; mp=dict(zip(old,new)); blk=[]
    for t,i,st,b in [d for d in ds if d[1] in old]:
        for o,n in mp.items(): b=re.sub(rf"\b{o}\b",str(n),b)
        blk.append([t,mp[i],st,b])
    b=blk[0][3]
    b=sub(b,"propertyPath: m_AnchoredPosition.x\n      value: -400\n      objectReference: {fileID: 0}\n    - target: {fileID: 4111913530658097584, guid: a814ad0453cde486ab048750af0f212e, type: 3}\n      propertyPath: m_AnchoredPosition.y\n      value: -1488",
            f"propertyPath: m_AnchoredPosition.x\n      value: {x}\n      objectReference: {{fileID: 0}}\n    - target: {{fileID: 4111913530658097584, guid: a814ad0453cde486ab048750af0f212e, type: 3}}\n      propertyPath: m_AnchoredPosition.y\n      value: {y}")
    b=sub(b,"propertyPath: m_text\n      value: $ 750",f"propertyPath: m_text\n      value: $ {price}")
    b=sub(b,"propertyPath: m_Name\n      value: Choux",f"propertyPath: m_Name\n      value: {key}")
    b=sub(b,"propertyPath: price\n      value: 750",f"propertyPath: price\n      value: {price}")
    b=sub(b,"propertyPath: itemName\n      value: Choux",f"propertyPath: itemName\n      value: {key}\n      objectReference: {{fileID: 0}}\n    - target: {{fileID: 8818029639339444698, guid: a814ad0453cde486ab048750af0f212e, type: 3}}\n      propertyPath: itemType\n      value: 2\n      objectReference: {{fileID: 0}}\n    - target: {{fileID: 8818029639339444698, guid: a814ad0453cde486ab048750af0f212e, type: 3}}\n      propertyPath: upgradeFoodType\n      value: {key}")
    b=re.sub(r'propertyPath: popupInfoText\n      value: "\$ 750 +\n        \\nChoux with fresh blueberries"',f'propertyPath: popupInfoText\n      value: "$ {price}                                                                                     \n        \\n{info}"',b)
    assert info in b
    b=sub(b,"propertyPath: purchaseButtonText\n      value: $ 750",f"propertyPath: purchaseButtonText\n      value: $ {price}")
    b=sub(b,"objectReference: {fileID: 21300000, guid: de236a7a017c44c879856fbb06568942, type: 3}",f"objectReference: {{fileID: 21300000, guid: {icon}, type: 3}}")
    blk[0][3]=b
    at=max(k for k,d in enumerate(ds) if d[1] in old or d[1]>=1900000300)+1; ds[at:at]=blk
    for d in ds:
        if d[1]==367766245: d[3]=sub(d[3],"  - {fileID: 1580701555}\n",f"  - {{fileID: 1580701555}}\n  - {{fileID: {new[1]}}}\n")
    print(key,"card",new[0])
open(P,"w").write(head+"".join(f"\n--- !u!{t} &{i}{' stripped' if st else ''}\n{b}" for t,i,st,b in ds))

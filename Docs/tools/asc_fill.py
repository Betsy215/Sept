"""Fill App Store Connect version 26.10.09 for Food Truck Cafe. Never submits.
python3 -I asc_fill.py texts|build|screenshots|preview|status"""
import sys, json, hashlib, os, urllib.request, urllib.error, time, subprocess

TOKEN = subprocess.run(["python3", "-I", os.path.join(os.path.dirname(os.path.abspath(__file__)), "asc_token.py")], capture_output=True, text=True).stdout.strip().splitlines()[-1]
A = "https://api.appstoreconnect.apple.com/v1"
APP = "6755611355"
VER = "228b7193-eae5-4e39-a087-c56551df7907"          # 26.10.09
LOC = "83ae5f36-9276-4ac2-9149-2c6581751e10"          # en-US version localization
REVIEW = "3267302a-374d-43ce-b435-348d0721f31d"       # appStoreReviewDetail of 26.10.09
APPINFO_LOC = "fc9d90dc-7da8-45e3-beef-0ffa9dc7844c"  # en-US app info localization (new app info)
SHOTS = "/Users/betzthebest/Desktop/Sept/Store/screenshots/final"
VIDEO = "/Users/betzthebest/Desktop/Sept/Store/video/FoodTruckCafe_appstore_v2.mp4"

def api(method, path, body=None, raw=False):
    req = urllib.request.Request(A + path if path.startswith("/") else path, method=method)
    req.add_header("Authorization", "Bearer " + TOKEN)
    data = None
    if body is not None:
        req.add_header("Content-Type", "application/json"); data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data) as r:
            txt = r.read().decode()
            return json.loads(txt) if txt else {}
    except urllib.error.HTTPError as e:
        err = e.read().decode()
        raise SystemExit(f"{method} {path} -> {e.code}: {err[:800]}")

def upload_asset(kind, set_type, set_id, path):
    """kind: appScreenshots | appPreviews. Creates the reservation, PUTs the chunks, commits with the checksum."""
    size = os.path.getsize(path); name = os.path.basename(path)
    attrs = {"fileName": name, "fileSize": size}
    if kind == "appPreviews": attrs["mimeType"] = "video/mp4"
    res = api("POST", f"/{kind}", {"data": {"type": kind, "attributes": attrs,
             "relationships": {set_type: {"data": {"type": set_type + "s" if not set_type.endswith("s") else set_type, "id": set_id}}}}})
    rid = res["data"]["id"]; ops = res["data"]["attributes"]["uploadOperations"]
    with open(path, "rb") as f: blob = f.read()
    for op in ops:
        chunk = blob[op["offset"]:op["offset"] + op["length"]]
        req = urllib.request.Request(op["url"], data=chunk, method=op["method"])
        for h in op["requestHeaders"]: req.add_header(h["name"], h["value"])
        with urllib.request.urlopen(req) as r: r.read()
    md5 = hashlib.md5(blob).hexdigest()
    api("PATCH", f"/{kind}/{rid}", {"data": {"type": kind, "id": rid, "attributes": {"uploaded": True, "sourceFileChecksum": md5}}})
    print(f"  uploaded {name} ({size} bytes) -> {rid}")
    return rid

DESCRIPTION = """A cute, cheerful bakery on wheels!

Enjoy a fun mix of baking and time management. Run your very own food truck, serve customers from morning to night, and grow your bakery over 24 days.

How to Play:
Bake delicious treats in your mobile kitchen and serve them to your customers before the timer runs out. Arrange your table your way before each day. Earn coins to upgrade your equipment, unlock new recipes, and meet new customers!

Gather Fresh Ingredients:
Pick fresh apples right from the trees, squeeze juice, use coffee beans, and prepare a variety of ingredients to keep your kitchen stocked and ready to go.

Bake with Ovens:
Unlock new ovens as you progress through the game. Match the right oven to each unique bakery item, and serve your treats fresh out of the heat.

Serve the Customers:
Meet a variety of customers, each with their own personality. Grandma tips double, the Chef orders big, the Yoga lady never hurries, and the Businessman tips triple but will not wait. Serve their orders quickly to keep them happy and earn tips.

From Morning to Night:
Each day brings more customers, and the evening slowly turns to night. Finish the day, count your tips, and come back tomorrow.

Shop & Upgrade:
Use the money you earn after completing levels to buy new recipes, upgrade your kitchenware, and invite new regulars to your truck."""
KEYWORDS = "bakery,restaurant,time management,coffee shop,cake,pastry,kitchen,tycoon,diner,chef,dessert,cooking"
PROMO = "Run a tiny bakery truck: brew coffee, bake bread and cake, keep the regulars happy from morning to night. 24 days, new customers, new kitchen upgrades."
WHATS_NEW = "10 more days (24 in all), each with more customers. Evenings now turn to night. A quick tutorial for new cooks. Four regulars with their own habits: Grandma, Chef, Yoga lady and Businessman. Fixes for the oven, serving on Day 2 and the last day."
NOTES = "No sign-in. The tutorial runs on first launch; tap Play to start a day. Ads are Unity Ads, shown after a day ends."
SUBTITLE = "Bake, serve & grow your cafe"

what = sys.argv[1] if len(sys.argv) > 1 else "status"

if what == "texts":
    assert len(KEYWORDS) <= 100 and len(PROMO) <= 170 and len(SUBTITLE) <= 30 and len(DESCRIPTION) <= 4000 and len(WHATS_NEW) <= 4000
    api("PATCH", f"/appStoreVersionLocalizations/{LOC}", {"data": {"type": "appStoreVersionLocalizations", "id": LOC, "attributes": {
        "description": DESCRIPTION, "keywords": KEYWORDS, "promotionalText": PROMO, "whatsNew": WHATS_NEW}}})
    print("version texts set: description", len(DESCRIPTION), "keywords", len(KEYWORDS), "promo", len(PROMO), "whatsNew", len(WHATS_NEW))
    api("PATCH", f"/appStoreVersions/{VER}", {"data": {"type": "appStoreVersions", "id": VER, "attributes": {"copyright": "© 2026 BetzzzGame", "releaseType": "MANUAL"}}})
    print("copyright 2026, release type MANUAL")
    api("PATCH", f"/appStoreReviewDetails/{REVIEW}", {"data": {"type": "appStoreReviewDetails", "id": REVIEW, "attributes": {"notes": NOTES, "demoAccountRequired": False}}})
    print("review notes set")
    api("PATCH", f"/appInfoLocalizations/{APPINFO_LOC}", {"data": {"type": "appInfoLocalizations", "id": APPINFO_LOC, "attributes": {"subtitle": SUBTITLE}}})
    print("subtitle set:", SUBTITLE)

elif what == "build":
    b = api("GET", f"/builds?filter%5Bapp%5D={APP}&sort=-uploadedDate&limit=5&fields%5Bbuilds%5D=version,processingState,uploadedDate,preReleaseVersion&include=preReleaseVersion&fields%5BpreReleaseVersions%5D=version")
    pre = {i["id"]: i["attributes"]["version"] for i in b.get("included", [])}
    chosen = None
    for x in b["data"]:
        pv = pre.get(x["relationships"]["preReleaseVersion"]["data"]["id"])
        print(" build", x["id"], x["attributes"], pv)
        if pv == "26.10.09" and x["attributes"]["version"] == "4" and x["attributes"]["processingState"] == "VALID": chosen = x["id"]
    if not chosen: raise SystemExit("build 26.10.09 (4) not found or not VALID")
    api("PATCH", f"/appStoreVersions/{VER}/relationships/build", {"data": {"type": "builds", "id": chosen}})
    print("build attached:", chosen)

elif what == "screenshots":
    sets = api("GET", f"/appStoreVersionLocalizations/{LOC}/appScreenshotSets?include=appScreenshots")["data"]
    for s in sets:
        api("DELETE", f"/appScreenshotSets/{s['id']}")
        print("deleted old set", s["attributes"]["screenshotDisplayType"])
    plan = [("APP_IPHONE_67", [f"{SHOTS}/iphone-0{i}.png" for i in range(1, 6)]),
            ("APP_IPAD_PRO_3GEN_129", [f"{SHOTS}/ipad-0{i}.png" for i in range(1, 6)])]
    for display, files in plan:
        res = api("POST", "/appScreenshotSets", {"data": {"type": "appScreenshotSets", "attributes": {"screenshotDisplayType": display},
                 "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": LOC}}}}})
        sid = res["data"]["id"]; print("set", display, sid)
        for f in files: upload_asset("appScreenshots", "appScreenshotSet", sid, f)

elif what == "preview":
    ptype = sys.argv[2] if len(sys.argv) > 2 else "IPHONE_67"
    existing = api("GET", f"/appStoreVersionLocalizations/{LOC}/appPreviewSets")["data"]
    for s in existing:
        if s["attributes"]["previewType"] == ptype: api("DELETE", f"/appPreviewSets/{s['id']}"); print("deleted old preview set")
    res = api("POST", "/appPreviewSets", {"data": {"type": "appPreviewSets", "attributes": {"previewType": ptype},
             "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": LOC}}}}})
    sid = res["data"]["id"]; print("preview set", ptype, sid)
    upload_asset("appPreviews", "appPreviewSet", sid, VIDEO)

elif what == "duo":
    sid = "08a37dc4-edc9-4886-aa8d-aeb6f3026f08"  # APP_IPHONE_DUO set created 9 Oct
    for f in [f"{SHOTS}/duo-0{i}.png" for i in range(1, 6)]: upload_asset("appScreenshots", "appScreenshotSet", sid, f)
    try:
        res = api("POST", "/appPreviewSets", {"data": {"type": "appPreviewSets", "attributes": {"previewType": "IPHONE_DUO"},
                 "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": LOC}}}}})
        pid = res["data"]["id"]; print("duo preview set", pid)
        upload_asset("appPreviews", "appPreviewSet", pid, VIDEO)
    except SystemExit as e:
        print("duo preview set not accepted:", e)

elif what == "status":
    v = api("GET", f"/appStoreVersions/{VER}?fields%5BappStoreVersions%5D=versionString,copyright,releaseType,appVersionState")
    print("version", v["data"]["attributes"])
    b = api("GET", f"/appStoreVersions/{VER}/relationships/build"); print("build", b.get("data"))
    l = api("GET", f"/appStoreVersionLocalizations/{LOC}")["data"]["attributes"]
    print({k: (len(v) if isinstance(v, str) else v) for k, v in l.items()})
    for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appScreenshotSets?include=appScreenshots")["data"]:
        print("shots", s["attributes"]["screenshotDisplayType"], len(s["relationships"]["appScreenshots"]["data"]))
    for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appPreviewSets?include=appPreviews")["data"]:
        print("preview set", s["attributes"]["previewType"], len(s["relationships"]["appPreviews"]["data"]))
        for p in api("GET", f"/appPreviewSets/{s['id']}/appPreviews")["data"]:
            a = p["attributes"]; print("   ", a.get("fileName"), a.get("assetDeliveryState", {}).get("state"), a.get("videoUrl") is not None)
    for p in api("GET", f"/appScreenshotSets?filter%5BappStoreVersionLocalization%5D={LOC}&include=appScreenshots")["data"] if False else []:
        pass

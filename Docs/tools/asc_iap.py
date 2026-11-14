"""The Remove Ads in-app purchase in App Store Connect, through the API (14 November 2026).
The owner's yes for the product and its US$1.99 price: 14 November 2026 ("For 1.99 yes"). Never run `submit` here: a first
in-app purchase goes to review together with an app version, on the owner's explicit yes.

python3 -I asc_iap.py status      what exists
python3 -I asc_iap.py create      create the non-consumable, its en-US name and description (the shop's own words), the
                                  US$1.99 price, availability in every territory, and the review screenshot
Each step is skipped when it is already done, so `create` can be run again after a failure."""
import hashlib, json, os, subprocess, sys, urllib.error, urllib.request

A = "https://api.appstoreconnect.apple.com"
APP = "6755611355"
HERE = os.path.dirname(os.path.abspath(__file__))
TOKEN = subprocess.run(["python3", "-I", os.path.join(HERE, "asc_token.py")], capture_output=True, text=True).stdout.strip().splitlines()[-1]
PRODUCT = "com.BetzzzGame.FoodTruckCafe.removeads"
REF_NAME = "Remove Ads"
NAME = "Remove Ads"                       # the shop card's name
DESCRIPTION = "No more ads between days."  # the shop popup's line (ShopManager.EnsureRemoveAdsCard)
PRICE = "1.99"
SHOT = os.path.join(HERE, "..", "..", "Assets", "Screenshots", "iap_review_shop.png")   # 1320 x 2868: Apple refuses sizes that are not a device screenshot (IMAGE_INCORRECT_DIMENSIONS)


def api(method, path, body=None, ok404=False):
    req = urllib.request.Request(A + path.replace("[", "%5B").replace("]", "%5D"), method=method)
    req.add_header("Authorization", "Bearer " + TOKEN)
    data = None
    if body is not None:
        req.add_header("Content-Type", "application/json"); data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data) as r:
            txt = r.read().decode()
            return json.loads(txt) if txt else {}
    except urllib.error.HTTPError as e:
        if ok404 and e.code == 404: return None
        raise SystemExit(f"{method} {path} -> {e.code}: {e.read().decode()[:1200]}")


def find():
    for x in api("GET", f"/v1/apps/{APP}/inAppPurchasesV2?limit=50")["data"]:
        if x["attributes"]["productId"] == PRODUCT: return x
    return None


def status():
    p = find()
    if not p: print("no product"); return None
    i = p["id"]; print("product", i, p["attributes"]["name"], p["attributes"]["inAppPurchaseType"], p["attributes"]["state"])
    for l in api("GET", f"/v2/inAppPurchases/{i}/inAppPurchaseLocalizations")["data"]:
        print("  locale", l["attributes"]["locale"], "|", l["attributes"]["name"], "|", l["attributes"].get("description"), "|", l["attributes"].get("state"))
    sch = api("GET", f"/v2/inAppPurchases/{i}/iapPriceSchedule?include=manualPrices,baseTerritory", ok404=True)
    print("  price schedule:", "none" if not sch else [(x["type"], x["id"][:40]) for x in sch.get("included", [])])
    av = api("GET", f"/v2/inAppPurchases/{i}/inAppPurchaseAvailability", ok404=True)
    print("  availability:", "none" if not av else av["data"]["attributes"])
    sh = api("GET", f"/v2/inAppPurchases/{i}/appStoreReviewScreenshot", ok404=True)
    print("  review screenshot:", "none" if not sh or not sh.get("data") else (sh["data"]["attributes"].get("fileName"), sh["data"]["attributes"].get("assetDeliveryState", {}).get("state")))
    return p


def create():
    p = find()
    if not p:
        p = api("POST", "/v2/inAppPurchases", {"data": {"type": "inAppPurchases", "attributes": {"name": REF_NAME, "productId": PRODUCT, "inAppPurchaseType": "NON_CONSUMABLE"},
                "relationships": {"app": {"data": {"type": "apps", "id": APP}}}}})["data"]
        print("created", p["id"])
    i = p["id"]
    locs = api("GET", f"/v2/inAppPurchases/{i}/inAppPurchaseLocalizations")["data"]
    if not any(l["attributes"]["locale"] == "en-US" for l in locs):
        api("POST", "/v1/inAppPurchaseLocalizations", {"data": {"type": "inAppPurchaseLocalizations", "attributes": {"locale": "en-US", "name": NAME, "description": DESCRIPTION},
            "relationships": {"inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": i}}}}})
        print("en-US text set")
    if not api("GET", f"/v2/inAppPurchases/{i}/iapPriceSchedule", ok404=True):
        pts = api("GET", f"/v2/inAppPurchases/{i}/pricePoints?filter[territory]=USA&limit=800")["data"]
        pt = [x for x in pts if x["attributes"]["customerPrice"] == PRICE]
        if not pt: raise SystemExit("no US price point at " + PRICE)
        api("POST", "/v1/inAppPurchasePriceSchedules", {"data": {"type": "inAppPurchasePriceSchedules",
            "relationships": {"inAppPurchase": {"data": {"type": "inAppPurchases", "id": i}}, "baseTerritory": {"data": {"type": "territories", "id": "USA"}},
                              "manualPrices": {"data": [{"type": "inAppPurchasePrices", "id": "${p1}"}]}}},
            "included": [{"type": "inAppPurchasePrices", "id": "${p1}", "attributes": {"startDate": None},
                          "relationships": {"inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": i}}, "inAppPurchasePricePoint": {"data": {"type": "inAppPurchasePricePoints", "id": pt[0]["id"]}}}}]})
        print("price set: US$" + PRICE, "(proceeds", pt[0]["attributes"].get("proceeds"), ")")
    if not api("GET", f"/v2/inAppPurchases/{i}/inAppPurchaseAvailability", ok404=True):
        terr = []; url = "/v1/territories?limit=200"
        while url:
            d = api("GET", url); terr += [t["id"] for t in d["data"]]; nxt = d.get("links", {}).get("next"); url = nxt.replace(A, "") if nxt else None
        api("POST", "/v1/inAppPurchaseAvailabilities", {"data": {"type": "inAppPurchaseAvailabilities", "attributes": {"availableInNewTerritories": True},
            "relationships": {"inAppPurchase": {"data": {"type": "inAppPurchases", "id": i}}, "availableTerritories": {"data": [{"type": "territories", "id": t} for t in terr]}}}})
        print("available in", len(terr), "territories")
    sh = api("GET", f"/v2/inAppPurchases/{i}/appStoreReviewScreenshot", ok404=True)
    if sh and sh.get("data") and (sh["data"]["attributes"].get("assetDeliveryState") or {}).get("state") == "FAILED":
        api("DELETE", f"/v1/inAppPurchaseAppStoreReviewScreenshots/{sh['data']['id']}"); sh = None; print("failed screenshot removed")
    if not sh or not sh.get("data"):
        raw = open(SHOT, "rb").read()
        r = api("POST", "/v1/inAppPurchaseAppStoreReviewScreenshots", {"data": {"type": "inAppPurchaseAppStoreReviewScreenshots", "attributes": {"fileName": "remove_ads_shop.png", "fileSize": len(raw)},
                "relationships": {"inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": i}}}}})["data"]
        for op in r["attributes"]["uploadOperations"]:
            q = urllib.request.Request(op["url"], data=raw[op["offset"]:op["offset"] + op["length"]], method=op["method"])
            for h in op["requestHeaders"]: q.add_header(h["name"], h["value"])
            urllib.request.urlopen(q).read()
        api("PATCH", f"/v1/inAppPurchaseAppStoreReviewScreenshots/{r['id']}", {"data": {"type": "inAppPurchaseAppStoreReviewScreenshots", "id": r["id"],
            "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(raw).hexdigest()}}})
        print("review screenshot uploaded")
    status()


if __name__ == "__main__":
    {"status": status, "create": create}[sys.argv[1] if len(sys.argv) > 1 else "status"]()

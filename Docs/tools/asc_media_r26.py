"""Media for App Store version 26.10.18 (26 October 2026, owner: "start to draft the release in app connect website once
ready"). Replaces only the iPhone 6.9-inch screenshot set and the iPhone preview; the iPad set copied from the previous
version stays. Never submits.
python3 -I asc_media_r26.py status|screenshots|preview"""
import sys, json, hashlib, os, urllib.request, urllib.error, subprocess

TOKEN = subprocess.run(["python3", "-I", os.path.join(os.path.dirname(os.path.abspath(__file__)), "asc_token.py")], capture_output=True, text=True).stdout.strip().splitlines()[-1]
A = "https://api.appstoreconnect.apple.com/v1"
APP = "6755611355"; VERSION = "26.10.26"
SHOTS = "/Users/betzthebest/Desktop/Sept/Store/screenshots/draft_r26"      # iphone-01..06, 1320 x 2868 (draft_r26.py)
VIDEO = "/Users/betzthebest/Desktop/Sept/Store/video/FoodTruckCafe_preview_r26_draft.mp4"   # 886 x 1920 (cut_r26.py)


def api(method, path, body=None):
    req = urllib.request.Request(A + path if path.startswith("/") else path, method=method)
    req.add_header("Authorization", "Bearer " + TOKEN); data = None
    if body is not None: req.add_header("Content-Type", "application/json"); data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data) as r:
            txt = r.read().decode(); return json.loads(txt) if txt else {}
    except urllib.error.HTTPError as e:
        raise SystemExit(f"{method} {path} -> {e.code}: {e.read().decode()[:800]}")


def upload_asset(kind, set_type, set_id, path):
    size = os.path.getsize(path); name = os.path.basename(path)
    attrs = {"fileName": name, "fileSize": size}
    if kind == "appPreviews": attrs["mimeType"] = "video/mp4"
    res = api("POST", f"/{kind}", {"data": {"type": kind, "attributes": attrs, "relationships": {set_type: {"data": {"type": set_type + "s", "id": set_id}}}}})
    rid = res["data"]["id"]; blob = open(path, "rb").read()
    for op in res["data"]["attributes"]["uploadOperations"]:
        req = urllib.request.Request(op["url"], data=blob[op["offset"]:op["offset"] + op["length"]], method=op["method"])
        for h in op["requestHeaders"]: req.add_header(h["name"], h["value"])
        with urllib.request.urlopen(req) as r: r.read()
    api("PATCH", f"/{kind}/{rid}", {"data": {"type": kind, "id": rid, "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(blob).hexdigest()}}})
    print(f"  uploaded {name} ({size} bytes) -> {rid}")


vers = api("GET", f"/apps/{APP}/appStoreVersions?filter[versionString]={VERSION}")["data"]
if not vers: raise SystemExit(f"{VERSION} does not exist")
VER = vers[0]["id"]
LOC = [l for l in api("GET", f"/appStoreVersions/{VER}/appStoreVersionLocalizations")["data"] if l["attributes"]["locale"] == "en-US"][0]["id"]
what = sys.argv[1] if len(sys.argv) > 1 else "status"

if what == "screenshots":
    for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appScreenshotSets")["data"]:
        if s["attributes"]["screenshotDisplayType"] == "APP_IPHONE_67": api("DELETE", f"/appScreenshotSets/{s['id']}"); print("deleted the old iPhone set")
    sid = api("POST", "/appScreenshotSets", {"data": {"type": "appScreenshotSets", "attributes": {"screenshotDisplayType": "APP_IPHONE_67"},
              "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": LOC}}}}})["data"]["id"]
    for i in range(1, 7): upload_asset("appScreenshots", "appScreenshotSet", sid, f"{SHOTS}/iphone-0{i}.png")
elif what == "preview":
    for ptype in ("IPHONE_67", "IPHONE_DUO"):                                       # both iPhone preview slots carried the old video
        for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appPreviewSets")["data"]:
            if s["attributes"]["previewType"] == ptype: api("DELETE", f"/appPreviewSets/{s['id']}"); print("deleted the old preview", ptype)
        sid = api("POST", "/appPreviewSets", {"data": {"type": "appPreviewSets", "attributes": {"previewType": ptype},
                  "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": LOC}}}}})["data"]["id"]
        upload_asset("appPreviews", "appPreviewSet", sid, VIDEO)

for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appScreenshotSets")["data"]:
    shots = api("GET", f"/appScreenshotSets/{s['id']}/appScreenshots")["data"]
    print("screenshots", s["attributes"]["screenshotDisplayType"], len(shots), [x["attributes"].get("assetDeliveryState", {}).get("state") for x in shots])
for s in api("GET", f"/appStoreVersionLocalizations/{LOC}/appPreviewSets")["data"]:
    pv = api("GET", f"/appPreviewSets/{s['id']}/appPreviews")["data"]
    print("preview", s["attributes"]["previewType"], len(pv), [x["attributes"].get("assetDeliveryState", {}).get("state") for x in pv])

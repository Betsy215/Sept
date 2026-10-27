"""Release a new App Store version of Food Truck Cafe through the App Store Connect API (first written for 26.10.18, 25 October 2026).
Never run `create` or `submit` without the owner's explicit yes in the same conversation.

python3 -I asc_release.py <version> status            what exists for that version (and the newest builds)
python3 -I asc_release.py <version> create            create the version (MANUAL release), copy every text of the live version, set WHATS_NEW
python3 -I asc_release.py <version> build <n>         attach TestFlight build <n> of that version (must be VALID)
python3 -I asc_release.py <version> submit            create the review submission with the version and submit it
python3 -I asc_release.py <version> release           release an approved version (PENDING_DEVELOPER_RELEASE) to the store

Screenshots and previews are copied by App Store Connect from the previous version when a version is created;
use asc_fill.py to replace them. The privacy label has no API. The token helper is asc_token.py beside this file."""
import json, os, subprocess, sys, urllib.error, urllib.request

A = "https://api.appstoreconnect.apple.com/v1"
APP = "6755611355"
TOKEN = subprocess.run(["python3", "-I", os.path.join(os.path.dirname(os.path.abspath(__file__)), "asc_token.py")],
                       capture_output=True, text=True).stdout.strip().splitlines()[-1]

WHATS_NEW = ""   # empty until the owner says yes to the wording (26 October 2026: the draft is in the report's MVP); set it then, or type it in App Store Connect
DESCRIPTION_EDITS = {}  # {"old words": "new words"}; filled only on the owner's tick, e.g. the "two customers" line


def api(method, path, body=None):
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
        raise SystemExit(f"{method} {path} -> {e.code}: {e.read().decode()[:800]}")


def q(path):
    return path.replace("[", "%5B").replace("]", "%5D")


def versions():
    d = api("GET", q(f"/apps/{APP}/appStoreVersions?limit=10&fields[appStoreVersions]=versionString,appVersionState,createdDate,releaseType&include=build&fields[builds]=version"))
    return d["data"]


def find_version(v):
    for x in versions():
        if x["attributes"]["versionString"] == v:
            return x
    return None


def live_version():
    for x in versions():
        if x["attributes"]["appVersionState"] == "READY_FOR_DISTRIBUTION":
            return x
    raise SystemExit("no live version found")


def localization(ver_id):
    d = api("GET", f"/appStoreVersions/{ver_id}/appStoreVersionLocalizations")
    for l in d["data"]:
        if l["attributes"]["locale"] == "en-US":
            return l
    raise SystemExit("no en-US localization")


def builds_of(v):
    d = api("GET", q(f"/builds?filter[app]={APP}&sort=-uploadedDate&limit=10&fields[builds]=version,uploadedDate,processingState,preReleaseVersion&include=preReleaseVersion&fields[preReleaseVersions]=version"))
    pre = {i["id"]: i["attributes"]["version"] for i in d.get("included", [])}
    out = []
    for b in d["data"]:
        pv = pre.get((b["relationships"]["preReleaseVersion"]["data"] or {}).get("id"))
        if pv == v:
            out.append((b["attributes"]["version"], b["attributes"]["processingState"], b["attributes"]["uploadedDate"], b["id"]))
    return out


if __name__ == "__main__":
    v, what = sys.argv[1], (sys.argv[2] if len(sys.argv) > 2 else "status")
    ver = find_version(v)

    if what == "status":
        print("version", v, ":", "none" if not ver else f"{ver['id']} {ver['attributes']['appVersionState']} build {(ver['relationships']['build']['data'] or {}).get('id', 'none')}")
        for b in builds_of(v):
            print("  build", *b)
        if ver:
            loc = localization(ver["id"])
            print("  whatsNew:", (loc["attributes"].get("whatsNew") or "").replace("\n", " / "))

    elif what == "create":
        if ver: raise SystemExit(f"{v} already exists: {ver['id']}")
        live = live_version(); src = localization(live["id"])["attributes"]
        res = api("POST", "/appStoreVersions", {"data": {"type": "appStoreVersions", "attributes": {"platform": "IOS", "versionString": v, "releaseType": "MANUAL", "copyright": "© 2026 BetzzzGame"},
                 "relationships": {"app": {"data": {"type": "apps", "id": APP}}}}})
        vid = res["data"]["id"]; print("created", v, vid)
        loc = localization(vid)
        desc = src["description"]
        for old, new in DESCRIPTION_EDITS.items():
            assert old in desc, old
            desc = desc.replace(old, new)
        attrs = {"description": desc, "keywords": src["keywords"], "promotionalText": src["promotionalText"], "whatsNew": WHATS_NEW,
                 "supportUrl": src.get("supportUrl"), "marketingUrl": src.get("marketingUrl")}
        attrs = {k: val for k, val in attrs.items() if val}
        api("PATCH", f"/appStoreVersionLocalizations/{loc['id']}", {"data": {"type": "appStoreVersionLocalizations", "id": loc["id"], "attributes": attrs}})
        print("texts copied from", live["attributes"]["versionString"], "; whatsNew:", repr(WHATS_NEW) or "left empty", "; description edits:", len(DESCRIPTION_EDITS))
        sets = api("GET", f"/appStoreVersionLocalizations/{loc['id']}/appScreenshotSets")["data"]
        print("screenshot sets on the new version:", [s["attributes"]["screenshotDisplayType"] for s in sets])

    elif what == "build":
        if not ver: raise SystemExit(f"{v} does not exist; run create first")
        n = sys.argv[3]
        chosen = [b for b in builds_of(v) if b[0] == n and b[1] == "VALID"]
        if not chosen: raise SystemExit(f"build {n} of {v} not VALID: {builds_of(v)}")
        api("PATCH", f"/appStoreVersions/{ver['id']}/relationships/build", {"data": {"type": "builds", "id": chosen[0][3]}})
        print("attached build", n, chosen[0][3])

    elif what == "submit":
        if not ver: raise SystemExit(f"{v} does not exist")
        res = api("POST", "/reviewSubmissions", {"data": {"type": "reviewSubmissions", "attributes": {"platform": "IOS"},
                 "relationships": {"app": {"data": {"type": "apps", "id": APP}}}}})
        sid = res["data"]["id"]; print("review submission", sid)
        api("POST", "/reviewSubmissionItems", {"data": {"type": "reviewSubmissionItems",
            "relationships": {"reviewSubmission": {"data": {"type": "reviewSubmissions", "id": sid}},
                              "appStoreVersion": {"data": {"type": "appStoreVersions", "id": ver["id"]}}}}})
        api("PATCH", f"/reviewSubmissions/{sid}", {"data": {"type": "reviewSubmissions", "id": sid, "attributes": {"submitted": True}}})
        print("submitted", v)

    elif what == "release":
        if not ver: raise SystemExit(f"{v} does not exist")
        res = api("POST", "/appStoreVersionReleaseRequests", {"data": {"type": "appStoreVersionReleaseRequests",
                 "relationships": {"appStoreVersion": {"data": {"type": "appStoreVersions", "id": ver["id"]}}}}})
        print("release requested", res["data"]["id"])

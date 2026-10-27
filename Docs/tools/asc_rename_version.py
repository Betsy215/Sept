"""Renames an App Store version that is not yet submitted (26 October 2026, owner: "yes rename", 26.10.18 to 26.10.26).
The build attached later must carry the same version (ProjectSettings bundleVersion).
python3 -I asc_rename_version.py <old> <new>"""
import json, os, subprocess, sys, urllib.request, urllib.error
TOKEN = subprocess.run(["python3", "-I", os.path.join(os.path.dirname(os.path.abspath(__file__)), "asc_token.py")], capture_output=True, text=True).stdout.strip().splitlines()[-1]
A = "https://api.appstoreconnect.apple.com/v1"; APP = "6755611355"


def api(method, path, body=None):
    req = urllib.request.Request(A + path, method=method); req.add_header("Authorization", "Bearer " + TOKEN); data = None
    if body is not None: req.add_header("Content-Type", "application/json"); data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(req, data) as r: t = r.read().decode(); return json.loads(t) if t else {}
    except urllib.error.HTTPError as e: raise SystemExit(f"{method} {path} -> {e.code}: {e.read().decode()[:600]}")


old, new = sys.argv[1], sys.argv[2]
v = api("GET", f"/apps/{APP}/appStoreVersions?filter[versionString]={old}")["data"]
if not v: raise SystemExit(f"{old} not found")
state = v[0]["attributes"]["appStoreState"]
if state != "PREPARE_FOR_SUBMISSION": raise SystemExit(f"{old} is {state}; only a version not yet submitted is renamed")
api("PATCH", f"/appStoreVersions/{v[0]['id']}", {"data": {"type": "appStoreVersions", "id": v[0]["id"], "attributes": {"versionString": new}}})
print("renamed", old, "->", new, v[0]["id"], api("GET", f"/appStoreVersions/{v[0]['id']}")["data"]["attributes"]["versionString"])

"""Print a 15-minute App Store Connect API JWT (ES256, signed with openssl; no pip packages).
Reads the key id and issuer id from ~/.private_keys/asc.json ({"key_id": ..., "issuer_id": ...})
and the key from ~/.private_keys/AuthKey_<key_id>.p8. Neither file is in the repo (it is public).
Usage: TOKEN=$(python3 -I Docs/tools/asc_token.py)"""
import base64, json, os, subprocess, time
cfg = json.load(open(os.path.expanduser("~/.private_keys/asc.json")))
kid, iss = cfg["key_id"], cfg["issuer_id"]
key = os.path.expanduser(f"~/.private_keys/AuthKey_{kid}.p8")
b64 = lambda b: base64.urlsafe_b64encode(b).rstrip(b"=").decode()
hdr = b64(json.dumps({"alg": "ES256", "kid": kid, "typ": "JWT"}).encode()); now = int(time.time())
pl = b64(json.dumps({"iss": iss, "iat": now, "exp": now + 900, "aud": "appstoreconnect-v1"}).encode())
der = subprocess.run(["openssl", "dgst", "-sha256", "-sign", key], input=f"{hdr}.{pl}".encode(), capture_output=True, check=True).stdout
def ri(i):  # one DER INTEGER -> 32 bytes
    l = der[i + 1]; v = der[i + 2:i + 2 + l]; return v.lstrip(b"\x00").rjust(32, b"\x00"), i + 2 + l
r, i = ri(2); s, _ = ri(i)
print(f"{hdr}.{pl}.{b64(r + s)}")

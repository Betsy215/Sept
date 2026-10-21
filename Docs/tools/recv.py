"""Local image receiver for pulling pictures out of ChatGPT / Gemini pages (no download prompt).
Run: python3 Docs/tools/recv.py <outdir> [port]   (default port 8765). In the page, through the JS tool:
  const b = await (await fetch(img.src)).blob(); await fetch('http://127.0.0.1:8765/?name=x.png', {method:'POST', body:b});
Files land in <outdir>/<name>."""
import sys, os
from http.server import BaseHTTPRequestHandler, HTTPServer
from urllib.parse import urlparse, parse_qs
OUT = sys.argv[1] if len(sys.argv) > 1 else "."; PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8765
os.makedirs(OUT, exist_ok=True)
class H(BaseHTTPRequestHandler):
    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*"); self.send_header("Access-Control-Allow-Methods", "POST, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")
    def do_OPTIONS(self):
        self.send_response(204); self._cors(); self.end_headers()
    def do_POST(self):
        q = parse_qs(urlparse(self.path).query); name = os.path.basename(q.get("name", ["image.bin"])[0])
        n = int(self.headers.get("Content-Length", 0)); data = self.rfile.read(n)
        with open(os.path.join(OUT, name), "wb") as f: f.write(data)
        self.send_response(200); self._cors(); self.end_headers(); self.wfile.write(b"ok %d" % n)
        sys.stderr.write("saved %s %d\n" % (name, n))
    def log_message(self, *a): pass
HTTPServer(("127.0.0.1", PORT), H).serve_forever()

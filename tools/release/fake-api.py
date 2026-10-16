#!/usr/bin/env python3
"""A stand-in for the GitHub releases API, for testing the launcher without GitHub.

Usage: tools/release/fake-api.py <root> [port=8765]
<root> holds one folder per release, named by its tag (build-472-a5f0428, ...), each with the files
tools/release/make-release.sh writes. Serves GET /repos/<any>/<any>/releases like GitHub
(newest first) and the files under /dl/<tag>/<name>. Folders added later show up at once.
"""
import http.server, json, os, sys

root = os.path.abspath(sys.argv[1])
port = int(sys.argv[2]) if len(sys.argv) > 2 else 8765


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=root, **kw)

    def do_GET(self):
        path = self.path.split("?")[0]
        if path.startswith("/repos/") and path.endswith("/releases"):
            body = json.dumps(self.releases()).encode()
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if path.startswith("/dl/"):
            self.path = path[3:]
            return super().do_GET()
        self.send_error(404)

    def releases(self):
        out = []
        tags = [t for t in os.listdir(root) if t.startswith("build-")]
        for tag in sorted(tags, key=lambda t: int(t[6:].split("-")[0]), reverse=True):
            d = os.path.join(root, tag)
            notes = open(os.path.join(d, "notes.md")).read() if os.path.exists(os.path.join(d, "notes.md")) else ""
            assets = [{"name": n, "browser_download_url": f"http://127.0.0.1:{port}/dl/{tag}/{n}"}
                      for n in sorted(os.listdir(d)) if n != "notes.md"]
            out.append({"tag_name": tag, "name": "Build " + tag[6:].split("-")[0], "body": notes,
                        "draft": False, "prerelease": False, "assets": assets})
        return out


http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()

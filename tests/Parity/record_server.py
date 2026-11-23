"""Tiny HTTP server that records every request as one JSON line and answers 200 {}."""
import http.server
import json
import sys

log = open(sys.argv[1], "a")


class Handler(http.server.BaseHTTPRequestHandler):
    def _handle(self):
        n = int(self.headers.get("Content-Length") or 0)
        raw = self.rfile.read(n).decode() if n else ""
        try:
            body = json.loads(raw) if raw else None
        except ValueError:
            body = raw
        log.write(json.dumps({
            "method": self.command,
            "path": self.path,
            "auth": self.headers.get("Authorization"),
            "contentType": (self.headers.get("Content-Type") or "").split(";")[0],
            "body": body,
        }, sort_keys=True) + "\n")
        log.flush()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(b"{}")

    do_GET = do_POST = do_PUT = do_PATCH = do_DELETE = _handle

    def log_message(self, *args):
        pass


http.server.HTTPServer(("127.0.0.1", int(sys.argv[2])), Handler).serve_forever()

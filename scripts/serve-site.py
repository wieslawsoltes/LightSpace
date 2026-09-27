#!/usr/bin/env python3
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from functools import partial
from urllib.parse import urlsplit
import argparse
parser=argparse.ArgumentParser();parser.add_argument('--directory',default='artifacts/site');parser.add_argument('--port',type=int,default=4173);args=parser.parse_args()
class Handler(SimpleHTTPRequestHandler):
    extensions_map={**SimpleHTTPRequestHandler.extensions_map,'.wasm':'application/wasm','.mjs':'text/javascript','.js':'text/javascript','.json':'application/json'}
    def do_GET(self):
        if self.path.startswith('/LightSpace/'): self.path=self.path[len('/LightSpace'):]
        super().do_GET()
ThreadingHTTPServer(('127.0.0.1',args.port),partial(Handler,directory=str(Path(args.directory).resolve()))).serve_forever()

#!/usr/bin/env python3
"""Collect the real Uno publication, theme its loader, and stamp provenance."""
from pathlib import Path
import gzip
import json
import os
import shutil
import subprocess
import sys

source, destination = map(Path, sys.argv[1:3])
candidates = [p for p in source.rglob('index.html') if (p.parent / 'uno-config.js').exists() or list(p.parent.glob('package_*'))]
if not candidates:
    candidates = list(source.rglob('index.html'))
if not candidates:
    raise SystemExit('No published Uno index.html was found.')
index = min(candidates, key=lambda p: len(p.parts))
if destination.exists():
    shutil.rmtree(destination)
shutil.copytree(index.parent, destination)

# Only the loading screen is HTML. The application workspace is rendered by Uno.
html = (destination / 'index.html').read_text()
metadata = '''
    <title>LightSpace — photography workspace</title>
    <meta name="description" content="A local-first, non-destructive photography workspace built with Uno Platform and Skia." />
    <meta name="theme-color" content="#1b1b1b" />
    <link rel="icon" href="./lightspace.svg" type="image/svg+xml" />
    <style>
      html, body, #uno-body { background: #1b1b1b; color: #dedede; }
      .uno-loader { background: #1b1b1b !important; color: #dedede; font-family: system-ui, sans-serif; }
      .uno-loader .logo { display: none !important; }
      .uno-loader::before { content: 'LightSpace'; position: absolute; top: 44%; left: 0; right: 0; text-align: center; font-size: 28px; font-weight: 500; letter-spacing: -.7px; }
      .uno-loader::after { content: 'Preparing your photography workspace'; position: absolute; top: calc(44% + 48px); left: 0; right: 0; text-align: center; color: #929292; font-size: 12px; }
      .uno-loader progress { accent-color: #88b6db; }
    </style>
'''
html = html.replace('</head>', metadata + '</head>')
(destination / 'index.html').write_text(html)
(destination / 'index.html.gz').write_bytes(gzip.compress(html.encode(), mtime=0))
# Avoid serving a stale precompressed entry point after modifying the loader.
(destination / 'index.html.br').unlink(missing_ok=True)
(destination / 'lightspace.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><rect width="64" height="64" rx="12" fill="#142d3c"/><text x="12" y="45" font-family="system-ui,sans-serif" font-size="38" fill="#a9d6ef">Ls</text></svg>')
(destination / '.nojekyll').write_text('')
commit = os.environ.get('GITHUB_SHA') or subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
(destination / 'build-info.json').write_text(json.dumps({'name': 'LightSpace', 'commit': commit, 'host': 'Uno WebAssembly', 'unoSdk': '6.7.30', 'skiaSharp': '3.119.4'}))
(destination / '404.html').write_text(html)
print(f'Collected {index.parent} -> {destination}')

#!/usr/bin/env python3
"""Locate Uno's published static application and stamp its commit identity."""
from pathlib import Path
import json, os, shutil, subprocess, sys
source, destination = map(Path, sys.argv[1:3])
candidates = [p for p in source.rglob('index.html') if (p.parent/'uno-config.js').exists() or list(p.parent.glob('package_*'))]
if not candidates: candidates = list(source.rglob('index.html'))
if not candidates: raise SystemExit('No published Uno index.html was found.')
index = min(candidates, key=lambda p: len(p.parts))
if destination.exists(): shutil.rmtree(destination)
shutil.copytree(index.parent, destination)
(destination/'.nojekyll').write_text('')
commit = os.environ.get('GITHUB_SHA') or subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
(destination/'build-info.json').write_text(json.dumps({'name':'LightSpace','commit':commit,'host':'Uno WebAssembly','unoSdk':'6.7.30','skiaSharp':'3.119.4'}))
(destination/'404.html').write_bytes((destination/'index.html').read_bytes())
print(f'Collected {index.parent} -> {destination}')

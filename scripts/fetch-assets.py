#!/usr/bin/env python3
"""Fetch hash-pinned OFL fonts and optional Unsplash demonstration photographs."""
from pathlib import Path
from urllib.request import Request, urlopen
import hashlib, json, os
root = Path(__file__).resolve().parents[1]
output = root / 'src/LightSpace.App/Assets/Fonts'
output.mkdir(parents=True, exist_ok=True)
base = 'https://raw.githubusercontent.com/google/fonts/main/ofl/notosans/'
assets = [('NotoSans%5Bwdth,wght%5D.ttf','NotoSans.ttf','bfb7bb691513f12e734dc346c03a03f784912432d7e3fa8e56efcf906fe86b3d'),('OFL.txt','OFL.txt','cee9892f9f0cc8fe882c9e9537ee6a89621d86ee7ceaf70b02e2b2b1c25c061a')]
def fetch(url):
    with urlopen(Request(url,headers={'User-Agent':'LightSpace-build'}),timeout=40) as response:
        return response.read()
for remote, local, expected in assets:
    path = output / local
    data = path.read_bytes() if path.exists() else fetch(base+remote)
    if hashlib.sha256(data).hexdigest() != expected: raise RuntimeError(f'{local}: upstream font hash changed; review the asset before updating its pinned hash.')
    path.write_bytes(data)
if os.environ.get('LIGHTSPACE_NO_DEMO_PHOTOS') == '1': raise SystemExit(0)
photos = [('01-Alpine-morning','photo-1464822759023-fed622ff2c3b'),('02-Lake-reflections','photo-1470770841072-f978cf4d019e'),('03-Quiet-shores','photo-1501785888041-af3ef285b470'),('04-Wild-country','photo-1472396961693-142e6e269027'),('05-Into-the-valley','photo-1469474968028-56623f02e42e'),('06-Forest-light','photo-1441974231531-c6227db76b6e')]
folder = root / 'src/LightSpace.App/Assets/Samples'; folder.mkdir(parents=True,exist_ok=True)
manifest=[]
for name, image in photos:
    url=f'https://images.unsplash.com/{image}?w=1600&q=85&fit=max&fm=jpg'
    try:
        data=fetch(url)
        if len(data)>8*1024*1024 or data[:2]!=b'\xff\xd8': raise ValueError('Expected a JPEG under 8 MiB.')
        (folder/(name+'.jpg')).write_bytes(data)
        manifest.append({'name':name,'url':url,'license':'https://unsplash.com/license','sha256':hashlib.sha256(data).hexdigest()})
    except Exception as error: print(f'Optional photo {name} unavailable: {error}')
(root/'artifacts').mkdir(exist_ok=True)
(root/'artifacts/demo-assets.json').write_text(json.dumps(manifest,indent=2))
print(f'{len(manifest)} demo photos. If none were downloaded, original procedural samples are used.')

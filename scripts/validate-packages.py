#!/usr/bin/env python3
"""Audit all eight package/symbol pairs without installing or executing them."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath
from zipfile import ZipFile, BadZipFile

PACKAGES = tuple('LightSpace.' + name for name in (
    'Core', 'Storage', 'Catalog', 'Imaging', 'Rendering.Skia', 'Editing', 'Controls', 'Workbench'))
REPOSITORY = 'https://github.com/wieslawsoltes/LightSpace'


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def inspect(path: Path, package_id: str, version: str, commit: str, symbols: bool) -> dict:
    expected_name = f'{package_id}.{version}.{"snupkg" if symbols else "nupkg"}'
    require(path.name == expected_name, f'Unexpected package filename: {path.name}')
    with ZipFile(path) as archive:
        entries = archive.infolist()
        require(len(entries) <= 4096, f'{path.name}: excessive entry count')
        names = [entry.filename for entry in entries]
        require(len(set(n.casefold() for n in names)) == len(names), f'{path.name}: duplicate archive path')
        for name in names:
            p = PurePosixPath(name)
            require(not p.is_absolute() and '..' not in p.parts and '\\' not in name,
                    f'{path.name}: unsafe archive path')
        require(sum(e.file_size for e in entries) <= 128 * 1024 * 1024, f'{path.name}: oversized archive')
        specs = [n for n in names if n.endswith('.nuspec')]
        require(len(specs) == 1, f'{path.name}: expected one nuspec')
        spec = archive.read(specs[0])
        require(len(spec) <= 1024 * 1024 and b'<!DOCTYPE' not in spec.upper(), f'{path.name}: invalid nuspec')
        metadata = ET.fromstring(spec).find('{*}metadata')
        require(metadata is not None, f'{path.name}: missing metadata')
        def text(name: str) -> str:
            return metadata.findtext('{*}' + name) or ''
        require(text('id') == package_id and text('version') == version, f'{path.name}: wrong ID or version')
        repository = metadata.find('{*}repository')
        require(repository is not None and repository.get('type') == 'git'
                and repository.get('url') == REPOSITORY and repository.get('commit') == commit,
                f'{path.name}: wrong repository commit')
        frameworks = {'net10.0-browserwasm1.0', 'net10.0-desktop1.0'} if package_id in (
            'LightSpace.Controls', 'LightSpace.Workbench') else {'net10.0'}
        suffix = '.pdb' if symbols else '.dll'
        payloads = [n for n in names if n.startswith('lib/') and n.endswith('/' + package_id + suffix)]
        require({PurePosixPath(n).parts[1] for n in payloads} == frameworks and len(payloads) == len(frameworks),
                f'{path.name}: missing or unexpected framework payload')
        for name in payloads:
            require(archive.getinfo(name).file_size > 0, f'{path.name}: empty assembly or symbols')
        for dependency in metadata.findall('.//{*}dependency'):
            if (dependency.get('id') or '').startswith('LightSpace.'):
                require(dependency.get('id') in PACKAGES and dependency.get('version') == version,
                        f'{path.name}: mixed internal dependency versions')
        if symbols:
            require(any(p.get('name') == 'SymbolsPackage' for p in metadata.findall('.//{*}packageType')),
                    f'{path.name}: missing symbols package type')
        else:
            license_element = metadata.find('{*}license')
            require(license_element is not None and license_element.get('type') == 'expression'
                    and license_element.text == 'MIT', f'{path.name}: missing MIT license expression')
            require(text('readme') == 'README.md' and 'README.md' in names
                    and text('icon') == 'icon.png' and 'icon.png' in names,
                    f'{path.name}: missing packaged documentation/icon')
        # Read every member to verify its CRC; do not extract or execute package payloads.
        require(archive.testzip() is None, f'{path.name}: damaged archive')
    return {'file': path.name, 'id': package_id, 'version': version, 'commit': commit,
            'symbols': symbols, 'frameworks': sorted(frameworks), 'bytes': path.stat().st_size,
            'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def audit(directory: Path, version: str, commit: str) -> dict:
    require(bool(re.fullmatch(r'[0-9a-f]{40}', commit)), 'Expected a full lowercase Git commit SHA')
    require(bool(re.fullmatch(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', version)), 'Invalid package version')
    expected = {f'{p}.{version}.{suffix}' for p in PACKAGES for suffix in ('nupkg', 'snupkg')}
    actual = {p.name for p in directory.iterdir() if p.suffix in ('.nupkg', '.snupkg')}
    require(actual == expected, f'Package set mismatch; missing={sorted(expected-actual)} extra={sorted(actual-expected)}')
    records = [inspect(directory / f'{p}.{version}.{suffix}', p, version, commit, suffix == 'snupkg')
               for p in PACKAGES for suffix in ('nupkg', 'snupkg')]
    return {'version': version, 'commit': commit, 'packages': 8, 'symbolPackages': 8,
            'scope': 'Archive CRC, metadata, payload presence and commit provenance; not assembly execution or NuGet publication.',
            'artifacts': records}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('artifacts/packages'))
    parser.add_argument('--version')
    parser.add_argument('--commit', required=True)
    parser.add_argument('--output', type=Path, default=Path('artifacts/package-validation.json'))
    args = parser.parse_args()
    try:
        version = args.version or ET.parse(Path(__file__).resolve().parents[1] / 'Directory.Build.props').findtext('.//Version')
        require(version is not None, 'Missing source version')
        result = audit(args.directory, version, args.commit)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
        print(f'Validated 8 packages and 8 symbol packages at {args.commit} ({version}).')
        return 0
    except (ValueError, OSError, ET.ParseError, BadZipFile) as error:
        print(f'Package audit failed: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())

"""Fault-injection tests for the packaging gate; no network or .NET runtime."""
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('package_audit', ROOT / 'scripts/validate-packages.py')
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
VERSION = '0.8.0-alpha.1'
COMMIT = 'c' * 40


class PackageAuditTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        for name in audit.PACKAGES:
            for symbols in (False, True):
                extension = 'snupkg' if symbols else 'nupkg'
                path = self.directory / f'{name}.{VERSION}.{extension}'
                frameworks = ['net10.0-browserwasm1.0', 'net10.0-desktop1.0'] if name in (
                    'LightSpace.Controls', 'LightSpace.Workbench') else ['net10.0']
                metadata = (f'<package xmlns="urn:nuspec"><metadata><id>{name}</id><version>{VERSION}</version>'
                            f'<repository type="git" url="{audit.REPOSITORY}" commit="{COMMIT}"/>'
                            '<license type="expression">MIT</license><readme>README.md</readme><icon>icon.png</icon>'
                            '<packageTypes><packageType name="SymbolsPackage"/></packageTypes>'
                            f'<dependencies><group><dependency id="LightSpace.Core" version="{VERSION}"/></group></dependencies>'
                            '</metadata></package>')
                with ZipFile(path, 'w') as archive:
                    archive.writestr(name + '.nuspec', metadata)
                    archive.writestr('README.md', 'Test fixture')
                    archive.writestr('icon.png', b'fixture')
                    for framework in frameworks:
                        archive.writestr(f'lib/{framework}/{name}.{"pdb" if symbols else "dll"}', b'fixture')

    def mutate(self, transform, name='LightSpace.Core', symbols=False):
        path = self.directory / f'{name}.{VERSION}.{"snupkg" if symbols else "nupkg"}'
        with ZipFile(path) as archive:
            contents = {n: archive.read(n) for n in archive.namelist()}
        transform(contents)
        with ZipFile(path, 'w') as archive:
            for n, content in contents.items():
                archive.writestr(n, content)

    def invalid(self):
        with self.assertRaises(ValueError):
            audit.audit(self.directory, VERSION, COMMIT)

    def change_spec(self, before, after, **kwargs):
        def change(files):
            key = next(n for n in files if n.endswith('.nuspec'))
            files[key] = files[key].replace(before.encode(), after.encode())
        self.mutate(change, **kwargs)

    def test_complete_pair_set(self):
        result = audit.audit(self.directory, VERSION, COMMIT)
        self.assertEqual(result['packages'], 8)
        self.assertEqual(len(result['artifacts']), 16)
        self.assertTrue(all(len(a['sha256']) == 64 for a in result['artifacts']))
        json.dumps(result)

    def test_missing_symbols(self):
        next(self.directory.glob('*.snupkg')).unlink()
        self.invalid()

    def test_wrong_commit(self):
        self.change_spec(COMMIT, 'b' * 40)
        self.invalid()

    def test_wrong_symbol_commit(self):
        self.change_spec(COMMIT, 'b' * 40, symbols=True)
        self.invalid()

    def test_mixed_internal_dependency(self):
        self.change_spec(f'version="{VERSION}"', 'version="0.1.0"')
        self.invalid()

    def test_missing_desktop_assembly(self):
        self.mutate(lambda f: f.pop('lib/net10.0-desktop1.0/LightSpace.Controls.dll'), name='LightSpace.Controls')
        self.invalid()

    def test_empty_payload(self):
        self.mutate(lambda f: f.update({'lib/net10.0/LightSpace.Core.dll': b''}))
        self.invalid()

    def test_wrong_repository(self):
        self.change_spec(audit.REPOSITORY, 'https://example.invalid/other')
        self.invalid()

    def test_missing_packaged_readme(self):
        self.mutate(lambda f: f.pop('README.md'))
        self.invalid()

    def test_wrong_license(self):
        self.change_spec('>MIT<', '>Other<')
        self.invalid()

    def test_unsafe_archive_path(self):
        self.mutate(lambda f: f.update({'../outside': b'not extracted'}))
        self.invalid()

    def test_short_commit_rejected(self):
        with self.assertRaises(ValueError):
            audit.audit(self.directory, VERSION, COMMIT[:8])


if __name__ == '__main__':
    unittest.main()

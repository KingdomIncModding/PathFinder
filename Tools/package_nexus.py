"""Package only the installed, runtime-verified Path Finder payload and source."""
from pathlib import Path, PurePosixPath
import hashlib
import json
import re
import tempfile
import xml.etree.ElementTree as ET
import zipfile

PROJECT = Path(__file__).resolve().parents[1]
GAME = PROJECT.parents[1]
OUT = PROJECT / 'Releases' / 'Nexus'
VERSION = ET.parse(PROJECT / 'PathFinder.csproj').getroot().findtext('PropertyGroup/Version')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def archive(path, entries):
    with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
        for name, data in sorted(entries.items()):
            parts = PurePosixPath(name)
            assert not parts.is_absolute() and '..' not in parts.parts and ':' not in name
            info = zipfile.ZipInfo(name, (2026, 11, 2, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            output.writestr(info, data)
    with zipfile.ZipFile(path) as check:
        assert check.testzip() is None
        assert set(check.namelist()) == set(entries)
        with tempfile.TemporaryDirectory(prefix='PackageExtract-', dir=PROJECT / 'Verification') as tmp:
            root = Path(tmp).resolve()
            assert root.parent == (PROJECT / 'Verification').resolve()
            check.extractall(root)
            actual = {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob('*') if p.is_file()}
            assert actual == entries, 'Clean extraction differs from explicit payload'


def main():
    plugin_source = (PROJECT / 'PathFinderPlugin.cs').read_text()
    assert VERSION == re.search(r'Version = "([^"]+)"', plugin_source).group(1)
    dll = (PROJECT / 'bin/Release/netstandard2.1/PathFinder.dll').read_bytes()
    assert dll == (GAME / 'BepInEx/plugins/PathFinder/PathFinder.dll').read_bytes()
    reports = [json.loads((PROJECT / 'Verification' / name).read_text())
               for name in ('runtime-tests.json', 'compatibility-tests.json')]
    for report in reports:
        assert report['passed'] and report['count'] >= 100 and not report['error']
        assert report['version'] == VERSION and report['dllSha256'] == sha(dll)
    assert reports[0]['leylineCompatibility'] is False
    assert reports[1]['leylineCompatibility'] is True
    assert f"Path Finder diagnostic passed: {reports[0]['count']} checks." in (PROJECT / 'Verification/unity-isolated.log').read_text()
    OUT.mkdir(parents=True, exist_ok=True)
    payload = {'BepInEx/plugins/PathFinder/PathFinder.dll': dll}
    for name in ('README.txt', 'CHANGELOG.txt'):
        payload['Path Finder ' + name] = (PROJECT / 'Nexus' / name).read_bytes()
    player_zip = OUT / f'KingdomInc-PathFinder-{VERSION}.zip'
    archive(player_zip, payload)

    # Never recurse through Verification's runtime junctions or include game binaries.
    names = ['PathFinder.csproj', 'PathFinderPlugin.cs', 'RainbowPath.cs', 'PathLaneLayout.cs', 'PathPin.cs',
             'RuntimeCheck.cs', 'LeylineCompatibility.cs', 'ConduitHighlight.cs', 'LeylineRuntimeCheck.cs', 'README.md', '.gitignore', 'install.ps1', 'verify-isolated.ps1',
             'Tools/package_nexus.py', 'Nexus/README.txt', 'Nexus/CHANGELOG.txt',
             'Nexus/NEXUS_DESCRIPTION.bbcode.txt', 'Nexus/SUMMARY.txt',
             'Assets/path-finder-hover.png', 'Assets/path-finder-pin-styles.png']
    sources = {'_Mods/PathFinder/' + name: (PROJECT / name).read_bytes() for name in names}
    source_zip = OUT / f'KingdomInc-PathFinder-{VERSION}-source.zip'
    archive(source_zip, sources)
    for name in ('NEXUS_DESCRIPTION.bbcode.txt', 'CHANGELOG.txt'):
        (OUT / name).write_bytes((PROJECT / 'Nexus' / name).read_bytes())
    (OUT / 'SUMMARY.txt').write_bytes((PROJECT / 'Nexus/SUMMARY.txt').read_bytes())
    screenshots = OUT / 'Screenshots'; screenshots.mkdir(exist_ok=True)
    for name in ('path-finder-hover.png', 'path-finder-pinned.png', 'path-finder-clear.png', 'path-finder-pin-styles.png', 'path-finder-overlapping-paths.png', 'path-finder-leyline.png'):
        (screenshots / name).write_bytes((PROJECT / 'Verification' / name).read_bytes())
    archives = {p.name: {'bytes': p.stat().st_size, 'sha256': sha(p.read_bytes())} for p in (player_zip, source_zip)}
    result = {'name': 'Path Finder', 'version': VERSION, 'publication': 'local preparation only',
              'gameVersion': reports[0]['gameVersion'], 'unityVersion': reports[0]['unityVersion'],
              'bepInExVersion': reports[0]['bepInExVersion'], 'dllSha256': sha(dll),
              'referenceGameAssemblySha256': sha((GAME / 'KingdomInc_Data/Managed/Assembly-CSharp.dll').read_bytes()),
              'runtimeChecksPassed': reports[0]['count'], 'companionChecksPassed': reports[1]['count'],
              'leylineCompatibilityVerified': reports[1]['leylineCompatibility'],
              'mainArchiveFiles': sorted(payload), 'sourceArchiveFiles': len(sources),
              'cleanExtractionPassed': True, 'archives': archives}
    (OUT / 'RELEASE_VERIFICATION.json').write_text(json.dumps(result, indent=2) + '\n')
    (OUT / 'SHA256.json').write_text(json.dumps(archives, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()

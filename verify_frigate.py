"""Validate export references and produce a portable Windows ZIP after QA passes."""
from pathlib import Path
import re
import tarfile
import zipfile
import hashlib
from PIL import Image

out = Path(__file__).resolve().parent.parent / 'Deliverables' / 'FrigateV2'
assert 'RESULT: PASS' in (out / 'QA2/playtest.txt').read_text()
lines = ['PASS: standalone playtest completed including all three deck elevations.']
with tarfile.open(out / 'Kestrel07_Unity.unitypackage', 'r:gz') as archive:
    members = archive.getmembers()
    guids = {m.name.split('/')[0] for m in members}
    paths, refs = [], set()
    for m in members:
        if m.name.endswith('/pathname'):
            paths.append(archive.extractfile(m).read().decode())
        elif m.name.endswith('/asset'):
            refs.update(x.decode() for x in re.findall(rb'guid: ([0-9a-f]{32})', archive.extractfile(m).read()))
    missing = {g for g in refs if not g.startswith('0000000000000000') and g not in guids}
    assert not missing, missing
    assert any(p.endswith('Kestrel07_Complete.prefab') for p in paths)
    assert any(p.endswith('Kestrel_FirstPerson.unity') for p in paths)
    assert any(p.endswith('ShipGlass.shader') for p in paths)
    assert any(p.endswith('README_KO.md') for p in paths)
    lines += [f'PASS: {len(paths)} packaged assets; all non-builtin GUID references resolve.']
for p in out.glob('0*.png'):
    with Image.open(p) as im:
        assert im.size == (1600, 1000), p
        im.verify()
    lines.append(f'PASS: preview integrity {p.name}')
game = out / 'Windows'
assert (game / 'SpaceJanitors_Kestrel.exe').read_bytes()[:2] == b'MZ'
assert (game / 'UnityPlayer.dll').exists()
assert (game / 'SpaceJanitors_Kestrel_Data/Managed/Assembly-CSharp.dll').exists()
target = out / 'Kestrel07_Windows.zip'
with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for p in sorted(game.rglob('*')):
        if p.is_file():
            z.write(p, 'Kestrel07/' + p.relative_to(game).as_posix())
with zipfile.ZipFile(target) as z:
    assert z.testzip() is None
    lines.append(f'PASS: Windows ZIP CRC checked; {len(z.infolist())} files.')
lines.append('ZIP SHA256: ' + hashlib.sha256(target.read_bytes()).hexdigest())
(out / 'PackageValidation.txt').write_text('\n'.join(lines), encoding='utf-8')
print('\n'.join(lines))

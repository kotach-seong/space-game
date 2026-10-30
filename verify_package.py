"""Inspect the exported Unity archive without importing user projects."""
import re
import tarfile
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parent.parent / "Deliverables"
with tarfile.open(out / "SpaceJanitors_StarterArt.unitypackage", "r:gz") as archive:
    members = archive.getmembers()
    guids = {m.name.split('/')[0] for m in members}
    paths = {}
    references = set()
    for member in members:
        if member.name.endswith('/pathname'):
            paths[member.name.split('/')[0]] = archive.extractfile(member).read().decode()
        elif member.name.endswith('/asset'):
            blob = archive.extractfile(member).read()
            references.update(g.decode() for g in re.findall(rb'guid: ([0-9a-f]{32})', blob))
    missing = {g for g in references if not g.startswith('0000000000000000') and g not in guids}
    assert not missing, f"Missing referenced assets: {missing}"
    prefabs = [p for p in paths.values() if p.endswith('.prefab')]
    assert len(prefabs) == 8, prefabs
    assert any(p.endswith('README_KO.md') for p in paths.values())
    assert any(p.endswith('ArtShowcase.unity') for p in paths.values())
    print(f"PASS: archive has {len(paths)} assets, 8 prefabs, guide and showcase scene")
    print("PASS: all non-builtin GUID references resolve inside the archive")
for path in sorted(out.glob('*.png')):
    with Image.open(path) as im:
        assert im.size == (1800, 1200)
        extrema = im.getextrema()
        assert any(hi-lo > 40 for lo,hi in extrema), f"Blank render: {path}"
    with Image.open(path) as im:
        im.verify()
    print(f"PASS: render file integrity {path.name}")

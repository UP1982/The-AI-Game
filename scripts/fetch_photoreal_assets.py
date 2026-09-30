#!/usr/bin/env python3
"""Fetch a small, fixed CC0 Poly Haven photographic material collection.

Uses only Poly Haven's catalog and its returned download URLs. Pillow is required;
the Codex bundled Python includes it. No keys, paid generation, or game-server calls.
"""
from __future__ import annotations
import hashlib, io, json, pathlib, urllib.parse, urllib.request
from PIL import Image, ImageOps

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'Assets/Resources/Photoreal'
DOCUMENT = ROOT / 'Documentation/asset-sources.md'
ASSETS = {
    'GroundForest': 'forest_ground_04',
    'Sand': 'coast_sand_04',
    'Stone': 'rock_face_03',
    'Snow': 'snow_02',
    'Bark': 'bark_willow',
}
SKY = 'kloofendal_48d_partly_cloudy'
ALLOWED_HOSTS = {'api.polyhaven.com', 'dl.polyhaven.org'}
USER_AGENT = 'Aether-game-asset-setup/1.0 (local CC0 material import)'
LIMIT = 100_000_000

def fetch(url: str, max_bytes: int = 35_000_000) -> bytes:
    parsed = urllib.parse.urlsplit(url)
    if parsed.scheme != 'https' or parsed.hostname not in ALLOWED_HOSTS:
        raise ValueError('Unexpected asset source domain: ' + url)
    request = urllib.request.Request(url, headers={'User-Agent': USER_AGENT})
    with urllib.request.urlopen(request, timeout=90) as response:
        if urllib.parse.urlsplit(response.url).hostname not in ALLOWED_HOSTS:
            raise ValueError('Unexpected asset redirect')
        data = response.read(max_bytes + 1)
        if len(data) > max_bytes:
            raise ValueError('Asset exceeded configured download size')
        return data

def api(endpoint: str):
    return json.loads(fetch('https://api.polyhaven.com/' + endpoint))

def choose(files, map_name):
    versions = files[map_name]['2k']
    for extension in ('jpg', 'png'):
        if extension in versions:
            return extension, versions[extension]
    raise ValueError('No supported 2K map for ' + map_name)

def digest(data):
    return hashlib.sha256(data).hexdigest()

def download(entry, target=None):
    existing = target.read_bytes() if target and target.exists() else None
    data = existing if existing and hashlib.md5(existing).hexdigest() == entry.get('md5') else fetch(entry['url'])
    if len(data) != entry['size'] or hashlib.md5(data).hexdigest() != entry['md5']:
        raise ValueError('Asset catalog size or MD5 mismatch: ' + entry['url'])
    return data

def save(path, data):
    other_size = sum(p.stat().st_size for p in OUTPUT.iterdir() if p.is_file() and p != path and p.suffix != '.meta')
    if other_size + len(data) > LIMIT:
        raise ValueError('Photoreal assets exceed the 100 MB limit')
    temporary = path.with_suffix(path.suffix + '.download')
    temporary.write_bytes(data)
    temporary.replace(path)


def main():
    print('Fetching CC0 photographic assets from Poly Haven (https://polyhaven.com).', flush=True)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    catalog = api('assets?t=textures')
    hdri_catalog = api('assets?t=hdris')
    records = []
    for prefix, asset_id in ASSETS.items():
        info = catalog[asset_id]
        files = api('files/' + asset_id)
        records.append(f"## {prefix}: [{info['name']}](https://polyhaven.com/a/{asset_id})\n\n"
                       f"Authors: {', '.join(name + ' (' + role + ')' for name, role in info['authors'].items())}. "
                       f"Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), "
                       f"[file metadata](https://api.polyhaven.com/files/{asset_id}).\n\n"
                       '| Local file | Source download | SHA-256 |\n|---|---|---|')
        for label, map_name in [('Diffuse', 'Diffuse'), ('Normal', 'nor_gl')]:
            extension, entry = choose(files, map_name)
            target = OUTPUT / f'{prefix}_{label}.{extension}'
            data = download(entry, target)
            with Image.open(io.BytesIO(data)) as image:
                image.verify()
            save(target, data)
            records.append(f'| `{target.name}` | [Original {map_name} 2K]({entry["url"]}) | `{digest(data)}` |')
            print(f'Saved {target.name}: {len(data):,} bytes', flush=True)
        _, rough_entry = choose(files, 'Rough')
        rough_data = download(rough_entry)
        with Image.open(io.BytesIO(rough_data)) as source:
            rough = source.convert('L')
        black = Image.new('L', rough.size, 0)
        white = Image.new('L', rough.size, 255)
        mask = Image.merge('RGBA', (black, white, white, ImageOps.invert(rough)))
        buffer = io.BytesIO();mask.save(buffer, format='PNG', optimize=True)
        target = OUTPUT / f'{prefix}_Mask.png';data = buffer.getvalue();save(target, data)
        records.append(f'| `{target.name}` | [Roughness input]({rough_entry["url"]}); transformed as described above | `{digest(data)}` |')
        records.append(f'\nRoughness source SHA-256: `{digest(rough_data)}`. Mask dimensions: {rough.width} × {rough.height}.\n')
        print(f'Saved {target.name}: {len(data):,} bytes', flush=True)
    info = hdri_catalog[SKY];files = api('files/' + SKY);entry = files['hdri']['2k']['hdr']
    target = OUTPUT / 'Sky.hdr';data = download(entry, target)
    if not data.startswith((b'#?RADIANCE', b'#?RGBE')):
        raise ValueError('Expected Radiance HDR image')
    save(target, data)
    records.append(f"## Outdoor HDRI: [{info['name']}](https://polyhaven.com/a/{SKY})\n\n"
                   f"Authors: {', '.join(info['authors'])}. Original 2K Radiance HDR, unchanged. "
                   f"[HDRI catalog](https://api.polyhaven.com/assets?t=hdris), [file metadata](https://api.polyhaven.com/files/{SKY}), "
                   f"[download]({entry['url']}).\n\n`Sky.hdr` SHA-256: `{digest(data)}`.\n")
    size = sum(p.stat().st_size for p in OUTPUT.iterdir() if p.is_file() and p.suffix != '.meta')
    text = '''# Photographic asset sources

Downloaded from Poly Haven for Aether. These are existing photographic/scanned surface maps and an outdoor panoramic HDRI, not AI-generated images. No purchase, subscription, or OpenAI API usage was involved.

All source assets are CC0 according to [Poly Haven's official license](https://polyhaven.com/license), which permits commercial use and redistribution. The [public API terms](https://github.com/Poly-Haven/Public-API/blob/master/ToS.md) separately allow catalog/download access; the acquisition script identifies itself with its own User-Agent. Aether ships local files and does not use Poly Haven's live API at runtime. Source previews and website graphics are not included.

## Import and conversion

All five material sets use the original 2K diffuse JPEG and OpenGL normal JPEG from the catalog. Treat diffuse as sRGB, normals as Normal Map/OpenGL (do not flip green), and masks as linear data. Import `Sky.hdr` as a cubemap, preserving HDR values.

Each derived lossless PNG mask packs **R = 0 (nonmetal), G = 255 (neutral AO), B = 255 (detail enabled), A = 255 − roughness**. Roughness is the original 8-bit source map. Only this technical channel conversion was performed; diffuse, normal, and HDRI bytes are unchanged. Individual source MD5 and byte sizes are checked against the provider catalog before saving; SHA-256 hashes below identify the local files and roughness inputs. Ambient occlusion is deliberately neutral rather than inaccurately inferred from albedo.

Reproduce with `python3 scripts/fetch_photoreal_assets.py` using a Python environment with Pillow. Downloads are limited to `api.polyhaven.com` and `dl.polyhaven.org`; the script enforces a 100,000,000-byte local asset cap. Existing source maps are reused only when their catalog MD5 matches.

'''
    DOCUMENT.parent.mkdir(parents=True, exist_ok=True)
    DOCUMENT.write_text(text + f'Final local asset total: **{size:,} bytes ({size / 1_000_000:.2f} MB)**, excluding Unity-generated `.meta` files.\n\n' + '\n'.join(records))
    print(f'PHOTOREAL_ASSETS_READY: {size:,} bytes; source and hash evidence in {DOCUMENT.relative_to(ROOT)}', flush=True)

if __name__ == '__main__':
    main()

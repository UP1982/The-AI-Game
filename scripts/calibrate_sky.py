#!/usr/bin/env python3
"""CPU midpoint estimate of HDRP's upper-hemisphere RGB irradiance convention.

Reads Radiance RGBE directly without tonemapping, image conversion, or dependencies.
This normalizes an HDRI in Unity's convention; it is not a physical measurement.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import math
import pathlib
import re
import struct

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Resources/Photoreal/Sky.hdr'
TARGET = ROOT / 'Assets/Resources/Photoreal/sky-lux.txt'
MARKER = '\n## CPU HDRI normalization\n'


def exact(stream, count):
    data = stream.read(count)
    if len(data) != count:
        raise ValueError('Truncated Radiance HDR data')
    return data


def decode_rgbe(raw):
    stream = io.BytesIO(raw)
    if stream.readline().strip() not in (b'#?RADIANCE', b'#?RGBE'):
        raise ValueError('Expected Radiance RGBE signature')
    headers = {}
    for _ in range(100):
        line = stream.readline()
        if not line:
            raise ValueError('Missing HDR header terminator')
        line = line.strip()
        if not line:
            break
        if b'=' in line:
            key, value = line.split(b'=', 1)
            headers[key] = value
    else:
        raise ValueError('HDR header exceeded 100 lines')
    if headers.get(b'FORMAT') != b'32-bit_rle_rgbe':
        raise ValueError('Only RGBE Radiance data is supported')
    for field in (b'GAMMA', b'EXPOSURE'):
        if field in headers and float(headers[field]) != 1:
            raise ValueError('Unsupported non-unit header ' + field.decode())
    dimensions = re.fullmatch(rb'-Y (\d+) \+X (\d+)', stream.readline().strip())
    if not dimensions:
        raise ValueError('Expected top-to-bottom -Y height +X width orientation')
    height, width = map(int, dimensions.groups())
    if not 8 <= width <= 32767 or not 2 <= height <= 16384 or width != height * 2 or height % 2:
        raise ValueError('Expected even-height 2:1 equirectangular dimensions')
    rows = []
    for _ in range(height):
        magic = exact(stream, 4)
        if magic[:2] != b'\x02\x02' or struct.unpack('>H', magic[2:])[0] != width:
            raise ValueError('Unsupported or invalid Radiance scanline encoding')
        channels = []
        for _ in range(4):
            channel = bytearray()
            while len(channel) < width:
                code = exact(stream, 1)[0]
                if code == 0:
                    raise ValueError('Invalid zero-length RGBE packet')
                count = code - 128 if code > 128 else code
                if len(channel) + count > width:
                    raise ValueError('RGBE packet exceeds scanline width')
                channel.extend(exact(stream, 1) * count if code > 128 else exact(stream, count))
            channels.append(channel)
        rows.append(channels)
    if stream.read().strip():
        raise ValueError('Unexpected trailing HDR data')
    return width, height, rows


def luminance_coefficients():
    candidates = sorted((ROOT / 'Library/PackageCache').glob('com.unity.render-pipelines.core@*/ShaderLibrary/Color.hlsl'))
    if len(candidates) != 1:
        raise ValueError('Expected exactly one installed Unity core Color.hlsl')
    source = candidates[0]
    match = re.search(r'real Luminance\(real3 linearRgb\).*?return dot\(linearRgb, real3\(([^)]+)\)\)', source.read_text(), re.S)
    if not match:
        raise ValueError('Cannot read Unity linear-sRGB luminance coefficients')
    coefficients = tuple(float(value.strip()) for value in match[1].split(','))
    if len(coefficients) != 3 or not all(math.isfinite(value) and value > 0 for value in coefficients):
        raise ValueError('Invalid Unity luminance coefficients')
    return source, coefficients


def integrate(width, height, rows, coefficients):
    dtheta = math.pi / height
    dphi = 2 * math.pi / width
    scales = [0.0] + [math.ldexp(1.0, exponent - 136) for exponent in range(1, 256)]
    sums = [[], [], []]
    for y in range(height // 2):
        theta = (y + .5) * dtheta
        weight = math.cos(theta) * math.sin(theta) * dtheta * dphi
        row = rows[y]
        for channel in range(3):
            sums[channel].append(math.fsum(value * scales[exponent] for value, exponent in zip(row[channel], row[3])) * weight)
    rgb = tuple(math.fsum(channel) for channel in sums)
    value = math.fsum(c * v for c, v in zip(coefficients, rgb))
    if not all(math.isfinite(v) and v >= 0 for v in rgb) or not math.isfinite(value) or value <= 0:
        raise ValueError('HDRI integral must be finite and positive')
    return rgb, value


def synthetic(width=16, height=8, upper=128, lower=128):
    raw = bytearray(f'#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y {height} +X {width}\n'.encode())
    for y in range(height):
        raw.extend(b'\x02\x02' + struct.pack('>H', width))
        for value in [upper if y < height // 2 else lower] * 3 + [129]:
            raw.extend(bytes([128 + width, value]))
    return bytes(raw)


def self_test():
    coefficients = (.2126729, .7151522, .0721750)
    width, height, rows = decode_rgbe(synthetic())
    rgb, value = integrate(width, height, rows, coefficients)
    # Low-resolution midpoint quadrature converges to pi for uniform unit radiance.
    assert all(abs(v - math.pi) / math.pi < .03 for v in rgb)
    bright_lower = decode_rgbe(synthetic(lower=255))
    assert integrate(*bright_lower, coefficients) == (rgb, value)
    bright_upper = decode_rgbe(synthetic(upper=255))
    assert integrate(*bright_upper, coefficients)[1] > value
    for bad in [synthetic()[:-3], synthetic().replace(b'-Y 8 +X 16', b'+Y 8 +X 16'), synthetic() + b'bad']:
        try:
            decode_rgbe(bad)
        except ValueError:
            pass
        else:
            raise AssertionError('Malformed HDR accepted')
    try:
        integrate(*decode_rgbe(synthetic(upper=0)), coefficients)
    except ValueError:
        pass
    else:
        raise AssertionError('Black upper hemisphere should not normalize')
    print('SKY_CALIBRATION_SELF_TEST_PASSED')


def main():
    parser = argparse.ArgumentParser();parser.add_argument('--self-test', action='store_true');args = parser.parse_args()
    self_test()
    if args.self_test:
        return
    color_source, coefficients = luminance_coefficients()
    shaders = sorted((ROOT / 'Library/PackageCache').glob('com.unity.render-pipelines.high-definition@*/Runtime/Sky/HDRISky/IntegrateHDRISky.shader'))
    if len(shaders) != 1 or 'cos(theta)*sin(theta)*coef' not in shaders[0].read_text():
        raise ValueError('Installed HDRP integration formula must be reviewed')
    raw = SOURCE.read_bytes();width, height, rows = decode_rgbe(raw);rgb, value = integrate(width, height, rows, coefficients)
    TARGET.write_text(f'{value:.12g}\n')
    document = ROOT / 'Documentation/asset-sources.md'
    existing = document.read_text().split(MARKER)[0]
    note = f'''\nThe optional `sky-lux.txt` value is **{value:.12g}**, generated by `python3 scripts/calibrate_sky.py` from the unchanged `Sky.hdr` RGBE bytes (SHA-256 `{hashlib.sha256(raw).hexdigest()}`). It is an approximate numerical normalization in Unity's convention, **not measured real-world illumination or a claim of calibrated physical lux**.

The script inspects the installed HDRP source, `{shaders[0].relative_to(ROOT)}`, and integrates the same upper-hemisphere expression: `sum RGB * cos(theta) * sin(theta) * dtheta * dphi`, followed by Unity's linear-sRGB/D65 luminance dot product `{coefficients}` from `{color_source.relative_to(ROOT)}`. RGB integral: `{rgb}`.

This CPU implementation uses all {width} × {height // 2} upper-hemisphere equirectangular texels at their angular midpoints, with `dtheta = pi/{height}` and `dphi = 2*pi/{width}`. HDRP's editor shader instead samples an imported cubemap with angular steps of 0.005 radians, so small differences from sampling, filtering and importer encoding are expected. Radiance mantissas are decoded as `channel * 2^(exponent - 136)` without gamma, exposure adjustment or tonemapping. The supplied file has `GAMMA=1`, `-Y {height} +X {width}` orientation, and no non-unit exposure header. The script rejects unsupported orientation, dimensions, encoding, malformed packets, non-finite or nonpositive integrals, and checks a synthetic constant-radiance hemisphere against pi before every run.
'''
    document.write_text(existing + MARKER + note)
    print(f'SKY_CALIBRATION_READY {value:.12g} RGB={rgb} size={width}x{height}')


if __name__ == '__main__':
    main()

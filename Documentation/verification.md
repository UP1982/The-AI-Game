# Aether verification — September 30, 2026

## HDRP realism upgrade

Unity 6000.3.24f1 / HDRP 17.3 Mac ARM64 build succeeded with zero errors. The native player ran on Apple M5 using Metal; hardware ray tracing correctly reported disabled. Runtime captures confirm visible textured terrain, a normalized photographic HDRI sky and directional daylight, shadows, stone/bark surfaces, individual leaf geometry, smoother rock formations and an 11,480-triangle hooded traveler. This is a realism upgrade to a procedural prototype, not a fully photorealistic finished game: residents and some architecture/ground-cover shapes remain stylized.

Both High and Medium graphics native verification passed all four saved regions: Jade Grove, Golden Dunes, Crystal Gardens and Amber Mesa. Checks include movement, terrain collision, imported models and resident scale, cached greeting, synthetic resident navigation, discovery deduplication, both side-site discoveries, conversation/journal pause-resume, atomic saves, position/orientation restoration and backup recovery. These checks use actual built-player execution with cached OpenAI content and isolated verification saves; they do not buy AI responses or overwrite the normal explorer save.

| Region | Active mesh renderers | Readable mesh triangles | Stationary median / p95 frame time |
| --- | ---: | ---: | --- |
| Jade Grove | 141 | 768,948 | 8.3 / 9.3 ms |
| Golden Dunes | 118 | 82,839 | 8.3 / 8.9 ms |
| Crystal Gardens | 215 | 96,448 | 8.3 / 8.9 ms |
| Amber Mesa | 124 | 81,657 | 8.3 / 8.4 ms |

Each frame-time result samples only 60 stationary frames at 1440×900. These are diagnostics, not a sustained gameplay benchmark or frame-rate guarantee; background build activity may affect results. Terrain heightfield triangles are excluded from mesh totals. Local synthetic navigation planned in 0–1 ms. High evidence is preserved in `HDRP-High/`; the separate Medium pass is in `HDRP-Medium/`.

Backend regression suite: **17 tests passed**, with no paid calls. Independent QA also verified Mac process-lock exclusion/release and Windows launcher syntax. Windows runtime locking, graphics, ray tracing and performance require a Windows test machine.

## Build artifacts

- macOS ARM64: succeeded, **zero errors**, **324,204,097 bytes**, HDRP Metal raster. Both High and Medium final native passes succeeded.
- Windows x64 Mono: succeeded, **zero errors**, **292,172,871 bytes**, HDRP raster. The cross-build excludes unused STP compute kernels; native-resolution TAA remains enabled. Windows execution has not been verified.
- `Build Windows.bat` performs the Windows-native editor build that enables the ray-tracing pipeline asset. That path requires Windows compilation and compatible GPU verification before hardware ray tracing can be claimed.

## AI and spending

Earlier live integration verified initial world generation, two related conversations, a weather/goal/destination response and the fourth region expansion. The ledger remains **6 requests**, **$0.0042772 estimated actual usage**, and **$0.0259276 conservatively reserved** against the authorized **$0.25 total**. No additional API requests, credit purchases or auto-reload changes occurred during this graphics upgrade. The account balance was not confirmed to be promotional credit.

The API key stays in ignored owner-only `.env.local`; it is not embedded in the player. The private world, memory, normal explorer save and spending ledger remain under `.runtime`. Keep that ledger when transferring between platforms and use one active spending copy.

## Limits and evidence

- Native keyboard/mouse Computer Use access was denied earlier. Programmatic built-player verification is complete; interactive playtesting remains separate.
- Finder double-click launch was not interactively tested. Use `Play Aether.command` to supply project path and start the service.
- AI chooses validated region descriptions, seeds, biomes, palettes, landmarks, residents, dialogue, weather and goals. Unity constructs scenes from a bounded procedural vocabulary; unrestricted AI mesh synthesis is not implemented.
- Mac uses HDRP raster/screen-space lighting. The Mac-cross-compiled Windows executable uses raster lighting. Building with the Windows editor enables the optional ray-tracing asset; activation then requires DX12, supported hardware and High graphics.
- Builds are local; no signing/notarization or public distribution is claimed.

See `build-result.txt`, `runtime-check.txt`, `graphics-capabilities.txt`, `scene-complexity.txt`, `photoreal-qa.md` and the region/detail PNGs. `asset-sources.md` records CC0 photographic asset sources and checksums. `windows.md` describes Windows packaging and platform-specific evidence.

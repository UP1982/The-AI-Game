# Photographic asset sources

Downloaded from Poly Haven for Aether. These are existing photographic/scanned surface maps and an outdoor panoramic HDRI, not AI-generated images. No purchase, subscription, or OpenAI API usage was involved.

All source assets are CC0 according to [Poly Haven's official license](https://polyhaven.com/license), which permits commercial use and redistribution. The [public API terms](https://github.com/Poly-Haven/Public-API/blob/master/ToS.md) separately allow catalog/download access; the acquisition script identifies itself with its own User-Agent. Aether ships local files and does not use Poly Haven's live API at runtime. Source previews and website graphics are not included.

## Import and conversion

All five material sets use the original 2K diffuse JPEG and OpenGL normal JPEG from the catalog. Treat diffuse as sRGB, normals as Normal Map/OpenGL (do not flip green), and masks as linear data. Import `Sky.hdr` as a cubemap, preserving HDR values.

Each derived lossless PNG mask packs **R = 0 (nonmetal), G = 255 (neutral AO), B = 255 (detail enabled), A = 255 − roughness**. Roughness is the original 8-bit source map. Only this technical channel conversion was performed; diffuse, normal, and HDRI bytes are unchanged. Individual source MD5 and byte sizes are checked against the provider catalog before saving; SHA-256 hashes below identify the local files and roughness inputs. Ambient occlusion is deliberately neutral rather than inaccurately inferred from albedo.

Reproduce with `python3 scripts/fetch_photoreal_assets.py` using a Python environment with Pillow. Downloads are limited to `api.polyhaven.com` and `dl.polyhaven.org`; the script enforces a 100,000,000-byte local asset cap. Existing source maps are reused only when their catalog MD5 matches.

Final local asset total: **62,740,459 bytes (62.74 MB)**, excluding Unity-generated `.meta` files.

## GroundForest: [Forest Ground 04](https://polyhaven.com/a/forest_ground_04)

Authors: Rob Tuytel (Photography, Processing), Rico Cilliers (Minor adjustment). Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), [file metadata](https://api.polyhaven.com/files/forest_ground_04).

| Local file | Source download | SHA-256 |
|---|---|---|
| `GroundForest_Diffuse.jpg` | [Original Diffuse 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/forest_ground_04/forest_ground_04_diff_2k.jpg) | `24b8aaf4c8547305d80b0e029ec365219ab1b90748d4ca5651bdc9e75bfde5e6` |
| `GroundForest_Normal.jpg` | [Original nor_gl 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/forest_ground_04/forest_ground_04_nor_gl_2k.jpg) | `b37ac799ed410976541e0011c246fca254e74eaa3f096ae7a7d74178db5a6d9b` |
| `GroundForest_Mask.png` | [Roughness input](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/forest_ground_04/forest_ground_04_rough_2k.jpg); transformed as described above | `37ea71511983e3234851d491910b2788dcd1b428e96f6b40ea9bf821cc0c6205` |

Roughness source SHA-256: `24194662948424c323c75e0918461ad9d38bc0a275022000144f248b3efb4119`. Mask dimensions: 2048 × 2048.

## Sand: [Coast Sand 04](https://polyhaven.com/a/coast_sand_04)

Authors: Rob Tuytel (Photography, Processing), Dario Barresi (Processing). Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), [file metadata](https://api.polyhaven.com/files/coast_sand_04).

| Local file | Source download | SHA-256 |
|---|---|---|
| `Sand_Diffuse.jpg` | [Original Diffuse 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/coast_sand_04/coast_sand_04_diff_2k.jpg) | `02744081f9aef538b829b00db8ef9d5a11d25c22fc439d5acb4c4353d7dcd4dc` |
| `Sand_Normal.jpg` | [Original nor_gl 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/coast_sand_04/coast_sand_04_nor_gl_2k.jpg) | `c2cd2fdd652229e0f4978ac7d6db2935d9845bcccb3dadecb01d6005221e17df` |
| `Sand_Mask.png` | [Roughness input](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/coast_sand_04/coast_sand_04_rough_2k.jpg); transformed as described above | `f2cada5744e8c35cf79fb9b5c36f8281a385c8af025d77488cafb7b54b1ba972` |

Roughness source SHA-256: `392c34ec2b154a614074fd9eafd25ad7dfa253c2d60c00bb28bcdad272410da2`. Mask dimensions: 2048 × 2048.

## Stone: [Rock Face 03](https://polyhaven.com/a/rock_face_03)

Authors: Dario Barresi (Photography), Rico Cilliers (Processing). Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), [file metadata](https://api.polyhaven.com/files/rock_face_03).

| Local file | Source download | SHA-256 |
|---|---|---|
| `Stone_Diffuse.jpg` | [Original Diffuse 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_face_03/rock_face_03_diff_2k.jpg) | `644ee79df7ffbf5f6afc330b64dd3f33794bab8820915fe61ffe0c88ecfd7ece` |
| `Stone_Normal.jpg` | [Original nor_gl 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_face_03/rock_face_03_nor_gl_2k.jpg) | `a498cef4ba6191f6959b6d03fb835263c77271948264a289af24497ab7111dec` |
| `Stone_Mask.png` | [Roughness input](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/rock_face_03/rock_face_03_rough_2k.jpg); transformed as described above | `49cda382e2b9cd8f5fe3be2ff74e4c78865cc7d3d37f24528c497e5017403c0b` |

Roughness source SHA-256: `9ca3e9e196685f702328f24d66f039b72f5330fc35cf2a2c265cfa50a7916301`. Mask dimensions: 2048 × 2048.

## Snow: [Snow 02](https://polyhaven.com/a/snow_02)

Authors: Rob Tuytel (All). Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), [file metadata](https://api.polyhaven.com/files/snow_02).

| Local file | Source download | SHA-256 |
|---|---|---|
| `Snow_Diffuse.jpg` | [Original Diffuse 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/snow_02/snow_02_diff_2k.jpg) | `d185c2853c7e674161789db4eed1c5708d85e4f6e3dcb5698209568adf02aa98` |
| `Snow_Normal.jpg` | [Original nor_gl 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/snow_02/snow_02_nor_gl_2k.jpg) | `288539ee33387e91035be4dc8d10da574bf6a95c8374d33b6b1dc465c6949e8f` |
| `Snow_Mask.png` | [Roughness input](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/snow_02/snow_02_rough_2k.jpg); transformed as described above | `cb5742ae89c28068993723b507316c4321162596f2f34543d6a41b0f020a0447` |

Roughness source SHA-256: `9cfa994318ef190d4540875a166c7c745184c1a166e82271bca7c0a628f8820f`. Mask dimensions: 2048 × 2048.

## Bark: [Bark Willow](https://polyhaven.com/a/bark_willow)

Authors: Dario Barresi (Processing), Dimitrios Savva (Photography). Catalog evidence: [texture catalog](https://api.polyhaven.com/assets?t=textures), [file metadata](https://api.polyhaven.com/files/bark_willow).

| Local file | Source download | SHA-256 |
|---|---|---|
| `Bark_Diffuse.jpg` | [Original Diffuse 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/bark_willow/bark_willow_diff_2k.jpg) | `d87e97687eb0abd61031161f2895ab7eafc241c850380e978d011ef98a45a67b` |
| `Bark_Normal.jpg` | [Original nor_gl 2K](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/bark_willow/bark_willow_nor_gl_2k.jpg) | `2984fe9d23f2cfb9b235fc50eea85f514b27be3f4addc7c7142afba077c9970e` |
| `Bark_Mask.png` | [Roughness input](https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/bark_willow/bark_willow_rough_2k.jpg); transformed as described above | `49285365e8c8755601415a99eb983838a4b3e3232196092242e85816d637b412` |

Roughness source SHA-256: `fc3cc861befbd9dcaec295d7ddb54b41c68ecf153a49a18fc8460c25b3f57c49`. Mask dimensions: 2048 × 2048.

## Outdoor HDRI: [Kloofendal 48d Partly Cloudy](https://polyhaven.com/a/kloofendal_48d_partly_cloudy)

Authors: Greg Zaal. Original 2K Radiance HDR, unchanged. [HDRI catalog](https://api.polyhaven.com/assets?t=hdris), [file metadata](https://api.polyhaven.com/files/kloofendal_48d_partly_cloudy), [download](https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/2k/kloofendal_48d_partly_cloudy_2k.hdr).

`Sky.hdr` SHA-256: `3fbd33f279f29bd64925c1dd3214fd46c627493d21f1100248b6b1098da4d06e`.

## CPU HDRI normalization

The optional `sky-lux.txt` value is **4.79258602862**, generated by `python3 scripts/calibrate_sky.py` from the unchanged `Sky.hdr` RGBE bytes (SHA-256 `3fbd33f279f29bd64925c1dd3214fd46c627493d21f1100248b6b1098da4d06e`). It is an approximate numerical normalization in Unity's convention, **not measured real-world illumination or a claim of calibrated physical lux**.

The script inspects the installed HDRP source, `Library/PackageCache/com.unity.render-pipelines.high-definition@700710090fa9/Runtime/Sky/HDRISky/IntegrateHDRISky.shader`, and integrates the same upper-hemisphere expression: `sum RGB * cos(theta) * sin(theta) * dtheta * dphi`, followed by Unity's linear-sRGB/D65 luminance dot product `(0.2126729, 0.7151522, 0.072175)` from `Library/PackageCache/com.unity.render-pipelines.core@0bb36005e9ba/ShaderLibrary/Color.hlsl`. RGB integral: `(4.582717503776926, 4.8139978572267, 5.198822983264697)`.

This CPU implementation uses all 2048 × 512 upper-hemisphere equirectangular texels at their angular midpoints, with `dtheta = pi/1024` and `dphi = 2*pi/2048`. HDRP's editor shader instead samples an imported cubemap with angular steps of 0.005 radians, so small differences from sampling, filtering and importer encoding are expected. Radiance mantissas are decoded as `channel * 2^(exponent - 136)` without gamma, exposure adjustment or tonemapping. The supplied file has `GAMMA=1`, `-Y 1024 +X 2048` orientation, and no non-unit exposure header. The script rejects unsupported orientation, dimensions, encoding, malformed packets, non-finite or nonpositive integrals, and checks a synthetic constant-radiance hemisphere against pi before every run.

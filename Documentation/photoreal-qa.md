# Photoreal / HDRP migration — independent QA

Review in progress. This reviewer reads source and runs no-cost checks; parent owns Editor/build/native launches and visual acceptance. No claim of Windows execution or hardware ray tracing verification may be inferred from source/configuration alone.

## Verified local package compatibility

Installed Unity 6000.3.24f1 bundles HDRP **17.3.0**. References below are official source/docs inside:
`/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/`

- `com.unity.render-pipelines.high-definition/Documentation~/System-Requirements.md` lists macOS Metal and Windows DirectX 11/12; HDRP requires linear color space.
- `Documentation~/Ray-Tracing-Getting-Started.md` limits ray tracing to DirectX 12 on supported Editor/Windows Player hardware (plus separately documented consoles). macOS Metal is the non-ray-traced target here.
- That ray-tracing guide requires disabling Unity Player static batching; game-generated combined meshes are separate from Unity's static-batching setting. It also requires raster counterpart support flags for ray-traced replacement effects.

## Exact creation/initialization APIs checked

- Built-in `Editor/RenderPipeline/HDAssetFactory.cs` creates an asset using `ScriptableObject.CreateInstance<HDRenderPipelineAsset>()` followed by `AssetDatabase.CreateAsset`.
- `HDRenderPipelineAsset.currentPlatformRenderPipelineSettings` has a public getter **and setter**. Modify a local settings copy then assign it back.
- Public fields include `supportSSR`, `supportSSRTransparent`, `supportSSAO`, `supportSSGI`, `supportRayTracing`, `supportedRayTracingMode`, and `hdShadowInitParams`.
- `HDRenderPipelineGlobalSettings` and its `Ensure(bool)` are internal in this package, so direct references from the project's Editor assembly will not compile.
- Public `UnityEngine.Rendering.RenderPipelineGlobalSettingsUtils.Create(Type, path, source = null)` creates/saves the asset, calls `EditorGraphicsSettings.PopulateRenderPipelineGraphicsSettings`, then `Initialize`, marks dirty and saves. Resolve the internal settings type through `typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings")` if needed.
- Public `UnityEditor.Rendering.EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(global)` registers the populated settings. Check/reuse `GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>()` before creating new assets.
- Simply calling `CreateInstance` on global settings skips the population sequence and is insufficient for required resource initialization.

## Acceptance evidence needed

1. macOS build succeeds with HDRP assets/resources included, Linear color space, Metal, compatible lit/unlit shaders and camera/light additional data.
2. All generated region renderers—including batched vegetation, imported models, particles and terrain—use HDRP-compatible materials. Vertex-color geometry must retain meaningful colors; photographic texture sampling needs actual usable UVs/triplanar mapping.
3. Fresh native images show no magenta/error materials, absent geometry, black world caused by exposure/light-unit mistakes, or unreadable UI.
4. Mac runtime records non-ray-traced mode honestly. Windows configuration includes DX12 and RT flags/effects only with runtime capability checks and a raster fallback.
5. Windows package/build outcome is distinct from Windows runtime verification. No Windows GPU/driver/frame-rate or RT-success claim until actually executed on appropriate hardware.
6. Backend platform lock and launcher changes keep the existing global $0.25 spending guard; no new paid tests are necessary for rendering migration.
7. Final native metrics must be refreshed after the migration: the previous Built-in pipeline frame/renderer metrics are not HDRP performance evidence.

## Follow-up source and no-cost checks

- **17 backend tests passed** after the cross-platform import/lock and UTF-8 changes. Tests use fake API transports; no paid generation occurred.
- A separate temporary-directory process test on macOS confirmed the service lock rejects a second process while held and can be acquired after the original handle closes. This validates the Unix branch only; the Windows `msvcrt` branch still needs Windows execution.
- `scripts/launch_windows.py` compiled as Python source. It uses argument arrays, UTF-8 reads, an authenticated project/storage handshake, and only cleans up a service it starts. This is source/syntax validation, not Windows runtime validation.
- Current `Documentation/build-result.txt` reports **Succeeded; errors=0; size=323717013; HDRP 17.3; StandaloneOSX**. The corresponding build log contains `AETHER_BUILD_PASSED`. A subsequent .NET-SDK diagnostic appears during shutdown; it does not erase the recorded successful build. Parent still owns native runtime/visual verification.
- Runtime HDRP quality/RT gates and profile cleanup reviewed: ray effects require WindowsPlayer, DX12, `SystemInfo.supportsRayTracing`, asset support, High quality, the request switch, SSR and AO support. Mac follows raster/screen-space lighting. Runtime profiles/components and environment objects are destroyed on shutdown.
- HDRI calibration was corrected to use measured `sky-lux` metadata when available. If metadata is absent, the runtime uses explicitly logged relative exposure instead of pretending the default measured lux value is calibrated. Fresh images remain necessary to assess actual exposure.
- Tree materials now preserve bark/leaf materials instead of discarding photographic maps through the previous generic tree vertex bake. Main terrain uses HDRP TerrainLit with photographic terrain layers, slope blending, normals and mask maps. Other vertex geometry uses the original HDRP Lit vertex/triplanar graph.
- Material-import source treats diffuse as sRGB, masks as linear, normal maps appropriately and Sky HDR as a cubemap. The texture owner supplied local provenance/hashes and documented neutral AO plus inverted roughness in the packed smoothness channel.
- `git diff --check` passed during this review.

No blocking source problem remains from this snapshot. The initial package-import failure's root cause is not established: a cloud-folder race should not be claimed solely because retrying after package population succeeds. Final report must distinguish macOS build success, native macOS runtime evidence, Windows build success (if produced), Windows runtime untested, and hardware ray tracing untested.

A subsequent material-owner fix explicitly retains the TerrainLit normal/mask shader keywords for runtime-created terrain. The successful macOS build recorded above predates that final retention fix; the parent must rebuild before treating it as final evidence.

## Verified HDRP High native evidence

Independently inspected the parent's refreshed `Documentation/HDRP-High/detail-0.png`, `region-1.png`, `region-2.png`, `region-3.png` and `detail-2.png`, plus the dialogue and eastern side-ruin images. No magenta error shaders, missing terrain, or obvious blocking rendering failures were visible. Ground surface detail, cast shadows, inhabitants, landmark structures, gates and legible UI are present across the four saved regions.

The verified visual result is an **HDRP and photographic-material upgrade**. Characters, small plants and architecture retain visibly stylized/faceted geometry; these images should not be described as completely photorealistic art.

Observed evidence files:

- `HDRP-High/runtime-check.txt`: `AETHER_RUNTIME_CHECKS_PASSED`, including cached-world visits, models/scale, movement/collision, greeting, modal pause/resume, discovery deduplication and save/restore/backup checks. This remains automated evidence, not keyboard/mouse playtesting.
- `HDRP-High/graphics-capabilities.txt`: Apple M5, Metal, OSXPlayer; device/asset/eligible ray tracing all false; HDRP raster/screen-space lighting; **Physical atmosphere**, not an active HDRI environment.
- `HDRP-High/scene-complexity.txt`: Jade Grove 159 active mesh renderers / 735,180 readable mesh triangles; Golden Dunes 136 / 49,071; Crystal Gardens 233 / 62,680; Amber Mesa 142 / 47,889. Terrain heightfield triangles are explicitly excluded from these mesh totals.
- The per-region 60-frame stationary samples report median 8.3 ms and p95 8.4–11.9 ms. Synthetic local navigation records 0–1 ms with no API request. These bounded samples are not comprehensive frame-rate or stress-test results.

No blocker was found in this High-mode snapshot. Mac Medium verification and Windows compilation were still parent-owned work in progress when this image review was recorded. Neither Windows native execution nor hardware ray tracing is verified here.

## Final art integration

After the independent screenshot review, the parent exported an original 11,480-triangle hooded traveler and enabled the photographic HDRI using a CPU-integrated normalization value. The parent inspected fresh grove and tundra captures, then reran both High and Medium native verification successfully across all four regions. `HDRP-High/` and `HDRP-Medium/` now contain those final art passes. macOS reports Metal, raster lighting, ray tracing disabled, and calibrated HDRI environment. “Calibrated” is numerical HDRI normalization in Unity's convention; it is not a measured real-world lighting survey.

The first Windows cross-build reported Succeeded but 64 shader errors for unused DXC-only STP kernels. Build validation was strengthened to reject any shader/build errors. Mac cross-builds now use a raster asset and strip those unused STP kernels; native Windows editor builds enable the ray-tracing asset. Windows build and execution outcomes must be checked separately.

Final Windows cross-build: Succeeded, zero reported errors, 292172871bytes, rayTracingAsset=False. No shader-error messages in final log; unused STP kernels were explicitly stripped. The built exe/runtime files exist. Windows execution and native Windows RT build remain unverified.

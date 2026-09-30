# Richer region detail — independent QA

Source review complete against the settled parallel detail changes. This reviewer does not launch Unity/the native game, call AI, or modify gameplay/art source. Parent owns the final build, native checks, screenshots, and playtest status.

## Acceptance constraints

- Explorer spawn remains `(0, -23)` in x/z with a clear approach toward the landmark.
- Landmark interaction compares 3D distance to `(0, Height(0,14), 14)` against radius **8**. A reachable floor point must remain inside that sphere; decorative collision cannot enclose the whole accessible boundary.
- Gates remain centered at x **-36 / +36**, z **0**, with interaction radius **5** in x/z. Navigation lanes and sight lines should expose both gates.
- Explorer capsule radius is **0.3**, step offset **0.35**, slope limit **55°**. Raised architectural flooring needs a compatible step or ramp and collision matching its visual surface.
- Resident navigation uses a **49×49 grid**, 2-unit spacing, with **0.65** clearance added to every obstacle radius. Fine ground decoration should not add hundreds of separate navigation obstacles.
- Generated meshes must enter the shared mesh ownership list so region changes destroy them. Generated materials must be cached/owned and destroyed with the region; shared imported meshes should not be destroyed.
- Fine vegetation/pebbles should use combined meshes or bounded object counts. The final native pass should record region renderer/triangle/object counts and resident route timing if new detail materially expands them.

## Review status

Terrain/path, landmark/gate, biome-detail, and import changes reviewed. Early collision and navigation-cost constraints were sent to the relevant owners; findings and limits follow.

## Settled-source follow-up

Read-only review completed for `WorldGeometry.cs`, `WorldLandmarks.cs`, `WorldBiomeDetails.cs`, current model import settings, and atmosphere rendering. `git diff --check` is clean; the 17 backend regression tests still pass using fake transports. This does not establish native frame rate or compile success.

### Findings resolved during review

- Island cliff skirt winding was inward in an intermediate snapshot. Final winding is `top,next,low` / `next,lowNext,low`, exposing outward faces.
- Biome model baking and the general static batch pass could duplicate the same source geometry. Final code both marks baked originals nonstatic and skips disabled renderers in the later pass.
- Imported mesh reads are guarded by `isReadable`, and the build importer enables Read/Write for all four required FBX models.
- Animated monolith fragments are explicitly nonstatic; resident models and rain are excluded from static baking.
- Two-sided foliage uses distinct reversed vertices, preserving normals instead of cancelling them during `RecalculateNormals`.

### Source-level playability checks

- Terrain remains exactly flat through the main x corridor and gate z corridor; the central landmark radius 9.5 is flattened. Spawn and gate centers retain their prior positions.
- Main landmark paving is 0.15 high. Observatory terraces rise in 0.20 increments, with a front stair approach. Sanctuary retains an open entrance; monolith ground rings are noncolliding. All three retain approach points inside the 8-unit study radius.
- Gate piers are now at z ±3.15; footing edges leave a clear center passage. Threshold paving is 0.15 high and interaction still centers on x ±36, z 0.
- Optional survey/well sites at `(22,26)` and `(-23,18)` use sparse structure collision and coarse resident navigation bounds. Scatter excludes their full radius-6 surroundings and straight access corridors from the central landmark with 1.8 half-width plus each object's radius.
- Fine grass, flowers, ferns, snow patches and pebbles do not create navigation obstacles. Major detail generation is bounded by eight clusters × five major objects, plus small numbers of logs, pottery and crystal props. This avoids multiplying the 49×49 navigation search by hundreds of grass obstacles.

### Source-level rendering/lifetime budget

- Terrain: 82×82 cells, 40,344 vertices and 13,448 triangles, covering ±61.5 units. UInt32 mesh indices are set explicitly.
- Biome additions use at most four combined renderer batches: solids, understory, canopy, and luminous clusters. Static architecture/props are combined by 24-unit spatial tile and material.
- Decorative particles reuse one mesh of 140 quads (560 vertices, 280 triangles); mesh arrays are retained and updated.
- New terrain, skirt, path, biome, torus and static-combination meshes are registered in the shared `meshes` ownership list. Region teardown deactivates/destroys the root, destroys generated meshes/materials, and clears material caches, obstacle entries and resident routes.
- Imported shared meshes are read/copied but not inserted into generated mesh ownership; region teardown does not destroy the imported resources.

### Outstanding native evidence

No source blocker remains from this review. Parent must establish the new build result and inspect fresh images, with special attention to side-site elevation, architecture seams, cloud/sky rendering, vegetation density and character scale. A compiled build and automated interactions still do not prove keyboard/mouse playtesting.

Recommended no-cost native checks: visit each region twice, record active renderer/triangle/object/navigation-obstacle counts, and time a synthetic director movement result around a landmark/outcrop. This distinguishes source-level bounded counts from observed frame rate and catches region teardown accumulation without an API request. The current automated verifier already checks the side-site discovery interactions; its final result should be reported separately from any human-input/native Computer Use test.

## Final batching/density snapshot

After native evidence reported excessive renderer counts, the main geometry owner replaced runtime static flags with explicit `MeshFilter` candidate registration. Independent source re-review found no blocker:

- Cube face winding agrees with face normals; cylinder side and cap winding point outward; sphere winding is outward (zero-area triangles at its poles are harmless).
- Primitive fallback dimensions preserve Unity cube side length 1, cylinder radius 0.5/height 2, and sphere radius 0.5, so existing primitive colliders retain the intended scale.
- Candidate batching excludes disabled biome source renderers and descendants of animated ring groups, residents, and rain. Readable fallback meshes are cached per primitive type, registered once in generated ownership, and cleared on region changes.
- Grove density now targets 66 trees with up to 40 undergrowth samples per tree. The canopy remains one combined batch; small foliage remains nonblocking. This supersedes the earlier eight-cluster major-object estimate specifically for the grove.
- `AETHER_BATCH` instrumentation records sources, submeshes, batches and skipped categories in the native log. Source review cannot establish the resulting actual renderer count; parent is rebuilding and measuring it.

The parent reported earlier native checks passed with route calculations around 1–4 ms, but those readings predate this final density/batching snapshot and are not verification of the final build.

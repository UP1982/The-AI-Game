# Independent QA review — 2026-09-30

This is a read-only source review and safe local test pass while other agents are editing. It is not the final native playtest. No API calls, Unity launches, game launches, or secret reads were performed by this reviewer.

## Evidence observed

- Initial `python3 -m unittest discover -s Tests -v`: 9 tests passed. These cover persistent budget exhaustion, malformed ledger rejection, basic schema validation, bad generated JSON, conversation persistence, cached-world reuse, and invalid character rejection. The backend owner is adding broader tests; rerun after their edits.
- Existing `Documentation/build-result.txt` reports `Succeeded; errors=0`. Build code explicitly loads all four required Blender model prefabs before packaging and throws when one is missing.
- Existing `Documentation/runtime-check.txt` reports success for a generated saved world, three regions, controller displacement, terrain collision, and explorer save. Those checks exercise a saved world; they do not prove a new paid generation happened in that Unity run.
- Private local captures record `source: OpenAI`, three regions, two dialogue replies, a director event, and usage estimates. These previous live-test artifacts are excluded from Git; this review made no new calls.
- Existing region screenshots predate the latest visual/material changes. Do not use them as evidence of the final appearance until refreshed.

## Findings dispatched to owners

- Gameplay: first entry and expansion called region loading inside a busy response callback, skipping the `/visit` request. Owner reports a deferred visit fix.
- Gameplay: Escape during pending dialogue could leave pause and cursor state inconsistent. Owner reports consolidated cursor/panel handling, captured dialogue indices, and save restoration improvements.
- Geometry: applying director movement must also update the client region's stored NPC x/z coordinates, or revisiting the region restores stale positions. Dispatched to geometry owner.
- Backend: cached-world palette repair could make a paid request despite the cached-load expectation. Owner reports removal plus fail-closed saved-world validation and additional regression tests.

## Final verification still required

1. Rebuild after all source and exported asset changes settle; a previous successful build does not cover later edits.
2. Rerun all backend tests after edits. Confirm the persistent ledger remains within the approved $0.25 allowance; do not reset it.
3. Refresh native runtime checks and images, inspecting all three regions for terrain/material/model appearance and readable UI.
4. Interactively verify mouse look, WASD, jump, nearby character interaction, landmark journal, gate travel, Escape/Tab transitions, focus loss, and save/resume. Programmatic controller displacement is narrower evidence than keyboard playtesting.
5. Confirm imported models in the new build, region revisits preserve NPC destinations, and first entry/new regions record visits.
6. Inspect pause during an in-flight request: no new automatic generation should begin while paused; already running generation may finish.
7. Verify launcher startup and service restart locally, including reloading the current session token. Normal player saves must remain separate from automated verification saves.

The product is a bounded AI-directed exploration prototype: the AI invents world data, dialogue, goals, and events, while Unity generates scenes from fixed geometry/asset families. Claims of unrestricted AI-generated meshes or every engine action being model-controlled would exceed the implementation.

## Follow-up review after reliability edits

Independent rerun: **17 Python tests passed**, and `bash -n` passed for launch, stop-service, and build scripts. All tests use fake transports or direct handler calls; no API or native application was invoked.

Resolved in inspected source:

- Deferred visit requests now wait for the current response callback to finish, covering first entry and generated-region transitions.
- Dialogue captures its character/region before asynchronous completion; cursor state is derived from pause, journal, conversation, focus, and playing state. Underlying dialogs are hidden while paused. Final reply/memory and journal views measure text height and scroll long generated content.
- Saving reports actual success, restores a backup after a damaged main save, validates finite coordinates/orientation, and clamps view tilt. Native verification now includes saved orientation/discoveries and backup recovery.
- Cached-world loading no longer performs paid palette migration. Strict saved-world and conversation validation rejects malformed state before launch.
- Missing usage ledger with an existing world or memory file refuses service initialization, preventing an accidental fresh allowance. Malformed ledgers and negative/invalid usage are rejected; tests confirm reservations persist across failures/restart.
- Expansion/world update persistence happens before replacing active in-memory world state. Tests verify failed expansion persistence and duplicate director indices keep the world unchanged.
- Launcher validates an authenticated `/session` response against both project root and storage directory. Session validation is non-generating and tested; wrong host, token, origin, and unsupported chunked input are rejected.

The actual existing save was loaded through the new Director validator with no API key supplied and no transport call: **valid, three regions, five recorded requests, ledger within $0.25**. No saved state was modified by this check.

Still outstanding at this review snapshot:

- Geometry destination persistence is now resolved in final inspected source: Apply writes NPC x/z as well as goal before planning a route. New collider/navigation behavior still needs the parent native check.
- Parent owns rebuilding and rerunning the strengthened native verification and refreshed screenshots.
- Actual keyboard/mouse playtesting remains unverified; Computer Use access is pending. No automated report should imply that manual/native input flows were exercised.
- Launcher identity checks may time out while the director holds its lock during a long generation. Packaging owner was informed. Until changed, wait for generation to finish before relaunching. After restarting the service, close and reopen the game so its session token refreshes.

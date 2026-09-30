# Aether — The AI Game

A native Unity HDRP exploration prototype for macOS and Windows, with macOS as the primary target. You move through the world; OpenAI invents its regions, inhabitants, conversations, personal goals, and changes in the world.

## Set up another computer or remote workspace

1. Clone this repository. Install Python 3 to run the backend tests; install **Unity 6000.3.24f1** through Unity Hub to edit or build the game.
2. For AI generation, copy `.env.example` to `.env.local` and set `OPENAI_API_KEY` there, or provide it through your remote workspace's secret settings as an environment variable. Keep credentials out of source files and commits. Editing code and running the unit tests require no API key.
3. Run `python3 -m unittest discover -s Tests -v` to check the backend without making API calls.
4. Open the project in Unity Hub and let Unity restore its packages and import the included assets. Build with `./scripts/build-mac.sh` on macOS or follow [the Windows instructions](Documentation/windows.md), then use the platform's Play launcher.

Builds, Unity caches, credentials, saved worlds, conversation history, live API response captures, and the usage ledger are excluded from Git. A fresh clone needs a local build before the Play launcher works. To continue an existing world, transfer `.runtime` privately, including `usage.json`, and use one active spending copy. A new checkout does not share another computer's spending ledger. Remote environments without Unity can still edit source and run the Python tests.

## Play

Double-click **Play Aether.command** in this folder. It starts the local world service and opens `Builds/Aether.app`. Click **Continue exploring** to enter the already generated world. Keep the project folder together: the launcher supplies its location so the app can find the local service and saves. Opening the app bundle directly does not supply that location.

| Control | Action |
| --- | --- |
| WASD + mouse | Walk and look |
| Shift / Space | Run / jump |
| E | Speak to a nearby inhabitant, study a landmark, or cross a horizon gate |
| Tab | Field journal |
| Escape | Close a panel or pause |
| Q | Toggle automatic forward walking |
| Left / Right arrows | Turn in 20-degree steps |

Follow the stone path to the central landmark. The gates to either side lead between regions. Crossing the final eastern gate asks the AI to invent another region. Up to twenty regions are supported in this prototype. Revisiting saved regions does not regenerate them.

Each region now has sculpted terrain, weathered paths, detailed architecture and two optional discoveries: the Eastern Survey Ruin and Western Wayfarer Well. Approach either and press **E** to add its region notes to the journal. Grove regions contain branching jade trees, ferns, flowers and fallen timber; deserts have dunes, layered sandstone and abandoned pottery; tundra regions contain icy ridges, snow patches and crystal clusters. Airborne pollen, sand or snow and quiet biome-specific sounds accompany exploration. Existing worlds automatically receive the visual upgrade without regenerating their AI content.

## Graphics and platforms

The HDRP upgrade adds 2K photographic terrain, bark and stone materials with normal and roughness maps, layered heightfield terrain, individual tree leaves, a normalized photographic cloud sky and directional daylight, ACES tone mapping, temporal anti-aliasing, soft/contact shadows, ambient occlusion, screen-space reflections and volumetric mist. Choose **High** or **Medium** in the pause menu. Medium reduces reflection/shadow cost and disables volumetric fog.

macOS uses Metal with raster and screen-space lighting. The Windows executable cross-built here uses raster lighting. A native Windows editor build requests DirectX 12 and enables ray-traced reflections and ambient occlusion only when the GPU, driver and HDRP configuration support them. Unity’s DirectX ray-tracing shader compilation requires the Windows editor. Unsupported configurations fall back to raster lighting; `--raytracing off` disables the option. These effects improve realism, but the generated architecture, vegetation and inhabitants remain a bounded procedural prototype.

For Windows builds and transfer instructions, see [Documentation/windows.md](Documentation/windows.md). Hardware ray tracing must be tested on a compatible Windows PC. The previous stylized Mac build is preserved at `Builds/Aether-Stylized.app`.

## What the AI controls

- World premise, region names and descriptions, terrain seeds, biomes, color palettes, landmark types, discoveries, and character identities.
- Characters' dialogue and remembered conversations, stored locally between sessions.
- A world director chooses weather, personal goals, and movement destinations approximately every 90 seconds while you explore.
- New regions beyond the initial three, generated on demand.

Unity handles movement, rendering, collision, and the rules that turn validated AI data into a playable scene. The visual vocabulary is intentionally bounded: three terrain styles, three landmark structures, and original Blender models. This is an extensible first playable, not an arbitrary mesh-generating engine. The initial title scene is an explicitly labeled procedural preview; the playable world is generated through the real OpenAI API.

## API access and cost

Store your API key in the ignored `.env.local` or the `OPENAI_API_KEY` environment variable. The API key is **not embedded in the Unity application**. A Python service bound to `127.0.0.1:8766` makes requests; the local game uses a separate ephemeral session token.

The prototype enforces a **$0.25 total** generation limit per local ledger. The persistent ledger at `.runtime/usage.json` reserves a conservative upper bound before every request, including failed or interrupted calls. When the remaining allowance cannot cover a request, generation stops; saved exploration still works. Do not delete or reset this ledger to continue spending. Account-wide usage from other apps is outside this game's limit.

The model is pinned to `gpt-4.1-mini-2025-04-14`. The calculation uses published September 30, 2026 pricing ($0.40 input / $1.60 output per million tokens), conservatively counts the serialized request bytes plus overhead, and reserves all allowed output tokens. Actual usage is shown separately as an estimate. Recheck pricing before changing models or continuing long-term use. The app does not buy credits or enable auto-reload.

Sources: [model and pricing](https://developers.openai.com/api/docs/models/gpt-4.1-mini), [structured outputs](https://developers.openai.com/api/docs/guides/structured-outputs).

## Saved data

- `.runtime/world.json`: generated regions, inhabitants, events, and visits.
- `.runtime/memory.json`: character conversation history and memory.
- `.runtime/player.json`: location, view direction, and journal discoveries.
- `.runtime/usage.json`: persistent cost guard and request count.
- `.runtime/session.json`: local connection token, renewed when the service restarts.

These files are private and excluded from Git. Run `./scripts/stop-service.sh` to stop only the service belonging to this project; it preserves all saved progress and the spending ledger. The local server stays idle when the game is closed; it only calls OpenAI in response to authenticated game requests. Dialogue and relevant fictional world context are sent to OpenAI with `store: false`.

## Edit or rebuild

Open **Open Unity Project.command** or add this folder to Unity Hub. The installed editor used here is **Unity 6000.3.24f1**. Open `Assets/Scenes/Aether.unity`, then press Play. Use **Aether → Build macOS** in the editor or run:

```sh
./scripts/build-mac.sh
```

The original Blender source is `ArtSource/Aether.blend`; the reproducible authoring script is `ArtSource/create_models.py`. Exported FBX models are under `Assets/Resources/Models`. Photographic material maps and the HDRI are CC0 assets from Poly Haven; source links, authors, checksums and reproduction steps are in [Documentation/asset-sources.md](Documentation/asset-sources.md). Python 3 is the only backend dependency; it uses the standard library. Build and editor launch scripts read the required Unity version from `ProjectSettings/ProjectVersion.txt`. For a custom editor installation, set `AETHER_UNITY_EDITOR` to its Unity executable when running the build script. Close this project in the Unity editor before building from the command line. This local prototype has not been signed and notarized for distribution.

## Launch troubleshooting

The launcher verifies both the service identity and this project's session token before opening the game. If port 8766 is occupied by another service or checkout, it stops with a diagnostic instead of connecting to it. A fresh service can take up to a minute to start, and the launcher also waits when an existing service is finishing an AI operation. Close the conflicting service and retry; `./scripts/stop-service.sh` only stops a matching service whose working directory is this project. After backend code changes, close the game, stop that service, and launch again to load the updated server. Always close and reopen the game after a service restart so it reads the new session token; clicking the launcher while the app is already open can simply focus its existing window.

Startup failures are recorded in `.runtime/server.log`; build failures are recorded in `.runtime/build.log`. A failed double-click launch keeps its Terminal window open so the error can be read. Missing Python or Unity installations produce an explicit message. Launching and checking the local service does not itself make an OpenAI request.

## Verification

```sh
python3 -m unittest discover -s Tests -v
```

`python3 scripts/probe_ai.py --interactions` runs a **billable**, bounded integration test against a running local service. `--verify-game` on the native executable runs Unity checks and writes an isolated explorer save plus images into `Documentation`; it does not overwrite the normal player save. See `Documentation/verification.md` for observed results and limitations.

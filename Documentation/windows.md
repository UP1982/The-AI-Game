# Windows build and local launch

macOS remains the primary development and play target. A Windows x64 Mono build uses the same Unity project and Python backend. Windows execution and hardware ray tracing require verification on a Windows machine; a successful cross-build on this Mac cannot establish GPU compatibility or performance.

## Cross-build on this Mac: raster rendering

The pinned editor is Unity **6000.3.24f1**. Its **Windows Build Support (Mono)** module is required. Unity Hub offers it under the installed editor's **Add modules** action. The matching package identified by this editor's local Hub metadata is [Unity's official Windows Mono support installer](https://download.unity3d.com/download_unity/4e7b9b5b6244/MacEditorTargetInstaller/UnitySetup-Windows-Mono-Support-for-Editor-6000.3.24f1.pkg). Download metadata records 384,323,082 bytes and approximately 997 MB installed. Downloading or expanding that package does not install the module.

With the module installed and the project closed in Unity, run:

```sh
./scripts/build-windows.sh
```

The script invokes `BuildAether.BuildWindows`, targets Windows x64, and requires an explicit success marker plus `Builds/Windows/Aether.exe`. A Mac-hosted Windows build uses the Windows raster rendering configuration with ray tracing disabled: Unity's Direct3D DXC shader compilation cannot produce the required ray-tracing shader path from the Mac editor. Diagnostics go to `.runtime/build-windows.log`. The default editor path follows `ProjectSettings/ProjectVersion.txt`; `AETHER_UNITY_EDITOR` can select a custom editor executable.

## Build on Windows: ray-tracing-capable rendering

Use the full Unity project on a Windows machine with **Unity 6000.3.24f1** installed through Unity Hub. Double-click **Build Windows.bat**, or run this from PowerShell:

```powershell
.\scripts\build-windows.ps1
```

The script reads the pinned editor version and defaults to `C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe`. For another installation, pass `-UnityEditor "D:\path\to\Editor\Unity.exe"` or set `AETHER_UNITY_EDITOR`. Close other Unity windows containing this project before building.

The Windows-native editor invokes the same `BuildAether.BuildWindows` method, targeting Win64 with the ray-tracing-capable Windows rendering asset. It uses Mono, so IL2CPP and a C++ compiler toolchain are not required for this path. Every invocation retains a complete timestamped log at `.runtime/build-windows-native-YYYYMMDD-HHMMSS.log`. Success requires both `AETHER_WINDOWS_BUILD_PASSED` in that log and the built executable. Both build scripts also reject any `Shader error` entry, even if Unity otherwise reports a successful build.

The batch file does not change PowerShell execution policy. If your Windows policy blocks local scripts, open the project directly in the pinned Unity editor and use its **Aether → Build Windows** menu instead. Building the rendering configuration does not verify hardware ray tracing: test the resulting game on a supported Direct3D 12 GPU and inspect the runtime capability status. A Windows virtual machine may lack the required GPU capability even when it can build the project.

## Play on your Windows machine

Install Python 3, then copy this project together with its complete `Builds/Windows` directory. At minimum the launcher needs:

- `Play Aether Windows.bat` and `scripts/launch_windows.py`.
- `Backend/world_server.py`.
- The complete `Builds/Windows` folder, including the executable, data, Unity runtime libraries, and Mono runtime.
- Your existing `.runtime` save files, **including `usage.json`**, to preserve progress and the approved spending allowance.
- Your private `.env.local` alongside the project root if further AI generation is needed. Keep this key out of the executable, public repositories, and distributable packages.

Double-click **Play Aether Windows.bat**. It checks for Python 3, authenticates the local service using this project's session file, verifies its project and storage paths, and launches the executable with the project-root argument. A new service creates a fresh session token. The launcher handles spaces and Unicode in project paths and bypasses HTTP proxies for local service checks.

A fresh service may take approximately a minute to start. Existing busy services receive a bounded readiness wait; another project's service on port 8766 causes a diagnostic instead of being reused. Startup logs are in `.runtime/server.log`. The service remains running but idle after closing the game; no OpenAI request is made merely by starting or checking it. Close and reopen the game after restarting the service so it reads the fresh token.

Keep one active copy of this saved world while using the **same $0.25 total allowance**. Copying the ledger to a second computer does not synchronize later spending between computers. Do not run independently spending copies simultaneously, delete the ledger, or reset it to obtain a fresh allowance. When moving back to another computer, preserve the most recent complete save and ledger together.

## Rendering verification

The Mac-cross-built Windows executable uses raster rendering. Hardware ray tracing requires a Windows-native build and a compatible Windows computer; it is not a claim that every Windows computer supports it. Its availability depends on the built rendering configuration, graphics API, GPU, and drivers. Use the game's runtime capability status and test on the target computer before treating ray tracing as working. The macOS build must continue to use its supported rendering path.

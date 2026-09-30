#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT_ROOT="$(pwd -P)"
mkdir -p .runtime
EDITOR_VERSION="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
UNITY="${AETHER_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$EDITOR_VERSION/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY" ]]; then
  printf 'Unity %s was not found. Install it in Unity Hub or set AETHER_UNITY_EDITOR to its executable.\n' "$EDITOR_VERSION" >&2
  exit 1
fi
MODULE="$(dirname "$UNITY")/../PlaybackEngines/WindowsStandaloneSupport"
if [[ ! -d "$MODULE" ]]; then
  printf 'Windows Build Support (Mono) is missing for Unity %s. In Unity Hub, use Installs > this editor > Add modules > Windows Build Support (Mono).\n' "$EDITOR_VERSION" >&2
  exit 1
fi
if ! "$UNITY" -batchmode -nographics -quit -buildTarget Win64 -projectPath "$PROJECT_ROOT" -executeMethod BuildAether.BuildWindows -logFile "$PROJECT_ROOT/.runtime/build-windows.log"; then
  printf 'Windows build failed. Close this project in Unity before a command-line build. See %s/.runtime/build-windows.log.\n' "$PROJECT_ROOT" >&2
  exit 1
fi
if grep -qi 'Shader error' .runtime/build-windows.log; then
  printf 'Unity reported shader compilation errors. Treat this Windows build as failed; see %s/.runtime/build-windows.log.\n' "$PROJECT_ROOT" >&2
  exit 1
fi
if ! grep -q 'AETHER_WINDOWS_BUILD_PASSED' .runtime/build-windows.log || [[ ! -f Builds/Windows/Aether.exe ]]; then
  printf 'Unity exited without a verified successful Windows build. See %s/.runtime/build-windows.log.\n' "$PROJECT_ROOT" >&2
  exit 1
fi
printf 'Windows build ready in %s/Builds/Windows. Run Play Aether Windows.bat on Windows.\n' "$PROJECT_ROOT"

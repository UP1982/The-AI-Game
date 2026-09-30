#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT_ROOT="$(pwd -P)"
mkdir -p .runtime
EDITOR_VERSION="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
UNITY="${AETHER_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$EDITOR_VERSION/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY" ]]; then
  printf 'Unity %s was not found at %s. Install it through Unity Hub, or set AETHER_UNITY_EDITOR to the Unity executable.\n' "$EDITOR_VERSION" "$UNITY" >&2
  exit 1
fi
if ! "$UNITY" -batchmode -nographics -quit -buildTarget OSXUniversal -projectPath "$PROJECT_ROOT" -executeMethod BuildAether.Build -logFile "$PROJECT_ROOT/.runtime/build.log"; then
  printf 'Unity build failed. Close this project in Unity before a command-line build. See %s/.runtime/build.log for details.\n' "$PROJECT_ROOT" >&2
  exit 1
fi
if ! grep -q 'AETHER_BUILD_PASSED' .runtime/build.log || [[ ! -x Builds/Aether.app/Contents/MacOS/Aether ]]; then
  printf 'Unity exited without a verified successful build. See %s/.runtime/build.log.\n' "$PROJECT_ROOT" >&2
  exit 1
fi
/usr/bin/xattr -rd com.apple.FinderInfo "Builds/Aether.app" 2>/dev/null || true
/usr/bin/xattr -rd com.apple.ResourceFork "Builds/Aether.app" 2>/dev/null || true
# iCloud may synthesize FinderInfo that prevents re-signing. No signing or
# notarization is claimed, and these scripts do not change macOS security settings.
printf 'Aether is ready in %s/Builds/Aether.app\n' "$PROJECT_ROOT"

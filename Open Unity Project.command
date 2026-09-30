#!/bin/bash
set -uo pipefail
cd "$(dirname "$0")" || exit 1
EDITOR_VERSION="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
UNITY_APP="/Applications/Unity/Hub/Editor/$EDITOR_VERSION/Unity.app"
if [[ ! -d "$UNITY_APP" ]]; then
  printf 'Unity %s is not installed in Unity Hub\047s default location. Open Unity Hub and add this project folder.\n' "$EDITOR_VERSION" >&2
  if [[ -t 0 ]]; then printf '\nPress Return to close this window.'; read -r _; fi
  exit 1
fi
/usr/bin/open -a "$UNITY_APP" --args -projectPath "$(pwd -P)"

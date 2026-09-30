#!/bin/bash
set -uo pipefail
cd "$(dirname "$0")" || exit 1
/bin/bash scripts/launch.sh
result=$?
if [[ "$result" -ne 0 && -t 0 ]]; then
  printf '\nPress Return to close this window.'
  read -r _
fi
exit "$result"

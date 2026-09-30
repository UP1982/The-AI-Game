#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT_ROOT="$(pwd -P)"
# Require both the script identity and this checkout's working directory.
# Leave all saved world, credential, session, and cost-ledger files intact.
stopped=false
for pid in $(/usr/sbin/lsof -tiTCP:8766 -sTCP:LISTEN 2>/dev/null || true); do
  process_command="$(ps -p "$pid" -o command= 2>/dev/null || true)"
  process_directory="$(/usr/sbin/lsof -a -p "$pid" -d cwd -Fn 2>/dev/null | sed -n 's/^n//p' || true)"
  if [[ "$process_directory" == "$PROJECT_ROOT" ]] &&
     { [[ "$process_command" == *"$PROJECT_ROOT/Backend/world_server.py"* ]] || [[ "$process_command" == *"Python Backend/world_server.py"* ]] || [[ "$process_command" == *"python3 Backend/world_server.py"* ]]; }; then
    kill "$pid"
    printf 'Stopped this project\047s world service (PID %s). Saved progress and the spending ledger are unchanged.\n' "$pid"
    stopped=true
  fi
done
if [[ "$stopped" != true ]]; then
  printf 'No world service belonging to this project was stopped.\n'
fi

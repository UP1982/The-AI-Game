#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT_ROOT="$(pwd -P)"
APP="$PROJECT_ROOT/Builds/Aether.app"
PORT=8766
fail() { printf 'Aether: %s\n' "$*" >&2; exit 1; }
[[ -x "$APP/Contents/MacOS/Aether" ]] || fail 'The game build is missing. Run ./scripts/build-mac.sh first.'
PYTHON="$(command -v python3)" || fail 'Python 3 is required. Install Python 3 and try again.'
mkdir -p .runtime
# Validate both service identity and this project's current session credentials.
# The authenticated session endpoint cannot generate content or spend credits.
service_ready() {
  "$PYTHON" - "$PROJECT_ROOT" "$PORT" <<'PY' >/dev/null 2>&1
import json,pathlib,socket,sys,urllib.request,urllib.error
root=pathlib.Path(sys.argv[1]); port=int(sys.argv[2])
opener=urllib.request.build_opener(urllib.request.ProxyHandler({}))
base='http://127.0.0.1:'+str(port)
try:
    with opener.open(base+'/health',timeout=1) as response:
        if json.load(response).get('service')!='AetherWorld': raise ValueError('Different service')
    session=json.loads((root/'.runtime/session.json').read_text())
    if session.get('port')!=port or not isinstance(session.get('token'),str) or not session['token']:
        raise ValueError('Invalid local session')
    request=urllib.request.Request(base+'/session',data=b'{}',headers={'Content-Type':'application/json','X-World-Token':session['token']})
    with opener.open(request,timeout=5) as response:
        identity=json.load(response)
    if pathlib.Path(identity['rootPath']).resolve()!=root.resolve():
        raise ValueError('Different project')
    if pathlib.Path(identity['storagePath']).resolve()!=(root/'.runtime').resolve():
        raise ValueError('Different save directory')
except (TimeoutError,socket.timeout):
    sys.exit(2)
except urllib.error.URLError as error:
    sys.exit(2 if isinstance(error.reason,(TimeoutError,socket.timeout)) else 1)
except Exception:
    sys.exit(1)
PY
}
# Use an elapsed-time deadline: filesystem startup can be slow on cloud-backed folders.
wait_for_service() {
  local deadline=$((SECONDS + 60))
  while (( SECONDS < deadline )); do
    if service_ready; then return 0; fi
    if [[ -n "${server_pid:-}" ]] && ! kill -0 "$server_pid" 2>/dev/null; then return 1; fi
    sleep 0.25
  done
  return 1
}
ready=false
probe_result=0
service_ready || probe_result=$?
if [[ "$probe_result" == 0 ]]; then
  ready=true
elif /usr/sbin/lsof -nP -iTCP:"$PORT" -sTCP:LISTEN >/dev/null 2>&1; then
  if [[ "$probe_result" == 2 ]]; then
    printf 'Aether: The world service is busy; waiting up to 60 seconds for its current operation.\n'
    if wait_for_service; then ready=true; fi
    [[ "$ready" == true ]] || fail 'The world service is still busy or unresponsive. Wait for generation to finish, then close the game and launch again. No service was stopped.'
  else
    fail "Port $PORT is already occupied, but this project's session did not authenticate. Close the other service, or close the game and run ./scripts/stop-service.sh for this project before retrying."
  fi
else
  printf 'Aether: Starting the local world service (allow up to 60 seconds).\n'
  nohup "$PYTHON" "$PROJECT_ROOT/Backend/world_server.py" --port "$PORT" --storage "$PROJECT_ROOT/.runtime" > .runtime/server.log 2>&1 < /dev/null &
  server_pid=$!
  if wait_for_service; then ready=true; fi
  if [[ "$ready" != true ]]; then
    printf 'Aether: The world service did not become ready. See %s/.runtime/server.log\n' "$PROJECT_ROOT" >&2
    # Only clean up the process started by this invocation; never another listener.
    if kill -0 "$server_pid" 2>/dev/null; then kill "$server_pid" 2>/dev/null || true; fi
    wait "$server_pid" 2>/dev/null || true
    exit 1
  fi
fi
/usr/bin/open "$APP" --args --project-root "$PROJECT_ROOT" || fail "macOS could not open the game. See .runtime/server.log for service diagnostics."

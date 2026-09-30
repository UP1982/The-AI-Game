#!/usr/bin/env python3
"""Start the local, authenticated world service and Windows player without API calls."""
import json
import os
import pathlib
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[1]
STORAGE = ROOT / '.runtime'
PORT = 8766


def same_path(value, expected):
    return os.path.normcase(str(pathlib.Path(value).resolve())) == os.path.normcase(str(expected.resolve()))


def probe_service():
    """Return ready, busy, absent, or conflict; never request generation."""
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    base = 'http://127.0.0.1:' + str(PORT)
    try:
        with opener.open(base + '/health', timeout=2) as response:
            if json.load(response).get('service') != 'AetherWorld':
                return 'conflict'
        session = json.loads((STORAGE / 'session.json').read_text(encoding='utf-8'))
        if session.get('port') != PORT or not isinstance(session.get('token'), str) or not session['token']:
            return 'conflict'
        request = urllib.request.Request(base + '/session', data=b'{}', headers={
            'Content-Type': 'application/json', 'X-World-Token': session['token']})
        with opener.open(request, timeout=5) as response:
            identity = json.load(response)
        return 'ready' if same_path(identity['rootPath'], ROOT) and same_path(identity['storagePath'], STORAGE) else 'conflict'
    except (TimeoutError, socket.timeout):
        return 'busy'
    except urllib.error.HTTPError:
        return 'conflict'
    except urllib.error.URLError as error:
        return 'busy' if isinstance(error.reason, (TimeoutError, socket.timeout)) else 'absent'
    except (OSError, ValueError, KeyError, TypeError):
        return 'conflict'


def port_occupied():
    try:
        with socket.create_connection(('127.0.0.1', PORT), timeout=1):
            return True
    except OSError:
        return False


def wait_for_service(process=None):
    deadline = time.monotonic() + 60
    while time.monotonic() < deadline:
        if process is not None and process.poll() is not None:
            return False
        if probe_service() == 'ready':
            return True
        time.sleep(.25)
    return False


def main():
    if os.name != 'nt':
        raise RuntimeError('This launcher runs on Windows. On macOS, use Play Aether.command.')
    player = ROOT / 'Builds' / 'Windows' / 'Aether.exe'
    if not player.is_file():
        raise RuntimeError('The Windows build is missing. Build it first and keep the complete Builds/Windows folder.')
    STORAGE.mkdir(parents=True, exist_ok=True)
    process = None
    state = probe_service()
    if state != 'ready':
        if port_occupied():
            if state != 'busy':
                raise RuntimeError('Port 8766 belongs to another service or this project has an outdated session. Close the game and the conflicting service, then retry.')
            print('The world service is busy. Waiting up to 60 seconds...')
            if not wait_for_service():
                raise RuntimeError('The existing service is still busy. Wait for generation to finish, close the game, and retry. It has not been stopped.')
        else:
            print('Starting the local world service. Allow up to 60 seconds...')
            with (STORAGE / 'server.log').open('ab') as log:
                process = subprocess.Popen(
                    [sys.executable, str(ROOT / 'Backend' / 'world_server.py'), '--port', str(PORT), '--storage', str(STORAGE)],
                    cwd=str(ROOT), stdin=subprocess.DEVNULL, stdout=log, stderr=log,
                    creationflags=subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS)
            if not wait_for_service(process):
                if process.poll() is None:
                    process.terminate()
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=5)
                raise RuntimeError('The service did not start. See .runtime/server.log. Saved progress and the spending ledger were preserved.')
    # Argument arrays safely handle spaces and Unicode in the project path.
    subprocess.Popen([str(player), '--project-root', str(ROOT)], cwd=str(ROOT))
    print('Aether launched. The local service stays idle after the game closes.')


if __name__ == '__main__':
    try:
        main()
    except (RuntimeError, OSError) as error:
        print('Aether: ' + str(error), file=sys.stderr)
        sys.exit(1)

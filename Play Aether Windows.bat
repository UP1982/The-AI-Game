@echo off
setlocal
cd /d "%~dp0"
py -3 -c "import sys;raise SystemExit(sys.version_info.major != 3)" >nul 2>&1
if not errorlevel 1 (
    py -3 scripts\launch_windows.py
    if errorlevel 1 pause
    exit /b
)
python -c "import sys;raise SystemExit(sys.version_info.major != 3)" >nul 2>&1
if not errorlevel 1 (
    python scripts\launch_windows.py
    if errorlevel 1 pause
    exit /b
)
echo Python 3 is required. Install Python 3 for Windows, then try again.
pause
exit /b 1

@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -File "%~dp0scripts\build-windows.ps1"
set "BUILD_RESULT=%ERRORLEVEL%"
if not "%BUILD_RESULT%"=="0" (
    echo.
    echo The build failed. Review the error above and the complete Unity log in .runtime.
    echo You can also open the project in the pinned Unity Windows editor and use Aether then Build Windows.
    pause
)
exit /b %BUILD_RESULT%

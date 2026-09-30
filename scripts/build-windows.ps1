# Build on Windows so Unity's native Direct3D shader compiler can include ray tracing.
[CmdletBinding()]
param([string]$UnityEditor = $env:AETHER_UNITY_EDITOR)
$ErrorActionPreference = 'Stop'

try {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $versionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    $versionLine = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
    if (-not $versionLine) { throw 'ProjectSettings/ProjectVersion.txt does not identify a Unity editor version.' }
    $editorVersion = ($versionLine -replace '^m_EditorVersion: ', '').Trim()
    if (-not $UnityEditor) {
        $UnityEditor = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
    }
    if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
        throw "Unity $editorVersion was not found at $UnityEditor. Install this exact Windows editor through Unity Hub, or set AETHER_UNITY_EDITOR to Unity.exe."
    }
    $runtime = Join-Path $projectRoot '.runtime'
    New-Item -ItemType Directory -Path $runtime -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $buildLog = Join-Path $runtime "build-windows-native-$stamp.log"
    # Start-Process joins ArgumentList into a command line; quote paths explicitly.
    $arguments = @(
        '-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64',
        '-projectPath', ('"' + $projectRoot + '"'),
        '-executeMethod', 'BuildAether.BuildWindows',
        '-logFile', ('"' + $buildLog + '"')
    )
    Write-Host "Building Windows x64 with Unity $editorVersion. Close this project in other Unity windows first."
    Write-Host "Complete Unity log: $buildLog"
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WorkingDirectory $projectRoot -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity exited with code $($process.ExitCode). See $buildLog" }
    if (-not (Test-Path -LiteralPath $buildLog -PathType Leaf)) { throw "Unity did not produce its build log: $buildLog" }
    if (Select-String -LiteralPath $buildLog -SimpleMatch 'Shader error' -Quiet) {
        throw "Unity reported shader compilation errors. Treat this Windows build as failed; see $buildLog"
    }
    $passed = Select-String -LiteralPath $buildLog -SimpleMatch 'AETHER_WINDOWS_BUILD_PASSED' -Quiet
    $player = Join-Path $projectRoot 'Builds\Windows\Aether.exe'
    if (-not $passed -or -not (Test-Path -LiteralPath $player -PathType Leaf)) {
        throw "Unity did not report a successful Windows build. See $buildLog"
    }
    Write-Host "Windows build ready: $player"
    Write-Host 'Use Play Aether Windows.bat to start the local service and game. Ray tracing still requires a compatible GPU and runtime capability verification.'
    exit 0
}
catch {
    [Console]::Error.WriteLine('Aether build: ' + $_.Exception.Message)
    exit 1
}

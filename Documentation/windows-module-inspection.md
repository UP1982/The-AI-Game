# Unity Windows Mono module inspection

Observed on September 30, 2026. No package installer scripts were executed during this inspection.

- Package: `UnitySetup-Windows-Mono-Support-for-Editor-6000.3.24f1.pkg`.
- Official source: https://download.unity3d.com/download_unity/4e7b9b5b6244/MacEditorTargetInstaller/UnitySetup-Windows-Mono-Support-for-Editor-6000.3.24f1.pkg
- Download size: **384,323,082 bytes**, exactly matching this editor's Unity Hub `modules.json`.
- MD5, base64: `Kh33BehZQ2WwHZR8tL7S8g==`, exactly matching Hub metadata.
- SHA-256: `9cf58e91737a4b885dcc2804e649f7175fb04caf6df660bfd2d7503d73881014`.
- Expanded payload: `.runtime/installers/windows-mono-expanded/TargetSupport.pkg.tmp/Payload`.
- Package metadata: 1,469 files, 996,822 KB installed, editor version 6000.3.24f1.

The initial sandboxed `pkgutil --check-signature` returned `invalid signature`. An approved read-only invocation outside the sandbox successfully verified the **same package** as “signed by a developer certificate issued by Apple for distribution,” with a trusted timestamp of **2026-09-01 23:59:45 UTC**. The initial result therefore reflected the restricted verification environment, not an expired certificate or corrupted download.

The signer is **Developer ID Installer: Unity Technologies SF (9QW8UQUTAA)**. Its leaf certificate and Apple Developer ID intermediate certificate expire February 1, 2027; the Apple Root CA expires February 9, 2035. The host clock during verification was September 30, 2026. Independent extraction of the XAR table of contents confirmed its SHA-1 checksum and successfully verified its RSA signature against the signer certificate using OpenSSL.

The package's preinstall script checks for an editor at `/Applications/Unity/Unity-6000.3.24f1`, a different location from the current Hub installation, and validates its version. The postinstall script recursively assigns root/wheel ownership to that target. Neither script is needed merely to inspect the payload. Installation should use Unity Hub's matching module workflow or an explicitly approved copy of the reviewed payload to the correct editor's `Contents/PlaybackEngines/WindowsStandaloneSupport` directory. This inspection itself did not change the Unity installation.

The parent subsequently installed the verified payload with approved `ditto` into the matching Hub editor’s `Contents/PlaybackEngines/WindowsStandaloneSupport`. No package installer scripts were executed.

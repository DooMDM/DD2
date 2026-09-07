# Build instructions

This repository contains the source code for **STALKER 2 Fire Save Repair**.

The application is a Windows x64 WinForms utility targeting `.NET 8` / `net8.0-windows`.

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 7 or Windows PowerShell
- A legally obtained local copy of `oo2core_9_win64.dll` if you need to build the same single-file local release package

The source repository does not include `oo2core_9_win64.dll`, private saves, game files, build output, or packaged executables.

## Restore and build from source

From the repository root:

```powershell
dotnet restore FireSaveRepair.csproj --configfile NuGet.config
dotnet build FireSaveRepair.csproj -c Release --configfile NuGet.config
```

This checks that the source compiles. It does not create the bundled release executable.

## Run self-tests

```powershell
dotnet run --project FireSaveRepair.csproj -- --self-test
```

The self-tests validate the conservative repair logic and guardrails used by the application.

## Publish the local bundled executable

To produce the same type of local portable executable, provide the path to a trusted local `oo2core_9_win64.dll`:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\BuildRelease.ps1 -OodleDllPath "C:\Path\To\oo2core_9_win64.dll"
```

The release script:

- publishes a self-contained Windows x64 executable;
- embeds the provided local Oodle runtime into that local build;
- copies README, testing and third-party notices;
- creates a source archive that excludes binaries, private saves and proprietary native libraries;
- prints SHA256 hashes for the generated artifacts.

Before publishing a bundled executable, confirm that you have permission to redistribute every included third-party component. See `THIRD_PARTY.md`.

## Runtime behavior

The application:

- runs offline;
- scans local STALKER 2 save files selected by the user;
- repairs only the supported persistent `FireBreathDamage` save-state case;
- does not modify the game executable;
- does not contact any server;
- does not collect telemetry or personal data.


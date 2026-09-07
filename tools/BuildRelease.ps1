param(
    [Parameter(Mandatory=$true)][string]$OodleDllPath,
    [switch]$SkipPublish
)
$ErrorActionPreference='Stop'
$projectDirectory=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$nativeLibrary=[IO.Path]::GetFullPath($OodleDllPath)
if (-not (Test-Path -LiteralPath $nativeLibrary -PathType Leaf)) { throw 'Oodle library does not exist' }
if ([IO.Path]::GetFileName($nativeLibrary) -ne 'oo2core_9_win64.dll') { throw 'Expected Oodle 9 x64 runtime filename' }
$env:DOTNET_CLI_HOME=Join-Path $projectDirectory '.buildhome'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$releaseRoot=Join-Path $projectDirectory 'dist'
$version=([xml](Get-Content -LiteralPath (Join-Path $projectDirectory 'FireSaveRepair.csproj') -Raw)).Project.PropertyGroup.Version
$appDirectory=Join-Path $releaseRoot "app-$version"
if (-not $SkipPublish) {
    & dotnet publish (Join-Path $projectDirectory 'FireSaveRepair.csproj') -c Release -r win-x64 --self-contained true `
        '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' `
        "-p:OodleDllPath=$nativeLibrary" "-p:OutputPath=$(Join-Path $projectDirectory '.publishbuild')\" `
        '--configfile' (Join-Path $projectDirectory 'NuGet.config') '-p:NuGetAudit=false' -o $appDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
}
$exePath=Join-Path $appDirectory 'Stalker2FireSaveRepair.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw 'Published EXE missing' }
$files=@('README.md','README_EN.md','BUILD.md','LICENSE','THIRD_PARTY.md','TESTING.md')
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $projectDirectory $file) -Destination (Join-Path $appDirectory $file) -Force }
$documentationDirectory=Join-Path $appDirectory 'docs'
New-Item -ItemType Directory -Path $documentationDirectory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectDirectory 'docs\FORMAT.md') -Destination (Join-Path $documentationDirectory 'FORMAT.md') -Force
$noticeDirectory=Join-Path $appDirectory 'licenses'
New-Item -ItemType Directory -Path $noticeDirectory -Force | Out-Null
$packageCache=Join-Path $projectDirectory '.buildhome\.nuget\packages'
$notices=@(
    @('microsoft.netcore.app.runtime.win-x64\8.0.30\LICENSE.TXT','dotnet-runtime-LICENSE.txt'),
    @('microsoft.netcore.app.runtime.win-x64\8.0.30\THIRD-PARTY-NOTICES.TXT','dotnet-THIRD-PARTY-NOTICES.txt'),
    @('microsoft.windowsdesktop.app.runtime.win-x64\8.0.30\LICENSE','dotnet-desktop-LICENSE.txt')
)
foreach ($notice in $notices) { Copy-Item -LiteralPath (Join-Path $packageCache $notice[0]) -Destination (Join-Path $noticeDirectory $notice[1]) -Force }
# Explicit allowlist only. Never archive the entire workspace or build-home:
# those may contain SDK credentials, private test saves or developer caches.
$stage=Join-Path $releaseRoot "source-staging-$version"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach ($file in ($files+@('FireSaveRepair.csproj','Program.cs','MainForm.cs','SelfTests.cs','NuGet.config','NuGet.offline.config','.gitignore'))) {
    Copy-Item -LiteralPath (Join-Path $projectDirectory $file) -Destination (Join-Path $stage $file) -Force
}
foreach ($directory in @('Core','UI','docs','tools')) {
    $target=Join-Path $stage $directory
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $projectDirectory $directory) -File |
        Where-Object { $_.Extension -in '.cs','.json','.md','.ps1','.mjs' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $target $_.Name) -Force }
}
$binaryZip=Join-Path $releaseRoot "Stalker2FireSaveRepair-$version-win-x64-bundled-LOCAL.zip"
$sourceZip=Join-Path $releaseRoot "Stalker2FireSaveRepair-$version-source.zip"
foreach ($path in @($binaryZip,$sourceZip)) { if (Test-Path -LiteralPath $path) { throw "Refusing to overwrite existing release archive: $path" } }
$binaryItems=@($exePath)+@($files | ForEach-Object { Join-Path $appDirectory $_ })+@($noticeDirectory,$documentationDirectory)
Compress-Archive -LiteralPath $binaryItems -DestinationPath $binaryZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $sourceZip -CompressionLevel Optimal
Get-FileHash -LiteralPath $exePath,$binaryZip,$sourceZip -Algorithm SHA256 | Format-Table -AutoSize
Write-Output 'Bundled EXE is an owner-requested local build. Confirm Oodle redistribution rights before publishing it. Source ZIP contains no Oodle DLL or private saves.'

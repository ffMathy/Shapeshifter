<#
.SYNOPSIS
  Packs dotnet publish output into a Microsoft Store upload (.msixupload holding an x64 + arm64 .msixbundle).

.DESCRIPTION
  make-msix.ps1 -PublishRoot publish -Version 7.1.3 -OutDir dist

  PublishRoot holds one folder per architecture (publish/x64, publish/arm64), each the output of
  dotnet publish -r win-<arch>. The package identity comes from these environment variables, copied from Partner Center
  (Product management > Product identity): MSSTORE_IDENTITY_NAME (Package/Identity/Name), MSSTORE_PUBLISHER
  (Package/Identity/Publisher, "CN=…") and MSSTORE_PUBLISHER_DISPLAY_NAME.

  The packages are not signed: the Store signs what it publishes. To try a package locally, sign it with a test
  certificate whose subject equals MSSTORE_PUBLISHER (signtool sign /fd SHA256 /a /f test.pfx …) and install it.
  Needs the Windows SDK (makeappx.exe, makepri.exe), which GitHub's Windows runners have.
#>
param(
    [Parameter(Mandatory)] [string] $PublishRoot,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $OutDir,
    [string[]] $Architectures = @('x64', 'arm64')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Find-SdkTool([string] $name) {
    $root = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $tool = Get-ChildItem $root -Directory |
        Where-Object { $_.Name -match '^10\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$name" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if (-not $tool) { throw "$name not found under $root; install the Windows SDK." }
    return $tool
}

function Invoke-Tool([string] $tool, [string[]] $arguments) {
    & $tool @arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path $tool -Leaf) failed with exit code $LASTEXITCODE" }
}

function Get-RequiredVariable([string] $name) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Set $name to the value Partner Center shows under Product identity." }
    return $value
}

# The Store wants four-part versions whose last part is 0: 7.1.3 -> 7.1.3.0.
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must be X.Y.Z, not $Version (prereleases do not go to the Store)." }
$packageVersion = "$Version.0"

$identity = @{
    '{{IDENTITY_NAME}}'          = Get-RequiredVariable 'MSSTORE_IDENTITY_NAME'
    '{{PUBLISHER}}'              = Get-RequiredVariable 'MSSTORE_PUBLISHER'
    '{{PUBLISHER_DISPLAY_NAME}}' = Get-RequiredVariable 'MSSTORE_PUBLISHER_DISPLAY_NAME'
    '{{VERSION}}'                = $packageVersion
}

$makeappx = Find-SdkTool 'makeappx.exe'
$makepri = Find-SdkTool 'makepri.exe'
$here = $PSScriptRoot
$work = Join-Path ([IO.Path]::GetTempPath()) ("msix-" + [guid]::NewGuid())
$packages = Join-Path $work 'packages'
New-Item -ItemType Directory -Force -Path $packages, $OutDir | Out-Null

foreach ($architecture in $Architectures) {
    $publish = Join-Path $PublishRoot $architecture
    if (-not (Test-Path $publish)) { throw "No publish output for $architecture at $publish" }

    $manifest = Get-Content -Raw (Join-Path $here 'AppxManifest.xml')
    foreach ($key in $identity.Keys) {
        $manifest = $manifest.Replace($key, [Security.SecurityElement]::Escape($identity[$key]))
    }
    $manifest = $manifest.Replace('{{ARCHITECTURE}}', $architecture)

    # resources.pri lets Windows pick Images\Square44x44Logo.scale-200.png and the like for Images\Square44x44Logo.png.
    # It is built from the images alone, so the application's own files are not indexed as resources.
    $resources = Join-Path $work "resources-$architecture"
    New-Item -ItemType Directory -Force -Path $resources | Out-Null
    Copy-Item -Recurse (Join-Path $here 'Images') $resources
    Set-Content -Path (Join-Path $resources 'AppxManifest.xml') -Value $manifest -Encoding utf8
    $priConfig = Join-Path $work 'priconfig.xml'
    Invoke-Tool $makepri @('createconfig', '/cf', $priConfig, '/dq', 'en-US', '/pv', '10.0.0', '/o')
    Invoke-Tool $makepri @('new', '/pr', $resources, '/cf', $priConfig, '/mn', (Join-Path $resources 'AppxManifest.xml'),
        '/of', (Join-Path $resources 'resources.pri'), '/o')

    $stage = Join-Path $work "stage-$architecture"
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item -Recurse -Path (Join-Path $publish '*') -Destination $stage
    Copy-Item -Recurse (Join-Path $resources 'Images') $stage
    Copy-Item (Join-Path $resources 'resources.pri') $stage
    Copy-Item (Join-Path $resources 'AppxManifest.xml') $stage

    $msix = Join-Path $packages "package-$architecture.msix"
    Invoke-Tool $makeappx @('pack', '/d', $stage, '/p', $msix, '/o')
    Write-Host "Packed $msix"
}

$name = $identity['{{IDENTITY_NAME}}']
$bundle = Join-Path $work "${name}_$packageVersion.msixbundle"
Invoke-Tool $makeappx @('bundle', '/d', $packages, '/p', $bundle, '/bv', $packageVersion, '/o')

# Partner Center takes the bundle wrapped in a zip named .msixupload. ZipFile writes ZIP64 when needed, which bundles
# over 2 GB require (Compress-Archive cannot).
$upload = Join-Path (Resolve-Path $OutDir) "${name}_$packageVersion.msixupload"
$uploadContent = Join-Path $work 'upload'
New-Item -ItemType Directory -Force -Path $uploadContent | Out-Null
Move-Item $bundle $uploadContent
Remove-Item -Force -ErrorAction SilentlyContinue $upload
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($uploadContent, $upload, [IO.Compression.CompressionLevel]::NoCompression, $false)
Remove-Item -Recurse -Force $work
Write-Host "Created $upload"

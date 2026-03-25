#Requires -Version 5.1

[CmdletBinding()]
param(
  [Parameter(Position = 0)]
  [string] $Version = "",
  [string] $InstallDir = ""
)

$ErrorActionPreference = "Stop"
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Step   { param([string]$Msg) [Console]::Out.WriteLine($Msg) }
function Write-Ok     { param([string]$Msg) [Console]::Out.WriteLine("✅ $Msg") }
function Write-Fail   { param([string]$Msg) [Console]::Out.WriteLine("❌ $Msg") }
function Write-Note   { param([string]$Msg) [Console]::Out.WriteLine("   $Msg") }

function Exit-WithError {
  param([string]$Msg)
  Write-Fail $Msg
  [Console]::Out.Flush()
  exit 1
}

# ── Resolve install dir ───────────────────────────────────────────────────────

if ($InstallDir -eq "" -and $env:DRIFT_INSTALL_DIR -ne $null -and $env:DRIFT_INSTALL_DIR -ne "") {
  $InstallDir = $env:DRIFT_INSTALL_DIR.TrimEnd('\', '/')
}
if ($InstallDir -eq "") {
  $InstallDir = Join-Path $env:LOCALAPPDATA "Programs\drift"
}

$TargetExe = Join-Path $InstallDir "drift.exe"

# ── Resolve version ───────────────────────────────────────────────────────────

$ApiBase   = "https://api.github.com/repos/hojmark/drift"
$Headers   = @{
  "Accept"               = "application/vnd.github+json"
  "X-GitHub-Api-Version" = "2022-11-28"
  "User-Agent"           = "drift-installer"
}
if ($env:GITHUB_TOKEN) {
  $Headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
}

$Platform = "win-x64"

if ($Version -eq "") {
  Write-Step "🔍 Fetching latest version..."

  try {
    $Releases = Invoke-RestMethod -Uri "$ApiBase/releases" -Headers $Headers -ErrorAction Stop
  } catch {
    Exit-WithError "Failed to fetch releases from GitHub: $_"
  }

  $Release = $Releases |
    Where-Object { -not $_.prerelease } |
    Sort-Object published_at -Descending |
    Select-Object -First 1

  if ($null -eq $Release) {
    Exit-WithError "No stable releases found."
  }

  $Version = $Release.tag_name
  $Asset   = @($Release.assets) | Where-Object { $_.name -like "*_${Platform}.zip" } | Select-Object -First 1

} else {
  # Normalise: accept both "1.2.3" and "v1.2.3"
  if (-not $Version.StartsWith("v")) { $Version = "v$Version" }

  Write-Step "🔍 Fetching version $Version..."

  try {
    $Release = Invoke-RestMethod -Uri "$ApiBase/releases/tags/$Version" -Headers $Headers -ErrorAction Stop
  } catch {
    Exit-WithError "Tag '$Version' not found on GitHub. Check https://github.com/hojmark/drift/releases for available versions."
  }

  if ($null -eq $Release -or $null -eq $Release.tag_name) {
    Exit-WithError "Tag '$Version' not found on GitHub. Check https://github.com/hojmark/drift/releases for available versions."
  }

  $Asset = @($Release.assets) | Where-Object { $_.name -like "*_${Platform}.zip" } | Select-Object -First 1
}

if ($null -eq $Asset) {
  Exit-WithError "Could not find a Windows asset in release $Version."
}

$VersionDisplay = $Version.TrimStart('v')
#Write-Note "Version : $Version"
#Write-Note "Asset   : $($Asset.name)"

# ── Download ──────────────────────────────────────────────────────────────────

$TmpDir  = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $TmpDir | Out-Null

try {
  $ZipPath = Join-Path $TmpDir $Asset.name

  Write-Step "🔽 Downloading $($Asset.name)..."

  $DownloadHeaders = $Headers.Clone()
  $DownloadHeaders["Accept"] = "application/octet-stream"

  Invoke-WebRequest `
    -Uri "https://api.github.com/repos/hojmark/drift/releases/assets/$($Asset.id)" `
    -Headers $DownloadHeaders `
    -OutFile $ZipPath

  # ── Extract ─────────────────────────────────────────────────────────────────

  Write-Step "📦 Extracting..."
  Expand-Archive -Path $ZipPath -DestinationPath $TmpDir -Force

  $ExtractedExe = Join-Path $TmpDir "drift.exe"
  if (-not (Test-Path $ExtractedExe)) {
    Exit-WithError "drift.exe not found in archive."
  }

  # ── Install ──────────────────────────────────────────────────────────────────

  Write-Step "🚀 Installing..."
  if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
  }

  Move-Item -Path $ExtractedExe -Destination $TargetExe -Force

  # ── PATH ─────────────────────────────────────────────────────────────────────

  $UserPath = [System.Environment]::GetEnvironmentVariable("PATH", "User")
  if ($null -eq $UserPath) { $UserPath = "" }
  $PathEntries = $UserPath -split ";" | Where-Object { $_ -ne "" }

  if ($PathEntries -notcontains $InstallDir) {
    Write-Note "Adding $InstallDir to user PATH..."
    $NewPath = ($PathEntries + $InstallDir) -join ";"
    [System.Environment]::SetEnvironmentVariable("PATH", $NewPath, "User")
    Write-Note "Restart your terminal for PATH changes to take effect."
  }

} finally {
  Remove-Item -Path $TmpDir -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Ok "Installed Drift CLI $VersionDisplay successfully!"

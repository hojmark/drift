#Requires -Version 5.1

[CmdletBinding()]
param(
  [Parameter(Position = 0)]
  [string] $Version = "",
  [string] $InstallDir = ""
)

$ErrorActionPreference = "Stop"

# ── Helpers ──────────────────────────────────────────────────────────────────

# pwsh (PowerShell Core) supports Unicode/emoji output; Windows PowerShell 5.1 does not.
$UseEmoji = $PSVersionTable.PSEdition -eq "Core"

# Ensure emoji characters survive the stdout pipe/redirect on all Windows terminals.
if ($UseEmoji) {
  [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
}

# Build emoji strings at runtime so no non-ASCII bytes appear in this source file.
# PowerShell 5.1 reads files using the system code page when no BOM is present,
# which would corrupt raw UTF-8 multi-byte sequences embedded in string literals.
$EmojiSearch    = [char]::ConvertFromUtf32(0x1F50D)  # 🔍
$EmojiDown      = [char]::ConvertFromUtf32(0x1F53D)  # 🔽
$EmojiPackage   = [char]::ConvertFromUtf32(0x1F4E6)  # 📦
$EmojiRocket    = [char]::ConvertFromUtf32(0x1F680)  # 🚀
$EmojiOk        = [char]::ConvertFromUtf32(0x2705)   # ✅
$EmojiFail      = [char]::ConvertFromUtf32(0x274C)   # ❌

function Write-Step {
  param([string]$Emoji, [string]$Msg)
  if ($UseEmoji) { Write-Output "$Emoji $Msg" } else { Write-Output ">> $Msg" }
}
function Write-Ok   { param([string]$Msg) if ($UseEmoji) { Write-Output "$EmojiOk $Msg" } else { Write-Output "[OK] $Msg" } }
function Write-Fail { param([string]$Msg) if ($UseEmoji) { Write-Output "$EmojiFail $Msg" } else { Write-Output "[ERROR] $Msg" } }
function Write-Note { param([string]$Msg) Write-Output "   $Msg" }

function Exit-WithError {
  param([string]$Msg)
  Write-Fail $Msg
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
  Write-Step $EmojiSearch "Fetching latest version..."

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

  Write-Step $EmojiSearch "Fetching version $Version..."

  try {
    $Release = Invoke-RestMethod -Uri "$ApiBase/releases/tags/$Version" -Headers $Headers -ErrorAction Stop
  } catch {
    Exit-WithError "Tag '$Version' not found on GitHub. Check https://github.com/hojmark/drift/releases for available versions."
  }

  if ($null -eq $Release -or [string]::IsNullOrEmpty($Release.tag_name)) {
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

  Write-Step $EmojiDown "Downloading $($Asset.name)..."

  $DownloadHeaders = $Headers.Clone()
  $DownloadHeaders["Accept"] = "application/octet-stream"

  Invoke-WebRequest `
    -Uri "https://api.github.com/repos/hojmark/drift/releases/assets/$($Asset.id)" `
    -Headers $DownloadHeaders `
    -OutFile $ZipPath

  # ── Extract ─────────────────────────────────────────────────────────────────

  Write-Step $EmojiPackage "Extracting..."
  Expand-Archive -Path $ZipPath -DestinationPath $TmpDir -Force

  $ExtractedExe = Join-Path $TmpDir "drift.exe"
  if (-not (Test-Path $ExtractedExe)) {
    Exit-WithError "drift.exe not found in archive."
  }

  # ── Install ──────────────────────────────────────────────────────────────────

  Write-Step $EmojiRocket "Installing..."
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

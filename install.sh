#!/bin/bash

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
GRAY='\033[90m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Utility functions
print_error() { echo -e "${RED}❌ $1${NC}"; }
exit_with_error() { print_error "$1"; exit 1; }
run_utility() {
  "$@" 2>&1 | while IFS= read -r line; do
    printf "\t${GRAY}%s${NC}\n" "$line"
  done
}

# Check installer prerequisites
for cmd in curl jq tar; do
 if ! command -v "$cmd" &>/dev/null; then
   exit_with_error "Required command '$cmd' not found. Please install it."
 fi
done

# nmap not required any longer
# TODO request to install via CLI instead, or additionally?
# Check if nmap is installed
if ! command -v nmap &>/dev/null && false; then
  echo "🔍 Installing prerequisites..."
  echo "Drift depends on Nmap"
  #echo -e "${YELLOW}'nmap' not found.${NC}"
  read -pr "Do you want to install 'nmap' now? [Y/n]: " install_nmap
  install_nmap=${install_nmap:-Y}
  if [[ "$install_nmap" =~ ^[Yy]$ ]]; then
    if command -v apt &>/dev/null; then # Debian, Ubuntu
      run_utility sudo apt update && sudo apt install -y nmap
    elif command -v dnf &>/dev/null; then # Fedora, CentOS/Rocky Linux/AlmaLinux, RHEL
      run_utility sudo dnf install -y nmap
    elif command -v pacman &>/dev/null; then # Arch, Manjaro
      run_utility sudo pacman -Sy --noconfirm nmap
    # TODO support macOS
    #elif command -v brew &>/dev/null; then # macOS
    #  run_utility brew install nmap
    else
      exit_with_error "'nmap' is required but could not be installed automatically. Please install it manually."
    fi
  else
    exit_with_error "'nmap' is required but was not installed."
  fi
fi

# Setup temp dir and cleanup trap
TMP_DIR=$(mktemp -d)
trap 'rm -rf "$TMP_DIR"' EXIT

# Defaults
VERBOSE=false
VERSION="" # I.e. latest
PLATFORM="linux-x64"
#GITHUB_TOKEN=""
if [ -n "${DRIFT_INSTALL_DIR:-}" ]; then
  TARGET="${DRIFT_INSTALL_DIR%/}/drift"
  TARGET_ROOT=""
else
  TARGET="/usr/local/bin/drift"
  TARGET_ROOT="/usr/bin/drift"
fi

# Parse arguments
while [ $# -gt 0 ]; do
  case "$1" in
    --verbose)
      VERBOSE=true
      shift
      ;;
    v*) # Git tag e.g.: v1.2.3
      VERSION="$1"
      shift
      ;;
    *)
      exit_with_error "Unknown argument: $1"
      ;;
  esac
done

# Enable verbose mode if requested
if [ "$VERBOSE" = true ]; then
  set -euo pipefail -x
  echo -e "${YELLOW}🐞 Verbose mode is ON${NC}"
else
  set -euo pipefail
fi

# Get asset ID from GitHub
if [ -z "$VERSION" ]; then
  echo "🔍 Fetching latest version..."

  RESP=$(curl -sSL \
    -H "Accept: application/vnd.github+json" \
    ${GITHUB_TOKEN:+-H "Authorization: Bearer $GITHUB_TOKEN"} \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "https://api.github.com/repos/hojmark/drift/releases")

  PRERELEASE=$(echo "$RESP" | jq '[.[] | select(.prerelease == true)] | sort_by(.published_at) | reverse | .[0]')
  VERSION=$(echo "$PRERELEASE" | jq -r '.tag_name')
  ASSET_ID=$(echo "$PRERELEASE" | jq -r ".assets[] | select(.name | endswith(\"${PLATFORM}.tar.gz\")) | .id")

else
  echo "🔍 Fetching version ${VERSION}..."

  RESP=$(curl -sSL \
    -H "Accept: application/vnd.github+json" \
    ${GITHUB_TOKEN:+-H "Authorization: Bearer $GITHUB_TOKEN"} \
    -H "X-GitHub-Api-Version: 2022-11-28" \
    "https://api.github.com/repos/hojmark/drift/releases/tags/${VERSION}")

  STATUS=$(echo "$RESP" | jq -r '.status // empty')
  if [ "$STATUS" = "404" ]; then
    exit_with_error "Tag '${VERSION}' not found on GitHub. Please ensure you typed the tag correctly, or see the list of available releases: https://github.com/hojmark/drift/releases"
  fi
  ASSET_ID=$(echo "$RESP" | jq -r ".assets[] | select(.name | endswith(\"${PLATFORM}.tar.gz\")) | .id")
fi

if [ -z "$VERSION" ] || [ "$VERSION" = "null" ] || [ -z "$ASSET_ID" ] || [ "$ASSET_ID" = "null" ]; then
  exit_with_error "Failed to retrieve valid version (${VERSION}) or asset ID (${ASSET_ID})."
fi

# Download
FILENAME="drift_${VERSION#v}_${PLATFORM}.tar.gz"

cd "$TMP_DIR" || exit_with_error "Failed to enter temp directory ${TMP_DIR}"

echo -e "🔽 Downloading ${BOLD}${FILENAME}${NC}..."
curl -sSL \
  -H "Accept: application/octet-stream" \
  ${GITHUB_TOKEN:+-H "Authorization: Bearer $GITHUB_TOKEN"} \
  -o "${FILENAME}" \
  "https://api.github.com/repos/hojmark/drift/releases/assets/${ASSET_ID}"

# Extract
echo "📦 Extracting..."
tar -xzf "${FILENAME}"

# Install
echo "🚀 Installing..."

chmod +x "drift" || exit_with_error "Could not make the drift binary executable."

if [ -w "$(dirname "$TARGET")" ]; then
  mv "drift" "$TARGET" || exit_with_error "Failed to move drift binary to ${TARGET}"
  #mv "drift.dbg" "$TARGET.dbg" || exit_with_error "Failed to move drift.dbg binary to ${TARGET}"
else
  if ! command -v sudo &>/dev/null; then
    exit_with_error "'sudo' is required but not found. Please install sudo or run this script as root."
  fi
  sudo mv "drift" "$TARGET" || exit_with_error "Failed to move drift binary to ${TARGET}"
  #sudo mv "drift.dbg" "$TARGET.dbg" || exit_with_error "Failed to move drift.dbg binary to ${TARGET}"
fi

if [ -n "$TARGET_ROOT" ]; then # $TARGET_ROOT is specified -> create symlink: root -> local
  if [ -f "$TARGET_ROOT" ] && [ ! -L "$TARGET_ROOT" ]; then
    # Unexpected, but possible -> err on the side of caution and do not overwrite such file
    exit_with_error "Refusing to create symlink $TARGET_ROOT -> $TARGET: a regular file with the same name already exists at $TARGET_ROOT"
  fi
  if [ -L "$TARGET_ROOT" ]; then
    actual_target="$(readlink -f "$TARGET_ROOT")"
    expected_target="$(readlink -f "$TARGET")"
    if [ "$actual_target" = "$expected_target" ]; then
      #echo "  ✅ Symlink already points to the correct file: $actual_target"
      :
    else
      exit_with_error "Refusing to update symlink: an existing symlink at $TARGET_ROOT points elsewhere and not to the expected binary at $TARGET"
    fi
  else
    #echo "  ➕ Creating new symlink: $TARGET_ROOT → $TARGET"
    sudo ln -s "$TARGET" "$TARGET_ROOT"
    #sudo ln -s "$TARGET.dbg" "$TARGET_ROOT.dbg"
  fi
  
  #echo "🔍 Verifying installation..."
  #if command -v drift >/dev/null; then
  #  echo "  🎉 'drift' installed and available in PATH"
  #else
  #  echo "  ⚠️ Warning: 'drift' may not be in your current shell PATH"
  #fi
fi

# 🚀
echo -e "${GREEN}✅ ${BOLD}Installed Drift CLI ${VERSION#v} successfully!${NC}"
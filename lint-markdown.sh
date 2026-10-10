#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if ! command -v markdownlint >/dev/null 2>&1; then
  if [[ -z "${CI:-}" ]]; then
    read -rp "Do you want to install markdownlint-cli? [y/N]: " INSTALL_DEPS
    if [[ ! "$INSTALL_DEPS" =~ ^[Yy]$ ]]; then
      echo "Markdown lint cancelled; markdownlint-cli is required."
      exit 1
    fi
  fi

  npm i -g markdownlint-cli@0.49.1
fi

markdownlint --config .markdownlint.jsonc '**/*.md' "$@"

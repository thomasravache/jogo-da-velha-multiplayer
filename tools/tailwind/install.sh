#!/usr/bin/env bash
# Instala o Tailwind CSS standalone (versão pinada em version.txt) em tools/tailwind/bin.
# O binário só recebe permissão de execução depois de o sha256 casar com o sha256sums.txt do release.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="$(tr -d '[:space:]' < "$DIR/version.txt")"
BASE="https://github.com/tailwindlabs/tailwindcss/releases/download/v${VERSION}"
BIN_DIR="$DIR/bin"
TARGET="$BIN_DIR/tailwindcss"

os="$(uname -s)"; arch="$(uname -m)"
case "$os-$arch" in
  Darwin-arm64)  asset="tailwindcss-macos-arm64" ;;
  Darwin-x86_64) asset="tailwindcss-macos-x64" ;;
  Linux-x86_64)  asset="tailwindcss-linux-x64" ;;
  Linux-aarch64|Linux-arm64) asset="tailwindcss-linux-arm64" ;;
  *) echo "install.sh: plataforma sem binário oficial: $os-$arch" >&2; exit 1 ;;
esac

if [ -x "$TARGET" ] && [ "$("$TARGET" --help 2>/dev/null | head -1 | grep -o '[0-9]\+\.[0-9]\+\.[0-9]\+' || true)" = "$VERSION" ]; then
  echo "Tailwind $VERSION já instalado."
  exit 0
fi

sha256_of() { if command -v sha256sum >/dev/null 2>&1; then sha256sum "$1" | cut -d' ' -f1; else shasum -a 256 "$1" | cut -d' ' -f1; fi; }

tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
curl -fsSL -o "$tmp/$asset" "$BASE/$asset"
curl -fsSL -o "$tmp/sha256sums.txt" "$BASE/sha256sums.txt"

expected="$(grep -E "[ *]\.?/?${asset}\$" "$tmp/sha256sums.txt" | head -1 | cut -d' ' -f1)"
actual="$(sha256_of "$tmp/$asset")"
if [ -z "$expected" ] || [ "$expected" != "$actual" ]; then
  echo "install.sh: sha256 divergente para $asset (esperado '$expected', obtido '$actual')" >&2
  exit 1
fi

mkdir -p "$BIN_DIR"
mv "$tmp/$asset" "$TARGET"
chmod +x "$TARGET"
echo "Tailwind $VERSION instalado em $TARGET (sha256 verificado)."

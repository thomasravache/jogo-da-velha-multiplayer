#!/usr/bin/env bash
# Gera wwwroot/css/cyber-arena.css a partir de Styles/cyber-arena.input.css.
#   build.sh           regenera o arquivo versionado
#   build.sh --check   gera em arquivo temporário e falha (exit 1) se diferir do versionado (drift)
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB="$DIR/../../src/TicTacToe/TicTacToe.Web"
BIN="$DIR/bin/tailwindcss"
INPUT="$WEB/Styles/cyber-arena.input.css"
OUTPUT="$WEB/wwwroot/css/cyber-arena.css"

if [ ! -x "$BIN" ]; then
  echo "build.sh: Tailwind não instalado. Rode tools/tailwind/install.sh" >&2
  exit 1
fi

if [ "${1:-}" = "--check" ]; then
  tmp="$(mktemp)"; trap 'rm -f "$tmp"' EXIT
  "$BIN" -i "$INPUT" -o "$tmp" --minify >/dev/null
  if ! diff -q "$tmp" "$OUTPUT" >/dev/null; then
    echo "build.sh --check: drift em cyber-arena.css. Rode tools/tailwind/build.sh e commite o resultado." >&2
    exit 1
  fi
  echo "cyber-arena.css está atualizado."
  exit 0
fi

mkdir -p "$(dirname "$OUTPUT")"
"$BIN" -i "$INPUT" -o "$OUTPUT" --minify
echo "Gerado $OUTPUT"

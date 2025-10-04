#!/usr/bin/env bash
set -euo pipefail

# Resolve repo root no matter where the script is called from
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [ -z "$ROOT" ]; then
  ROOT="$(cd "$(dirname "$0")/.." && pwd)"
fi

ENGINE_PROJ="$ROOT/engine/src/SchackBot.Engine/SchackBot.Engine.csproj"
OUT_DLL="$ROOT/engine/src/SchackBot.Engine/bin/Release/netstandard2.1/SchackBot.Engine.dll"
DEST="$ROOT/visualizer/Assets/Plugins/HugoChessEngine"

dotnet build "$ENGINE_PROJ" -c Release -f netstandard2.1
mkdir -p "$DEST"
cp "$OUT_DLL" "$DEST/"
echo "Updated $(basename "$OUT_DLL") → $DEST"

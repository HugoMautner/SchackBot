#!/usr/bin/env bash
# build.sh - publish SchackBot.UciAdapter as a single-file self-contained executable
# Run from SchackBot/scripts: ./build.sh win-x64 | linux-x64 | osx-x64 | osx-arm64 | all [zip]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"   # SchackBot root
PROJECT_PATH="$ROOT_DIR/engine/src/SchackBot.UciAdapter/SchackBot.UciAdapter.csproj"

if [ ! -f "$PROJECT_PATH" ]; then
  echo "ERROR: project not found at: $PROJECT_PATH"
  exit 2
fi

if [ "$#" -lt 1 ]; then
  echo "Usage: $0 <rid|all> [zip]"
  echo "RIDs: win-x64, linux-x64, osx-x64, osx-arm64"
  exit 1
fi

RID="$1"
DO_ZIP=false
if [ "${2-}" = "zip" ]; then DO_ZIP=true; fi

RIDS_TO_BUILD=()
if [ "$RID" = "all" ]; then
  RIDS_TO_BUILD=(win-x64 linux-x64 osx-x64 osx-arm64)
else
  RIDS_TO_BUILD=("$RID")
fi

for rid in "${RIDS_TO_BUILD[@]}"; do
  OUT_DIR="$ROOT_DIR/publish/$rid"
  echo "Publishing for RID=$rid -> $OUT_DIR"
  mkdir -p "$OUT_DIR"
  dotnet publish "$PROJECT_PATH" -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUT_DIR"
  echo " -> build finished: $OUT_DIR"

  if [ "$DO_ZIP" = true ]; then
    ZIP_PATH="$ROOT_DIR/publish/${rid}.zip"
    (cd "$OUT_DIR" && zip -r -q "$ZIP_PATH" .)
    echo " -> zipped: $ZIP_PATH"
  fi
done

echo "All done."

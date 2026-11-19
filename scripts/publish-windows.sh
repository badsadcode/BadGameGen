#!/usr/bin/env bash
# Cross-publishes the Windows executables from Linux/macOS. See publish-windows.ps1.
set -euo pipefail
cd "$(dirname "$0")/.."
(cd frontend && npm ci && npm run build)
dotnet test tests/BadGameGen.Core.Tests -c Release
COMMON=(-c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None)
dotnet publish src/BadGameGen.Desktop "${COMMON[@]}" -p:IncludeNativeLibrariesForSelfExtract=true -o release/desktop
dotnet publish src/BadGameGen.Cli "${COMMON[@]}" -o release/cli
cp release/desktop/BadGameGen.exe release/cli/bgg.exe release/
echo "Done: release/BadGameGen.exe and release/bgg.exe"

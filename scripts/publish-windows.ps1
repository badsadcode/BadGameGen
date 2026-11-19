# Builds single-file, self-contained Windows executables into ./release
#   release/BadGameGen.exe  - the desktop app (UI, packs and themes embedded)
#   release/bgg.exe         - the command-line companion
# Requirements: .NET 10 SDK, Node.js 20+. Run from the repository root:
#   powershell -ExecutionPolicy Bypass -File scripts/publish-windows.ps1
# (scripts/publish-windows.sh does the same from Linux/macOS: cross-publishing works.)
$ErrorActionPreference = "Stop"
Push-Location frontend
npm ci
npm run build
Pop-Location
dotnet test tests/BadGameGen.Core.Tests -c Release
$common = @("-c", "Release", "-r", "win-x64", "--self-contained", "true",
            "-p:PublishSingleFile=true", "-p:EnableCompressionInSingleFile=true", "-p:DebugType=None")
dotnet publish src/BadGameGen.Desktop @common -p:IncludeNativeLibrariesForSelfExtract=true -o release/desktop
dotnet publish src/BadGameGen.Cli @common -o release/cli
Copy-Item release/desktop/BadGameGen.exe release/BadGameGen.exe -Force
Copy-Item release/cli/bgg.exe release/bgg.exe -Force
Write-Host "Done: release/BadGameGen.exe and release/bgg.exe" -ForegroundColor Magenta

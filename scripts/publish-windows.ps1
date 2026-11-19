# Builds a self-contained BadGameGen for Windows into ./publish/BadGameGen
# Requirements: .NET 10 SDK, Node.js 20+. Run from the repository root:
#   powershell -ExecutionPolicy Bypass -File scripts/publish-windows.ps1
$ErrorActionPreference = "Stop"
Push-Location frontend
npm ci
npm run build
Pop-Location
dotnet test tests/BadGameGen.Core.Tests -c Release
dotnet publish src/BadGameGen.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/BadGameGen
Write-Host "Done. Run publish/BadGameGen/BadGameGen.exe" -ForegroundColor Magenta

@echo off
setlocal
cd /d "%~dp0.."
if not exist artifacts mkdir artifacts
if not exist artifacts\publish mkdir artifacts\publish

dotnet publish src\ARNetDiscovery.Wpf\ARNetDiscovery.Wpf.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:PublishTrimmed=false ^
  -o artifacts\publish\win-x64

if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo.
echo Publish completed: artifacts\publish\win-x64
endlocal

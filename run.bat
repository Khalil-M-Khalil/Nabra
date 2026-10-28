@echo off
setlocal
cd /d "%~dp0"
if exist "dist\Nabra.exe" (
  start "" "dist\Nabra.exe"
  exit /b 0
)
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [!] .NET SDK 8 not found and dist\Nabra.exe does not exist. Run build.bat first.
  pause
  exit /b 1
)
dotnet run --project src\Nabra\Nabra.csproj -c Release

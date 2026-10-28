@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [!] .NET SDK 8 not found. Install it from https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)

echo [1/3] Running conversion self-tests...
dotnet run --project tests\Nabra.SelfTest\Nabra.SelfTest.csproj -c Release
if errorlevel 1 (
  echo [!] Self-tests failed. Build stopped.
  pause
  exit /b 1
)

echo.
echo [2/3] Building Nabra (Release)...
if exist dist rmdir /s /q dist
dotnet publish src\Nabra\Nabra.csproj -c Release -r win-x64 --self-contained false -o dist -p:DebugType=none
if errorlevel 1 (
  echo [!] Build failed. Copy the error text and send it to your developer.
  pause
  exit /b 1
)

echo.
echo [3/3] Done.  Run:  dist\Nabra.exe   (or double-click run.bat)
pause

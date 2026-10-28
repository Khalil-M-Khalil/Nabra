@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [!] .NET SDK 8 not found. Install it from https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet run --project tests\Nabra.SelfTest\Nabra.SelfTest.csproj -c Release
set RC=%ERRORLEVEL%
echo.
if %RC%==0 (echo ALL TESTS PASSED) else (echo SOME TESTS FAILED)
pause
exit /b %RC%

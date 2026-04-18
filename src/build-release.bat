@echo off
setlocal
cd /d %~dp0

echo [1/3] Checking dotnet...
where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet SDK not found. Please install .NET 8 SDK first.
  echo https://dotnet.microsoft.com/en-us/download/dotnet/8.0
  pause
  exit /b 1
)

echo [2/3] Publishing self-contained single-file WPF app...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo [3/3] Done.
echo Output folder: %~dp0publish
if exist "%~dp0publish\WslBackupManager.exe" (
  echo Executable: %~dp0publish\WslBackupManager.exe
  start "" "%~dp0publish\WslBackupManager.exe"
) else (
  echo Publish completed, but EXE name may differ. Please check the publish folder.
  start "" "%~dp0publish"
)
pause

@echo off
setlocal

rem Usage: rebuild_and_run.bat [port] [--headless]
rem This script stops only AssetRipper processes started from this repository.
rem Then it cleans build output, rebuilds the solution, and starts the GUI.

set "ROOT=%~dp0"
for %%I in ("%ROOT%.") do set "ROOT_FULL=%%~fI"
set "ASSET_RIPPER_ROOT=%ROOT_FULL%"

set "PORT=%~1"
if "%PORT%"=="" set "PORT=58910"
set "ASSET_RIPPER_PORT=%PORT%"

set "HEADLESS_ARG="
if /I "%~2"=="--headless" set "HEADLESS_ARG=--headless"

call :EnsureDotnet10
if errorlevel 1 exit /b %ERRORLEVEL%

echo [INFO] Stopping old AssetRipper process for this repository
powershell -NoProfile -ExecutionPolicy Bypass -Command "$root = (Resolve-Path -LiteralPath $env:ASSET_RIPPER_ROOT).Path; $items = Get-CimInstance Win32_Process | Where-Object { ($_.Name -eq 'AssetRipper.GUI.Free.exe' -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -and $_.CommandLine.IndexOf('AssetRipper.GUI.Free.csproj', [StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.CommandLine.IndexOf($root, [StringComparison]::OrdinalIgnoreCase) -ge 0) }; foreach ($item in $items) { Write-Host ('[INFO] Stop process ' + $item.ProcessId + ' ' + $item.Name); Stop-Process -Id $item.ProcessId -Force }"
if errorlevel 1 exit /b %ERRORLEVEL%

echo [INFO] Shutting down dotnet build server
"%DOTNET_EXE%" build-server shutdown >nul 2>nul

echo [INFO] Cleaning build output
powershell -NoProfile -ExecutionPolicy Bypass -Command "$root = (Resolve-Path -LiteralPath $env:ASSET_RIPPER_ROOT).Path; $target = Join-Path $root 'Source\0Bins'; if (Test-Path -LiteralPath $target) { $resolved = (Resolve-Path -LiteralPath $target).Path; if (-not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target path is outside repository' }; Remove-Item -LiteralPath $resolved -Recurse -Force; Write-Host '[INFO] Cleaned Source\0Bins' } else { Write-Host '[INFO] Source\0Bins does not exist' }"
if errorlevel 1 exit /b %ERRORLEVEL%

echo [INFO] Building solution
"%DOTNET_EXE%" build "%ROOT_FULL%\AssetRipper.slnx"
if errorlevel 1 (
	echo [FAIL] Build failed
	exit /b %ERRORLEVEL%
)

echo [INFO] Starting AssetRipper GUI
start "AssetRipper GUI Free" /D "%ROOT_FULL%" "%DOTNET_EXE%" run --project "%ROOT_FULL%\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj" --no-build -- --port %PORT% %HEADLESS_ARG%

if "%PORT%"=="0" (
	echo [OK] Started with a random port. Check the app window for the listening URL.
	exit /b 0
)

echo [INFO] Waiting for http://127.0.0.1:%PORT%/
powershell -NoProfile -ExecutionPolicy Bypass -Command "$url = 'http://127.0.0.1:' + $env:ASSET_RIPPER_PORT + '/'; for ($i = 0; $i -lt 30; $i++) { try { $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2; if ($response.StatusCode -eq 200) { Write-Host ('[OK] Server is ready ' + $url); exit 0 } } catch { }; Start-Sleep -Seconds 1 }; Write-Host ('[WARN] Server did not respond in time ' + $url); exit 1"
exit /b %ERRORLEVEL%

:EnsureDotnet10
set "DOTNET_DIR=%TEMP%\codex-dotnet-10"
set "DOTNET_EXE=%DOTNET_DIR%\dotnet.exe"

if exist "%DOTNET_EXE%" (
	"%DOTNET_EXE%" --list-sdks 2>nul | findstr /R /C:"^10\." >nul
	if not errorlevel 1 (
		set "DOTNET_ROOT=%DOTNET_DIR%"
		set "PATH=%DOTNET_DIR%;%PATH%"
		echo [INFO] Using local .NET SDK: %DOTNET_EXE%
		exit /b 0
	)
)

where dotnet >nul 2>nul
if not errorlevel 1 (
	for /f "delims=" %%D in ('dotnet --list-sdks 2^>nul ^| findstr /R /C:"^10\."') do (
		set "DOTNET_EXE=dotnet"
		echo [INFO] Using system .NET SDK: %%D
		exit /b 0
	)
)

echo [INFO] .NET 10 SDK was not found. Installing into %DOTNET_DIR%
set "DOTNET_INSTALL=%TEMP%\dotnet-install.ps1"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $env:DOTNET_INSTALL"
if errorlevel 1 (
	echo [FAIL] Failed to download dotnet-install.ps1
	exit /b %ERRORLEVEL%
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%DOTNET_INSTALL%" -Channel 10.0 -InstallDir "%DOTNET_DIR%" -Architecture x64 -NoPath
if errorlevel 1 (
	echo [FAIL] Failed to install .NET 10 SDK
	exit /b %ERRORLEVEL%
)

if not exist "%DOTNET_EXE%" (
	echo [FAIL] dotnet.exe was not found after installation
	exit /b 1
)

"%DOTNET_EXE%" --list-sdks 2>nul | findstr /R /C:"^10\." >nul
if errorlevel 1 (
	echo [FAIL] Installed SDK does not include .NET 10
	exit /b 1
)

set "DOTNET_ROOT=%DOTNET_DIR%"
set "PATH=%DOTNET_DIR%;%PATH%"
echo [INFO] Using local .NET SDK: %DOTNET_EXE%
exit /b 0

@echo off
chcp 65001 >nul
setlocal
rem
rem start.bat - AssetRipper GUI
rem
rem Usage:
rem   start.bat              port 58910
rem   start.bat 8080         custom port
rem   start.bat 8080 --headless

pushd "%~dp0"
set "ROOT_FULL=%CD%"
popd

set "PROJECT=%ROOT_FULL%\Source\AssetRipper.GUI.Free\AssetRipper.GUI.Free.csproj"
set "HOST=127.0.0.1"

set "PORT=%~1"
if "%PORT%"=="" set "PORT=58910"
set "HEADLESS_ARG="
if /I "%~2"=="--headless" set "HEADLESS_ARG=--headless"

echo.
echo [INFO] ==============================
echo [INFO]   AssetRipper Quick Start
echo [INFO] ==============================
echo.

rem ============================================================
rem Find dotnet
rem ============================================================
set "DOTNET_EXE="

set "LOCAL_DOTNET=%TEMP%\codex-dotnet-10\dotnet.exe"
if exist "%LOCAL_DOTNET%" (
    "%LOCAL_DOTNET%" --list-sdks 2>nul | findstr /R /C:"^10\." >nul
    if not errorlevel 1 (
        set "DOTNET_EXE=%LOCAL_DOTNET%"
        set "DOTNET_ROOT=%TEMP%\codex-dotnet-10"
        set "PATH=%TEMP%\codex-dotnet-10;%PATH%"
        echo [INFO] Using local .NET SDK: %LOCAL_DOTNET%
        goto :found_dotnet
    )
)

where dotnet >nul 2>nul
if not errorlevel 1 (
    for /f "delims=" %%D in ('dotnet --list-sdks 2^>nul ^| findstr /R /C:"^10\."') do (
        set "DOTNET_EXE=dotnet"
        echo [INFO] Using system .NET SDK: %%D
        goto :found_dotnet
    )
)

echo [FAIL] dotnet not found, please install .NET 10 SDK
pause
exit /b 1

:found_dotnet

rem ============================================================
rem Kill process on port
rem ============================================================
echo [INFO] Checking port %PORT%...

set "PORT_BUSY=0"
for /f "tokens=5" %%P in ('netstat -ano 2^>nul ^| findstr ":%PORT% " ^| findstr LISTENING') do (
    set "PORT_BUSY=1"
    for /f "tokens=1 delims=," %%N in ('tasklist /FI "PID eq %%P" /FO CSV /NH 2^>nul') do (
        echo [WARN] Port %PORT% in use: PID=%%P Process=%%~N, killing...
        taskkill /F /PID %%P >nul 2>nul
    )
)

if "%PORT_BUSY%"=="1" (
    timeout /t 2 /nobreak >nul
)

set "STILL_BUSY=0"
for /f "tokens=5" %%P in ('netstat -ano 2^>nul ^| findstr ":%PORT% " ^| findstr LISTENING') do (
    set "STILL_BUSY=1"
)

if "%STILL_BUSY%"=="1" (
    echo [FAIL] Cannot free port %PORT%, please kill manually
    pause
    exit /b 1
)

echo [OK] Port %PORT% available

rem ============================================================
rem Start
rem ============================================================
echo [INFO] Starting AssetRipper GUI...
echo [INFO] URL: http://%HOST%:%PORT%/

start "AssetRipper GUI" /D "%ROOT_FULL%" "%DOTNET_EXE%" run --project "%PROJECT%" --no-build -- --port %PORT% %HEADLESS_ARG%

rem ============================================================
rem Wait for ready
rem ============================================================
echo [INFO] Waiting for server...
set /a WAIT_COUNT=0

:wait_loop
if %WAIT_COUNT% GEQ 30 (
    echo [WARN] Server not responding in 30s, may still be starting
    goto :done
)

powershell -NoProfile -Command "try{Invoke-WebRequest -Uri 'http://127.0.0.1:%PORT%/' -UseBasicParsing -TimeoutSec 2|Out-Null;exit 0}catch{exit 1}" >nul 2>nul
if not errorlevel 1 (
    echo [OK] Server ready: http://%HOST%:%PORT%/
    goto :done
)

set /a WAIT_COUNT+=1
timeout /t 1 /nobreak >nul
goto :wait_loop

:done
echo.
echo [INFO] Done. You can close this window.

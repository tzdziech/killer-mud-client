@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
cd /d "%ROOT%"

rem ---- Odczyt wersji z Directory.Build.props ----
for /f "tokens=*" %%A in ('findstr "<Version>" Directory.Build.props') do (
    set "LINE=%%A"
)
set "VERSION=!LINE:*<Version>=!"
set "VERSION=!VERSION:</Version>=!"
set "VERSION=!VERSION: =!"
if "%VERSION%"=="" set "VERSION=0.1.0"

rem ---- Parametr: beta / release (domyslnie release) ----
set "FLAVOR=%~1"
if "%FLAVOR%"=="" set "FLAVOR=release"
if /i "%FLAVOR%"=="beta" (
    set "SUFFIX=-beta"
) else (
    set "FLAVOR=release"
    set "SUFFIX="
)

set "PROJECT=src\MudClient.App\MudClient.App.csproj"
set "BASE_OUTDIR=%ROOT%publish\win-x64"
set "OUTDIR=%BASE_OUTDIR%\%FLAVOR%"
set "USER_APP_NAME=KillerMudClient-%VERSION%%SUFFIX%"
set "ADMIN_APP_NAME=KillerMudClient-%VERSION%%SUFFIX%-admin"

if not exist "%PROJECT%" (
    echo ERROR: Project not found at %PROJECT%
    exit /b 1
)

echo ============================================================
echo  MudClient.App  %VERSION%  ^(%FLAVOR%^)
echo  Self-contained win-x64, single-file
echo ============================================================
echo.
echo Project : %PROJECT%
echo Output  : %OUTDIR%
echo User    : %USER_APP_NAME%.exe
echo Admin   : %ADMIN_APP_NAME%.exe
echo.

rem ---- Czysty katalog wyjsciowy zapobiega pozostawieniu plikow starego release ----
if exist "%OUTDIR%" (
    echo Cleaning: %OUTDIR%
    rmdir /s /q "%OUTDIR%"
    if exist "%OUTDIR%" (
        echo ERROR: Could not clean output directory: %OUTDIR%
        exit /b 1
    )
)

mkdir "%OUTDIR%"
if errorlevel 1 (
    echo ERROR: Could not create output directory: %OUTDIR%
    exit /b 1
)

call :PublishVariant User "%USER_APP_NAME%"
if errorlevel 1 exit /b %ERRORLEVEL%

call :PublishVariant Admin "%ADMIN_APP_NAME%"
if errorlevel 1 exit /b %ERRORLEVEL%

echo.
echo ============================================================
echo  Publish successful.
echo  User executable : %OUTDIR%\%USER_APP_NAME%.exe
echo  Admin executable: %OUTDIR%\%ADMIN_APP_NAME%.exe
echo ============================================================
echo.
echo  Uzycie: publish.bat [beta^|release]
echo.

endlocal
exit /b 0

:PublishVariant
set "BUILD_CONFIGURATION=%~1"
set "APP_NAME=%~2"
set "STAGING_DIR=%OUTDIR%\%BUILD_CONFIGURATION%"

echo.
echo  Publishing %BUILD_CONFIGURATION% build...

dotnet publish "%PROJECT%" ^
    --configuration "%BUILD_CONFIGURATION%" ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%STAGING_DIR%" ^
    /p:PublishSingleFile=true ^
    /p:IncludeAllContentForSelfExtract=true ^
    /p:DebugType=None ^
    /p:DebugSymbols=false ^
    /p:NativeDebugSymbols=false ^
    /p:Version=%VERSION%

if errorlevel 1 exit /b %ERRORLEVEL%

if not exist "%STAGING_DIR%\MudClient.App.exe" (
    echo ERROR: Expected executable was not produced for %BUILD_CONFIGURATION%.
    exit /b 1
)

move /y "%STAGING_DIR%\MudClient.App.exe" "%OUTDIR%\%APP_NAME%.exe" >nul
if errorlevel 1 exit /b %ERRORLEVEL%

rmdir /s /q "%STAGING_DIR%"
if exist "%STAGING_DIR%" (
    echo ERROR: Could not clean temporary output for %BUILD_CONFIGURATION%.
    exit /b 1
)

exit /b 0

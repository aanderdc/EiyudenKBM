@echo off
setlocal EnableDelayedExpansion

set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Eiyuden Chronicle"

echo ============================================================
echo   EiyudenKBM - Desinstalador Limpio
echo ============================================================
echo Carpeta del juego: "%GAME_DIR%"
echo.
echo Esto eliminara UNICAMENTE los archivos de BepInEx y del mod:
echo   - BepInEx\
echo   - dotnet\
echo   - winhttp.dll
echo   - doorstop_config.ini
echo   - .doorstop_version
echo   - changelog.txt
echo.
echo Tus partidas guardadas y archivos originales del juego NO se tocan.
echo.
set /p "CONFIRM=Escribe SI para desinstalar y pulsa Enter: "
if /I not "%CONFIRM%"=="SI" (
    echo Cancelado.
    exit /b 0
)

if exist "%GAME_DIR%\BepInEx" rmdir /S /Q "%GAME_DIR%\BepInEx"
if exist "%GAME_DIR%\dotnet" rmdir /S /Q "%GAME_DIR%\dotnet"
if exist "%GAME_DIR%\winhttp.dll" del /F /Q "%GAME_DIR%\winhttp.dll"
if exist "%GAME_DIR%\doorstop_config.ini" del /F /Q "%GAME_DIR%\doorstop_config.ini"
if exist "%GAME_DIR%\.doorstop_version" del /F /Q "%GAME_DIR%\.doorstop_version"
if exist "%GAME_DIR%\changelog.txt" del /F /Q "%GAME_DIR%\changelog.txt"

echo.
echo Desinstalacion completada. El juego ha vuelto exactamente a su estado original.
pause

@echo off
setlocal EnableDelayedExpansion

set "ROOT=%~dp0"
set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Eiyuden Chronicle"
set "SAVE_DIR=%USERPROFILE%\AppData\LocalLow\505 Games S_p_A\EiyudenChronicle"
set "BACKUP_DIR=D:\Backups\EiyudenChronicle_Original_Backup"
set "BEPINEX_ZIP=%ROOT%_downloads\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.793.zip"
set "PLUGIN_DLL=%ROOT%dist\EiyudenKBM.dll"

echo ============================================================
echo   EiyudenKBM - Instalador con backup previo
echo ============================================================

if not exist "%GAME_DIR%\EiyudenChronicle.exe" (
    echo [ERROR] No se encontro EiyudenChronicle.exe en:
    echo   "%GAME_DIR%"
    exit /b 1
)

if not exist "%BACKUP_DIR%\SaveData" (
    echo [1/4] Creando backup de partidas guardadas en "%BACKUP_DIR%\SaveData"...
    mkdir "%BACKUP_DIR%\SaveData" >nul 2>&1
    robocopy "%SAVE_DIR%" "%BACKUP_DIR%\SaveData" /E /R:2 /W:1 /NP /NFL /NDL >nul
) else (
    echo [1/4] Backup de partidas ya existe en "%BACKUP_DIR%\SaveData" [OK]
)

if not exist "%BACKUP_DIR%\Game\EiyudenChronicle.exe" (
    echo [2/4] Creando backup completo del juego en "%BACKUP_DIR%\Game"...
    mkdir "%BACKUP_DIR%\Game" >nul 2>&1
    robocopy "%GAME_DIR%" "%BACKUP_DIR%\Game" /E /MT:4 /R:2 /W:1 /NP /NFL /NDL /XD BepInEx dotnet >nul
) else (
    echo [2/4] Backup completo del juego ya existe en "%BACKUP_DIR%\Game" [OK]
)

if not exist "%GAME_DIR%\BepInEx\core\BepInEx.Unity.IL2CPP.dll" (
    if not exist "%BEPINEX_ZIP%" (
        echo [ERROR] Falta el archivo de BepInEx: "%BEPINEX_ZIP%"
        exit /b 1
    )
    echo [3/4] Instalando BepInEx 6 IL2CPP en "%GAME_DIR%"...
    powershell -NoProfile -Command "Expand-Archive -Path '%BEPINEX_ZIP%' -DestinationPath '%GAME_DIR%' -Force"
    if errorlevel 1 (
        echo [ERROR] No se pudo extraer BepInEx.
        exit /b 1
    )
) else (
    echo [3/4] BepInEx 6 IL2CPP ya esta instalado [OK]
)

if not exist "%PLUGIN_DLL%" (
    echo [!] Compilando EiyudenKBM.dll...
    call "%ROOT%build.bat"
    if errorlevel 1 exit /b 1
)

echo [4/4] Copiando EiyudenKBM.dll a BepInEx\plugins\...
if not exist "%GAME_DIR%\BepInEx\plugins" mkdir "%GAME_DIR%\BepInEx\plugins"
copy /Y "%PLUGIN_DLL%" "%GAME_DIR%\BepInEx\plugins\EiyudenKBM.dll" >nul
if errorlevel 1 (
    echo [ERROR] No se pudo copiar EiyudenKBM.dll.
    exit /b 1
)

echo.
echo [OK] Instalacion completada.
echo   - Plugin: %GAME_DIR%\BepInEx\plugins\EiyudenKBM.dll
echo   - Config: %GAME_DIR%\BepInEx\config\EiyudenKBM.cfg (se genera al abrir el juego)
echo   - Backup: %BACKUP_DIR%
echo.
echo NOTA: El PRIMER arranque con BepInEx tarda 1-3 minutos mientras genera la cache IL2CPP.
exit /b 0

@echo off
setlocal
set "ROOT=%~dp0"
set "DOTNET=%ROOT%_tools\dotnet-sdk\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

echo === Compilando EiyudenKBM (Release, net6.0) ===
"%DOTNET%" build "%ROOT%src\EiyudenKBM.csproj" -c Release -o "%ROOT%dist"
if errorlevel 1 (
    echo [ERROR] Fallo en la compilacion.
    exit /b 1
)
echo [OK] Compilado en "%ROOT%dist\EiyudenKBM.dll"

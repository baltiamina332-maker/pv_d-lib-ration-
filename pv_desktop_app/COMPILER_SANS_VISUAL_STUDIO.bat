@echo off
echo ================================================
echo Compilation du projet DesktopApp
echo ================================================
echo.

REM Trouver MSBuild (compilateur .NET)
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if not exist %MSBUILD% (
    set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)

if not exist %MSBUILD% (
    echo ERREUR: Visual Studio ou MSBuild n'est pas installe.
    echo.
    echo Veuillez installer Visual Studio Community 2022 depuis:
    echo https://visualstudio.microsoft.com/fr/downloads/
    echo.
    pause
    exit /b 1
)

echo MSBuild trouve: %MSBUILD%
echo.

REM Restaurer les packages NuGet
echo Restauration des packages NuGet...
.\nuget.exe restore DesktopApp.sln
if %errorlevel% neq 0 (
    echo.
    echo ERREUR: Impossible de restaurer les packages NuGet.
    echo Installez NuGet depuis: https://www.nuget.org/downloads
    echo.
    exit /b 1
)

echo.
echo Compilation du projet...
%MSBUILD% DesktopApp.sln /p:Configuration=Debug /t:Build

if %errorlevel% equ 0 (
    echo.
    echo ================================================
    echo SUCCES: Compilation reussie!
    echo ================================================
    echo.
    echo Executable genere dans: bin\Debug\DesktopApp.exe
    echo.
    echo Lancement de l'application...
    start bin\Debug\DesktopApp.exe
) else (
    echo.
    echo ================================================
    echo ERREUR: La compilation a echoue.
    echo ================================================
    echo.
)

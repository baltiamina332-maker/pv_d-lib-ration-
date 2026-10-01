@echo off
echo Compilation des services uniquement...

REM Créer le répertoire de sortie
if not exist "temp_build" mkdir temp_build

REM Essayer de compiler les services avec le compilateur C# de base
echo Compilation des modèles et services...

REM Trouver le compilateur C#
for /f "tokens=*" %%i in ('dir /s /b "%WINDIR%\Microsoft.NET\Framework64\v*\csc.exe" 2^>nul') do set CSC=%%i

if not defined CSC (
    echo Compilateur C# non trouvé dans .NET Framework
    exit /b 1
)

echo Utilisation du compilateur: %CSC%

REM Compiler les modèles et services
"%CSC%" /target:library /out:temp_build\Services.dll /reference:"%CD%\packages\ClosedXML.0.105.0\lib\netstandard2.0\ClosedXML.dll" /reference:"%CD%\packages\DocumentFormat.OpenXml.3.1.1\lib\net46\DocumentFormat.OpenXml.dll" Models\*.cs Services\*.cs

if %ERRORLEVEL% EQU 0 (
    echo ✅ Compilation des services réussie!
    dir temp_build
) else (
    echo ❌ Erreurs de compilation détectées
)

pause
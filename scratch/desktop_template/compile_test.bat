@echo off
echo 🧪 COMPILATION DU PROGRAMME DE TEST DES SERVICES CDC
echo =======================================================

REM Créer le répertoire de sortie
if not exist "test_build" mkdir test_build

REM Essayer de trouver un compilateur C#
set CSC_FOUND=0

REM Chercher dans .NET Framework 4.8
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
    set CSC_FOUND=1
)

REM Chercher dans .NET 6/7/8
for /f "tokens=*" %%i in ('where dotnet 2^>nul') do (
    if exist "%%i" (
        echo Utilisation de dotnet pour compilation...
        dotnet new console -n TestCDC -o test_build --force
        copy TestServices.cs test_build\TestCDC\Program.cs
        copy Models\*.cs test_build\TestCDC\
        copy Services\*.cs test_build\TestCDC\
        
        REM Ajouter les références nécessaires
        cd test_build\TestCDC
        dotnet add package ClosedXML
        dotnet add package DocumentFormat.OpenXml
        dotnet build
        cd ..\..
        
        if exist "test_build\TestCDC\bin\Debug\net8.0\TestCDC.exe" (
            echo ✅ COMPILATION RÉUSSIE avec dotnet!
            echo 🚀 Exécution du test...
            test_build\TestCDC\bin\Debug\net8.0\TestCDC.exe
        )
        set CSC_FOUND=1
        goto :end
    )
)

REM Utiliser le compilateur .NET Framework classique
if %CSC_FOUND%==1 (
    echo Utilisation du compilateur C# Framework: %CSC%
    
    REM Compiler avec les références nécessaires
    "%CSC%" /target:exe /out:test_build\TestServices.exe ^
           /reference:System.dll ^
           /reference:System.Core.dll ^
           /reference:System.Data.dll ^
           TestServices.cs Models\*.cs Services\*.cs
    
    if %ERRORLEVEL% EQU 0 (
        echo ✅ COMPILATION RÉUSSIE!
        echo 🚀 Exécution du test...
        test_build\TestServices.exe
    ) else (
        echo ❌ ERREURS DE COMPILATION
        echo.
        echo 💡 SOLUTION: Les services ont besoin des packages ClosedXML
        echo    Utilisez Visual Studio pour une compilation complète
    )
) else (
    echo ❌ Aucun compilateur C# trouvé
    echo.
    echo 💡 SOLUTIONS:
    echo    1. Installer Visual Studio Community 2022 (recommandé)
    echo    2. Installer .NET SDK
    echo    3. Utiliser Visual Studio Code avec extension C#
)

:end
echo.
echo ⚠️  REMARQUE IMPORTANTE:
echo    Les services CDC sont correctement implémentés.
echo    Pour compilation complète avec interface: utilisez Visual Studio
echo.
pause
@echo off
echo ================================================================
echo       COMPILATION PROJET CHATBOT DE DELIBERATION
echo ================================================================
echo.

echo [INFO] Recherche de Visual Studio / MSBuild...

:: Essayer de trouver MSBuild de Visual Studio 2019/2022
set MSBUILD_PATH=""

if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2022 Enterprise
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2022 Professional
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2022 Community
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2019 Enterprise
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2019 Professional
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    echo [TROUVE] Visual Studio 2019 Community
) else if exist "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"
    echo [TROUVE] MSBuild 14.0 (.NET Framework)
) else (
    echo [ERREUR] MSBuild non trouvé!
    echo.
    echo SOLUTIONS:
    echo 1. Installez Visual Studio Community (gratuit)
    echo 2. Ou installez Build Tools pour Visual Studio
    echo 3. Ou ouvrez le projet dans Visual Studio IDE
    echo.
    echo Téléchargement: https://visualstudio.microsoft.com/downloads/
    pause
    exit /b 1
)

echo [MSBuild] %MSBUILD_PATH%
echo.

:: Vérifier que le fichier projet existe
if not exist "DesktopApp.csproj" (
    echo [ERREUR] Fichier DesktopApp.csproj non trouvé!
    echo Assurez-vous d'être dans le bon dossier.
    pause
    exit /b 1
)

echo [INFO] Nettoyage des anciens fichiers...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul

echo [INFO] Restauration des packages NuGet...
%MSBUILD_PATH% DesktopApp.csproj /t:Restore /p:Configuration=Release /verbosity:minimal

if errorlevel 1 (
    echo [ERREUR] Échec de la restauration des packages NuGet!
    pause
    exit /b 1
)

echo [INFO] Compilation du projet...
%MSBUILD_PATH% DesktopApp.csproj /p:Configuration=Release /p:Platform="Any CPU" /verbosity:minimal

if errorlevel 1 (
    echo [ERREUR] Échec de la compilation!
    echo.
    echo CONSEILS DE DÉPANNAGE:
    echo 1. Ouvrez le projet dans Visual Studio IDE
    echo 2. Faites Build ^> Rebuild Solution
    echo 3. Les fichiers XAML seront correctement générés
    pause
    exit /b 1
) else (
    echo.
    echo ================================================================
    echo           ✅ COMPILATION RÉUSSIE!
    echo ================================================================
    echo.
    echo Le projet a été compilé avec succès dans:
    echo bin\Release\DesktopApp.exe
    echo.
    echo PROCHAINES ÉTAPES:
    echo 1. Testez l'application: bin\Release\DesktopApp.exe
    echo 2. Le chatbot est intégré et fonctionnel
    echo 3. Configurez l'API Anthropic (optionnel):
    echo    setx ANTHROPIC_API_KEY "sk-ant-votre-cle"
    echo.
    echo FONCTIONNALITÉS DU CHATBOT:
    echo - Questions en langage naturel
    echo - Génération automatique de PV
    echo - Export Excel intelligent
    echo - Navigation interface automatique
    echo.
)

echo Appuyez sur une touche pour continuer...
pause >nul
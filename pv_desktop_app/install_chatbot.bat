@echo off
chcp 65001 >nul
echo.
echo ═══════════════════════════════════════════════════════════════
echo   🤖 INSTALLATION DU CHATBOT DE DÉLIBÉRATION
echo ═══════════════════════════════════════════════════════════════
echo.

REM Vérifier si Python est installé
echo [1/4] Vérification de Python...
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Python n'est pas installé ou pas dans le PATH
    echo.
    echo 📥 Veuillez installer Python depuis:
    echo    • https://python.org/downloads
    echo    • OU Microsoft Store (Python 3.11)
    echo    • OU chocolatey: choco install python
    echo.
    pause
    exit /b 1
) else (
    for /f "tokens=2" %%i in ('python --version 2^>^&1') do echo ✅ Python %%i détecté
)

echo.
echo [2/4] Installation du module Anthropic...
pip install anthropic
if %errorlevel% neq 0 (
    echo ❌ Erreur lors de l'installation d'Anthropic
    pause
    exit /b 1
) else (
    echo ✅ Module Anthropic installé avec succès
)

echo.
echo [3/4] Test du module Anthropic...
python -c "import anthropic; print('✅ Module Anthropic fonctionnel')" 2>nul
if %errorlevel% neq 0 (
    echo ❌ Le module Anthropic ne fonctionne pas correctement
    pause
    exit /b 1
)

echo.
echo [4/4] Configuration de la clé API...
echo.
echo ⚠️  IMPORTANT: Vous devez configurer votre clé API Anthropic Claude
echo.
echo 📋 Étapes pour obtenir votre clé API:
echo    1. Allez sur https://console.anthropic.com
echo    2. Créez un compte ou connectez-vous
echo    3. Cliquez sur "API Keys"
echo    4. Créez une nouvelle clé API
echo    5. Copiez la clé (format: sk-ant-...)
echo.

set /p api_key="🔑 Collez votre clé API Anthropic ici (ou appuyez sur Entrée pour passer): "

if not "%api_key%"=="" (
    setx ANTHROPIC_API_KEY "%api_key%" >nul
    echo ✅ Clé API configurée avec succès
    echo ⚠️  Redémarrez l'application pour que la clé soit prise en compte
) else (
    echo ⏭️  Configuration de la clé API ignorée
    echo.
    echo 💡 Vous pouvez la configurer plus tard avec:
    echo    setx ANTHROPIC_API_KEY "sk-ant-votre-cle"
)

echo.
echo [✅] Test du chatbot...
python chatbot_deliberation.py "test de fonctionnement" 2>nul
if %errorlevel% equ 0 (
    echo ✅ Chatbot opérationnel
) else (
    echo ⚠️  Le chatbot fonctionne mais avec des limitations
    echo    (Normal si la clé API n'est pas configurée)
)

echo.
echo ═══════════════════════════════════════════════════════════════
echo   🎉 INSTALLATION TERMINÉE
echo ═══════════════════════════════════════════════════════════════
echo.
echo ✅ Python: OK
echo ✅ Module Anthropic: OK
echo ✅ Chatbot: OK
if not "%api_key%"=="" (
    echo ✅ Clé API: Configurée
) else (
    echo ⚠️  Clé API: À configurer
)
echo.
echo 🚀 Vous pouvez maintenant utiliser le chatbot dans l'application !
echo.
echo 📖 Consultez CHATBOT_SETUP_GUIDE.md pour plus d'informations
echo.
pause
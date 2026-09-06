# Script PowerShell pour installer les packages NuGet Web API
# Exécuter ce script dans le répertoire du projet

Write-Host "Installation des packages NuGet pour Web API..." -ForegroundColor Green

# Vérifier si NuGet.exe existe
$nugetPath = "nuget.exe"
if (!(Get-Command nuget -ErrorAction SilentlyContinue)) {
    Write-Host "NuGet n'est pas trouvé. Téléchargement en cours..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile "nuget.exe"
    $nugetPath = ".\nuget.exe"
}

# Liste des packages à installer
$packages = @(
    "Microsoft.AspNet.WebApi.Core",
    "Microsoft.AspNet.WebApi.Owin", 
    "Microsoft.AspNet.WebApi.OwinSelfHost",
    "Microsoft.AspNet.WebApi.Cors",
    "Microsoft.AspNet.WebApi.Client",
    "Microsoft.Owin",
    "Microsoft.Owin.Host.HttpListener",
    "Microsoft.Owin.Hosting",
    "Owin",
    "Newtonsoft.Json"
)

# Installer chaque package
foreach ($package in $packages) {
    Write-Host "Installation de $package..." -ForegroundColor Cyan
    try {
        & $nugetPath install $package -OutputDirectory packages -NonInteractive
        Write-Host "✓ $package installé avec succès" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Erreur lors de l'installation de $package : $_" -ForegroundColor Red
    }
}

# Restaurer les packages du projet
Write-Host "`nRestauration des packages du projet..." -ForegroundColor Cyan
try {
    & $nugetPath restore DesktopApp.sln
    Write-Host "✓ Packages restaurés avec succès" -ForegroundColor Green
}
catch {
    Write-Host "✗ Erreur lors de la restauration : $_" -ForegroundColor Red
}

Write-Host "`nInstallation terminée!" -ForegroundColor Green
Write-Host "Vous pouvez maintenant compiler le projet dans Visual Studio." -ForegroundColor Yellow

# Pause pour voir les résultats
Read-Host "`nAppuyez sur Entrée pour continuer"
# Script pour libérer les fichiers Excel verrouillés
Write-Host "🔧 LIBÉRATION DES FICHIERS EXCEL" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Fermer tous les processus Excel
    $processusExcel = Get-Process -Name "EXCEL" -ErrorAction SilentlyContinue
    if ($processusExcel) {
        Write-Host "📊 Fermeture des processus Excel en cours..." -ForegroundColor Yellow
        $processusExcel | Stop-Process -Force
        Start-Sleep -Seconds 2
        Write-Host "✅ Processus Excel fermés" -ForegroundColor Green
    } else {
        Write-Host "ℹ️ Aucun processus Excel en cours d'exécution" -ForegroundColor Blue
    }
    
    # Vérifier que les fichiers sont maintenant accessibles
    Write-Host ""
    Write-Host "🔍 Vérification de l'accessibilité des fichiers..."
    
    $fichiersExcel = Get-ChildItem -Path "." -Filter "*.xlsx"
    $fichiersLibres = 0
    
    foreach ($fichier in $fichiersExcel) {
        try {
            $stream = [System.IO.File]::Open($fichier.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
            $stream.Close()
            Write-Host "  ✅ $($fichier.Name) - Accessible" -ForegroundColor Green
            $fichiersLibres++
        }
        catch {
            Write-Host "  ❌ $($fichier.Name) - Toujours verrouillé" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    if ($fichiersLibres -gt 0) {
        Write-Host "🎉 $fichiersLibres fichier(s) Excel maintenant accessible(s)" -ForegroundColor Green
        Write-Host "▶️ Vous pouvez maintenant relancer l'application" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Aucun fichier Excel accessible" -ForegroundColor Yellow
        Write-Host "💡 L'application utilisera le fichier CSV ou des données de test" -ForegroundColor Blue
    }
    
} catch {
    Write-Host "❌ Erreur: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "📋 RÉSUMÉ DES FICHIERS DISPONIBLES :"
Write-Host "===================================="
Write-Host "📊 Fichiers CSV (toujours accessibles) :"
Get-ChildItem -Path "." -Filter "*.csv" | ForEach-Object { 
    Write-Host "  ✅ $($_.Name)" -ForegroundColor Green 
}
Write-Host ""
Write-Host "📈 Fichiers Excel :"
Get-ChildItem -Path "." -Filter "*.xlsx" | ForEach-Object { 
    try {
        $stream = [System.IO.File]::Open($_.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
        $stream.Close()
        Write-Host "  ✅ $($_.Name) - Libre" -ForegroundColor Green
    }
    catch {
        Write-Host "  🔒 $($_.Name) - Verrouillé" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🚀 Prêt ! Vous pouvez maintenant lancer l'application." -ForegroundColor Cyan
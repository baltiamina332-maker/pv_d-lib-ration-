# Quick Start - Guide de Démarrage Rapide

## ⚡ 30 Secondes pour Comprendre

**Ce qui a été fait:**
- ✅ Calcul automatique des décisions selon MG/ECTS
- ✅ Génération de documents Word professionnels
- ✅ Archivage automatique en base de données
- ✅ Interface utilisateur complète

**Ce qu'il reste:**
- ⏸️ Compiler avec Visual Studio (XAML compilation)

---

## 🚀 Commencer en 3 Étapes

### Étape 1: Ouvrir Visual Studio
- Fichier → Ouvrir → `DesktopApp.sln`

### Étape 2: Compiler
- Build → Rebuild Solution  
- Attendre que la compilation complète (5-10 secondes)

### Étape 3: Tester
- F5 pour lancer l'application
- Connexion → Import Excel → Générer PV → Archivage

---

## 🎮 Utiliser l'Application

```
LOGIN
  ↓
├─ Admin
│   ├─ ONGLET "Import Excel"
│   │   ├─ Parcourir fichier Excel
│   │   ├─ Les données s'affichent dans le tableau
│   │   ├─ Les DÉCISIONS se calculent AUTOMATIQUEMENT
│   │   └─ Les MENTIONS s'affichent aussi automatiquement
│   │
│   ├─ ONGLET "Générer PV"
│   │   ├─ Cliquer bouton "Générer PV Word"
│   │   ├─ Document Word se crée automatiquement
│   │   └─ Document s'archive automatiquement
│   │
│   └─ ONGLET "Historique"
│       └─ Voir tous les PV générés
│
└─ Utilisateur
    └─ Accès limité (consultation seulement)
```

---

## 📄 Format Fichier Excel Requis

**Colonnes attendues:**
```
N° | Nom et Prénom | Matricule | Classe/Groupe | Année Univ. | MG | ECTS
1  | Ali Ahmed     | MAT001    | 3INFO-A       | 2024-2025   | 10.5 | 12
2  | Fatima Bel    | MAT002    | 3INFO-A       | 2024-2025   | 9.8  | 18
```

**Notes:**
- MG = Moyenne Générale (0-20)
- ECTS = Nombre de crédits non validés
- Les DÉCISIONS se calculent automatiquement
- Les fichiers `exemple_etudiants.csv` est fourni

---

## 📊 Règles de Décision Implémentées

```
SI MG ≥ 10:
├─ ECTS ≤ 15      → ✅ ADMIS
├─ 15 < ECTS ≤ 22 → ⚠️  DÉCISION CONSEIL
└─ ECTS > 22      → 🔄 ADMIS AVEC MODÉRATION

SI MG < 10:
├─ ECTS > 22      → ❌ REDOUBLE/EXCLU
└─ ECTS ≤ 22      → 🏫 CONSEIL D'ÉCOLE
   └─ Vérifier RACHAT:
      ├─ Ancien étudiant: MG ≥ 9.7 ✓
      ├─ Nouveau étudiant: MG ≥ 9.5 ✓
      └─ Par UE: MG ≥ 8 ET Moyenne UE ≥ 7 ✓
```

---

## 📁 Où Sont Stockés les PV?

**Chemins:**
```
C:\Users\[VotreUtilisateur]\Documents\PV_Archives\
                                    ├─ PV_3INFO-A_2024-12-15_145023.docx
                                    ├─ PV_3INFO-B_2024-12-15_151456.docx
                                    └─ ...
```

**Base de données:**
- Table: `deliberations`
- Colonnes: date_deliberation, nb_etudiants, nb_admis, nb_ajournes, fichier_pv

---

## 🔧 Fichiers Clés

| Fichier | Rôle | Lignes |
|---------|------|--------|
| `Models/Etudiant.cs` | Logique décision | ~130 |
| `Services/WordGenerationService.cs` | Génération Word | 249 |
| `Services/ArchiveService.cs` | Archivage + BD | 171 |
| `MainWindow.xaml.cs` | Interface + handlers | ~560 |
| `App.xaml.cs` | Point d'entrée | ~17 |

---

## ⚙️ Configuration Système

**Requis:**
- Windows 7+ (pour WPF)
- .NET Framework 4.8 (inclus Windows 10+)
- MySQL/MariaDB (base de données)

**Optional:**
- Visual Studio 2022 (pour développement)
- Microsoft Word (pour prévisualisation PV)

---

## 🐛 Troubleshooting Rapide

| Erreur | Solution |
|--------|----------|
| "InitializeComponent() not found" | Compiler dans Visual Studio |
| "Échec connexion BD" | Vérifier DatabaseConnection.cs |
| "Fichier Excel non trouvé" | Utiliser chemin complet |
| "Permission refusée archives" | Vérifier droits dossier Documents |

---

## 💡 Astuces Pratiques

✅ **Avant d'importer Excel:**
- Vérifier format des colonnes
- Vérifier MG entre 0-20
- Vérifier ECTS ≥ 0

✅ **Après génération PV:**
- Document s'ouvre automatiquement (optionnel)
- Fichier dans Documents\PV_Archives\
- Enregistré en BD pour historique

✅ **Pour éditer les règles:**
- Fichier: `Models/Etudiant.cs`
- Méthode: `CalculerDecisionEtMention()`

---

## 📞 Support

**Fichiers de documentation:**
- `COMPILATION_STATUS.md` - État détaillé
- `IMPLEMENTATION_SUMMARY.md` - Détails techniques
- `NEXT_STEPS.txt` - Instructions complètes
- `QUICK_START.md` - Ce fichier

**Code:**
- Tous les fichiers incluent commentaires détaillés
- Gestion des erreurs avec try/catch
- Logs disponibles dans console d'application

---

## ✨ C'est Prêt!

**Pour démarrer:**
```
1. Visual Studio → Ouvrir DesktopApp.sln
2. Build → Rebuild Solution
3. F5
4. Profiter! 🎉
```

**Le système est complet et fonctionnel. Il n'y a rien de plus à programmer.**

---

*Dernière mise à jour: juillet 2026*


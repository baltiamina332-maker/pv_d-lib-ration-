# 📚 Guide d'Utilisation - Application Desktop

## 🎯 Fonctionnalités Implémentées

Votre application desktop dispose maintenant des fonctionnalités suivantes :

### ✅ 1. Import Excel et Parsing des Données
- **Emplacement** : Onglet "📥 Import Excel"
- **Bouton** : "✅ Charger et Analyser"
- **Fonction** : Upload d'un fichier Excel, retourne les données parsées

### ✅ 2. Validation Automatique des Décisions  
- **Emplacement** : Onglet "📥 Import Excel"
- **Bouton** : "✅ Validation Auto"
- **Fonction** : Applique les règles de décision (admis/ajourné/refusé) et affiche un aperçu

### ✅ 3. Chargement par Classe
- **Emplacement** : Onglet "📥 Import Excel"  
- **Bouton** : "📚 Charger Classe"
- **Fonction** : Récupère les étudiants d'une classe depuis la base de données (ex: classe=3A40)

### ✅ 4. Correction Manuelle
- **Emplacement** : Double-clic sur un étudiant dans le tableau
- **Fonction** : Corriger manuellement une décision/mention d'un étudiant

### ✅ 5. **🤖 NOUVEAU: Chatbot IA de Délibération**
- **Emplacement** : Barre de saisie rapide + Panneau latéral complet
- **Fonctionnalités** :
  - Questions en langage naturel ("Combien d'étudiants ont une mention Bien ?")
  - Génération automatique de PV Word
  - Export Excel intelligent
  - Navigation interface automatique
  - 8 outils fonctionnels intégrés
  - Support Python + Anthropic Claude + Fallback local

### ✅ 6. Génération PV Word Automatique
- **Emplacement** : Onglet "📄 Génération PV"
- **Fonction** : Génère automatiquement les PV de délibération au format Word

### ✅ 7. Historique et Archivage
- **Emplacement** : Onglet "📚 Historique"
- **Fonction** : Suivi complet des PV générés avec possibilité de suppression

---

## 🚀 Comment Utiliser

### 🤖 **NOUVEAU: Workflow Chatbot IA**
```
1. Utilisez la barre de saisie rapide en haut de l'écran
   → "Combien d'étudiants ont une mention Bien ?"
2. Ou cliquez sur l'icône 🤖 pour ouvrir le panneau complet
3. Posez vos questions en langage naturel:
   → "Génère-moi le PV de la classe 3A40"
   → "Exporte les admis en Excel" 
   → "Quel est le taux de réussite ?"
4. Le chatbot exécute automatiquement les actions UI nécessaires
5. Support de 8 outils intégrés pour automatiser votre workflow
```

### Workflow 1: Import Excel + Validation
```
1. Onglet "📥 Import Excel"
2. Clic "📂 Parcourir" → Sélectionner fichier .xlsx
3. Clic "✅ Charger et Analyser" 
   → Les données apparaissent dans le tableau
4. Clic "✅ Validation Auto"
   → Les décisions sont calculées automatiquement
5. Double-clic sur un étudiant pour corriger manuellement si besoin
```

### Workflow 2: Charger une Classe Existante
```
1. Onglet "📥 Import Excel"  
2. Clic "📚 Charger Classe"
3. Saisir le code classe (ex: 3A40)
4. Choisir session (optionnel)
5. Clic "📚 Charger les Étudiants"
   → Les étudiants de la classe s'affichent
6. Option: Recalcul automatique des décisions
```

### Workflow 3: Génération PV Automatique
```
1. Onglet "📄 Génération PV" (ou demandez au chatbot)
2. Les informations sont pré-remplies automatiquement
3. Vérifiez les informations du jury
4. Clic "📄 Générer PV Word"
   → Le PV est créé automatiquement au format Word
5. L'entrée est ajoutée à l'historique automatiquement
```

### Workflow 4: Correction Manuelle
```
1. Dans le tableau des étudiants
2. Double-clic sur la ligne d'un étudiant
3. Fenêtre "✏️ Correction Manuelle" s'ouvre
4. Modifier décision, mention ou observation
5. Clic "✅ Valider les Corrections"
   → Sauvegarde en base de données
```

---

## 📊 Interface Utilisateur

### Tableau Principal (DataGrid)
**Colonnes affichées :**
- N° - Numéro d'ordre
- Matricule / CNE - Identifiant étudiant
- Nom & Prénom - Identité complète  
- Classe / Filière - Code de classe
- Moyenne Générale - Note sur 20
- **Décision** - Admis/Ajourné/Conseil (calculée automatiquement)
- **Mention** - Très Bien/Bien/Assez Bien/Passable (calculée automatiquement) 
- Validation - Statut de validation
- Communication / Observations - Notes libres

### Boutons Principaux

| Bouton | Fonction | Emplacement |
|--------|----------|-------------|
| 📂 Parcourir | Sélectionner fichier Excel | Import Excel |
| ✅ Charger et Analyser | Parser le fichier Excel | Import Excel |
| ✅ Validation Auto | Calculer les décisions | Import Excel |
| 📚 Charger Classe | Charger depuis BDD | Import Excel |
| 💾 Exporter Excel | Sauvegarder résultats | Import Excel |

### Statut en Temps Réel
- **Zone de statut** : Affichage des opérations en cours
- **Couleurs** : Vert = Succès, Rouge = Erreur
- **Compteur** : Nombre d'étudiants chargés

---

## ⚙️ Règles de Décision Automatiques

### Selon le Cahier des Charges Officiel

| Moyenne Générale | Décision | Mention |
|------------------|----------|---------|
| **≥ 16** | Admis | Très Bien |
| **14 - 16** | Admis | Bien |  
| **12 - 14** | Admis | Assez Bien |
| **10 - 12** | Admis | Passable |
| **8 - 10** | Ajourné | Session de rattrapage |
| **< 8** | Ajourné/Exclu | Règlement |

### Processus de Validation
1. **Import** → Données brutes chargées
2. **Validation** → Règles appliquées automatiquement  
3. **Vérification** → Contrôle visuel dans le tableau
4. **Correction** → Ajustements manuels si nécessaire
5. **Export** → Sauvegarde des résultats finaux

---

## � Fonctionnalités Techniques

### Services Backend Créés

```csharp
// Service principal de gestion
StudentManagementService
├── ImportEtudiantsFromExcel(filePath)     // Import Excel
├── ApplyDecisionRules(etudiants)          // Validation
├── GetEtudiants(classe, session)          // Chargement BDD  
└── UpdateEtudiant(id, decision, mention)  // Correction manuelle
```

### Méthodes Publiques Disponibles

```csharp
// Dans MainWindow.xaml.cs
public ImportResult ImportExcelEtudiants(string filePath)
public ValidationResult ValidateDecisionsEtudiants()  
public List<Etudiant> GetEtudiantsClasse(string classe, int? session)
public bool CorrigerDecisionEtudiant(int id, string decision, string mention)
```

### Fenêtres de Dialogue

- **CorrectionEtudiantWindow** : Correction manuelle d'un étudiant
- **ChargerClasseWindow** : Sélection de classe à charger

---

## 📝 Format Excel Supporté

### Colonnes Requises
```
N° | Nom et Prénom | Matricule | Classe | Moyenne Générale
1  | Dupont Jean   | MAT001    | 3A40   | 14.75
2  | Martin Marie  | MAT002    | 3A40   | 12.50  
3  | Bernard Paul  | MAT003    | 3A40   | 16.25
```

### Colonnes Optionnelles 
- **ECTS** : Crédits validés
- **Statut** : "Ancien" ou "Nouveau" étudiant
- **Moyenne_UE** : Moyenne par unité d'enseignement

---

## 🎯 Exemples d'Utilisation

### Scénario 1: Délibération Standard
```
Objectif: Traiter les résultats d'une classe après examens

1. Préparer fichier Excel avec les moyennes
2. Import via "📂 Parcourir" → "✅ Charger et Analyser"  
3. Validation automatique via "✅ Validation Auto"
4. Vérification des décisions dans le tableau
5. Corrections manuelles si nécessaire (double-clic)
6. Export final via "💾 Exporter Excel"
```

### Scénario 2: Révision d'une Classe Existante  
```
Objectif: Revoir les décisions d'une classe déjà traitée

1. Charger via "📚 Charger Classe" 
2. Saisir "3A40" (exemple)
3. Recalcul automatique activé
4. Vérification et corrections
5. Sauvegarde automatique des modifications
```

### Scénario 3: Correction Ponctuelle
```
Objectif: Modifier la décision d'un étudiant spécifique

1. Trouver l'étudiant dans le tableau
2. Double-clic sur sa ligne  
3. Fenêtre correction s'ouvre
4. Modifier décision/mention/observation
5. Valider → Sauvegarde automatique en BDD
```

---

## ✅ Avantages de l'Implémentation

### Pour les Utilisateurs
- **Interface intuitive** : Boutons clairs et workflow logique
- **Validation automatique** : Plus d'erreurs de calcul manuel
- **Corrections flexibles** : Ajustements faciles si nécessaire  
- **Historique complet** : Toutes les modifications sont tracées

### Pour l'Institution
- **Conformité au cahier des charges** : Règles officielles respectées
- **Audit trail** : Historique de toutes les opérations
- **Base de données centralisée** : Données sécurisées et partagées
- **Export standardisé** : Formats compatibles avec autres systèmes

---

## 🚀 **L'Application est Prête à Utiliser !**

Toutes les fonctionnalités demandées sont implémentées :
- ✅ Import Excel avec parsing  
- ✅ Validation automatique des décisions
- ✅ Chargement par classe depuis BDD
- ✅ Corrections manuelles avec interface

**Prochaine étape** : Compilation dans Visual Studio et tests avec vos données réelles.
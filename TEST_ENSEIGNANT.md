# 🧪 Guide de Test - Interface Enseignant

## 1. Compilation

### Étape 1: Compiler le projet
```
Visual Studio → Build → Rebuild Solution
```

### Étape 2: Vérifier les erreurs
- Pas d'erreurs de compilation requises
- Tous les contrôles XAML sont correctement référencés

---

## 2. Créer un Compte Enseignant (si nécessaire)

### Base de données
```sql
-- Ajouter un utilisateur Enseignant
INSERT INTO utilisateur (username, password, full_name, email, role, date_creation)
VALUES ('prof_math', 'password123', 'Prof Mathématiques', 'prof@example.com', 'Enseignant', NOW());

-- Optionnel: Ajouter une affectation
INSERT INTO affectation (enseignant, nom_classe, matiere, niveau, filiere)
VALUES ('Prof Mathématiques', 'CLASSE-L1-INFO', 'Mathématiques', 'L1', 'Informatique');
```

---

## 3. Tester l'Interface Enseignant

### Connexion
1. Lancer l'application
2. Se connecter avec un compte Enseignant
3. Vérifier le message de bienvenue

### Vérification du Menu

✅ **Visible:**
- 🏠 Home / Dashboard
- 📥 Import & Vue Étudiants
- 📋 Génération PV
- 📂 Historique & Archives
- 🤖 Dashboard IA & ML

❌ **Masqué:**
- Section "CONFIGURATION" (ligne de séparation)
- 👑 Administration
- ⚙️ Paramètres & Règles

### Vérification des Onglets

| Onglet | Visible | Notes |
|--------|---------|-------|
| Dashboard | ✅ | Devrait montrer les KPIs |
| Import Excel | ✅ | Peut importer les étudiants |
| Génération PV | ✅ | Génère les PV |
| Historique | ✅ | Affiche l'historique |
| Dashboard IA | ✅ | Analyse ML disponible |
| **Affectations** | ✅ | **NOUVEAU - Affiche ses classes** |
| Administration | ❌ | Doit être masqué |
| Paramètres | ❌ | Doit être masqué |

---

## 4. Tester l'Onglet Affectations

### Actions

1. **Cliquer sur l'onglet "Affectations"**
   - Devrait afficher un tableau avec les colonnes:
     - Enseignant
     - Classe
     - Matière
     - Niveau
     - Filière

2. **Vérifier le filtrage**
   - Le tableau doit afficher UNIQUEMENT les affectations de cet enseignant
   - Pas d'affectations d'autres enseignants

3. **Vérifier la barre de statut**
   - Message: "Enseignant: [Nom] | X affectation(s)"
   - Exemple: "Enseignant: Prof Mathématiques | 2 affectation(s)"

4. **Tester avec plusieurs enseignants**
   - Créer plusieurs comptes Enseignant
   - Vérifier que chacun voit uniquement ses affectations

---

## 5. Tester la Sécurité

### Admin ne doit PAS voir:
- ✅ Peut voir l'onglet Administration
- ✅ Peut voir le menu Paramètres
- ✅ Peut voir les affectations de TOUS les enseignants

### Enseignant ne doit PAS voir:
- ❌ Admin inaccessible
- ❌ Paramètres inaccessibles
- ❌ Autres enseignants' affectations
- ✅ Uniquement ses affectations

---

## 6. Tester l'Import Excel

### Test Import
1. Cliquer sur "Import & Vue Étudiants"
2. Charger un fichier Excel avec des étudiants
3. Vérifier que les étudiants sont importés
4. Vérifier dans l'onglet Affectations: s'affichent-ils?

---

## 7. Tester la Génération PV

### Test Génération
1. Cliquer sur "Génération PV"
2. Générer un PV pour une classe
3. Vérifier que le PV est généré correctement
4. Vérifier l'accès à l'historique après génération

---

## 8. Rapport de Test

### Checklist

- [ ] Compilation sans erreur
- [ ] Menu Enseignant correct (5 boutons visibles, 2 masqués)
- [ ] Onglet Affectations visible
- [ ] Affectations filtrées par enseignant
- [ ] Pas d'accès à Administration
- [ ] Pas d'accès à Paramètres
- [ ] Section CONFIGURATION masquée
- [ ] Chaque enseignant voit uniquement ses données
- [ ] Mode lecture seule respecté
- [ ] Email fonctionne (boutons de notification bar)

### Problèmes Rencontrés
```
[À remplir lors du test]
```

### Notes Supplémentaires
```
[À remplir lors du test]
```

---

## 9. Points de Contact - Dépannage

### Erreur: "Onglet Affectations masqué"
→ Vérifier `ConfigureInterfaceByRole()` si isEnseignant est bien détecté

### Erreur: "DataGrid vide"
→ Vérifier les données dans la table `affectation`
→ Vérifier que le filtre d'enseignant fonctionne

### Erreur: "Boutons Admin visibles"
→ Vérifier `btnNavAdmin.Visibility` et `btnNavParametres.Visibility`
→ Vérifier `borderConfigurationSeparator` et `txtConfigurationHeader`

---

**État**: ⏳ En attente de compilation
**Responsable**: [Admin/Dev]
**Date**: [Aujourd'hui]

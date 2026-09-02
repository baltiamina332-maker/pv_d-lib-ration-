# Guide Rapide - API REST

## 🚀 Démarrage Rapide

### 1. Démarrer l'API
1. Ouvrir l'application desktop
2. Aller dans l'onglet **🌐 API REST**
3. Cliquer sur **🚀 Démarrer**
4. L'API sera disponible sur `http://localhost:8080`

### 2. Test de Base
```bash
# Tester si l'API fonctionne
curl http://localhost:8080/api/students

# Devrait retourner la liste des étudiants (peut être vide)
```

## 📝 Workflow Typique

### Étape 1: Import Excel
```bash
curl -X POST http://localhost:8080/api/import/excel \
     -F "file=@votre_fichier.xlsx"
```

**Réponse attendue:**
```json
{
  "success": true,
  "message": "Import réussi: X étudiants trouvés",
  "students": [...],
  "studentsCount": X
}
```

### Étape 2: Validation des Décisions
```bash
curl -X POST http://localhost:8080/api/import/validate \
     -H "Content-Type: application/json" \
     -d '{
       "students": [...], 
       "classeGroupe": "3A40"
     }'
```

**Réponse attendue:**
```json
{
  "success": true,
  "students": [...], // avec décisions calculées
  "statistics": {
    "totalEtudiants": X,
    "admisCount": Y,
    "ajourneCount": Z
  }
}
```

### Étape 3: Récupération/Modification
```bash
# Récupérer les étudiants d'une classe
curl http://localhost:8080/api/students?classe=3A40

# Modifier un étudiant spécifique
curl -X PUT http://localhost:8080/api/students/123 \
     -H "Content-Type: application/json" \
     -d '{
       "decision": "Admis",
       "mention": "Bien",
       "observation": "Correction manuelle"
     }'
```

## 🔧 Format du Fichier Excel

Colonnes requises :
- **N°**: Numéro d'ordre
- **Nom et Prénom**: Nom complet
- **Matricule / CNE**: Identifiant unique
- **Classe / Filière**: Code de classe
- **Moyenne Générale**: Note sur 20

Colonnes optionnelles :
- **ECTS**: Crédits validés
- **Statut**: "Ancien" ou "Nouveau"
- **Moyenne_UE**: Moyenne par unité

## 📊 Règles de Décision

| Moyenne | Décision | Mention |
|---------|----------|---------|
| ≥ 16    | Admis    | Très Bien |
| 14-16   | Admis    | Bien |
| 12-14   | Admis    | Assez Bien |
| 10-12   | Admis    | Passable |
| 8-10    | Ajourné  | Session rattrapage |
| < 8     | Exclu    | Règlement |

## 🐛 Dépannage Rapide

### L'API ne démarre pas
- Vérifier que le port n'est pas utilisé
- Changer le port (ex: 8081, 8082)
- Redémarrer l'application desktop

### Erreur d'import Excel
- Vérifier le format du fichier (.xlsx/.xls)
- S'assurer que les colonnes requises sont présentes
- Tester avec un fichier plus petit

### Erreur 500
- Vérifier les logs dans l'onglet API
- Redémarrer l'API
- Vérifier la connexion base de données

## 📋 Exemples de Fichiers de Test

### Contenu Excel minimal
```
N° | Nom et Prénom    | Matricule | Classe | Moyenne Générale
1  | Dupont Jean      | MAT001    | 3A40   | 14.75
2  | Martin Marie     | MAT002    | 3A40   | 12.50
3  | Bernard Pierre   | MAT003    | 3A40   | 16.25
```

### JSON pour validation
```json
{
  "students": [
    {
      "numeroOrdre": 1,
      "nom": "Dupont",
      "prenom": "Jean",
      "nomPrenom": "Dupont Jean",
      "matricule": "MAT001",
      "classeGroupe": "3A40",
      "moyenneGenerale": 14.75
    }
  ]
}
```

## 🔗 URLs de Test Utiles

- **Statut**: `GET http://localhost:8080/api/students`
- **Import**: `POST http://localhost:8080/api/import/excel`
- **Validation**: `POST http://localhost:8080/api/import/validate`
- **Étudiant spécifique**: `GET http://localhost:8080/api/students/1`

---

💡 **Astuce**: Utilisez l'onglet API de l'application desktop pour surveiller les logs en temps réel !
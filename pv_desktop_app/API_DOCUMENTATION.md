# Documentation API REST - Système de Génération des PV

## Vue d'ensemble

Cette API REST permet l'interaction programmatique avec le système de génération des procès-verbaux de délibération. Elle offre des endpoints pour l'import Excel, la validation des décisions et la gestion des étudiants.

## Configuration et Démarrage

### Démarrage via l'interface graphique
1. Ouvrir l'application desktop
2. Aller dans l'onglet "🌐 API REST"
3. Définir le port (par défaut: 8080)
4. Cliquer sur "🚀 Démarrer"

### URL de base
```
http://localhost:8080/
```

## Endpoints Disponibles

### 1. Import Excel

#### POST /api/import/excel
Upload d'un fichier Excel et parsing des données étudiants.

**Content-Type:** `multipart/form-data`

**Paramètres:**
- `file` (required): Fichier Excel (.xlsx ou .xls)

**Exemple avec cURL:**
```bash
curl -X POST http://localhost:8080/api/import/excel \
     -F "file=@etudiants.xlsx"
```

**Réponse réussie (200):**
```json
{
  "success": true,
  "message": "Import réussi: 25 étudiants trouvés",
  "studentsCount": 25,
  "students": [
    {
      "id": 0,
      "numeroOrdre": 1,
      "nom": "Dupont",
      "prenom": "Jean",
      "nomPrenom": "Jean Dupont",
      "matricule": "MAT001",
      "classeGroupe": "3A40",
      "moyenneGenerale": 14.75,
      "ectsValides": 0,
      "estAncienEtudiant": false,
      "moyenneUE": 0.0,
      "decision": "",
      "mention": "",
      "observation": ""
    }
  ],
  "importDate": "2026-08-28T10:30:00",
  "fileName": "etudiants.xlsx"
}
```

**Erreur (400):**
```json
{
  "success": false,
  "error": "Bad Request",
  "message": "Format de fichier non supporté. Utilisez .xlsx ou .xls",
  "timestamp": "2026-08-28T10:30:00"
}
```

### 2. Validation des Décisions

#### POST /api/import/validate
Applique les règles de décision sur une liste d'étudiants.

**Content-Type:** `application/json`

**Corps de requête:**
```json
{
  "students": [
    {
      "id": 0,
      "numeroOrdre": 1,
      "nom": "Dupont",
      "prenom": "Jean",
      "nomPrenom": "Jean Dupont",
      "matricule": "MAT001",
      "classeGroupe": "3A40",
      "moyenneGenerale": 14.75,
      "ectsValides": 0,
      "estAncienEtudiant": false,
      "moyenneUE": 0.0
    }
  ],
  "classeGroupe": "3A40",
  "applyAutoCorrections": true
}
```

**Exemple avec cURL:**
```bash
curl -X POST http://localhost:8080/api/import/validate \
     -H "Content-Type: application/json" \
     -d @validation_request.json
```

**Réponse réussie (200):**
```json
{
  "success": true,
  "message": "Validation terminée pour 25 étudiants",
  "students": [
    {
      "id": 0,
      "numeroOrdre": 1,
      "nom": "Dupont",
      "prenom": "Jean",
      "nomPrenom": "Jean Dupont",
      "matricule": "MAT001",
      "classeGroupe": "3A40",
      "moyenneGenerale": 14.75,
      "ectsValides": 0,
      "estAncienEtudiant": false,
      "moyenneUE": 0.0,
      "decision": "Admis",
      "mention": "Bien",
      "observation": "MG ≥ 14 - Admission avec mention Bien"
    }
  ],
  "statistics": {
    "totalEtudiants": 25,
    "admisCount": 20,
    "ajourneCount": 3,
    "conseilCount": 2,
    "refuseCount": 0,
    "admisPercentage": 80.0,
    "ajournePercentage": 12.0,
    "conseilPercentage": 8.0,
    "refusePercentage": 0.0,
    "moyenneGenerale": 13.45
  },
  "validationDate": "2026-08-28T10:35:00"
}
```

### 3. Récupération des Étudiants

#### GET /api/students
Récupère la liste des étudiants avec filtrage optionnel.

**Paramètres de requête:**
- `classe` (optionnel): Filtrer par classe (ex: 3A40)
- `session` (optionnel): Filtrer par session ID

**Exemples:**
```bash
# Tous les étudiants
curl http://localhost:8080/api/students

# Étudiants d'une classe spécifique
curl http://localhost:8080/api/students?classe=3A40

# Étudiants d'une classe et session
curl "http://localhost:8080/api/students?classe=3A40&session=1"
```

**Réponse réussie (200):**
```json
{
  "success": true,
  "students": [...],
  "count": 25,
  "filteredByClasse": "3A40",
  "filteredBySession": null
}
```

#### GET /api/students/{id}
Récupère un étudiant spécifique par son ID.

**Exemple:**
```bash
curl http://localhost:8080/api/students/123
```

### 4. Modification d'un Étudiant

#### PUT /api/students/{id}
Met à jour les informations d'un étudiant (correction manuelle).

**Content-Type:** `application/json`

**Corps de requête:**
```json
{
  "decision": "Admis avec modération",
  "mention": "Passable",
  "observation": "Correction manuelle - cas particulier",
  "moyenneGenerale": 12.5,
  "reasonForChange": "Décision jury après délibération"
}
```

**Exemple avec cURL:**
```bash
curl -X PUT http://localhost:8080/api/students/123 \
     -H "Content-Type: application/json" \
     -d '{
       "decision": "Admis",
       "mention": "Bien",
       "observation": "Correction après recours"
     }'
```

**Réponse réussie (200):**
```json
{
  "success": true,
  "message": "Étudiant mis à jour avec succès",
  "updatedFields": 3,
  "timestamp": "2026-08-28T10:40:00"
}
```

### 5. Suppression d'un Étudiant

#### DELETE /api/students/{id}
Supprime un étudiant de la base de données.

**Exemple:**
```bash
curl -X DELETE http://localhost:8080/api/students/123
```

**Réponse réussie (200):**
```json
{
  "success": true,
  "message": "Étudiant supprimé avec succès",
  "studentId": 123,
  "timestamp": "2026-08-28T10:45:00"
}
```

## Codes de Réponse HTTP

| Code | Signification | Description |
|------|---------------|-------------|
| 200  | OK            | Requête réussie |
| 400  | Bad Request   | Paramètres invalides |
| 404  | Not Found     | Ressource non trouvée |
| 500  | Server Error  | Erreur interne du serveur |

## Format des Erreurs

Toutes les erreurs suivent ce format :

```json
{
  "success": false,
  "error": "Type d'erreur",
  "message": "Description de l'erreur",
  "details": "Détails techniques (optionnel)",
  "timestamp": "2026-08-28T10:30:00"
}
```

## Règles de Décision Appliquées

Selon le cahier des charges officiel :

| Moyenne Générale | Décision | Mention |
|------------------|----------|---------|
| ≥ 16             | Admis    | Très Bien |
| 14 - 16          | Admis    | Bien |
| 12 - 14          | Admis    | Assez Bien |
| 10 - 12          | Admis    | Passable |
| 8 - 10           | Ajourné  | Session de rattrapage |
| < 8              | Ajourné/Exclu | Règlement |

## Sécurité et Limitations

- **CORS**: Activé pour tous les domaines (développement seulement)
- **Authentification**: Non implémentée dans cette version
- **Rate Limiting**: Non implémenté
- **Taille de fichier**: Limitée par la configuration du serveur
- **Format de fichier**: Uniquement .xlsx et .xls

## Exemples d'Intégration

### JavaScript/Node.js
```javascript
// Import Excel
const formData = new FormData();
formData.append('file', fileInput.files[0]);

fetch('http://localhost:8080/api/import/excel', {
  method: 'POST',
  body: formData
})
.then(response => response.json())
.then(data => console.log(data));

// Validation des décisions
fetch('http://localhost:8080/api/import/validate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    students: students,
    classeGroupe: '3A40'
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

### Python
```python
import requests

# Import Excel
with open('etudiants.xlsx', 'rb') as f:
    files = {'file': f}
    response = requests.post('http://localhost:8080/api/import/excel', files=files)
    print(response.json())

# Récupération des étudiants
response = requests.get('http://localhost:8080/api/students?classe=3A40')
print(response.json())
```

### PowerShell
```powershell
# Import Excel
$uri = "http://localhost:8080/api/import/excel"
$filePath = "C:\path\to\etudiants.xlsx"
$response = Invoke-RestMethod -Uri $uri -Method Post -InFile $filePath -ContentType "multipart/form-data"

# Récupération des étudiants
$students = Invoke-RestMethod -Uri "http://localhost:8080/api/students?classe=3A40" -Method Get
```

## Dépannage

### Erreurs Communes

1. **Port déjà utilisé**
   - Changer le port dans l'interface ou arrêter l'autre service
   
2. **Fichier Excel corrompu**
   - Vérifier que le fichier s'ouvre dans Excel
   - S'assurer que les colonnes requises sont présentes
   
3. **Erreur de connexion base de données**
   - Vérifier la configuration de la base de données dans l'application
   
4. **Timeout de requête**
   - Réduire la taille du fichier Excel
   - Augmenter le timeout côté client

### Logs et Monitoring

Les logs de l'API sont disponibles dans l'onglet "API REST" de l'interface graphique. Ils incluent :
- Démarrage/arrêt du serveur
- Requêtes reçues
- Erreurs de traitement
- Statistiques d'utilisation

---

**Version**: 1.0  
**Dernière mise à jour**: 28 août 2026  
**Support**: Interface graphique de l'application desktop
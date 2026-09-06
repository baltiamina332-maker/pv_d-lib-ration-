# Guide d'Installation et d'Utilisation du Chatbot de Délibération

## 🎯 Vue d'ensemble

Ce système intègre un chatbot intelligent avec Anthropic Claude pour automatiser et faciliter les tâches de délibération. Le chatbot peut :

- **Analyser les données** d'étudiants et fournir des statistiques
- **Générer automatiquement les PV** de délibération  
- **Exporter des données** vers Excel
- **Naviguer dans l'interface** par commandes vocales
- **Gérer l'historique** des délibérations

## 🛠️ Installation

### 1. Prérequis
- Windows avec .NET Framework (déjà installé avec l'application)
- Python 3.7+ installé sur le système
- Accès Internet pour l'API Anthropic Claude

### 2. Installation Python
Si Python n'est pas installé :
```bash
# Télécharger depuis https://python.org
# OU via Chocolatey (si installé)
choco install python

# OU via Windows Store
# Rechercher "Python 3.11" dans le Microsoft Store
```

### 3. Installation du module Anthropic
```bash
pip install anthropic
```

### 4. Configuration de la clé API

#### Option A: Via Variable d'environnement (Recommandé)
```bash
# Dans un terminal Windows (cmd ou PowerShell en mode Admin)
setx ANTHROPIC_API_KEY "sk-ant-votre-cle-api-ici"

# Redémarrer le terminal après cette commande
```

#### Option B: Via le fichier .env (Alternative)
Créer un fichier `.env` dans le dossier de l'application :
```
ANTHROPIC_API_KEY=sk-ant-votre-cle-api-ici
```

### 5. Obtenir une clé API Anthropic
1. Aller sur https://console.anthropic.com
2. Créer un compte ou se connecter
3. Aller dans "API Keys"
4. Créer une nouvelle clé API
5. Copier la clé (format: `sk-ant-...`)

## 🚀 Utilisation

### Démarrage Automatique
Le chatbot se lance automatiquement avec l'application. Vous verrez :
- **Mode Avancé (🐍 Python + Claude)** si tout est configuré
- **Mode Local** si Python/API non configurés

### Interface Utilisateur

#### 1. Barre de saisie rapide (en haut)
```
Demandez à l'IA... (ex: Mentions Bien ?, PV 3A40)
```
Tapez votre question et appuyez sur Entrée ou cliquez sur ➔

#### 2. Assistant IA latéral
Cliquez sur **🤖 Assistant IA** dans la barre supérieure pour ouvrir le panneau complet avec :
- Historique des conversations
- Suggestions de questions
- Statistiques en temps réel

### Exemples de Commandes

#### 📊 Statistiques et Analyses
```
"Combien d'étudiants ont eu une mention Bien ?"
"Quel est le taux de réussite de la classe 3A40 ?"
"Montre-moi les statistiques globales"
"Compare la classe 3A40 avec 4TWIN1"
```

#### 📄 Génération de Documents
```
"Génère-moi le PV de la classe 3A40"
"Crée le procès-verbal pour la promotion 2024"
"Prépare le document de délibération"
```

#### 📁 Export et Gestion
```
"Exporte les données en Excel"
"Exporte seulement les étudiants admis"
"Supprime l'entrée d'historique #15"
```

#### 🧭 Navigation
```
"Va dans l'onglet historique"
"Ouvre la page des paramètres"
"Affiche le tableau de bord"
"Filtre la classe 3A40"
```

#### ✉️ Actions Avancées
```
"Envoie le PV à admin@ecole.fr et directeur@ecole.fr"
"Supprime le filtre actuel"
"Rafraîchis les données"
```

## 🔧 Fonctionnalités Techniques

### Actions UI Automatiques
Le chatbot peut déclencher automatiquement :
- **Changement d'onglet** (`basculer_onglet_generation_pv`)
- **Export Excel** (`exporter_excel`) 
- **Filtrage de classe** (`filtrer_classe`)
- **Suppression d'historique** (`supprimer_historique`)
- **Envoi d'email** (`envoyer_email`)

### Système de Fallback à 3 Niveaux
1. **Chatbot Python + Anthropic Claude** (Mode optimal)
2. **API Anthropic directe C#** (Si Python indisponible)
3. **Moteur local intelligent** (Si pas d'API)

### Données Partagées
Le chatbot accède aux données via `donnees_etudiants_import.csv` exporté automatiquement par l'application.

## 🐛 Dépannage

### Problème: "Mode Local" au lieu de "Mode Avancé"

#### Vérification 1: Python installé ?
```bash
python --version
```
Doit retourner `Python 3.x.x`

#### Vérification 2: Module anthropic installé ?
```bash
python -c "import anthropic; print('OK')"
```
Doit retourner `OK`

#### Vérification 3: Clé API configurée ?
```bash
echo %ANTHROPIC_API_KEY%
```
Doit retourner votre clé `sk-ant-...`

#### Vérification 4: Fichier chatbot présent ?
Le fichier `chatbot_deliberation.py` doit être dans le dossier de l'application.

### Problème: "Timeout" ou erreurs Python

#### Solution 1: Redémarrer l'application
Fermez et relancez l'application après avoir configuré Python.

#### Solution 2: Vérifier les permissions
Exécutez l'application en tant qu'administrateur si nécessaire.

#### Solution 3: Tester manuellement
```bash
python chatbot_deliberation.py
# Devrait démarrer un mode interactif
```

### Problème: API Anthropic inaccessible

#### Vérification des quotas
- Connectez-vous à https://console.anthropic.com
- Vérifiez votre usage et vos crédits
- Vérifiez que la clé API est valide

#### Test direct
```python
import anthropic
client = anthropic.Anthropic(api_key="votre-cle")
print("API OK")
```

## 📈 Ajout de Nouvelles Capacités

### Pour ajouter un nouvel outil au chatbot :

#### 1. Déclarer l'outil dans `chatbot_deliberation.py`
```python
TOOLS.append({
    "name": "mon_nouvel_outil",
    "description": "Description de ce que fait l'outil",
    "input_schema": {
        "type": "object", 
        "properties": {
            "param1": {"type": "string", "description": "Description du paramètre"}
        },
        "required": ["param1"]
    }
})
```

#### 2. Ajouter le traitement dans `_executer_outil()`
```python
elif nom_outil == "mon_nouvel_outil":
    param1 = arguments.get("param1")
    # Logique de traitement
    return json.dumps({
        "statut": "succès",
        "message": "Action réalisée",
        "action_ui": "nom_action_ui",
        "param1": param1
    }, ensure_ascii=False)
```

#### 3. Ajouter le traitement UI dans `ConvertirReponsePython()` (C#)
```csharp
case "nom_action_ui":
    response.Action = new AiAction
    {
        Type = AiActionType.MonNouveauType,
        TargetParameter = pythonResponse.Param1
    };
    break;
```

## 📞 Support

- **Fichiers de log** : Vérifiez la console de l'application pour les messages de debug
- **Test manuel** : Utilisez `python chatbot_deliberation.py` pour tester en mode CLI
- **Mode développeur** : Activez la console Windows pour voir les messages détaillés

---

*Version 1.0 - Compatible avec l'application de délibération PV Generator*
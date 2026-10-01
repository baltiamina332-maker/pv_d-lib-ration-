# 🔧 Guide de Dépannage - Application de Délibération

## 🚨 **PROBLÈMES DE COMPILATION**

### **Erreur: "PythonChatbotService introuvable"**

**Cause** : Ancienne référence dans le code ou fichiers XAML non générés

**Solution** :
```bash
1. Ouvrir le projet dans Visual Studio (pas VS Code)
2. Build → Clean Solution
3. Build → Rebuild Solution
4. Les fichiers .g.cs seront régénérés automatiquement
```

### **Erreur: "HttpClient introuvable dans System.Net.Http"**

**Cause** : Version .NET Framework ou références manquantes

**Solution** :
```xml
<!-- Vérifier dans DesktopApp.csproj -->
<Reference Include="System.Net.Http" />

<!-- Ou ajouter via NuGet -->
Install-Package System.Net.Http -Version 4.3.4
```

### **Erreur: "ChatbotResponse introuvable"**

**Cause** : Cette classe n'existe pas, elle a été remplacée par AiAssistantResponse

**Solution** : Le code est déjà corrigé, recompiler proprement

---

## 🐍 **PROBLÈMES CHATBOT PYTHON**

### **Python non trouvé**

**Diagnostic** :
```bash
python --version
# Si erreur: Python n'est pas installé
```

**Solution** :
```bash
# Télécharger Python depuis python.org
# Ou installer via Windows Store
# Ou utiliser Anaconda

# Vérifier installation
python --version
pip --version
```

### **Modules Python manquants**

**Erreur** : `ModuleNotFoundError: No module named 'anthropic'`

**Solution** :
```bash
# Installer les dépendances
pip install anthropic python-dotenv

# Ou utiliser le script fourni
install_chatbot.bat
```

### **Clé API Anthropic invalide**

**Erreur** : `401 Unauthorized` dans les logs

**Solution** :
```bash
# Obtenir une clé sur console.anthropic.com
# Configurer la variable d'environnement
setx ANTHROPIC_API_KEY "sk-ant-votre-cle-ici"

# Redémarrer l'application après cette commande
```

---

## 📁 **PROBLÈMES DE FICHIERS**

### **Fichiers Excel non reconnus**

**Symptômes** : 
- "Format de fichier non supporté"
- "Impossible de lire le fichier"

**Solutions** :
```
1. Vérifier que le fichier n'est pas ouvert dans Excel
2. Utiliser format .xlsx (pas .xls)
3. Vérifier les permissions de lecture
4. Essayer avec un fichier CSV en alternative
```

### **Erreurs de permissions**

**Symptômes** : 
- "Accès refusé"
- "Impossible d'écrire dans le dossier"

**Solutions** :
```
1. Lancer l'application en tant qu'administrateur
2. Vérifier les permissions du dossier Documents
3. Créer manuellement le dossier PV_Générés
4. Changer l'emplacement de sortie
```

---

## 🗄️ **PROBLÈMES DE BASE DE DONNÉES**

### **Connexion MySQL échoue**

**Erreur** : `Unable to connect to MySQL server`

**Solution** :
```
1. Vérifier que MySQL est démarré
2. Contrôler les paramètres de connexion dans App.config
3. Tester la connexion avec MySQL Workbench
4. Vérifier le pare-feu Windows
```

### **Tables manquantes**

**Erreur** : `Table 'deliberations' doesn't exist`

**Solution** :
```sql
-- Créer les tables nécessaires
CREATE TABLE IF NOT EXISTS deliberations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    date_deliberation DATETIME,
    classe VARCHAR(50),
    session VARCHAR(20),
    nom_fichier VARCHAR(255),
    chemin_fichier VARCHAR(500),
    nb_etudiants INT,
    nb_admis INT,
    nb_ajournes INT,
    utilisateur_id INT,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## 🖥️ **PROBLÈMES D'INTERFACE**

### **Chatbot ne s'affiche pas**

**Diagnostic** :
- Vérifier les logs de la console
- Tester la barre de saisie rapide
- Essayer d'ouvrir le panneau latéral

**Solution** :
```
1. Recompiler avec Visual Studio
2. Vérifier que AiAssistantService est initialisé
3. Tester le mode fallback local
```

### **Actions UI ne fonctionnent pas**

**Symptômes** :
- "Génère PV" ne bascule pas d'onglet
- Export Excel ne se déclenche pas

**Solution** :
```csharp
// Vérifier dans ExecuteAiAction() que les contrôles existent
if (tabMain != null && action.TargetIndex < tabMain.Items.Count)
{
    tabMain.SelectedIndex = action.TargetIndex;
}
```

---

## ⚡ **PROBLÈMES DE PERFORMANCE**

### **Génération PV trop lente**

**Diagnostic** : Temps > 5 secondes (non conforme CDC)

**Solutions** :
```
1. Vérifier la taille des données (< 1000 étudiants)
2. Contrôler l'espace disque disponible
3. Désactiver l'antivirus temporairement
4. Utiliser un SSD plutôt qu'un HDD
```

### **Import Excel lent**

**Causes possibles** :
- Fichier Excel très volumineux
- Nombreuses formules dans le fichier
- Antivirus qui scanne chaque cellule

**Solutions** :
```
1. Convertir en CSV pour accélérer
2. Exporter depuis Excel sans formules
3. Utiliser des fichiers < 10MB
```

---

## 🔍 **DIAGNOSTICS AVANCÉS**

### **Mode Debug Console**

**Activer les logs** :
```csharp
// Dans App.config, ajouter
<appSettings>
    <add key="DebugMode" value="true" />
    <add key="LogLevel" value="Verbose" />
</appSettings>
```

**Lire les logs** :
```
1. Ouvrir Visual Studio
2. Debug → Windows → Output
3. Sélectionner "Debug" dans la liste déroulante
4. Voir tous les messages console
```

### **Test des Services Indépendamment**

**Tester ExcelImportService** :
```csharp
var service = new ExcelImportService();
var result = service.ImportFromExcel("test.xlsx");
Console.WriteLine(result.Message);
```

**Tester AiAssistantService** :
```csharp
var ai = new AiAssistantService();
var response = ai.ProcessPrompt("Test", new List<Etudiant>());
Console.WriteLine(response.ResponseText);
```

---

## 📞 **SUPPORT TECHNIQUE**

### **Informations à Collecter**

Avant de demander de l'aide, rassemblez :

```
1. Version de Windows
2. Version de Visual Studio / .NET Framework
3. Message d'erreur exact
4. Logs de la console (si disponibles)
5. Étapes pour reproduire le problème
6. Taille et format des fichiers utilisés
```

### **Tests de Base**

**Checklist rapide** :
- [ ] Projet compile sans erreurs
- [ ] Application se lance
- [ ] Import Excel fonctionne
- [ ] Validation automatique marche
- [ ] Génération PV réussit
- [ ] Chatbot répond (au moins en mode local)
- [ ] Export Excel fonctionne

---

## 🎯 **SOLUTIONS RAPIDES COURANTES**

### **"L'application plante au démarrage"**
```
1. Compiler en mode Release (pas Debug)
2. Vérifier App.config (syntaxe XML)
3. Tester sur une machine propre
```

### **"Import Excel ne marche jamais"**
```
1. Tester avec etudiants.csv fourni
2. Vérifier format des colonnes
3. Essayer fichier Excel simple (3-4 lignes)
```

### **"Chatbot ne répond pas"**
```
1. Normal: Le fallback local doit fonctionner
2. Tester: "Combien d'étudiants ?"
3. Vérifier: Initialisation AiAssistantService
```

---

**En cas de problème persistant, compiler et tester étape par étape en suivant le guide STATUS_FINAL_IMPLEMENTATION.md**
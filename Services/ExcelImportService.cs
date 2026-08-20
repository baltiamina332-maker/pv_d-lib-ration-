using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour importer les données depuis un fichier Excel (utilise ClosedXML)
    /// </summary>
    public class ExcelImportService
    {
        public ExcelImportService()
        {
            // ClosedXML ne nécessite pas de licence
        }

        /// <summary>
        /// Importer les données d'un fichier Excel ou CSV
        /// Route automatiquement vers le bon parser selon l'extension du fichier
        /// </summary>
        public ImportResult ImporterDonneesExcel(string cheminFichier)
        {
            // Déterminer le type de fichier et router vers le bon parser
            string extension = Path.GetExtension(cheminFichier).ToLower();
            
            if (extension == ".csv")
            {
                return ImporterDonneesCsv(cheminFichier);
            }
            else if (extension == ".xlsx" || extension == ".xls" || extension == ".xlsm" || extension == ".xlt" || extension == ".xltm")
            {
                return ImporterDonneesExcelNatif(cheminFichier);
            }
            else
            {
                return new ImportResult
                {
                    Succes = false,
                    MessageErreur = $"Extension '{extension}' non supportée. Les formats acceptés sont : .xlsx, .xls, .xlsm, .xlt, .xltm et .csv"
                };
            }
        }

        /// <summary>
        /// Importer les données d'un fichier Excel (format natif)
        /// </summary>
        private ImportResult ImporterDonneesExcelNatif(string cheminFichier)
        {
            var result = new ImportResult();

            try
            {
                // Vérifier que le fichier existe
                if (!File.Exists(cheminFichier))
                {
                    result.Succes = false;
                    result.MessageErreur = "Le fichier spécifié n'existe pas.";
                    return result;
                }

                // Essayer de libérer le verrou du fichier (fichiers temporaires Excel)
                LibererVerrouFichier(cheminFichier);

                // Essayer d'ouvrir le fichier en lecture avec partage complet
                XLWorkbook workbook = null;
                FileStream fileStream = null;
                int tentatives = 3;
                int delai = 500; // millisecondes

                while (tentatives > 0)
                {
                    try
                    {
                        fileStream = new FileStream(cheminFichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        workbook = new XLWorkbook(fileStream);
                        break; // Succès
                    }
                    catch (IOException) when (tentatives > 1)
                    {
                        if (fileStream != null)
                        {
                            fileStream.Dispose();
                            fileStream = null;
                        }
                        tentatives--;
                        System.Threading.Thread.Sleep(delai);
                    }
                }

                if (workbook == null)
                {
                    if (fileStream != null) fileStream.Dispose();
                    result.Succes = false;
                    result.MessageErreur = "Le fichier est toujours verrouillé malgré les tentatives. Fermez-le dans Excel et réessayez.";
                    return result;
                }

                // Charger le fichier Excel avec ClosedXML
                using (workbook)
                {
                    if (workbook.Worksheets.Count == 0)
                    {
                        result.Succes = false;
                        result.MessageErreur = "Le fichier Excel ne contient aucune feuille.";
                        return result;
                    }

                    // Prendre la première feuille
                    var worksheet = workbook.Worksheet(1);

                    // Valider la structure
                    if (!ValiderStructureFeuille(worksheet, result))
                    {
                        return result;
                    }

                    // Lire les données
                    result.Etudiants = LireDonneesEtudiants(worksheet, result);

                    if (result.Etudiants.Count > 0)
                    {
                        result.Succes = true;
                        result.MessageSucces = $"{result.Etudiants.Count} étudiant(s) importé(s) avec succès.";
                    }
                    else
                    {
                        result.Succes = false;
                        result.MessageErreur = "Aucune donnée valide trouvée dans le fichier.";
                    }
                }
                
                // Libérer le stream après utilisation de ClosedXML
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
            catch (IOException ioEx)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur d'accès au fichier : {ioEx.Message}\n\nVérifiez que :\n- Le fichier n'est pas ouvert dans Excel\n- Vous avez les permissions d'accès\n- Le fichier n'est pas sur un lecteur réseau inaccessible";
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de l'import : {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Importer les données d'un fichier CSV
        /// Format: id_etudiant, nom, prenom, matricule, classe_groupe, id_session, type_session, annee_universitaire, moyenne_generale, ects, statut, moyenne_ue
        /// </summary>
        private ImportResult ImporterDonneesCsv(string cheminFichier)
        {
            var result = new ImportResult();

            try
            {
                // Vérifier que le fichier existe
                if (!File.Exists(cheminFichier))
                {
                    result.Succes = false;
                    result.MessageErreur = "Le fichier CSV spécifié n'existe pas.";
                    return result;
                }

                Console.WriteLine("[IMPORT CSV] Début de l'import du fichier CSV");
                var etudiants = new List<Etudiant>();

                using (StreamReader reader = new StreamReader(cheminFichier, System.Text.Encoding.UTF8))
                {
                    // Lire l'en-tête
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        result.Succes = false;
                        result.MessageErreur = "Le fichier CSV est vide.";
                        return result;
                    }

                    Console.WriteLine($"[IMPORT CSV] En-tête : {headerLine}");

                    // Parser le fichier CSV ligne par ligne
                    int lineNumber = 1; // Ligne 1 = en-tête
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;

                        // Ignorer les lignes vides
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        try
                        {
                            // Parser la ligne CSV
                            var values = ParcerLigneCSV(line);

                            if (values.Count < 9)
                            {
                                result.Avertissements.Add($"Ligne {lineNumber} : nombre de colonnes insuffisant ({values.Count} colonnes trouvées, 9 attendues)");
                                continue;
                            }

                            // Extraire les valeurs (colonnes: id_etudiant, nom, prenom, matricule, classe_groupe, id_session, type_session, annee_universitaire, moyenne_generale, ects, statut, moyenne_ue)
                            int idEtudiant = ParseInt(values[0]);
                            string nom = values[1].Trim();
                            string prenom = values[2].Trim();
                            string matricule = values[3].Trim();
                            string classeGroupe = values[4].Trim();
                            int idSession = ParseInt(values[5]);
                            string typeSession = values[6].Trim();
                            string anneeUniv = values[7].Trim();
                            decimal mg = ParseDecimal(values[8]);

                            // Les colonnes optionnelles
                            int ects = (values.Count > 9) ? ParseInt(values[9]) : 30;
                            string statut = (values.Count > 10) ? values[10].Trim() : "Nouveau";
                            decimal moyenneUE = (values.Count > 11) ? ParseDecimal(values[11]) : mg;

                            string nomPrenom = $"{nom} {prenom}".Trim();

                            var etudiant = new Etudiant
                            {
                                Id = idEtudiant,
                                NumeroOrdre = idEtudiant,
                                Nom = nom,
                                Prenom = prenom,
                                NomPrenom = nomPrenom,
                                Matricule = matricule,
                                ClasseGroupe = classeGroupe,
                                IdSession = idSession,
                                AnneeUniversitaire = anneeUniv,
                                MoyenneGenerale = mg,
                                EctsValides = ects > 0 ? ects : 30,
                                EctsTotal = 30,
                                Statut = statut,
                                EstAncienEtudiant = statut.ToLower().Contains("ancien"),
                                MoyenneUE = moyenneUE > 0 ? moyenneUE : mg,
                                ModulesGrades = new List<ModuleGrades>()
                            };

                            Console.WriteLine($"[IMPORT CSV] Ligne {lineNumber}: {nomPrenom} | MG:{mg:F2} | ECTS:{etudiant.EctsValides} | Statut:{statut}");
                            etudiants.Add(etudiant);
                        }
                        catch (Exception ex)
                        {
                            result.Avertissements.Add($"Ligne {lineNumber} : {ex.Message}");
                            Console.WriteLine($"[IMPORT CSV] ⚠️ Erreur ligne {lineNumber}: {ex.Message}");
                        }
                    }
                }

                if (etudiants.Count > 0)
                {
                    result.Succes = true;
                    result.Etudiants = etudiants;
                    result.MessageSucces = $"{etudiants.Count} étudiant(s) importé(s) depuis le fichier CSV avec succès.";
                    Console.WriteLine($"[IMPORT CSV] Import terminé : {etudiants.Count} étudiant(s)");
                }
                else
                {
                    result.Succes = false;
                    result.MessageErreur = "Aucune donnée valide trouvée dans le fichier CSV.";
                }
            }
            catch (IOException ioEx)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur d'accès au fichier CSV : {ioEx.Message}";
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de l'import CSV : {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Parser une ligne CSV en tenant compte des guillemets et des séparateurs
        /// </summary>
        private List<string> ParcerLigneCSV(string line)
        {
            var values = new List<string>();
            var currentValue = new System.Text.StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (c == ',' && !insideQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Ajouter la dernière valeur
            values.Add(currentValue.ToString());

            return values;
        }

        /// <summary>
        /// Parser un entier avec gestion d'erreur
        /// </summary>
        private int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            if (int.TryParse(value.Trim(), out int result))
                return result;
            return 0;
        }

        /// <summary>
        /// Parser un décimal avec gestion d'erreur
        /// </summary>
        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;
            
            string trimmed = value.Trim();
            trimmed = trimmed.Replace(',', '.');  // Gérer les deux formats

            if (decimal.TryParse(trimmed, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;
            
            return 0m;
        }

        /// <summary>
        /// Libérer le verrou du fichier Excel (supprimer les fichiers temporaires)
        /// </summary>
        private void LibererVerrouFichier(string cheminFichier)
        {
            try
            {
                string repertoire = Path.GetDirectoryName(cheminFichier);
                string nomFichier = Path.GetFileName(cheminFichier);

                // Chercher les fichiers temporaires Excel (commençant par ~$)
                string fichierTemp = Path.Combine(repertoire, $"~${nomFichier}");

                if (File.Exists(fichierTemp))
                {
                    try
                    {
                        File.Delete(fichierTemp);
                    }
                    catch
                    {
                        // Ignorer l'erreur - le fichier peut être en cours d'utilisation
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs de nettoyage
            }
        }

        /// <summary>
        /// Valider la structure de la feuille Excel
        /// </summary>
        private bool ValiderStructureFeuille(IXLWorksheet worksheet, ImportResult result)
        {
            // Vérifier qu'il y a des données
            if (worksheet.LastRowUsed() == null)
            {
                result.MessageErreur = "La feuille Excel est vide.";
                return false;
            }

            // Vérifier les en-têtes (ligne 1)
            int colCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            
            // Validation souple : au moins 5 colonnes essentielles
            if (colCount < 5)
            {
                result.MessageErreur = "Le fichier ne contient pas assez de colonnes. Format attendu : N°, Nom et Prénom, Matricule, Classe, Moyenne, Décision, Mention.";
                result.Avertissements.Add("Colonnes minimales requises : N°, Nom, Matricule, Classe, Moyenne");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lire les données des étudiants
        /// Supporte deux formats:
        /// Format 1: N°, Nom Prénom, Matricule, Classe, Année, MG, ECTS (format classique)
        /// Format 2: N°, Nom Prénom, Matricule, Classe, Année, Module, CC, TP, Examen, Crédit (calcul de moyenne)
        /// </summary>
        private List<Etudiant> LireDonneesEtudiants(IXLWorksheet worksheet, ImportResult result)
        {
            var etudiants = new List<Etudiant>();
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            // Lire l'en-tête pour déterminer le format
            string colonneF_Header = LireTexte(worksheet, 1, 6).ToLower();
            bool estFormatModuleGrades = colonneF_Header.Contains("module") || 
                                        colonneF_Header.Contains("cc") ||
                                        colonneF_Header.Contains("tp");

            if (estFormatModuleGrades)
            {
                // Format 2: Avec modules et grades CC/TP/Examen
                etudiants = LireDonneesAvecModules(worksheet, result, lastRow);
            }
            else
            {
                // Format 1: Avec MG pré-calculée
                etudiants = LireDonneesClassique(worksheet, result, lastRow);
            }

            return etudiants;
        }

        /// <summary>
        /// <summary>
        /// Lire les données au format classique (MG pré-calculée)
        /// Supporte le gabarit CDC Annexe A (N°, Nom et Prénom, Matricule, Classe, Moyenne générale, Décision, Mention, Observation)
        /// et les formats CSV/Excel personnalisés.
        /// </summary>
        private List<Etudiant> LireDonneesClassique(IXLWorksheet worksheet, ImportResult result, int lastRow)
        {
            var etudiants = new List<Etudiant>();

            Console.WriteLine("[IMPORT] Parsing des colonnes Excel selon les en-têtes...");

            // Détecter les positions des colonnes d'après la ligne 1
            int colCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 10;
            
            int colNum = -1, colNom = -1, colPrenom = -1, colMatricule = -1, colClasse = -1;
            int colMoyenne = -1, colDecision = -1, colMention = -1, colObservation = -1;
            int colSession = -1, colAnnee = -1, colValidation = -1;

            for (int col = 1; col <= colCount; col++)
            {
                string header = LireTexte(worksheet, 1, col).ToLower();
                if (string.IsNullOrWhiteSpace(header)) continue;

                if ((header.Contains("n°") || header.Contains("num") || header == "id" || header.Contains("id_etudiant")) && colNum == -1)
                    colNum = col;
                else if (header.Contains("nom et prénom") || header.Contains("nom & prénom") || header == "nom_prenom" || (header.Contains("nom") && !header.Contains("prénom") && !header.Contains("prenom") && colNom == -1))
                    colNom = col;
                else if ((header.Contains("prénom") || header.Contains("prenom")) && colPrenom == -1)
                    colPrenom = col;
                else if ((header.Contains("matricule") || header.Contains("cin") || header.Contains("cne")) && colMatricule == -1)
                    colMatricule = col;
                else if ((header.Contains("classe") || header.Contains("groupe") || header.Contains("filiere") || header.Contains("filière")) && colClasse == -1)
                    colClasse = col;
                else if ((header.Contains("moyenne") || header == "mg" || header.Contains("moyenne_generale")) && colMoyenne == -1)
                    colMoyenne = col;
                else if ((header.Contains("décision") || header.Contains("decision")) && colDecision == -1)
                    colDecision = col;
                else if (header.Contains("mention") && colMention == -1)
                    colMention = col;
                else if ((header.Contains("observation") || header.Contains("communication") || header.Contains("remarque")) && colObservation == -1)
                    colObservation = col;
                else if (header.Contains("session") && colSession == -1)
                    colSession = col;
                else if (header.Contains("annee") || header.Contains("année") && colAnnee == -1)
                    colAnnee = col;
                else if (header.Contains("validation") && colValidation == -1)
                    colValidation = col;
            }

            // Positions par défaut si non trouvées par en-tête
            if (colNum == -1) colNum = 1;
            if (colNom == -1) colNom = 2;
            if (colPrenom == -1 && colNom != 2 && colNom != -1) colPrenom = 3;
            if (colMatricule == -1) colMatricule = colPrenom != -1 ? 4 : 3;
            if (colClasse == -1) colClasse = colMatricule + 1;
            if (colMoyenne == -1)
            {
                // Chercher colonne numérique entre colClasse+1 et colCount
                for (int c = colClasse + 1; c <= colCount; c++)
                {
                    string h = LireTexte(worksheet, 1, c).ToLower();
                    if (h.Contains("moyenne") || h.Contains("mg") || h == "")
                    {
                        colMoyenne = c;
                        break;
                    }
                }
                if (colMoyenne == -1) colMoyenne = 5;
            }
            if (colDecision == -1) colDecision = colMoyenne + 1;
            if (colMention == -1) colMention = colDecision + 1;
            if (colObservation == -1) colObservation = colMention + 1;

            Console.WriteLine($"[IMPORT] Mapping colonnes -> N°:{colNum}, Nom:{colNom}, Prénom:{colPrenom}, Matricule:{colMatricule}, Classe:{colClasse}, MG:{colMoyenne}, Décision:{colDecision}, Mention:{colMention}, Obs:{colObservation}");

            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    if (EstLigneVide(worksheet, row))
                        continue;

                    int numOrdre = LireEntier(worksheet, row, colNum);
                    if (numOrdre == 0) numOrdre = row - 1;

                    string nomVal = LireTexte(worksheet, row, colNom);
                    string prenomVal = colPrenom != -1 ? LireTexte(worksheet, row, colPrenom) : "";

                    string nomPrenom;
                    string nom;
                    string prenom;

                    if (!string.IsNullOrEmpty(prenomVal))
                    {
                        nom = nomVal;
                        prenom = prenomVal;
                        nomPrenom = $"{nomVal} {prenomVal}".Trim();
                    }
                    else
                    {
                        nomPrenom = nomVal;
                        var parts = nomVal.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            nom = parts[0];
                            prenom = string.Join(" ", parts.Skip(1));
                        }
                        else
                        {
                            nom = nomVal;
                            prenom = "";
                        }
                    }

                    string matricule = colMatricule != -1 ? LireTexte(worksheet, row, colMatricule) : "";
                    string classeGroupe = colClasse != -1 ? LireTexte(worksheet, row, colClasse) : "";
                    decimal mg = colMoyenne != -1 ? LireDecimal(worksheet, row, colMoyenne) : 0m;
                    string decision = colDecision != -1 ? LireTexte(worksheet, row, colDecision) : "";
                    string mention = colMention != -1 ? LireTexte(worksheet, row, colMention) : "";
                    string observation = colObservation != -1 ? LireTexte(worksheet, row, colObservation) : "";
                    string anneeUniv = colAnnee != -1 ? LireTexte(worksheet, row, colAnnee) : DateTime.Now.Year.ToString();
                    string validation = colValidation != -1 ? LireTexte(worksheet, row, colValidation) : "";

                    var etudiant = new Etudiant
                    {
                        Id = numOrdre,
                        NumeroOrdre = numOrdre,
                        Nom = nom,
                        Prenom = prenom,
                        NomPrenom = nomPrenom,
                        Matricule = matricule,
                        ClasseGroupe = classeGroupe,
                        Filiere = classeGroupe,
                        AnneeUniversitaire = anneeUniv,
                        MoyenneGenerale = mg,
                        Decision = decision,
                        Mention = mention,
                        Observation = observation,
                        Validation = validation,
                        EctsValides = mg >= 10.0m ? 30 : 0,
                        EctsTotal = 30,
                        ModulesGrades = new List<ModuleGrades>()
                    };

                    Console.WriteLine($"[IMPORT] Ligne {row}: {nomPrenom} | Matricule:{matricule} | MG:{mg:F3} | Dec:{decision} | Mention:{mention}");

                    etudiants.Add(etudiant);
                }
                catch (Exception ex)
                {
                    result.Avertissements.Add($"Ligne {row} : {ex.Message}");
                    Console.WriteLine($"[IMPORT] ⚠️ Erreur ligne {row}: {ex.Message}");
                }
            }

            // Calculer le rang par MG décroissante
            var sorted = etudiants.OrderByDescending(e => e.MoyenneGenerale).ToList();
            for (int i = 0; i < sorted.Count; i++)
                sorted[i].Rang = i + 1;

            return etudiants;
        }

        /// <summary>
        /// Lire les données au format avec modules et grades
        /// Format: N°, Nom Prénom, Matricule, Classe, Année, Module, CC, TP, Examen, Crédit
        /// Note: Peut avoir plusieurs lignes par étudiant (une par module)
        /// </summary>
        private List<Etudiant> LireDonneesAvecModules(IXLWorksheet worksheet, ImportResult result, int lastRow)
        {
            var etudiantsDict = new Dictionary<string, Etudiant>(); // Key: Matricule (pour grouper par étudiant)
            
            Console.WriteLine("[IMPORT] Format détecté: AVEC MODULES (CC/TP/Examen à calculer)");

            for (int row = 2; row <= lastRow; row++)
            {
                try
                {
                    if (EstLigneVide(worksheet, row))
                        continue;

                    // Lire les colonnes de base
                    int numeroOrdre = LireEntier(worksheet, row, 1);    // Colonne A
                    string colonneB = LireTexte(worksheet, row, 2);     // Colonne B
                    string colonneC = LireTexte(worksheet, row, 3);     // Colonne C
                    string colonneD = LireTexte(worksheet, row, 4);     // Colonne D
                    
                    // Déterminer le format du nom
                    string nomPrenom;
                    string matricule;
                    string classeGroupe;
                    string anneeUniv;
                    int colModule;  // Colonne où commence "Module"

                    if (colonneB.Contains(" "))
                    {
                        nomPrenom = colonneB;
                        matricule = colonneC;
                        classeGroupe = colonneD;
                        anneeUniv = LireTexte(worksheet, row, 5);
                        colModule = 6;  // Colonne F
                    }
                    else
                    {
                        string prenom = colonneC;
                        nomPrenom = $"{colonneB} {prenom}".Trim();
                        matricule = colonneD;
                        classeGroupe = LireTexte(worksheet, row, 5);
                        anneeUniv = LireTexte(worksheet, row, 6);
                        colModule = 7;  // Colonne G
                    }

                    // Lire les données du module
                    string nomModule = LireTexte(worksheet, row, colModule);          // Module
                    decimal cc = LireDecimal(worksheet, row, colModule + 1);          // CC
                    decimal tp = LireDecimal(worksheet, row, colModule + 2);          // TP
                    decimal examen = LireDecimal(worksheet, row, colModule + 3);      // Examen
                    int credit = LireEntier(worksheet, row, colModule + 4);           // Crédit

                    // Créer ou récupérer l'étudiant
                    if (!etudiantsDict.ContainsKey(matricule))
                    {
                        var etudiant = new Etudiant
                        {
                            NumeroOrdre = numeroOrdre,
                            NomPrenom = nomPrenom,
                            Matricule = matricule,
                            ClasseGroupe = classeGroupe,
                            AnneeUniversitaire = anneeUniv,
                            MoyenneGenerale = 0,  // À calculer
                            EctsValides = 0,      // À calculer
                            EctsTotal = 30,
                            EstAncienEtudiant = false,
                            MoyenneUE = 0,
                            ModulesGrades = new List<ModuleGrades>()
                        };

                        // Parser Nom et Prenom
                        if (!string.IsNullOrEmpty(nomPrenom) && nomPrenom.Contains(" "))
                        {
                            var parts = nomPrenom.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                etudiant.Nom = parts[0];
                                etudiant.Prenom = string.Join(" ", parts.Skip(1));
                            }
                            else if (parts.Length == 1)
                            {
                                etudiant.Nom = parts[0];
                                etudiant.Prenom = "";
                            }
                        }
                        else
                        {
                            etudiant.Nom = nomPrenom ?? "";
                            etudiant.Prenom = "";
                        }

                        etudiantsDict[matricule] = etudiant;
                    }

                    // Ajouter le module à l'étudiant
                    var moduleGrades = new ModuleGrades
                    {
                        NomModule = nomModule,
                        CC = cc,
                        TP = tp,
                        Examen = examen,
                        Credit = credit
                    };

                    etudiantsDict[matricule].ModulesGrades.Add(moduleGrades);
                    etudiantsDict[matricule].EctsValides += credit;  // Accumuler les crédits

                    Console.WriteLine($"  Ligne {row}: {nomPrenom} | Module: {nomModule} | CC:{cc:F2} TP:{tp:F2} Exam:{examen:F2} | Crédit: {credit}");
                }
                catch (Exception ex)
                {
                    result.Avertissements.Add($"Ligne {row} : {ex.Message}");
                }
            }

            // Convertir le dictionnaire en liste
            var etudiants = etudiantsDict.Values.ToList();
            Console.WriteLine($"[IMPORT] {etudiants.Count} étudiant(s) trouvé(s) avec modules");

            return etudiants;
        }

        // Méthodes utilitaires de lecture

        private bool EstLigneVide(IXLWorksheet worksheet, int row)
        {
            int lastCol = Math.Min(5, worksheet.LastColumnUsed()?.ColumnNumber() ?? 5);
            for (int col = 1; col <= lastCol; col++)
            {
                var cellule = worksheet.Cell(row, col);
                if (!cellule.IsEmpty())
                    return false;
            }
            return true;
        }

        private string LireTexte(IXLWorksheet worksheet, int row, int col)
        {
            return worksheet.Cell(row, col).GetString().Trim();
        }

        private int LireEntier(IXLWorksheet worksheet, int row, int col)
        {
            var texte = LireTexte(worksheet, row, col);
            if (int.TryParse(texte, out int valeur))
                return valeur;
            return 0;
        }

        private decimal LireDecimal(IXLWorksheet worksheet, int row, int col)
        {
            try
            {
                return (decimal)worksheet.Cell(row, col).GetDouble();
            }
            catch
            {
                var texte = LireTexte(worksheet, row, col);
                texte = texte.Replace(',', '.');
                
                if (decimal.TryParse(texte, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out decimal valeur))
                    return valeur;
                
                return 0;
            }
        }
    }

    /// <summary>
    /// Résultat de l'import Excel
    /// </summary>
    public class ImportResult
    {
        public bool Succes { get; set; }
        public string MessageSucces { get; set; }
        public string MessageErreur { get; set; }
        public List<string> Avertissements { get; set; }
        public List<Etudiant> Etudiants { get; set; }

        public ImportResult()
        {
            Avertissements = new List<string>();
            Etudiants = new List<Etudiant>();
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour importer les données depuis un fichier Excel ou CSV.
    /// Supporte la détection dynamique des colonnes (en-têtes) et le parsing multilingue (français/anglais)
    /// pour garantir l'exactitude de la Moyenne Générale.
    /// </summary>
    public class ExcelImportService
    {
        public ExcelImportService()
        {
        }

        /// <summary>
        /// Importer les données d'un fichier Excel ou CSV
        /// Route automatiquement vers le bon parser selon l'extension du fichier
        /// </summary>
        public ImportResult ImporterDonneesExcel(string cheminFichier)
        {
            if (string.IsNullOrWhiteSpace(cheminFichier) || !File.Exists(cheminFichier))
            {
                return new ImportResult
                {
                    Succes = false,
                    MessageErreur = "Le fichier spécifié n'existe pas."
                };
            }

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
                if (!File.Exists(cheminFichier))
                {
                    result.Succes = false;
                    result.MessageErreur = "Le fichier spécifié n'existe pas.";
                    return result;
                }

                result.Avertissements.Add($"DIAGNOSTIC: Fichier trouvé: {cheminFichier}");
                result.Avertissements.Add($"DIAGNOSTIC: Taille du fichier: {new FileInfo(cheminFichier).Length} octets");

                LibererVerrouFichier(cheminFichier);

                XLWorkbook workbook = null;
                FileStream fileStream = null;
                int tentatives = 3;
                int delai = 500;

                result.Avertissements.Add($"DIAGNOSTIC: Tentative d'ouverture du fichier Excel...");

                string shadowCopyCreated = null;

                while (tentatives > 0)
                {
                    try
                    {
                        fileStream = new FileStream(cheminFichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        workbook = new XLWorkbook(fileStream);
                        result.Avertissements.Add($"DIAGNOSTIC: Fichier ouvert avec succès");
                        break;
                    }
                    catch (IOException ioEx)
                    {
                        if (fileStream != null)
                        {
                            fileStream.Dispose();
                            fileStream = null;
                        }

                        tentatives--;
                        if (tentatives > 0)
                        {
                            result.Avertissements.Add($"DIAGNOSTIC: Tentative échouée ({ioEx.Message}), réessai...");
                            System.Threading.Thread.Sleep(delai);
                        }
                        else
                        {
                            // Tentative ultime de contournement des verrous d'application (Excel open lock)
                            try
                            {
                                shadowCopyCreated = Path.Combine(Path.GetTempPath(), $"pv_shadow_{Guid.NewGuid():N}.xlsx");
                                File.Copy(cheminFichier, shadowCopyCreated, true);
                                fileStream = new FileStream(shadowCopyCreated, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                workbook = new XLWorkbook(fileStream);
                                result.Avertissements.Add($"DIAGNOSTIC: Fichier ouvert avec succès via copie temporaire anti-verrouillage.");
                                break;
                            }
                            catch (Exception shadowEx)
                            {
                                result.Avertissements.Add($"DIAGNOSTIC: Échec de la copie temporaire: {shadowEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Avertissements.Add($"DIAGNOSTIC: Erreur d'ouverture: {ex.GetType().Name}: {ex.Message}");
                        if (fileStream != null)
                        {
                            fileStream.Dispose();
                            fileStream = null;
                        }
                        result.Succes = false;
                        result.MessageErreur = $"Impossible d'ouvrir le fichier Excel: {ex.Message}";
                        return result;
                    }
                }

                if (workbook == null)
                {
                    if (fileStream != null) fileStream.Dispose();
                    result.Succes = false;
                    result.MessageErreur = "Le fichier est verrouillé par un autre programme. Fermez-le et réessayez.";
                    return result;
                }

                using (workbook)
                {
                    if (workbook.Worksheets.Count == 0)
                    {
                        result.Succes = false;
                        result.MessageErreur = "Le fichier Excel ne contient aucune feuille.";
                        return result;
                    }

                    var worksheet = workbook.Worksheet(1);
                    result.Avertissements.Add($"DIAGNOSTIC: Analyse de la feuille: '{worksheet.Name}'");

                    if (!ValiderStructureCDC(worksheet, result))
                    {
                        result.MessageErreur += "\n\nDIAGNOSTIC DÉTAILLÉ:\n" + string.Join("\n", result.Avertissements);
                        return result;
                    }

                    result.Etudiants = LireDonneesClassique(worksheet, result);

                    if (result.Etudiants.Count > 0)
                    {
                        // Vérification post-lecture EF-02 : s'assurer que les moyennes ne sont pas toutes à 0.00/20
                        bool toutesZero = result.Etudiants.All(e => e.MoyenneGenerale == 0.00m);
                        if (toutesZero)
                        {
                            result.Succes = false;
                            result.MessageErreur = "⛔ Validation Gabarit Échouée (EF-02) : Les moyennes générales lues sont toutes égales à 0.00/20.\n\n" +
                                                   "Le format numérique des moyennes ou le nom de la colonne ne correspond pas au gabarit officiel. " +
                                                   "Veuillez vérifier que la colonne 'Moyenne Générale' contient des valeurs numériques valides (ex: 12.50 ou 12,50).";
                            return result;
                        }

                        result.Succes = true;
                        result.MessageSucces = $"{result.Etudiants.Count} étudiant(s) importé(s) avec succès (moyennes valides).";
                    }
                    else
                    {
                        result.Succes = false;
                        result.MessageErreur = "⛔ Validation Gabarit Échouée (EF-02) : Aucune donnée d'étudiant valide trouvée dans le fichier Excel.";
                        if (result.Erreurs.Any())
                        {
                            result.MessageErreur += "\n\nERREURS DÉTECTÉES:\n" + string.Join("\n", result.Erreurs);
                        }
                    }
                }
                
                if (fileStream != null) fileStream.Dispose();
                if (!string.IsNullOrEmpty(shadowCopyCreated) && File.Exists(shadowCopyCreated))
                {
                    try { File.Delete(shadowCopyCreated); } catch { }
                }
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de l'import Excel: {ex.Message}";
            }

            return result;
        }

        private bool ValiderStructureCDC(IXLWorksheet worksheet, ImportResult result)
        {
            try
            {
                if (worksheet.LastRowUsed() == null)
                {
                    result.MessageErreur = "⛔ Validation Gabarit (EF-02) : La feuille Excel est vide.";
                    return false;
                }

                int lastRow = worksheet.LastRowUsed().RowNumber();
                int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                result.Avertissements.Add($"DIAGNOSTIC: Feuille '{worksheet.Name}' -> {lastRow} ligne(s), {lastCol} colonne(s)");

                if (lastCol < 2)
                {
                    result.MessageErreur = $"⛔ Validation Gabarit (EF-02) : Nombre de colonnes insuffisant ({lastCol} trouvée(s), 2 minimum requises : Nom/Prénom et Moyenne).";
                    return false;
                }

                // Inspection des 5 premières lignes pour détecter la présence des colonnes indispensables selon EF-02
                var firstRows = new List<List<string>>();
                for (int r = 1; r <= Math.Min(5, lastRow); r++)
                {
                    var rowCells = new List<string>();
                    for (int c = 1; c <= lastCol; c++)
                    {
                        var cell = worksheet.Cell(r, c);
                        rowCells.Add(cell.IsEmpty() ? "" : cell.GetString().Trim());
                    }
                    firstRows.Add(rowCells);
                }

                ColumnIndices detectedMap = new ColumnIndices();
                for (int r = 0; r < firstRows.Count; r++)
                {
                    var map = DetectColumns(firstRows[r]);
                    if (map.Moyenne != -1) detectedMap.Moyenne = map.Moyenne;
                    if (map.Nom != -1) detectedMap.Nom = map.Nom;
                    if (map.Prenom != -1) detectedMap.Prenom = map.Prenom;
                    if (map.NomPrenom != -1) detectedMap.NomPrenom = map.NomPrenom;
                    if (map.Matricule != -1) detectedMap.Matricule = map.Matricule;
                    if (map.Classe != -1) detectedMap.Classe = map.Classe;
                    if (map.Decision != -1) detectedMap.Decision = map.Decision;
                    if (map.Mention != -1) detectedMap.Mention = map.Mention;
                }

                // Si Moyenne est toujours introuvable dans les en-têtes, scanner les colonnes numériques
                if (detectedMap.Moyenne == -1)
                {
                    detectedMap = FallbackColumnIndices(detectedMap, lastCol, firstRows, 1);
                }

                // Vérifier la présence des colonnes clés pour validation du gabarit imposé (EF-02)
                var colonnesManquantes = new List<string>();
                if (detectedMap.Nom == -1 && detectedMap.NomPrenom == -1)
                {
                    colonnesManquantes.Add("Nom & Prénom (ou Nom)");
                }
                if (detectedMap.Moyenne == -1)
                {
                    colonnesManquantes.Add("Moyenne Générale (ou Note)");
                }

                if (colonnesManquantes.Count > 0)
                {
                    result.MessageErreur = $"⛔ Gabarit Excel non conforme (EF-02) : Colonnes obligatoires introuvables : {string.Join(", ", colonnesManquantes)}.\n\nVeuillez respecter le modèle officiel imposé avec les colonnes : N°, Matricule, Nom, Prénom, Moyenne Générale, Décision, Mention.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                result.MessageErreur = $"Erreur lors de la validation du gabarit: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Modèle interne pour la cartographie dynamique des colonnes
        /// </summary>
        private class ColumnIndices
        {
            public int NumOrdre { get; set; } = -1;
            public int Nom { get; set; } = -1;
            public int Prenom { get; set; } = -1;
            public int NomPrenom { get; set; } = -1;
            public int Matricule { get; set; } = -1;
            public int Classe { get; set; } = -1;
            public int Moyenne { get; set; } = -1;
            public int Decision { get; set; } = -1;
            public int Mention { get; set; } = -1;
            public int Observation { get; set; } = -1;
        }

        /// <summary>
        /// Détecte les index de colonnes en fonction des en-têtes (insensible à la casse, aux accents et aux espaces)
        /// </summary>
        private ColumnIndices DetectColumns(List<string> headers)
        {
            var map = new ColumnIndices();

            for (int col = 0; col < headers.Count; col++)
            {
                string header = headers[col]?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(header)) continue;

                string normHeader = RemoveAccents(header.ToLowerInvariant())
                                    .Replace("_", "").Replace(" ", "").Replace("-", "").Replace("\"", "").Replace("'", "");

                if (normHeader.Contains("moyenne") || normHeader.Contains("mg") || normHeader == "note" || normHeader == "notegenerale" || normHeader == "notefinale" || normHeader == "moy" || normHeader == "avg" || normHeader == "average" || normHeader.Contains("moyennegenerale"))
                {
                    if (map.Moyenne == -1) map.Moyenne = col;
                }
                else if ((normHeader.Contains("nom") && normHeader.Contains("prenom")) || normHeader == "nomprenom" || normHeader == "etudiant" || normHeader == "student" || normHeader == "nomcomplet" || normHeader == "fullname")
                {
                    if (map.NomPrenom == -1) map.NomPrenom = col;
                }
                else if (normHeader == "nom" || normHeader == "nometudiant" || normHeader == "surname" || normHeader == "lastname" || normHeader.StartsWith("nom"))
                {
                    if (map.Nom == -1) map.Nom = col;
                }
                else if (normHeader == "prenom" || normHeader == "prenometudiant" || normHeader == "firstname" || normHeader.StartsWith("prenom"))
                {
                    if (map.Prenom == -1) map.Prenom = col;
                }
                else if (normHeader.Contains("matricule") || normHeader.Contains("cne") || normHeader.Contains("cin") || normHeader == "idetudiant" || normHeader == "id" || normHeader == "code" || normHeader.Contains("numetudiant"))
                {
                    if (map.Matricule == -1) map.Matricule = col;
                }
                else if (normHeader.Contains("classe") || normHeader.Contains("groupe") || normHeader.Contains("filiere") || normHeader.Contains("promo") || normHeader.Contains("section"))
                {
                    if (map.Classe == -1) map.Classe = col;
                }
                else if (normHeader.Contains("decision") || normHeader.Contains("resultat") || normHeader.Contains("avis") || normHeader.Contains("statut"))
                {
                    if (map.Decision == -1) map.Decision = col;
                }
                else if (normHeader.Contains("mention"))
                {
                    if (map.Mention == -1) map.Mention = col;
                }
                else if (normHeader.Contains("observation") || normHeader.Contains("remarque") || normHeader == "obs")
                {
                    if (map.Observation == -1) map.Observation = col;
                }
                else if (normHeader == "n" || normHeader == "num" || normHeader == "numero" || normHeader == "ordre" || normHeader == "numeroordre" || normHeader == "no" || normHeader == "seq")
                {
                    if (map.NumOrdre == -1) map.NumOrdre = col;
                }
            }

            return map;
        }

        private ColumnIndices FallbackColumnIndices(ColumnIndices map, int totalCols, List<List<string>> dataRows, int startRow)
        {
            if (map == null) map = new ColumnIndices();

            if (totalCols >= 9)
            {
                if (map.NumOrdre == -1) map.NumOrdre = 0;
                if (map.Nom == -1 && map.NomPrenom == -1) map.Nom = 1;
                if (map.Prenom == -1 && map.NomPrenom == -1) map.Prenom = 2;
                if (map.Matricule == -1) map.Matricule = 3;
                if (map.Classe == -1) map.Classe = 4;
                if (map.Moyenne == -1) map.Moyenne = 8; // Colonne I (9ème)
            }
            else if (totalCols >= 8)
            {
                if (map.NumOrdre == -1) map.NumOrdre = 0;
                if (map.NomPrenom == -1 && map.Nom == -1) map.NomPrenom = 1;
                if (map.Matricule == -1) map.Matricule = 2;
                if (map.Classe == -1) map.Classe = 3;
                if (map.Moyenne == -1) map.Moyenne = 4; // Colonne E (5ème)
                if (map.Decision == -1) map.Decision = 5;
                if (map.Mention == -1) map.Mention = 6;
                if (map.Observation == -1) map.Observation = 7;
            }
            else if (totalCols >= 5)
            {
                if (map.NumOrdre == -1) map.NumOrdre = 0;
                if (map.NomPrenom == -1 && map.Nom == -1) map.NomPrenom = 1;
                if (map.Matricule == -1) map.Matricule = 2;
                if (map.Classe == -1) map.Classe = 3;
                if (map.Moyenne == -1) map.Moyenne = 4; // Colonne E (5ème)
            }
            else if (totalCols >= 2)
            {
                if (map.NomPrenom == -1 && map.Nom == -1) map.NomPrenom = 0;
                if (map.Moyenne == -1) map.Moyenne = 1;
            }

            // Si la moyenne n'est toujours pas trouvée, scanner les 5 premières lignes de données pour repérer la 1ère colonne numérique <= 20
            if (map.Moyenne == -1 && dataRows != null && dataRows.Count > startRow)
            {
                for (int c = 0; c < totalCols; c++)
                {
                    if (c == map.NumOrdre || c == map.Nom || c == map.Prenom || c == map.NomPrenom || c == map.Matricule || c == map.Classe)
                        continue;

                    int validDecimals = 0;
                    for (int r = startRow; r < Math.Min(startRow + 5, dataRows.Count); r++)
                    {
                        string val = dataRows[r].ElementAtOrDefault(c) ?? "";
                        decimal d = ParseDecimal(val);
                        if (d > 0 && d <= 20) validDecimals++;
                    }

                    if (validDecimals >= 1)
                    {
                        map.Moyenne = c;
                        break;
                    }
                }
            }

            return map;
        }

        private static string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private string GetRowValue(List<string> row, int colIndex)
        {
            if (colIndex >= 0 && colIndex < row.Count)
            {
                return row[colIndex]?.Trim() ?? "";
            }
            return "";
        }

        /// <summary>
        /// Lire les données Excel de manière dynamique et ultra-fiable
        /// </summary>
        private List<Etudiant> LireDonneesClassique(IXLWorksheet worksheet, ImportResult result)
        {
            var etudiants = new List<Etudiant>();
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            result.Avertissements.Add($"LECTURE EXCEL: {lastRow} lignes x {lastCol} colonnes");

            var allRows = new List<List<string>>();
            for (int r = 1; r <= lastRow; r++)
            {
                var rowCells = new List<string>();
                for (int c = 1; c <= lastCol; c++)
                {
                    var cell = worksheet.Cell(r, c);
                    string val = "";
                    if (!cell.IsEmpty())
                    {
                        if (cell.DataType == XLDataType.Number)
                        {
                            val = cell.GetDouble().ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            val = cell.GetString().Trim();
                        }
                    }
                    rowCells.Add(val);
                }
                allRows.Add(rowCells);
            }

            int headerRowIndex = -1;
            ColumnIndices colMap = null;

            for (int r = 0; r < Math.Min(5, allRows.Count); r++)
            {
                var map = DetectColumns(allRows[r]);
                if (map.Moyenne != -1 || map.Nom != -1 || map.NomPrenom != -1)
                {
                    colMap = map;
                    headerRowIndex = r;
                    result.Avertissements.Add($"DÉTECTION EN-TÊTE: Ligne {r + 1}");
                    break;
                }
            }

            int ligneDepartIndex = headerRowIndex >= 0 ? headerRowIndex + 1 : 0;
            colMap = FallbackColumnIndices(colMap, lastCol, allRows, ligneDepartIndex);

            string colMoyenneName = colMap.Moyenne >= 0 ? GetColumnName(colMap.Moyenne + 1) : "Inconnue";
            result.Avertissements.Add($"CARTOGRAPHIE: Moyenne={colMoyenneName} (Col {colMap.Moyenne + 1}), Nom={colMap.Nom + 1}, Prénom={colMap.Prenom + 1}, NomPrenom={colMap.NomPrenom + 1}, Matricule={colMap.Matricule + 1}, Classe={colMap.Classe + 1}");

            int etudiantsValides = 0;
            int lignesIgnorees = 0;

            for (int rIndex = ligneDepartIndex; rIndex < allRows.Count; rIndex++)
            {
                int rowNum = rIndex + 1;
                try
                {
                    var rowCells = allRows[rIndex];
                    if (rowCells.All(string.IsNullOrWhiteSpace))
                    {
                        lignesIgnorees++;
                        continue;
                    }

                    string valMoyenne = GetRowValue(rowCells, colMap.Moyenne);
                    decimal mg = ParseDecimal(valMoyenne);
                    string moyenneOriginale = valMoyenne.Trim();

                    string nomEtudiant = GetRowValue(rowCells, colMap.Nom);
                    string prenomEtudiant = GetRowValue(rowCells, colMap.Prenom);
                    string nomPrenom = GetRowValue(rowCells, colMap.NomPrenom);

                    if (string.IsNullOrWhiteSpace(nomPrenom))
                    {
                        nomPrenom = $"{nomEtudiant} {prenomEtudiant}".Trim();
                    }
                    if (string.IsNullOrWhiteSpace(nomEtudiant) && !string.IsNullOrWhiteSpace(nomPrenom))
                    {
                        var parts = nomPrenom.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        nomEtudiant = parts.FirstOrDefault() ?? "";
                        prenomEtudiant = string.Join(" ", parts.Skip(1));
                    }

                    string matricule = GetRowValue(rowCells, colMap.Matricule);
                    string classeGroupe = GetRowValue(rowCells, colMap.Classe);
                    string decision = GetRowValue(rowCells, colMap.Decision);
                    string mention = GetRowValue(rowCells, colMap.Mention);
                    string observation = GetRowValue(rowCells, colMap.Observation);
                    int numOrdre = ParseInt(GetRowValue(rowCells, colMap.NumOrdre));

                    if (string.IsNullOrWhiteSpace(nomPrenom) && mg <= 0)
                    {
                        lignesIgnorees++;
                        continue;
                    }

                    if (numOrdre <= 0) numOrdre = etudiantsValides + 1;
                    if (string.IsNullOrWhiteSpace(matricule)) matricule = $"TEMP_{numOrdre:000}";
                    if (string.IsNullOrWhiteSpace(classeGroupe)) classeGroupe = "Non spécifiée";
                    if (mg > 20) mg = 20;

                    var etudiant = new Etudiant
                    {
                        Id = numOrdre,
                        NumeroOrdre = numOrdre,
                        Nom = nomEtudiant,
                        Prenom = prenomEtudiant,
                        NomPrenom = nomPrenom,
                        Matricule = matricule,
                        ClasseGroupe = classeGroupe,
                        Filiere = classeGroupe,
                        AnneeUniversitaire = DateTime.Now.Year.ToString(),
                        MoyenneGenerale = mg,
                        MoyenneOriginale = moyenneOriginale,
                        Decision = decision,
                        Mention = mention,
                        Observation = observation,
                        Validation = "",
                        EctsValides = mg >= 10.0m ? 30 : 0,
                        EctsTotal = 30,
                        ModulesGrades = new List<ModuleGrades>()
                    };

                    if (string.IsNullOrWhiteSpace(etudiant.Decision))
                    {
                        etudiant.CalculerDecisionEtMention();
                    }

                    result.Avertissements.Add($"LIGNE {rowNum}: {nomPrenom} | Moyenne brute:'{valMoyenne}' -> Moyenne lue={mg} | Décision:{etudiant.Decision}");
                    etudiants.Add(etudiant);
                    etudiantsValides++;
                }
                catch (Exception ex)
                {
                    result.Erreurs.Add($"LIGNE {rowNum}: EXCEPTION {ex.GetType().Name}: {ex.Message}");
                    lignesIgnorees++;
                }
            }

            result.Avertissements.Add($"RÉSULTAT FINAL: {etudiantsValides} étudiant(s) importé(s)");

            if (etudiants.Any())
            {
                var sorted = etudiants.OrderByDescending(e => e.MoyenneGenerale).ToList();
                for (int i = 0; i < sorted.Count; i++) sorted[i].Rang = i + 1;
            }

            return etudiants;
        }

        /// <summary>
        /// Importer les données d'un fichier CSV (Format CSV complet)
        /// </summary>
        private ImportResult ImporterDonneesCsv(string cheminFichier)
        {
            var result = new ImportResult();
            var etudiants = new List<Etudiant>();

            try
            {
                if (!File.Exists(cheminFichier))
                {
                    result.Succes = false;
                    result.MessageErreur = "Le fichier CSV spécifié n'existe pas.";
                    return result;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(cheminFichier, Encoding.UTF8);
                }
                catch
                {
                    lines = File.ReadAllLines(cheminFichier, Encoding.Default);
                }

                if (lines.Length == 0)
                {
                    result.Succes = false;
                    result.MessageErreur = "Le fichier CSV est vide.";
                    return result;
                }

                // Détection automatique du séparateur (';', ',', ou '\t')
                char separator = ';';
                string firstNonEmpty = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
                int countSemicolon = firstNonEmpty.Count(c => c == ';');
                int countComma = firstNonEmpty.Count(c => c == ',');
                int countTab = firstNonEmpty.Count(c => c == '\t');

                if (countComma > countSemicolon && countComma >= countTab)
                {
                    separator = ',';
                }
                else if (countTab > countSemicolon && countTab > countComma)
                {
                    separator = '\t';
                }

                result.Avertissements.Add($"IMPORT CSV: Détecté séparateur '{separator}'");

                var parsedRows = new List<List<string>>();
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var fields = ParseCsvLine(line, separator);
                    parsedRows.Add(fields);
                }

                if (parsedRows.Count == 0)
                {
                    result.Succes = false;
                    result.MessageErreur = "Aucune ligne valide trouvée dans le CSV.";
                    return result;
                }

                int headerRowIndex = -1;
                ColumnIndices colMap = null;

                for (int i = 0; i < Math.Min(5, parsedRows.Count); i++)
                {
                    var headers = parsedRows[i];
                    var map = DetectColumns(headers);
                    if (map.Moyenne != -1 || map.Nom != -1 || map.NomPrenom != -1)
                    {
                        colMap = map;
                        headerRowIndex = i;
                        result.Avertissements.Add($"CSV EN-TÊTE: Ligne {i + 1}");
                        break;
                    }
                }

                int startRow = headerRowIndex >= 0 ? headerRowIndex + 1 : 0;
                int maxCols = parsedRows.Max(r => r.Count);
                colMap = FallbackColumnIndices(colMap, maxCols, parsedRows, startRow);

                int etudiantsValides = 0;
                for (int i = startRow; i < parsedRows.Count; i++)
                {
                    var row = parsedRows[i];
                    if (row.All(string.IsNullOrWhiteSpace)) continue;

                    string valMoyenne = GetRowValue(row, colMap.Moyenne);
                    decimal mg = ParseDecimal(valMoyenne);
                    string moyenneOriginale = valMoyenne.Trim();

                    string nom = GetRowValue(row, colMap.Nom);
                    string prenom = GetRowValue(row, colMap.Prenom);
                    string nomPrenom = GetRowValue(row, colMap.NomPrenom);

                    if (string.IsNullOrWhiteSpace(nomPrenom))
                    {
                        nomPrenom = $"{nom} {prenom}".Trim();
                    }
                    if (string.IsNullOrWhiteSpace(nom) && !string.IsNullOrWhiteSpace(nomPrenom))
                    {
                        var parts = nomPrenom.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        nom = parts.FirstOrDefault() ?? "";
                        prenom = string.Join(" ", parts.Skip(1));
                    }

                    string matricule = GetRowValue(row, colMap.Matricule);
                    string classeGroupe = GetRowValue(row, colMap.Classe);
                    string decision = GetRowValue(row, colMap.Decision);
                    string mention = GetRowValue(row, colMap.Mention);
                    string observation = GetRowValue(row, colMap.Observation);
                    int numOrdre = ParseInt(GetRowValue(row, colMap.NumOrdre));

                    if (string.IsNullOrWhiteSpace(nomPrenom) && mg <= 0) continue;

                    if (numOrdre <= 0) numOrdre = etudiantsValides + 1;
                    if (string.IsNullOrWhiteSpace(matricule)) matricule = $"TEMP_{numOrdre:000}";
                    if (string.IsNullOrWhiteSpace(classeGroupe)) classeGroupe = "Non spécifiée";
                    if (mg > 20) mg = 20;

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
                        AnneeUniversitaire = DateTime.Now.Year.ToString(),
                        MoyenneGenerale = mg,
                        MoyenneOriginale = moyenneOriginale,
                        Decision = decision,
                        Mention = mention,
                        Observation = observation,
                        Validation = "",
                        EctsValides = mg >= 10.0m ? 30 : 0,
                        EctsTotal = 30,
                        ModulesGrades = new List<ModuleGrades>()
                    };

                    if (string.IsNullOrWhiteSpace(etudiant.Decision))
                    {
                        etudiant.CalculerDecisionEtMention();
                    }

                    etudiants.Add(etudiant);
                    etudiantsValides++;
                }

                if (etudiants.Any())
                {
                    var sorted = etudiants.OrderByDescending(e => e.MoyenneGenerale).ToList();
                    for (int i = 0; i < sorted.Count; i++) sorted[i].Rang = i + 1;

                    result.Succes = true;
                    result.Etudiants = etudiants;
                    result.MessageSucces = $"{etudiantsValides} étudiant(s) importé(s) avec succès depuis le fichier CSV.";
                }
                else
                {
                    result.Succes = false;
                    result.MessageErreur = "Aucune donnée d'étudiant n'a pu être extraite du fichier CSV.";
                }
            }
            catch (Exception ex)
            {
                result.Succes = false;
                result.MessageErreur = $"Erreur lors de l'import CSV: {ex.Message}";
            }

            return result;
        }

        private List<string> ParseCsvLine(string line, char separator)
        {
            var fields = new List<string>();
            if (string.IsNullOrEmpty(line)) return fields;

            bool inQuotes = false;
            var field = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == separator && !inQuotes)
                {
                    fields.Add(field.ToString().Trim());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }
            fields.Add(field.ToString().Trim());
            return fields;
        }

        private int ParseInt(string valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return 0;
            if (int.TryParse(valeur.Trim(), out int result)) return result;
            return 0;
        }

        /// <summary>
        /// Méthode ultra-robuste de parsing des décimaux (Moyenne Générale).
        /// Gère la virgule française ("12,43"), le point anglais ("12.43"), les espaces,
        /// et les notations avec sur 20 ("12,43 / 20").
        /// Evite l'erreur de conversion de la virgule en séparateur de milliers.
        /// </summary>
        private decimal ParseDecimal(string valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) 
            {
                return 0m;
            }
            
            string valeurOriginale = valeur;
            valeur = valeur.Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(valeur)) return 0m;

            // 1. Si la valeur contient une virgule, prioriser la culture française (où ',' est la séparateur décimal)
            if (valeur.Contains(","))
            {
                var frCulture = CultureInfo.GetCultureInfo("fr-FR");
                if (decimal.TryParse(valeur, NumberStyles.Number, frCulture, out decimal resFr))
                {
                    Console.WriteLine($"[PARSE] SUCCÈS FR (virgule): '{valeurOriginale}' -> {resFr}");
                    return resFr;
                }
            }
            
            // 2. Essayer InvariantCulture avec NumberStyles.Number (sans AllowThousands pour éviter 12,43 -> 1243!)
            if (decimal.TryParse(valeur, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resultExact))
            {
                Console.WriteLine($"[PARSE] SUCCÈS INVARIANT: '{valeurOriginale}' -> {resultExact}");
                return resultExact;
            }
            
            // 3. Essayer avec remplacement virgule -> point (avec NumberStyles.Number)
            string valeurAvecPoint = valeur.Replace(',', '.');
            if (decimal.TryParse(valeurAvecPoint, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resultPoint))
            {
                Console.WriteLine($"[PARSE] SUCCÈS CONVERT_POINT: '{valeurOriginale}' -> {resultPoint}");
                return resultPoint;
            }

            // 4. Extraction par regex au cas où la valeur est formatée type "12,43 / 20" ou "12.43/20"
            var match = Regex.Match(valeur, @"[0-9]+([.,][0-9]+)?");
            if (match.Success)
            {
                string extract = match.Value.Replace(',', '.');
                if (decimal.TryParse(extract, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resultRegex))
                {
                    Console.WriteLine($"[PARSE] SUCCÈS REGEX: '{valeurOriginale}' -> {resultRegex}");
                    return resultRegex;
                }
            }

            // 5. Cas ultra-spécifiques condensés (ex: "15A5" -> 15.5)
            if (valeur.Length == 4 && valeur[1].ToString().ToUpper() == "A" && 
                char.IsDigit(valeur[0]) && char.IsDigit(valeur[2]) && char.IsDigit(valeur[3]))
            {
                string converti = $"{valeur[0]}.{valeur.Substring(2, 2)}";
                if (decimal.TryParse(converti, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal resultA))
                {
                    return resultA;
                }
            }
            
            Console.WriteLine($"[PARSE] ÉCHEC DE PARSING: Impossible de lire '{valeurOriginale}'");
            return 0m;
        }

        private bool IsDigitsOnly(string valeur)
        {
            return !string.IsNullOrEmpty(valeur) && valeur.All(char.IsDigit);
        }

        private bool IsNumeric(string valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return false;
            valeur = valeur.Trim().Replace(',', '.');
            return decimal.TryParse(valeur, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
        }

        private void LibererVerrouFichier(string cheminFichier)
        {
            try
            {
                string repertoire = Path.GetDirectoryName(cheminFichier);
                string nomFichier = Path.GetFileName(cheminFichier);
                string fichierTemp = Path.Combine(repertoire, $"~${nomFichier}");

                if (File.Exists(fichierTemp))
                {
                    try
                    {
                        File.Delete(fichierTemp);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private string GetColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                columnNumber--;
                columnName = (char)('A' + (columnNumber % 26)) + columnName;
                columnNumber /= 26;
            }
            return columnName;
        }
    }

    public class ImportResult
    {
        public bool Succes { get; set; }
        public string MessageSucces { get; set; }
        public string MessageErreur { get; set; }
        public List<string> Avertissements { get; set; }
        public List<string> Erreurs { get; set; }  
        public List<Etudiant> Etudiants { get; set; }
        public DeliberationStatistics Statistics { get; set; }

        public bool Success
        {
            get => Succes;
            set => Succes = value;
        }

        private string _message;
        public string Message
        {
            get => _message ?? (Succes ? MessageSucces : MessageErreur);
            set => _message = value;
        }

        public ImportResult()
        {
            Avertissements = new List<string>();
            Erreurs = new List<string>();
            Etudiants = new List<Etudiant>();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour créer des fichiers Excel d'exemple conformes au CDC Annexe A
    /// </summary>
    public class ExempleExcelService
    {
        /// <summary>
        /// Créer un fichier Excel d'exemple avec la structure CDC Annexe A
        /// </summary>
        public bool CreerFichierExemple(string cheminSortie, bool avecDonnees = true)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Délibération");

                    // En-têtes conformes CDC Annexe A
                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom et Prénom";
                    worksheet.Cell(1, 3).Value = "Matricule / CNE";
                    worksheet.Cell(1, 4).Value = "Classe / Groupe";
                    worksheet.Cell(1, 5).Value = "Moyenne générale";
                    worksheet.Cell(1, 6).Value = "Décision";
                    worksheet.Cell(1, 7).Value = "Mention";
                    worksheet.Cell(1, 8).Value = "Observation";

                    // Formatage en-têtes
                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366");
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                    headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Ajouter des données d'exemple si demandé
                    if (avecDonnees)
                    {
                        AjouterDonneesExemple(worksheet);
                    }
                    else
                    {
                        // Ajouter juste une ligne vide pour montrer le format
                        worksheet.Cell(2, 1).Value = "1";
                        worksheet.Cell(2, 2).Value = "NOM Prénom";
                        worksheet.Cell(2, 3).Value = "20231045";
                        worksheet.Cell(2, 4).Value = "L3-INFO-A";
                        worksheet.Cell(2, 5).Value = "12.75";
                        worksheet.Cell(2, 6).Value = "Admis";
                        worksheet.Cell(2, 7).Value = "Assez Bien";
                        worksheet.Cell(2, 8).Value = "";

                        // Gris pour indiquer que c'est un exemple
                        var exampleRange = worksheet.Range(2, 1, 2, 8);
                        exampleRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                        exampleRange.Style.Font.Italic = true;
                    }

                    // Ajustement automatique des colonnes
                    worksheet.Columns().AdjustToContents();

                    // Largeurs spécifiques pour une meilleure lisibilité
                    worksheet.Column(1).Width = 5;   // N°
                    worksheet.Column(2).Width = 25;  // Nom et Prénom
                    worksheet.Column(3).Width = 15;  // Matricule
                    worksheet.Column(4).Width = 15;  // Classe
                    worksheet.Column(5).Width = 12;  // Moyenne
                    worksheet.Column(6).Width = 12;  // Décision
                    worksheet.Column(7).Width = 15;  // Mention
                    worksheet.Column(8).Width = 30;  // Observation

                    // Ajouter une feuille d'instructions
                    AjouterFeuilleInstructions(workbook);

                    // Sauvegarder
                    workbook.SaveAs(cheminSortie);
                    
                    Console.WriteLine($"[EXEMPLE] Fichier Excel CDC créé: {cheminSortie}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXEMPLE] Erreur création fichier Excel: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ajouter des données d'exemple réalistes
        /// </summary>
        private void AjouterDonneesExemple(IXLWorksheet worksheet)
        {
            var etudiants = new List<(string nom, string matricule, string classe, decimal moyenne, string decision, string mention)>
            {
                ("ALAMI Youssef", "20231001", "L3-INFO-A", 16.75m, "Admis", "Très Bien"),
                ("BENJELLOUN Fatima", "20231002", "L3-INFO-A", 14.25m, "Admis", "Bien"),
                ("CHAOUI Mohamed", "20231003", "L3-INFO-A", 12.80m, "Admis", "Assez Bien"),
                ("DAHBI Aicha", "20231004", "L3-INFO-A", 10.50m, "Admis", "Passable"),
                ("EL AMRANI Omar", "20231005", "L3-INFO-A", 9.25m, "Ajourné", "Session de rattrapage"),
                ("FASSI Laila", "20231006", "L3-INFO-A", 15.60m, "Admis", "Bien"),
                ("GHALI Khalid", "20231007", "L3-INFO-A", 11.90m, "Admis", "Assez Bien"),
                ("HABIB Nadia", "20231008", "L3-INFO-A", 7.50m, "Exclu", "—"),
                ("IDRISSI Hassan", "20231009", "L3-INFO-A", 13.75m, "Admis", "Assez Bien"),
                ("JAMAL Zineb", "20231010", "L3-INFO-A", 17.25m, "Admis", "Très Bien")
            };

            for (int i = 0; i < etudiants.Count; i++)
            {
                int row = i + 2; // Ligne 2+ (ligne 1 = en-têtes)
                var etudiant = etudiants[i];

                worksheet.Cell(row, 1).Value = i + 1; // N°
                worksheet.Cell(row, 2).Value = etudiant.nom;
                worksheet.Cell(row, 3).Value = etudiant.matricule;
                worksheet.Cell(row, 4).Value = etudiant.classe;
                worksheet.Cell(row, 5).Value = etudiant.moyenne;
                worksheet.Cell(row, 6).Value = etudiant.decision;
                worksheet.Cell(row, 7).Value = etudiant.mention;
                worksheet.Cell(row, 8).Value = ""; // Observation vide par défaut

                // Formatage conditionnel selon la décision
                var decisionCell = worksheet.Cell(row, 6);
                var mentionCell = worksheet.Cell(row, 7);
                
                switch (etudiant.decision)
                {
                    case "Admis":
                        decisionCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        mentionCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        break;
                    case "Ajourné":
                        decisionCell.Style.Fill.BackgroundColor = XLColor.Orange;
                        mentionCell.Style.Fill.BackgroundColor = XLColor.Orange;
                        break;
                    case "Exclu":
                        decisionCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        mentionCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                        break;
                }

                // Bordures pour toutes les cellules de données
                var dataRange = worksheet.Range(row, 1, row, 8);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
        }

        /// <summary>
        /// Ajouter une feuille d'instructions CDC
        /// </summary>
        private void AjouterFeuilleInstructions(XLWorkbook workbook)
        {
            var instructionsSheet = workbook.Worksheets.Add("Instructions CDC");

            var instructions = new List<string>
            {
                "INSTRUCTIONS - FORMAT EXCEL CONFORME AU CAHIER DES CHARGES",
                "",
                "STRUCTURE OBLIGATOIRE (Annexe A):",
                "• Colonne A : N° (numéro d'ordre de l'étudiant)",
                "• Colonne B : Nom et Prénom (identité complète)",
                "• Colonne C : Matricule / CNE (identifiant unique)",
                "• Colonne D : Classe / Groupe (obligatoire si feuille unique)",
                "• Colonne E : Moyenne générale (numérique, 0-20)",
                "• Colonne F : Décision (Admis / Ajourné / Exclu)",
                "• Colonne G : Mention (Très Bien / Bien / Assez Bien / Passable / Session de rattrapage / —)",
                "• Colonne H : Observation (texte libre optionnel)",
                "",
                "RÈGLES DE VALIDATION:",
                "• Pas de ligne vide au milieu des données",
                "• Pas de cellule fusionnée dans la zone de données", 
                "• La colonne Décision doit utiliser exactement les libellés convenus",
                "• La colonne Moyenne générale doit être numérique (pas de texte)",
                "",
                "DEUX MODES DE FONCTIONNEMENT:",
                "• Mode 1: Décision déjà fournie - Le système recopie les décisions du fichier Excel",
                "• Mode 2: Calcul automatique - Le système calcule les décisions selon les seuils:",
                "  - MG ≥ 16: Admis (Très Bien)",
                "  - 14 ≤ MG < 16: Admis (Bien)", 
                "  - 12 ≤ MG < 14: Admis (Assez Bien)",
                "  - 10 ≤ MG < 12: Admis (Passable)",
                "  - 8 ≤ MG < 10: Ajourné (Session de rattrapage)",
                "  - MG < 8: Exclu",
                "",
                "CONSEILS D'UTILISATION:",
                "• Utilisez la feuille 'Délibération' comme modèle",
                "• Remplacez les données d'exemple par vos vraies données",
                "• Respectez exactement la structure des colonnes A-H",
                "• Vérifiez que les moyennes sont bien au format numérique",
                "• Sauvegardez au format .xlsx avant import"
            };

            for (int i = 0; i < instructions.Count; i++)
            {
                instructionsSheet.Cell(i + 1, 1).Value = instructions[i];
                
                if (i == 0) // Titre
                {
                    instructionsSheet.Cell(i + 1, 1).Style.Font.Bold = true;
                    instructionsSheet.Cell(i + 1, 1).Style.Font.FontSize = 14;
                    instructionsSheet.Cell(i + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#003366");
                }
                else if (instructions[i].EndsWith(":")) // Sous-titres
                {
                    instructionsSheet.Cell(i + 1, 1).Style.Font.Bold = true;
                    instructionsSheet.Cell(i + 1, 1).Style.Font.FontColor = XLColor.FromHtml("#666666");
                }
            }

            instructionsSheet.Column(1).Width = 80;
            instructionsSheet.Column(1).Style.Alignment.WrapText = true;
        }

        /// <summary>
        /// Créer un gabarit Excel vide pour un type de classe spécifique
        /// </summary>
        public bool CreerGabaritClasse(string cheminSortie, string nomClasse, int nombreEtudiants = 30)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add($"Délibération {nomClasse}");

                    // En-têtes
                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom et Prénom";
                    worksheet.Cell(1, 3).Value = "Matricule / CNE";
                    worksheet.Cell(1, 4).Value = "Classe / Groupe";
                    worksheet.Cell(1, 5).Value = "Moyenne générale";
                    worksheet.Cell(1, 6).Value = "Décision";
                    worksheet.Cell(1, 7).Value = "Mention";
                    worksheet.Cell(1, 8).Value = "Observation";

                    // Formatage en-têtes
                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#003366");
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Pré-remplir les colonnes fixes
                    for (int i = 1; i <= nombreEtudiants; i++)
                    {
                        worksheet.Cell(i + 1, 1).Value = i; // N°
                        worksheet.Cell(i + 1, 4).Value = nomClasse; // Classe
                    }

                    // Formatage conditionnel pour les colonnes obligatoires
                    var dataRange = worksheet.Range(2, 1, nombreEtudiants + 1, 8);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

                    // Mise en forme des colonnes
                    worksheet.Column(1).Width = 5;
                    worksheet.Column(2).Width = 25;
                    worksheet.Column(3).Width = 15;
                    worksheet.Column(4).Width = 15;
                    worksheet.Column(5).Width = 12;
                    worksheet.Column(6).Width = 12;
                    worksheet.Column(7).Width = 15;
                    worksheet.Column(8).Width = 30;

                    workbook.SaveAs(cheminSortie);
                    
                    Console.WriteLine($"[EXEMPLE] Gabarit Excel créé pour {nomClasse}: {cheminSortie}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXEMPLE] Erreur création gabarit: {ex.Message}");
                return false;
            }
        }
    }
}
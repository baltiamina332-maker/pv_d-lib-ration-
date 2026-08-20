using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour exporter les décisions d'admission en fichier Excel
    /// </summary>
    public class ExcelDecisionExportService
    {
        public ExcelDecisionExportService()
        {
        }

        /// <summary>
        /// Exporter les étudiants avec décisions en Excel
        /// </summary>
        public bool ExporterDecisions(List<Etudiant> etudiants, string dossierSortie, string nomFichier = "")
        {
            try
            {
                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  EXPORT EXCEL DÉCISIONS D'ADMISSION");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");

                // Déterminer le nom du fichier
                if (string.IsNullOrEmpty(nomFichier))
                {
                    string dateActuelle = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                    nomFichier = $"Decisions_Admission_{dateActuelle}.xlsx";
                }

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(dossierSortie))
                {
                    Directory.CreateDirectory(dossierSortie);
                    Console.WriteLine($"[EXPORT] Dossier créé: {dossierSortie}");
                }

                // Chemin complet du fichier
                string cheminComplet = Path.Combine(dossierSortie, nomFichier);

                // Créer le classeur Excel
                using (var workbook = new XLWorkbook())
                {
                    // === Feuille 1: Données détaillées ===
                    var worksheet = workbook.Worksheets.Add("Décisions");

                    // En-têtes
                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom";
                    worksheet.Cell(1, 3).Value = "Prénom";
                    worksheet.Cell(1, 4).Value = "Matricule";
                    worksheet.Cell(1, 5).Value = "Classe/Groupe";
                    worksheet.Cell(1, 6).Value = "Statut";
                    worksheet.Cell(1, 7).Value = "Moyenne Générale";
                    worksheet.Cell(1, 8).Value = "Décision";
                    worksheet.Cell(1, 9).Value = "Date";

                    // Style de l'en-tête
                    var headerRange = worksheet.Range(1, 1, 1, 9);
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(139, 58, 58); // Bordeaux
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // Remplir les données
                    int row = 2;
                    foreach (var etudiant in etudiants)
                    {
                        worksheet.Cell(row, 1).Value = row - 1;
                        worksheet.Cell(row, 2).Value = etudiant.Nom ?? "";
                        worksheet.Cell(row, 3).Value = etudiant.Prenom ?? "";
                        worksheet.Cell(row, 4).Value = etudiant.Matricule ?? "";
                        worksheet.Cell(row, 5).Value = etudiant.ClasseGroupe ?? "";
                        worksheet.Cell(row, 6).Value = etudiant.Statut ?? "";
                        worksheet.Cell(row, 7).Value = etudiant.MoyenneGenerale;
                        worksheet.Cell(row, 8).Value = etudiant.Decision ?? "";
                        worksheet.Cell(row, 9).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                        // Formater les cellules
                        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.00";
                        worksheet.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Colorer la colonne Décision
                        var cellDecision = worksheet.Cell(row, 8);
                        if (etudiant.Decision != null && etudiant.Decision.StartsWith("Admis"))
                        {
                            cellDecision.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            cellDecision.Style.Font.Bold = true;
                        }
                        else if (etudiant.Decision == "Conseil d'École")
                        {
                            cellDecision.Style.Fill.BackgroundColor = XLColor.Yellow;
                            cellDecision.Style.Font.Bold = true;
                        }
                        else if (etudiant.Decision != null && etudiant.Decision.Contains("Redouble/Exclu"))
                        {
                            cellDecision.Style.Fill.BackgroundColor = XLColor.Red;
                            cellDecision.Style.Font.Bold = true;
                        }

                        cellDecision.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        row++;
                    }

                    // Ajuster les largeurs de colonnes
                    worksheet.Columns().AdjustToContents();
                    worksheet.Column(1).Width = 5;
                    worksheet.Column(2).Width = 15;
                    worksheet.Column(3).Width = 15;
                    worksheet.Column(4).Width = 12;
                    worksheet.Column(5).Width = 15;
                    worksheet.Column(6).Width = 12;
                    worksheet.Column(7).Width = 18;
                    worksheet.Column(8).Width = 15;
                    worksheet.Column(9).Width = 20;

                    // Ajouter des filtres
                    worksheet.Range(1, 1, row - 1, 9).CreateTable("TableDecisions");

                    // === Feuille 2: Statistiques ===
                    var statsSheet = workbook.Worksheets.Add("Statistiques");
                    
                    AjouterStatistiques(statsSheet, etudiants);

                    // === Feuille 3: Résumé ===
                    var resumeSheet = workbook.Worksheets.Add("Résumé");
                    
                    AjouterResume(resumeSheet, etudiants);

                    // Sauvegarder le fichier
                    workbook.SaveAs(cheminComplet);

                    Console.WriteLine($"[EXPORT] ✓ Fichier créé: {cheminComplet}");
                    Console.WriteLine($"[EXPORT] Nombre d'étudiants: {etudiants.Count}");

                    FileInfo fileInfo = new FileInfo(cheminComplet);
                    Console.WriteLine($"[EXPORT] Taille du fichier: {fileInfo.Length / 1024} KB");

                    Console.WriteLine("\n════════════════════════════════════════════════════════════");
                    Console.WriteLine("  EXPORT RÉUSSI");
                    Console.WriteLine("════════════════════════════════════════════════════════════\n");

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXPORT] ❌ Erreur lors de l'export: {ex.Message}");
                Console.WriteLine($"[EXPORT] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Ajouter la feuille Statistiques
        /// </summary>
        private void AjouterStatistiques(IXLWorksheet worksheet, List<Etudiant> etudiants)
        {
            try
            {
                int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis"));
                int rattrapage = etudiants.Count(e => e.Decision == "Conseil d'École");
                int refuse = etudiants.Count(e => e.Decision != null && e.Decision.Contains("Redouble/Exclu"));
                int total = etudiants.Count;

                // Titre
                worksheet.Cell(1, 1).Value = "STATISTIQUES DES DÉCISIONS";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;

                // En-têtes
                worksheet.Cell(3, 1).Value = "Décision";
                worksheet.Cell(3, 2).Value = "Nombre";
                worksheet.Cell(3, 3).Value = "Pourcentage";

                // Style en-têtes
                var headerRange = worksheet.Range(3, 1, 3, 3);
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(139, 58, 58);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.White;

                // Données
                int row = 4;

                // Admis
                worksheet.Cell(row, 1).Value = "Admis";
                worksheet.Cell(row, 2).Value = admis;
                worksheet.Cell(row, 3).Value = total > 0 ? (decimal)admis / total * 100 : 0;
                worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00\"%\"";
                row++;

                // Rattrapage -> Conseil d'École
                worksheet.Cell(row, 1).Value = "Conseil d'École";
                worksheet.Cell(row, 2).Value = rattrapage;
                worksheet.Cell(row, 3).Value = total > 0 ? (decimal)rattrapage / total * 100 : 0;
                worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.Yellow;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00\"%\"";
                row++;

                // Refusé -> Redouble/Exclu
                worksheet.Cell(row, 1).Value = "Redouble/Exclu";
                worksheet.Cell(row, 2).Value = refuse;
                worksheet.Cell(row, 3).Value = total > 0 ? (decimal)refuse / total * 100 : 0;
                worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.Red;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00\"%\"";
                row++;

                // Total
                worksheet.Cell(row, 1).Value = "TOTAL";
                worksheet.Cell(row, 2).Value = total;
                worksheet.Cell(row, 3).Value = 100;
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 2).Style.Font.Bold = true;
                worksheet.Cell(row, 3).Value = "100%";
                worksheet.Cell(row, 3).Style.Font.Bold = true;

                // Ajuster les largeurs
                worksheet.Columns().AdjustToContents();
                worksheet.Column(1).Width = 15;
                worksheet.Column(2).Width = 15;
                worksheet.Column(3).Width = 15;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erreur lors de l'ajout des statistiques: {ex.Message}");
            }
        }

        /// <summary>
        /// Ajouter la feuille Résumé
        /// </summary>
        private void AjouterResume(IXLWorksheet worksheet, List<Etudiant> etudiants)
        {
            try
            {
                // Titre
                worksheet.Cell(1, 1).Value = "RÉSUMÉ DE LA DÉLIBÉRATION";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;

                int row = 3;

                // Informations générales
                worksheet.Cell(row, 1).Value = "Date de délibération:";
                worksheet.Cell(row, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                row += 2;

                worksheet.Cell(row, 1).Value = "Nombre total d'étudiants:";
                worksheet.Cell(row, 2).Value = etudiants.Count;
                row += 2;

                // Résultats
                int admis = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis"));
                int rattrapage = etudiants.Count(e => e.Decision == "Conseil d'École");
                int refuse = etudiants.Count(e => e.Decision != null && e.Decision.Contains("Redouble/Exclu"));

                worksheet.Cell(row, 1).Value = "Résultats:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row += 2;

                worksheet.Cell(row, 1).Value = "Admis";
                worksheet.Cell(row, 2).Value = admis;
                row++;

                worksheet.Cell(row, 1).Value = "Conseil d'École";
                worksheet.Cell(row, 2).Value = rattrapage;
                row++;

                worksheet.Cell(row, 1).Value = "Redouble/Exclu";
                worksheet.Cell(row, 2).Value = refuse;
                row += 2;

                // Statistiques
                decimal moyAdmis = etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis")).Any()
                    ? etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis"))
                        .Average(e => e.MoyenneGenerale)
                    : 0;
                decimal moyRattrapage = etudiants.Where(e => e.Decision == "Conseil d'École").Any()
                    ? etudiants.Where(e => e.Decision == "Conseil d'École")
                        .Average(e => e.MoyenneGenerale)
                    : 0;
                decimal moyRefuse = etudiants.Where(e => e.Decision != null && e.Decision.Contains("Redouble/Exclu")).Any()
                    ? etudiants.Where(e => e.Decision != null && e.Decision.Contains("Redouble/Exclu"))
                        .Average(e => e.MoyenneGenerale)
                    : 0;

                worksheet.Cell(row, 1).Value = "Moyennes par résultat:";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                row += 2;

                worksheet.Cell(row, 1).Value = "Moyenne des Admis:";
                worksheet.Cell(row, 2).Value = admis > 0 ? moyAdmis : 0;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                row++;

                worksheet.Cell(row, 1).Value = "Moyenne (Conseil d'École):";
                worksheet.Cell(row, 2).Value = rattrapage > 0 ? moyRattrapage : 0;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                row++;

                worksheet.Cell(row, 1).Value = "Moyenne (Redouble/Exclu):";
                worksheet.Cell(row, 2).Value = refuse > 0 ? moyRefuse : 0;
                worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
                row++;

                // Ajuster les largeurs
                worksheet.Columns().AdjustToContents();
                worksheet.Column(1).Width = 25;
                worksheet.Column(2).Width = 25;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erreur lors de l'ajout du résumé: {ex.Message}");
            }
        }
    }
}

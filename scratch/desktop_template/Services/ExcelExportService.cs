using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using DesktopApp.Models;
using System.IO;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour exporter les données des étudiants en fichier Excel
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Exporter une liste d'étudiants en fichier Excel
        /// </summary>
        public bool ExporterEtudiants(List<Etudiant> etudiants, string cheminSortie, string nomFichier)
        {
            try
            {
                Console.WriteLine($"[EXPORT] Début de l'export Excel");
                Console.WriteLine($"[EXPORT] Nombre d'étudiants: {etudiants?.Count ?? 0}");
                Console.WriteLine($"[EXPORT] Chemin de sortie: {cheminSortie}");
                Console.WriteLine($"[EXPORT] Nom du fichier: {nomFichier}");

                if (etudiants == null || etudiants.Count == 0)
                {
                    Console.WriteLine("[EXPORT] ❌ Erreur: Pas de données à exporter");
                    return false;
                }

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(cheminSortie))
                {
                    Console.WriteLine($"[EXPORT] Création du dossier: {cheminSortie}");
                    Directory.CreateDirectory(cheminSortie);
                }

                string cheminComplet = Path.Combine(cheminSortie, nomFichier);
                Console.WriteLine($"[EXPORT] Chemin complet: {cheminComplet}");

                return ExporterVersExcel(etudiants, cheminComplet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXPORT] ❌ Exception: {ex.GetType().Name}");
                Console.WriteLine($"[EXPORT] Message: {ex.Message}");
                Console.WriteLine($"[EXPORT] StackTrace: {ex.StackTrace}");
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'export Excel:\n\n{ex.Message}\n\nVérifiez la console Visual Studio pour plus de détails.", 
                    "Erreur d'export");
                return false;
            }
        }

        /// <summary>
        /// Exporter vers un fichier Excel avec chemin complet (nouvelle méthode pour DocumentGenerator)
        /// </summary>
        public bool ExporterVersExcel(List<Etudiant> etudiants, string cheminComplet)
        {
            try
            {
                Console.WriteLine($"[EXPORT] Début de l'export Excel vers: {cheminComplet}");
                Console.WriteLine($"[EXPORT] Nombre d'étudiants: {etudiants?.Count ?? 0}");

                if (etudiants == null || etudiants.Count == 0)
                {
                    Console.WriteLine("[EXPORT] ❌ Erreur: Pas de données à exporter");
                    return false;
                }

                // Créer le dossier s'il n'existe pas
                string dossier = Path.GetDirectoryName(cheminComplet);
                if (!Directory.Exists(dossier))
                {
                    Console.WriteLine($"[EXPORT] Création du dossier: {dossier}");
                    Directory.CreateDirectory(dossier);
                }

                // Vérifier que le dossier est accessible
                if (!Directory.Exists(dossier))
                {
                    Console.WriteLine("[EXPORT] ❌ Erreur: Impossible de créer le dossier");
                    return false;
                }

                // Créer le workbook
                using (var workbook = new XLWorkbook())
                {
                    Console.WriteLine("[EXPORT] Création du workbook");
                    var worksheet = workbook.Worksheets.Add("Résultats Délibération");

                    // En-têtes (ligne 1) avec formatage
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0DCFF); // Bleu clair
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontColor = XLColor.Black;

                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom et Prénom";
                    worksheet.Cell(1, 3).Value = "Matricule";
                    worksheet.Cell(1, 4).Value = "Classe";
                    worksheet.Cell(1, 5).Value = "MG";
                    worksheet.Cell(1, 6).Value = "ECTS";
                    worksheet.Cell(1, 7).Value = "Statut";
                    worksheet.Cell(1, 8).Value = "Décision";
                    worksheet.Cell(1, 9).Value = "Mention";
                    worksheet.Cell(1, 10).Value = "Observation";

                    Console.WriteLine("[EXPORT] En-têtes créés");

                    // Ajouter les données
                    int rowNum = 2;
                    foreach (var etudiant in etudiants)
                    {
                        worksheet.Cell(rowNum, 1).Value = etudiant.NumeroOrdre;
                        worksheet.Cell(rowNum, 2).Value = etudiant.NomPrenom;
                        worksheet.Cell(rowNum, 3).Value = etudiant.Matricule;
                        worksheet.Cell(rowNum, 4).Value = etudiant.ClasseGroupe;
                        worksheet.Cell(rowNum, 5).Value = etudiant.MoyenneGenerale;
                        worksheet.Cell(rowNum, 6).Value = etudiant.EctsValides;
                        worksheet.Cell(rowNum, 7).Value = etudiant.Statut;
                        worksheet.Cell(rowNum, 8).Value = etudiant.Decision;
                        worksheet.Cell(rowNum, 9).Value = etudiant.Mention;
                        worksheet.Cell(rowNum, 10).Value = etudiant.Observation;

                        // Formatage des cellules numériques
                        worksheet.Cell(rowNum, 5).Style.NumberFormat.Format = "0.00";

                        // Couleurs selon la décision
                        var decisionCell = worksheet.Cell(rowNum, 8);
                        if (etudiant.Decision != null)
                        {
                            if (etudiant.Decision.StartsWith("Admis"))
                            {
                                decisionCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                            else if (etudiant.Decision.Contains("Conseil"))
                            {
                                decisionCell.Style.Fill.BackgroundColor = XLColor.LightYellow;
                            }
                            else if (etudiant.Decision.Contains("Redouble") || etudiant.Decision.Contains("Exclu"))
                            {
                                decisionCell.Style.Fill.BackgroundColor = XLColor.LightCoral;
                            }
                        }

                        rowNum++;
                    }

                    Console.WriteLine($"[EXPORT] {etudiants.Count} lignes de données ajoutées");

                    // Ajuster la largeur des colonnes
                    worksheet.Column(1).Width = 5;   // N°
                    worksheet.Column(2).Width = 25;  // Nom
                    worksheet.Column(3).Width = 15;  // Matricule
                    worksheet.Column(4).Width = 15;  // Classe
                    worksheet.Column(5).Width = 10;  // MG
                    worksheet.Column(6).Width = 8;   // ECTS
                    worksheet.Column(7).Width = 12;  // Statut
                    worksheet.Column(8).Width = 20;  // Décision
                    worksheet.Column(9).Width = 20;  // Mention
                    worksheet.Column(10).Width = 30; // Observation

                    Console.WriteLine("[EXPORT] Colonnes formatées");

                    // Ajouter des filtres
                    try
                    {
                        var range = worksheet.Range(1, 1, rowNum - 1, 10);
                        range.SetAutoFilter();
                        Console.WriteLine("[EXPORT] Filtres activés");
                    }
                    catch (Exception filterEx)
                    {
                        Console.WriteLine($"[EXPORT] ⚠ Avertissement: Impossible d'activer les filtres: {filterEx.Message}");
                    }

                    // Sauvegarder le fichier
                    Console.WriteLine($"[EXPORT] Sauvegarde du fichier: {cheminComplet}");
                    workbook.SaveAs(cheminComplet);

                    // Vérifier que le fichier a été créé
                    if (File.Exists(cheminComplet))
                    {
                        FileInfo fileInfo = new FileInfo(cheminComplet);
                        Console.WriteLine($"[EXPORT] ✓ Fichier créé avec succès ({fileInfo.Length} bytes)");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[EXPORT] ❌ Erreur: Le fichier n'a pas été créé");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXPORT] ❌ Exception: {ex.GetType().Name}");
                Console.WriteLine($"[EXPORT] Message: {ex.Message}");
                Console.WriteLine($"[EXPORT] StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}

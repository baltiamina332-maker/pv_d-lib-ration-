using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour exporter les données des étudiants directement depuis la base de données en Excel
    /// </summary>
    public class ExcelExportFromDatabaseService
    {
        private DatabaseConnection _dbConnection;

        public ExcelExportFromDatabaseService()
        {
            _dbConnection = new DatabaseConnection();
        }

        /// <summary>
        /// Récupérer les étudiants depuis la base de données
        /// </summary>
        public List<Etudiant> GetStudentsFromDatabase()
        {
            var etudiants = new List<Etudiant>();

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    // Requête pour récupérer les étudiants (adaptée à la structure réelle)
                    string query = @"
                        SELECT 
                            id_etudiant,
                            nom,
                            prenom,
                            matricule,
                            classe_groupe,
                            statut
                        FROM etudiant
                        ORDER BY id_etudiant ASC
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = null;
                    
                    try
                    {
                        reader = cmd.ExecuteReader();

                        int rowCount = 0;
                        while (reader.Read())
                        {
                            try
                            {
                                // Récupérer les valeurs avec gestion des NULL
                                int id = 0;
                                if (!reader.IsDBNull(0) && int.TryParse(reader[0].ToString(), out int parsedId))
                                {
                                    id = parsedId;
                                }

                                string nom = reader.IsDBNull(1) ? "" : reader[1].ToString();
                                string prenom = reader.IsDBNull(2) ? "" : reader[2].ToString();
                                string matricule = reader.IsDBNull(3) ? "" : reader[3].ToString();
                                string classeGroupe = reader.IsDBNull(4) ? "" : reader[4].ToString();
                                string statut = reader.IsDBNull(5) ? "" : reader[5].ToString();

                                var etudiant = new Etudiant
                                {
                                    NumeroOrdre = id,
                                    NomPrenom = $"{nom} {prenom}".Trim(),
                                    Matricule = matricule,
                                    ClasseGroupe = classeGroupe,
                                    AnneeUniversitaire = "",
                                    MoyenneGenerale = 0m,
                                    EctsValides = 0,
                                    Decision = statut,
                                    Mention = ""
                                };

                                etudiants.Add(etudiant);
                                rowCount++;
                            }
                            catch (Exception rowEx)
                            {
                                Console.WriteLine($"[EXCEL-DB] Erreur ligne: {rowEx.Message}");
                            }
                        }

                        Console.WriteLine($"[EXCEL-DB] ✓ {rowCount} étudiants récupérés");
                    }
                    finally
                    {
                        // Fermer le reader AVANT la connexion
                        if (reader != null && !reader.IsClosed)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                    }

                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEL-DB] ❌ Erreur: {ex.Message}");
                Console.WriteLine($"[EXCEL-DB] Stack: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"Erreur BD:\n{ex.Message}", "Erreur DB");
            }

            return etudiants;
        }

        /// <summary>
        /// Exporter les étudiants depuis la base de données en fichier Excel
        /// </summary>
        public bool ExporterDepuisDatabase(string dossierSortie = "")
        {
            try
            {
                Console.WriteLine("\n════════════════════════════════════════════════════════════");
                Console.WriteLine("  EXPORT EXCEL DEPUIS BASE DE DONNÉES");
                Console.WriteLine("════════════════════════════════════════════════════════════\n");

                // Si pas de dossier spécifié, utiliser Documents
                if (string.IsNullOrEmpty(dossierSortie))
                {
                    dossierSortie = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Exports_Étudiants"
                    );
                }

                Console.WriteLine($"[EXCEL-DB] Récupération des étudiants depuis la base de données...");
                var etudiants = GetStudentsFromDatabase();

                if (etudiants.Count == 0)
                {
                    Console.WriteLine("[EXCEL-DB] ⚠ Aucune donnée trouvée dans la base de données");
                    System.Windows.MessageBox.Show(
                        "Aucun étudiant trouvé dans la base de données.",
                        "Aucune donnée");
                    return false;
                }

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(dossierSortie))
                {
                    Console.WriteLine($"[EXCEL-DB] Création du dossier: {dossierSortie}");
                    Directory.CreateDirectory(dossierSortie);
                }

                // Nom du fichier
                string dateActuelle = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nomFichier = $"Etudiants_Base_{dateActuelle}.xlsx";
                string cheminComplet = Path.Combine(dossierSortie, nomFichier);

                Console.WriteLine($"[EXCEL-DB] Chemin complet: {cheminComplet}");

                // Créer le workbook Excel
                using (var workbook = new XLWorkbook())
                {
                    Console.WriteLine("[EXCEL-DB] Création du workbook");
                    var worksheet = workbook.Worksheets.Add("Étudiants");

                    // En-têtes (ligne 1) avec formatage
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(0x8B3A3A); // Bordeaux
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontColor = XLColor.White;

                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom et Prénom";
                    worksheet.Cell(1, 3).Value = "Matricule";
                    worksheet.Cell(1, 4).Value = "Classe/Groupe";
                    worksheet.Cell(1, 5).Value = "Année Univ.";
                    worksheet.Cell(1, 6).Value = "MG";
                    worksheet.Cell(1, 7).Value = "ECTS";
                    worksheet.Cell(1, 8).Value = "Décision";
                    worksheet.Cell(1, 9).Value = "Mention";

                    Console.WriteLine("[EXCEL-DB] En-têtes créés");

                    // Ajouter les données
                    int rowNum = 2;
                    foreach (var etudiant in etudiants)
                    {
                        worksheet.Cell(rowNum, 1).Value = etudiant.NumeroOrdre;
                        worksheet.Cell(rowNum, 2).Value = etudiant.NomPrenom;
                        worksheet.Cell(rowNum, 3).Value = etudiant.Matricule;
                        worksheet.Cell(rowNum, 4).Value = etudiant.ClasseGroupe;
                        worksheet.Cell(rowNum, 5).Value = etudiant.AnneeUniversitaire;
                        worksheet.Cell(rowNum, 6).Value = etudiant.MoyenneGenerale;
                        worksheet.Cell(rowNum, 7).Value = etudiant.EctsValides;
                        worksheet.Cell(rowNum, 8).Value = etudiant.Decision;
                        worksheet.Cell(rowNum, 9).Value = etudiant.Mention;

                        // Formatage des cellules numériques
                        worksheet.Cell(rowNum, 6).Style.NumberFormat.Format = "0.00";

                        rowNum++;
                    }

                    Console.WriteLine($"[EXCEL-DB] {etudiants.Count} lignes de données ajoutées");

                    // Ajuster la largeur des colonnes
                    worksheet.Column(1).Width = 5;
                    worksheet.Column(2).Width = 25;
                    worksheet.Column(3).Width = 15;
                    worksheet.Column(4).Width = 15;
                    worksheet.Column(5).Width = 15;
                    worksheet.Column(6).Width = 10;
                    worksheet.Column(7).Width = 10;
                    worksheet.Column(8).Width = 18;
                    worksheet.Column(9).Width = 20;

                    Console.WriteLine("[EXCEL-DB] Colonnes formatées");

                    // Ajouter des filtres
                    try
                    {
                        var range = worksheet.Range(1, 1, rowNum - 1, 9);
                        range.SetAutoFilter();
                        Console.WriteLine("[EXCEL-DB] Filtres activés");
                    }
                    catch (Exception filterEx)
                    {
                        Console.WriteLine($"[EXCEL-DB] ⚠ Avertissement: Impossible d'activer les filtres: {filterEx.Message}");
                    }

                    // Sauvegarder le fichier
                    Console.WriteLine($"[EXCEL-DB] Sauvegarde du fichier: {cheminComplet}");
                    workbook.SaveAs(cheminComplet);

                    // Vérifier que le fichier a été créé
                    if (System.IO.File.Exists(cheminComplet))
                    {
                        FileInfo fileInfo = new FileInfo(cheminComplet);
                        Console.WriteLine($"[EXCEL-DB] ✓ Fichier créé avec succès ({fileInfo.Length} bytes)");
                        
                        Console.WriteLine("\n════════════════════════════════════════════════════════════");
                        Console.WriteLine("  FIN EXPORT EXCEL");
                        Console.WriteLine("════════════════════════════════════════════════════════════\n");

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[EXCEL-DB] ❌ Erreur: Le fichier n'a pas été créé");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEL-DB] ❌ Exception: {ex.GetType().Name}");
                Console.WriteLine($"[EXCEL-DB] Message: {ex.Message}");
                Console.WriteLine($"[EXCEL-DB] StackTrace: {ex.StackTrace}");
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'export Excel depuis la base:\n\n{ex.Message}",
                    "Erreur d'export");
                return false;
            }
        }
    }
}

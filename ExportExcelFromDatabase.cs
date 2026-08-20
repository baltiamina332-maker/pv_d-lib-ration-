using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ClosedXML.Excel;

namespace DesktopApp
{
    /// <summary>
    /// Service pour exporter les données de la base de données vers un fichier Excel
    /// </summary>
    public class ExportExcelService
    {
        private DatabaseHelper dbHelper;

        public ExportExcelService()
        {
            dbHelper = new DatabaseHelper();
        }

        /// <summary>
        /// Exporter les données des étudiants vers un fichier Excel
        /// </summary>
        public bool ExporterEtudiantsEnExcel(string cheminSortie, string nomFichier = null)
        {
            try
            {
                // Générer le nom du fichier s'il n'est pas fourni
                if (string.IsNullOrEmpty(nomFichier))
                {
                    nomFichier = $"etudiants_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                }

                string cheminComplet = Path.Combine(cheminSortie, nomFichier);

                // Créer un nouveau classeur Excel
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Étudiants");

                    // Ajouter les en-têtes
                    worksheet.Cell(1, 1).Value = "N°";
                    worksheet.Cell(1, 2).Value = "Nom et Prénom";
                    worksheet.Cell(1, 3).Value = "Matricule";
                    worksheet.Cell(1, 4).Value = "Classe";
                    worksheet.Cell(1, 5).Value = "Moyenne";
                    worksheet.Cell(1, 6).Value = "Décision";
                    worksheet.Cell(1, 7).Value = "Mention";

                    // Formater les en-têtes
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Récupérer les données de la base de données
                    var donnees = LireDonneesDeLaBase();

                    // Remplir les données
                    int row = 2;
                    foreach (DataRow etudiant in donnees.Rows)
                    {
                        worksheet.Cell(row, 1).Value = etudiant["id_etudiant"] != DBNull.Value ? (int)etudiant["id_etudiant"] : 0;
                        worksheet.Cell(row, 2).Value = $"{etudiant["nom"]} {etudiant["prenom"]}";
                        worksheet.Cell(row, 3).Value = etudiant["matricule"]?.ToString() ?? "";
                        worksheet.Cell(row, 4).Value = etudiant["classe_groupe"]?.ToString() ?? "";
                        
                        // Moyenne générale
                        if (decimal.TryParse(etudiant["moyenne_generale"]?.ToString() ?? "", out decimal moyenne))
                        {
                            worksheet.Cell(row, 5).Value = moyenne;
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";
                        }
                        else
                        {
                            worksheet.Cell(row, 5).Value = "";
                        }

                        worksheet.Cell(row, 6).Value = ""; // Vide - sera rempli automatiquement
                        worksheet.Cell(row, 7).Value = ""; // Vide - sera rempli automatiquement

                        row++;
                    }

                    // Ajuster la largeur des colonnes
                    worksheet.Column(1).Width = 5;
                    worksheet.Column(2).Width = 25;
                    worksheet.Column(3).Width = 15;
                    worksheet.Column(4).Width = 12;
                    worksheet.Column(5).Width = 12;
                    worksheet.Column(6).Width = 15;
                    worksheet.Column(7).Width = 15;

                    // Centrer les colonnes numériques
                    worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Sauvegarder le fichier
                    workbook.SaveAs(cheminComplet);

                    Console.WriteLine($"✅ Fichier Excel créé avec succès : {cheminComplet}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'export : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lire les données de la base de données
        /// </summary>
        private DataTable LireDonneesDeLaBase()
        {
            var dataTable = new DataTable();

            try
            {
                // Essayer de lire depuis la table decision_mention
                string query = @"
                    SELECT 
                        id_etudiant,
                        nom,
                        prenom,
                        matricule,
                        classe_groupe,
                        moyenne_generale
                    FROM decision_mention
                    ORDER BY id_etudiant
                ";

                dataTable = dbHelper.ExecuteSelectQuery(query);

                if (dataTable.Rows.Count == 0)
                {
                    Console.WriteLine("⚠️ Aucune donnée trouvée dans la table decision_mention");
                }
                else
                {
                    Console.WriteLine($"✅ {dataTable.Rows.Count} enregistrement(s) trouvé(s)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la lecture de la base : {ex.Message}");
            }

            return dataTable;
        }

        /// <summary>
        /// Obtenir la liste des tables disponibles dans la base
        /// </summary>
        public List<string> ObtenirListesTables()
        {
            var tables = new List<string>();

            try
            {
                var helper = new DatabaseHelper();
                tables = helper.GetAllTables();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des tables : {ex.Message}");
            }

            return tables;
        }
    }
}

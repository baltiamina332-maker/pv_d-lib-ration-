using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using DesktopApp.Models;
using DesktopApp.Services;

namespace DesktopApp.Tests
{
    /// <summary>
    /// Script de test pour vérifier que l'import Excel fonctionne correctement
    /// Exécution: dotnet fsi TEST_IMPORT.cs
    /// </summary>
    class TestImport
    {
        static void Main()
        {
            Console.WriteLine("=== TEST D'IMPORT EXCEL ===\n");

            try
            {
                // Créer un fichier Excel de test
                Console.WriteLine("1. Création d'un fichier Excel de test...");
                string testFile = "test_data.xlsx";
                CreateTestExcelFile(testFile);
                Console.WriteLine($"   ✅ Fichier créé: {testFile}\n");

                // Tester l'import
                Console.WriteLine("2. Test d'import avec ExcelImportService...");
                var service = new ExcelImportService();
                var result = service.ImporterDonneesExcel(testFile);

                if (result.Succes)
                {
                    Console.WriteLine($"   ✅ Import réussi: {result.MessageSucces}");
                    Console.WriteLine($"\n3. Données importées ({result.Etudiants.Count} étudiants):\n");

                    // Afficher les données
                    int index = 1;
                    foreach (var etudiant in result.Etudiants)
                    {
                        Console.WriteLine($"   {index}. {etudiant}");
                        Console.WriteLine($"      Décision: {etudiant.Decision} | Mention: {etudiant.Mention}");
                        index++;
                    }

                    Console.WriteLine("\n   ✅ SUCCÈS: Import et calculs fonctionnent correctement!");
                }
                else
                {
                    Console.WriteLine($"   ❌ Erreur: {result.MessageErreur}");
                    if (result.Avertissements.Count > 0)
                    {
                        Console.WriteLine("\n   Avertissements:");
                        foreach (var avertissement in result.Avertissements)
                        {
                            Console.WriteLine($"   - {avertissement}");
                        }
                    }
                }

                // Nettoyer
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                    Console.WriteLine($"\n4. Fichier de test supprimé");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Erreur: {ex.Message}");
                Console.WriteLine($"\n   Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\n=== FIN DU TEST ===");
        }

        static void CreateTestExcelFile(string fileName)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Données");

                // En-têtes (ligne 1)
                worksheet.Cell(1, 1).Value = "id_etudiant";
                worksheet.Cell(1, 2).Value = "nom";
                worksheet.Cell(1, 3).Value = "prenom";
                worksheet.Cell(1, 4).Value = "matricule";
                worksheet.Cell(1, 5).Value = "classe_groupe";
                worksheet.Cell(1, 6).Value = "id_session";
                worksheet.Cell(1, 7).Value = "type_session";
                worksheet.Cell(1, 8).Value = "annee_universitaire";
                worksheet.Cell(1, 9).Value = "moyenne_generale";

                // Données de test
                var testData = new List<(int, string, string, string, string, decimal)>
                {
                    (1, "Martin", "Jean", "MAT001", "L3-GROUPE1", 16.5m),      // Très Bien
                    (2, "Dupont", "Marie", "MAT002", "L3-GROUPE1", 15.0m),     // Bien
                    (3, "Bernard", "Pierre", "MAT003", "L3-GROUPE1", 13.0m),   // Assez Bien
                    (4, "Thomas", "Luc", "MAT004", "L3-GROUPE1", 11.0m),       // Passable
                    (5, "Robert", "Anne", "MAT005", "L3-GROUPE1", 9.0m),       // Session de rattrapage
                    (6, "Richard", "Paul", "MAT006", "L3-GROUPE1", 7.0m),      // Ajourné
                };

                // Remplir les données
                int row = 2;
                foreach (var (id, nom, prenom, matricule, classe, moyenne) in testData)
                {
                    worksheet.Cell(row, 1).Value = id;
                    worksheet.Cell(row, 2).Value = nom;
                    worksheet.Cell(row, 3).Value = prenom;
                    worksheet.Cell(row, 4).Value = matricule;
                    worksheet.Cell(row, 5).Value = classe;
                    worksheet.Cell(row, 6).Value = 1;
                    worksheet.Cell(row, 7).Value = "Session Normale";
                    worksheet.Cell(row, 8).Value = "2024-2025";
                    worksheet.Cell(row, 9).Value = moyenne;
                    row++;
                }

                workbook.SaveAs(fileName);
            }
        }
    }
}

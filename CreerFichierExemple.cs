using System;
using System.IO;
using ClosedXML.Excel;

/// <summary>
/// Créer un fichier Excel exemple que MainWindow peut charger automatiquement
/// </summary>
public class CreerFichierExemple
{
    public static void Main()
    {
        Console.WriteLine("Création du fichier Excel exemple...");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Etudiants");

                // En-têtes
                worksheet.Cell(1, 1).Value = "id_etudiant";
                worksheet.Cell(1, 2).Value = "nom";
                worksheet.Cell(1, 3).Value = "prenom";
                worksheet.Cell(1, 4).Value = "matricule";
                worksheet.Cell(1, 5).Value = "classe_groupe";
                worksheet.Cell(1, 6).Value = "id_session";
                worksheet.Cell(1, 7).Value = "type_session";
                worksheet.Cell(1, 8).Value = "annee_universitaire";
                worksheet.Cell(1, 9).Value = "moyenne_generale";

                // Données d'exemple
                worksheet.Cell(2, 1).Value = 1;
                worksheet.Cell(2, 2).Value = "DUPONT";
                worksheet.Cell(2, 3).Value = "Jean";
                worksheet.Cell(2, 4).Value = "2021001";
                worksheet.Cell(2, 5).Value = "L3-INFO-A";
                worksheet.Cell(2, 6).Value = 1;
                worksheet.Cell(2, 7).Value = "Session Normale";
                worksheet.Cell(2, 8).Value = "2024-2025";
                worksheet.Cell(2, 9).Value = 15.75;

                worksheet.Cell(3, 1).Value = 2;
                worksheet.Cell(3, 2).Value = "MARTIN";
                worksheet.Cell(3, 3).Value = "Marie";
                worksheet.Cell(3, 4).Value = "2021002";
                worksheet.Cell(3, 5).Value = "L3-INFO-A";
                worksheet.Cell(3, 6).Value = 1;
                worksheet.Cell(3, 7).Value = "Session Normale";
                worksheet.Cell(3, 8).Value = "2024-2025";
                worksheet.Cell(3, 9).Value = 13.50;

                worksheet.Cell(4, 1).Value = 3;
                worksheet.Cell(4, 2).Value = "BERNARD";
                worksheet.Cell(4, 3).Value = "Pierre";
                worksheet.Cell(4, 4).Value = "2021003";
                worksheet.Cell(4, 5).Value = "L3-INFO-A";
                worksheet.Cell(4, 6).Value = 1;
                worksheet.Cell(4, 7).Value = "Session Normale";
                worksheet.Cell(4, 8).Value = "2024-2025";
                worksheet.Cell(4, 9).Value = 11.25;

                worksheet.Cell(5, 1).Value = 4;
                worksheet.Cell(5, 2).Value = "THOMAS";
                worksheet.Cell(5, 3).Value = "Lucie";
                worksheet.Cell(5, 4).Value = "2021004";
                worksheet.Cell(5, 5).Value = "L3-INFO-A";
                worksheet.Cell(5, 6).Value = 1;
                worksheet.Cell(5, 7).Value = "Session Normale";
                worksheet.Cell(5, 8).Value = "2024-2025";
                worksheet.Cell(5, 9).Value = 16.80;

                worksheet.Cell(6, 1).Value = 5;
                worksheet.Cell(6, 2).Value = "ROBERT";
                worksheet.Cell(6, 3).Value = "Anne";
                worksheet.Cell(6, 4).Value = "2021005";
                worksheet.Cell(6, 5).Value = "L3-INFO-A";
                worksheet.Cell(6, 6).Value = 1;
                worksheet.Cell(6, 7).Value = "Session Normale";
                worksheet.Cell(6, 8).Value = "2024-2025";
                worksheet.Cell(6, 9).Value = 9.75;

                // Formater les colonnes
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 15;
                worksheet.Column(3).Width = 15;
                worksheet.Column(4).Width = 12;
                worksheet.Column(5).Width = 12;
                worksheet.Column(6).Width = 8;
                worksheet.Column(7).Width = 15;
                worksheet.Column(8).Width = 12;
                worksheet.Column(9).Width = 12;

                // Formater les en-têtes
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Sauvegarder le fichier
                workbook.SaveAs("etudiants.xlsx");
            }

            Console.WriteLine("✅ Fichier 'etudiants.xlsx' créé avec succès!");
            Console.WriteLine("   Contient 5 étudiants exemples avec toutes les coordonnées requises");
            Console.WriteLine("   L'application MainWindow le chargera automatiquement au démarrage");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur: {ex.Message}");
        }

        Console.WriteLine("\nAppuyez sur une touche pour quitter...");
        Console.ReadKey();
    }
}
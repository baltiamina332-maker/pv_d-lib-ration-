using System;
using System.IO;

namespace DesktopApp
{
    /// <summary>
    /// Programme pour exporter les données de la base de données vers Excel
    /// Lancez ce programme une fois pour générer le fichier Excel
    /// </summary>
    class ExportData
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Export des données vers Excel                           ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            try
            {
                // Service d'export
                var exportService = new ExportExcelService();

                // Dossier de sortie (Bureau ou Documents)
                string dossierSortie = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                // Si le Bureau n'existe pas, utiliser Documents
                if (!Directory.Exists(dossierSortie))
                {
                    dossierSortie = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                Console.WriteLine($"📂 Dossier de destination : {dossierSortie}\n");

                // Exporter les étudiants
                Console.WriteLine("⏳ Export en cours...\n");

                bool succes = exportService.ExporterEtudiantsEnExcel(
                    dossierSortie, 
                    $"etudiants_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                );

                if (succes)
                {
                    Console.WriteLine("\n✅ Export réussi !");
                    Console.WriteLine($"📄 Le fichier Excel a été créé dans : {dossierSortie}");
                    Console.WriteLine("\n Vous pouvez maintenant :");
                    Console.WriteLine("  1. Ouvrir le fichier dans Excel");
                    Console.WriteLine("  2. L'importer dans l'application via 'Import Excel'");
                }
                else
                {
                    Console.WriteLine("\n❌ L'export a échoué. Vérifiez la connexion à la base de données.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Erreur : {ex.Message}");
            }

            Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            Console.ReadKey();
        }
    }
}

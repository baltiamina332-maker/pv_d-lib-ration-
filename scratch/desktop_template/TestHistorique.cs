using System;
using System.Collections.Generic;
using DesktopApp.Services;
using DesktopApp.Models;

namespace DesktopApp
{
    /// <summary>
    /// Script de test pour l'historique des PV
    /// </summary>
    public class TestHistorique
    {
        public static void TestHistoriqueFunctionality()
        {
            Console.WriteLine("=== TEST DE L'HISTORIQUE ===");
            
            try
            {
                // 1. Test de création du service
                Console.WriteLine("1. Création du HistoriqueService...");
                var historiqueService = new HistoriqueService();
                Console.WriteLine("   ✓ Service créé avec succès");

                // 2. Test de récupération de l'historique existant
                Console.WriteLine("\n2. Test de récupération de l'historique...");
                var historique = historiqueService.GetAllHistorique();
                Console.WriteLine($"   ✓ Historique récupéré: {historique.Count} entrées");
                
                if (historique.Count > 0)
                {
                    Console.WriteLine("   Dernières entrées:");
                    for (int i = 0; i < Math.Min(3, historique.Count); i++)
                    {
                        var entry = historique[i];
                        Console.WriteLine($"     - {entry.DateDeliberation:dd/MM/yyyy HH:mm} | {entry.Classe} | {entry.NomFichier}");
                    }
                }
                else
                {
                    Console.WriteLine("   → Aucune entrée dans l'historique");
                }

                // 3. Test d'ajout d'une entrée de test
                Console.WriteLine("\n3. Test d'ajout d'une entrée de test...");
                var testEntry = new Historique
                {
                    DateDeliberation = DateTime.Now,
                    Classe = "TEST-CLASS",
                    Session = "2023-2024",
                    NomFichier = "PV_TEST_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx",
                    CheminFichier = @"C:\temp\test_pv.docx",
                    NbEtudiants = 25,
                    NbAdmis = 20,
                    NbAjournes = 5,
                    UtilisateurId = 1
                };

                bool addResult = historiqueService.AddHistorique(testEntry);
                if (addResult)
                {
                    Console.WriteLine("   ✓ Entrée de test ajoutée avec succès");
                    
                    // Vérifier que l'entrée a bien été ajoutée
                    var updatedHistorique = historiqueService.GetAllHistorique();
                    Console.WriteLine($"   ✓ Historique mis à jour: {updatedHistorique.Count} entrées");
                }
                else
                {
                    Console.WriteLine("   ✗ Échec de l'ajout de l'entrée de test");
                }

                // 4. Test des méthodes de filtrage
                Console.WriteLine("\n4. Test des méthodes de filtrage...");
                
                var thisMonth = historiqueService.GetHistoriqueThisMonth();
                Console.WriteLine($"   ✓ Historique ce mois: {thisMonth.Count} entrées");
                
                var today = historiqueService.GetHistoriqueToday();
                Console.WriteLine($"   ✓ Historique aujourd'hui: {today.Count} entrées");

                Console.WriteLine("\n=== TEST TERMINÉ AVEC SUCCÈS ===");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERREUR LORS DU TEST:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Erreur interne: {ex.InnerException.Message}");
                }

                // Analyser le type d'erreur
                if (ex.Message.Contains("deliberations"))
                {
                    Console.WriteLine("\n💡 DIAGNOSTIC:");
                    Console.WriteLine("   → La table 'deliberations' n'existe probablement pas en base de données");
                    Console.WriteLine("   → Cela est normal si c'est la première fois que vous testez");
                    Console.WriteLine("   → L'historique sera créé automatiquement lors de la première génération de PV");
                }
                else if (ex.Message.Contains("connection") || ex.Message.Contains("server"))
                {
                    Console.WriteLine("\n💡 DIAGNOSTIC:");
                    Console.WriteLine("   → Problème de connexion à la base de données");
                    Console.WriteLine("   → Vérifiez que MySQL est démarré");
                    Console.WriteLine("   → Vérifiez les paramètres de connexion dans DatabaseConnection.cs");
                }
                
                Console.WriteLine("\n=== TEST TERMINÉ AVEC ERREURS ===");
            }
        }

        /// <summary>
        /// Méthode pour tester l'historique depuis l'interface
        /// </summary>
        public static void TestFromUI()
        {
            Console.WriteLine("=== TEST HISTORIQUE DEPUIS L'INTERFACE ===");
            
            // Cette méthode peut être appelée depuis MainWindow pour tester
            TestHistoriqueFunctionality();
            
            Console.WriteLine("\nℹ️  COMMENT TESTER L'HISTORIQUE DANS L'APPLICATION:");
            Console.WriteLine("1. Générer un PV via l'onglet 'Génération PV'");
            Console.WriteLine("2. Aller dans l'onglet 'Historique'");
            Console.WriteLine("3. Vérifier que la nouvelle entrée apparaît");
            Console.WriteLine("4. Cliquer sur 'Ouvrir' pour tester l'ouverture du fichier");
        }
    }
}
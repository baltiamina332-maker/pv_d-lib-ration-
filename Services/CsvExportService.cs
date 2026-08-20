using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour exporter les données des étudiants en fichier CSV
    /// </summary>
    public class CsvExportService
    {
        private DatabaseConnection _dbConnection;

        public CsvExportService()
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
                    // Requête pour récupérer les étudiants (adapter selon votre schéma)
                    string query = @"
                        SELECT 
                            id as NumeroOrdre,
                            nom_prenom as NomPrenom,
                            matricule as Matricule,
                            classe_groupe as ClasseGroupe,
                            annee_universitaire as AnneeUniversitaire,
                            moyenne_generale as MoyenneGenerale,
                            ects_valides as EctsValides,
                            decision as Decision,
                            mention as Mention
                        FROM etudiants
                        ORDER BY id ASC
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var etudiant = new Etudiant
                        {
                            NumeroOrdre = reader["NumeroOrdre"] != System.DBNull.Value ? (int)reader["NumeroOrdre"] : 0,
                            NomPrenom = reader["NomPrenom"]?.ToString() ?? "",
                            Matricule = reader["Matricule"]?.ToString() ?? "",
                            ClasseGroupe = reader["ClasseGroupe"]?.ToString() ?? "",
                            AnneeUniversitaire = reader["AnneeUniversitaire"]?.ToString() ?? "",
                            MoyenneGenerale = reader["MoyenneGenerale"] != System.DBNull.Value ? (decimal)(double)reader["MoyenneGenerale"] : 0m,
                            EctsValides = reader["EctsValides"] != System.DBNull.Value ? (int)reader["EctsValides"] : 0,
                            Decision = reader["Decision"]?.ToString() ?? "",
                            Mention = reader["Mention"]?.ToString() ?? ""
                        };

                        etudiants.Add(etudiant);
                    }

                    reader.Close();
                    _dbConnection.CloseConnection();

                    Console.WriteLine($"[CSV] {etudiants.Count} étudiants récupérés de la base de données");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CSV] ❌ Erreur lors de la récupération depuis la BD: {ex.Message}");
            }

            return etudiants;
        }

        /// <summary>
        /// Exporter une liste d'étudiants en fichier CSV
        /// </summary>
        public bool ExporterEtudiants(List<Etudiant> etudiants, string cheminSortie, string nomFichier)
        {
            try
            {
                Console.WriteLine($"[CSV] Début de l'export CSV");
                Console.WriteLine($"[CSV] Nombre d'étudiants: {etudiants?.Count ?? 0}");
                Console.WriteLine($"[CSV] Chemin de sortie: {cheminSortie}");
                Console.WriteLine($"[CSV] Nom du fichier: {nomFichier}");

                if (etudiants == null || etudiants.Count == 0)
                {
                    Console.WriteLine("[CSV] ❌ Erreur: Pas de données à exporter");
                    return false;
                }

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(cheminSortie))
                {
                    Console.WriteLine($"[CSV] Création du dossier: {cheminSortie}");
                    Directory.CreateDirectory(cheminSortie);
                }

                string cheminComplet = Path.Combine(cheminSortie, nomFichier);
                Console.WriteLine($"[CSV] Chemin complet: {cheminComplet}");

                // Créer le fichier CSV
                using (StreamWriter writer = new StreamWriter(cheminComplet, false, Encoding.UTF8))
                {
                    // En-têtes
                    string[] headers = new string[]
                    {
                        "N°",
                        "Nom et Prénom",
                        "Matricule",
                        "Classe/Groupe",
                        "Année Univ.",
                        "MG",
                        "ECTS",
                        "Décision",
                        "Mention"
                    };

                    writer.WriteLine(string.Join(",", headers));
                    Console.WriteLine("[CSV] En-têtes écrits");

                    // Données
                    int rowNum = 0;
                    foreach (var etudiant in etudiants)
                    {
                        string[] row = new string[]
                        {
                            etudiant.NumeroOrdre.ToString(),
                            EscapeCsvValue(etudiant.NomPrenom),
                            EscapeCsvValue(etudiant.Matricule),
                            EscapeCsvValue(etudiant.ClasseGroupe),
                            EscapeCsvValue(etudiant.AnneeUniversitaire),
                            etudiant.MoyenneGenerale.ToString("0.00"),
                            etudiant.EctsValides.ToString(),
                            EscapeCsvValue(etudiant.Decision),
                            EscapeCsvValue(etudiant.Mention)
                        };

                        writer.WriteLine(string.Join(",", row));
                        rowNum++;
                    }

                    Console.WriteLine($"[CSV] {rowNum} lignes de données écrites");
                }

                // Vérifier que le fichier a été créé
                if (File.Exists(cheminComplet))
                {
                    FileInfo fileInfo = new FileInfo(cheminComplet);
                    Console.WriteLine($"[CSV] ✓ Fichier créé avec succès ({fileInfo.Length} bytes)");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[CSV] ❌ Erreur: Le fichier n'a pas été créé");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CSV] ❌ Exception: {ex.GetType().Name}");
                Console.WriteLine($"[CSV] Message: {ex.Message}");
                Console.WriteLine($"[CSV] StackTrace: {ex.StackTrace}");
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'export CSV:\n\n{ex.Message}",
                    "Erreur d'export");
                return false;
            }
        }

        /// <summary>
        /// Exporter les étudiants depuis la base de données en CSV
        /// </summary>
        public bool ExporterDepuisDatabase(string cheminSortie, string nomFichier = "etudiant.csv")
        {
            try
            {
                Console.WriteLine("[CSV] Récupération des données depuis la base de données...");
                var etudiants = GetStudentsFromDatabase();

                if (etudiants.Count == 0)
                {
                    Console.WriteLine("[CSV] ⚠ Aucune donnée trouvée dans la base de données");
                    // Créer un fichier vide avec en-têtes
                }

                return ExporterEtudiants(etudiants, cheminSortie, nomFichier);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CSV] ❌ Erreur lors de l'export depuis la base: {ex.Message}");
                System.Windows.MessageBox.Show($"Erreur lors de l'export depuis la base:\n\n{ex.Message}", "Erreur");
                return false;
            }
        }

        /// <summary>
        /// Échapper les valeurs CSV (guillemets et virgules)
        /// </summary>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Si la valeur contient des virgules ou des guillemets, l'entourer de guillemets
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                // Échapper les guillemets en les doublant
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}

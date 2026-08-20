using System;
using System.Collections.Generic;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour calculer les décisions (Admis/Rattrapage/Refusé) basées sur la moyenne générale
    /// </summary>
    public class DecisionService
    {
        private DatabaseConnection _dbConnection;

        // Seuils de décision selon le Cahier des Charges (Annexe C)
        private const decimal MOYENNE_ADMIS = 10.0m;      // Moyenne pour Admis (>= 10)
        private const decimal MOYENNE_RATTRAPAGE = 8.0m;  // Moyenne pour Ajourné/Rattrapage (8 à 10)
        // Moins de 8 = Exclu

        public DecisionService()
        {
            _dbConnection = new DatabaseConnection();
        }

        /// <summary>
        /// Récupérer tous les étudiants avec calcul de moyenne et décision
        /// </summary>
        public List<Etudiant> GetStudentsWithDecisions()
        {
            var students = new List<Etudiant>();

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    // Requête pour récupérer les étudiants avec leurs notes
                    string query = @"
                        SELECT 
                            id_etudiant,
                            nom,
                            prenom,
                            matricule,
                            classe_groupe,
                            statut
                        FROM etudiant
                        ORDER BY nom, prenom
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = null;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            try
                            {
                                int id = reader.IsDBNull(0) ? 0 : (int)reader["id_etudiant"];
                                string nom = reader.IsDBNull(1) ? "" : reader["nom"].ToString();
                                string prenom = reader.IsDBNull(2) ? "" : reader["prenom"].ToString();
                                string matricule = reader.IsDBNull(3) ? "" : reader["matricule"].ToString();
                                string classeGroupe = reader.IsDBNull(4) ? "" : reader["classe_groupe"].ToString();
                                string statut = reader.IsDBNull(5) ? "" : reader["statut"].ToString();

                                // Créer objet Etudiant
                                var student = new Etudiant
                                {
                                    Id = id,
                                    Nom = nom,
                                    Prenom = prenom,
                                    Matricule = matricule,
                                    ClasseGroupe = classeGroupe,
                                    Statut = statut,
                                    MoyenneGenerale = 0, // Sera calculée
                                    Decision = "" // Sera déterminée
                                };

                                // Calculer la moyenne générale
                                CalculateMoyenne(student);

                                // Déterminer la décision
                                DetermineDecision(student);

                                students.Add(student);

                                Console.WriteLine($"✓ Étudiant chargé: {nom} {prenom} - Moyenne: {student.MoyenneGenerale:F2} - Décision: {student.Decision}");
                            }
                            catch (Exception rowEx)
                            {
                                Console.WriteLine($"⚠️ Erreur lors du traitement d'un étudiant: {rowEx.Message}");
                                continue;
                            }
                        }
                    }
                    finally
                    {
                        if (reader != null && !reader.IsClosed)
                        {
                            reader.Close();
                            reader.Dispose();
                        }
                    }

                    _dbConnection.CloseConnection();

                    if (students.Count == 0)
                    {
                        Console.WriteLine("⚠️ Aucun étudiant trouvé dans la base de données.");
                    }
                    else
                    {
                        Console.WriteLine($"✓ {students.Count} étudiant(s) chargé(s) avec décisions");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Impossible de se connecter à la base de données.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du chargement des étudiants: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            return students;
        }

        /// <summary>
        /// Calculer la moyenne générale d'un étudiant
        /// À adapter selon votre système de notes
        /// </summary>
        private void CalculateMoyenne(Etudiant student)
        {
            try
            {
                // NOTE: Cette implémentation dépend de la structure de votre base de données
                // Vous devrez adapter la requête selon vos tables de notes
                
                if (_dbConnection.OpenConnection())
                {
                    // Exemple: si vous avez une table de notes
                    string query = @"
                        SELECT AVG(note) as moyenne
                        FROM notes
                        WHERE id_etudiant = @studentId
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@studentId", student.Id);

                    try
                    {
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            student.MoyenneGenerale = (decimal)(double)result;
                        }
                        else
                        {
                            // Si aucune note trouvée, moyenne par défaut
                            student.MoyenneGenerale = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Impossible de calculer la moyenne pour {student.Nom}: {ex.Message}");
                        student.MoyenneGenerale = 0;
                    }

                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du calcul de moyenne: {ex.Message}");
                student.MoyenneGenerale = 0;
            }
        }

        /// <summary>
        /// Déterminer la décision basée sur la moyenne générale
        /// </summary>
        private void DetermineDecision(Etudiant student)
        {
            if (student.MoyenneGenerale >= MOYENNE_ADMIS)
            {
                student.Decision = "Admis";
            }
            else if (student.MoyenneGenerale >= MOYENNE_RATTRAPAGE)
            {
                student.Decision = "Ajourné";
                student.Mention = "Session de rattrapage";
            }
            else
            {
                student.Decision = "Exclu";
                student.Mention = "—";
            }
        }

        /// <summary>
        /// Obtenir les seuils de décision actuels
        /// </summary>
        public class ThresholdsInfo
        {
            public decimal MoyenneAdmis { get; set; }
            public decimal MoyenneRattrapage { get; set; }
            public string Description { get; set; }
        }

        public ThresholdsInfo GetThresholds()
        {
            return new ThresholdsInfo
            {
                MoyenneAdmis = MOYENNE_ADMIS,
                MoyenneRattrapage = MOYENNE_RATTRAPAGE,
                Description = $"Admis: >= {MOYENNE_ADMIS}, Rattrapage: {MOYENNE_RATTRAPAGE} à {MOYENNE_ADMIS - 0.01m}, Refusé: < {MOYENNE_RATTRAPAGE}"
            };
        }

        /// <summary>
        /// Statistiques des décisions
        /// </summary>
        public Dictionary<string, int> GetDecisionStats(List<Etudiant> students)
        {
            var stats = new Dictionary<string, int>
            {
                { "Admis", 0 },
                { "Conseil d'École", 0 },
                { "Redouble/Exclu", 0 }
            };

            foreach (var student in students)
            {
                if (stats.ContainsKey(student.Decision))
                {
                    stats[student.Decision]++;
                }
            }

            return stats;
        }
    }
}

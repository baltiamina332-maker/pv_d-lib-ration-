using System;
using System.Collections.Generic;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour gérer l'historique des PV générés
    /// </summary>
    public class HistoriqueService
    {
        private DatabaseConnection _dbConnection;

        public HistoriqueService()
        {
            _dbConnection = new DatabaseConnection();
        }

        /// <summary>
        /// Obtenir tous les PV de l'historique
        /// </summary>
        public List<Historique> GetAllHistorique()
        {
            var historique = new List<Historique>();

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    // Requête pour récupérer l'historique des délibérations
                    string query = @"
                        SELECT 
                            id, 
                            date_deliberation, 
                            nb_etudiants, 
                            nb_admis, 
                            nb_ajournes, 
                            fichier_pv, 
                            utilisateur_id
                        FROM deliberations
                        ORDER BY date_deliberation DESC
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = null;
                    
                    try
                    {
                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            string cheminFichier = reader["fichier_pv"].ToString();
                            string nomFichier = System.IO.Path.GetFileName(cheminFichier);

                            // Extraire la classe/groupe du nom du fichier ou de la BD
                            // Format supposé: PV_ClassName_DateHeure.docx
                            string classe = ExtractClassFromFilename(nomFichier);

                            historique.Add(new Historique
                            {
                                Id = (int)reader["id"],
                                DateDeliberation = (DateTime)reader["date_deliberation"],
                                Classe = classe,
                                Session = DateTime.Now.Year.ToString(), // À améliorer si session est en BD
                                NomFichier = nomFichier,
                                CheminFichier = cheminFichier,
                                NbEtudiants = (int)reader["nb_etudiants"],
                                NbAdmis = (int)reader["nb_admis"],
                                NbAjournes = (int)reader["nb_ajournes"],
                                UtilisateurId = reader["utilisateur_id"] != DBNull.Value ? (int)reader["utilisateur_id"] : 0
                            });
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
                }
            }
            catch (Exception ex)
            {
                // Juste log l'erreur, ne pas afficher de MessageBox
                Console.WriteLine($"⚠ Avertissement HistoriqueService: {ex.Message}");
                Console.WriteLine($"   La table 'deliberations' peut ne pas exister.");
                // Retourner une liste vide pour que l'app continue normalement
            }

            return historique;
        }

        /// <summary>
        /// Obtenir l'historique filtré pour un utilisateur spécifique (EF-08 - Mes PV)
        /// </summary>
        public List<Historique> GetHistoriqueForUser(int userId)
        {
            var tous = GetAllHistorique();
            if (userId <= 0) return tous;

            var filtres = tous.FindAll(h => h.UtilisateurId == userId);
            return filtres.Count > 0 ? filtres : tous;
        }

        /// <summary>
        /// Obtenir l'historique du mois courant
        /// </summary>
        public List<Historique> GetHistoriqueThisMonth()
        {
            var historique = GetAllHistorique();
            var thisMonth = DateTime.Now;

            return historique.FindAll(h => 
                h.DateDeliberation.Year == thisMonth.Year && 
                h.DateDeliberation.Month == thisMonth.Month
            );
        }

        /// <summary>
        /// Obtenir l'historique du jour courant
        /// </summary>
        public List<Historique> GetHistoriqueToday()
        {
            var historique = GetAllHistorique();
            var today = DateTime.Now.Date;

            return historique.FindAll(h => h.DateDeliberation.Date == today);
        }

        /// <summary>
        /// Ajouter une entrée à l'historique avec vérification de table
        /// </summary>
        public bool AddHistorique(Historique record)
        {
            try
            {
                // Vérifier d'abord si la table existe
                if (!TableExists("deliberations"))
                {
                    Console.WriteLine("⚠ Avertissement: La table 'deliberations' n'existe pas. L'historique ne sera pas sauvegardé.");
                    Console.WriteLine("   Pour créer la table, exécutez le script CREATE_TABLE_DELIBERATIONS.sql");
                    return false; // Retourner false mais ne pas afficher d'erreur utilisateur
                }

                if (_dbConnection.OpenConnection())
                {
                    string query = @"
                        INSERT INTO deliberations 
                        (date_deliberation, nb_etudiants, nb_admis, nb_ajournes, fichier_pv, utilisateur_id)
                        VALUES 
                        (NOW(), @nbEtudiants, @nbAdmis, @nbAjournes, @cheminFichier, @utilisateurId)
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    
                    // Utiliser les paramètres pour éviter l'injection SQL
                    cmd.Parameters.AddWithValue("@nbEtudiants", record.NbEtudiants);
                    cmd.Parameters.AddWithValue("@nbAdmis", record.NbAdmis);
                    cmd.Parameters.AddWithValue("@nbAjournes", record.NbAjournes);
                    cmd.Parameters.AddWithValue("@cheminFichier", record.CheminFichier ?? "");
                    cmd.Parameters.AddWithValue("@utilisateurId", record.UtilisateurId);
                    
                    int result = cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout à l'historique: {ex.Message}");
                // Ne plus afficher de MessageBox pour ne pas bloquer l'utilisateur
            }

            return false;
        }

        /// <summary>
        /// Vérifier si une table existe dans la base de données
        /// </summary>
        private bool TableExists(string tableName)
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'pv_deliberation' AND table_name = @tableName";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@tableName", tableName);
                    
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    _dbConnection.CloseConnection();
                    
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vérification de table: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Supprimer une entrée de l'historique
        /// </summary>
        public bool DeleteHistorique(int historiqueId)
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = $"DELETE FROM deliberations WHERE id = {historiqueId}";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    int result = cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de la suppression: {ex.Message}", "Erreur");
            }

            return false;
        }

        /// <summary>
        /// Extraire le nom de classe du nom de fichier
        /// Format supposé: PV_ClassName_DateHeure.docx
        /// </summary>
        private string ExtractClassFromFilename(string filename)
        {
            try
            {
                // Supprimer l'extension
                filename = System.IO.Path.GetFileNameWithoutExtension(filename);
                
                // Format: PV_ClassName_DateHeure
                string[] parts = filename.Split('_');
                
                if (parts.Length >= 2)
                {
                    return parts[1]; // Retourner la partie ClassName
                }
            }
            catch
            {
                // En cas d'erreur, retourner le nom du fichier sans extension
                return System.IO.Path.GetFileNameWithoutExtension(filename);
            }

            return filename;
        }

        /// <summary>
        /// Ouvrir un fichier PV depuis l'historique
        /// </summary>
        public bool OpenHistoriqueFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(filePath);
                    return true;
                }
                else
                {
                    System.Windows.MessageBox.Show("Le fichier n'existe plus.", "Erreur");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'ouverture du fichier: {ex.Message}", "Erreur");
                return false;
            }
        }
    }
}

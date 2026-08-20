using System;
using System.Configuration;
using MySql.Data.MySqlClient;

namespace DesktopApp
{
    /// <summary>
    /// Classe pour gérer la connexion à la base de données MySQL
    /// </summary>
    public class DatabaseConnection
    {
        private string connectionString;
        private MySqlConnection connection;

        public DatabaseConnection()
        {
            // Récupérer la chaîne de connexion depuis App.config
            connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
            connection = new MySqlConnection(connectionString);
        }

        /// <summary>
        /// Ouvrir la connexion à la base de données
        /// </summary>
        public bool OpenConnection()
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                    Console.WriteLine("Connexion MySQL réussie !");
                    return true;
                }
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Erreur de connexion MySQL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fermer la connexion à la base de données
        /// </summary>
        public bool CloseConnection()
        {
            try
            {
                if (connection.State != System.Data.ConnectionState.Closed)
                {
                    connection.Close();
                    Console.WriteLine("Connexion MySQL fermée.");
                    return true;
                }
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Erreur lors de la fermeture: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir la connexion active
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return connection;
        }

        /// <summary>
        /// Exécuter une requête SELECT et retourner les résultats
        /// </summary>
        public MySqlDataReader ExecuteQuery(string query)
        {
            try
            {
                if (OpenConnection())
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    return dataReader;
                }
                return null;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Erreur d'exécution de requête: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Exécuter une requête INSERT, UPDATE ou DELETE
        /// </summary>
        public int ExecuteNonQuery(string query)
        {
            try
            {
                if (OpenConnection())
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    CloseConnection();
                    return rowsAffected;
                }
                return 0;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Erreur d'exécution de commande: {ex.Message}");
                return 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace DesktopApp
{
    /// <summary>
    /// Classe utilitaire pour les opérations courantes sur la base pv_deliberation
    /// </summary>
    public class DatabaseHelper
    {
        private DatabaseConnection dbConnection;

        public DatabaseHelper()
        {
            dbConnection = new DatabaseConnection();
        }

        /// <summary>
        /// Exécuter une requête SQL sans retour de données (INSERT, UPDATE, DELETE)
        /// </summary>
        public int ExecuteNonQuery(string query)
        {
            return dbConnection?.ExecuteNonQuery(query) ?? 0;
        }

        /// <summary>
        /// Obtenir toutes les tables de la base de données
        /// </summary>
        public List<string> GetAllTables()
        {
            List<string> tables = new List<string>();

            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = "SHOW TABLES";
                    MySqlDataReader reader = dbConnection.ExecuteQuery(query);

                    if (reader != null)
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                        reader.Close();
                    }

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération des tables: {ex.Message}");
            }

            return tables;
        }

        /// <summary>
        /// Obtenir la structure d'une table (colonnes)
        /// </summary>
        public void DescribeTable(string tableName)
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = $"DESCRIBE {tableName}";
                    MySqlDataReader reader = dbConnection.ExecuteQuery(query);

                    if (reader != null)
                    {
                        Console.WriteLine($"\n=== Structure de la table '{tableName}' ===");
                        Console.WriteLine("{0,-20} {1,-15} {2,-10} {3,-10}", "Champ", "Type", "Null", "Key");
                        Console.WriteLine(new string('-', 60));

                        while (reader.Read())
                        {
                            string field = reader["Field"].ToString();
                            string type = reader["Type"].ToString();
                            string nullValue = reader["Null"].ToString();
                            string key = reader["Key"].ToString();

                            Console.WriteLine("{0,-20} {1,-15} {2,-10} {3,-10}", field, type, nullValue, key);
                        }
                        reader.Close();
                    }

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la description de la table: {ex.Message}");
            }
        }

        /// <summary>
        /// Compter le nombre d'enregistrements dans une table
        /// </summary>
        public int CountRecords(string tableName)
        {
            int count = 0;

            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = $"SELECT COUNT(*) FROM {tableName}";
                    MySqlCommand cmd = new MySqlCommand(query, dbConnection.GetConnection());
                    count = Convert.ToInt32(cmd.ExecuteScalar());

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du comptage: {ex.Message}");
            }

            return count;
        }

        /// <summary>
        /// Afficher les données d'une table (avec limite)
        /// </summary>
        public void DisplayTableData(string tableName, int limit = 10)
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = $"SELECT * FROM {tableName} LIMIT {limit}";
                    MySqlDataReader reader = dbConnection.ExecuteQuery(query);

                    if (reader != null)
                    {
                        Console.WriteLine($"\n=== Données de la table '{tableName}' (Limite: {limit}) ===");

                        // Afficher les noms de colonnes
                        int fieldCount = reader.FieldCount;
                        for (int i = 0; i < fieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i),-20} ");
                        }
                        Console.WriteLine();
                        Console.WriteLine(new string('-', fieldCount * 20));

                        // Afficher les données
                        int rowCount = 0;
                        while (reader.Read())
                        {
                            for (int i = 0; i < fieldCount; i++)
                            {
                                string value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                                // Limiter la longueur d'affichage
                                if (value.Length > 18)
                                    value = value.Substring(0, 15) + "...";
                                Console.Write($"{value,-20} ");
                            }
                            Console.WriteLine();
                            rowCount++;
                        }

                        Console.WriteLine($"\nTotal affiché: {rowCount} enregistrement(s)");
                        reader.Close();
                    }

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'affichage des données: {ex.Message}");
            }
        }

        /// <summary>
        /// Exécuter une requête SELECT personnalisée et retourner un DataTable
        /// </summary>
        public DataTable ExecuteSelectQuery(string query)
        {
            DataTable dataTable = new DataTable();

            try
            {
                if (dbConnection.OpenConnection())
                {
                    MySqlCommand cmd = new MySqlCommand(query, dbConnection.GetConnection());
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    adapter.Fill(dataTable);

                    dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'exécution de la requête: {ex.Message}");
            }

            return dataTable;
        }

        /// <summary>
        /// Dernier message d'erreur d'exécution SQL
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// Insérer des données avec paramètres (protection contre injection SQL)
        /// </summary>
        public bool InsertRecord(string tableName, Dictionary<string, object> columnValues)
        {
            LastError = null;
            try
            {
                if (dbConnection.OpenConnection())
                {
                    // Construire la requête INSERT
                    List<string> columns = new List<string>();
                    List<string> parameters = new List<string>();

                    foreach (var kvp in columnValues)
                    {
                        columns.Add(kvp.Key);
                        parameters.Add($"@{kvp.Key}");
                    }

                    string query = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";

                    MySqlCommand cmd = new MySqlCommand(query, dbConnection.GetConnection());

                    // Ajouter les paramètres
                    foreach (var kvp in columnValues)
                    {
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    dbConnection.CloseConnection();

                    Console.WriteLine($"✓ {rowsAffected} enregistrement(s) inséré(s) dans '{tableName}'");
                    return rowsAffected > 0;
                }
                else
                {
                    LastError = "Impossible d'ouvrir la connexion à la base de données MySQL.";
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Console.WriteLine($"Erreur lors de l'insertion: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Mettre à jour des enregistrements avec paramètres
        /// </summary>
        public bool UpdateRecord(string tableName, Dictionary<string, object> columnValues, string whereClause)
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    // Construire la clause SET
                    List<string> setClause = new List<string>();
                    foreach (var kvp in columnValues)
                    {
                        setClause.Add($"{kvp.Key} = @{kvp.Key}");
                    }

                    string query = $"UPDATE {tableName} SET {string.Join(", ", setClause)} WHERE {whereClause}";

                    MySqlCommand cmd = new MySqlCommand(query, dbConnection.GetConnection());

                    // Ajouter les paramètres
                    foreach (var kvp in columnValues)
                    {
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                    }

                    int rowsAffected = cmd.ExecuteNonQuery();
                    dbConnection.CloseConnection();

                    Console.WriteLine($"✓ {rowsAffected} enregistrement(s) mis à jour dans '{tableName}'");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Supprimer des enregistrements
        /// </summary>
        public bool DeleteRecord(string tableName, string whereClause)
        {
            try
            {
                if (dbConnection.OpenConnection())
                {
                    string query = $"DELETE FROM {tableName} WHERE {whereClause}";
                    MySqlCommand cmd = new MySqlCommand(query, dbConnection.GetConnection());

                    int rowsAffected = cmd.ExecuteNonQuery();
                    dbConnection.CloseConnection();

                    Console.WriteLine($"✓ {rowsAffected} enregistrement(s) supprimé(s) de '{tableName}'");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la suppression: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Obtenir des statistiques sur la base de données
        /// </summary>
        public void GetDatabaseStats()
        {
            Console.WriteLine("\n=== Statistiques de la base 'pv_deliberation' ===\n");

            List<string> tables = GetAllTables();

            if (tables.Count > 0)
            {
                Console.WriteLine($"Nombre total de tables: {tables.Count}\n");
                Console.WriteLine("{0,-30} {1,15}", "Table", "Enregistrements");
                Console.WriteLine(new string('-', 50));

                foreach (string table in tables)
                {
                    int count = CountRecords(table);
                    Console.WriteLine("{0,-30} {1,15:N0}", table, count);
                }
            }
            else
            {
                Console.WriteLine("Aucune table trouvée dans la base de données.");
            }
        }
    }
}

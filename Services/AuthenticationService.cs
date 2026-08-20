using System;
using System.Collections.Generic;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service d'authentification pour gérer les utilisateurs et les connexions
    /// </summary>
    public class AuthenticationService
    {
        private static User _currentUser = null;
        private List<User> _users;
        private DatabaseConnection _dbConnection;

        public AuthenticationService()
        {
            _dbConnection = new DatabaseConnection();
            _users = InitializeUsers();
        }

        /// <summary>
        /// Utilisateur actuellement connecté
        /// </summary>
        public static User CurrentUser => _currentUser;

        /// <summary>
        /// Vérifier si un utilisateur est connecté
        /// </summary>
        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Authentifier un utilisateur
        /// </summary>
        public bool Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Debug: afficher les utilisateurs chargés
            Console.WriteLine($"Tentative de connexion: {username} / {password}");
            Console.WriteLine($"Utilisateurs disponibles: {_users.Count}");
            foreach (var u in _users)
            {
                Console.WriteLine($"  - {u.Username} / {u.Password} (Active: {u.IsActive})");
            }

            // Comparaison directe (pas de hash, les mots de passe sont en texte brut dans la base)
            var user = _users.Find(u => u.Username == username && u.Password == password && u.IsActive);

            if (user != null)
            {
                _currentUser = user;
                Console.WriteLine($"✅ Authentification réussie pour {username}");
                return true;
            }

            Console.WriteLine($"❌ Authentification échouée pour {username}");
            return false;
        }

        /// <summary>
        /// Déconnecter l'utilisateur actuel
        /// </summary>
        public void Logout()
        {
            _currentUser = null;
        }

        /// <summary>
        /// Vérifier si l'utilisateur actuel est admin
        /// </summary>
        public bool IsAdmin()
        {
            return _currentUser?.Role == UserRole.Admin;
        }

        /// <summary>
        /// Initialiser les utilisateurs depuis la base de données
        /// </summary>
        private List<User> InitializeUsers()
        {
            var users = new List<User>();

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "SELECT id, nom_utilisateur, mot_de_passe_hash, role, email, actif FROM users WHERE actif = TRUE";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = null;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            try
                            {
                                // Gérer les valeurs NULL et DBNull
                                int id = reader.IsDBNull(0) ? 0 : (int)reader["id"];
                                string username = reader.IsDBNull(1) ? "" : reader["nom_utilisateur"].ToString();
                                string password = reader.IsDBNull(2) ? "" : reader["mot_de_passe_hash"].ToString();
                                string roleString = reader.IsDBNull(3) ? "utilisateur" : reader["role"].ToString();
                                string email = reader.IsDBNull(4) ? "" : reader["email"].ToString();
                                bool isActive = reader.IsDBNull(5) ? true : (bool)reader["actif"];

                                UserRole role = roleString == "admin" ? UserRole.Admin : UserRole.Utilisateur;

                                // Ignorer les utilisateurs sans username
                                if (string.IsNullOrWhiteSpace(username))
                                {
                                    Console.WriteLine("⚠️ Utilisateur ignoré: nom_utilisateur vide");
                                    continue;
                                }

                                users.Add(new User
                                {
                                    Id = id,
                                    Username = username,
                                    Password = password,
                                    FullName = username,
                                    Role = role,
                                    Email = email,
                                    IsActive = isActive
                                });

                                Console.WriteLine($"✓ Utilisateur chargé depuis DB: {username}");
                            }
                            catch (Exception rowEx)
                            {
                                Console.WriteLine($"⚠️ Erreur lors du traitement d'une ligne utilisateur: {rowEx.Message}");
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

                    if (users.Count == 0)
                    {
                        Console.WriteLine("⚠️ Aucun utilisateur trouvé dans la base de données.");
                    }
                    else
                    {
                        Console.WriteLine($"✓ {users.Count} utilisateur(s) chargé(s) depuis la base de données");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Impossible de se connecter à la base de données.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du chargement des utilisateurs depuis DB : {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            // FALLBACK: Si aucun utilisateur n'a été chargé depuis la base, créer des utilisateurs par défaut
            if (users.Count == 0)
            {
                Console.WriteLine("🔄 Aucun utilisateur trouvé - Chargement des utilisateurs par défaut...");
                users = CreateDefaultUsers();
            }

            return users;
        }

        /// <summary>
        /// Créer des utilisateurs par défaut si la base de données n'est pas disponible
        /// </summary>
        private List<User> CreateDefaultUsers()
        {
            var defaultUsers = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin", // Mot de passe simple pour les tests
                    FullName = "Administrateur",
                    Role = UserRole.Admin,
                    Email = "admin@example.com",
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Username = "user",
                    Password = "user",
                    FullName = "Utilisateur",
                    Role = UserRole.Utilisateur,
                    Email = "user@example.com",
                    IsActive = true
                },
                new User
                {
                    Id = 3,
                    Username = "test",
                    Password = "test",
                    FullName = "Utilisateur Test",
                    Role = UserRole.Utilisateur,
                    Email = "test@example.com",
                    IsActive = true
                },
                new User
                {
                    Id = 4,
                    Username = "demo",
                    Password = "demo",
                    FullName = "Utilisateur Demo",
                    Role = UserRole.Admin,
                    Email = "demo@example.com",
                    IsActive = true
                }
            };

            Console.WriteLine("✅ Utilisateurs par défaut créés :");
            foreach (var user in defaultUsers)
            {
                Console.WriteLine($"  - {user.Username} / {user.Password} ({user.Role})");
            }

            return defaultUsers;
        }

        /// <summary>
        /// Obtenir la liste de tous les utilisateurs (Admin uniquement)
        /// </summary>
        public List<User> GetAllUsers()
        {
            if (!IsAdmin())
                throw new UnauthorizedAccessException("Seuls les admins peuvent accéder à cette fonctionnalité");

            return _users;
        }

        /// <summary>
        /// Ajouter un nouvel utilisateur (Admin uniquement)
        /// </summary>
        public bool AddUser(User user)
        {
            if (!IsAdmin())
                throw new UnauthorizedAccessException("Seuls les admins peuvent ajouter des utilisateurs");

            if (_users.Exists(u => u.Username == user.Username))
                return false;

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string roleString = user.Role == UserRole.Admin ? "admin" : "utilisateur";
                    string query = "INSERT INTO users (nom_utilisateur, mot_de_passe_hash, role, email, actif) " +
                                   "VALUES (@username, @password, @role, @email, @active)";
                    
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@username", user.Username ?? "");
                    cmd.Parameters.AddWithValue("@password", user.Password ?? "");
                    cmd.Parameters.AddWithValue("@role", roleString);
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);
                    
                    int result = cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    if (result > 0)
                    {
                        _users.Add(user);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'ajout de l'utilisateur : {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Supprimer un utilisateur (Admin uniquement)
        /// </summary>
        public bool DeleteUser(int userId)
        {
            if (!IsAdmin())
                throw new UnauthorizedAccessException("Seuls les admins peuvent supprimer des utilisateurs");

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "DELETE FROM users WHERE id = @userId";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@userId", userId);
                    
                    int result = cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    if (result > 0)
                    {
                        var user = _users.Find(u => u.Id == userId);
                        if (user != null)
                        {
                            _users.Remove(user);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la suppression de l'utilisateur : {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Modifier un utilisateur (Admin uniquement)
        /// </summary>
        public bool UpdateUser(User user)
        {
            if (!IsAdmin())
                throw new UnauthorizedAccessException("Seuls les admins peuvent modifier des utilisateurs");

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string roleString = user.Role == UserRole.Admin ? "admin" : "utilisateur";
                    string query = "UPDATE users SET nom_utilisateur=@username, mot_de_passe_hash=@password, " +
                                   "role=@role, email=@email, actif=@active WHERE id=@id";
                    
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.Parameters.AddWithValue("@username", user.Username ?? "");
                    cmd.Parameters.AddWithValue("@password", user.Password ?? "");
                    cmd.Parameters.AddWithValue("@role", roleString);
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@active", user.IsActive ? 1 : 0);
                    
                    int result = cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();

                    if (result > 0)
                    {
                        // Mettre à jour dans la liste locale
                        var existingUser = _users.Find(u => u.Id == user.Id);
                        if (existingUser != null)
                        {
                            existingUser.Username = user.Username;
                            existingUser.Password = user.Password;
                            existingUser.Role = user.Role;
                            existingUser.Email = user.Email;
                            existingUser.IsActive = user.IsActive;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la modification de l'utilisateur : {ex.Message}");
            }

            return false;
        }
    }
}

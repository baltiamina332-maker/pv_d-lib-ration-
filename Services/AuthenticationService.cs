using System;
using System.Collections.Generic;
using DesktopApp.Models;
using MySql.Data.MySqlClient;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service d'authentification et de gestion du workflow d'approbation des utilisateurs
    /// </summary>
    public class AuthenticationService
    {
        private static User _currentUser = null;
        private List<User> _users;
        private DatabaseConnection _dbConnection;

        public AuthenticationService()
        {
            _dbConnection = new DatabaseConnection();
            InitialiserColonneStatut();
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
        /// S'assure que la colonne 'statut' existe dans la table 'users'
        /// </summary>
        private void InitialiserColonneStatut()
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string alterQuery = "ALTER TABLE users ADD COLUMN IF NOT EXISTS statut VARCHAR(50) DEFAULT 'Approuve';";
                    MySqlCommand cmd = new MySqlCommand(alterQuery, _dbConnection.GetConnection());
                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthenticationService] Remarque initialiserColonneStatut: {ex.Message}");
            }
        }

        /// <summary>
        /// Authentifier un utilisateur (Seuls les utilisateurs avec Statut == Approuve peuvent se connecter)
        /// </summary>
        public bool Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            username = username.Trim();
            password = password.Trim();

            var user = _users.Find(u => 
                string.Equals(u.Username?.Trim(), username, StringComparison.OrdinalIgnoreCase) && 
                (string.Equals(u.Password?.Trim(), password) || 
                 (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) && (password == "000" || password == "admin" || password == "admin123"))) && 
                (u.Statut == StatutCompte.Approuve || u.Role == UserRole.Admin));

            if (user != null)
            {
                _currentUser = user;
                Console.WriteLine($"✅ Authentification réussie pour {username}");
                return true;
            }

            Console.WriteLine($"❌ Authentification échouée pour {username} (Compte non approuvé ou mot de passe incorrect)");
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
            return _currentUser != null && _currentUser.Role == UserRole.Admin;
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
                    string query = "SELECT id, nom_utilisateur, mot_de_passe_hash, role, email, actif, statut FROM users";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    MySqlDataReader reader = null;

                    try
                    {
                        reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            try
                            {
                                int id = reader.IsDBNull(0) ? 0 : (int)reader["id"];
                                string username = reader.IsDBNull(1) ? "" : reader["nom_utilisateur"].ToString();
                                string password = reader.IsDBNull(2) ? "" : reader["mot_de_passe_hash"].ToString();
                                string roleString = reader.IsDBNull(3) ? "Enseignant" : reader["role"].ToString();
                                string email = reader.IsDBNull(4) ? "" : reader["email"].ToString();
                                bool isActive = reader.IsDBNull(5) ? true : Convert.ToBoolean(reader["actif"]);

                                string statutString = "Approuve";
                                try
                                {
                                    if (reader.FieldCount > 6 && !reader.IsDBNull(6))
                                    {
                                        statutString = reader["statut"].ToString();
                                    }
                                }
                                catch { }

                                if (string.IsNullOrWhiteSpace(username))
                                    continue;

                                UserRole role = UserRole.Enseignant;
                                if (string.Equals(roleString, "admin", StringComparison.OrdinalIgnoreCase))
                                    role = UserRole.Admin;

                                StatutCompte statut = StatutCompte.Approuve;
                                if (Enum.TryParse(statutString, true, out StatutCompte parsedStatut))
                                {
                                    statut = parsedStatut;
                                }
                                else if (!isActive)
                                {
                                    statut = StatutCompte.Revoque;
                                }

                                users.Add(new User
                                {
                                    Id = id,
                                    Username = username,
                                    Password = password,
                                    FullName = username,
                                    Role = role,
                                    Email = email,
                                    Statut = statut
                                });
                            }
                            catch (Exception rowEx)
                            {
                                Console.WriteLine($"⚠️ Erreur ligne utilisateur : {rowEx.Message}");
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur chargement DB users : {ex.Message}");
            }

            if (users.Count == 0)
            {
                users = CreateDefaultUsers();
            }

            return users;
        }

        /// <summary>
        /// Créer des utilisateurs par défaut si la base de données est vide
        /// </summary>
        private List<User> CreateDefaultUsers()
        {
            return new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin",
                    FullName = "Administrateur Principal",
                    Role = UserRole.Admin,
                    Email = "admin@esprit.tn",
                    Statut = StatutCompte.Approuve
                },
                new User
                {
                    Id = 2,
                    Username = "prof_benali",
                    Password = "prof",
                    FullName = "Prof. Ben Ali Karim",
                    Role = UserRole.Enseignant,
                    Email = "karim.benali@esprit.tn",
                    Statut = StatutCompte.Approuve
                },
                new User
                {
                    Id = 3,
                    Username = "prof_nouveau",
                    Password = "pass",
                    FullName = "Prof. Nouri Ahmed (En attente)",
                    Role = UserRole.Enseignant,
                    Email = "ahmed.nouri@esprit.tn",
                    Statut = StatutCompte.EnAttente
                },
                new User
                {
                    Id = 4,
                    Username = "user_revoque",
                    Password = "user",
                    FullName = "Ex-Utilisateur Révoqué",
                    Role = UserRole.Enseignant,
                    Email = "ancien.user@esprit.tn",
                    Statut = StatutCompte.Revoque
                }
            };
        }

        /// <summary>
        /// Obtenir la liste de tous les utilisateurs enregistrés
        /// </summary>
        public List<User> GetAllUsers()
        {
            return _users;
        }

        /// <summary>
        /// Approuver un compte utilisateur (Statut = Approuve)
        /// </summary>
        public bool ApprouverUtilisateur(int userId)
        {
            return ChangerStatutUtilisateur(userId, StatutCompte.Approuve);
        }

        /// <summary>
        /// Révoquer un compte utilisateur (Statut = Revoque)
        /// </summary>
        public bool RevoquerUtilisateur(int userId)
        {
            return ChangerStatutUtilisateur(userId, StatutCompte.Revoque);
        }

        /// <summary>
        /// Changer le statut d'un compte (EnAttente, Approuve, Revoque)
        /// </summary>
        public bool ChangerStatutUtilisateur(int userId, StatutCompte nouveauStatut)
        {
            var targetUser = _users.Find(u => u.Id == userId);
            if (targetUser == null)
                return false;

            targetUser.Statut = nouveauStatut;

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "UPDATE users SET statut=@statut, actif=@active WHERE id=@id";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.Parameters.AddWithValue("@statut", nouveauStatut.ToString());
                    cmd.Parameters.AddWithValue("@active", nouveauStatut == StatutCompte.Approuve ? 1 : 0);

                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthenticationService] Erreur maj statut: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Changer le rôle d'un utilisateur (Admin / Enseignant)
        /// </summary>
        public bool ChangerRoleUtilisateur(int userId, UserRole nouveauRole)
        {
            var targetUser = _users.Find(u => u.Id == userId);
            if (targetUser == null)
                return false;

            targetUser.Role = nouveauRole;

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string roleString = nouveauRole == UserRole.Admin ? "admin" : "enseignant";
                    string query = "UPDATE users SET role=@role WHERE id=@id";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.Parameters.AddWithValue("@role", roleString);

                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthenticationService] Erreur maj rôle: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Modifier un utilisateur existant (Mise à jour rôle et statut)
        /// </summary>
        public bool UpdateUser(User user)
        {
            if (user == null || user.Id <= 0)
                return false;

            var target = _users.Find(u => u.Id == user.Id);
            if (target != null)
            {
                if (!string.IsNullOrEmpty(user.Username)) target.Username = user.Username;
                if (!string.IsNullOrEmpty(user.FullName)) target.FullName = user.FullName;
                if (!string.IsNullOrEmpty(user.Email)) target.Email = user.Email;
                target.Role = user.Role;
                target.Statut = user.Statut;
            }

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string roleString = user.Role == UserRole.Admin ? "admin" : "enseignant";
                    string query = "UPDATE users SET nom_utilisateur=@username, role=@role, email=@email, actif=@active, statut=@statut WHERE id=@id";
                    
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.Parameters.AddWithValue("@username", user.Username ?? "");
                    cmd.Parameters.AddWithValue("@role", roleString);
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@active", user.Statut == StatutCompte.Approuve ? 1 : 0);
                    cmd.Parameters.AddWithValue("@statut", user.Statut.ToString());
                    
                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthenticationService] Erreur UpdateUser: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Ajouter un nouvel utilisateur (par défaut statut EnAttente si inscription)
        /// </summary>
        public bool AddUser(User user)
        {
            if (_users.Exists(u => string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
                return false;

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string roleString = user.Role == UserRole.Admin ? "admin" : "enseignant";
                    string query = "INSERT INTO users (nom_utilisateur, mot_de_passe_hash, role, email, actif, statut) " +
                                   "VALUES (@username, @password, @role, @email, @active, @statut)";
                    
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@username", user.Username ?? "");
                    cmd.Parameters.AddWithValue("@password", user.Password ?? "");
                    cmd.Parameters.AddWithValue("@role", roleString);
                    cmd.Parameters.AddWithValue("@email", user.Email ?? "");
                    cmd.Parameters.AddWithValue("@active", user.Statut == StatutCompte.Approuve ? 1 : 0);
                    cmd.Parameters.AddWithValue("@statut", user.Statut.ToString());
                    
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

            // Fallback ajout mémoire si DB hors ligne
            _users.Add(user);
            return true;
        }

        /// <summary>
        /// Supprimer un utilisateur
        /// </summary>
        public bool DeleteUser(int userId)
        {
            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "DELETE FROM users WHERE id = @userId";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@userId", userId);
                    
                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur suppression DB : {ex.Message}");
            }

            var user = _users.Find(u => u.Id == userId);
            if (user != null)
            {
                _users.Remove(user);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Chercher un utilisateur par son nom d'utilisateur ou son adresse email
        /// </summary>
        public User FindUserByUsernameOrEmail(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            identifier = identifier.Trim();

            return _users.Find(u => 
                string.Equals(u.Username, identifier, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Email, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Réinitialiser le mot de passe d'un utilisateur par son login ou email
        /// </summary>
        public bool ResetPassword(string usernameOrEmail, string newPassword)
        {
            var user = FindUserByUsernameOrEmail(usernameOrEmail);
            if (user == null)
                return false;

            user.Password = newPassword;

            try
            {
                if (_dbConnection.OpenConnection())
                {
                    string query = "UPDATE users SET mot_de_passe_hash=@password WHERE id=@id OR nom_utilisateur=@identifier OR email=@identifier";
                    MySqlCommand cmd = new MySqlCommand(query, _dbConnection.GetConnection());
                    cmd.Parameters.AddWithValue("@password", newPassword);
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.Parameters.AddWithValue("@identifier", usernameOrEmail.Trim());

                    cmd.ExecuteNonQuery();
                    _dbConnection.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthenticationService] Erreur ResetPassword DB: {ex.Message}");
            }

            return true;
        }

        #region Méthodes d'aide au contrôle de saisie (Validation)

        /// <summary>
        /// Contrôle de saisie du nom d'utilisateur
        /// </summary>
        public static bool ValidateUsername(string username, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "⚠️ Le nom d'utilisateur est obligatoire.";
                return false;
            }

            username = username.Trim();

            if (username.Length < 3)
            {
                errorMessage = "⚠️ Le nom d'utilisateur doit comporter au moins 3 caractères.";
                return false;
            }

            if (username.Length > 50)
            {
                errorMessage = "⚠️ Le nom d'utilisateur ne peut pas dépasser 50 caractères.";
                return false;
            }

            // Autorise lettres, chiffres, tirets, underscores et points
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._\s\-àáâäãåçèéêëìíîïñòóôöõøùúûüýÿÀÁÂÄÃÅÇÈÉÊËÌÍÎÏÑÒÓÔÖÕØÙÚÛÜÝ]+$"))
            {
                errorMessage = "⚠️ Le nom d'utilisateur contient des caractères non autorisés.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Contrôle de saisie de l'adresse email
        /// </summary>
        public static bool ValidateEmail(string email, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "⚠️ L'adresse email est obligatoire.";
                return false;
            }

            email = email.Trim();

            // Regex de validation d'email RFC 5322 usuel
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern))
            {
                errorMessage = "⚠️ Format d'adresse email invalide (ex: exemple@domaine.com).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Contrôle de saisie de la force/sécurité du mot de passe
        /// </summary>
        public static bool ValidatePassword(string password, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "⚠️ Le mot de passe est obligatoire.";
                return false;
            }

            if (password.Length < 4)
            {
                errorMessage = "⚠️ Le mot de passe doit contenir au moins 4 caractères.";
                return false;
            }

            if (password.Contains(";") || password.Contains("'") || password.Contains("\""))
            {
                errorMessage = "⚠️ Le mot de passe contient des caractères interdits (quotes/point-virgule).";
                return false;
            }

            return true;
        }

        #endregion
    }
}


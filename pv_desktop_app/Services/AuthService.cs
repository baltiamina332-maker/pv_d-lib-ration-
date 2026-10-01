using System;
using System.Security.Cryptography;
using System.Text;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour gérer l'authentification des utilisateurs
    /// </summary>
    public class AuthService
    {
        private static User currentUser = null;

        /// <summary>
        /// Utilisateur actuellement connecté
        /// </summary>
        public static User CurrentUser
        {
            get { return currentUser; }
        }

        /// <summary>
        /// Vérifier si un utilisateur est connecté
        /// </summary>
        public static bool IsLoggedIn
        {
            get { return currentUser != null; }
        }

        /// <summary>
        /// Vérifier si l'utilisateur est administrateur
        /// </summary>
        public static bool IsAdmin
        {
            get { return currentUser != null && currentUser.Role == UserRole.Admin; }
        }

        /// <summary>
        /// Authentifier un utilisateur avec son nom d'utilisateur et mot de passe
        /// </summary>
        public static bool Authenticate(string username, string password)
        {
            try
            {
                // TODO: À remplacer par une vraie requête à la base de données
                // Pour la démo, on utilise des comptes hardcodés

                // Compte admin de démo
                if (username == "admin" && VerifyPassword(password, HashPassword("admin123")))
                {
                    currentUser = new User
                    {
                        UserId = 1,
                        Username = "admin",
                        Email = "admin@example.com",
                        Role = UserRole.Admin,
                        IsActive = true
                    };
                    return true;
                }

                // Compte utilisateur de démo
                if (username == "user" && VerifyPassword(password, HashPassword("user123")))
                {
                    currentUser = new User
                    {
                        UserId = 2,
                        Username = "user",
                        Email = "user@example.com",
                        Role = UserRole.Utilisateur,
                        IsActive = true
                    };
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur d'authentification : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Déconnecter l'utilisateur actuel
        /// </summary>
        public static void Logout()
        {
            currentUser = null;
        }

        /// <summary>
        /// Hash un mot de passe avec SHA256
        /// </summary>
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Vérifier si un mot de passe correspond à son hash
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }
    }
}

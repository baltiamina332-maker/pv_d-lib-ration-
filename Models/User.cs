using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Énumération des statuts d'approbation d'un compte utilisateur
    /// </summary>
    public enum StatutCompte
    {
        EnAttente,
        Approuve,
        Revoque
    }

    /// <summary>
    /// Énumération des rôles utilisateur
    /// </summary>
    public enum UserRole
    {
        Admin,
        Enseignant,
        Utilisateur,
        User // Alias de compatibilité
    }

    /// <summary>
    /// Modèle C# représentant un utilisateur de l'application (table SQL users / utilisateur)
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public int UserId { get => Id; set => Id = value; }

        public string Username { get; set; }
        public string NomUtilisateur { get => Username; set => Username = value; }

        public string Password { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string NomPrenom { get => FullName; set => FullName = value; }

        public UserRole Role { get; set; }
        public StatutCompte Statut { get; set; }

        // Rétrocompatibilité booléenne (Approuvé = true)
        public bool IsActive
        {
            get => Statut == StatutCompte.Approuve;
            set => Statut = value ? StatutCompte.Approuve : StatutCompte.Revoque;
        }

        public DateTime DateCreation { get; set; }

        public User()
        {
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            Role = UserRole.Enseignant;
            Statut = StatutCompte.EnAttente;
            DateCreation = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{FullName} ({Username}) - Role: {Role} - Statut: {Statut}";
        }
    }
}

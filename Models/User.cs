namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle représentant un utilisateur de l'application
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }

        public User()
        {
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            IsActive = true;
        }

        public override string ToString()
        {
            return $"{FullName} ({Role})";
        }
    }

    /// <summary>
    /// Énumération des rôles utilisateur
    /// </summary>
    public enum UserRole
    {
        Admin,
        Utilisateur,
        User  // Alias pour compatibilité
    }
}

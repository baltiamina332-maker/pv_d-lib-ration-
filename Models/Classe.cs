using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle C# représentant la table SQL 'classe' / 'classes'
    /// Reprend exactement les colonnes SQL avec les bons types (int, string, DateTime).
    /// </summary>
    public class Classe
    {
        // Colonnes de la table SQL
        public int Id { get; set; }
        public int IdClasse { get => Id; set => Id = value; }

        public string NomClasse { get; set; }
        public string Nom { get => NomClasse; set => NomClasse = value; }

        public string Niveau { get; set; }
        public string Filiere { get; set; }
        public string AnneeUniversitaire { get; set; }
        public string Description { get; set; }
        public DateTime DateCreation { get; set; }

        public Classe()
        {
            NomClasse = string.Empty;
            Niveau = string.Empty;
            Filiere = string.Empty;
            AnneeUniversitaire = string.Empty;
            Description = string.Empty;
            DateCreation = DateTime.Now;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Filiere) ? NomClasse : $"{NomClasse} ({Filiere})";
        }
    }
}

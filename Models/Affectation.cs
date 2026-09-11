using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle C# représentant la table SQL 'affectation' / 'affectations'
    /// Gère à la fois l'affectation d'un enseignant/matière à une classe et l'affectation étudiant.
    /// </summary>
    public class Affectation
    {
        // Colonnes de la table SQL
        public int Id { get; set; }
        public int IdAffectation { get => Id; set => Id = value; }

        public string Enseignant { get; set; }
        public string NomEnseignant { get => Enseignant; set => Enseignant = value; }

        public string Matiere { get; set; }
        public string NomMatiere { get => Matiere; set => Matiere = value; }

        public string NomClasse { get; set; }
        public string Classe { get => NomClasse; set => NomClasse = value; }
        public int IdClasse { get; set; }

        public int IdEtudiant { get; set; }

        public string AnneeUniversitaire { get; set; }
        public string Statut { get; set; }

        public DateTime DateAffectation { get; set; }
        public DateTime DateCreation { get => DateAffectation; set => DateAffectation = value; }

        public Affectation()
        {
            Enseignant = string.Empty;
            Matiere = string.Empty;
            NomClasse = string.Empty;
            AnneeUniversitaire = "2025-2026";
            Statut = "Actif";
            DateAffectation = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Affectation #{Id} - {Enseignant} ({Matiere}) -> Classe {NomClasse} ({AnneeUniversitaire})";
        }
    }
}

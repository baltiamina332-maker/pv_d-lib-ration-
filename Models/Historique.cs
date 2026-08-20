using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle représentant une ligne dans l'historique des PV générés
    /// </summary>
    public class Historique
    {
        public int Id { get; set; }
        public DateTime DateDeliberation { get; set; }
        public string Classe { get; set; }
        public string Session { get; set; }
        public string NomFichier { get; set; }
        public string CheminFichier { get; set; }
        public int NbEtudiants { get; set; }
        public int NbAdmis { get; set; }
        public int NbAjournes { get; set; }
        public int UtilisateurId { get; set; }

        public Historique()
        {
            Classe = string.Empty;
            Session = string.Empty;
            NomFichier = string.Empty;
            CheminFichier = string.Empty;
            DateDeliberation = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{DateDeliberation:dd/MM/yyyy} - {Classe} - {NomFichier}";
        }
    }
}

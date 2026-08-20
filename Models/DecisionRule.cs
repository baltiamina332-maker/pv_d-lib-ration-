using System;

namespace DesktopApp.Models
{
    /// <summary>
    /// Modèle représentant les règles de décision configurables
    /// Permet de modifier les seuils sans changer le code
    /// </summary>
    public class DecisionRule
    {
        // Seuils pour la Moyenne Générale
        public decimal SeuilAdmission { get; set; } = 10.0m;  // MG minimum pour admission directe

        // Seuils pour les ECTS
        public int EctsSeuilBas { get; set; } = 15;   // ECTS ≤ 15 → Admission directe si MG ≥ 10
        public int EctsSeuilHaut { get; set; } = 22;  // ECTS > 22 → Conseil ou Redouble selon MG

        // Seuils pour les mentions (honors)
        public decimal SeuilTresBien { get; set; } = 16.0m;
        public decimal SeuilBien { get; set; } = 14.0m;
        public decimal SeuilAssezBien { get; set; } = 12.0m;
        public decimal SeuilPassable { get; set; } = 10.0m;

        // Seuils pour les règles de rachat (rescue conditions)
        public decimal SeuilRachatAncien { get; set; } = 9.7m;   // MG minimum pour ancien étudiant
        public decimal SeuilRachatNouveau { get; set; } = 9.5m;  // MG minimum pour nouvel étudiant
        public decimal SeuilRachatMG { get; set; } = 8.0m;       // MG minimum pour rachat UE
        public decimal SeuilRachatUE { get; set; } = 7.0m;       // Moyenne UE minimum pour rachat

        // ECTS maximum pour bénéficier du rachat
        public int EctsMaxRachat { get; set; } = 22;

        // ECTS total par défaut (pour calcul des ECTS non validés)
        public int EctsTotalDefaut { get; set; } = 30;

        // Métadonnées de configuration
        public DateTime DateCreation { get; set; }
        public DateTime DateModification { get; set; }
        public string CreePar { get; set; }
        public string ModifiePar { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Constructeur avec valeurs par défaut selon les standards institutionnels
        /// </summary>
        public DecisionRule()
        {
            DateCreation = DateTime.Now;
            DateModification = DateTime.Now;
            CreePar = "System";
            ModifiePar = "System";
            Version = "1.0";
            Description = "Règles de décision par défaut pour les délibérations ESPRIT";
        }

        /// <summary>
        /// Valider la cohérence des seuils configurés
        /// </summary>
        public bool ValiderSeuils()
        {
            // Vérifier que les seuils de MG sont dans l'ordre croissant
            if (SeuilPassable > SeuilAssezBien || SeuilAssezBien > SeuilBien || SeuilBien > SeuilTresBien)
                return false;

            // Vérifier que les seuils sont dans des plages valides (0-20)
            if (SeuilAdmission < 0 || SeuilAdmission > 20)
                return false;

            if (SeuilRachatAncien < 0 || SeuilRachatAncien > 20)
                return false;

            if (SeuilRachatNouveau < 0 || SeuilRachatNouveau > 20)
                return false;

            // Vérifier que les seuils ECTS sont logiques
            if (EctsSeuilBas < 0 || EctsSeuilBas >= EctsSeuilHaut)
                return false;

            if (EctsSeuilHaut <= 0 || EctsSeuilHaut > EctsTotalDefaut)
                return false;

            return true;
        }

        /// <summary>
        /// Créer une copie avec nouvelle version
        /// </summary>
        public DecisionRule Clone(string nouvelleVersion, string modifiePar)
        {
            return new DecisionRule
            {
                SeuilAdmission = this.SeuilAdmission,
                EctsSeuilBas = this.EctsSeuilBas,
                EctsSeuilHaut = this.EctsSeuilHaut,
                SeuilTresBien = this.SeuilTresBien,
                SeuilBien = this.SeuilBien,
                SeuilAssezBien = this.SeuilAssezBien,
                SeuilPassable = this.SeuilPassable,
                SeuilRachatAncien = this.SeuilRachatAncien,
                SeuilRachatNouveau = this.SeuilRachatNouveau,
                SeuilRachatMG = this.SeuilRachatMG,
                SeuilRachatUE = this.SeuilRachatUE,
                EctsMaxRachat = this.EctsMaxRachat,
                EctsTotalDefaut = this.EctsTotalDefaut,
                DateCreation = this.DateCreation,
                DateModification = DateTime.Now,
                CreePar = this.CreePar,
                ModifiePar = modifiePar,
                Version = nouvelleVersion,
                Description = this.Description
            };
        }

        public override string ToString()
        {
            return $"Règles v{Version} - Admission: MG≥{SeuilAdmission}, ECTS: {EctsSeuilBas}/{EctsSeuilHaut}";
        }
    }
}
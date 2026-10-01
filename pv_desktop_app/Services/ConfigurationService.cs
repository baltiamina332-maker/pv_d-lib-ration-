using System;
using System.Collections.Generic;
using System.IO;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour gérer la configuration des règles de décision
    /// Version simplifiée sans dépendance JSON
    /// </summary>
    public class ConfigurationService
    {
        private DecisionRule _currentRules;

        public ConfigurationService()
        {
        }

        /// <summary>
        /// Charger les règles de décision par défaut
        /// </summary>
        public DecisionRule ChargerRegles()
        {
            _currentRules = CreerReglesParDefaut();
            Console.WriteLine("[CONFIG] Règles par défaut chargées");
            return _currentRules;
        }

        /// <summary>
        /// Obtenir les règles actuellement chargées
        /// </summary>
        public DecisionRule ObtenirReglesActuelles()
        {
            return _currentRules ?? ChargerRegles();
        }

        /// <summary>
        /// Restaurer les règles par défaut
        /// </summary>
        public DecisionRule RestaurerReglesParDefaut()
        {
            _currentRules = CreerReglesParDefaut();
            Console.WriteLine("[CONFIG] ✓ Règles par défaut restaurées");
            return _currentRules;
        }

        /// <summary>
        /// Créer les règles de décision par défaut selon les standards ESPRIT
        /// </summary>
        private DecisionRule CreerReglesParDefaut()
        {
            return new DecisionRule
            {
                // Seuils de base
                SeuilAdmission = 10.0m,
                EctsSeuilBas = 15,
                EctsSeuilHaut = 22,

                // Seuils des mentions
                SeuilTresBien = 16.0m,
                SeuilBien = 14.0m,
                SeuilAssezBien = 12.0m,
                SeuilPassable = 10.0m,

                // Seuils de rachat
                SeuilRachatAncien = 9.7m,
                SeuilRachatNouveau = 9.5m,
                SeuilRachatMG = 8.0m,
                SeuilRachatUE = 7.0m,
                EctsMaxRachat = 22,

                // Configuration
                EctsTotalDefaut = 30,
                Version = "1.0.0",
                Description = "Règles de décision par défaut pour les délibérations ESPRIT",
                CreePar = "System",
                ModifiePar = "System",
                DateCreation = DateTime.Now,
                DateModification = DateTime.Now
            };
        }

        /// <summary>
        /// Valider les règles de configuration et retourner les erreurs
        /// </summary>
        public string[] ValiderConfiguration(DecisionRule rules)
        {
            var erreurs = new List<string>();

            if (rules == null)
            {
                erreurs.Add("Les règles ne peuvent pas être nulles");
                return erreurs.ToArray();
            }

            // Validation des seuils MG
            if (rules.SeuilAdmission <= 0 || rules.SeuilAdmission > 20)
                erreurs.Add($"Seuil d'admission invalide: {rules.SeuilAdmission} (doit être entre 0 et 20)");

            if (rules.SeuilPassable > rules.SeuilAssezBien)
                erreurs.Add($"Seuil Passable ({rules.SeuilPassable}) > Seuil Assez Bien ({rules.SeuilAssezBien})");

            if (rules.SeuilAssezBien > rules.SeuilBien)
                erreurs.Add($"Seuil Assez Bien ({rules.SeuilAssezBien}) > Seuil Bien ({rules.SeuilBien})");

            if (rules.SeuilBien > rules.SeuilTresBien)
                erreurs.Add($"Seuil Bien ({rules.SeuilBien}) > Seuil Très Bien ({rules.SeuilTresBien})");

            return erreurs.ToArray();
        }
    }
}
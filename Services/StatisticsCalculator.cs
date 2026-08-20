using System;
using System.Collections.Generic;
using System.Linq;
using DesktopApp.Models;

namespace DesktopApp.Services
{
    /// <summary>
    /// Service pour calculer les statistiques des délibérations
    /// Génère des rapports détaillés sur les décisions et performances
    /// </summary>
    public class StatisticsCalculator
    {
        /// <summary>
        /// Calculer les statistiques complètes d'une délibération
        /// </summary>
        public DeliberationStatistics CalculerStatistiques(List<Etudiant> etudiants)
        {
            if (etudiants == null || etudiants.Count == 0)
                return new DeliberationStatistics();

            var stats = new DeliberationStatistics
            {
                DateCalcul = DateTime.Now,
                NombreTotal = etudiants.Count
            };

            // Statistiques des décisions
            CalculerStatistiquesDecisions(etudiants, stats);

            // Statistiques des moyennes
            CalculerStatistiquesMoyennes(etudiants, stats);

            // Statistiques des mentions
            CalculerStatistiquesMentions(etudiants, stats);

            // Statistiques des ECTS
            CalculerStatistiquesEcts(etudiants, stats);

            // Statistiques des rachats
            CalculerStatistiquesRachats(etudiants, stats);

            // Calculer les pourcentages
            CalculerPourcentages(stats);

            return stats;
        }

        private void CalculerStatistiquesDecisions(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            stats.NombreAdmis = etudiants.Count(e => e.Decision == "Admis");
            stats.NombreAdmisAvecModeration = 0; // Ancienne règle
            stats.NombreDecisionConseil = etudiants.Count(e => e.Decision != null && e.Decision.StartsWith("Admis avec"));
            stats.NombreConseilEcole = etudiants.Count(e => e.Decision == "Conseil d'École");
            stats.NombreRedoubleExclu = etudiants.Count(e => e.Decision != null && e.Decision.Contains("Redouble/Exclu"));
            
            // Total des admis (toutes variantes)
            stats.NombreTotalAdmis = stats.NombreAdmis + stats.NombreDecisionConseil;
            
            // Total nécessitant conseil
            stats.NombreTotalConseil = stats.NombreConseilEcole;
        }

        private void CalculerStatistiquesMoyennes(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            // Moyenne générale globale
            stats.MoyenneGeneraleGlobale = etudiants.Average(e => e.MoyenneGenerale);

            // Moyennes par type de décision
            var admis = etudiants.Where(e => e.Decision == "Admis").ToList();
            if (admis.Any())
                stats.MoyenneAdmis = admis.Average(e => e.MoyenneGenerale);

            var admisModeration = etudiants.Where(e => e.Decision != null && e.Decision.StartsWith("Admis avec")).ToList();
            if (admisModeration.Any())
                stats.MoyenneAdmisAvecModeration = admisModeration.Average(e => e.MoyenneGenerale);

            var conseil = etudiants.Where(e => e.Decision.Contains("Conseil")).ToList();
            if (conseil.Any())
                stats.MoyenneConseil = conseil.Average(e => e.MoyenneGenerale);

            // Moyennes min/max
            stats.MoyenneMini = etudiants.Min(e => e.MoyenneGenerale);
            stats.MoyenneMaxi = etudiants.Max(e => e.MoyenneGenerale);
        }

        private void CalculerStatistiquesMentions(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            stats.NombreTresBien = etudiants.Count(e => e.Mention == "Très Bien");
            stats.NombreBien = etudiants.Count(e => e.Mention == "Bien");
            stats.NombreAssezBien = etudiants.Count(e => e.Mention == "Assez Bien");
            stats.NombrePassable = etudiants.Count(e => e.Mention == "Passable");
        }

        private void CalculerStatistiquesEcts(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            stats.MoyenneEcts = (decimal)etudiants.Average(e => e.EctsValides);
            
            stats.NombreEcts0A15 = etudiants.Count(e => e.EctsValides >= 0 && e.EctsValides <= 15);
            stats.NombreEcts16A22 = etudiants.Count(e => e.EctsValides >= 16 && e.EctsValides <= 22);
            stats.NombreEctsPlus22 = etudiants.Count(e => e.EctsValides > 22);
            
            stats.EctsMin = etudiants.Min(e => e.EctsValides);
            stats.EctsMax = etudiants.Max(e => e.EctsValides);
        }

        private void CalculerStatistiquesRachats(List<Etudiant> etudiants, DeliberationStatistics stats)
        {
            stats.NombreRachatAncien = etudiants.Count(e => e.RescueType == "MG_Ancien");
            stats.NombreRachatNouveau = etudiants.Count(e => e.RescueType == "MG_Nouveau");
            stats.NombreRachatUE = etudiants.Count(e => e.RescueType == "UE_Based");
            stats.NombreTotalRachat = stats.NombreRachatAncien + stats.NombreRachatNouveau + stats.NombreRachatUE;
        }

        private void CalculerPourcentages(DeliberationStatistics stats)
        {
            if (stats.NombreTotal == 0) return;

            stats.PourcentageAdmis = Math.Round((decimal)stats.NombreTotalAdmis / stats.NombreTotal * 100, 2);
            stats.PourcentageConseil = Math.Round((decimal)stats.NombreTotalConseil / stats.NombreTotal * 100, 2);
            stats.PourcentageRedoubleExclu = Math.Round((decimal)stats.NombreRedoubleExclu / stats.NombreTotal * 100, 2);
            stats.PourcentageRachat = Math.Round((decimal)stats.NombreTotalRachat / stats.NombreTotal * 100, 2);
        }

        /// <summary>
        /// Générer un résumé textuel des statistiques
        /// </summary>
        public string GenererResume(DeliberationStatistics stats)
        {
            if (stats == null) return "Aucune statistique disponible";

            var resume = $"=== RÉSUMÉ DE LA DÉLIBÉRATION ===\n\n";
            resume += $"Total étudiants: {stats.NombreTotal}\n\n";
            
            resume += $"DÉCISIONS:\n";
            resume += $"  • Admis: {stats.NombreAdmis} ({(decimal)stats.NombreAdmis/stats.NombreTotal*100:F1}%)\n";
            resume += $"  • Admis avec ECTS non validés: {stats.NombreDecisionConseil} ({(decimal)stats.NombreDecisionConseil/stats.NombreTotal*100:F1}%)\n";
            resume += $"  • Conseil d'École: {stats.NombreConseilEcole} ({(decimal)stats.NombreConseilEcole/stats.NombreTotal*100:F1}%)\n";
            resume += $"  • Redouble/Exclu: {stats.NombreRedoubleExclu} ({(decimal)stats.NombreRedoubleExclu/stats.NombreTotal*100:F1}%)\n\n";
            
            resume += $"MOYENNES:\n";
            resume += $"  • Globale: {stats.MoyenneGeneraleGlobale:F2}\n";
            resume += $"  • Admis: {stats.MoyenneAdmis:F2}\n";
            resume += $"  • Conseil: {stats.MoyenneConseil:F2}\n";
            resume += $"  • Min/Max: {stats.MoyenneMini:F2} - {stats.MoyenneMaxi:F2}\n\n";
            
            resume += $"MENTIONS:\n";
            resume += $"  • Très Bien: {stats.NombreTresBien}\n";
            resume += $"  • Bien: {stats.NombreBien}\n";
            resume += $"  • Assez Bien: {stats.NombreAssezBien}\n";
            resume += $"  • Passable: {stats.NombrePassable}\n\n";
            
            resume += $"RACHATS: {stats.NombreTotalRachat} ({stats.PourcentageRachat}%)\n";
            resume += $"  • Ancien: {stats.NombreRachatAncien}\n";
            resume += $"  • Nouveau: {stats.NombreRachatNouveau}\n";
            resume += $"  • UE: {stats.NombreRachatUE}\n\n";
            
            resume += $"ECTS: Moyenne {stats.MoyenneEcts:F1} (Min: {stats.EctsMin}, Max: {stats.EctsMax})";

            return resume;
        }

        /// <summary>
        /// Comparer les statistiques avec une délibération précédente
        /// </summary>
        public string ComparerAvecPrecedent(DeliberationStatistics current, DeliberationStatistics previous)
        {
            if (previous == null) return "Aucune délibération précédente pour comparaison";

            var comparaison = "=== COMPARAISON AVEC LA DÉLIBÉRATION PRÉCÉDENTE ===\n\n";

            // Évolution du taux d'admission
            decimal evolutionAdmis = current.PourcentageAdmis - previous.PourcentageAdmis;
            string tendanceAdmis = evolutionAdmis > 0 ? "↗️" : evolutionAdmis < 0 ? "↘️" : "→";
            comparaison += $"Taux d'admission: {current.PourcentageAdmis}% ({evolutionAdmis:+0.0;-0.0;0}%) {tendanceAdmis}\n";

            // Évolution de la moyenne
            decimal evolutionMoyenne = current.MoyenneGeneraleGlobale - previous.MoyenneGeneraleGlobale;
            string tendanceMoyenne = evolutionMoyenne > 0 ? "↗️" : evolutionMoyenne < 0 ? "↘️" : "→";
            comparaison += $"Moyenne globale: {current.MoyenneGeneraleGlobale:F2} ({evolutionMoyenne:+0.00;-0.00;0.00}) {tendanceMoyenne}\n";

            // Évolution du taux de rachat
            decimal evolutionRachat = current.PourcentageRachat - previous.PourcentageRachat;
            string tendanceRachat = evolutionRachat > 0 ? "↗️" : evolutionRachat < 0 ? "↘️" : "→";
            comparaison += $"Taux de rachat: {current.PourcentageRachat}% ({evolutionRachat:+0.0;-0.0;0}%) {tendanceRachat}\n";

            return comparaison;
        }
    }

    /// <summary>
    /// Classe représentant les statistiques complètes d'une délibération
    /// </summary>
    public class DeliberationStatistics
    {
        // Métadonnées
        public DateTime DateCalcul { get; set; }
        public int NombreTotal { get; set; }

        // Statistiques des décisions
        public int NombreAdmis { get; set; }
        public int NombreAdmisAvecModeration { get; set; }
        public int NombreDecisionConseil { get; set; }
        public int NombreConseilEcole { get; set; }
        public int NombreRedoubleExclu { get; set; }
        public int NombreTotalAdmis { get; set; }
        public int NombreTotalConseil { get; set; }

        // Statistiques des moyennes
        public decimal MoyenneGeneraleGlobale { get; set; }
        public decimal MoyenneAdmis { get; set; }
        public decimal MoyenneAdmisAvecModeration { get; set; }
        public decimal MoyenneConseil { get; set; }
        public decimal MoyenneMini { get; set; }
        public decimal MoyenneMaxi { get; set; }

        // Statistiques des mentions
        public int NombreTresBien { get; set; }
        public int NombreBien { get; set; }
        public int NombreAssezBien { get; set; }
        public int NombrePassable { get; set; }

        // Statistiques des ECTS
        public decimal MoyenneEcts { get; set; }
        public int NombreEcts0A15 { get; set; }
        public int NombreEcts16A22 { get; set; }
        public int NombreEctsPlus22 { get; set; }
        public int EctsMin { get; set; }
        public int EctsMax { get; set; }

        // Statistiques des rachats
        public int NombreRachatAncien { get; set; }
        public int NombreRachatNouveau { get; set; }
        public int NombreRachatUE { get; set; }
        public int NombreTotalRachat { get; set; }

        // Pourcentages calculés
        public decimal PourcentageAdmis { get; set; }
        public decimal PourcentageConseil { get; set; }
        public decimal PourcentageRedoubleExclu { get; set; }
        public decimal PourcentageRachat { get; set; }

        public DeliberationStatistics()
        {
            DateCalcul = DateTime.Now;
        }
    }
}